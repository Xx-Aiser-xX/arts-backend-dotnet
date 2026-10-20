using Arts.Application.DTOs.Responses.Users;

namespace Arts.Application.DTOs.Responses.Arts;

public record ArtResponse(
    Guid Id,
    string Name,
    string? Description,
    string ImageUrl,
    int CountLikes,
    int CountViews,
    bool IsNsfw,
    DateTime PublicationTime,
    UserMinResponse Author
);