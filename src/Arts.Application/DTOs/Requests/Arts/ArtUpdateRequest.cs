using System.ComponentModel.DataAnnotations;

namespace Arts.Application.DTOs.Requests.Arts;

public record ArtUpdateRequest(
    [Required] 
    Guid Id,

    [Required(ErrorMessage = "Название обязательно")]
    [Length(6, 200, ErrorMessage = "Название должно содержать от 6 до 200 символов")]
    string Name,

    [MaxLength(5000, ErrorMessage = "Описание не может превышать 5000 символов")]
    string? Description,

    bool IsNsfw
);