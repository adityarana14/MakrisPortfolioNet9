using System.Net.Http.Headers;
using System.Net.Http.Json;
using MakrisPortfolio.Client.Auth;

namespace MakrisPortfolio.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly ApiAuthenticationStateProvider _auth;

    public AuthService(HttpClient http, ApiAuthenticationStateProvider auth)
    {
        _http = http;
        _auth = auth;
    }

    public record AuthResponse(string Token);

    public async Task<bool> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
        if (!res.IsSuccessStatusCode) return false;

        var obj = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (obj is null || string.IsNullOrWhiteSpace(obj.Token)) return false;

        await _auth.SetTokenAsync(obj.Token);
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", obj.Token);
        return true;
    }

    public async Task<bool> RegisterAsync(string email, string password, string? displayName = null)
    {
        var res = await _http.PostAsJsonAsync("api/auth/register", new { email, password, displayName });
        if (!res.IsSuccessStatusCode) return false;

        var obj = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (obj is null || string.IsNullOrWhiteSpace(obj.Token)) return false;

        await _auth.SetTokenAsync(obj.Token);
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", obj.Token);
        return true;
    }

    public async Task LogoutAsync()
    {
        await _auth.ClearTokenAsync();
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<bool> TryRestoreAsync() => await _auth.TryRestoreAsync();

    public async Task<bool> RefreshAsync()
    {
        var res = await _http.GetAsync("api/auth/me");
        if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await _auth.ClearTokenAsync();
            return false;
        }
        if (!res.IsSuccessStatusCode) return false;

        var obj = await res.Content.ReadFromJsonAsync<AuthResponse>();
        if (obj is null || string.IsNullOrWhiteSpace(obj.Token)) return false;

        await _auth.SetTokenAsync(obj.Token);
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", obj.Token);
        return true;
    }
}