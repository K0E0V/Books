-- ============================================================
-- spBooksGetById V2: ПЕРВЫЙ набор должен включать CountPages,
-- иначе в режиме просмотра карточки книги «Количество страниц»
-- будет пустым (модель Book.CountPages не заполняется).
-- Выполнять на dbo в SSMS. Скрипт идемпотентен (CREATE OR ALTER).
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.spBooksGetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Набор 1: книга (включая CountPages)
    SELECT Id, Title, Author, PublicationYear,
           Isbn, Publisher, Description, CountPages,
           CreatedAt, UpdatedAt
    FROM dbo.tblBooks
    WHERE Id = @Id;

    -- Набор 2: главы/оглавление
    SELECT Id, BookId, ChapterNumber, Title, PageStart
    FROM dbo.tblChapters
    WHERE BookId = @Id
    ORDER BY ChapterNumber;
END;
GO
