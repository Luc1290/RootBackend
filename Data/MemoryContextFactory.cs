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

            var host = config["DB_HOST"] ?? "rootdb.internal";
            var db = config["DB_NAME"] ?? "postgres";
            var user = config["DB_USER"] ?? "postgres";
            var password = config["DB_PASSWORD"] ?? "your_default_password"; // Same default as Program.cs
            var port = config["DB_PORT"] ?? "5432";
            var ssl = config["DB_SSL_MODE"] ?? "Require"; // Match Program.cs

            var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={password};SSL Mode={ssl};Trust Server Certificate=true";

            Console.WriteLine($"🏭 Factory: Connexion PostgreSQL → Host={host}, DB={db}, SSL={ssl}");

            optionsBuilder.UseNpgsql(connectionString);

            return new MemoryContext(optionsBuilder.Options);
        }
    }
}