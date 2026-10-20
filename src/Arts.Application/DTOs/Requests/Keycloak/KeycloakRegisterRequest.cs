using System.ComponentModel.DataAnnotations;

namespace Arts.Application.DTOs.Requests.Keycloak;

public record KeycloakRegisterRequest(
    [Required(ErrorMessage = "Логин обязателен")]
    [Length(2, 60, ErrorMessage = "Логин должен быть от 2 до 60 символов")]
    string Username,

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email адреса")]
    string Email,

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
    string Password
);