-- ============================================================
-- dbo.spBooksGetListV2 — список книг с фильтрами, сортировкой и пагинацией
-- Заменяет пару ХП spBooksGetList + spBooksSearch для страницы «Список книг».
-- Фильтры: поиск (название/автор/ISBN/оглавление), жанр, тип, год, автор.
-- Сортировка: title | author | year | pages | id, направление — @SortDesc.
-- Выполнять на dbo.
-- ============================================================

IF OBJECT_ID('dbo.spBooksGetListV2', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spBooksGetListV2;
GO

CREATE PROCEDURE dbo.spBooksGetListV2
    @PageNumber INT = 1,
    @PageSize   INT = 10,
    @Search     NVARCHAR(200) = NULL,   -- название / автор / ISBN / оглавление
    @GenreId    INT = NULL,             -- фильтр по жанру (TblGenres.Id)
    @TypeId     INT = NULL,             -- фильтр по типу (TblTypes.Id)
    @Year       SMALLINT = NULL,        -- фильтр по году издания
    @Author     NVARCHAR(200) = NULL,   -- точный фильтр по автору
    @SortBy     NVARCHAR(20) = NULL,    -- title | author | year | pages | id (NULL = по Id)
    @SortDesc   BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize < 1 OR @PageSize > 100 SET @PageSize = 10;

    -- Базовая выборка с применением фильтров
    ;WITH Filtered AS (
        SELECT
            b.Id, b.Title, b.Author, b.PublicationYear,
            b.Isbn, b.Publisher, b.Description, b.CountPages,
            b.CreatedAt, b.UpdatedAt
        FROM dbo.tblBooks b
        WHERE (@Search IS NULL OR @Search = ''
               OR b.Title    LIKE N'%' + @Search + N'%'
               OR b.Author   LIKE N'%' + @Search + N'%'
               OR b.Isbn     LIKE N'%' + @Search + N'%'
               OR TRY_CAST(b.ContentsXml AS NVARCHAR(MAX)) LIKE N'%' + @Search + N'%')
          AND (@GenreId IS NULL OR EXISTS (
                   SELECT 1 FROM dbo.TblBookGenres bg
                   WHERE bg.BookId = b.Id AND bg.GenreId = @GenreId))
          AND (@TypeId IS NULL OR EXISTS (
                   SELECT 1 FROM dbo.TblBookTypes bt
                   WHERE bt.BookId = b.Id AND bt.TypeId = @TypeId))
          AND (@Year IS NULL OR b.PublicationYear = @Year)
          AND (@Author IS NULL OR @Author = '' OR b.Author = @Author)
    )

    -- Первый набор: страница данных (сортировка — безопасный whitelist, без динамического SQL)
    SELECT Id, Title, Author, PublicationYear,
           Isbn, Publisher, Description, CountPages,
           CreatedAt, UpdatedAt
    FROM Filtered
    ORDER BY
        CASE WHEN @SortBy = 'title'  AND @SortDesc = 0 THEN Title END ASC,
        CASE WHEN @SortBy = 'title'  AND @SortDesc = 1 THEN Title END DESC,
        CASE WHEN @SortBy = 'author' AND @SortDesc = 0 THEN Author END ASC,
        CASE WHEN @SortBy = 'author' AND @SortDesc = 1 THEN Author END DESC,
        CASE WHEN @SortBy = 'year'   AND @SortDesc = 0 THEN CAST(PublicationYear AS INT) END ASC,
        CASE WHEN @SortBy = 'year'   AND @SortDesc = 1 THEN CAST(PublicationYear AS INT) END DESC,
        CASE WHEN @SortBy = 'pages'  AND @SortDesc = 0 THEN CountPages END ASC,
        CASE WHEN @SortBy = 'pages'  AND @SortDesc = 1 THEN CountPages END DESC,
        CASE WHEN (@SortBy IS NULL OR @SortBy = 'id' OR @SortBy = '') AND @SortDesc = 0 THEN Id END ASC,
        CASE WHEN (@SortBy IS NULL OR @SortBy = 'id' OR @SortBy = '') AND @SortDesc = 1 THEN Id END DESC,
        Id ASC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    -- Второй набор: общее количество отфильтрованных записей (для пагинации)
    SELECT COUNT(*) FROM Filtered;
END;
GO
