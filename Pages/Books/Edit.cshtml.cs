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

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var (book, chapters) = await _repo.GetByIdWithChaptersAsync(id);
        if (book == null)
            return NotFound();

        Book = book;
        Chapters = chapters;
        await LoadRefsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddChapter()
    {
        var last = Chapters.LastOrDefault();
        var start = last is null ? (short)1 : (short)(last.EndPage + 1);
        Chapters.Add(new Chapter
        {
            Number = (short)(Chapters.Count + 1),
            StartPage = start,
            EndPage = 0 // Пустое значение для валидации
        });
        await LoadRefsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveChapter(int index)
    {
        if (index >= 0 && index < Chapters.Count)
            Chapters.RemoveAt(index);
        await LoadRefsAsync();
        return Page();
    }

    private string? ValidateChapterRanges()
    {
        // Проверяем, что у всех глав заполнен EndPage
        for (int i = 0; i < Chapters.Count; i++)
        {
            if (Chapters[i].EndPage == 0)
            {
                return $"У главы \"{Chapters[i].Title}\" не указана конечная страница. Заполните поле.";
            }
        }

        var sorted = Chapters.OrderBy(c => c.StartPage).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].StartPage <= sorted[i - 1].EndPage)
            {
                return $"Диапазоны страниц пересекаются: глава \"{sorted[i].Title}\" (стр. {sorted[i].StartPage}) начинается раньше, чем заканчивается предыдущая глава \"{sorted[i - 1].Title}\" (стр. {sorted[i - 1].EndPage}).";
            }
        }
        return null;
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        // Нумеруем главы
        for (int i = 0; i < Chapters.Count; i++) 
            Chapters[i].Number = (short)(i + 1);
        
        // Проверяем валидацию диапазонов
        var validationError = ValidateChapterRanges();
        if (!string.IsNullOrEmpty(validationError))
        {
            ModelState.AddModelError(string.Empty, validationError);
            // НЕ перезагружаем данные из БД, чтобы сохранить введенные пользователем значения
            await LoadRefsAsync();
            return Page();
        }

        if (!ModelState.IsValid)
        {
            // НЕ перезагружаем данные из БД, чтобы сохранить введенные пользователем значения и показать ошибки валидации
            await LoadRefsAsync();
            return Page();
        }

        var code = await _repo.UpdateAsync(Book, Chapters);
        switch (code)
        {
            case OperationCode.Success:
                await _repo.SaveBookExtrasAsync(Book.Id, Book.Publisher, Book.GenreIds, Book.BookTypeIds);
                return RedirectToPage("Details", new { id = Book.Id });
            case OperationCode.Duplicate:
                _logger.LogWarning("Дубль ISBN {Isbn} при обновлении Id={Id}", Book.Isbn, Book.Id);
                ModelState.AddModelError(nameof(Book.Isbn), "Книга с таким ISBN уже есть.");
                break;
            case OperationCode.NotFound:
                ModelState.AddModelError(string.Empty, "Запись не найдена: возможно, она удалена ранее.");
                break;
            default:
                throw new InvalidOperationException($"Неожиданный код {(byte)code} от spBooksUpdate");
        }

        // Перезагружаем данные при ошибке бизнес-логики (Duplicate/NotFound), так как там могли измениться данные в БД или запись удалена
        var (reloadBook, reloadChapters) = await _repo.GetByIdWithChaptersAsync(Book.Id);
        if (reloadBook == null) return NotFound();
        Book = reloadBook;
        Chapters = reloadChapters;
        await LoadRefsAsync();
        return Page();
    }
}