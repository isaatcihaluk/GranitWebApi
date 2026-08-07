namespace GranitWebApi.Models
{
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
    public class TokenRequest
    {
        public required string RefreshToken { get; set; }
    }
}
