using TaskManagement.Application.Common.Models;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Common;

/// <summary>
/// Single source of truth for per-task permissions. <paramref name="task"/> must have <c>Team</c> loaded.
/// The query-side equivalent lives in the task repository's visibility filter.
/// </summary>
public static class TaskAccessPolicy
{
    /// <summary>Admin: any task. Manager: tasks they created or that belong to a team they manage.</summary>
    public static bool CanManage(TaskItem task, AccessScope scope) => scope.Role switch
    {
        UserRole.Admin => true,
        UserRole.Manager => task.CreatedById == scope.UserId || task.Team?.ManagerId == scope.UserId,
        _ => false
    };

    public static bool CanView(TaskItem task, AccessScope scope) =>
        CanManage(task, scope) || task.AssignedToId == scope.UserId;

    public static bool CanChangeStatus(TaskItem task, AccessScope scope) =>
        CanManage(task, scope) || task.AssignedToId == scope.UserId;
}
