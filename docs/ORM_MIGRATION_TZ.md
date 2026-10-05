# ТЗ: перевод проекта «Домашняя библиотека» (Books) с Dapper + ХП на ORM (EF Core)

Версия документа: 1.0
Дата: 05.10.2026
Целевой проект: `Books.csproj` (ASP.NET Core 8, Razor Pages, MS SQL Server `books`)

---

## 1. Цель и обоснование выбора ORM

### 1.1. Текущее состояние
- Доступ к данным: **Dapper 2.1.86** + `Microsoft.Data.SqlClient`, вызов хранимых процедур (`spBooksCreate/Update/Delete/GetById/GetListV2/Search`), часть запросов — inline SQL в `Data/BookRepository.cs`.
- Главы книг хранятся **только** в XML-колонке `dbo.tblBooks.ContentsXml` (требование учебного ТЗ, таблица `tblChapters` запрещена).
- Справочники: `TblPublishers`, `TblGenres`, `TblTypes` + связи M:N `TblBookGenres`, `TblBookTypes`.
- Репозиторий возвращает доменные коды `OperationCode` из OUTPUT-параметров ХП; бизнес-валидация частично живёт внутри ХП.

### 1.2. Рекомендуемая ORM: **Entity Framework Core 8 (`Microsoft.EntityFrameworkCore.SqlServer`)**
Обоснование под данную платформу (.NET 8 + MS SQL Server + Razor Pages):
1. **Первое-class поддержка SQL Server**: тип `xml`, `datetime2`, filtered unique index (`UX_TblBooks_Isbn WHERE Isbn IS NOT NULL`), `FOR JSON`, компиляция LINQ → оптимизированный T-SQL.
2. **Миграции (migrations)** — версионирование схемы в коде, отказ от ручных скриптов в SSMS (сейчас порядок выполнения хранится в `docs/DB_SCRIPTS.md` — это риск).
3. **Change Tracker** — решает текущие проблемы репозитория: замена наборов жанров/типов через DELETE+INSERT одним `SaveChangesAsync()` в транзакции.
4. **Согласованность со стеком**: DI (`AddDbContext` с пулом соединений вместо `new SqlConnection` на каждый вызов), встроенная валидация через те же DataAnnotations, что уже в `Models/Book.cs`.
5. **Ниша проекта** — учебное CRUD-приложение небольшого объёма; тяжёлые сценарии (concurrency high-load, complex reporting) отсутствуют, поэтому простота EF Core важнее гибкости micro-ORM.

### 1.3. Альтернативы (почему не выбраны)
| ORM | Вердикт | Причина |
|---|---|---|
| **Dapper (текущий)** | Оставить как fallback для сложных запросов | Это micro-ORM; цель перехода — автоматический mapping, изменения графов, миграции |
| **LINQ to DB 5.x** | 2-е место | Отличная производительность и SQL-first подход, но нет миграций «из коробки», слабее экосистема, меньше материалов для обучения |
| **NHibernate 5** | Не рекомендуется | Тяжёлая конфигурация (XML/fluent mappings), устаревает относительно EF Core в новых проектах на .NET 8 |
| **ServiceStack.Ormlite** | Не рекомендуется | Платная после 3-го уровня поддержки, слабая работа с M:N и XML |
| **Micro-EMU / FreeSql и т.п.** | Не рассматриваются | Узкая экосистема, риски поддержки |

### 1.4. Гибридная стратегия (принято решением)
EF Core — основной способ доступа. Сложные запросы (полнотекстовый поиск по оглавлению, статистика) допускают `FromSqlRaw` / сырой SQL через тот же `DbContext`. ХП **не удаляются** до конца миграции — они остаются источником поведения `OperationCode` на Этапах 1–3.

---

## 2. Скоуп работ

### В области (In Scope)
- DbContext, entity-классы, маппинг на существующую БД `books` (database-first без пересоздания таблиц).
- Замена internals `BookRepository` на EF Core с сохранением публичного API (сигнатуры методов не меняются → страницы `Pages/Books/*` правятся минимально или не правятся вовсе).
- XML-колонка `ContentsXml`: сериализация/десериализация глав `List<Chapter>` ⇄ XML в слое репозитория.
- Миграции: `EnableMigrations` против существующей БД, последующее управление схемой только через миграции.
- Unit-тесты репозитория на InMemory/SQLite-провайдере (или Testcontainers для SQL Server — см. п. 7).

### Вне области (Out of Scope)
- Изменение UI Razor Pages, вёрстки, HTML-редактора оглавления.
- Переработка системы логирования (Serilog остаётся).
- Добавление авторизации/ролей.
- Удаление ХП из БД (до полного переключения и стабилизации).

---

## 3. Целевая архитектура

