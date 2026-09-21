using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Books.Data;
using Books.Models;

namespace Books.Pages.Books;

public class DeleteModel : PageModel
{
    private readonly BookRepository _repo;

    public DeleteModel(BookRepository repo)
    {
        _repo = repo;
    }

    public Book? Book { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var (book, chapters) = await _repo.GetByIdWithChaptersAsync(id);
        if (book == null)
            return NotFound();

        Book = book;       
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id)
    {
        var code = await _repo.DeleteAsync(id);
        switch (code)
        {
            case OperationCode.Success:
                TempData["SuccessMessage"] = "Книга успешно удалена.";
                return RedirectToPage("Index");                       // PRG
            case OperationCode.Duplicate:                             // FK: на книгу есть ссылки
                ModelState.AddModelError(string.Empty,
                    "Книгу нельзя удалить: на неё ссылаются другие записи.");
                // Перезагружаем данные для отображения страницы подтверждения снова
                var (book, _) = await _repo.GetByIdWithChaptersAsync(id);
                Book = book;
                return Page();
            case OperationCode.NotFound:
                // Книга уже удалена или не найдена — редирект на список с сообщением
                TempData["ErrorMessage"] = "Книга не найдена: возможно, она уже была удалена.";
                return RedirectToPage("Index");
            default:
                throw new InvalidOperationException(
                    $"Неожиданный код {(byte)code} от spBooksDelete");
        }
    }
}