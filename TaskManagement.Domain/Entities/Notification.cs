using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class Notification : BaseEntity
{
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public bool EmailSent { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? TaskId { get; set; }
    public TaskItem? Task { get; set; }
}
