using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Arts.Application.DTOs.Requests.Users;

public record UserUpdateRequest(
    [Required] 
    Guid Id,

    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [Length(2, 60, ErrorMessage = "Имя пользователя должно быть от 2 до 60 символов")]
    string UserName,

    [MaxLength(5000, ErrorMessage = "Описание слишком длинное")]
    string? Description,

    IFormFile? AvatarFile,

    // ЗАЩИТА: не больше 10 ссылок на соцсети
    [Length(0, 10, ErrorMessage = "Нельзя указать более 10 ссылок на соцсети")]
    List<SocialNetworkRequest>? SocialNetworks
);