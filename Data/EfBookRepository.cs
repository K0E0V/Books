using Books.Models;
using Microsoft.EntityFrameworkCore;

namespace Books.Data;

/// <summary>
/// Реализация IBookRepository на EF Core 8 (Этапы 1–4 ТЗ docs/ORM_MIGRATION_TZ.md).
/// Публичный API идентичен Dapper-реализации <see cref="BookRepository"/>,
/// поэтому Razor Pages переключаются только смене реализации в DI (+feature-flag).
/// </summary>
public class EfBookRepository : IBookRepository
{
    private readonly BookLibraryContext _db;

    public EfBookRepository(BookLibraryContext db)
    {
        _db = db;
    }

    // ===== ЧТЕНИЕ =====

    public async Task<(Book? Book, List<Chapter> Chapters)> GetByIdWithChaptersAsync(int id)
    {
        var entity = await _db.Books
            .AsNoTracking()
            .Include(b => b.GenreLinks)
                .ThenInclude(l => l.Genre)
            .Include(b => b.TypeLinks)
                .ThenInclude(l => l.Type)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (entity is null)
            return (null, new List<Chapter>());

        var book = ToModel(entity);
        var chapters = ContentsXmlConverter.FromXml(entity.ContentsXmlRaw);
        return (book, chapters);
    }

    public async Task<(List<Book> Books, int TotalCount)> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? search = null,
        int? genreId = null,
        int? typeId = null,
        int? year = null,
        string? author = null,
        string? sortBy = null,
        bool sortDesc = false)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10; // как в spBooksGetListV2

        var query = ApplyFilters(
            _db.Books.AsNoTracking(), search, genreId, typeId, year, author);

        var totalCount = await query.CountAsync();

        query = ApplySort(query, sortBy, sortDesc);

        var entities = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var books = entities.Select(ToModel).ToList();
        await AttachGenresAndTypesAsync(books);

        return (books, totalCount);
    }

#pragma warning disable CS0618 // SearchAsync помечен [Obsolete] в контракте для обратной совместимости
    [Obsolete("Оставлен для обратной совместимости; страница Index использует GetAllAsync с фильтрами.")]
    public Task<(List<Book> Books, int TotalCount)> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
        => GetAllAsync(pageNumber, pageSize, search: searchTerm);
