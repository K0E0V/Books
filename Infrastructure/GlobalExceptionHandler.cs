using System.Text;
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

        // Передаём текст ошибки на страницу /Error через TempData.
        // ITempDataDictionary нельзя получить через RequestServices напрямую
        // (он не зарегистрирован в DI) — создаём экземпляр вручную,
        // провайдер берётся из DI (регистрация AddRazorPages гарантирует его наличие).
        try
        {
            var provider = httpContext.RequestServices.GetRequiredService<ITempDataProvider>();
            var tempData = new TempDataDictionary(httpContext, provider);
            tempData["ErrorMessage"] = BuildErrorMessage(exception);
            tempData["ErrorRequestId"] = httpContext.TraceIdentifier;
            tempData["ErrorPath"] = $"{httpContext.Request.Method} {httpContext.Request.Path}{httpContext.Request.QueryString}";
            tempData.Save(); // записывает куку TempData — она доживёт до редиректа
        }
        catch (Exception tempDataEx)
        {
            _logger.LogWarning(tempDataEx, "Не удалось сохранить сообщение об ошибке в TempData.");
        }

        // Для обычных страниц Razor Pages уводим на страницу ошибки
        httpContext.Response.Redirect("/Error");
        return true;
    }

    private static string BuildErrorMessage(Exception exception)
    {
        var sb = new StringBuilder();
        sb.Append($"{exception.GetType().Name}: {exception.Message}");
        var inner = exception.InnerException;
        var depth = 0;
        while (inner is not null && depth < 3)
        {
            sb.Append($" | Внутренняя: {inner.GetType().Name}: {inner.Message}");
            inner = inner.InnerException;
            depth++;
        }
        return sb.ToString();
    }
}