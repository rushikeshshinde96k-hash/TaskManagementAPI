using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Notifications;

public sealed class NotificationDto
{
    public int Id { get; init; }
    public NotificationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public bool EmailSent { get; init; }
    public int? TaskId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record UnreadCountDto(int Count);
