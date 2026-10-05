using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Books.Data;

/// <summary>
/// Маппер ошибок БД в контракт OperationCode (docs/ORM_MIGRATION_TZ.md, п. 3.4.1).
/// Воспроизводит поведение TRY/CATCH хранимых процедур spBooksCreate/Update/Delete:
///   2627/2601 (нарушение уникальности / UX_TblBooks_Isbn) → Duplicate
///   547       (нарушение FK)                              → Duplicate (контракт spBooksDelete)
///   прочее                                               → пробрасываем вверх
/// </summary>
public static class EfErrorMapper
{
    private static readonly Regex DuplicateKeyRegex =
        new(@"Cannot insert duplicate key|Violation of (UNIQUE|PRIMARY) KEY", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsDuplicateKey(DbUpdateException ex)
    {
        if (InnerSqlException(ex) is { } sql)
            return sql.Number == 2627 || sql.Number == 2601 || DuplicateKeyRegex.IsMatch(sql.Message);

        // SqlException не всегда достижима как тип (например, в тестах/провайдерах-заглушках) —
        // фолбэк на текстовый маппинг по внутреннему исключению.
        var inner = ex.InnerException;
        while (inner != null)
        {
            if (inner is not DbUpdateException && IsDuplicateKeyMessage(inner.Message))
                return true;
            inner = inner.InnerException;
        }
        return false;
    }

    /// <summary>Regex-часть маппинга «текст ошибки SQL Server → Duplicate» (вынесена для тестов, ТЗ п. 3.4.1).</summary>
    public static bool IsDuplicateKeyMessage(string message) => DuplicateKeyRegex.IsMatch(message);

    public static bool IsForeignKeyViolation(DbUpdateException ex) =>
        InnerSqlException(ex)?.Number == 547;

    private static SqlException? InnerSqlException(Exception ex) => ex switch
    {
        DbUpdateException dbe => dbe.InnerException switch
        {
            SqlException sql => sql,
            Exception inner => InnerSqlException(inner),
            _ => null
        },
        _ => null
    };
}
