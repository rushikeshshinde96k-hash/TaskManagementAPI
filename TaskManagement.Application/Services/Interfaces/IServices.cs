using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);
}

public interface IUserService
{
    Task<UserDto> GetCurrentAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> GetAllAsync(UserQueryParams query, CancellationToken ct = default);
    Task<UserDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> GetAssignableAsync(int? teamId, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateRoleAsync(int id, UpdateUserRoleRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateStatusAsync(int id, UpdateUserStatusRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}

public interface ITeamService
{
    Task<IReadOnlyList<TeamDto>> GetAllAsync(CancellationToken ct = default);
    Task<TeamDetailDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TeamDetailDto> CreateAsync(CreateTeamRequest request, CancellationToken ct = default);
    Task<TeamDetailDto> UpdateAsync(int id, UpdateTeamRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<TeamDetailDto> AddMembersAsync(int teamId, AddTeamMembersRequest request, CancellationToken ct = default);
    Task<TeamDetailDto> RemoveMemberAsync(int teamId, int userId, CancellationToken ct = default);
}

public interface ITaskService
{
    Task<PagedResult<TaskDto>> GetAllAsync(TaskFilterParams filter, CancellationToken ct = default);
    Task<TaskDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskDto> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskDto> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken ct = default);
    Task<TaskDto> AssignAsync(int id, AssignTaskRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface ICommentService
{
    Task<IReadOnlyList<CommentDto>> GetByTaskAsync(int taskId, CancellationToken ct = default);
    Task<CommentDto> AddAsync(int taskId, CreateCommentRequest request, CancellationToken ct = default);
    Task<CommentDto> UpdateAsync(int taskId, int commentId, UpdateCommentRequest request, CancellationToken ct = default);
    Task DeleteAsync(int taskId, int commentId, CancellationToken ct = default);
}

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetMineAsync(bool unreadOnly, int take, CancellationToken ct = default);
    Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken ct = default);
    Task MarkAsReadAsync(int id, CancellationToken ct = default);
    Task MarkAllAsReadAsync(CancellationToken ct = default);

    // Event triggers used by other services.
    Task NotifyTaskAssignedAsync(TaskItem task, User assignee, User actor, CancellationToken ct = default);
    Task NotifyTaskStatusChangedAsync(TaskItem task, TaskItemStatus oldStatus, User actor, CancellationToken ct = default);
    Task NotifyCommentAddedAsync(TaskItem task, TaskComment comment, User author, CancellationToken ct = default);
    Task NotifyAddedToTeamAsync(Team team, User member, User actor, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(TaskFilterParams filter, CancellationToken ct = default);
}
