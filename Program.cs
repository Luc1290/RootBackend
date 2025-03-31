using Microsoft.EntityFrameworkCore;
using RootBackend.Utils;
using RootBackend.Explorer.ApiClients;
using RootBackend.Explorer.Services;
using RootBackend.Explorer.Skills;
using RootBackend.Services;
using RootBackend.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.CookiePolicy;
using System.Web;


var builder = WebApplication.CreateBuilder(args);

// 🔧 Proxy / HTTPS
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 🟢 CORS
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

// 🛠️ Services
builder.Services.AddControllers();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<GroqService>();
builder.Services.AddHttpClient<GeocodingClient>();
builder.Services.AddHttpClient<OpenMeteoClient>();
builder.Services.AddSingleton<GeocodingClient>();
builder.Services.AddSingleton<OpenMeteoClient>();
builder.Services.AddSingleton<WeatherExplorer>();
builder.Services.AddSingleton<WeatherSkill>();
builder.Services.AddSingleton<IRootSkill, WeatherSkill>();
builder.Services.AddSingleton<ConversationSkill>();
builder.Services.AddSingleton<IRootSkill, ConversationSkill>();
builder.Services.AddSingleton<GroqService>();
builder.Services.AddScoped<MessageService>();

// 📊 DB
var connectionString = DbUtils.GetConnectionStringFromEnv();
builder.Services.AddDbContext<MemoryContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    });
});

// 🔐 Auth
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.HttpOnly = true;
});

if (builder.Environment.IsProduction() ||
    (!string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"]) &&
     !string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientSecret"])))
{
    builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        var clientId = builder.Configuration["Authentication:Google:ClientId"];
        var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Google ClientId and ClientSecret must be provided.");
        }
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var uriBuilder = new UriBuilder(context.RedirectUri);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);

            query["access_type"] = "offline";
            query["prompt"] = "consent";

            uriBuilder.Query = query.ToString();
            context.Response.Redirect(uriBuilder.ToString());

            return Task.CompletedTask;
        };

        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.CallbackPath = "/api/auth/google-callback";

        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");


        if (builder.Environment.IsProduction())
        {
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.CorrelationCookie.SameSite = SameSiteMode.None;
            options.CorrelationCookie.HttpOnly = true;
        }
    });
}

var app = builder.Build();

// 🧪 Ajoute le header CORS manuellement si jamais ça bloque
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
    await next();
});


// 🔐 Appliquer les options de cookies SameSite
app.UseCookiePolicy();

// 🛠️ DB Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MemoryContext>();
    if (!app.Environment.IsDevelopment())
    {
        context.Database.Migrate();
    }
}

// 📜 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

record ChatRequest(string Message);
