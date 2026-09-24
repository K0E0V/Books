using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Books.Data;
using Books.Models;

namespace Books.Pages.Books;

public class CreateModel : PageModel


{
    private readonly BookRepository _repo;

    public CreateModel(BookRepository repo)
    {
        _repo = repo;
    }

    [BindProperty]
    public Book Book { get; set; } = new();

    [BindProperty]
    public List<Chapter> Chapters { get; set; } = new();

    // Справочники для формы
    public List<RefItem> Publishers { get; set; } = new();
    public List<RefItem> Genres { get; set; } = new();
    public List<RefItem> BookTypes { get; set; } = new();

    private async Task LoadRefsAsync()
    {
        Publishers = await _repo.GetPublishersAsync();
        Genres = await _repo.GetGenresAsync();
        BookTypes = await _repo.GetBookTypesAsync();
    }

    public async Task<IActionResult> OnGet()
    {
        Chapters = new List<Chapter> { new Chapter { Number = 1, Title = "Глава 1" } };
        await LoadRefsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadRefsAsync();
            return Page();
        }
        for (int i = 0; i < Chapters.Count; i++) Chapters[i].Number = (short)(i + 1);

        var (code, newId) = await _repo.CreateAsync(Book, Chapters);
        switch (code)
        {
            case OperationCode.Success:
                await _repo.SaveBookExtrasAsync(newId, Book.Publisher, Book.GenreIds, Book.BookTypeIds);
                return RedirectToPage("Details", new { id = newId });
            case OperationCode.Duplicate:
                ModelState.AddModelError(nameof(Book.Isbn), "Книга с таким ISBN уже есть.");
                await LoadRefsAsync();
                return Page();
            default:   // NotFound при создании недостижим: это нарушение контракта
                throw new InvalidOperationException($"Неожиданный код {(byte)code} от spBooksCreate");
        }
    }

    public async Task<IActionResult> OnPostAddChapter()
    {
        var last = Chapters.LastOrDefault();
        var start = last is null ? 1 : last.EndPage + 1;
        Chapters.Add(new Chapter
        {
            Number = (short)(Chapters.Count + 1),
            StartPage = start,
            EndPage = 0 // пустое — пользователь обязан ввести
        });
        await LoadRefsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveChapter(int index)
    {
        if (index >= 0 && index < Chapters.Count) Chapters.RemoveAt(index);
        await LoadRefsAsync();
        return Page();
    }

}