using System.ComponentModel.DataAnnotations;
using Arts.Application.DTOs.Requests.Tags;
using Microsoft.AspNetCore.Http;

namespace Arts.Application.DTOs.Requests.Arts;

public record ArtCreateRequest(
    [Required(ErrorMessage = "Название обязательно")]
    [Length(6, 200, ErrorMessage = "Название должно содержать от 6 до 200 символов")]
    string Name,

    [MaxLength(5000, ErrorMessage = "Описание не может превышать 5000 символов")]
    string? Description,

    [Required(ErrorMessage = "Файл изображения обязателен")]
    IFormFile ImageFile,

    bool IsNsfw,

    [Required]
    [Length(0, 15, ErrorMessage = "Нельзя привязать более 15 тегов к одному арту")]
    List<TagRequest> Tags
);