using RootBackend.Data;
using RootBackend.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

var claudeApiKey = builder.Configuration["Claude:ApiKey"];

// CORS : autoriser le front en prod
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

// Connexion PostgreSQL depuis Railway ou locale
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";

    builder.Services.AddDbContext<MemoryContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
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

// 👇 IMPORTANT : bien placer dans cet ordre
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();

// Route directe pour Claude
app.MapPost("/api/chat", async (ChatRequest request, ClaudeService claudeService) =>
{
    var reply = await claudeService.GetCompletionAsync(request.Message);
    return Results.Json(new { reply });
});

app.Run();

record ChatRequest(string Message);
