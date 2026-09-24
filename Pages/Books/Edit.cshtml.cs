using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Books.Data;
using Books.Models;
using Books.Validation;


namespace Books.Pages.Books;

public class EditModel : PageModel
{
    private readonly BookRepository _repo;
    private readonly ILogger<EditModel> _logger;
    private readonly IBookEditValidator _validator;

    public EditModel(
        BookRepository repo,
        ILogger<EditModel> logger,
        IBookEditValidator validator)
    {
        _repo = repo;
        _logger = logger;
        _validator = validator;
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
    public async Task<IActionResult> OnPostSaveAsync()
    {
        // Нумеруем главы
        for (int i = 0; i < Chapters.Count; i++)
            Chapters[i].Number = (short)(i + 1);


        // 1. Человечные сообщения для ошибок привязки
        ModelState.HumanizeNumericBindingErrors();

        // 2. Бизнес-валидация: обязательность, ISBN, диапазоны, пересечения
        var validation = _validator.Validate(Book, Chapters);

        // 3. Перекладываем ошибки в ModelState
        validation.AddToModelState(ModelState);

        // 4. СТОП-КРАН: стоит ПОСЛЕ сбора всех ошибок и ДО сохранения
        if (!ModelState.IsValid)
        {
            // НЕ перезагружаем данные из БД, чтобы сохранить введенные пользователем значения и показать ошибки валидации
            await LoadRefsAsync();
            return Page();
        }

        // 5. Сохранение — только для прошедших проверку данных
        var (dbBook, _) = await _repo.GetByIdWithChaptersAsync(Book.Id);
        if (dbBook == null)
        {
            ModelState.AddModelError(string.Empty, "Запись не найдена: возможно, она удалена ранее.");
            return Page();
        }

        dbBook.Title = Book.Title;
        dbBook.Author = Book.Author;
        dbBook.PublicationYear = Book.PublicationYear;
        dbBook.Isbn = Book.Isbn;
        dbBook.Publisher = Book.Publisher;
        dbBook.Description = Book.Description;

        var code = await _repo.UpdateAsync(dbBook, Chapters);

        switch (code)
        {
            case OperationCode.Success:
                await _repo.SaveBookExtrasAsync(dbBook.Id, dbBook.Publisher, Book.GenreIds, Book.BookTypeIds);
                return RedirectToPage("Details", new { id = dbBook.Id });

            case OperationCode.Duplicate:
                _logger.LogWarning("Дубль ISBN {Isbn} при обновлении Id={Id}", dbBook.Isbn, dbBook.Id);
                ModelState.AddModelError(nameof(Book.Isbn), "Книга с таким ISBN уже есть.");
                break;

            case OperationCode.NotFound:
                ModelState.AddModelError(string.Empty, "Запись не найдена: возможно, она удалена ранее.");
                break;

            default:
                throw new InvalidOperationException($"Неожиданный код {(byte)code} от spBooksUpdate");
        }

        var (reloadBook, reloadChapters) = await _repo.GetByIdWithChaptersAsync(Book.Id);
        if (reloadBook == null)
            return NotFound();

        Book = reloadBook;
        Chapters = reloadChapters;
        await LoadRefsAsync();
        return Page();
    }

}