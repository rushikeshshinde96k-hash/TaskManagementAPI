using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Dashboard;

public sealed class DashboardSummaryDto
{
    public int Total { get; init; }
    public int ToDo { get; init; }
    public int InProgress { get; init; }
    public int Done { get; init; }
    public int Overdue { get; init; }
    public int DueThisWeek { get; init; }
    public double CompletionRate { get; init; }
    public IReadOnlyList<PriorityCountDto> ByPriority { get; init; } = Array.Empty<PriorityCountDto>();
    public IReadOnlyList<UserTaskStatsDto> ByUser { get; init; } = Array.Empty<UserTaskStatsDto>();
    public IReadOnlyList<TaskDto> UpcomingDeadlines { get; init; } = Array.Empty<TaskDto>();
}

public sealed record PriorityCountDto(TaskPriority Priority, int Count);

public sealed class UserTaskStatsDto
{
    public int? UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int ToDo { get; init; }
    public int InProgress { get; init; }
    public int Done { get; init; }
    public int Overdue { get; init; }
    public int Total => ToDo + InProgress + Done;
}

/// <summary>Raw aggregate row produced by the repository: one row per (assignee, status, priority).</summary>
public sealed record TaskStatsRow(int? UserId, string? UserName, TaskItemStatus Status, TaskPriority Priority, int Count, int OverdueCount, int DueThisWeekCount);
