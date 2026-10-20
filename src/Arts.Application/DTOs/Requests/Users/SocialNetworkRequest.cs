using System.ComponentModel.DataAnnotations;

namespace Arts.Application.DTOs.Requests.Users;

public record SocialNetworkRequest(
    [Required(ErrorMessage = "Ссылка обязательна")]
    [Url(ErrorMessage = "Некорректный формат URL")]
    [MaxLength(2000, ErrorMessage = "Ссылка не может превышать 2000 символов")]
    string Link
);