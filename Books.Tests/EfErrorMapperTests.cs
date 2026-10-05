using System.Xml.Linq;
using Books.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Books.Tests;

/// <summary>
/// Критерий приёмки №3 ТЗ: таблица соответствия «ошибка БД → OperationCode»
/// зафиксирована в EfErrorMapper и покрыта тестами (п. 3.4.1).
/// Реальные SqlException не конструируются из кода (нет публичного ctor), поэтому
/// проверка идёт по двум слоям: чистый regex-маппинг текстов ошибок SQL Server +
/// интеграция с DbUpdateException на фейковом внутреннем исключении.
/// </summary>
public class EfErrorMapperTests
{
    // ===== Regex-слой: тексты реальных ошибок SQL Server =====

    [Theory]
    [InlineData("Violation of UNIQUE KEY constraint 'UX_TblBooks_Isbn'. Cannot insert duplicate key in object 'dbo.tblBooks'. The duplicate key value is (978-5-...).")]
    [InlineData("Cannot insert duplicate key row in object 'dbo.tblBooks' with unique index 'UX_TblBooks_Isbn'. The duplicate key value is (978-1-2345-6789-7).")]
    [InlineData("Violation of PRIMARY KEY constraint 'PK_tblBooks'. Cannot insert duplicate key in object 'dbo.tblBooks'.")]
    public void DuplicateKeyMessages_Recognized(string message)
    {
        Assert.True(EfErrorMapper.IsDuplicateKeyMessage(message));
    }

    [Theory]
    [InlineData("The INSERT statement conflicted with the FOREIGN KEY constraint \"FK_tblBooks_Publisher\".")]
    [InlineData("Transaction (Process ID 52) was deadlocked on lock resources with another process.")]
    public void NonDuplicateMessages_NotRecognizedAsDuplicate(string message)
    {
        Assert.False(EfErrorMapper.IsDuplicateKeyMessage(message));
    }

    // ===== Интеграционный слой: DbUpdateException → внутренний exception =====

    private static DbUpdateException Wrap(Exception inner) =>
        new("SaveChanges failed", inner);

    [Fact]
    public void IsDuplicateKey_InnerMessageMatchesRegex_True()
    {
        var inner = new InvalidOperationException(
            "SqlException: Cannot insert duplicate key row in object 'dbo.tblBooks' with unique index 'UX_TblBooks_Isbn'.");
        Assert.True(EfErrorMapper.IsDuplicateKey(Wrap(inner)));
    }

    [Fact]
    public void IsDuplicateKey_NestedInner_True()
    {
        // EF иногда оборачивает ошибку в несколько уровней
        var deepest = new Exception("Violation of UNIQUE KEY constraint 'UX_TblBooks_Isbn'. Cannot insert duplicate key...");
        var middle = new InvalidOperationException("wrapper", deepest);
        Assert.True(EfErrorMapper.IsDuplicateKey(Wrap(middle)));
    }

    [Fact]
    public void IsForeignKeyViolation_Number547_True()
    {
        var sqlEx = CreateSqlException(547,
            "The DELETE statement conflicted with the REFERENCE constraint \"FK_tblBookGenres\".");
        var ex = Wrap(sqlEx);
        Assert.True(EfErrorMapper.IsForeignKeyViolation(ex));
        Assert.False(EfErrorMapper.IsDuplicateKey(ex));
    }

    [Fact]
    public void OtherErrors_MapToNeither()
    {
        var sqlEx = CreateSqlException(1205,
            "Transaction (Process ID 52) was deadlocked on lock resources with another process and has been chosen as the deadlock victim.");
        var ex = Wrap(sqlEx);
        Assert.False(EfErrorMapper.IsDuplicateKey(ex));
        Assert.False(EfErrorMapper.IsForeignKeyViolation(ex));
    }

    [Fact]
    public void NonSqlInnerException_ReturnsFalse()
    {
        var ex = Wrap(new InvalidOperationException("not sql"));
        Assert.False(EfErrorMapper.IsDuplicateKey(ex));
        Assert.False(EfErrorMapper.IsForeignKeyViolation(ex));
    }

    /// <summary>
    /// SqlException в Microsoft.Data.SqlClient 7.x создаётся только приватным
    /// ctor (string, SqlErrorCollection, Exception, Guid); Number берётся из
    /// первого элемента errorCollection, поэтому коллекцию заполняем рефлексией.
    /// </summary>
    private static SqlException CreateSqlException(int number, string message)
    {
        var errorsType = typeof(SqlErrorCollection);
        var errorCtor = typeof(SqlError).GetConstructors(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .First(c =>
            {
                var ps = c.GetParameters();
                return ps.Length == 9 &&
                       ps[0].ParameterType == typeof(int) &&
                       ps[1].ParameterType == typeof(byte) &&
                       ps[2].ParameterType == typeof(byte) &&
                       ps[8].ParameterType == typeof(Exception);
            });
        var error = errorCtor.Invoke(new object[]
            { number, (byte)0, (byte)10, "server", message, "procedure", 1, 0, null });

        var collection = (SqlErrorCollection)Activator.CreateInstance(errorsType, nonPublic: true)!;
        var addMethod = errorsType.GetMethod("Add",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        Assert.NotNull(addMethod);
        addMethod!.Invoke(collection, new[] { error });

        var ctorSql = typeof(SqlException).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid) },
            null);
        Assert.NotNull(ctorSql);
        var sqlEx = (SqlException)ctorSql!.Invoke(new object[] { message, collection, null, Guid.NewGuid() });
        Assert.Equal(number, sqlEx.Number); // sanity: номер ошибки действительно прокинулся
        return sqlEx;
    }
}
