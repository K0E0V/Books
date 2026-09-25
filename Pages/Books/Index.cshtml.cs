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

    // Параметры фильтрации/сортировки (BIND)
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? GenreId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? TypeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Author { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }   // title | author | year | pages | id

    [BindProperty(SupportsGet = true)]
    public bool SortDesc { get; set; }

    // Данные для выпадающих списков фильтров
    public List<RefItem> Genres { get; set; } = new();
    public List<RefItem> BookTypes { get; set; } = new();
    public List<string> Authors { get; set; } = new();
    public List<int> Years { get; set; } = new();

    public async Task OnGetAsync(int page = 1)
    {
        CurrentPage = page < 1 ? 1 : page;

        Genres = await _repo.GetGenresAsync();
        BookTypes = await _repo.GetBookTypesAsync();
        Authors = await _repo.GetAuthorsAsync();
        Years = await _repo.GetYearsAsync();

        var result = await _repo.GetAllAsync(
            CurrentPage, PageSize,
            search: Search,
            genreId: GenreId,
            typeId: TypeId,
            year: Year,
            author: Author,
            sortBy: SortBy,
            sortDesc: SortDesc);

        Books = result.Books;
        TotalCount = result.TotalCount;
    }

    // Ссылка на смену сортировки по колонке: сохраняет текущие фильтры,
    // при повторном клике по той же колонке меняет направление.
    public string SortLink(string column)
    {
        bool newDesc = SortBy == column && !SortDesc;
        return Href(page: CurrentPage, sortBy: column, sortDesc: newDesc);
    }

    public string Href(int page, string? sortBy = null, bool? sortDesc = null)
    {
        var qs = new List<string> { $"page={page}" };

        void Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                qs.Add($"{name}={Uri.EscapeDataString(value)}");
        }

        Add("search", Search);
        Add("genreId", GenreId?.ToString());
        Add("typeId", TypeId?.ToString());
        Add("year", Year?.ToString());
        Add("author", Author);

        var sb = sortBy ?? SortBy;
        var sd = sortDesc ?? SortDesc;
        if (!string.IsNullOrWhiteSpace(sb))
        {
            qs.Add($"sortBy={Uri.EscapeDataString(sb)}");
            if (sd) qs.Add("sortDesc=true");
        }

        return "?" + string.Join("&", qs);
    }
}
