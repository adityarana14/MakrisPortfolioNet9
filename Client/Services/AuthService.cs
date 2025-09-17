using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MakrisPortfolio.Client.Auth;

namespace MakrisPortfolio.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ApiAuthenticationStateProvider _authState;

    private record AuthResponse(string Token, string Email, string? DisplayName);
    private record ErrorResponse(string Title, string[] Errors);

    public AuthService(HttpClient http, ApiAuthenticationStateProvider authState)
    {
        _http = http;
        _authState = authState;
    }
    public async Task<bool> RefreshAsync()
    {
        // body can be null; server only needs the current Authorization header
        var res = await _http.PostAsync("api/auth/refresh", content: null);

        if (!res.IsSuccessStatusCode) return false;

        var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (data?.Token is { } token)
        {
            await _authState.SetTokenAsync(token);
            return true;
        }
        return false;
    }
    public async Task<(bool ok, string? message)> RegisterAsync(string email, string password, string? displayName = null)
    {
        var res = await _http.PostAsJsonAsync("api/auth/register", new { email, password, displayName });

        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
            if (data?.Token is { } token)
            {
                await _authState.SetTokenAsync(token);
            }
            return (true, null);
        }

        // Prefer server ErrorResponse
        try
        {
            var err = await res.Content.ReadFromJsonAsync<ErrorResponse>();
            if (err?.Errors?.Length > 0) return (false, string.Join("\n", err.Errors));
        }
        catch { /* ignore parse errors */ }

        if (res.StatusCode == HttpStatusCode.Conflict)
            return (false, "An account with this email already exists.");

        if (res.StatusCode == HttpStatusCode.BadRequest)
            return (false, "Registration failed. Please check your password requirements.");

        return (false, "Registration failed. Please try again.");
    }

    public async Task<(bool ok, string? message)> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync("api/auth/login", new { email, password });

        if (res.IsSuccessStatusCode)
        {
            var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
            if (data?.Token is { } token)
            {
                await _authState.SetTokenAsync(token);
                return (true, null);
            }
        }

        return (false, "Invalid email or password.");
    }

    public Task LogoutAsync() => _authState.ClearTokenAsync();
}