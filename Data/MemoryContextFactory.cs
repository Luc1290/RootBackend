using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using RootBackend.Data;

namespace RootBackend.Factory
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

            // Essayer d'abord DATABASE_URL (fourni par fly postgres attach)
            var databaseUrl = config["DATABASE_URL"];
            if (!string.IsNullOrEmpty(databaseUrl))
            {
                Console.WriteLine($"🏭 Factory: Utilisation de DATABASE_URL pour la connexion PostgreSQL");
                connectionString = databaseUrl;
            }
            // Sinon, construire à partir des variables individuelles
            else
            {
                var host = config["DB_HOST"] ?? "rootdb-new.internal"; // Mise à jour
                var db = config["DB_NAME"] ?? "postgres";
                var user = config["DB_USER"] ?? "postgres";
                var password = config["DB_PASSWORD"];
                var port = config["DB_PORT"] ?? "5432";
                var ssl = config["DB_SSL_MODE"] ?? "Require";

                if (string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("⚠️ Factory: DB_PASSWORD non défini!");
                }

                connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={password};SSL Mode={ssl};Trust Server Certificate=true;Timeout=30;Command Timeout=30;";
                Console.WriteLine($"🏭 Factory: Connexion PostgreSQL → Host={host}, DB={db}, SSL={ssl}, Timeout=30s");
            }

            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                // Augmenter les timeouts
                npgsqlOptions.CommandTimeout(30);

                // Configurer la stratégie de retry
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            return new MemoryContext(optionsBuilder.Options);
        }
    }
}