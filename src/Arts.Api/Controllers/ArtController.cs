using Arts.Application.DTOs.Requests;
using Arts.Application.DTOs.Requests.Arts;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Arts;
using Arts.Application.Services.Arts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arts.Api.Controllers;

[ApiController]
[Route("arts")]
public class ArtController(IArtService artService) : ControllerBase
{
    [HttpGet("with-author")]
    public async Task<ActionResult<ArtResponse>> GetArtAndAuthorById([FromQuery] Guid id, CancellationToken ct)
    {
        await artService.ViewArtAsync(id, ct);
        var response = await artService.GetArtAndAuthorByIdAsync(id, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("like")]
    public async Task<ActionResult<bool>> LikeArt([FromQuery] Guid id, CancellationToken ct)
    {
        var like = await artService.LikeArtAsync(id, ct);
        return Ok(like);
    }

    [HttpGet("like")]
    public async Task<ActionResult<bool>> IsLikeArt([FromQuery] Guid? id, CancellationToken ct)
    {
        var result = await artService.IsLikeArtAsync(id, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ArtResponse>> CreateArt([FromForm] ArtCreateRequest request, CancellationToken ct)
    {
        var art = await artService.CreateAsync(request, ct);
        return Ok(art);
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> DeletedArt([FromQuery] Guid id, CancellationToken ct)
    {
        await artService.DeleteAsync(id, ct);
        return Ok();
    }

    [Authorize]
    [HttpPut]
    public async Task<ActionResult<ArtResponse>> UpdateArt([FromForm] ArtUpdateRequest request, CancellationToken ct)
    {
        var response = await artService.UpdateAsync(request, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("likes")]
    public async Task<ActionResult<PagedResponse<ArtCardResponse>>> GetLikedArts([FromQuery] PaginationRequest pagination = default!, CancellationToken ct = default)
    {
        var response = await artService.GetLikedArtsByCurrentUserAsync(pagination.Page, pagination.Size, ct);
        return Ok(response);
    }

    [HttpGet("feed")]
    public async Task<ActionResult<PagedResponse<ArtCardResponse>>> GetFeed([FromQuery] string type = "latest",
        [FromQuery] PaginationRequest pagination = default!, CancellationToken ct = default)
    {
        var response = await artService.GetFeedAsync(type, pagination.Page, pagination.Size, ct);
        return Ok(response);
    }


    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<ArtCardResponse>>> SearchArts([FromQuery] string query = "",
        [FromQuery] PaginationRequest pagination = default!, CancellationToken ct = default)
    {
        var response = await artService.SearchArtsByTagNameAndNameAsync(query, pagination.Page, pagination.Size, ct);
        return Ok(response);
    }
}