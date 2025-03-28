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

// 🔎 Log DATABASE_URL + valeurs par défaut
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "rootdb.flycast"; // ✅ PAR DÉFAUT = flycast
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var sslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Disable";

if (string.IsNullOrEmpty(dbPassword))
{
    Console.WriteLine("⚠️ DB_PASSWORD non défini !");
}

string connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode={sslMode};";
Console.WriteLine($"📊 Connexion PostgreSQL → Host={dbHost}, DB={dbName}");

builder.Services.AddDbContext<MemoryContext>(options =>
    options.UseNpgsql(connectionString));

// 🎯 Sentry (prod-ready)
builder.WebHost.UseSentry(o =>
{
    o.Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
    o.TracesSampleRate = 1.0;
    o.Environment = "production";
});

var app = builder.Build();

// 📦 Appliquer les migrations
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

// 🔁 Endpoint /api/chat (Claude)
app.MapPost("/api/chat", async (ChatRequest request, ClaudeService claudeService) =>
{
    var reply = await claudeService.GetCompletionAsync(request.Message);
    return Results.Json(new { reply });
});

app.Run();

record ChatRequest(string Message);
