namespace Arts.Application.DTOs.Responses.Users;

public record UserMinResponse(
    Guid Id,
    string UserName,
    string? PhotoUrl
);