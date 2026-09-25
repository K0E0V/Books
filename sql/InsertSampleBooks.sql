-- ============================================================
-- Добавление тестовых записей о книгах (TblBooks + связи с
-- жанрами и типами). Выполнять на dbo в SSMS.
-- Перед повторным запуском можно очистить:
--   DELETE FROM dbo.TblBookGenres WHERE BookId IN (SELECT Id FROM dbo.tblBooks WHERE Author LIKE 'Тест%');
-- ============================================================

-- 1. Книги через ХП spBooksCreate (возвращает @NewId)
DECLARE @NewId INT, @ResultCode TINYINT;

EXEC dbo.spBooksCreate
    @Title = N'Мастер и Маргарита',
    @Author = N'Михаил Булгаков',
    @PublicationYear = 1967,
    @Isbn = N'978-5-17-123456-1',
    @Publisher = N'АСТ',
    @Description = N'Роман о визите дьявола в Москву.',
    @ContentsXml = NULL,
    @CountPages = 480,
    @NewId = @NewId OUTPUT,
    @ResultCode = @ResultCode OUTPUT;
SELECT @NewId AS BookId1;

EXEC dbo.spBooksCreate
    @Title = N'Преступление и наказание',
    @Author = N'Фёдор Достоевский',
    @PublicationYear = 1866,
    @Isbn = N'978-5-17-654321-2',
    @Publisher = N'Эксмо',
    @Description = N'Роман о студере Раскольникове.',
    @ContentsXml = NULL,
    @CountPages = 672,
    @NewId = @NewId OUTPUT,
    @ResultCode = @ResultCode OUTPUT;
SELECT @NewId AS BookId2;

EXEC dbo.spBooksCreate
    @Title = N'Тихий Дон',
    @Author = N'Михаил Шолохов',
    @PublicationYear = 1940,
    @Isbn = N'978-5-17-112233-4',
    @Publisher = N'АСТ',
    @Description = N'Роман-эпопея о донском казачестве.',
    @ContentsXml = NULL,
    @CountPages = 1208,
    @NewId = @NewId OUTPUT,
    @ResultCode = @ResultCode OUTPUT;
SELECT @NewId AS BookId3;
GO

-- 2. Привязка жанров и типов к последним трём книгам
--    (если справочники пусты — создадим базовые записи)
IF NOT EXISTS (SELECT 1 FROM dbo.TblGenres)
    INSERT INTO dbo.TblGenres (Name) VALUES (N'Роман'), (N'Классика'), (N'Исторический роман');
IF NOT EXISTS (SELECT 1 FROM dbo.TblTypes)
    INSERT INTO dbo.TblTypes (Name) VALUES (N'Художественная литература'), (N'Аудиокнига'), (N'Электронная книга');

;WITH last3 AS (
    SELECT TOP 3 Id FROM dbo.tblBooks ORDER BY Id DESC
), numbered AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn FROM last3
)
INSERT INTO dbo.TblBookGenres (BookId, GenreId)
SELECT n.Id, g.Id
FROM numbered n
CROSS JOIN dbo.TblGenres g
WHERE NOT EXISTS (SELECT 1 FROM dbo.TblBookGenres bg WHERE bg.BookId = n.Id AND bg.GenreId = g.Id);

;WITH last3 AS (
    SELECT TOP 3 Id FROM dbo.tblBooks ORDER BY Id DESC
)
INSERT INTO dbo.TblBookTypes (BookId, TypeId)
SELECT b.Id, t.Id
FROM last3 b
JOIN dbo.TblTypes t ON t.Name IN (N'Художественная литература', N'Электронная книга')
WHERE NOT EXISTS (SELECT 1 FROM dbo.TblBookTypes bt WHERE bt.BookId = b.Id AND bt.TypeId = t.Id);
GO

SELECT 'Готово: добавлено 3 книги со связями' AS Result;
