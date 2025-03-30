using Microsoft.EntityFrameworkCore;
using RootBackend.Models;

namespace RootBackend.Data
{
    public class MemoryContext : DbContext
    {
        public MemoryContext(DbContextOptions<MemoryContext> options) : base(options) { }

        public DbSet<MessageLog> Messages { get; set; }
    }
}
