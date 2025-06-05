namespace Backend.DTOs
{
    // Data som sendes inn ved registrering
    public class RegisterRequest
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
    }


    // Data som sendes inn ved login
    public class LoginRequest
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
    }


    // Svar vi returnerer tilbake til frontend etter login
    public class AuthResponse
    {
        public string Token { get; set; } = null!;
        public DateTime Expiration { get; set; }
    }


}
