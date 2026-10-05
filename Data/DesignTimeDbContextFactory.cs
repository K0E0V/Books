using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Books.Data;

/// <summary>
/// Фабрика DbContext для инструментов EF Core CLI (dotnet ef migrations/scripts)
/// без подъёма всего приложения. Строка подключения — из appsettings.json,
/// как в Program.cs.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BookLibraryContext>
{
    public BookLibraryContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=books;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<BookLibraryContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new BookLibraryContext(options);
    }
}
