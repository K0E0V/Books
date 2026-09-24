using Microsoft.AspNetCore.Diagnostics;

namespace Books.Infrastructure;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Необработанное исключение: {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        // Для JSON-запросов отдаём ProblemDetails-подобный ответ
        if (httpContext.Request.Headers.Accept.ToString().Contains("application/json"))
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(
                new { error = "Внутренняя ошибка сервера" },
                cancellationToken);
            return true;
        }

        // Для обычных страниц Razor Pages уводим на страницу ошибки
        httpContext.Response.Redirect("/Error");
        return true;
    }
}