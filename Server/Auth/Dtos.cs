using System.ComponentModel.DataAnnotations;

namespace MakrisPortfolio.Server.Auth
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [StringLength(64)]
        public string? DisplayName { get; set; }
    }

    public record AuthResponse(string Token, string Email, string? DisplayName);
}