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
                return RedirectToPage("Index");                       // PRG
            case OperationCode.Duplicate:                             // FK: на книгу есть ссылки
                ModelState.AddModelError(string.Empty,
                    "Книгу нельзя удалить: на неё ссылаются другие записи.");
                return Page();
            case OperationCode.NotFound:
                return NotFound();                                    // удалена ранее — честный 404
            default:
                throw new InvalidOperationException(
                    $"Неожиданный код {(byte)code} от spBooksDelete");
        }
    }
}