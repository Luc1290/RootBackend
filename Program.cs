using RootBackend.Data;
using RootBackend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var claudeApiKey = builder.Configuration["Claude:ApiKey"];

// 🔐 CORS pour ton frontend Railway
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://rootfrontend-production.up.railway.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<ClaudeService>();

// 🔎 Log DATABASE_URL
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine("➡️ DATABASE_URL: " + databaseUrl);

// ✅ PostgreSQL configuration dynamique
if (!string.IsNullOrEmpty(databaseUrl))
{
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

        Console.WriteLine($"➡️ Connexion PostgreSQL via Railway: {connectionString}");

        builder.Services.AddDbContext<MemoryContext>(options =>
            options.UseNpgsql(connectionString));
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Erreur parsing DATABASE_URL: " + ex.Message);
    }
}
else
{
    Console.WriteLine("⚠️ Pas de DATABASE_URL trouvé, fallback sur appsettings");
    builder.Services.AddDbContext<MemoryContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
}

var app = builder.Build();

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
