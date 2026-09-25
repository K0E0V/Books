using Books.Data;
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

// Регистрация репозитория
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddSingleton(connectionString);
builder.Services.AddScoped<BookRepository>();

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