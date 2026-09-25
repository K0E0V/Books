# Инструкция: модификация объектов БД (кратко, чтобы не забывать)

База данных: **books**. Все скрипты лежат в папке `sql/` проекта.
Выполнять в **SSMS** (подключиться к серверу → New Query → вставить скрипт → F5).

## ⚠️ Главные правила
1. **Сначала скрипт в БД, потом запуск новой сборки приложения.** Приложение вызывает ХП по имени с актуальными параметрами — если ХП старая, будет ошибка.
2. Перед изменениями — **бэкап**: `BACKUP DATABASE [books] TO DISK = N'C:\backup\books_pre.bak'`
3. После выполнения скрипта можно проверить текст ХП: `EXEC sp_helptext 'spBooksGetById';`
4. По ТЗ главы хранятся **ТОЛЬКО** в поле `dbo.tblBooks.ContentsXml` (тип xml). Таблицы `tblChapters` нет и не будет!

## 📋 Порядок выполнения скриптов (актуальный на 2026-09-25)

| № | Скрипт | Когда выполнять | Что делает |
|---|--------|-----------------|------------|
| 1 | `RenamePageCountToCountPages.sql` | ОДИН РАЗ при обновлении схемы | Переименовывает параметр `@PageCount` → `@CountPages` в `spBooksCreate`/`spBooksUpdate`, меняет тип `@ContentsXml` на `XML` |
| 2 | `spBooksGetByIdV3.sql` | ОДИН РАЗ при обновлении схемы | Пересоздаёт `spBooksGetById`: главы читаются из `ContentsXml` через `.nodes()`, БЕЗ `tblChapters`. Идемпотентен (CREATE OR ALTER) |
| 3 | `spBooksGetListV2.sql` | ОДИН РАЗ при обновлении схемы | Создаёт `spBooksGetListV2` — список книг с фильтрами (жанр/тип/год/автор), сортировкой и пагинацией |
| 4 | `InsertSampleBooks.sql` | По желанию | Добавляет тестовые книги, жанры, типы и связи. Не перезаписывает существующие данные |
| 5 | `CleanupForeignObjects.sql` | ОСТОРОЖНО, после бэкапа | Удаляет чужие объекты: схему `oper.*` (в т.ч. `procTransferMoney`), схему `feature255` |
| 6 | `usp_GetDatabaseStructure.sql` | По желанию | Создаёт процедуру выгрузки структуры БД (для папки `sql/StructureBD`) |

❌ Устарело, НЕ выполнять: `spBooksGetByIdV2.sql` (содержит обращение к tblChapters — заменён на V3), `UpdateDatabaseSchema.sql` (конфликтует с V3).

## 🔁 Быстрая шпаргалка
```text
Обновили код приложения → посмотрели таблицу выше → выполнили нужные скрипты №1–3 в SSMS → перезапустили приложение.
Сомневаетесь, какая версия ХП в БД → EXEC sp_helptext 'имя_хп';
```

## ✅ Проверка после выполнения
```sql
-- 1. ХП не содержит tblChapters:
SELECT name FROM sys.procedures p
CROSS APPLY sys.dm_sql_referenced_entities('dbo.'+p.name,'OBJECT') r
WHERE r.referenced_entity_name = 'tblChapters';   -- должно быть 0 строк

-- 2. Карточка книги работает: открыть /Books/Details/1
```
