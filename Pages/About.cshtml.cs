using Books.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Books.Pages;

public class AboutModel : PageModel
{
    private readonly BookRepository _repository;

    public AboutModel(BookRepository repository)
    {
        _repository = repository;
    }

    public int BookCount { get; private set; }
    public long TotalPages { get; private set; }

    public async Task OnGetAsync()
    {
        (BookCount, TotalPages) = await _repository.GetAboutStatsAsync();
    }
}
