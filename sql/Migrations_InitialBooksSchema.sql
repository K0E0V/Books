IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE TABLE [tblBooks] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(300) NOT NULL,
        [Author] nvarchar(250) NOT NULL,
        [PublicationYear] int NOT NULL,
        [Isbn] nvarchar(20) NULL,
        [Publisher] nvarchar(250) NULL,
        [Description] nvarchar(4000) NULL,
        [CountPages] int NOT NULL,
        [ContentsXml] xml NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (sysutcdatetime()),
        [UpdatedAt] datetime2 NOT NULL DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_tblBooks] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE TABLE [TblGenres] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_TblGenres] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE TABLE [TblPublishers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_TblPublishers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE TABLE [TblTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_TblTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE TABLE [TblBookGenres] (
        [BookId] int NOT NULL,
        [GenreId] int NOT NULL,
        CONSTRAINT [PK_TblBookGenres] PRIMARY KEY ([BookId], [GenreId]),
        CONSTRAINT [FK_TblBookGenres_TblGenres_GenreId] FOREIGN KEY ([GenreId]) REFERENCES [TblGenres] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TblBookGenres_tblBooks_BookId] FOREIGN KEY ([BookId]) REFERENCES [tblBooks] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE TABLE [TblBookTypes] (
        [BookId] int NOT NULL,
        [TypeId] int NOT NULL,
        CONSTRAINT [PK_TblBookTypes] PRIMARY KEY ([BookId], [TypeId]),
        CONSTRAINT [FK_TblBookTypes_TblTypes_TypeId] FOREIGN KEY ([TypeId]) REFERENCES [TblTypes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TblBookTypes_tblBooks_BookId] FOREIGN KEY ([BookId]) REFERENCES [tblBooks] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE INDEX [IX_TblBookGenres_GenreId] ON [TblBookGenres] ([GenreId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE INDEX [IX_Books_Title_Author] ON [tblBooks] ([Title], [Author]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_TblBooks_Isbn] ON [tblBooks] ([Isbn]) WHERE [Isbn] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE INDEX [IX_TblBookTypes_TypeId] ON [TblBookTypes] ([TypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_TblGenres_Name] ON [TblGenres] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_TblPublishers_Name] ON [TblPublishers] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_TblTypes_Name] ON [TblTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005074742_InitialBooksSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005074742_InitialBooksSchema', N'8.0.20');
END;
GO

COMMIT;
GO

