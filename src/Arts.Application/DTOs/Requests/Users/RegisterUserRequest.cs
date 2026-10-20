using System.ComponentModel.DataAnnotations;

namespace Arts.Application.DTOs.Requests.Users;

public record RegisterUserRequest(
    [Required] 
    Guid Id,

    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [Length(2, 60, ErrorMessage = "Имя пользователя должно быть от 2 до 60 символов")]
    string UserName
);