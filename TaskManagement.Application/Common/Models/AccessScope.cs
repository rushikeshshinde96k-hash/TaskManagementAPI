using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Common.Models;

/// <summary>Who is asking. Repositories use it to limit query results to what the caller may see.</summary>
public sealed record AccessScope(int UserId, UserRole Role)
{
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsManager => Role == UserRole.Manager;
}
