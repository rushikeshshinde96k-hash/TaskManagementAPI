using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Users;
using TaskManagement.Application.Services.Interfaces;
using TaskManagementAPI.Authorization;

namespace TaskManagementAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>List users. Managers use this to pick people to add to their teams.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.AdminOrManager)]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll([FromQuery] UserQueryParams query, CancellationToken ct)
        => Ok(await _users.GetAllAsync(query, ct));

    /// <summary>Users the caller may assign a task to (optionally within one team).</summary>
    [HttpGet("assignable")]
    [Authorize(Roles = Roles.AdminOrManager)]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAssignable([FromQuery] int? teamId, CancellationToken ct)
        => Ok(await _users.GetAssignableAsync(teamId, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken ct)
        => Ok(await _users.GetByIdAsync(id, ct));

    /// <summary>Admin creates an account with any role.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await _users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}/role")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> UpdateRole(int id, UpdateUserRoleRequest request, CancellationToken ct)
        => Ok(await _users.UpdateRoleAsync(id, request, ct));

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> UpdateStatus(int id, UpdateUserStatusRequest request, CancellationToken ct)
        => Ok(await _users.UpdateStatusAsync(id, request, ct));

    [HttpPut("me/profile")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> UpdateProfile(UpdateProfileRequest request, CancellationToken ct)
        => Ok(await _users.UpdateProfileAsync(request, ct));

    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _users.ChangePasswordAsync(request, ct);
        return NoContent();
    }
}
