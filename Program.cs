using Books.Data;

var builder = WebApplication.CreateBuilder(args);

// Регистрация репозитория
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddSingleton(connectionString);
builder.Services.AddScoped<BookRepository>();

// Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseExceptionHandler("/Error");

// Static files
app.UseStaticFiles();

// Routing
app.UseRouting();

app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

// Authorization & Authentication (если нужно)
app.UseAuthorization();

// Map pages
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Books/Index"));

app.Run();

