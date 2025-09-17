namespace MakrisPortfolio.Shared.Models
{
    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string Email, string Password, string DisplayName);
    public record AuthResponse(string Token, string? DisplayName, string? Email, string[] Roles);
    public record ResourceDto(int Id, string Title, string Url, bool IsPremium);
    public record CreateResourceRequest(string Title, string Url, bool IsPremium);
}
