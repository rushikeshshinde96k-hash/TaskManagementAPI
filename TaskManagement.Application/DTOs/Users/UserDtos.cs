using System.ComponentModel.DataAnnotations;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Users;

public sealed class UserDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public IReadOnlyList<UserTeamDto> Teams { get; init; } = Array.Empty<UserTeamDto>();
}

public sealed record UserTeamDto(int TeamId, string TeamName);

public sealed class CreateUserRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
    public string Password { get; init; } = string.Empty;

    [Required, EnumDataType(typeof(UserRole))]
    public UserRole Role { get; init; } = UserRole.User;
}

public sealed class UpdateUserRoleRequest
{
    [Required, EnumDataType(typeof(UserRole))]
    public UserRole Role { get; init; }
}

public sealed class UpdateUserStatusRequest
{
    public bool IsActive { get; init; }
}

public sealed class UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
    public string NewPassword { get; init; } = string.Empty;
}

public sealed class UserQueryParams
{
    public string? Search { get; init; }
    public UserRole? Role { get; init; }
    public bool? IsActive { get; init; }
}
