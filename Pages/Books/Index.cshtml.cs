using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Books.Data;
using Books.Models;

namespace Books.Pages.Books;

public class IndexModel : PageModel
{
    private readonly BookRepository _repo;

    public IndexModel(BookRepository repo)
    {
        _repo = repo;
    }
  

    public List<Book> Books { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }

    public async Task OnGetAsync(int page = 1, string? search = null)
    {
        CurrentPage = page;
        SearchTerm = search;

        if (string.IsNullOrWhiteSpace(search))
        {
            var result = await _repo.GetAllAsync(page, PageSize);
            Books = result.Books;
            TotalCount = result.TotalCount;
        }
        else
        {
            var result = await _repo.SearchAsync(search, page, PageSize);
            Books = result.Books;
            TotalCount = result.TotalCount;
        }
    }
}