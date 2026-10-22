namespace TaskManagement.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Manager = 2,
    User = 3
}

public enum TaskItemStatus
{
    ToDo = 1,
    InProgress = 2,
    Done = 3
}

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum NotificationType
{
    TaskAssigned = 1,
    TaskStatusChanged = 2,
    TaskCommented = 3,
    TeamMembership = 4
}
