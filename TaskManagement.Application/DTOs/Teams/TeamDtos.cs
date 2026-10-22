using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Teams;

public class TeamDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public int MemberCount { get; init; }
    public int TaskCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class TeamDetailDto : TeamDto
{
    public IReadOnlyList<TeamMemberDto> Members { get; init; } = Array.Empty<TeamMemberDto>();
}

public sealed record TeamMemberDto(int UserId, string FullName, string Email, UserRole Role, DateTime JoinedAt);

public sealed class CreateTeamRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    public int? ManagerId { get; init; }
}

public sealed class UpdateTeamRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    public int? ManagerId { get; init; }
}

public sealed class AddTeamMembersRequest
{
    [Required, MinLength(1, ErrorMessage = "Select at least one user.")]
    public List<int> UserIds { get; init; } = new();
}
