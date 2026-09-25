-- ============================================================
-- spBooksGetById V3 — актуальная версия по структуре БД (sql/StructureBD)
-- и ТЗ: ГЛАВЫ ХРАНЯТСЯ ТОЛЬКО В ПОЛЕ dbo.tblBooks.ContentsXml (тип xml).
-- Таблицы tblChapters НЕ СУЩЕСТВУЕТ И НЕ БУДЕТ — обращение к ней запрещено!
--
-- Отличия от текущей версии в БД:
--   1. Набор №2 (главы) читается из ContentsXml через .nodes(), а не из tblChapters;
--   2. Добавлен набор №3: жанры и типы книги (приложение вызывает ХП
--      в GetByIdWithChaptersAsync и затем догружает их отдельным запросом —
--      теперь может получить сразу здесь, но совместимость сохранена);
--   3. Тип @ContentsXml = XML (соответствует колонке).
--
-- Вызывается приложением: Books.Data.BookRepository.GetByIdWithChaptersAsync
-- Выполнять после RenamePageCountToCountPages.sql. Идемпотентен (CREATE OR ALTER).
-- ============================================================
USE [books];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[spBooksGetById]
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

    -- Набор 2: главы из поля ContentsXml (xml), БЕЗ таблицы tblChapters!
    -- Формат XML строго соответствует генератору приложения
    -- (BookRepository.GenerateXmlFromChapters):
    --   <BookContents><Chapter number="1" title="..." startPage="5" endPage="9"/></BookContents>
    SELECT
        c.n.value('@number',    'int')           AS Number,
        c.n.value('@title',     'nvarchar(300)') AS Title,
        c.n.value('@startPage', 'int')           AS StartPage,
        c.n.value('@endPage',   'int')           AS EndPage
    FROM dbo.tblBooks b
    CROSS APPLY b.ContentsXml.nodes('/BookContents/Chapter') AS c(n)
    WHERE b.Id = @Id
    ORDER BY c.n.value('@number', 'int');

    -- Набор 3: жанры и типы (справочники TblGenres/TblTypes, связи TblBookGenres/TblBookTypes)
    SELECT g.Name
    FROM dbo.TblBookGenres bg
    JOIN dbo.TblGenres g ON g.Id = bg.GenreId
    WHERE bg.BookId = @Id
    ORDER BY g.Name;

    SELECT t.Name
    FROM dbo.TblBookTypes bt
    JOIN dbo.TblTypes t ON t.Id = bt.TypeId
    WHERE bt.BookId = @Id
    ORDER BY t.Name;
END;
GO
