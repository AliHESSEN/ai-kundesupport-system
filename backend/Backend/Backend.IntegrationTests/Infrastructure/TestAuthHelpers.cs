using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Backend.IntegrationTests.Infrastructure
{
    public static class TestAuthHelpers
    {
        public static async Task<(HttpClient Client, string UserId)> CreateClientWithUserRoleAsync(
            CustomWebApplicationFactory factory,
            string role)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var config = sp.GetRequiredService<IConfiguration>();

            // Sørg for at rollen finnes
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

            // Opprett testbruker
            var user = new ApplicationUser
            {
                UserName = $"user_{Guid.NewGuid():N}@test.local",
                Email = $"user_{Guid.NewGuid():N}@test.local",
                EmailConfirmed = true
            };
            var createRes = await userManager.CreateAsync(user, "P@ssw0rd!");
            if (!createRes.Succeeded)
                throw new Exception("Feil ved opprettelse av testbruker: " +
                                    string.Join(", ", createRes.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, role);

            // Bygg JWT-token
            var secret = config["JwtSettings:SecretKey"]!;
            var issuer = config["JwtSettings:Issuer"];
            var audience = config["JwtSettings:Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("sub", user.Id),
                new Claim("role", role)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            // Opprett klient med token i header
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            return (client, user.Id);
        }
    }
}
