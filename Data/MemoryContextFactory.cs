using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Explorer.Utils;

namespace Explorer.Data
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
