using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Application.Mappings;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class TaskRepository : GenericRepository<TaskItem>, ITaskRepository
{
    public TaskRepository(AppDbContext context) : base(context) { }

    // Due dates are calendar dates: a task is overdue from the day after its due date.
    private static DateTime Today => DateTime.UtcNow.Date;

    public Task<TaskItem?> GetWithDetailsAsync(int id, CancellationToken ct = default)
        => Set.Include(t => t.Team).ThenInclude(t => t!.Manager)
              .Include(t => t.AssignedTo)
              .Include(t => t.CreatedBy)
              .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TaskDto?> GetDtoAsync(int id, CancellationToken ct = default)
        => Set.AsNoTracking()
              .Where(t => t.Id == id)
              .Select(MappingExtensions.TaskProjection(Today))
              .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<TaskDto>> GetPagedAsync(TaskFilterParams filter, AccessScope scope, CancellationToken ct = default)
    {
        var q = Query(filter, scope);
        var total = await q.CountAsync(ct);

        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 100);

        var items = await ApplySort(q, filter.SortBy, filter.SortDir)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(MappingExtensions.TaskProjection(Today))
            .ToListAsync(ct);

        return new PagedResult<TaskDto>(items, page, size, total);
    }

    public async Task<IReadOnlyList<TaskStatsRow>> GetStatsAsync(TaskFilterParams filter, AccessScope scope, CancellationToken ct = default)
    {
        var today = Today;
        var weekEnd = today.AddDays(7);

        var groups = await Query(filter, scope)
            .GroupBy(t => new { t.AssignedToId, t.Status, t.Priority })
            .Select(g => new
            {
                g.Key.AssignedToId,
                g.Key.Status,
                g.Key.Priority,
                Count = g.Count(),
                Overdue = g.Count(t => t.Status != TaskItemStatus.Done && t.DueDate < today),
                DueThisWeek = g.Count(t => t.Status != TaskItemStatus.Done && t.DueDate >= today && t.DueDate < weekEnd)
            })
            .ToListAsync(ct);

        var userIds = groups.Where(g => g.AssignedToId.HasValue).Select(g => g.AssignedToId!.Value).Distinct().ToList();
        var names = await Context.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return groups
            .Select(g => new TaskStatsRow(
                g.AssignedToId,
                g.AssignedToId.HasValue ? names.GetValueOrDefault(g.AssignedToId.Value) : null,
                g.Status, g.Priority, g.Count, g.Overdue, g.DueThisWeek))
            .ToList();
    }

    public async Task<IReadOnlyList<TaskDto>> GetUpcomingAsync(TaskFilterParams filter, AccessScope scope, int days, int take, CancellationToken ct = default)
    {
        var today = Today;
        var end = today.AddDays(days);

        return await Query(filter, scope)
            .Where(t => t.Status != TaskItemStatus.Done && t.DueDate >= today && t.DueDate < end)
            .OrderBy(t => t.DueDate)
            .Take(take)
            .Select(MappingExtensions.TaskProjection(today))
            .ToListAsync(ct);
    }

    private IQueryable<TaskItem> Query(TaskFilterParams filter, AccessScope scope)
        => ApplyFilter(ApplyVisibility(Set.AsNoTracking(), scope), filter, scope);

    /// <summary>Query-side mirror of <c>TaskAccessPolicy.CanView</c>.</summary>
    private static IQueryable<TaskItem> ApplyVisibility(IQueryable<TaskItem> q, AccessScope scope)
    {
        var uid = scope.UserId;
        return scope.Role switch
        {
            UserRole.Admin => q,
            UserRole.Manager => q.Where(t => t.CreatedById == uid
                                             || t.AssignedToId == uid
                                             || (t.Team != null && t.Team.ManagerId == uid)),
            _ => q.Where(t => t.AssignedToId == uid)
        };
    }

    private static IQueryable<TaskItem> ApplyFilter(IQueryable<TaskItem> q, TaskFilterParams f, AccessScope scope)
    {
        var today = Today;

        if (f.Status.HasValue) q = q.Where(t => t.Status == f.Status.Value);
        if (f.Priority.HasValue) q = q.Where(t => t.Priority == f.Priority.Value);
        if (f.TeamId.HasValue) q = q.Where(t => t.TeamId == f.TeamId.Value);
        if (f.AssignedToId.HasValue) q = q.Where(t => t.AssignedToId == f.AssignedToId.Value);
        if (f.AssignedToMe) q = q.Where(t => t.AssignedToId == scope.UserId);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = f.Search.Trim();
            q = q.Where(t => t.Title.Contains(term) || (t.Description != null && t.Description.Contains(term)));
        }

        if (f.DueFrom.HasValue)
        {
            var from = f.DueFrom.Value.Date;
            q = q.Where(t => t.DueDate >= from);
        }
        if (f.DueTo.HasValue)
        {
            var to = f.DueTo.Value.Date.AddDays(1);
            q = q.Where(t => t.DueDate < to);
        }

        q = f.Deadline switch
        {
            DeadlineFilter.Overdue => q.Where(t => t.Status != TaskItemStatus.Done && t.DueDate < today),
            DeadlineFilter.Today => q.Where(t => t.DueDate >= today && t.DueDate < today.AddDays(1)),
            DeadlineFilter.ThisWeek => q.Where(t => t.DueDate >= today && t.DueDate < today.AddDays(7)),
            DeadlineFilter.ThisMonth => q.Where(t => t.DueDate >= today && t.DueDate < today.AddDays(30)),
            DeadlineFilter.NoDueDate => q.Where(t => t.DueDate == null),
            _ => q
        };

        return q;
    }

    private static IQueryable<TaskItem> ApplySort(IQueryable<TaskItem> q, string? sortBy, string? sortDir)
    {
        var desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLowerInvariant()) switch
        {
            // Tasks without a due date go last regardless of direction.
            "duedate" => desc
                ? q.OrderBy(t => t.DueDate == null).ThenByDescending(t => t.DueDate)
                : q.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate),
            // Priority/Status are stored as strings, so sort on the underlying enum order explicitly.
            "priority" => desc
                ? q.OrderByDescending(t => t.Priority == TaskPriority.Critical ? 4 : t.Priority == TaskPriority.High ? 3 : t.Priority == TaskPriority.Medium ? 2 : 1).ThenBy(t => t.DueDate)
                : q.OrderBy(t => t.Priority == TaskPriority.Critical ? 4 : t.Priority == TaskPriority.High ? 3 : t.Priority == TaskPriority.Medium ? 2 : 1).ThenBy(t => t.DueDate),
            "status" => desc
                ? q.OrderByDescending(t => t.Status == TaskItemStatus.Done ? 3 : t.Status == TaskItemStatus.InProgress ? 2 : 1)
                : q.OrderBy(t => t.Status == TaskItemStatus.Done ? 3 : t.Status == TaskItemStatus.InProgress ? 2 : 1),
            "title" => desc ? q.OrderByDescending(t => t.Title) : q.OrderBy(t => t.Title),
            _ => desc ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt)
        };
    }
}
