using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Books.Pages;

public class ErrorModel : PageModel
{
    public string? RequestId { get; private set; }
    public int? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorPath { get; private set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet(int? code = null, string? rid = null)
    {
        RequestId = rid ?? HttpContext.TraceIdentifier;
        ErrorCode = code;

        // Сообщение из GlobalExceptionHandler (TempData survives redirect to /Error)
        ErrorMessage = TempData["ErrorMessage"] as string;
        ErrorPath = TempData["ErrorPath"] as string;
    }
}
