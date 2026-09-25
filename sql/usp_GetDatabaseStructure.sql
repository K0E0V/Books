-- ============================================================
-- Новый объект: dbo.usp_GetDatabaseStructure
-- Замена неиспользуемой usp_GenerateTableDDL (приложением не вызывается).
-- Возвращает актуальную структуру БД books (таблицы, колонки, ХП) —
-- удобно для сверки кода приложения с БД и обновления sql/StructureBD.
-- Идемпотентен (CREATE OR ALTER).
-- ============================================================
USE [books];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[usp_GetDatabaseStructure]
    @SchemaName NVARCHAR(128) = N'dbo'
AS
BEGIN
    SET NOCOUNT ON;

    -- Таблицы и колонки
    SELECT
        s.name            AS SchemaName,
        t.name            AS TableName,
        c.column_id       AS ColumnId,
        c.name            AS ColumnName,
        ty.name           AS DataType,
        c.max_length      AS MaxLength,
        c.precision       AS [Precision],
        c.scale           AS Scale,
        c.is_nullable     AS IsNullable
    FROM sys.tables t
    JOIN sys.schemas s   ON s.schema_id = t.schema_id
    JOIN sys.columns c   ON c.object_id = t.object_id
    JOIN sys.types ty    ON ty.user_type_id = c.user_type_id
    WHERE s.name = @SchemaName
    ORDER BY t.name, c.column_id;

    -- Хранимые процедуры
    SELECT
        s.name  AS SchemaName,
        p.name  AS ProcedureName,
        DEFINITION = OBJECT_DEFINITION(p.object_id)
    FROM sys.procedures p
    JOIN sys.schemas s ON s.schema_id = p.schema_id
    WHERE s.name = @SchemaName
    ORDER BY p.name;
END;
GO
