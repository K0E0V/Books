using Microsoft.EntityFrameworkCore;

namespace Books.Data;

/// <summary>
/// EF Core DbContext поверх существующей БД books (database-first).
/// Маппинг повторяет схему из sql/StructureBD без пересоздания таблиц
/// (docs/ORM_MIGRATION_TZ.md, п. 3.1).
/// </summary>
public class BookLibraryContext : DbContext
{
    public BookLibraryContext(DbContextOptions<BookLibraryContext> options)
        : base(options)
    {
    }

    public DbSet<BookEntity> Books => Set<BookEntity>();
    public DbSet<GenreEntity> Genres => Set<GenreEntity>();
    public DbSet<BookTypeEntity> BookTypes => Set<BookTypeEntity>();
    public DbSet<PublisherEntity> Publishers => Set<PublisherEntity>();
    public DbSet<BookGenreLink> BookGenres => Set<BookGenreLink>();
    public DbSet<BookTypeLink> BookTypeLinks => Set<BookTypeLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ===== dbo.tblBooks =====
        modelBuilder.Entity<BookEntity>(e =>
        {
            e.ToTable("tblBooks");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).UseIdentityColumn();

            e.Property(b => b.Title).HasMaxLength(300).IsRequired();
            e.Property(b => b.Author).HasMaxLength(250).IsRequired();
            e.Property(b => b.PublicationYear).IsRequired();
            e.Property(b => b.Isbn).HasMaxLength(20);
            e.Property(b => b.Publisher).HasMaxLength(250);
            e.Property(b => b.Description).HasMaxLength(4000);
            e.Property(b => b.CountPages).IsRequired();

            // Колонка xml: string-свойство + явный тип столбца (ТЗ п. 3.2, вариант A)
            e.Property(b => b.ContentsXmlRaw)
                .HasColumnName("ContentsXml")
                .HasColumnType("xml")
                .IsRequired();

            e.Property(b => b.CreatedAt).HasDefaultValueSql("sysutcdatetime()");
            e.Property(b => b.UpdatedAt).HasDefaultValueSql("sysutcdatetime()");

            // UX_TblBooks_Isbn — filtered unique index (WHERE Isbn IS NOT NULL)
            e.HasIndex(b => b.Isbn)
                .IsUnique()
                .HasDatabaseName("UX_TblBooks_Isbn")
                .HasFilter("[Isbn] IS NOT NULL");

            e.HasIndex(b => new { b.Title, b.Author })
                .HasDatabaseName("IX_Books_Title_Author");
        });

        // ===== dbo.TblGenres =====
        modelBuilder.Entity<GenreEntity>(e =>
        {
            e.ToTable("TblGenres");
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).UseIdentityColumn();
            e.Property(g => g.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(g => g.Name).IsUnique().HasDatabaseName("UX_TblGenres_Name");
        });

        // ===== dbo.TblTypes =====
        modelBuilder.Entity<BookTypeEntity>(e =>
        {
            e.ToTable("TblTypes");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).UseIdentityColumn();
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(t => t.Name).IsUnique().HasDatabaseName("UX_TblTypes_Name");
        });

        // ===== dbo.TblPublishers =====
        modelBuilder.Entity<PublisherEntity>(e =>
        {
            e.ToTable("TblPublishers");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).UseIdentityColumn();
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(p => p.Name).IsUnique().HasDatabaseName("UX_TblPublishers_Name");
        });

        // ===== dbo.TblBookGenres (M:N через явную join-сущность) =====
        modelBuilder.Entity<BookGenreLink>(e =>
        {
            e.ToTable("TblBookGenres");
            e.HasKey(x => new { x.BookId, x.GenreId });

            e.HasOne(x => x.Book)
                .WithMany(b => b.GenreLinks)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Genre)
                .WithMany(g => g.BookLinks)
                .HasForeignKey(x => x.GenreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== dbo.TblBookTypes (M:N через явную join-сущность) =====
        modelBuilder.Entity<BookTypeLink>(e =>
        {
            e.ToTable("TblBookTypes");
            e.HasKey(x => new { x.BookId, x.TypeId });

            e.HasOne(x => x.Book)
                .WithMany(b => b.TypeLinks)
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Type)
                .WithMany(t => t.BookLinks)
                .HasForeignKey(x => x.TypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
