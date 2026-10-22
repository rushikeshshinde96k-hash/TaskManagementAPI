using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Application.Mappings;
using TaskManagement.Application.Services.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public sealed class TeamService : ITeamService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;
    private readonly INotificationService _notifications;

    public TeamService(IUnitOfWork uow, ICurrentUserService current, INotificationService notifications)
    {
        _uow = uow;
        _current = current;
        _notifications = notifications;
    }

    public Task<IReadOnlyList<TeamDto>> GetAllAsync(CancellationToken ct = default)
        => _uow.Teams.GetVisibleTeamsAsync(_current.Scope, ct);

    public async Task<TeamDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var team = await LoadTeamAsync(id, ct);

        var canView = _current.Role == UserRole.Admin
                      || team.ManagerId == _current.UserId
                      || team.Members.Any(m => m.UserId == _current.UserId);
        if (!canView) throw new ForbiddenException("You are not a member of this team.");

        return team.ToDetailDto(await _uow.Teams.CountTasksAsync(id, ct));
    }

    public async Task<TeamDetailDto> CreateAsync(CreateTeamRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _uow.Teams.NameExistsAsync(name, null, ct))
            throw new ConflictException($"A team named '{name}' already exists.");

        await EnsureValidManagerAsync(request.ManagerId, ct);

        var team = new Team { Name = name, Description = request.Description?.Trim(), ManagerId = request.ManagerId };
        await _uow.Teams.AddAsync(team, ct);
        await _uow.SaveChangesAsync(ct);

        return await GetDetailAsync(team.Id, ct);
    }

    public async Task<TeamDetailDto> UpdateAsync(int id, UpdateTeamRequest request, CancellationToken ct = default)
    {
        var team = await LoadTeamAsync(id, ct);
        var name = request.Name.Trim();

        if (await _uow.Teams.NameExistsAsync(name, id, ct))
            throw new ConflictException($"A team named '{name}' already exists.");

        await EnsureValidManagerAsync(request.ManagerId, ct);

        team.Name = name;
        team.Description = request.Description?.Trim();
        team.ManagerId = request.ManagerId;
        _uow.Teams.Update(team);
        await _uow.SaveChangesAsync(ct);

        return await GetDetailAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var team = await _uow.Teams.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Team), id);
        _uow.Teams.Remove(team);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<TeamDetailDto> AddMembersAsync(int teamId, AddTeamMembersRequest request, CancellationToken ct = default)
    {
        var team = await LoadTeamAsync(teamId, ct);
        EnsureCanManageMembers(team);

        var ids = request.UserIds.Distinct().ToList();
        var users = await _uow.Users.GetByIdsAsync(ids, ct);

        var missing = ids.Except(users.Select(u => u.Id)).ToList();
        if (missing.Count > 0)
            throw new BadRequestException($"Unknown user id(s): {string.Join(", ", missing)}.");

        foreach (var user in users)
        {
            if (!user.IsActive)
                throw new BadRequestException($"{user.FullName} is deactivated and cannot be added to a team.");
            if (user.Role == UserRole.Admin)
                throw new BadRequestException("Administrators cannot be added as team members.");
            if (_current.Role == UserRole.Manager && user.Role != UserRole.User)
                throw new ForbiddenException("Managers can only add users with the 'User' role to their teams.");
        }

        var actor = await _uow.Users.GetByIdAsync(_current.UserId, ct) ?? throw new UnauthorizedException();
        var added = new List<User>();

        foreach (var user in users.Where(u => team.Members.All(m => m.UserId != u.Id)))
        {
            await _uow.Teams.AddMemberAsync(new TeamMember { TeamId = teamId, UserId = user.Id }, ct);
            added.Add(user);
        }
        await _uow.SaveChangesAsync(ct);

        foreach (var user in added)
            await _notifications.NotifyAddedToTeamAsync(team, user, actor, ct);

        return await GetDetailAsync(teamId, ct);
    }

    public async Task<TeamDetailDto> RemoveMemberAsync(int teamId, int userId, CancellationToken ct = default)
    {
        var team = await LoadTeamAsync(teamId, ct);
        EnsureCanManageMembers(team);

        var member = await _uow.Teams.GetMemberAsync(teamId, userId, ct)
                     ?? throw new NotFoundException("Team member", userId);

        _uow.Teams.RemoveMember(member);
        await _uow.SaveChangesAsync(ct);

        return await GetDetailAsync(teamId, ct);
    }

    private async Task<Team> LoadTeamAsync(int id, CancellationToken ct)
        => await _uow.Teams.GetWithMembersAsync(id, ct) ?? throw new NotFoundException(nameof(Team), id);

    private async Task<TeamDetailDto> GetDetailAsync(int id, CancellationToken ct)
    {
        var team = await LoadTeamAsync(id, ct);
        return team.ToDetailDto(await _uow.Teams.CountTasksAsync(id, ct));
    }

    private void EnsureCanManageMembers(Team team)
    {
        if (_current.Role == UserRole.Admin) return;
        if (_current.Role == UserRole.Manager && team.ManagerId == _current.UserId) return;
        throw new ForbiddenException("Only an administrator or this team's manager can change its members.");
    }

    private async Task EnsureValidManagerAsync(int? managerId, CancellationToken ct)
    {
        if (managerId is null) return;

        var manager = await _uow.Users.GetByIdAsync(managerId.Value, ct)
                      ?? throw new BadRequestException($"Manager with id '{managerId}' does not exist.");

        if (manager.Role != UserRole.Manager)
            throw new BadRequestException($"{manager.FullName} does not have the Manager role.");
        if (!manager.IsActive)
            throw new BadRequestException($"{manager.FullName} is deactivated.");
    }
}
