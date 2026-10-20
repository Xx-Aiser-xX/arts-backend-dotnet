using Arts.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Arts.Api.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            logger.LogInformation("Клиент отменил HTTP-запрос (соединение закрыто)");
            return true;
        }

        var (statusCode, errorTitle) = exception switch
        {
            ArgumentException or InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            DbUpdateException => (StatusCodes.Status409Conflict, "Data Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        if (statusCode >= 500)
            logger.LogError(exception, "Непредвиденная ошибка сервера при обработке запроса {Path}", httpContext.Request.Path);
        else
            logger.LogWarning("Клиентская ошибка ({StatusCode} {Title}): {Message}. Путь: {Path}", 
                statusCode, errorTitle, exception.Message, httpContext.Request.Path);

        var response = new ErrorResponse(
            Status: statusCode,
            Error: errorTitle,
            Message: statusCode == 500 
                ? "Произошла непредвиденная ошибка на сервере. Пожалуйста, попробуйте позже." 
                : exception.Message,
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true; 
    }
}