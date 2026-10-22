using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class TeamRepository : GenericRepository<Team>, ITeamRepository
{
    public TeamRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<TeamDto>> GetVisibleTeamsAsync(AccessScope scope, CancellationToken ct = default)
    {
        var q = Set.AsNoTracking();
        var uid = scope.UserId;

        q = scope.Role switch
        {
            UserRole.Admin => q,
            UserRole.Manager => q.Where(t => t.ManagerId == uid || t.Members.Any(m => m.UserId == uid)),
            _ => q.Where(t => t.Members.Any(m => m.UserId == uid))
        };

        return await q
            .OrderBy(t => t.Name)
            .Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                ManagerId = t.ManagerId,
                ManagerName = t.Manager != null ? t.Manager.FullName : null,
                MemberCount = t.Members.Count,
                TaskCount = t.Tasks.Count,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);
    }

    public Task<Team?> GetWithMembersAsync(int id, CancellationToken ct = default)
        => Set.Include(t => t.Manager)
              .Include(t => t.Members).ThenInclude(m => m.User)
              .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<int> CountTasksAsync(int teamId, CancellationToken ct = default)
        => Context.Tasks.CountAsync(t => t.TeamId == teamId, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default)
        => Set.AnyAsync(t => t.Name == name && (excludeId == null || t.Id != excludeId), ct);

    public async Task<IReadOnlyList<int>> GetManagedTeamIdsAsync(int managerId, CancellationToken ct = default)
        => await Set.Where(t => t.ManagerId == managerId).Select(t => t.Id).ToListAsync(ct);

    public Task<bool> IsMemberAsync(int teamId, int userId, CancellationToken ct = default)
        => Context.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == userId, ct);

    public Task<bool> IsUserInManagedTeamsAsync(int managerId, int userId, CancellationToken ct = default)
        => Context.TeamMembers.AnyAsync(m => m.UserId == userId && m.Team.ManagerId == managerId, ct);

    public Task<TeamMember?> GetMemberAsync(int teamId, int userId, CancellationToken ct = default)
        => Context.TeamMembers.FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId, ct);

    public async Task AddMemberAsync(TeamMember member, CancellationToken ct = default)
        => await Context.TeamMembers.AddAsync(member, ct);

    public void RemoveMember(TeamMember member) => Context.TeamMembers.Remove(member);
}
