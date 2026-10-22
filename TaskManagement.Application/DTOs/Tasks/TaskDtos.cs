using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Tasks;

public sealed class TaskDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskItemStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public int? TeamId { get; init; }
    public string? TeamName { get; init; }
    public int? TeamManagerId { get; init; }
    public int? AssignedToId { get; init; }
    public string? AssignedToName { get; init; }
    public int CreatedById { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
    public int CommentCount { get; init; }
    public bool IsOverdue { get; init; }
}

public sealed class CreateTaskRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    public DateTime? DueDate { get; init; }
    public int? TeamId { get; init; }
    public int? AssignedToId { get; init; }
}

public sealed class UpdateTaskRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; init; }

    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    [EnumDataType(typeof(TaskItemStatus))]
    public TaskItemStatus Status { get; init; } = TaskItemStatus.ToDo;

    public DateTime? DueDate { get; init; }
    public int? TeamId { get; init; }
    public int? AssignedToId { get; init; }
}

public sealed class UpdateTaskStatusRequest
{
    [Required, EnumDataType(typeof(TaskItemStatus))]
    public TaskItemStatus Status { get; init; }
}

public sealed class AssignTaskRequest
{
    /// <summary>null un-assigns the task.</summary>
    public int? AssignedToId { get; init; }
}

/// <summary>Deadline presets accepted by the <c>deadline</c> filter.</summary>
public enum DeadlineFilter
{
    Overdue,
    Today,
    ThisWeek,
    ThisMonth,
    NoDueDate
}

public sealed class TaskFilterParams
{
    public TaskItemStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public DeadlineFilter? Deadline { get; init; }
    public DateTime? DueFrom { get; init; }
    public DateTime? DueTo { get; init; }
    public int? AssignedToId { get; init; }
    public int? TeamId { get; init; }
    public bool AssignedToMe { get; init; }

    [StringLength(100)]
    public string? Search { get; init; }

    /// <summary>dueDate | priority | status | title | createdAt (default)</summary>
    public string? SortBy { get; init; }

    /// <summary>asc | desc (default)</summary>
    public string? SortDir { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}
