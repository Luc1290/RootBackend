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
                Console.WriteLine($"📊 Utilisation de DATABASE_URL pour la connexion PostgreSQL");

                // Convertir l'URL en chaîne de connexion Npgsql
                try
                {
                    var uri = new Uri(databaseUrl);
                    var userInfo = uri.UserInfo.Split(':');
                    var username = userInfo[0];
                    var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
                    var host = uri.Host;
                    var port = uri.Port > 0 ? uri.Port : 5432;
                    var database = uri.AbsolutePath.TrimStart('/');

                    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
                    Console.WriteLine($"📊 URL convertie en chaîne de connexion Npgsql: Host={host}, DB={database}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erreur de conversion DATABASE_URL: {ex.Message}");
                    // Fallback au format standard si la conversion échoue
                    connectionString = null;
                }
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