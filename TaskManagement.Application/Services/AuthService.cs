using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Application.Mappings;
using TaskManagement.Application.Services.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AuthService(IUnitOfWork uow, IPasswordHasher hasher, ITokenService tokens)
    {
        _uow = uow;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        if (await _uow.Users.EmailExistsAsync(email, ct))
            throw new ConflictException("An account with this email already exists.");

        // Self-registration always creates a regular User; elevated roles are granted by an Admin.
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            Role = UserRole.User,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByEmailAsync(NormalizeEmail(request.Email), ct);

        // Same message for unknown email and wrong password so accounts can't be enumerated.
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("Your account has been deactivated. Contact an administrator.");

        user.LastLoginAt = DateTime.UtcNow;
        _uow.Users.Update(user);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await _uow.RefreshTokens.GetByTokenHashAsync(_tokens.HashToken(request.RefreshToken), ct);

        if (stored is null || !stored.IsActive)
            throw new UnauthorizedException("Session expired. Please sign in again.");

        if (!stored.User.IsActive)
            throw new ForbiddenException("Your account has been deactivated. Contact an administrator.");

        // Rotate: the presented token can only be used once.
        stored.RevokedAt = DateTime.UtcNow;
        _uow.RefreshTokens.Update(stored);

        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var stored = await _uow.RefreshTokens.GetByTokenHashAsync(_tokens.HashToken(request.RefreshToken), ct);
        if (stored is null || stored.RevokedAt is not null) return;

        stored.RevokedAt = DateTime.UtcNow;
        _uow.RefreshTokens.Update(stored);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = _tokens.GenerateAccessToken(user);
        var refresh = _tokens.GenerateRefreshToken();
        var refreshExpires = DateTime.UtcNow.AddDays(_tokens.RefreshTokenLifetimeDays);

        await _uow.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = _tokens.HashToken(refresh),
            ExpiresAt = refreshExpires
        }, ct);
        await _uow.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = access.Token,
            AccessTokenExpiresAt = access.ExpiresAt,
            RefreshToken = refresh,
            RefreshTokenExpiresAt = refreshExpires,
            User = user.ToDto()
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
