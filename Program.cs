using Books.Data;
using Microsoft.EntityFrameworkCore;
using Books.Infrastructure;
using Books.Validation;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Переключатель режима логирования:
//   "Serilog:Mode" = "ErrorsOnly" — писать только ошибки (Error и Fatal)
//   "Serilog:Mode" = "All" (или любое другое значение) — писать весь лог
// Значение берётся из appsettings.json / appsettings.{Environment}.json,
// также можно задать через переменную окружения Serilog__Mode=ErrorsOnly.
var loggingMode = builder.Configuration["Serilog:Mode"];
var errorsOnly = string.Equals(loggingMode, "ErrorsOnly", StringComparison.OrdinalIgnoreCase);

builder.Host.UseSerilog((context, configuration) => configuration
    .MinimumLevel.Is(errorsOnly ? LogEventLevel.Error : LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

// Регистрация доступа к данным.
// Feature-flag "Data:UseEfRepository" (docs/ORM_MIGRATION_TZ.md, п. 7 «Откат»):
//   true  — EF Core (EfBookRepository + BookLibraryContext);
//   false — legacy Dapper+ХП (BookRepository), чтобы откатываться без деплоя кода.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var useEf = builder.Configuration.GetValue<bool>("Data:UseEfRepository");

if (useEf)
{
    builder.Services.AddDbContextPool<BookLibraryContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddScoped<IBookRepository, EfBookRepository>();
}
else
{
    builder.Services.AddSingleton(connectionString);
    builder.Services.AddScoped<BookRepository>();
    builder.Services.AddScoped<IBookRepository>(sp => sp.GetRequiredService<BookRepository>());
}

// Razor Pages
builder.Services.AddRazorPages();

// Валидация
builder.Services.AddSingleton<IBookEditValidator, BookEditValidator>();

// Обработчик исключений
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();  

var app = builder.Build();

app.UseExceptionHandler();

// Static files
app.UseStaticFiles();

// Routing
app.UseRouting();

// Коды состояния HTTP (404, 401 и т.д.)
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");


app.UseAuthorization();

// Map pages
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Books/Index"));

app.Run();