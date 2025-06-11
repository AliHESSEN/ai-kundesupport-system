namespace Backend.DTOs
{
    public class RegisterWithRoleRequest
    {
        public string? Email { get; set; } // bruker ? som gjør at de de kan ha verdien null, siden dette skal fylles fra HTTP-forespørsel
        public string? Password { get; set; } // samme her

        public string? Role { get; set; } // her også, de fylles fra HTTP-request
    }
}
