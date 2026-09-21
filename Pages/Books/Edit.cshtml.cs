using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Books.Data;
using Books.Models;

namespace Books.Pages.Books;

public class EditModel : PageModel
{
    private readonly BookRepository _repo;
    private readonly ILogger<EditModel> _logger;

    public EditModel(BookRepository repo, ILogger<EditModel> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [BindProperty]
    public Book Book { get; set; } = new();

    [BindProperty]
    public List<Chapter> Chapters { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var (book, chapters) = await _repo.GetByIdWithChaptersAsync(id);
        if (book == null)
            return NotFound();

        Book = book;
        Chapters = chapters;
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return Page();
        for (int i = 0; i < Chapters.Count; i++) Chapters[i].Number = (short)(i + 1);

        var code = await _repo.UpdateAsync(Book, Chapters);
        switch (code)
        {
            case OperationCode.Success:
                return RedirectToPage("Details", new { id = Book.Id });
            case OperationCode.Duplicate:
                _logger.LogWarning("Дубль ISBN {Isbn} при обновлении Id={Id}", Book.Isbn, Book.Id);
                ModelState.AddModelError(nameof(Book.Isbn), "Книга с таким ISBN уже есть.");
                return Page();
            case OperationCode.NotFound:
                ModelState.AddModelError(string.Empty, "Запись не найдена: возможно, она удалена ранее.");
                return Page();
            default:
                throw new InvalidOperationException($"Неожиданный код {(byte)code} от spBooksUpdate");
        }
    }

    public IActionResult OnPostAddChapter()
    {
        var last = Chapters.LastOrDefault();
        var start = last is null ? (short)1 : (short)(last.EndPage + 1);
        Chapters.Add(new Chapter
        {
            Number = (short)(Chapters.Count + 1),
            StartPage = start,
            EndPage = start
        });
        return Page();
    }

    public IActionResult OnPostRemoveChapter(int index)
    {
        if (index >= 0 && index < Chapters.Count)
            Chapters.RemoveAt(index);
        return Page();
    }
}