using Arts.Application.DTOs.Requests.Keycloak;
using Arts.Application.DTOs.Responses.Keycloak;
using Arts.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Arts.Api.Controllers;

[ApiController]
[Route("keycloak")]
public class KeycloakController(IKeycloakService keycloakService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<KeycloakUserResponse>> Register([FromBody] KeycloakRegisterRequest request, CancellationToken ct)
    {
        var user = await keycloakService.RegisterUserAsync(request, ct);
        return Ok(user);
    }
}