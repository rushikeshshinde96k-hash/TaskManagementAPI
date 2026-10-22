using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class CommentRepository : GenericRepository<TaskComment>, ICommentRepository
{
    public CommentRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<TaskComment>> GetByTaskAsync(int taskId, CancellationToken ct = default)
        => await Set.AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<TaskComment?> GetWithUserAsync(int id, CancellationToken ct = default)
        => Set.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id, ct);
}

public sealed class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool unreadOnly, int take, CancellationToken ct = default)
    {
        var q = Set.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly) q = q.Where(n => !n.IsRead);
        return await q.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).Take(take).ToListAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default)
        => Set.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<int> MarkAllAsReadAsync(int userId, CancellationToken ct = default)
    {
        var unread = await Set.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        unread.ForEach(n => n.IsRead = true);
        return unread.Count;
    }
}

public sealed class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context) { }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        => Set.Include(r => r.User).FirstOrDefaultAsync(r => r.Token == tokenHash, ct);

    public async Task RevokeAllForUserAsync(int userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var active = await Set.Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > now).ToListAsync(ct);
        active.ForEach(r => r.RevokedAt = now);
    }
}
