# Books.Tests — тесты перехода на EF Core (ТЗ docs/ORM_MIGRATION_TZ.md, п. 8)

## Что уже автоматизировано (`dotnet test`, без Docker и SQL Server)
| Файл | Покрывает критерий приёмки ТЗ (п. 6) |
|---|---|
| `ContentsXmlConverterTests.cs` | №2: round-trip оглавления ⇄ XML-колонка, формат совместим с `.nodes()` старых ХП |
| `EfErrorMapperTests.cs` | №3: таблица «ошибка БД → OperationCode» (2627/2601 → Duplicate, 547 → FK) |

## Что нужно проверить вручную на вашей машине (Этап 5 «Верификация»)
Автотесты выше не требуют подключения к БД. Остальные критерии приёмки проверяются
на реальном SQL Server — это ваш локальный `WIN-MJ28NJOK5GH` или Testcontainers.

### Вариант A — локальная SQL Server (быстрее)
1. **Бэкап**: в SSMS правая кнопка на базе `Books` → Tasks → Back Up… (обязательно!).
2. Из папки репозитория применить схему (если ещё не применена):
   ```powershell
   dotnet ef database update --project Books.csproj
   # либо скриптом: sql/Migrations_InitialBooksSchema.sql в SSMS
   ```
3. Seed-данные: выполнить `sql/InsertSampleBooks.sql`.
4. Запустить приложение: `dotnet run` (в `appsettings.json` стоит `"Data": { "UseEfRepository": true }`).
5. Пройти чек-лист UI:
   - [ ] Список книг: фильтры (жанр, тип, год, автор), сортировки, пагинация — совпадают с поведением до миграции;
   - [ ] Создание книги с оглавлением → открыть карточку: главы на месте (round-trip через колонку xml);
   - [ ] Дубликат ISBN при сохранении → сообщение об ошибке (OperationCode.Duplicate), а не падение;
   - [ ] Удаление книги; редактирование жанров/типов (M:N обновляется целиком);
   - [ ] Страница «О библиотеке»: статистика за последние сутки корректна (исправлен баг UTC);
   - [ ] Откат: поменять `"UseEfRepository": false`, перезапустить — всё работает через старый Dapper+ХП путь.

### Вариант B — Testcontainers (для CI, нужен Docker Desktop)
Интеграционный проект пока не создан (см. ТЗ п. 8). При необходимости добавляем:
`mcr.microsoft.com/mssql/server:2022-latest` + накатка миграций + seed из `InsertSampleBooks.sql`.

## Полезные команды
```powershell
dotnet test                       # юнит-тесты (без БД)
dotnet ef migrations list         # состояние схемы
```
