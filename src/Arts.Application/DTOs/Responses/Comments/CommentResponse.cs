namespace Arts.Application.DTOs.Responses.Comments;

public record CommentResponse(
    Guid Id,
    Guid ArtId,
    Guid AuthorId,
    string AuthorUserName,
    string? AuthorPhotoUrl,
    string Text,
    DateTime PublicationTime
);