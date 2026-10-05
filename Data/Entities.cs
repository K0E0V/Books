namespace Books.Data;

// EF-сущности, зеркалирующие существующую схему БД books (database-first,
// без пересоздания таблиц). Отделены от презентационной модели Books.Models.Book,
// которая остаётся DTO для Razor Pages (см. docs/ORM_MIGRATION_TZ.md, п. 3.1).

/// <summary>Строка dbo.tblBooks.</summary>
public class BookEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public string? Description { get; set; }
    public int CountPages { get; set; }

    /// <summary>Колонка xml dbo.tblBooks.ContentsXml. Главы парсятся ContentsXmlConverter.</summary>
    public string? ContentsXmlRaw { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<BookGenreLink> GenreLinks { get; set; } = new List<BookGenreLink>();
    public ICollection<BookTypeLink> TypeLinks { get; set; } = new List<BookTypeLink>();
}

/// <summary>Строка dbo.TblGenres.</summary>
public class GenreEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<BookGenreLink> BookLinks { get; set; } = new List<BookGenreLink>();
}

/// <summary>Строка dbo.TblTypes.</summary>
public class BookTypeEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<BookTypeLink> BookLinks { get; set; } = new List<BookTypeLink>();
}

/// <summary>Строка dbo.TblPublishers.</summary>
public class PublisherEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Join-сущность dbo.TblBookGenres (M:N Book↔Genre), составной PK.</summary>
public class BookGenreLink
{
    public int BookId { get; set; }
    public int GenreId { get; set; }
    public BookEntity? Book { get; set; }
    public GenreEntity? Genre { get; set; }
}

/// <summary>Join-сущность dbo.TblBookTypes (M:N Book↔Type), составной PK.</summary>
public class BookTypeLink
{
    public int BookId { get; set; }
    public int TypeId { get; set; }
    public BookEntity? Book { get; set; }
    public BookTypeEntity? Type { get; set; }
}
