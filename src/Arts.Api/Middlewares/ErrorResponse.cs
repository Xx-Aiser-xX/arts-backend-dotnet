namespace Arts.Api.Middlewares;

public record ErrorResponse(
    int Status,
    string Error,
    string Message,
    long Timestamp
);