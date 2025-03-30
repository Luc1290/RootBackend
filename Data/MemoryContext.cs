using Microsoft.EntityFrameworkCore;
using RootBackend.Models;
using RootBackend.Services;
using RootBackend.Data;
using RootBackend.Explorer.Skills;
using RootBackend.Utils;

namespace RootBackend.Data
{
    public class MemoryContext : DbContext
    {
        public MemoryContext(DbContextOptions<MemoryContext> options) : base(options) { }

        public DbSet<MessageLog> Messages { get; set; }
    }
}
