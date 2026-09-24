using Books.Data;
using Books.Infrastructure;
using Books.Validation;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .MinimumLevel.Information()
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