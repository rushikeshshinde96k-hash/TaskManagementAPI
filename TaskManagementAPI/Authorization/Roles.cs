namespace TaskManagementAPI.Authorization;

/// <summary>Role names for <c>[Authorize(Roles = ...)]</c>. Must match <c>UserRole</c> enum names.</summary>
public static class Roles
{
    public const string Admin = nameof(TaskManagement.Domain.Enums.UserRole.Admin);
    public const string Manager = nameof(TaskManagement.Domain.Enums.UserRole.Manager);
    public const string User = nameof(TaskManagement.Domain.Enums.UserRole.User);

    public const string AdminOrManager = Admin + "," + Manager;
}