```
Program.cs
  └─ builder.Services.AddDbContextPool<BookLibraryContext>(o => o.UseSqlServer(cs))
Domain/                 (сущности, без зависимостей от EF)
  Book.cs, Chapter.cs (POCO), Genre.cs, BookType.cs, Publisher.cs,
  BookGenre.cs, BookTypeLink.cs
Infrastructure/Data/
  BookLibraryContext.cs      // DbContext
  EntityConfigurations/      // IEntityTypeConfiguration<T> по файлу
  EfBookRepository.cs        // новый репозиторий (реализует IBookRepository)
  IBookRepository.cs         // интерфейс, извлечённый из текущего BookRepository
Mapping/
  ContentsXmlConverter.cs    // List<Chapter> <-> xml (XDocument)
sql/ (замораживается, только read-only справочные скрипты)
Migrations/                  // создаются в проекте
```

### 3.1. Модель данных (маппинг на существующие таблицы)

| Класс | Таблица | Ключевые настройки |
|---|---|---|
| `Book` | `dbo.tblBooks` | `Id` ValueGeneratedOnAdd; `ContentsXml` → колонка `xml`, `[NotMapped] List<Chapter> Chapters`; `Title nvarchar(300)`, `Author nvarchar(250)`, `Isbn nvarchar(20)` c **filtered unique index** `HasFilter("[Isbn] IS NOT NULL")`; `CountPages int`; `CreatedAt/UpdatedAt datetime2(0)` с DEFAULT `sysutcdatetime()` и `.IsRowVersion()`-подобным обновлением в `SaveChanges` (см. п. 3.4) |
| `Genre` | `dbo.TblGenres` | `Name nvarchar(100)`, unique |
| `BookType` | `dbo.TblTypes` | `Name nvarchar(100)`, unique |
| `Publisher` | `dbo.TblPublishers` | `Name` |
| `BookGenre` | `dbo.TblBookGenres` | составной PK `(BookId, GenreId)`, FK cascade |
| `BookTypeLink` | `dbo.TblBookTypes` | составной PK `(BookId, TypeId)`, FK cascade |

Связь M:N `Book ↔ Genre` настраивается через явные join-сущности (`UsingEntity<Junction, ...>`), чтобы сохранить возможность точечного управления связями (эквивалент текущего `SaveBookExtrasAsync`).

### 3.2. Работа с ContentsXml (главный риск — принять явно)
Варианты:
- **A (рекомендуемый):** свойство `string? ContentsXmlRaw { get; set; }` маппится на колонку `xml`; `Chapters` — `[NotMapped]`; конвертер `ContentsXmlConverter.ToXml(List<Chapter>) / FromXml(string)` на базе `XDocument` (безопасное экранирование — заменяет ручной `SecurityElement.Escape`). Репозиторий вызывает конвертер при Create/Update и парсит XML при GetById/GetList.
- **B:** ValueComparer/HasConversion для `List<Chapter>` ⇄ string — удобнее, но сложнее отладка и потеря контроля над форматом XML (ТЗ требует конкретный формат `<BookContents><Chapter .../>`).

Принять **вариант A**. Формат XML сохраняется байт-в-байт совместимым (атрибуты `number/title/startPage/endPage`), чтобы старые ХП (`spBooksGetByIdV3` с `.nodes()`) продолжали работать в переходный период.

### 3.3. Замена вызовов ХП на LINQ — карта методов

| Текущий метод `BookRepository` | Реализация на EF Core | Примечание |
|---|---|---|
| `GetByIdWithChaptersAsync(int)` | `Books.Include(b => b.BookGenres).Include(b => b.BookTypes)...` + `ContentsXmlConverter.FromXml` | Убирается GridReader/«Connection already has an associated DataReader» (комментарий в коде) |
| `CreateAsync(Book, chapters)` | `context.Books.Add(book)` + `SaveChangesAsync()` → `NewId = book.Id`; `ResultCode` формируется в C# | См. п. 3.4 про контракт OperationCode |
| `UpdateAsync(Book, chapters)` | `var tracked = await Books.FindAsync(id)`; copy поля + `ContentsXml = ToXml(chapters)`; `SaveChanges` |Оптимистическая блокировка (optimistic lock): `UpdatedAt` concurrency token (п. 3.4) |
| `DeleteAsync(int)` | `ExecuteDeleteAsync(b => b.Id == id)`; `catch DbUpdateException` → `OperationCode.NotFound/ForeignKeyViolation` | Эквивалент обработки ошибки 547 из ХП |
| `GetAllAsync(...фильтры, сортировка, пагинация...)` | Один IQueryable: `Where(search/genre/type/year/author)` + `OrderBy` (whitelist sortBy!) + `Skip/Take`; `TotalCount` = `LongCountAsync` тем же фильтром | Полнотекст по оглавлению: `EF.Functions.Contains` требует FTS — допустимо оставить `FromSqlRaw(spBooksSearch)` на Этапе 2 |
| `SearchAsync(...)` | то же, что выше (метод помечен `[Obsolete]` — удалить на Этапе 4) | |
| `GetAuthorsAsync/GetYearsAsync` | `Books.Select(...).Distinct().OrderBy...ToListAsync()` | |
| `GetPublishersAsync/GetGenresAsync/GetBookTypesAsync` | `DbSet<Publisher/Genre/BookType>.OrderBy(x => x.Name)` | |
| `GetBookExtraNamesAsync` | `Include` или навигация `book.Genres.Select(g => g.Name)` | |
| `SaveBookExtrasAsync` | загрузка tracked-книги с junction-навигацией, diff набора связей, один `SaveChangesAsync()` (транзакция автоматически) | Значительно проще текущего DELETE+N×INSERT |
| `GetAboutStatsAsync` | 3 агрегата через `Any/Count/Sum` на `DbSet<Book>`; можно объединить в один запрос группировкой | Заменить `DateTime.Now` на `DateTime.UtcNow` (колонки заполняются `sysutcdatetime()`) — **баг, исправить в рамках миграции** |

