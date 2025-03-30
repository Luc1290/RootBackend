using Microsoft.EntityFrameworkCore;
using RootBackend.Utils;
using Sentry;
using RootBackend.Explorer.ApiClients;
using RootBackend.Explorer.Services;
using RootBackend.Explorer.Skills;
using RootBackend.Services;
using RootBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// CORS
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

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<GroqService>();
builder.Services.AddHttpClient<GeocodingClient>();
builder.Services.AddHttpClient<OpenMeteoClient>();
builder.Services.AddScoped<WeatherExplorer>();
builder.Services.AddScoped<IRootSkill, WeatherSkill>();


// DB
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});
var connectionString = DbUtils.GetConnectionStringFromEnv();
builder.Services.AddDbContext<MemoryContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    });
});

// Sentry
var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN") ?? builder.Configuration["SENTRY_DSN"];
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

// Migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MemoryContext>();
    if (!app.Environment.IsDevelopment())
    {
        context.Database.Migrate();
    }

}

// Middleware
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

// Endpoints
app.MapPost("/api/chat", async (ChatRequest request, GroqService groqService) =>
{
    var reply = await groqService.GetCompletionAsync(request.Message);
    return Results.Json(new { reply });
});

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

record ChatRequest(string Message);
