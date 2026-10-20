namespace Arts.Application.DTOs.Responses.Arts;

public record ArtCardResponse(
    Guid Id,
    string Name,
    string ImageUrl,
    int CountLikes,
    int CountViews,
    DateTime PublicationTime
);