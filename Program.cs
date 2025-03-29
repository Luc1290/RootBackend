using RootBackend.Data;
using RootBackend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Sentry;
using System.Net.NetworkInformation;

var builder = WebApplication.CreateBuilder(args);

// 🔐 CORS pour ton frontend Fly.io
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://www.rootai.fr", "https://rootai.fr", "https://rootfrontend.fly.dev", "http://localhost:61583")
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

string? connectionString = null;

// 1. Essayer d'abord DATABASE_URL (fourni par fly postgres attach)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine($"📊 Utilisation de DATABASE_URL pour la connexion PostgreSQL");

    try
    {
        // Parse l'URL de connexion au format postgres://user:password@host:port/database
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Disable;Timeout=30;Command Timeout=30;";
        Console.WriteLine($"📊 URL convertie en chaîne de connexion Npgsql: Host={host}, DB={database}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Erreur de conversion DATABASE_URL: {ex.Message}");
        // Si la conversion échoue, on utilise les variables individuelles
        databaseUrl = null;
    }
}

// 2. Si DATABASE_URL est invalide ou absent, construire à partir des variables individuelles
if (string.IsNullOrEmpty(connectionString))
{
    var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "rootdb-new.internal";
    var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
    var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
    var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
    var sslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Disable";

    if (string.IsNullOrEmpty(dbPassword))
    {
        Console.WriteLine("⚠️ ATTENTION: DB_PASSWORD non défini!");
    }

    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Disable;Timeout=30;Command Timeout=30;";
    Console.WriteLine($"📊 Connexion PostgreSQL via variables individuelles → Host={dbHost}, DB={dbName}, SSL={sslMode}, Timeout=30s");
}

// 3. Configuration plus robuste pour gérer les problèmes de connexion
builder.Services.AddDbContext<MemoryContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Augmenter les timeouts
        npgsqlOptions.CommandTimeout(30);

        // Configurer la stratégie de retry
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
});


var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN") ?? builder.Configuration["SENTRY_DSN"];
if (!string.IsNullOrEmpty(sentryDsn))
{
    try
    {
        Console.WriteLine($"📡 Initialisation de Sentry avec DSN: {sentryDsn.Substring(0, 10)}...");
        SentrySdk.Init(o =>
        {
            o.Dsn = sentryDsn;
            o.Debug = true;
            o.TracesSampleRate = 1.0;
        });
        Console.WriteLine("✅ Sentry initialisé avec succès");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Erreur lors de l'initialisation de Sentry: {ex.Message}");
    }
}
else
{
    Console.WriteLine("ℹ️ Pas de DSN Sentry configuré");
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
    app.UseHttpsRedirection(); // HTTPS Redirection uniquement en développement
}
else
{
    // En production, on n'utilise pas HTTPS Redirection car Fly.io gère le HTTPS
    Console.WriteLine("⚠️ Mode Production: HTTPS Redirection désactivée (gérée par Fly.io)");
}


app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// 🔁 Endpoint chatbot (Claude)
app.MapPost("/api/chat", async (ChatRequest request, ClaudeService claudeService) =>
{
    var reply = await claudeService.GetCompletionAsync(request.Message);
    return Results.Json(new { reply });
});

app.Run();

record ChatRequest(string Message);
