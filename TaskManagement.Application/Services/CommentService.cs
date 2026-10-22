using TaskManagement.Application.Common;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Application.Mappings;
using TaskManagement.Application.Services.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public sealed class CommentService : ICommentService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;
    private readonly INotificationService _notifications;

    public CommentService(IUnitOfWork uow, ICurrentUserService current, INotificationService notifications)
    {
        _uow = uow;
        _current = current;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<CommentDto>> GetByTaskAsync(int taskId, CancellationToken ct = default)
    {
        await LoadVisibleTaskAsync(taskId, ct);
        var comments = await _uow.Comments.GetByTaskAsync(taskId, ct);
        return comments.Select(c => c.ToDto()).ToList();
    }

    public async Task<CommentDto> AddAsync(int taskId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var task = await LoadVisibleTaskAsync(taskId, ct);
        var author = await _uow.Users.GetByIdAsync(_current.UserId, ct) ?? throw new UnauthorizedException();

        var comment = new TaskComment { TaskId = taskId, UserId = author.Id, Content = request.Content.Trim(), User = author };
        await _uow.Comments.AddAsync(comment, ct);
        await _uow.SaveChangesAsync(ct);

        await _notifications.NotifyCommentAddedAsync(task, comment, author, ct);
        return comment.ToDto();
    }

    public async Task<CommentDto> UpdateAsync(int taskId, int commentId, UpdateCommentRequest request, CancellationToken ct = default)
    {
        var comment = await LoadCommentAsync(taskId, commentId, ct);
        if (comment.UserId != _current.UserId)
            throw new ForbiddenException("You can only edit your own comments.");

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        _uow.Comments.Update(comment);
        await _uow.SaveChangesAsync(ct);
        return comment.ToDto();
    }

    public async Task DeleteAsync(int taskId, int commentId, CancellationToken ct = default)
    {
        var comment = await LoadCommentAsync(taskId, commentId, ct);
        if (comment.UserId != _current.UserId && _current.Role != UserRole.Admin)
            throw new ForbiddenException("You can only delete your own comments.");

        _uow.Comments.Remove(comment);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<TaskItem> LoadVisibleTaskAsync(int taskId, CancellationToken ct)
    {
        var task = await _uow.Tasks.GetWithDetailsAsync(taskId, ct) ?? throw new NotFoundException("Task", taskId);
        if (!TaskAccessPolicy.CanView(task, _current.Scope))
            throw new ForbiddenException("You do not have access to this task.");
        return task;
    }

    private async Task<TaskComment> LoadCommentAsync(int taskId, int commentId, CancellationToken ct)
    {
        await LoadVisibleTaskAsync(taskId, ct);
        var comment = await _uow.Comments.GetWithUserAsync(commentId, ct);
        if (comment is null || comment.TaskId != taskId) throw new NotFoundException("Comment", commentId);
        return comment;
    }
}
