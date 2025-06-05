using Backend.DTOs;  // Importerer DTO-klassene våre (data fra klient)
using Backend.Services;  // Importerer AuthService som vi laget
using Microsoft.AspNetCore.Mvc;  // ASP.NET Core sitt API-rammeverk

namespace Backend.Controllers
{
    [ApiController]  // Forteller ASP.NET at dette er en API-kontroller
    [Route("api/[controller]")]  // Setter URL-ruten til: /api/auth

    public class AuthController : ControllerBase  // Arver fra ControllerBase (minimal API-kontroller)
    {
        private readonly AuthService _authService;  // Oppretter en variabel for AuthService

        // Dependency Injection: får automatisk AuthService fra DI-containeren
        public AuthController(AuthService authService)
        {
            _authService = authService;  // Setter lokal variabel
        }


        // API-endepunkt for registrering av ny bruker
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // Kaller på AuthService for å opprette bruker
            var result = await _authService.RegisterAsync(request);

            // Hvis registreringen var vellykket
            if (result.Succeeded)
            {
                return Ok("Bruker registrert");  // Returnerer 200 OK
            }


            // Hvis registreringen feilet (f.eks. passord for svakt, bruker eksisterer)
            return BadRequest(result.Errors);  // Returnerer 400 BadRequest med feilmelding
        }



        // API-endepunkt for innlogging av bruker
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {

            // Kaller på AuthService for å logge inn bruker og lage token
            var response = await _authService.LoginAsync(request);


            // Hvis login feilet (feil brukernavn/passord)
            if (response == null)
            {
                return Unauthorized("Feil brukernavn eller passord");  // Returnerer 401 Unauthorized
            }


            // Hvis login lykkes, returnerer vi tokenet i JSON-format
            return Ok(response);


        }
    }
}
