using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces.Repositories;

public interface ITeamRepository : IGenericRepository<Team>
{
    /// <summary>Admin: all teams. Manager: teams they manage or belong to. User: teams they belong to.</summary>
    Task<IReadOnlyList<TeamDto>> GetVisibleTeamsAsync(AccessScope scope, CancellationToken ct = default);
    /// <summary>Loads a team with Manager and Members (with their User).</summary>
    Task<Team?> GetWithMembersAsync(int id, CancellationToken ct = default);
    Task<int> CountTasksAsync(int teamId, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetManagedTeamIdsAsync(int managerId, CancellationToken ct = default);
    Task<bool> IsMemberAsync(int teamId, int userId, CancellationToken ct = default);

    /// <summary>True when <paramref name="userId"/> is a member of any team managed by <paramref name="managerId"/>.</summary>
    Task<bool> IsUserInManagedTeamsAsync(int managerId, int userId, CancellationToken ct = default);

    Task<TeamMember?> GetMemberAsync(int teamId, int userId, CancellationToken ct = default);
    Task AddMemberAsync(TeamMember member, CancellationToken ct = default);
    void RemoveMember(TeamMember member);
}
