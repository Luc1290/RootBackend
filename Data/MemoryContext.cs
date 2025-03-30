using Microsoft.EntityFrameworkCore;
using Explorer.Models;

namespace Explorer.Data
{
    public class MemoryContext : DbContext
    {
        public MemoryContext(DbContextOptions<MemoryContext> options) : base(options) { }

        public DbSet<MessageLog> Messages { get; set; }
    }
}
