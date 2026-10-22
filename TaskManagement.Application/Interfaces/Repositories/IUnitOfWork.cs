namespace TaskManagement.Application.Interfaces.Repositories;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ITeamRepository Teams { get; }
    ITaskRepository Tasks { get; }
    ICommentRepository Comments { get; }
    INotificationRepository Notifications { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
