using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

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

        // Сохраняем текст ошибки в TempData, чтобы страница /Error показала
        // его пользователю без похода в лог.
        try
        {
            var tempData = httpContext.RequestServices.GetRequiredService<ITempDataDictionary>();
            tempData["ErrorMessage"] = $"{exception.GetType().Name}: {exception.Message}";
            if (exception.InnerException is not null)
                tempData["ErrorMessage"] += $" | Внутренняя: {exception.InnerException.Message}";
            tempData["ErrorRequestId"] = httpContext.TraceIdentifier;
            tempData["ErrorPath"] = $"{httpContext.Request.Method} {httpContext.Request.Path}{httpContext.Request.QueryString}";
            tempData.Load();      // обязательно: иначе изменения не сохранятся при редиректе
            tempData.Save();
        }
        catch (Exception tempDataEx)
        {
            _logger.LogWarning(tempDataEx, "Не удалось сохранить сообщение об ошибке в TempData.");
        }

        // Для обычных страниц Razor Pages уводим на страницу ошибки
        httpContext.Response.Redirect("/Error");
        return true;
    }
}