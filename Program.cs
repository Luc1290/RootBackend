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
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    try
    {
        var uri = new Uri(databaseUrl);

        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo[1];
        var host = uri.Host;
        var port = uri.Port;
        var database = uri.AbsolutePath.TrimStart('/');

        // Utiliser un nom de base de données par défaut si vide
        if (string.IsNullOrEmpty(database))
        {
            database = "postgres"; // Base de données par défaut dans PostgreSQL
            Console.WriteLine($"⚠️ Nom de base de données manquant, utilisation de '{database}' par défaut");
        }

        var sslmode = uri.Query.Contains("sslmode=Disable") ? "Disable" : "Require";

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslmode};Trust Server Certificate=true";

        Console.WriteLine($"➡️ Connexion PostgreSQL: {connectionString}");

        builder.Services.AddDbContext<MemoryContext>(options =>
            options.UseNpgsql(connectionString));
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Erreur parsing DATABASE_URL: " + ex.Message);
    }
}


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
