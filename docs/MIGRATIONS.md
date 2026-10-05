# Управление схемой БД: EF Core Migrations

С 2026-10 (ветка `feature/ef-core`, Этап 4 ТЗ `docs/ORM_MIGRATION_TZ.md`) схема БД **books**
версионируется миграциями EF Core в папке `Migrations/`. Ручные скрипты из `sql/` для изменений
схемы больше НЕ выполняются — этот документ заменяет `docs/DB_SCRIPTS.md`.

## Основные команды (выполнять из корня проекта)

```bash
# новая миграция после изменения моделей/контекста
dotnet ef migrations add ИмяМиграции

# проверить SQL, который будет выполнен (идемпотентность!)
dotnet ef migrations script --idempotent

# применить к БД (Development — через dotnet ef; Production — только через развёртывание)
dotnet ef database update

# состояние БД относительно миграций
SELECT * FROM dbo.__EFMigrationsHistory;
```

Connection string берётся из `appsettings.json` → `ConnectionStrings:DefaultConnection`
(см. `Data/DesignTimeDbContextFactory.cs`).

## Текущее состояние

| Миграция | Содержание |
|---|---|
| `20261005074742_InitialBooksSchema` | Полная зеркальная схема: tblBooks (колонка ContentsXml типа xml, filtered unique индекс UX_TblBooks_Isbn), TblGenres, TblTypes, TblPublishers, junction-таблицы TblBookGenres/TblBookTypes (составной PK, FK cascade), дефолты CreatedAt/UpdatedAt = sysutcdatetime() |

⚠️ Для существующей БД, созданной скриптами `sql/StructureBD`, миграцию применять НЕ нужно —
схема уже эквивалентна. Чтобы «привязать» её к истории миграций без пересоздания таблиц:

```bash
dotnet ef migrations script --idempotent   # сверить diff с реальной БД вручную
# затем вставить строку в __EFMigrationsHistory (или выполнить Up-скрипт на чистой БД)
```

## Правила

1. Любое изменение схемы — только новой миграцией; править опубликованные файлы миграций запрещено.
2. Перед мержем — `dotnet ef migrations script` и ревью SQL.
3. ХП (`spBooks*`) переведены в статус **deprecated**: не удалять минимум 1 релиз ( Transitional period, п. 7 ТЗ), не изменять их сигнатуры.
4. Главы хранятся ТОЛЬКО в `dbo.tblBooks.ContentsXml` (тип xml); таблица `tblChapters` запрещена ТЗ.
5. Папка `sql/` заморожена: остаётся read-only справочником исторических скриптов.

## Откат режима доступа к данным

Если EF-реализация показала дефект в продакшене: `appsettings` → `"Data": { "UseEfRepository": false }`
возвращает legacy Dapper+ХП репозиторий без пересборки (миграции схемы при этом остаются).
