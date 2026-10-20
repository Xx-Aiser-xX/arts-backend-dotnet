using Arts.Application.DTOs.Requests.Keycloak;
using Arts.Application.DTOs.Responses.Keycloak;

namespace Arts.Application.Interfaces;

public interface IKeycloakService
{
    Task<KeycloakUserResponse> RegisterUserAsync(KeycloakRegisterRequest request, CancellationToken ct = default);
}