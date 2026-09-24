-- ============================================================
-- Доработка БД по ТЗ «Расширение реквизитов книги»
-- Поле CountPages и таблицы-справочники УЖЕ существуют в схеме;
-- здесь — обновление ХП (CountPages) и опциональные индексы.
-- Выполнять на dbo.
-- ============================================================

-- 1. spBooksCreate: добавить @PageCount (в ТЗ имя @PageCount;
--    колонка фактически CountPages INT NOT NULL DEFAULT 0)
ALTER PROCEDURE dbo.spBooksCreate
    @Title NVARCHAR(200), @Author NVARCHAR(200), @PublicationYear SMALLINT,
    @Isbn NVARCHAR(20) = NULL, @Publisher NVARCHAR(100) = NULL,
    @Description NVARCHAR(MAX) = NULL, @ContentsXml NVARCHAR(MAX),
    @PageCount INT = 0,
    @NewId INT OUTPUT, @ResultCode TINYINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO dbo.tblBooks (Title, Author, PublicationYear, Isbn, Publisher,
            Description, ContentsXml, CountPages, CreatedAt, UpdatedAt)
        VALUES (@Title, @Author, @PublicationYear, @Isbn, @Publisher,
            @Description, @ContentsXml, ISNULL(@PageCount, 0), SYSUTCDATETIME(), SYSUTCDATETIME());
        SET @NewId = SCOPE_IDENTITY();
        SET @ResultCode = 1;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2627, 2601) SET @ResultCode = 2;  -- уникальность ISBN
        ELSE THROW;
    END CATCH
END;
GO

-- 2. spBooksUpdate: добавить @PageCount
ALTER PROCEDURE dbo.spBooksUpdate
    @Id INT, @Title NVARCHAR(200), @Author NVARCHAR(200), @PublicationYear SMALLINT,
    @Isbn NVARCHAR(20) = NULL, @Publisher NVARCHAR(100) = NULL,
    @Description NVARCHAR(MAX) = NULL, @ContentsXml NVARCHAR(MAX),
    @PageCount INT = 0,
    @ResultCode TINYINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        UPDATE dbo.tblBooks SET Title=@Title, Author=@Author, PublicationYear=@PublicationYear,
            Isbn=@Isbn, Publisher=@Publisher, Description=@Description,
            ContentsXml=@ContentsXml, CountPages=ISNULL(@PageCount, 0), UpdatedAt=SYSUTCDATETIME()
        WHERE Id = @Id;
        SET @ResultCode = CASE WHEN @@ROWCOUNT = 0 THEN 0 ELSE 1 END;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2627, 2601) SET @ResultCode = 2;
        ELSE THROW;
    END CATCH
END;
GO

-- 3. spBooksGetById: вернуть CountPages в первом наборе
ALTER PROCEDURE dbo.spBooksGetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id, Title, Author, PublicationYear,
        Isbn, Publisher, Description, CountPages,
        ContentsXml, CreatedAt, UpdatedAt
    FROM dbo.tblBooks
    WHERE Id = @Id;

    SELECT
        T.c.value('@number', 'INT') AS Number,
        T.c.value('@title', 'NVARCHAR(200)') AS Title,
        T.c.value('@startPage', 'INT') AS StartPage,
        T.c.value('@endPage', 'INT') AS EndPage
    FROM dbo.tblBooks
    CROSS APPLY ContentsXml.nodes('/BookContents/Chapter') AS T(c)
    WHERE Id = @Id
    ORDER BY T.c.value('@number', 'INT');
END;
GO

-- 4. Опционально: уникальные индексы по имени в справочниках
--    (защита от дублей при свободном вводе издательства).
--    Если данные уже содержат дубли — скрипт упадёт, сначала почистите дубли.
CREATE UNIQUE NONCLUSTERED INDEX UX_TblPublishers_Name ON dbo.TblPublishers (Name);
GO
CREATE UNIQUE NONCLUSTERED INDEX UX_TblGenres_Name ON dbo.TblGenres (Name);
GO
CREATE UNIQUE NONCLUSTERED INDEX UX_TblTypes_Name ON dbo.TblTypes (Name);
GO
