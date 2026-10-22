using TaskManagement.Application.Common;
using TaskManagement.Application.Common.Models;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Unit;

public class TaskAccessPolicyTests
{
    private static readonly TaskItem Task = new()
    {
        Id = 1,
        CreatedById = 2,
        AssignedToId = 4,
        TeamId = 10,
        Team = new Team { Id = 10, ManagerId = 2 }
    };

    [Theory]
    [InlineData(1, UserRole.Admin, true, true)]     // admin
    [InlineData(2, UserRole.Manager, true, true)]   // team manager / creator
    [InlineData(3, UserRole.Manager, false, false)] // other manager
    [InlineData(4, UserRole.User, true, false)]     // assignee
    [InlineData(5, UserRole.User, false, false)]    // unrelated user
    public void Permissions_FollowRoleRules(int userId, UserRole role, bool canView, bool canManage)
    {
        var scope = new AccessScope(userId, role);

        Assert.Equal(canView, TaskAccessPolicy.CanView(Task, scope));
        Assert.Equal(canManage, TaskAccessPolicy.CanManage(Task, scope));
        Assert.Equal(canView, TaskAccessPolicy.CanChangeStatus(Task, scope));
    }
}
