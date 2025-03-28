using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using RootBackend.Data;

namespace RootBackend.Data
{
    public class MemoryContextFactory : IDesignTimeDbContextFactory<MemoryContext>
    {
        public MemoryContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<MemoryContext>();

            string? connectionString = null;

            // 1. Essayer d'abord DATABASE_URL (fourni par fly postgres attach)
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrEmpty(databaseUrl))
            {
                Console.WriteLine($"📊 Utilisation de DATABASE_URL pour la connexion PostgreSQL");

                try
                {
                    // Parse l'URL de connexion au format postgres://user:password@host:port/database
                    var uri = new Uri(databaseUrl);
                    var userInfo = uri.UserInfo.Split(':');
                    var username = userInfo[0];
                    var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
                    var host = uri.Host;
                    var port = uri.Port > 0 ? uri.Port : 5432;
                    var database = uri.AbsolutePath.TrimStart('/');

                    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Disable";
                    Console.WriteLine($"📊 URL convertie en chaîne de connexion Npgsql: Host={host}, DB={database}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erreur de conversion DATABASE_URL: {ex.Message}");
                    // Si la conversion échoue, on utilise les variables individuelles
                    databaseUrl = null;
                }
            }

            // 2. Si DATABASE_URL est invalide ou absent, construire à partir des variables individuelles
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
                Console.WriteLine($"📊 Connexion PostgreSQL via variables individuelles → Host={dbHost}, DB={dbName}, SSL={sslMode}, Timeout=30s");
            }

            return new MemoryContext(optionsBuilder.Options);
        }
    }
}