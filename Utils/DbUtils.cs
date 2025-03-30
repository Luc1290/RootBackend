using System;

namespace Explorer.Utils
{
    public static class DbUtils
    {
        public static string GetConnectionStringFromEnv()
        {
            string? connectionString = null;

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

                    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Disable;Timeout=30;Command Timeout=30;";
                    Console.WriteLine($"📊 URL convertie : Host={host}, DB={database}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erreur DATABASE_URL : {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(connectionString))
            {
                var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "rootdb-new.internal";
                var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
                var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
                var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
                var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
                var sslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Disable";

                if (string.IsNullOrEmpty(dbPassword))
                {
                    Console.WriteLine("⚠️ ATTENTION: DB_PASSWORD non défini!");
                }

                connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Disable;Timeout=30;Command Timeout=30;";
                Console.WriteLine($"📊 Connexion via variables individuelles : Host={dbHost}, DB={dbName}, SSL={sslMode}");
            }

            return connectionString ?? throw new InvalidOperationException("❌ Impossible de générer une chaîne de connexion PostgreSQL.");
        }
    }
}

