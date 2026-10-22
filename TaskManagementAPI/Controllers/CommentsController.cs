using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.Services.Interfaces;

namespace TaskManagementAPI.Controllers;

[ApiController]
[Route("api/tasks/{taskId:int}/comments")]
[Authorize]
[Produces("application/json")]
public sealed class CommentsController : ControllerBase
{
    private readonly ICommentService _comments;

    public CommentsController(ICommentService comments) => _comments = comments;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CommentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetAll(int taskId, CancellationToken ct)
        => Ok(await _comments.GetByTaskAsync(taskId, ct));

    [HttpPost]
    [ProducesResponseType<CommentDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CommentDto>> Create(int taskId, CreateCommentRequest request, CancellationToken ct)
    {
        var comment = await _comments.AddAsync(taskId, request, ct);
        return CreatedAtAction(nameof(GetAll), new { taskId }, comment);
    }

    [HttpPut("{commentId:int}")]
    [ProducesResponseType<CommentDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentDto>> Update(int taskId, int commentId, UpdateCommentRequest request, CancellationToken ct)
        => Ok(await _comments.UpdateAsync(taskId, commentId, request, ct));

    [HttpDelete("{commentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int taskId, int commentId, CancellationToken ct)
    {
        await _comments.DeleteAsync(taskId, commentId, ct);
        return NoContent();
    }
}
