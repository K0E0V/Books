using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Books.Data;
using Books.Models;

namespace Books.Pages.Books;

public class DetailsModel : PageModel
{
    private readonly BookRepository _repo;

    public DetailsModel(BookRepository repo)
    {
        _repo = repo;
    }

    public Book? Book { get; set; }
    public List<Chapter> Chapters { get; set; } = new();
    public List<string> Genres { get; set; } = new();
    public List<string> BookTypes { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var (book, chapters) = await _repo.GetByIdWithChaptersAsync(id);

        if (book == null)
        {
            return NotFound();
        }

        Book = book;
        Chapters = chapters;
        (Genres, BookTypes) = await _repo.GetBookExtraNamesAsync(book.Id);
        return Page();
    }
}