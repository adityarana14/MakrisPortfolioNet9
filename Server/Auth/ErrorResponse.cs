namespace MakrisPortfolio.Server.Auth
{
    /// <summary>Lightweight error payload for 4xx responses.</summary>
    public record ErrorResponse(string Error, string? Details = null);
}