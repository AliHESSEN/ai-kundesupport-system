using Microsoft.IdentityModel.Tokens; // Brukes for å håndtere nøkler og signering
using System.IdentityModel.Tokens.Jwt; // Bibliotek for å lage JWT-tokens
using System.Security.Claims; // For claims (informasjon inni tokenet)
using System.Text; // For å konvertere hemmelig nøkkel til byte-array
using Backend.Models; // For ApplicationUser-modellen
using Backend.DTOs; // For AuthResponse-DTO

namespace Backend.Helpers
{
    // Klasse som lager JWT-token
    public class JwtHelper
    {
        private readonly IConfiguration _configuration; // For å hente inn appsettings

        // Konstruktør - får IConfiguration injisert
        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        // Metode som genererer JWT-token for brukeren
        public AuthResponse GenerateJwtToken(ApplicationUser user)
        {
            // Henter ut JWT-innstillinger fra appsettings.json
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!; // Hemmelig nøkkel (brukes til signering)
            var issuer = jwtSettings["Issuer"]; // Hvem som utsteder tokenet
            var audience = jwtSettings["Audience"]; // Hvem som kan bruke tokenet
            var expires = DateTime.UtcNow.AddHours(2);  // Når tokenet skal utløpe


            // Lager en liste med claims (data som pakkes inn i tokenet)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id), // Bruker-ID
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!) // Brukernavn
            };


            // Lager sikkerhetsnøkkelen fra hemmelig nøkkel (må konverteres til bytes)
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // Lager signaturen


            // Lager selve JWT-tokenet
            var token = new JwtSecurityToken(
                issuer: issuer, // hvem som utsteder
                audience: audience, // hvem som skal bruke tokenet
                claims: claims, // data inni tokenet
                expires: expires, // utløpstid
                signingCredentials: creds // signaturen
            );

            // Returnerer ferdig token i AuthResponse DTO
            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token), // Konverterer token til string
                Expiration = expires // Sender med utløpstid
            };
        }
    }
}
