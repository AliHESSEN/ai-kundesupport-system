using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // <-- Ny using for Identity-støtte
using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    // Dette er databasen vår, og arver nå fra IdentityDbContext for å støtte brukerautentisering i tillegg til EF Core
    public class AppDbContext : IdentityDbContext<ApplicationUser> // <-- Endret basisklasse til IdentityDbContext
    {
        // Konstruktør som lar EF bruke konfigurasjon fra Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Dette representerer tabellen "SupportCases" i databasen
        public DbSet<SupportCase> SupportCases { get; set; } = null!;
    }
}
