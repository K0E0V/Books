using System.Data;
using System.Text;
using System.Security;
using Books.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Books.Data;

public class BookRepository
{
    private readonly string _connectionString;

    public BookRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Получение книги с главами
    public async Task<(Book? Book, List<Chapter> Chapters)> GetByIdWithChaptersAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);

        using var multi = await connection.QueryMultipleAsync(
            "dbo.spBooksGetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);

        var book = await multi.ReadFirstOrDefaultAsync<Book>();
        var chapters = (await multi.ReadAsync<Chapter>()).ToList();

        if (book != null)
        {
            var extras = await GetBookExtrasAsync(connection, book.Id);
            book.GenreIds = extras.GenreIds;
            book.BookTypeIds = extras.BookTypeIds;
        }

        return (book, chapters);
    }

    // Создание книги
    public async Task<(OperationCode Code, int NewId)> CreateAsync(Book book, List<Chapter> chapters)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();

        parameters.AddDynamicParams(new
        {
            book.Title,
            book.Author,
            book.PublicationYear,
            book.Isbn,
            book.Publisher,
            book.Description,
            book.CountPages, // NOT NULL в dbo.TblBooks — всегда конкретное число
            ContentsXml = GenerateXmlFromChapters(chapters)
        });

        parameters.Add("@NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("@ResultCode", dbType: DbType.Byte, direction: ParameterDirection.Output);


        await connection.ExecuteAsync("dbo.spBooksCreate", parameters,
        commandType: CommandType.StoredProcedure);

        return ((OperationCode)parameters.Get<byte>("@ResultCode"), parameters.Get<int>("@NewId"));    
    }

    // Обновление книги
    // Обновление книги — контракт ADR-002: код операции наружу, техника насквозь
    public async Task<OperationCode> UpdateAsync(Book book, List<Chapter> chapters)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.AddDynamicParams(new
        {
            book.Id,
            book.Title,
            book.Author,
            book.PublicationYear,
            book.Isbn,
            book.Publisher,
            book.Description,
            book.CountPages, // NOT NULL в dbo.TblBooks — всегда конкретное число
            ContentsXml = GenerateXmlFromChapters(chapters)
        });
        parameters.Add("@ResultCode", dbType: DbType.Byte, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "dbo.spBooksUpdate",
            parameters,
            commandType: CommandType.StoredProcedure);

        return (OperationCode)parameters.Get<byte>("@ResultCode");
    }

    // Удаление книги
    // Удаление книги — контракт ADR-002
    public async Task<OperationCode> DeleteAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);
        parameters.Add("@ResultCode", dbType: DbType.Byte, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(
            "dbo.spBooksDelete",
            parameters,
            commandType: CommandType.StoredProcedure);

        return (OperationCode)parameters.Get<byte>("@ResultCode");
    }

    // Получение всех книг (для списка)
    public async Task<(List<Book> Books, int TotalCount)> GetAllAsync(int pageNumber = 1, int pageSize = 10)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new { PageNumber = pageNumber, PageSize = pageSize };

        using var multi = await connection.QueryMultipleAsync(
            "dbo.spBooksGetList",
            parameters,
            commandType: CommandType.StoredProcedure);

        var books = (await multi.ReadAsync<Book>()).ToList();
        var totalCount = await multi.ReadFirstAsync<int>();

        return (books, totalCount);
    }

    // Поиск книг
    public async Task<(List<Book> Books, int TotalCount)> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new { Search = searchTerm, PageNumber = pageNumber, PageSize = pageSize };

        using var multi = await connection.QueryMultipleAsync(
            "dbo.spBooksSearch",
            parameters,
            commandType: CommandType.StoredProcedure);

        var books = (await multi.ReadAsync<Book>()).ToList();
        var totalCount = await multi.ReadFirstAsync<int>();

        return (books, totalCount);
    }

    // Статистика для /About: количество книг и суммарная длина полки по CountPages
    public async Task<(int BookCount, long TotalPages)> GetAboutStatsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstAsync<(int BookCount, long TotalPages)>(@"
            SELECT COUNT(*)                    AS BookCount,
                   ISNULL(SUM(CountPages), 0)  AS TotalPages
            FROM dbo.tblBooks");
    }

    // ===== СПРАВОЧНИКИ =====

    public async Task<List<RefItem>> GetPublishersAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return (await connection.QueryAsync<RefItem>(
            "SELECT Id, Name FROM dbo.TblPublishers ORDER BY Name")).ToList();
    }

    public async Task<List<RefItem>> GetGenresAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return (await connection.QueryAsync<RefItem>(
            "SELECT Id, Name FROM dbo.TblGenres ORDER BY Name")).ToList();
    }

    public async Task<List<RefItem>> GetBookTypesAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return (await connection.QueryAsync<RefItem>(
            "SELECT Id, Name FROM dbo.TblTypes ORDER BY Name")).ToList();
    }

    // Названия жанров/типов для отображения в Details (текстовые списки)
    public async Task<(List<string> Genres, List<string> BookTypes)> GetBookExtraNamesAsync(int bookId)
    {
        using var connection = new SqlConnection(_connectionString);
        var genres = (await connection.QueryAsync<string>(@"
            SELECT g.Name
            FROM dbo.TblBookGenres bg
            JOIN dbo.TblGenres g ON g.Id = bg.GenreId
            WHERE bg.BookId = @BookId
            ORDER BY g.Name", new { BookId = bookId })).ToList();
        var types = (await connection.QueryAsync<string>(@"
            SELECT t.Name
            FROM dbo.TblBookTypes bt
            JOIN dbo.TblTypes t ON t.Id = bt.TypeId
            WHERE bt.BookId = @BookId
            ORDER BY t.Name", new { BookId = bookId })).ToList();
        return (genres, types);
    }

    // ===== СВЯЗИ КНИГИ СО СПРАВОЧНИКАМИ =====

    private static async Task<(List<int> GenreIds, List<int> BookTypeIds)> GetBookExtrasAsync(
        IDbConnection connection, int bookId)
    {
        var genreIds = (await connection.QueryAsync<int>(
            "SELECT GenreId FROM dbo.TblBookGenres WHERE BookId = @BookId",
            new { BookId = bookId })).ToList();
        var typeIds = (await connection.QueryAsync<int>(
            "SELECT TypeId FROM dbo.TblBookTypes WHERE BookId = @BookId",
            new { BookId = bookId })).ToList();
        return (genreIds, typeIds);
    }

    // Сохранение дополнительных реквизитов (жанры, типы, издательство-справочник).
    // Вызывается после успешного spBooksCreate / spBooksUpdate. Прямой SQL, вне ХП — по ТЗ.
    public async Task SaveBookExtrasAsync(int bookId, string? publisherName, List<int> genreIds, List<int> bookTypeIds)
    {
        using var connection = new SqlConnection(_connectionString);
        await SaveBookExtrasInternalAsync(connection, bookId, publisherName, genreIds, bookTypeIds);
    }

    private static async Task SaveBookExtrasInternalAsync(
        IDbConnection connection, int bookId, string? publisherName, List<int> genreIds, List<int> bookTypeIds)
    {
        // Издательство: если ввели название, которого нет в справочнике — добавляем.
        if (!string.IsNullOrWhiteSpace(publisherName))
        {
            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.TblPublishers WHERE Name = @Name)
                    INSERT INTO dbo.TblPublishers (Name) VALUES (@Name);",
                new { Name = publisherName.Trim() });
        }

        // Жанры: заменяем набор связей на выбранный.
        await connection.ExecuteAsync(
            "DELETE FROM dbo.TblBookGenres WHERE BookId = @BookId;", new { BookId = bookId });
        foreach (var gid in (genreIds ?? new List<int>()).Distinct())
        {
            await connection.ExecuteAsync(
                "INSERT INTO dbo.TblBookGenres (BookId, GenreId) VALUES (@BookId, @GenreId);",
                new { BookId = bookId, GenreId = gid });
        }

        // Типы книг: аналогично.
        await connection.ExecuteAsync(
            "DELETE FROM dbo.TblBookTypes WHERE BookId = @BookId;", new { BookId = bookId });
        foreach (var tid in (bookTypeIds ?? new List<int>()).Distinct())
        {
            await connection.ExecuteAsync(
                "INSERT INTO dbo.TblBookTypes (BookId, TypeId) VALUES (@BookId, @TypeId);",
                new { BookId = bookId, TypeId = tid });
        }
    }

    // ===== ВСПОМОГАТЕЛЬНЫЙ МЕТОД =====
    // Собирает XML из списка глав
    private string GenerateXmlFromChapters(List<Chapter> chapters)
    {
        if (chapters == null || chapters.Count == 0)
            return "<BookContents></BookContents>";

        var xml = new StringBuilder();
        xml.AppendLine("<BookContents>");

        foreach (var ch in chapters)
        {
            // Экранируем специальные XML-символы для безопасности
            var safeTitle = SecurityElement.Escape(ch.Title) ?? ch.Title;

            xml.AppendLine(
                $"  <Chapter number=\"{ch.Number}\" " +
                $"title=\"{safeTitle}\" " +
                $"startPage=\"{ch.StartPage}\" " +
                $"endPage=\"{ch.EndPage}\"/>");
        }

        xml.AppendLine("</BookContents>");
        return xml.ToString();
    }
}