using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public int? AssignedToId { get; set; }
    public User? AssignedTo { get; set; }

    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();

    /// <summary>Due dates are calendar dates: a task becomes overdue the day after its due date.</summary>
    public bool IsOverdue => Status != TaskItemStatus.Done && DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date;
}
