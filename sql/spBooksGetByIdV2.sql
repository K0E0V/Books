-- ============================================================
-- spBooksGetById V2 (исправленная, СООТВЕТСТВУЕТ ТЗ):
--   * Главы хранятся ТОЛЬКО в поле tblBooks.ContentsXml (тип xml).
--     Таблицы глав в БД НЕТ и не будет — обращения к ней запрещены.
--   * Первый набор должен включать CountPages и ContentsXml,
--     иначе в режиме просмотра карточки книги «Количество страниц»
--     будет пустым (модель Book.CountPages не заполняется).
-- Выполнять на dbo в SSMS. Скрипт идемпотентен (CREATE OR ALTER).
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.spBooksGetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Набор 1: книга (включая CountPages и ContentsXml)
    SELECT Id, Title, Author, PublicationYear,
           Isbn, Publisher, Description, CountPages,
           ContentsXml, CreatedAt, UpdatedAt
    FROM dbo.tblBooks
    WHERE Id = @Id;

    -- Набор 2: главы — извлекаются из XML-поля ContentsXml
    -- (структура /BookContents/Chapter с атрибутами
    --  number, title, startPage, endPage)
    SELECT
        T.c.value('@number', 'INT')              AS Number,
        T.c.value('@title', 'NVARCHAR(200)')     AS Title,
        T.c.value('@startPage', 'INT')           AS StartPage,
        T.c.value('@endPage', 'INT')             AS EndPage
    FROM dbo.tblBooks
    CROSS APPLY ContentsXml.nodes('/BookContents/Chapter') AS T(c)
    WHERE Id = @Id
      AND ContentsXml IS NOT NULL
    ORDER BY T.c.value('@number', 'INT');
END;
GO
