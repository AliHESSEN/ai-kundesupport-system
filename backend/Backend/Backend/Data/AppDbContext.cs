using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    // Dette er databasen vår, og arver fra EF Core sin DbContext
    public class AppDbContext : DbContext
    {
        // Konstruktør som lar EF bruke konfigurasjon fra Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Dette representerer tabellen "SupportCases" i databasen
        public DbSet<SupportCase> SupportCases { get; set; } = null!;
    }
}
