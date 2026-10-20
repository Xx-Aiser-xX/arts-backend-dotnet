using Arts.Application.DTOs.Responses.Tags;
using Arts.Application.Services.Tags;
using Microsoft.AspNetCore.Mvc;

namespace Arts.Api.Controllers;

[ApiController]
[Route("tags")]
public class TagController(ITagService tagService) : ControllerBase
{
    [HttpGet("art")]
    public async Task<ActionResult<List<TagResponse>>> GetTagsByArtId([FromQuery] Guid id, CancellationToken ct)
    {
        var response = await tagService.GetTagsByArtIdAsync(id, ct);
        return Ok(response);
    }
}