### 3.4. Сохранение бизнес-контрактов (обязательно)
1. **OperationCode.** Текущие страницы зависят от возврата `(OperationCode, NewId)`. Правило: код операции теперь формируется в приложении по результатам try/catch + проверок:
   - `Success = 1`, `NotFound = 0` (0 строк затронуто), `ForeignKeyViolation` (SqlException 547 → `DbUpdateException`), `DuplicateIsbn` (2601/2627 на `UX_TblBooks_Isbn`).
   - Таблица соответствия «ошибка БД → OperationCode» зафиксировать в `EfErrorMapper` и покрыть тестами.
2. **Валидация, живущая в ХП** (диапазон года издания, обязательность полей, проверка ISBN через `Domain/Isbn.cs`, `PageRange`) — перенести в `Domain`-слой (метод `Book.Validate()`, вызываемый репозиторием перед SaveChanges) либо в `OnModelCreating`-конвенции + DataAnnotations. Перед началом миграции — аудит текстов всех ХП (`sql/StructureBD/dbo.spBooks*.sql`) и выписывание списка проверяемых инвариантов (Приложение А, заполнить на Этапе 0).
3. **Concurrency.** При переводе Update с ХП добавить `ConcurrencyToken` на `UpdatedAt` (или `RowVersion rowversion` — потребует ALTER-миграции, согласовать отдельно). Поведение при коллизии — вернуть `OperationCode.Conflict` (новый член enum, если отсутствует).
4. **CreatedAt/UpdatedAt.** Сейчас дефолты на стороне БД. В EF значения проставлять в `SaveChanges` через interceptor или override `DetectChanges` (add/update entries), чтобы модель сразу содержала корректные UTC-значения.

---

## 4. Этапы и план работ

| Этап | Содержание | Артефакты | Оценка |
|---|---|---|---|
| **0. Подготовка** | Заморозка фич-веток; бэкап БД; аудит инвариантов из ХП (Приложение А); фиксация текущего контракта `IBookRepository` | Чек-лист, branch `feature/ef-core` | 1 д. |
| **1. Инфраструктура** | Добавить пакеты (п. 5); `BookLibraryContext` + конфигурации; scaffold/mirror существующей схемы **без** пересоздания БД; `AddDbContextPool` в `Program.cs`; `IBookRepository` + DI | Код контекста, тест «подключение+загрузка списка» | 2 д. |
| **2. Чтение** | `EfBookRepository`: GetById, GetAll (фильтры/сортировка/пагинация), справочники, GetAboutStats (+fix UtcNow). Переключить Index/Details/About на новый репозиторий за feature-flag (`UseEfRepository=true` в appsettings) | Работающие страницы Read-пути | 3 д. |
| **3. Письмо** | Create/Update/Delete/SaveBookExtras на EF + ChangeTracker; `ContentsXmlConverter` + тесты round-trip; `EfErrorMapper` + сохранение OperationCode; перенос валидаций из ХП | Полный CRUD через EF | 4 д. |
| **4. Миграции и зачистка** | `dotnet ef migrations` от существующей БД (только недостающие индексы/constraints, сверить с `sql/StructureBD`); удаление Dapper-кода и `[Obsolete] SearchAsync`; обновление `docs/DB_SCRIPTS.md` → `docs/MIGRATIONS.md`; ХП перевести в статус deprecated (не удалять ≤ 1 релиза) | `Migrations/`, cleaned code | 2 д. |
| **5. Верификация** | Прогон сценариев (п. 6), нагрузочная проверка списков, сравнение планов запросов (SET STATISTICS) для ключевых выборок | Отчёт о тестировании | 2 д. |

