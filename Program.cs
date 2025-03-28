using RootBackend.Data;
using RootBackend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Sentry;

var builder = WebApplication.CreateBuilder(args);

// 🔐 CORS pour ton frontend Fly.io
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://www.rootai.fr", "https://rootfrontend.fly.dev", "http://localhost:61583")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<ClaudeService>();

// 🌍 Écoute sur 0.0.0.0:8080 pour Fly.io
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});

// 🔎 Log DATABASE_URL + fallback interne
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "rootdb.internal"; // ✅ DNS interne
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "your_default_password"; // Set a default or handle empty case
var sslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Require"; // Change to Require for consistency

if (string.IsNullOrEmpty(dbPassword))
{
    Console.WriteLine("⚠️ DB_PASSWORD non défini ! Utilisation d'un mot de passe par défaut");
}

string connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode={sslMode};Trust Server Certificate=true;";
Console.WriteLine($"📊 Connexion PostgreSQL → Host={dbHost}, DB={dbName}, SSL={sslMode}");

builder.Services.AddDbContext<MemoryContext>(options =>
    options.UseNpgsql(connectionString));

// 🎯 Sentry intégré pour prod
var sentryDsn = builder.Configuration["SENTRY_DSN"];
if (!string.IsNullOrEmpty(sentryDsn))
{
    SentrySdk.Init(o =>
    {
        o.Dsn = sentryDsn;
        o.Debug = true;
        o.TracesSampleRate = 1.0;
    });
}


var app = builder.Build();

// 📦 Migrations automatiques
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        Console.WriteLine("🔄 Migrations en cours...");
        var context = services.GetRequiredService<MemoryContext>();
        context.Database.Migrate();
        Console.WriteLine("✅ Migrations OK !");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERREUR MIGRATION : {ex.Message}");
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


// 🧪 Test de connexion DB
app.MapGet("/api/db-test", async (IServiceProvider serviceProvider) =>
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MemoryContext>();

    try
    {
        // Test si la connexion est possible
        bool canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            return Results.Ok(new { status = "success", message = "Connection à la base de données réussie!" });
        }
        else
        {
            return Results.BadRequest(new { status = "error", message = "Impossible de se connecter à la base de données" });
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new
        {
            status = "error",
            message = ex.Message,
            details = ex.ToString(),
            connectionString = connectionString.Replace(dbPassword, "***HIDDEN***") // Masquer le mot de passe
        });
    }
});

// 🔁 Endpoint chatbot (Claude)
app.MapPost("/api/chat", async (ChatRequest request, ClaudeService claudeService) =>
{
    var reply = await claudeService.GetCompletionAsync(request.Message);
    return Results.Json(new { reply });
});

app.Run();

record ChatRequest(string Message);
