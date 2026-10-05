using Books.Models;

namespace Books.Data;

/// <summary>
/// Контракт доступа к данным. Исторический публичный API репозитория сохранён
/// без изменений (см. docs/ORM_MIGRATION_TZ.md, п. 2 «В области»), чтобы
/// Razor Pages не зависели от конкретной реализации (Dapper+ХП или EF Core).
/// </summary>
public interface IBookRepository
{
    // ===== ЧТЕНИЕ =====
    Task<(Book? Book, List<Chapter> Chapters)> GetByIdWithChaptersAsync(int id);

    Task<(List<Book> Books, int TotalCount)> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? search = null,
        int? genreId = null,
        int? typeId = null,
        int? year = null,
        string? author = null,
        string? sortBy = null,
        bool sortDesc = false);

    // SearchAsync удалён (Этап 4 ТЗ docs/ORM_MIGRATION_TZ.md): ни одна страница
    // не вызывал метод — Index использует GetAllAsync со структурированными фильтрами.

    Task<List<string>> GetAuthorsAsync();
    Task<List<int>> GetYearsAsync();

    // ===== СПРАВОЧНИКИ =====
    Task<List<RefItem>> GetPublishersAsync();
    Task<List<RefItem>> GetGenresAsync();
    Task<List<RefItem>> GetBookTypesAsync();
    Task<(List<string> Genres, List<string> BookTypes)> GetBookExtraNamesAsync(int bookId);

    // ===== ЗАПИСЬ =====
    Task<(OperationCode Code, int NewId)> CreateAsync(Book book, List<Chapter> chapters);
    Task<OperationCode> UpdateAsync(Book book, List<Chapter> chapters);
    Task<OperationCode> DeleteAsync(int id);
    Task SaveBookExtrasAsync(int bookId, string? publisherName, List<int> genreIds, List<int> bookTypeIds);

    // ===== СТАТИСТИКА =====
    Task<AboutStats> GetAboutStatsAsync();
}
