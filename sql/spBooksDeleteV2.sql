/* ============================================================================
   spBooksDeleteV2.sql — обновление ХП dbo.spBooksDelete

   ПРОБЛЕМА: текущая версия ХП выполняет только
       DELETE FROM dbo.TblBooks WHERE Id = @Id;
   и при ошибке внешнего ключа (547) возвращает @ResultCode = 2.
   Приложение трактует 2 как «Книгу нельзя удалить: на неё ссылаются другие записи».
   Но по факту книгу блокируют ДЕЙСТВУЮЩИЕ связи из таблиц-связок
   dbo.TblBookGenres и dbo.TblBookTypes (FK_TblBookGenres_TblBooks,
   FK_TblBookTypes_TblBooks). Их нужно очищать автоматически, а не показывать ошибку.

   РЕШЕНИЕ: перед удалением книги удаляются все строки связок жанров и типов
   для этого BookId. Ошибку 547 (код 2) теперь могут вызывать только ссылки
   извне каталога (например, операционные таблицы) — это корректное поведение.

   КОДЫ РЕЗУЛЬТАТА (контракт ADR-002, enum OperationCode в приложении):
       0 = NotFound  — книга не найдена
       1 = Success   — связи очищены, книга удалена
       2 = Duplicate — удаление заблокировано внешними FK (ERROR_NUMBER 547)

   ВЫПОЛНИТЬ В SSMS ЦЕЛИКОМ (CREATE OR ALTER перезапишет старую ХП).
   Проверка после выполнения: EXEC sp_helptext 'dbo.spBooksDelete';
   (в тексте должен появиться DELETE из TblBookGenres/TblBookTypes).
   ============================================================================ */
USE [books];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[spBooksDelete]
    @Id         INT,
    @ResultCode TINYINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. Очищаем таблицы-связки, которые ссылаются на книгу по FK.
        DELETE FROM dbo.TblBookGenres WHERE BookId = @Id;
        DELETE FROM dbo.TblBookTypes  WHERE BookId = @Id;

        -- 2. Удаляем саму книгу.
        DELETE FROM dbo.tblBooks WHERE Id = @Id;

        SET @ResultCode = CASE WHEN @@ROWCOUNT = 0 THEN 0 ELSE 1 END;  -- 0 = NotFound, 1 = Success
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (547)
            SET @ResultCode = 2;  -- 2 = Duplicate: внешние FK за пределами связок
        ELSE
            THROW;
    END CATCH
END;
GO
