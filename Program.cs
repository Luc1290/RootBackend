using Microsoft.EntityFrameworkCore;
using RootBackend.Utils;
using RootBackend.Services;
using RootBackend.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.CookiePolicy;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpsPolicy;
using System.Security.Claims;
using RootBackend.Core.IntentHandlers;
using RootBackend.Core;

var builder = WebApplication.CreateBuilder(args);

// 🔧 Proxy / HTTPS
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});

// Configurer correctement ForwardedHeaders pour fonctionner derrière un proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 🟢 CORS - Simplifié et plus cohérent
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://www.rootai.fr",
                "https://rootai.fr",
                "http://localhost:3000" // Pour le développement
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 🛠️ Services
builder.Services.AddControllers();

// Configuration de la politique de cookies consolidée
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    // SameSite=None est nécessaire pour les cookies cross-domain
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Services HTTP externes 
builder.Services.AddHttpClient<GroqHttpClient>();
builder.Services.AddScoped<GroqService>();
builder.Services.AddHttpClient<NlpService>((client) => {
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<WebScraperService>((client) => {
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Services internes 
builder.Services.AddScoped<WebScraperService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<PromptService>();

// Enregistrement des handlers d'intention
builder.Services.AddScoped<WebSearchIntentHandler>();
builder.Services.AddScoped<IIntentHandler, WebSearchIntentHandler>();
builder.Services.AddScoped<ConversationIntentHandler>();
builder.Services.AddScoped<IIntentHandler, ConversationIntentHandler>();
builder.Services.AddScoped<CodeGenerationIntentHandler>();
builder.Services.AddScoped<IIntentHandler, CodeGenerationIntentHandler>();
builder.Services.AddScoped<ImageGenerationIntentHandler>();
builder.Services.AddScoped<IIntentHandler, ImageGenerationIntentHandler>();
// Ajouter d'autres handlers au besoin

// Enregistrer la factory après les handlers
builder.Services.AddScoped<IntentHandlerFactory>();
builder.Services.AddScoped<IntentRouter>();

// 📊 DB
var connectionString = DbUtils.GetConnectionStringFromEnv(builder.Configuration);
builder.Services.AddDbContext<MemoryContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30);
        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
    });
});

// Session - Configuration simplifiée
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true;
});

// 🔐 Auth - Configuration corrigée
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None; // Nécessaire pour l'auth cross-domain
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "RootAI.Auth";

    // Ne pas définir Cookie.Domain si ce n'est pas nécessaire
    // Cela peut causer des problèmes avec les subdomains
    options.Cookie.Domain = ".rootai.fr";

    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);

    // Gestion des redirections d'authentification pour les APIs
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        },
        OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
    };
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
            throw new InvalidOperationException("Google ClientId et ClientSecret doivent être fournis.");
        }

        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.CallbackPath = "/api/auth/google-callback";
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = true;

        // Simplification de la configuration des cookies de corrélation
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite = SameSiteMode.None; // Crucial pour l'OAuth cross-origin
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.IsEssential = true;

        // Configurer les événements OAuth
        options.Events = new OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                // Assurer que l'URL utilise HTTPS
                var redirectUri = new UriBuilder(context.RedirectUri)
                {
                    Scheme = "https"
                }.Uri.ToString();

                context.Response.Redirect(redirectUri);
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                Console.WriteLine($"Erreur OAuth: {context.Failure?.Message}");

                // Rediriger vers la page de login du frontend avec des informations d'erreur
                var errorMessage = context.Failure?.Message ?? "Erreur d'authentification";
                context.Response.Redirect("https://rootai.fr/login?error=" + Uri.EscapeDataString(errorMessage));
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });
}

var app = builder.Build();

// Middleware de débogage uniquement en développement
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/login") ||
            context.Request.Path.StartsWithSegments("/debug-cookies"))
        {
            Console.WriteLine($"[DEBUG] Request Path: {context.Request.Path}");
            Console.WriteLine("[DEBUG] Cookies avant traitement:");
            foreach (var cookie in context.Request.Cookies)
            {
                Console.WriteLine($"[DEBUG] Cookie: {cookie.Key}={cookie.Value}");
            }

            context.Response.OnStarting(() =>
            {
                Console.WriteLine("[DEBUG] Cookies après traitement:");
                foreach (var cookie in context.Response.Headers.Where(h => h.Key == "Set-Cookie"))
                {
                    Console.WriteLine($"[DEBUG] Set-Cookie: {cookie.Value}");
                }
                return Task.CompletedTask;
            });
        }

        await next();
    });
}

// ORDRE CORRECT DES MIDDLEWARES:

// 1. D'abord, ForwardedHeaders pour déterminer l'IP et le protocole réels
app.UseForwardedHeaders();

// 2. HTTPS Redirection simplifiée
app.UseHttpsRedirection();

// 3. Session avant Authentication
app.UseSession();

// 4. Routing avant CORS
app.UseRouting();

// 5. CORS avant Authentication
app.UseCors("AllowFrontend");

// 6. Authentication avant Authorization
app.UseAuthentication();
app.UseAuthorization();

// Configuration endpoint
app.MapControllers();

// Migrations DB uniquement en production
if (!app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<MemoryContext>();
        context.Database.Migrate();
    }
}

// Swagger uniquement en développement
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint de diagnostic
app.MapGet("/debug-cookies", (HttpContext context) => {
    var cookies = context.Request.Cookies;
    var cookieList = cookies.Select(c => $"{c.Key}: {c.Value}").ToList();

    var headers = context.Request.Headers
        .Select(h => $"{h.Key}: {string.Join(", ", h.Value.ToArray())}")
        .ToList();

    var userClaims = new List<object>();
    if (context.User?.Claims != null)
    {
        userClaims = context.User.Claims
            .Select(c => new { Type = c.Type, Value = c.Value })
            .ToList<object>();
    }

    return Results.Ok(new
    {
        Cookies = cookieList,
        Headers = headers,
        AuthenticatedUser = context.User?.Identity?.IsAuthenticated ?? false,
        UserName = context.User?.Identity?.Name,
        UserClaims = userClaims,
        RequestScheme = context.Request.Scheme,
        RequestHost = context.Request.Host.ToString(),
        RequestPath = context.Request.Path.ToString()
    });
});

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();