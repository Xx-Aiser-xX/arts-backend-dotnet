using Arts.Application.DTOs.Requests;
using Arts.Application.DTOs.Requests.Comments;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Comments;
using Arts.Application.Services.Comments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arts.Api.Controllers;

[ApiController]
[Route("comments")]
public class CommentController(ICommentService commentService) : ControllerBase
{

    [HttpGet("art")]
    public async Task<ActionResult<PagedResponse<CommentResponse>>> GetArtComments([FromQuery] Guid id, [FromQuery] PaginationRequest pagination = default!, CancellationToken ct = default)
    {
        var response = await commentService.GetCommentsByArtIdAsync(id, pagination.Page, pagination.Size, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("art")]
    public async Task<ActionResult<CommentResponse>> CreateComment([FromBody] CommentCreateRequest request, CancellationToken ct)
    {
        var response = await commentService.CreateAsync(request, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpPut("art")]
    public async Task<ActionResult<CommentResponse>> UpdateComment([FromBody] CommentUpdateRequest request,
        CancellationToken ct)
    {
        var response = await commentService.UpdateAsync(request, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> Deleted([FromQuery] Guid id, CancellationToken ct)
    {
        await commentService.DeleteAsync(id,ct);
        return Ok();
    }
}