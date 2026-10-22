using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Application.Mappings;
using TaskManagement.Application.Services.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;
    private readonly IPasswordHasher _hasher;

    public UserService(IUnitOfWork uow, ICurrentUserService current, IPasswordHasher hasher)
    {
        _uow = uow;
        _current = current;
        _hasher = hasher;
    }

    public async Task<UserDto> GetCurrentAsync(CancellationToken ct = default)
    {
        var user = await _uow.Users.GetWithTeamsAsync(_current.UserId, ct)
                   ?? throw new NotFoundException(nameof(User), _current.UserId);
        return user.ToDto();
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(UserQueryParams query, CancellationToken ct = default)
    {
        var users = await _uow.Users.SearchAsync(query, ct);
        return users.Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (_current.Role == UserRole.User && id != _current.UserId)
            throw new ForbiddenException();

        var user = await _uow.Users.GetWithTeamsAsync(id, ct) ?? throw new NotFoundException(nameof(User), id);
        return user.ToDto();
    }

    /// <summary>
    /// Users the caller may assign a task to.
    /// Admin: every active Manager/User (optionally limited to a team).
    /// Manager: members of the teams they manage, plus themselves.
    /// </summary>
    public async Task<IReadOnlyList<UserDto>> GetAssignableAsync(int? teamId, CancellationToken ct = default)
    {
        if (_current.Role == UserRole.User)
            throw new ForbiddenException();

        IEnumerable<User> candidates;

        if (teamId.HasValue)
        {
            var team = await _uow.Teams.GetWithMembersAsync(teamId.Value, ct)
                       ?? throw new NotFoundException(nameof(Team), teamId.Value);

            if (_current.Role == UserRole.Manager && team.ManagerId != _current.UserId)
                throw new ForbiddenException("You can only assign tasks within teams you manage.");

            candidates = team.Members.Select(m => m.User);
            if (team.Manager is not null) candidates = candidates.Append(team.Manager);
        }
        else if (_current.Role == UserRole.Admin)
        {
            candidates = await _uow.Users.SearchAsync(new UserQueryParams { IsActive = true }, ct);
            candidates = candidates.Where(u => u.Role != UserRole.Admin);
        }
        else
        {
            var all = new List<User>();
            foreach (var id in await _uow.Teams.GetManagedTeamIdsAsync(_current.UserId, ct))
            {
                var team = await _uow.Teams.GetWithMembersAsync(id, ct);
                if (team is not null) all.AddRange(team.Members.Select(m => m.User));
            }
            var self = await _uow.Users.GetByIdAsync(_current.UserId, ct);
            if (self is not null) all.Add(self);
            candidates = all;
        }

        return candidates
            .Where(u => u.IsActive)
            .DistinctBy(u => u.Id)
            .OrderBy(u => u.FullName)
            .Select(u => u.ToDto())
            .ToList();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _uow.Users.EmailExistsAsync(email, ct))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = request.Role,
            IsActive = true
        };
        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<UserDto> UpdateRoleAsync(int id, UpdateUserRoleRequest request, CancellationToken ct = default)
    {
        if (id == _current.UserId)
            throw new BadRequestException("You cannot change your own role.");

        var user = await _uow.Users.GetWithTeamsAsync(id, ct) ?? throw new NotFoundException(nameof(User), id);
        user.Role = request.Role;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<UserDto> UpdateStatusAsync(int id, UpdateUserStatusRequest request, CancellationToken ct = default)
    {
        if (id == _current.UserId)
            throw new BadRequestException("You cannot deactivate your own account.");

        var user = await _uow.Users.GetWithTeamsAsync(id, ct) ?? throw new NotFoundException(nameof(User), id);
        user.IsActive = request.IsActive;
        _uow.Users.Update(user);

        // A deactivated user must not be able to keep refreshing their session.
        if (!request.IsActive)
            await _uow.RefreshTokens.RevokeAllForUserAsync(id, ct);

        await _uow.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetWithTeamsAsync(_current.UserId, ct)
                   ?? throw new NotFoundException(nameof(User), _current.UserId);
        user.FullName = request.FullName.Trim();
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(_current.UserId, ct)
                   ?? throw new NotFoundException(nameof(User), _current.UserId);

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Current password is incorrect.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        _uow.Users.Update(user);
        await _uow.RefreshTokens.RevokeAllForUserAsync(user.Id, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
