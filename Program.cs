using RootBackend.Data;
using RootBackend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var claudeApiKey = builder.Configuration["Claude:ApiKey"];
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";


// 🔐 CORS pour ton frontend Fly.io
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://www.rootai.fr")
              .WithOrigins("https://rootfrontend.fly.dev")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });

    options.AddPolicy(MyAllowSpecificOrigins, builder =>
    {
        builder.WithOrigins("https://www.rootai.fr")
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

        var sslmode = uri.Query.Contains("sslmode=Disable") ? "Disable" : "Require";

        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslmode};Trust Server Certificate=true";

        Console.WriteLine($"➡️ Connexion PostgreSQL via Fly.io (parsed): {connectionString}");

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
app.UseCors(MyAllowSpecificOrigins);
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
