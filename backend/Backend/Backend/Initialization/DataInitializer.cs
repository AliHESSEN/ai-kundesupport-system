using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Initialization
{
    public static class DataInitializer
    {
        // Oppretter nødvendige roller hvis de ikke finnes (idempotent)
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            // Denne metoden oppretter nødvendige roller hvis de ikke finnes fra før
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Definerer rollene som skal opprettes
            string[] roleNames = { "Admin", "SupportStaff", "User" };

            // Går gjennom hver rolle og oppretter den hvis den ikke finnes
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName)
                    {
                        
                        NormalizedName = roleName.ToUpperInvariant()
                    });

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => $"{e.Code}:{e.Description}"));
                        throw new InvalidOperationException($"Klarte ikke å opprette rolle '{roleName}': {errors}");
                    }
                }
            }
        }
    }
}