Итого: ~14 человеко-дней, один разработчик.

---

## 5. Пакеты и конфигурация

```xml
<!-- Books.csproj: добавить -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<!-- Dapper: удалить на Этапе 4 -->
```

`Program.cs` (замена регистрации репозитория):
```csharp
builder.Services.AddDbContextPool<BookLibraryContext>(o =>
    o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));
builder.Services.AddScoped<IBookRepository, EfBookRepository>();
```

Замечания:
- `AddDbContextPool` несовместим с внедрением `IHttpContextAccessor` в контекст — у нас его нет, ок. Если понадобится per-request state — перейти на `AddDbContext`.
- `EnableRetryOnFailure` включает execution strategy; явные транзакции тогда только через `CreateExecutionStrategy` (учесть в тестах Этапа 3).

---

## 6. Критерии приёмки

1. Все страницы (`Index, Create, Edit, Details, Delete, About`) работают через EF Core при `UseEfRepository=true`; поведение, визуал и сообщения об ошибках идентичны текущим.
2. Возвраты `OperationCode` совпадают с эталонными сценариями: успех, несуществующий Id, нарушение FK, дубликат ISBN, невалидные данные (год, ISBN, диапазоны страниц).
3. Round-trip оглавления: создано N глав → сохранено → прочитано → XML побитово эквивалентен формату `<BookContents><Chapter number title startPage endPage/>`; спецсимволы (`<`, `&`, `"`, кириллица) корректны.
4. Поиск работает по названию, автору и **оглавлению** (либо через перенесённый в EF предикат `Contains` по расшированному полю, либо документированное исключение `FromSqlRaw(spBooksSearch)`).
5. Пагинация/сортировка/фильтры `GetAllAsync` дают те же наборы и `TotalCount`, что ХП `spBooksGetListV2` (сравнительный тест на контрольной выборке из `InsertSampleBooks.sql`).
6. Нет утечек соединений: все обращения идут через pooled `DbContext`; старый паттерн `using var connection = new SqlConnection(...)` полностью удалён.
7. Производительность: время отклика списка (1000 книг) не хуже текущей реализации ±15% (меряется Etape 5).
8. Тесты: ≥ 90% покрытие `EfBookRepository` и `ContentsXmlConverter`; CI-прогон `dotnet test`.
9. Схема БД управляется миграциями; повторный `database update` идемпотентен; `docs/DB_SCRIPTS.md` заменён/переименован.

---

## 7. Риски и меры

| Риск | Вероятность | Мера |
|---|---|---|
| Потеря скрытых валидаций из ХП | Высокая | Аудит Приложения А на Этапе 0; сравнительные интеграционные тесты «ХП vs EF» на одном датасете |
| Несовместимость формата XML с `.nodes()` старых ХП в переходный период | Средняя | Тест round-trip + прогон `spBooksGetByIdV3` против нового XML |
| `xml`-тип и EF: `HasColumnType("xml")` требует string-свойство; длинные XML > 2 ГБ — не наш случай | Низкая | Ограничить размер оглавления (например, 64 КБ) на уровне валидации |
| Performance деградация на Include-запросах (раздувание строк M:N) | Средняя | Использовать отдельный projection-запрос для жанров вместо двойного Include; проверить планы запросов |
| Полнотекстовый поиск по оглавлению недоступен в LINQ | Средняя | На Этапе 2 допустим `FromSqlRaw`; на Этапе 4 рассмотреть computed-колонку + индекс или FTS-миграцию |
| Execution strategy конфликт с ручными транзакциями | Низкая | Обёртка `CreateExecutionStrategy` в сервисах записи |
| Различие часовых поясов CreatedAt (Local vs UTC) | Уже существует | Единый `DateTime.UtcNow` + явная конвертация при выводе |

**Откат:** feature-flag `UseEfRepository=false` возвращает старый `DapperBookRepository` (на Этапах 2–3 оба класса сосуществуют; удаление Dapper — только на Этапе 4 после приёмки).

---

## 8. Тестовая среда
- Интеграционные тесты: **Testcontainers mssql** (образ `mcr.microsoft.com/mssql/server:2022-latest`) с накаткой миграций + seed из `sql/InsertSampleBooks.sql`. InMemory provider **не использовать** для проверки SQL-специфичного (xml-колонка, filtered index, ExecuteDelete — не поддерживаются in-memory).
- Юнит-тесты конвертера и маппинга ошибок — xUnit + FluentAssertions (добавить в test-проект `Books.Tests`, создать решение на 2 проекта).

## Приложение А (заполнить на Этапе 0)
Чек-лист инвариантов из текстов ХП: поле | правило | источник (имя ХП/строка) | куда перенесено (код/миграция) | тест.
