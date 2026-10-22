using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Application.Services.Interfaces;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;

    public DashboardService(IUnitOfWork uow, ICurrentUserService current)
    {
        _uow = uow;
        _current = current;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(TaskFilterParams filter, CancellationToken ct = default)
    {
        var scope = _current.Scope;
        var rows = await _uow.Tasks.GetStatsAsync(filter, scope, ct);
        var upcoming = await _uow.Tasks.GetUpcomingAsync(filter, scope, days: 7, take: 5, ct);

        int CountStatus(TaskItemStatus s) => rows.Where(r => r.Status == s).Sum(r => r.Count);

        var total = rows.Sum(r => r.Count);
        var done = CountStatus(TaskItemStatus.Done);

        var byUser = rows
            .GroupBy(r => r.UserId)
            .Select(g => new UserTaskStatsDto
            {
                UserId = g.Key,
                UserName = g.Key is null ? "Unassigned" : g.First().UserName ?? "Unknown",
                ToDo = g.Where(r => r.Status == TaskItemStatus.ToDo).Sum(r => r.Count),
                InProgress = g.Where(r => r.Status == TaskItemStatus.InProgress).Sum(r => r.Count),
                Done = g.Where(r => r.Status == TaskItemStatus.Done).Sum(r => r.Count),
                Overdue = g.Sum(r => r.OverdueCount)
            })
            .OrderBy(u => u.UserId is null)
            .ThenByDescending(u => u.Total)
            .ThenBy(u => u.UserName)
            .ToList();

        var byPriority = Enum.GetValues<TaskPriority>()
            .Select(p => new PriorityCountDto(p, rows.Where(r => r.Priority == p).Sum(r => r.Count)))
            .ToList();

        return new DashboardSummaryDto
        {
            Total = total,
            ToDo = CountStatus(TaskItemStatus.ToDo),
            InProgress = CountStatus(TaskItemStatus.InProgress),
            Done = done,
            Overdue = rows.Sum(r => r.OverdueCount),
            DueThisWeek = rows.Sum(r => r.DueThisWeekCount),
            CompletionRate = total == 0 ? 0 : Math.Round(done * 100.0 / total, 1),
            ByPriority = byPriority,
            ByUser = byUser,
            UpcomingDeadlines = upcoming
        };
    }
}
