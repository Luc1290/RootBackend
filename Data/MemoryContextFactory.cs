using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RootBackend.Utils;

namespace RootBackend.Data
{
    public class MemoryContextFactory : IDesignTimeDbContextFactory<MemoryContext>
    {
        public MemoryContext CreateDbContext(string[] args)
        {
            var connectionString = DbUtils.GetConnectionStringFromEnv();
            var optionsBuilder = new DbContextOptionsBuilder<MemoryContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new MemoryContext(optionsBuilder.Options);
        }
    }
}
