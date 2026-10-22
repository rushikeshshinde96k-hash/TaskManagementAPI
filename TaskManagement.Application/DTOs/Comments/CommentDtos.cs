using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.DTOs.Comments;

public sealed class CommentDto
{
    public int Id { get; init; }
    public int TaskId { get; init; }
    public string Content { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public UserRole UserRole { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CreateCommentRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}

public sealed class UpdateCommentRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Content { get; init; } = string.Empty;
}
