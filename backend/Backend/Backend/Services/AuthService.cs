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

        // Dependency Injection
        public AuthService(UserManager<ApplicationUser> userManager, JwtHelper jwtHelper)
        {
            _userManager = userManager;
            _jwtHelper = jwtHelper;
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
                return _jwtHelper.GenerateJwtToken(user);
            }

            // Returnerer null hvis login feiler
            return null;
        }
    }
}
