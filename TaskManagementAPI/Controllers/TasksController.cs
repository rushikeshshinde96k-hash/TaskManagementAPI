using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Services.Interfaces;
using TaskManagementAPI.Authorization;

namespace TaskManagementAPI.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks) => _tasks = tasks;

    /// <summary>
    /// Tasks visible to the caller, filtered by status, priority, deadline (preset or range), team, assignee and text.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<TaskDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TaskDto>>> GetAll([FromQuery] TaskFilterParams filter, CancellationToken ct)
        => Ok(await _tasks.GetAllAsync(filter, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(int id, CancellationToken ct)
        => Ok(await _tasks.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = Roles.AdminOrManager)]
    [ProducesResponseType<TaskDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request, CancellationToken ct)
    {
        var task = await _tasks.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.AdminOrManager)]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskDto>> Update(int id, UpdateTaskRequest request, CancellationToken ct)
        => Ok(await _tasks.UpdateAsync(id, request, ct));

    /// <summary>Assignee, the task's manager or an admin moves a task between To Do / In Progress / Done.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskDto>> UpdateStatus(int id, UpdateTaskStatusRequest request, CancellationToken ct)
        => Ok(await _tasks.UpdateStatusAsync(id, request, ct));

    [HttpPatch("{id:int}/assign")]
    [Authorize(Roles = Roles.AdminOrManager)]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskDto>> Assign(int id, AssignTaskRequest request, CancellationToken ct)
        => Ok(await _tasks.AssignAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdminOrManager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _tasks.DeleteAsync(id, ct);
        return NoContent();
    }
}
