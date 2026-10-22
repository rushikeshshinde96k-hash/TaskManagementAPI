using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces.Repositories;

public interface ICommentRepository : IGenericRepository<TaskComment>
{
    Task<IReadOnlyList<TaskComment>> GetByTaskAsync(int taskId, CancellationToken ct = default);
    Task<TaskComment?> GetWithUserAsync(int id, CancellationToken ct = default);
}

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool unreadOnly, int take, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(int userId, CancellationToken ct = default);
}

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task RevokeAllForUserAsync(int userId, CancellationToken ct = default);
}
