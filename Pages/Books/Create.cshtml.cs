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

    public IActionResult OnGet()
    {
        Chapters = new List<Chapter> { new Chapter { Number = 1, Title = "Глава 1" } };
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return Page();
        for (int i = 0; i < Chapters.Count; i++) Chapters[i].Number = (short)(i + 1);

        var (code, newId) = await _repo.CreateAsync(Book, Chapters);
        switch (code)
        {
            case OperationCode.Success:
                return RedirectToPage("Details", new { id = newId });
            case OperationCode.Duplicate:
                ModelState.AddModelError(nameof(Book.Isbn), "Книга с таким ISBN уже есть.");
                return Page();
            default:   // NotFound при создании недостижим: это нарушение контракта
                throw new InvalidOperationException($"Неожиданный код {(byte)code} от spBooksCreate");
        }
    }

    public IActionResult OnPostAddChapter()
    {
        Chapters.Add(new Chapter { Number = (short)(Chapters.Count + 1) });
        return Page();
    }

    public IActionResult OnPostRemoveChapter(int index)
    {
        if (index >= 0 && index < Chapters.Count) Chapters.RemoveAt(index);
        return Page();
    }

}