#pragma warning restore CS0618

    public async Task<List<string>> GetAuthorsAsync() =>
        await _db.Books.AsNoTracking()
            .Where(b => b.Author != null && b.Author != "")
            .Select(b => b.Author)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

    public async Task<List<int>> GetYearsAsync() =>
        await _db.Books.AsNoTracking()
            .Select(b => b.PublicationYear)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

    // ===== СПРАВОЧНИКИ =====

    public async Task<List<RefItem>> GetPublishersAsync() =>
        await _db.Publishers.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new RefItem { Id = p.Id, Name = p.Name })
            .ToListAsync();

    public async Task<List<RefItem>> GetGenresAsync() =>
        await _db.Genres.AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new RefItem { Id = g.Id, Name = g.Name })
            .ToListAsync();

    public async Task<List<RefItem>> GetBookTypesAsync() =>
        await _db.BookTypes.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new RefItem { Id = t.Id, Name = t.Name })
            .ToListAsync();

    public async Task<(List<string> Genres, List<string> BookTypes)> GetBookExtraNamesAsync(int bookId)
    {
        var genres = await _db.BookGenres.AsNoTracking()
            .Where(x => x.BookId == bookId)
            .Select(x => x.Genre!.Name)
            .OrderBy(n => n)
            .ToListAsync();

        var types = await _db.BookTypeLinks.AsNoTracking()
            .Where(x => x.BookId == bookId)
            .Select(x => x.Type!.Name)
            .OrderBy(n => n)
            .ToListAsync();

        return (genres, types);
    }

    // ===== ЗАПИСЬ =====

    public async Task<(OperationCode Code, int NewId)> CreateAsync(Book book, List<Chapter> chapters)
    {
        var entity = new BookEntity
        {
            Title = book.Title,
            Author = book.Author,
            PublicationYear = book.PublicationYear,
            Isbn = book.Isbn,
            Publisher = book.Publisher,
            Description = book.Description,
            CountPages = book.CountPages,
            ContentsXmlRaw = ContentsXmlConverter.ToXml(chapters),
        };

        var now = DateTime.UtcNow;
        entity.CreatedAt = now;
        entity.UpdatedAt = now;

        _db.Books.Add(entity);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EfErrorMapper.IsDuplicateKey(ex))
        {
            _db.ChangeTracker.Clear(); // эквивалент CATCH 2627/2601 в spBooksCreate
            return (OperationCode.Duplicate, 0);
        }

        return (OperationCode.Success, entity.Id);
    }

    public async Task<OperationCode> UpdateAsync(Book book, List<Chapter> chapters)
    {
        var entity = await _db.Books.FirstOrDefaultAsync(b => b.Id == book.Id);
        if (entity is null)
            return OperationCode.NotFound; // @@ROWCOUNT = 0 в spBooksUpdate

        entity.Title = book.Title;
        entity.Author = book.Author;
        entity.PublicationYear = book.PublicationYear;
        entity.Isbn = book.Isbn;
        entity.Publisher = book.Publisher;
        entity.Description = book.Description;
        entity.CountPages = book.CountPages;
        entity.ContentsXmlRaw = ContentsXmlConverter.ToXml(chapters);
        entity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EfErrorMapper.IsDuplicateKey(ex))
        {
            return OperationCode.Duplicate;
        }

        return OperationCode.Success;
    }

    public async Task<OperationCode> DeleteAsync(int id)
    {
        try
        {
            // set-based удаление без загрузки графа; join-строки TblBookGenres/TblBookTypes
            // снимаются FK ON DELETE CASCADE на стороне БД
            var rows = await _db.Books.Where(b => b.Id == id).ExecuteDeleteAsync();
            return rows == 0 ? OperationCode.NotFound : OperationCode.Success;
        }
        catch (DbUpdateException ex) when (EfErrorMapper.IsForeignKeyViolation(ex))
        {
            // эквивалент CATCH 547 в spBooksDelete → Duplicate («на книгу есть ссылки»)
            return OperationCode.Duplicate;
        }
    }

    /// <summary>
    /// Замена наборов жанров/типов и доводка справочника издательств одним SaveChanges
    /// (в транзакции) вместо текущего DELETE + N×INSERT без явной транзакции.
    /// </summary>
    public async Task SaveBookExtrasAsync(int bookId, string? publisherName, List<int> genreIds, List<int> bookTypeIds)
    {
        var entity = await _db.Books
            .Include(b => b.GenreLinks)
            .Include(b => b.TypeLinks)
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (entity is null)
            return; // книга удалена конкурентно — молча выходим, как старый код после успешного кода операции

        // Издательство: если ввели название, которого нет в справочнике — добавляем.
        var trimmed = publisherName?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            var exists = await _db.Publishers.AnyAsync(p => p.Name == trimmed);
            if (!exists)
                _db.Publishers.Add(new PublisherEntity { Name = trimmed });
        }

        ReplaceLinks(entity, genreIds ?? new List<int>());
        ReplaceTypeLinks(entity, bookTypeIds ?? new List<int>());

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EfErrorMapper.IsDuplicateKey(ex))
        {
            // гонка при вставке имени издателя в unique UX_TblPublishers_Name — не критично
            _db.ChangeTracker.Clear();
        }
    }

    // ===== СТАТИСТИКА =====

    public async Task<AboutStats> GetAboutStatsAsync()
    {
        var since = DateTime.UtcNow.AddHours(-24); // CreatedAt заполняется sysutcdatetime() — UTC (исправлен баг DateTime.Now)

        // Один запрос вместо трёх round-trip
        var stats = await _db.Books.AsNoTracking()
            .GroupBy(b => 1)
            .Select(g => new
            {
                TotalBooks = g.Count(),
                BooksLast24h = g.Count(b => b.CreatedAt >= since),
                TotalPages = g.Sum(b => (int?)b.CountPages) ?? 0
            })
            .FirstOrDefaultAsync();

        if (stats is null)
            return new AboutStats(0, 0, 0);

        return new AboutStats(stats.TotalBooks, stats.BooksLast24h, stats.TotalPages);
    }

    // ===== ВСПОМОГАТЕЛЬНЫЕ =====

    private static IQueryable<BookEntity> ApplyFilters(
        IQueryable<BookEntity> query,
        string? search, int? genreId, int? typeId, int? year, string? author)
    {
        var s = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (s != null)
        {
            // Эквивалент LIKE N'%@Search%' по названию/автору/ISBN/оглавлению из spBooksGetListV2.
            // Contains по колонке xml EF translating не может — сравниваем строковое представление XML
            // (колонка объявлена HasColumnType("xml"), значение читается какnvarchar(MAX)).
            query = query.Where(b =>
                EF.Functions.Like(b.Title, $"%{s}%") ||
                EF.Functions.Like(b.Author, $"%{s}%") ||
                (b.Isbn != null && EF.Functions.Like(b.Isbn, $"%{s}%")) ||
                (b.ContentsXmlRaw != null && EF.Functions.Like(b.ContentsXmlRaw, $"%{s}%")));
        }

        if (genreId.HasValue)
            query = query.Where(b => b.GenreLinks.Any(x => x.GenreId == genreId.Value));

        if (typeId.HasValue)
            query = query.Where(b => b.TypeLinks.Any(x => x.TypeId == typeId.Value));

        if (year.HasValue)
            query = query.Where(b => b.PublicationYear == year.Value);

        if (!string.IsNullOrWhiteSpace(author))
        {
            var a = author.Trim();
            query = query.Where(b => b.Author == a);
        }

        return query;
    }

    private static IOrderedQueryable<BookEntity> ApplySort(IQueryable<BookEntity> query, string? sortBy, bool desc)
    {
        // whitelist значений, идентичный CASE-сортировке spBooksGetListV2; иначе — по Id
        var key = string.IsNullOrWhiteSpace(sortBy) ? "id" : sortBy.Trim().ToLowerInvariant();

        IOrderedQueryable<BookEntity> ordered = key switch
        {
            "title" => desc ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
            "author" => desc ? query.OrderByDescending(b => b.Author) : query.OrderBy(b => b.Author),
            "year" => desc ? query.OrderByDescending(b => b.PublicationYear) : query.OrderBy(b => b.PublicationYear),
            "pages" => desc ? query.OrderByDescending(b => b.CountPages) : query.OrderBy(b => b.CountPages),
            _ => desc ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Id),
        };

        return ordered.ThenBy(b => b.Id); // детерминированная пагинация (как Id ASC в ХП)
    }

    /// <summary>Пакетная подгрузка названий/ид жанров и типов для списка (один запрос, без раздувания Include).</summary>
    private async Task AttachGenresAndTypesAsync(List<Book> books)
    {
        if (books.Count == 0) return;

        var ids = books.Select(b => b.Id).ToList();

        var genreRows = await _db.BookGenres.AsNoTracking()
            .Where(x => ids.Contains(x.BookId))
            .Select(x => new { x.BookId, Name = x.Genre!.Name, x.GenreId })
            .ToListAsync();

        var typeRows = await _db.BookTypeLinks.AsNoTracking()
            .Where(x => ids.Contains(x.BookId))
            .Select(x => new { x.BookId, Name = x.Type!.Name, x.TypeId })
            .ToListAsync();

        foreach (var book in books)
        {
            book.Genres = genreRows.Where(r => r.BookId == book.Id).Select(r => r.Name).OrderBy(n => n).ToList();
            book.BookTypes = typeRows.Where(r => r.BookId == book.Id).Select(r => r.Name).OrderBy(n => n).ToList();
            book.GenreIds = genreRows.Where(r => r.BookId == book.Id).Select(r => r.GenreId).OrderBy(i => i).ToList();
            book.BookTypeIds = typeRows.Where(r => r.BookId == book.Id).Select(r => r.TypeId).OrderBy(i => i).ToList();
        }
    }

    private static Book ToModel(BookEntity e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Author = e.Author,
        PublicationYear = e.PublicationYear,
        Isbn = e.Isbn,
        Publisher = e.Publisher,
        Description = e.Description,
        CountPages = e.CountPages,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        GenreIds = e.GenreLinks.Select(l => l.GenreId).OrderBy(i => i).ToList(),
        BookTypeIds = e.TypeLinks.Select(l => l.TypeId).OrderBy(i => i).ToList(),
        Genres = e.GenreLinks.Select(l => l.Genre!.Name).OrderBy(n => n).ToList(),
        BookTypes = e.TypeLinks.Select(l => l.Type!.Name).OrderBy(n => n).ToList(),
    };

    private void ReplaceLinks(BookEntity entity, List<int> desiredGenreIds)
    {
        var desired = desiredGenreIds.Distinct().ToHashSet();
        var current = entity.GenreLinks.ToList();

        foreach (var link in current.Where(l => !desired.Contains(l.GenreId)))
            _db.BookGenres.Remove(link);

        foreach (var gid in desired.Where(id => current.All(l => l.GenreId != id)))
            entity.GenreLinks.Add(new BookGenreLink { BookId = entity.Id, GenreId = gid });
    }

    private void ReplaceTypeLinks(BookEntity entity, List<int> desiredTypeIds)
    {
        var desired = desiredTypeIds.Distinct().ToHashSet();
        var current = entity.TypeLinks.ToList();

        foreach (var link in current.Where(l => !desired.Contains(l.TypeId)))
            _db.BookTypeLinks.Remove(link);

        foreach (var tid in desired.Where(id => current.All(l => l.TypeId != id)))
            entity.TypeLinks.Add(new BookTypeLink { BookId = entity.Id, TypeId = tid });
    }
}
