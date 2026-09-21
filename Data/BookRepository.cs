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