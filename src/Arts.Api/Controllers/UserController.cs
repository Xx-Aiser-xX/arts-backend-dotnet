using System.ComponentModel.DataAnnotations;
using Arts.Application.DTOs.Requests;
using Arts.Application.DTOs.Requests.Users;
using Arts.Application.DTOs.Responses;
using Arts.Application.DTOs.Responses.Arts;
using Arts.Application.DTOs.Responses.SocialNetworks;
using Arts.Application.DTOs.Responses.Users;
using Arts.Application.Services.Users;
using Arts.Application.Services.Arts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arts.Api.Controllers;

[ApiController]
[Route("users")]
public class UserController(IUserService userService, IArtService artService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        var response = await userService.RegisterUserAsync(request, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserResponse>> UpdateProfile([FromForm] UserUpdateRequest request, CancellationToken ct)
    {
        var response = await userService.UpdateUserAsync(request, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken ct)
    {
        var response = await userService.GetCurrentUserDtoAsync(ct);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me/min")]
    public async Task<ActionResult<UserMinResponse>> GetCurrentMinUser(CancellationToken ct)
    {
        var response = await userService.GetCurrentUserMinDtoAsync(ct);
        return Ok(response);
    }

    [HttpGet("social-networks")]
    public async Task<ActionResult<List<SocialNetworkResponse>>> GetSocialNetworks([FromQuery] Guid id, CancellationToken ct)
    {
        var response = await userService.GetSocialNetworkUserAsync(id, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("subscribe")]
    public async Task<ActionResult<SubscribeResponse>> Subscribe([FromQuery] Guid id, CancellationToken ct)
    {
        var response = await userService.SubscribeAsync(id, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("subscribe")]
    public async Task<ActionResult<bool>> IsSubscribe([FromQuery] Guid id, CancellationToken ct)
    {
        var response = await userService.IsSubscribeAsync(id, ct);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<UserResponse>> GetUser([FromQuery] Guid id, CancellationToken ct)
    {
        var response = await userService.GetUserAsync(id, ct);
        return Ok(response);
    }

    [HttpGet("arts")]
    public async Task<ActionResult<PagedResponse<ArtCardResponse>>> GetArtByAuthor([FromQuery] Guid id, [FromQuery] PaginationRequest pagination = default!, CancellationToken ct = default)
    {
        var response = await artService.GetArtByAuthorIdAsync(id, pagination.Page, pagination.Size, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("subs-with-arts")]
    public async Task<ActionResult<PagedResponse<UserMinResponse>>> GetSubsWithArts([FromQuery] PaginationRequest pagination = default!, CancellationToken ct = default)
    {
        var response = await userService.GetSubscriptionsWithArtsAsync(pagination.Page, pagination.Size, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("get-recent-sub")]
    public async Task<ActionResult<List<UserMinResponse>>> GetRecentSubscriptions([FromQuery, Range(1, 20, ErrorMessage = "Лимит должен быть от 1 до 20")] int limit = 4, CancellationToken ct = default)
    {
        var response = await userService.GetRecentSubscriptionsAsync(limit, ct);
        return Ok(response);
    }
}