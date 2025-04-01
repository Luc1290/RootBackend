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

// Session pour stocker des données temporaires
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
    options.Cookie.Name = "RootAI.Auth"; // Nom spécifique pour éviter les conflits

    // Augmenter la durée de vie du cookie
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);

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
        options.CallbackPath = "https://api.rootai.fr/api/auth/google-callback";

        // Ajouter des scopes explicites
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Sauvegarder les tokens pour référence future
        options.SaveTokens = true;

        // Configurer le cookie de corrélation spécifiquement pour Google
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite = SameSiteMode.None; // Crucial pour le flux OAuth cross-domain
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.MaxAge = TimeSpan.FromMinutes(15);
        options.CorrelationCookie.Name = "RootAI.GoogleOAuth.Correlation";

        // Configurer les événements OAuth pour forcer HTTPS et améliorer la gestion des erreurs
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
                context.Response.Redirect(redirectUri);
                return Task.CompletedTask;
            },
            OnRemoteFailure = context =>
            {
                Console.WriteLine($"Erreur OAuth: {context.Failure?.Message}");
                // Rediriger vers la page de login du frontend
                context.Response.Redirect("https://rootai.fr/login?error=" + Uri.EscapeDataString(context.Failure?.Message ?? "Erreur d'authentification"));
                context.HandleResponse();
                return Task.CompletedTask;
            },
            OnCreatingTicket = context =>
            {
                Console.WriteLine("Création du ticket d'authentification réussie");
                return Task.CompletedTask;
            },
            OnTicketReceived = context =>
            {
                Console.WriteLine("Ticket d'authentification reçu");
                // Vous pouvez ajouter du code ici pour inspecter le ticket
                return Task.CompletedTask;
            }
        };
    });
}

var app = builder.Build();

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

// Activer la session
app.UseSession();

// Appliquer les options de cookies SameSite
app.UseCookiePolicy();

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

// Ne pas utiliser HttpsRedirection car nous le gérons manuellement
// app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Ajouter un point de terminaison pour le débogage des cookies
app.MapGet("/debug-cookies", (HttpContext context) => {
    var cookies = context.Request.Cookies;
    var cookieList = cookies.Select(c => $"{c.Key}: {c.Value}").ToList();
    return Results.Ok(new
    {
        Cookies = cookieList,
        Headers = context.Request.Headers.Select(h => $"{h.Key}: {h.Value}").ToList(),
        AuthenticatedUser = context.User?.Identity?.IsAuthenticated ?? false,
        UserName = context.User?.Identity?.Name
    });
});

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

record ChatRequest(string Message);