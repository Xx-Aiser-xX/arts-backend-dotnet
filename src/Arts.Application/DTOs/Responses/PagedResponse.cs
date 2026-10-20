namespace Arts.Application.DTOs.Responses;

public record PagedResponse<T>(
    IEnumerable<T> Content, 
    long TotalElements, 
    int PageNumber,
    int PageSize
);