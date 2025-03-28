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

            var host = config["DB_HOST"];
            var db = config["DB_NAME"];
            var user = config["DB_USER"];
            var password = config["DB_PASSWORD"];
            var port = config["DB_PORT"] ?? "5432";
            var ssl = config["DB_SSL_MODE"] ?? "Require";

            var connectionString = $"Host={host};Port={port};Database={db};Username={user};Password={password};SSL Mode={ssl};Trust Server Certificate=true";

            optionsBuilder.UseNpgsql(connectionString);

            return new MemoryContext(optionsBuilder.Options);
        }
    }
}
