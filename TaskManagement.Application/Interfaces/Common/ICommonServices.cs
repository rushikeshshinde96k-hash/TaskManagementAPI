using TaskManagement.Application.Common.Models;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Interfaces.Common;

/// <summary>The authenticated caller, resolved from the request by the API layer.</summary>
public interface ICurrentUserService
{
    int UserId { get; }
    UserRole Role { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    AccessScope Scope { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashToken(string token);
    int RefreshTokenLifetimeDays { get; }
}

public interface IEmailSender
{
    /// <returns>true when the message was handed off (or mocked) successfully.</returns>
    Task<bool> SendAsync(string toEmail, string toName, string subject, string body, CancellationToken ct = default);
}
