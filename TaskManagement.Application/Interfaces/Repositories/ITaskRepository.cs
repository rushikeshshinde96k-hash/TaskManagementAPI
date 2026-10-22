using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces.Repositories;

public interface ITaskRepository : IGenericRepository<TaskItem>
{
    /// <summary>Loads a task with Team, AssignedTo and CreatedBy.</summary>
    Task<TaskItem?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<TaskDto?> GetDtoAsync(int id, CancellationToken ct = default);
    Task<PagedResult<TaskDto>> GetPagedAsync(TaskFilterParams filter, AccessScope scope, CancellationToken ct = default);
    Task<IReadOnlyList<TaskStatsRow>> GetStatsAsync(TaskFilterParams filter, AccessScope scope, CancellationToken ct = default);
    Task<IReadOnlyList<TaskDto>> GetUpcomingAsync(TaskFilterParams filter, AccessScope scope, int days, int take, CancellationToken ct = default);
}
