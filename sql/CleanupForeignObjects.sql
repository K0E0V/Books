-- ============================================================
-- Удаление посторонних объектов из другой БД (по дампу sql/StructureBD):
--   схема oper.*  : procTransferMoney, fnGetOperationStatusCode,
--                   fnGetOperationStatusMessage, tblOperationStatus,
--                   tblOperationResultStatusMap
--   схема feature255
-- Приложение Books к этим объектам НЕ обращается.
-- ВНИМАНИЕ: необратимо. Перед выполнением сделайте резервную копию БД.
-- Скрипт идемпотентен: можно запускать повторно.
-- ============================================================
USE [books];
GO

SET NOCOUNT ON;

-- 1. Объекты схемы oper (сначала зависимые: ХП/ФУ, затем таблицы)
IF OBJECT_ID(N'oper.procTransferMoney', N'P') IS NOT NULL
    DROP PROCEDURE oper.procTransferMoney;
GO
IF OBJECT_ID(N'oper.fnGetOperationStatusCode', N'FN') IS NOT NULL OR OBJECT_ID(N'oper.fnGetOperationStatusCode', N'IF') IS NOT NULL
    DROP FUNCTION oper.fnGetOperationStatusCode;
GO
IF OBJECT_ID(N'oper.fnGetOperationStatusMessage', N'FN') IS NOT NULL OR OBJECT_ID(N'oper.fnGetOperationStatusMessage', N'IF') IS NOT NULL
    DROP FUNCTION oper.fnGetOperationStatusMessage;
GO
IF OBJECT_ID(N'oper.tblOperationResultStatusMap', N'U') IS NOT NULL
    DROP TABLE oper.tblOperationResultStatusMap;
GO
IF OBJECT_ID(N'oper.tblOperationStatus', N'U') IS NOT NULL
    DROP TABLE oper.tblOperationStatus;
GO

-- Удаляем сами схемы, если в них не осталось объектов
IF EXISTS (SELECT 1 FROM sys.schemas s WHERE s.name = N'oper'
               AND NOT EXISTS (SELECT 1 FROM sys.objects o WHERE o.schema_id = s.schema_id))
    EXEC(N'DROP SCHEMA oper');
GO

-- 2. Схема feature255 (объектов в дампе нет — удаляем саму схему)
IF EXISTS (SELECT 1 FROM sys.schemas s WHERE s.name = N'feature255'
               AND NOT EXISTS (SELECT 1 FROM sys.objects o WHERE o.schema_id = s.schema_id))
    EXEC(N'DROP SCHEMA feature255');
GO

-- 3. Проверка результата
SELECT name AS leftover_object, type_desc
FROM sys.objects
WHERE schema_name(schema_id) IN (N'oper', N'feature255');
-- Если строк нет — всё удалено.
GO
