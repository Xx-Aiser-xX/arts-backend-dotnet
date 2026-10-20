namespace Arts.Application.DTOs.Responses.Users;

public record UserResponse(
    Guid Id,
    string UserName,
    string? Description,
    string? PhotoUrl,
    int CountJobs,
    int CountSubscriptions,
    int CountSubscribers
);