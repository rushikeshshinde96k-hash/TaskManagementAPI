using TaskManagement.Application.Common;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Application.Services.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

/// <summary>
/// Task rules:
///  - Admin sees and manages every task and may assign to any Manager or User.
///  - Manager sees/manages tasks they created or that belong to teams they manage,
///    and may only assign to members of those teams (or themselves).
///  - User sees tasks assigned to them and may only change their status.
/// </summary>
public sealed class TaskService : ITaskService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;
    private readonly INotificationService _notifications;

    public TaskService(IUnitOfWork uow, ICurrentUserService current, INotificationService notifications)
    {
        _uow = uow;
        _current = current;
        _notifications = notifications;
    }

    public Task<PagedResult<TaskDto>> GetAllAsync(TaskFilterParams filter, CancellationToken ct = default)
        => _uow.Tasks.GetPagedAsync(filter, _current.Scope, ct);

    public async Task<TaskDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var task = await LoadTaskAsync(id, ct);
        if (!CanView(task)) throw new ForbiddenException("You do not have access to this task.");
        return await ToDtoAsync(id, ct);
    }

    public async Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        if (_current.Role == UserRole.User)
            throw new ForbiddenException("Only administrators and managers can create tasks.");

        EnsureDueDateNotInPast(request.DueDate);
        var assignee = await ValidateAssignmentAsync(request.TeamId, request.AssignedToId, ct);

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            Status = TaskItemStatus.ToDo,
            DueDate = request.DueDate,
            TeamId = request.TeamId,
            AssignedToId = request.AssignedToId,
            CreatedById = _current.UserId
        };

        await _uow.Tasks.AddAsync(task, ct);
        await _uow.SaveChangesAsync(ct);

        if (assignee is not null)
            await _notifications.NotifyTaskAssignedAsync(task, assignee, await GetActorAsync(ct), ct);

        return await ToDtoAsync(task.Id, ct);
    }

    public async Task<TaskDto> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await LoadTaskAsync(id, ct);
        if (!CanManage(task)) throw new ForbiddenException("You cannot edit this task.");

        if (request.DueDate != task.DueDate) EnsureDueDateNotInPast(request.DueDate);

        var assigneeChanged = request.AssignedToId != task.AssignedToId;
        var teamChanged = request.TeamId != task.TeamId;
        User? newAssignee = null;
        if (assigneeChanged || teamChanged)
            newAssignee = await ValidateAssignmentAsync(request.TeamId, request.AssignedToId, ct);

        var oldStatus = task.Status;

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.TeamId = request.TeamId;
        task.AssignedToId = request.AssignedToId;
        ApplyStatus(task, request.Status);
        task.UpdatedAt = DateTime.UtcNow;

        _uow.Tasks.Update(task);
        await _uow.SaveChangesAsync(ct);

        var actor = await GetActorAsync(ct);
        if (assigneeChanged && newAssignee is not null)
            await _notifications.NotifyTaskAssignedAsync(task, newAssignee, actor, ct);
        if (oldStatus != task.Status)
            await NotifyStatusAsync(task.Id, oldStatus, actor, ct);

        return await ToDtoAsync(id, ct);
    }

    public async Task<TaskDto> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await LoadTaskAsync(id, ct);
        if (!TaskAccessPolicy.CanChangeStatus(task, _current.Scope))
            throw new ForbiddenException("Only the assignee or the task's manager can change its status.");

        if (task.Status == request.Status) return await ToDtoAsync(id, ct);

        var oldStatus = task.Status;
        ApplyStatus(task, request.Status);
        task.UpdatedAt = DateTime.UtcNow;
        _uow.Tasks.Update(task);
        await _uow.SaveChangesAsync(ct);

        await NotifyStatusAsync(task.Id, oldStatus, await GetActorAsync(ct), ct);
        return await ToDtoAsync(id, ct);
    }

    public async Task<TaskDto> AssignAsync(int id, AssignTaskRequest request, CancellationToken ct = default)
    {
        var task = await LoadTaskAsync(id, ct);
        if (!CanManage(task)) throw new ForbiddenException("You cannot assign this task.");

        if (task.AssignedToId == request.AssignedToId) return await ToDtoAsync(id, ct);

        var assignee = await ValidateAssignmentAsync(task.TeamId, request.AssignedToId, ct);
        task.AssignedToId = request.AssignedToId;
        task.UpdatedAt = DateTime.UtcNow;
        _uow.Tasks.Update(task);
        await _uow.SaveChangesAsync(ct);

        if (assignee is not null)
            await _notifications.NotifyTaskAssignedAsync(task, assignee, await GetActorAsync(ct), ct);

        return await ToDtoAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var task = await LoadTaskAsync(id, ct);
        if (!CanManage(task)) throw new ForbiddenException("You cannot delete this task.");

        _uow.Tasks.Remove(task);
        await _uow.SaveChangesAsync(ct);
    }

    // ----- rules -----

    private bool CanView(TaskItem task) => TaskAccessPolicy.CanView(task, _current.Scope);

    private bool CanManage(TaskItem task) => TaskAccessPolicy.CanManage(task, _current.Scope);

    /// <summary>Validates the team/assignee pair for the current caller and returns the assignee, if any.</summary>
    private async Task<User?> ValidateAssignmentAsync(int? teamId, int? assigneeId, CancellationToken ct)
    {
        Team? team = null;
        if (teamId.HasValue)
        {
            team = await _uow.Teams.GetByIdAsync(teamId.Value, ct)
                   ?? throw new BadRequestException($"Team with id '{teamId}' does not exist.");

            if (_current.Role == UserRole.Manager && team.ManagerId != _current.UserId)
                throw new ForbiddenException("Managers can only create tasks for teams they manage.");
        }

        if (!assigneeId.HasValue) return null;

        var assignee = await _uow.Users.GetByIdAsync(assigneeId.Value, ct)
                       ?? throw new BadRequestException($"User with id '{assigneeId}' does not exist.");

        if (!assignee.IsActive)
            throw new BadRequestException($"{assignee.FullName} is deactivated and cannot be assigned tasks.");

        if (team is not null)
        {
            var inTeam = team.ManagerId == assignee.Id || await _uow.Teams.IsMemberAsync(team.Id, assignee.Id, ct);
            if (!inTeam)
                throw new BadRequestException($"{assignee.FullName} is not a member of team '{team.Name}'.");
        }

        if (_current.Role == UserRole.Admin)
        {
            if (assignee.Role == UserRole.Admin && assignee.Id != _current.UserId)
                throw new BadRequestException("Tasks can only be assigned to managers and users.");
        }
        else if (_current.Role == UserRole.Manager && assignee.Id != _current.UserId && team is null)
        {
            if (!await _uow.Teams.IsUserInManagedTeamsAsync(_current.UserId, assignee.Id, ct))
                throw new ForbiddenException("Managers can only assign tasks to members of their own teams.");
        }

        return assignee;
    }

    private static void ApplyStatus(TaskItem task, TaskItemStatus status)
    {
        if (task.Status == status) return;
        task.Status = status;
        task.CompletedAt = status == TaskItemStatus.Done ? DateTime.UtcNow : null;
    }

    private static void EnsureDueDateNotInPast(DateTime? dueDate)
    {
        if (dueDate.HasValue && dueDate.Value.Date < DateTime.UtcNow.Date)
            throw new BadRequestException("Due date cannot be in the past.",
                new Dictionary<string, string[]> { ["DueDate"] = ["Due date cannot be in the past."] });
    }

    private async Task NotifyStatusAsync(int taskId, TaskItemStatus oldStatus, User actor, CancellationToken ct)
    {
        // Reload so CreatedBy / AssignedTo reflect the saved state.
        var task = await LoadTaskAsync(taskId, ct);
        await _notifications.NotifyTaskStatusChangedAsync(task, oldStatus, actor, ct);
    }

    private async Task<TaskItem> LoadTaskAsync(int id, CancellationToken ct)
        => await _uow.Tasks.GetWithDetailsAsync(id, ct) ?? throw new NotFoundException("Task", id);

    private async Task<TaskDto> ToDtoAsync(int id, CancellationToken ct)
        => await _uow.Tasks.GetDtoAsync(id, ct) ?? throw new NotFoundException("Task", id);

    private async Task<User> GetActorAsync(CancellationToken ct)
        => await _uow.Users.GetByIdAsync(_current.UserId, ct) ?? throw new UnauthorizedException();
}
