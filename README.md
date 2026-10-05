# Домашняя библиотека (Books)

ASP.NET Core 8 (Razor Pages) + MS SQL Server `books`. Учебный проект 5.2.

## Технологический стек
- **Доступ к данным: EF Core 8** (`Data/EfBookRepository.cs`, `Data/BookLibraryContext.cs`) — основной режим, feature-flag `Data:UseEfRepository=true` в `appsettings.json`.
- Legacy Dapper+ХП (`Data/BookRepository.cs`) — режим отката (`UseEfRepository=false`); ХП в статусе deprecated.
- Главы книг хранятся в XML-колонке `dbo.tblBooks.ContentsXml` (таблица `tblChapters` запрещена ТЗ).
- Логирование: Serilog (`docs/LOGGING.md`). Схема БД: EF Migrations (`docs/MIGRATIONS.md`).

## Структура
```
Data/          IBookRepository, EfBookRepository (EF Core), BookRepository (Dapper-fallback),
               BookLibraryContext, ContentsXmlConverter, EfErrorMapper
Domain/        Isbn, PageRange, DomainRuleException
Models/        Book, Chapter, OperationCode, RefItem, AboutStats
Pages/Books/   Index (список+фильтры+пагинация), Create, Edit, Details, Delete, About
Migrations/    InitialBooksSchema
Books.Tests/   xUnit: конвертер XML + маппер ошибок (16 тестов) + чек-лист ручной верификации
docs/          ORM_MIGRATION_TZ.md (ТЗ миграции), MIGRATIONS.md, LOGGING.md, DB_SCRIPTS.archived.md
sql/           замороженные исторические скрипты и тексты ХП
```

## Запуск
```bash
dotnet restore && dotnet build
dotnet test Books.Tests/Books.Tests.csproj
dotnet run            # http://localhost:5000 → /Books/Index
```
Требуется SQL Server с БД `books` (connection string в `appsettings.json`). Для чистой БД:
`dotnet ef database update` применит миграцию `InitialBooksSchema`.

## Исходное учебное задание
1. По каждой книге хранить: название, автор, год издания, оглавление в виде XML-поля, прочие данные.
2. Создать хранимые процедуры для insert, update, delete, select.
3. Реализовать карточку, список, формы создания/редактирования/просмотра записей.
4. В карточке — HTML-редактор оглавления, сохранение содержимого в XML-поле.
5. Поиск по оглавлению, названию, автору.
