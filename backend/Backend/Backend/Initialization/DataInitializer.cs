using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Initialization
{
    public static class DataInitializer
    {
        // Denne metoden oppretter nødvendige roller hvis de ikke finnes fra før
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            // Henter RoleManager fra DI (dependency injection)
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Definerer rollene som skal opprettes
            string[] roleNames = { "Admin", "SupportStaff", "User" };

            // Går gjennom hver rolle og oppretter den hvis den ikke finnes
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
    }
}
