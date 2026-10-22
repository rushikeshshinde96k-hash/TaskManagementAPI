using Microsoft.Extensions.Logging;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Application.Mappings;
using TaskManagement.Application.Services.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

/// <summary>
/// Stores an in-app notification for each recipient and sends an email through <see cref="IEmailSender"/>
/// (SMTP when configured, otherwise a logging mock). Email failures never fail the triggering request.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;
    private readonly IEmailSender _email;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUnitOfWork uow, ICurrentUserService current, IEmailSender email, ILogger<NotificationService> logger)
    {
        _uow = uow;
        _current = current;
        _email = email;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(bool unreadOnly, int take, CancellationToken ct = default)
    {
        var items = await _uow.Notifications.GetForUserAsync(_current.UserId, unreadOnly, Math.Clamp(take, 1, 100), ct);
        return items.Select(n => n.ToDto()).ToList();
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken ct = default)
        => new(await _uow.Notifications.GetUnreadCountAsync(_current.UserId, ct));

    public async Task MarkAsReadAsync(int id, CancellationToken ct = default)
    {
        var n = await _uow.Notifications.GetByIdAsync(id, ct);
        if (n is null || n.UserId != _current.UserId) throw new NotFoundException(nameof(Notification), id);
        if (n.IsRead) return;

        n.IsRead = true;
        _uow.Notifications.Update(n);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(CancellationToken ct = default)
    {
        if (await _uow.Notifications.MarkAllAsReadAsync(_current.UserId, ct) > 0)
            await _uow.SaveChangesAsync(ct);
    }

    public Task NotifyTaskAssignedAsync(TaskItem task, User assignee, User actor, CancellationToken ct = default)
    {
        if (assignee.Id == actor.Id) return Task.CompletedTask;

        var due = task.DueDate.HasValue ? $" Due {task.DueDate:dd MMM yyyy}." : string.Empty;
        return SendAsync(new[] { assignee }, NotificationType.TaskAssigned, task.Id,
            "New task assigned",
            $"{actor.FullName} assigned you \"{task.Title}\" (Priority: {task.Priority}).{due}", ct);
    }

    public Task NotifyTaskStatusChangedAsync(TaskItem task, TaskItemStatus oldStatus, User actor, CancellationToken ct = default)
    {
        var recipients = new[] { task.AssignedTo, task.CreatedBy, task.Team?.Manager };
        return SendAsync(Except(recipients, actor), NotificationType.TaskStatusChanged, task.Id,
            "Task status updated",
            $"{actor.FullName} moved \"{task.Title}\" from {oldStatus.ToDisplay()} to {task.Status.ToDisplay()}.", ct);
    }

    public Task NotifyCommentAddedAsync(TaskItem task, TaskComment comment, User author, CancellationToken ct = default)
    {
        var preview = comment.Content.Length > 120 ? comment.Content[..120] + "…" : comment.Content;
        return SendAsync(Except(new[] { task.AssignedTo, task.CreatedBy }, author), NotificationType.TaskCommented, task.Id,
            "New comment",
            $"{author.FullName} commented on \"{task.Title}\": {preview}", ct);
    }

    public Task NotifyAddedToTeamAsync(Team team, User member, User actor, CancellationToken ct = default)
        => SendAsync(new[] { member }, NotificationType.TeamMembership, null,
            "Added to team",
            $"{actor.FullName} added you to the team \"{team.Name}\".", ct);

    private static IEnumerable<User> Except(IEnumerable<User?> users, User actor) =>
        users.OfType<User>().Where(u => u.Id != actor.Id).DistinctBy(u => u.Id);

    private async Task SendAsync(IEnumerable<User> recipients, NotificationType type, int? taskId, string title, string message, CancellationToken ct)
    {
        var created = new List<(Notification Notification, User User)>();
        foreach (var user in recipients)
        {
            var n = new Notification { UserId = user.Id, TaskId = taskId, Type = type, Title = title, Message = message };
            await _uow.Notifications.AddAsync(n, ct);
            created.Add((n, user));
        }
        if (created.Count == 0) return;

        foreach (var (n, user) in created)
        {
            try
            {
                n.EmailSent = await _email.SendAsync(user.Email, user.FullName, $"[Task Manager] {title}", message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send '{Type}' email to {Email}", type, user.Email);
            }
        }

        await _uow.SaveChangesAsync(ct);
    }
}
