-- ============================================================
-- Переименование параметра @PageCount -> @CountPages
-- во всех хранимых процедурах, чтобы имя параметра ХП совпадало
-- с именем свойства модели Book.CountPages и колонки tblBooks.CountPages.
-- Код приложения (Dapper) передаёт параметры по именам — после этого
-- скрипта вызовы чистые: new { ..., book.CountPages, ... }.
-- Выполнять ПОСЛЕ обновления кода приложения.
-- ============================================================
USE [books];
GO

-- 1. spBooksCreate
CREATE OR ALTER PROCEDURE [dbo].[spBooksCreate]
    @Title NVARCHAR(300),
    @Author NVARCHAR(250),
    @PublicationYear INT,
    @Isbn NVARCHAR(20) = NULL,
    @Publisher NVARCHAR(250) = NULL,
    @Description NVARCHAR(4000) = NULL,
    @ContentsXml XML,
    @CountPages INT = 0,
    @NewId INT OUTPUT,
    @ResultCode TINYINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.tblBooks (Title, Author, PublicationYear, Isbn, Publisher,
            Description, ContentsXml, CountPages, CreatedAt, UpdatedAt)
        VALUES (@Title, @Author, @PublicationYear, @Isbn, @Publisher,
            @Description, @ContentsXml, ISNULL(@CountPages, 0), SYSUTCDATETIME(), SYSUTCDATETIME());
        SET @NewId = SCOPE_IDENTITY();
        SET @ResultCode = 1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2627, 2601) SET @ResultCode = 2;  -- дубликат ISBN
        ELSE THROW;
    END CATCH
END;
GO

-- 2. spBooksUpdate
CREATE OR ALTER PROCEDURE [dbo].[spBooksUpdate]
    @Id INT,
    @Title NVARCHAR(300),
    @Author NVARCHAR(250),
    @PublicationYear INT,
    @Isbn NVARCHAR(20) = NULL,
    @Publisher NVARCHAR(250) = NULL,
    @Description NVARCHAR(4000) = NULL,
    @ContentsXml XML,
    @CountPages INT = 0,
    @ResultCode TINYINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.tblBooks
        SET Title = @Title, Author = @Author, PublicationYear = @PublicationYear,
            Isbn = @Isbn, Publisher = @Publisher, Description = @Description,
            ContentsXml = @ContentsXml, CountPages = ISNULL(@CountPages, 0),
            UpdatedAt = SYSUTCDATETIME()
        WHERE Id = @Id;
        SET @ResultCode = CASE WHEN @@ROWCOUNT = 0 THEN 0 ELSE 1 END;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2627, 2601) SET @ResultCode = 2;
        ELSE THROW;
    END CATCH
END;
GO

PRINT 'spBooksCreate / spBooksUpdate: параметр @PageCount переименован в @CountPages, тип @ContentsXml = XML.';
GO
