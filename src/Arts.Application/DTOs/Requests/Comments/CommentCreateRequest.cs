using System.ComponentModel.DataAnnotations;

namespace Arts.Application.DTOs.Requests.Comments;

public record CommentCreateRequest(
    [Required] 
    Guid ArtId,

    [Required(ErrorMessage = "Текст комментария обязателен")]
    [Length(1, 200, ErrorMessage = "Длина комментария от 1 до 200 символов")]
    string Text
);