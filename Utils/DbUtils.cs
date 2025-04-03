using Microsoft.Extensions.Configuration;
using System;

namespace RootBackend.Utils
{
    public static class DbUtils
    {
        public static string GetConnectionStringFromEnv(IConfiguration? configuration = null)
        {
            // 1️⃣ Préférence : appsettings.Development.json (mode local EF Core)
            if (configuration != null)
            {
                var configString = configuration.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrEmpty(configString))
                {
                    Console.WriteLine("📦 ConnectionString chargée depuis appsettings.Development.json");
                    return configString;
                }
            }

            // 2️⃣ Sinon, check si DATABASE_URL est défini (Fly.io style)
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrEmpty(databaseUrl))
            {
                Console.WriteLine("📊 Utilisation de DATABASE_URL pour la connexion PostgreSQL");

                try
                {
                    var uri = new Uri(databaseUrl);
                    var userInfo = uri.UserInfo.Split(':');
                    var username = userInfo[0];
                    var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
                    var host = uri.Host;
                    var port = uri.Port > 0 ? uri.Port : 5432;
                    var database = uri.AbsolutePath.TrimStart('/');

                    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Timeout=30;Command Timeout=30;";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erreur DATABASE_URL : {ex.Message}");
                }
            }

            // 3️⃣ Sinon, fallback sur les variables séparées (en prod Fly.io)
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
            var sslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Disable";

            if (string.IsNullOrEmpty(dbHost))
            {
                throw new InvalidOperationException("❌ DB_HOST est manquant. Impossible de construire la chaîne de connexion.");
            }

            if (string.IsNullOrEmpty(dbPassword))
            {
                Console.WriteLine("⚠️ ATTENTION: DB_PASSWORD non défini !");
            }

            Console.WriteLine($"📊 Connexion via variables individuelles : Host={dbHost}, DB={dbName}, SSL={sslMode}");

            return $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode={sslMode};Timeout=30;Command Timeout=30;";
        }
    }
}
