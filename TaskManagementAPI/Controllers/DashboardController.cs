using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Services.Interfaces;

namespace TaskManagementAPI.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>
    /// Status totals, per-user status overview, priority breakdown and upcoming deadlines for the tasks
    /// visible to the caller. Accepts the same filters as GET /api/tasks (status, priority, deadline, ...).
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> Summary([FromQuery] TaskFilterParams filter, CancellationToken ct)
        => Ok(await _dashboard.GetSummaryAsync(filter, ct));
}
