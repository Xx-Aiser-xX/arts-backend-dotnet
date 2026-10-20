namespace Arts.Application.DTOs.Responses.Keycloak;

public record KeycloakUserResponse(
    string Id, 
    string Username, 
    string Email
);