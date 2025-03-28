using RootBackend.Data;
using RootBackend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var claudeApiKey = builder.Configuration["Claude:ApiKey"];



// 🔐 CORS pour ton frontend Fly.io
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://www.rootai.fr", "https://rootfrontend.fly.dev", "http://localhost:61583")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Ajoutez cette ligne
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<ClaudeService>();

// 🔎 Log DATABASE_URL
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "rootdb.internal";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var sslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Disable";

if (string.IsNullOrEmpty(dbPassword))
{
    Console.WriteLine("⚠️ Attention: DB_PASSWORD n'est pas défini, la connexion risque d'échouer");
}

string connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode={sslMode};";
Console.WriteLine($"📊 Tentative de connexion PostgreSQL avec Host={dbHost} et Database={dbName}");

builder.Services.AddDbContext<MemoryContext>(options =>
    options.UseNpgsql(connectionString));

builder.WebHost.UseSentry(o =>
{
    o.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
    o.TracesSampleRate = 1.0;
    o.Debug = true; // à désactiver plus tard en prod
});


var app = builder.Build();

// Appliquer les migrations au démarrage
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        Console.WriteLine("🔄 Application des migrations de base de données...");
        var context = services.GetRequiredService<MemoryContext>();
        context.Database.Migrate();
        Console.WriteLine("✅ Migrations appliquées avec succès");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERREUR lors des migrations: {ex.Message}");
        // En environnement de développement, on pourrait vouloir re-throw l'exception
        // mais en production, mieux vaut logger et continuer
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

app.MapPost("/api/chat", async (ChatRequest request, ClaudeService claudeService) =>
{
    var reply = await claudeService.GetCompletionAsync(request.Message);
    return Results.Json(new { reply });
});

app.Run();

record ChatRequest(string Message);
