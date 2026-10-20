using System.ComponentModel.DataAnnotations;

namespace Arts.Application.DTOs.Requests;

public record PaginationRequest(
    [Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть >= 1")]
    int Page = 1,

    [Range(1, 50, ErrorMessage = "Размер страницы должен быть от 1 до 50")]
    int Size = 12
);