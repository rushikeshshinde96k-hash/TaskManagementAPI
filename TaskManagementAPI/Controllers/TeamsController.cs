using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Services.Interfaces;
using TaskManagementAPI.Authorization;

namespace TaskManagementAPI.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize]
[Produces("application/json")]
public sealed class TeamsController : ControllerBase
{
    private readonly ITeamService _teams;

    public TeamsController(ITeamService teams) => _teams = teams;

    /// <summary>Admin: all teams. Manager: managed + member teams. User: member teams.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TeamDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetAll(CancellationToken ct)
        => Ok(await _teams.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<TeamDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamDetailDto>> GetById(int id, CancellationToken ct)
        => Ok(await _teams.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<TeamDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamDetailDto>> Create(CreateTeamRequest request, CancellationToken ct)
    {
        var team = await _teams.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<TeamDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDetailDto>> Update(int id, UpdateTeamRequest request, CancellationToken ct)
        => Ok(await _teams.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _teams.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Admin, or the team's manager, adds users to the team.</summary>
    [HttpPost("{id:int}/members")]
    [Authorize(Roles = Roles.AdminOrManager)]
    [ProducesResponseType<TeamDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDetailDto>> AddMembers(int id, AddTeamMembersRequest request, CancellationToken ct)
        => Ok(await _teams.AddMembersAsync(id, request, ct));

    [HttpDelete("{id:int}/members/{userId:int}")]
    [Authorize(Roles = Roles.AdminOrManager)]
    [ProducesResponseType<TeamDetailDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamDetailDto>> RemoveMember(int id, int userId, CancellationToken ct)
        => Ok(await _teams.RemoveMemberAsync(id, userId, ct));
}
