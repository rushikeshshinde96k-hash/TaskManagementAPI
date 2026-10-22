using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        => Set.AnyAsync(u => u.Email == email, ct);

    public Task<User?> GetWithTeamsAsync(int id, CancellationToken ct = default)
        => Set.Include(u => u.TeamMemberships).ThenInclude(m => m.Team)
              .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IReadOnlyList<User>> SearchAsync(UserQueryParams query, CancellationToken ct = default)
    {
        var q = Set.AsNoTracking()
            .Include(u => u.TeamMemberships).ThenInclude(m => m.Team)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(u => u.FullName.Contains(term) || u.Email.Contains(term));
        }
        if (query.Role.HasValue) q = q.Where(u => u.Role == query.Role.Value);
        if (query.IsActive.HasValue) q = q.Where(u => u.IsActive == query.IsActive.Value);

        return await q.OrderBy(u => u.FullName).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var list = ids.ToList();
        return await Set.Where(u => list.Contains(u.Id)).ToListAsync(ct);
    }
}
