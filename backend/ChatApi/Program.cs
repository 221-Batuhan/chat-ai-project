using ChatApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Respect existing env (ASPNETCORE_URLS/PORT); otherwise choose a free port
var existingUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(existingUrls))
{
    builder.WebHost.UseUrls(existingUrls);
}
else if (!string.IsNullOrEmpty(portEnv))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
}
else
{
    // 0 lets Kestrel pick a free ephemeral port and avoids conflicts
    builder.WebHost.UseUrls("http://0.0.0.0:0");
}

// DB - SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=chat.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// HTTP Client for AI calls
builder.Services.AddHttpClient();

// Bind AI config
var aiServiceUrlFromConfig = builder.Configuration.GetSection("AI:ServiceUrl").Value;
builder.Services.AddSingleton(new AIConfig
{
    ServiceUrl = aiServiceUrlFromConfig
});

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (dev için gevşek, production'da kısıtla)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// run migrations on startup (development convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Console.WriteLine("Applying database migrations (if any)...");
    db.Database.Migrate();
    Console.WriteLine("Database is up to date.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Log effective URLs and AI endpoint
var aiConfig = app.Services.GetRequiredService<AIConfig>();
var resolvedAiUrl = Environment.GetEnvironmentVariable("AI_SERVICE_URL") ?? aiConfig.ServiceUrl ?? "<not set>";
Console.WriteLine($"ChatApi starting on: {string.Join(", ", app.Urls)}");
Console.WriteLine($"AI endpoint: {resolvedAiUrl}");

app.Run();

public class AIConfig
{
    public string? ServiceUrl { get; set; }
}
