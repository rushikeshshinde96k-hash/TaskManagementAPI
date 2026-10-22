using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    private IUserRepository? _users;
    private ITeamRepository? _teams;
    private ITaskRepository? _tasks;
    private ICommentRepository? _comments;
    private INotificationRepository? _notifications;
    private IRefreshTokenRepository? _refreshTokens;

    public UnitOfWork(AppDbContext context) => _context = context;

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ITeamRepository Teams => _teams ??= new TeamRepository(_context);
    public ITaskRepository Tasks => _tasks ??= new TaskRepository(_context);
    public ICommentRepository Comments => _comments ??= new CommentRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
