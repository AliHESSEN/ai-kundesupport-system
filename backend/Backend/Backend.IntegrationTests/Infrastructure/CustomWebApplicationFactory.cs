namespace Backend.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Backend; // for Program

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["ConnectionStrings:TestConnection"] = "Data Source=TestDb.sqlite",
                ["JwtSettings:SecretKey"] = "TestSigningKey123!",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience"
            };
            config.AddInMemoryCollection(dict!);
        });

        // Ikke legg til AddDbContext her – Program.cs håndterer det.
    }
}
