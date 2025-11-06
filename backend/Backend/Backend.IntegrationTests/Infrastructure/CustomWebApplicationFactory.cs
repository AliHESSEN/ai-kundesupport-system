using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath;

        public CustomWebApplicationFactory()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"TestDb_{Guid.NewGuid():N}.sqlite");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                // VIKTIG: bruk samme nøkkel/issuer/audience som appsettings.json
                var settings = new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = "DenneBørVæreMinst32TegnLangOgHemmelig123456789",
                    ["JwtSettings:Issuer"] = "KundesupportSystem",
                    ["JwtSettings:Audience"] = "KundesupportBrukere",

                    // SQLite testdatabase
                    ["ConnectionStrings:TestConnection"] = $"Data Source={_dbPath}"
                };

                config.AddInMemoryCollection(settings);
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={_dbPath}");
                });
            });
        }
    }
}
