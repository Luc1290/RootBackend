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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpsPolicy;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 🔧 Proxy / HTTPS
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configurer les options HTTPS
builder.Services.Configure<HttpsRedirectionOptions>(options =>
{
    options.HttpsPort = 443; // Port standard HTTPS
});

// 🟢 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "https://www.rootai.fr",
                "https://rootai.fr",
                "https://api.rootai.fr",
                "http://api.rootai.fr", // Ajouter la version HTTP pour les redirections
                "https://rootfrontend.fly.dev",
                "http://localhost:61583"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 🛠️ Services
builder.Services.AddControllers();

// Configuration de la politique de cookies améliorée
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
    options.CheckConsentNeeded = context => false; // Ne pas demander de consentement pour les cookies
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
builder.Services.AddSingleton<IntentionSkill>();
builder.Services.AddSingleton<IRootSkill, IntentionSkill>();
builder.Services.AddSingleton<SkillDispatcher>();
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

// Session pour stocker des données temporaires - Configuration améliorée
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Durée prolongée
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true; // Marquer comme essentiel
});

// 🔐 Auth - Configuration améliorée
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
    options.Cookie.Name = "RootAI.Auth";
    options.Cookie.Domain = ".rootai.fr";
    options.Cookie.IsEssential = true; // Marquer comme essentiel

    // Augmenter la durée de vie du cookie
    options.ExpireTimeSpan = TimeSpan.FromHours(1);

    // Configurer pour gérer les erreurs d'authentification
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
            throw new InvalidOperationException("Google ClientId and ClientSecret must be provided.");
        }

        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.CallbackPath = "/api/auth/google-callback";

        // Ajouter des scopes explicites
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Sauvegarder les tokens pour référence future
        options.SaveTokens = true;

        // Configuration améliorée du cookie de corrélation pour Google
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.Domain = ".rootai.fr";
        options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(30);
        options.CorrelationCookie.IsEssential = true; // Marquer comme essentiel
        options.CorrelationCookie.Name = "RootAI.GoogleOAuth.Correlation";
        options.CorrelationCookie.Path = "/";

        // Configurer les événements OAuth pour améliorer la gestion des erreurs
        options.Events = new OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                // Assurer que l'URL utilise HTTPS
                var redirectUri = new UriBuilder(context.RedirectUri)
                {
                    Scheme = "https"
                }.Uri.ToString();

                Console.WriteLine($"Redirection vers le point d'autorisation: {redirectUri}");

                // Ajouter un paramètre de cache-buster pour éviter les problèmes de cache
                var separator = redirectUri.Contains("?") ? "&" : "?";
                redirectUri = $"{redirectUri}{separator}t={DateTime.UtcNow.Ticks}";

                context.Response.Redirect(redirectUri);
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                Console.WriteLine($"Erreur OAuth: {context.Failure?.Message}");
                // Ajouter plus de détails dans les logs
                if (context.Failure != null)
                {
                    Console.WriteLine($"Exception détails: {context.Failure}");
                    Console.WriteLine($"Stack trace: {context.Failure.StackTrace}");
                }

                // Rediriger vers la page de login du frontend avec des informations d'erreur
                var errorMessage = context.Failure?.Message ?? "Erreur d'authentification";
                context.Response.Redirect("https://rootai.fr/login?error=" + Uri.EscapeDataString(errorMessage));
                context.HandleResponse();
                return Task.CompletedTask;
            },
            OnCreatingTicket = context =>
            {
                Console.WriteLine("Création du ticket d'authentification réussie");
                Console.WriteLine($"Identité de l'utilisateur: {context.Identity?.Name}");
                return Task.CompletedTask;
            },
            OnTicketReceived = context =>
            {
                Console.WriteLine("Ticket d'authentification reçu");

                // Évite les requêtes répétées
                if (context.Properties?.Items.ContainsKey(".redirect") == true)
                {
                    context.Properties.RedirectUri = "https://rootai.fr"; // redirection frontend après login
                }

                return Task.CompletedTask;
            }
        };
    });
}

var app = builder.Build();

// Middleware de débogage des cookies - AJOUT
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

        // Capture les Set-Cookie de la réponse
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

// Placer ForwardedHeaders AVANT tout le reste
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost
});

// Forcer le schéma HTTPS dans les URLs générées avec des exceptions pour les callbacks
app.Use(async (context, next) =>
{
    // Forcer HTTPS dans le schéma
    context.Request.Scheme = "https";

    // Si nous sommes déjà dans une requête de callback OAuth, ne pas rediriger
    if (context.Request.Path.StartsWithSegments("/api/auth/google-callback"))
    {
        await next();
        return;
    }

    // Si la requête arrive en HTTP, rediriger vers HTTPS
    if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) &&
        proto == "http")
    {
        var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(httpsUrl, permanent: true);
        return;
    }

    await next();
});

// Ajouter le header CORS manuellement 
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
    await next();
});

// Ajouter plus de détails d'erreur en développement
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}

// ORDRE CORRECT DES MIDDLEWARES
app.UseSession(); // Session avant CookiePolicy
//app.UseCookiePolicy(); // CookiePolicy avant Authentication

app.Use(async (context, next) => {
    Console.WriteLine($"Request Path: {context.Request.Path}, Host: {context.Request.Host}, Scheme: {context.Request.Scheme}");
    await next();
});

// DB Migration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MemoryContext>();
    if (!app.Environment.IsDevelopment())
    {
        context.Database.Migrate();
    }
}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ordre correct des middlewares
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication(); // Authentication avant Authorization
app.UseAuthorization();
app.MapControllers();

// Endpoint de débogage amélioré
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

record ChatRequest(string Message);