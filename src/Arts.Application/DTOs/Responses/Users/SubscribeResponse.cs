namespace Arts.Application.DTOs.Responses.Users;

public record SubscribeResponse(
    Guid SubscriberId,
    Guid TargetId,
    bool IsDeleted
);