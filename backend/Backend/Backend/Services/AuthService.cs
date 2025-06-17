using Backend.DTOs;
using Backend.Models;
using Backend.Helpers;
using Microsoft.AspNetCore.Identity;

namespace Backend.Services
{
    // Authklasse for all autentisering
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager; // Identity sin brukerhåndtering
        private readonly JwtHelper _jwtHelper; // Vår egen helper for token-generering
        private readonly RoleManager<IdentityRole> _roleManager; // for rollebasert registrering


        // Dependency Injection
        public AuthService(UserManager<ApplicationUser> userManager,
                   RoleManager<IdentityRole> roleManager,JwtHelper jwtHelper)
        {
            _userManager = userManager;
            _jwtHelper = jwtHelper;
            _roleManager = roleManager;
        }

        // Metode for å registrere ny bruker
        public async Task<IdentityResult> RegisterAsync(RegisterRequest request)
        {
            var user = new ApplicationUser { UserName = request.UserName };
            return await _userManager.CreateAsync(user, request.Password);
        }


        // Metode for å logge inn bruker
        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);

            // Sjekker at brukeren eksisterer og at passordet er riktig
            if (user != null && await _userManager.CheckPasswordAsync(user, request.Password))
            {
                // Genererer JWT-token hvis alt er ok
                return await _jwtHelper.GenerateJwtToken(user);

            }

            // Returnerer null hvis login feiler
            return null;
        }


        public async Task<IdentityResult> RegisterWithRoleAsync(RegisterWithRoleRequest request)
        {
            // Sjekker at alle nødvendige data er fylt ut
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Email, passord og rolle må fylles ut"
                });
            }

            // Sjekker om rollen finnes i systemet
            var roleExists = await _roleManager.RoleExistsAsync(request.Role);
            if (!roleExists)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Description = $"Rollen '{request.Role}' finnes ikke"
                });
            }

            // Lager en ny bruker
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            // Oppretter brukeren med passord
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return result;
            }

            // Tildeler rollen til brukeren
            var roleResult = await _userManager.AddToRoleAsync(user, request.Role);

            // Kombinerer resultatene – hvis noe feilet med rolletildelingen
            if (!roleResult.Succeeded)
            {
                return IdentityResult.Failed(roleResult.Errors.ToArray());
            }

            return IdentityResult.Success;
        }




    }



}
