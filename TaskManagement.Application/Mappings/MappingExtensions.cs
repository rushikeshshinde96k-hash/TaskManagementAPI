using System.Linq.Expressions;
using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Mappings;

public static class MappingExtensions
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        Teams = user.TeamMemberships
            .Where(m => m.Team is not null)
            .Select(m => new UserTeamDto(m.TeamId, m.Team.Name))
            .ToList()
    };

    public static TeamDetailDto ToDetailDto(this Team team, int taskCount) => new()
    {
        Id = team.Id,
        Name = team.Name,
        Description = team.Description,
        ManagerId = team.ManagerId,
        ManagerName = team.Manager?.FullName,
        MemberCount = team.Members.Count,
        TaskCount = taskCount,
        CreatedAt = team.CreatedAt,
        Members = team.Members
            .OrderBy(m => m.User.FullName)
            .Select(m => new TeamMemberDto(m.UserId, m.User.FullName, m.User.Email, m.User.Role, m.JoinedAt))
            .ToList()
    };

    public static CommentDto ToDto(this TaskComment comment) => new()
    {
        Id = comment.Id,
        TaskId = comment.TaskId,
        Content = comment.Content,
        UserId = comment.UserId,
        UserName = comment.User?.FullName ?? string.Empty,
        UserRole = comment.User?.Role ?? UserRole.User,
        CreatedAt = comment.CreatedAt,
        UpdatedAt = comment.UpdatedAt
    };

    public static NotificationDto ToDto(this Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        EmailSent = n.EmailSent,
        TaskId = n.TaskId,
        CreatedAt = n.CreatedAt
    };

    /// <summary>Server-side projection used by repositories so lists don't load navigation collections.</summary>
    public static Expression<Func<TaskItem, TaskDto>> TaskProjection(DateTime today) => t => new TaskDto
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status,
        Priority = t.Priority,
        DueDate = t.DueDate,
        CompletedAt = t.CompletedAt,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        TeamId = t.TeamId,
        TeamName = t.Team != null ? t.Team.Name : null,
        TeamManagerId = t.Team != null ? t.Team.ManagerId : null,
        AssignedToId = t.AssignedToId,
        AssignedToName = t.AssignedTo != null ? t.AssignedTo.FullName : null,
        CreatedById = t.CreatedById,
        CreatedByName = t.CreatedBy.FullName,
        CommentCount = t.Comments.Count,
        IsOverdue = t.Status != TaskItemStatus.Done && t.DueDate != null && t.DueDate < today
    };

    public static string ToDisplay(this TaskItemStatus status) => status switch
    {
        TaskItemStatus.ToDo => "To Do",
        TaskItemStatus.InProgress => "In Progress",
        TaskItemStatus.Done => "Done",
        _ => status.ToString()
    };
}
