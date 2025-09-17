using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace MakrisPortfolio.Client.Auth
{
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;
        private readonly HttpClient _http;
        private const string TokenKey = "authToken";

        private readonly AuthenticationState _anonymous =
            new(new ClaimsPrincipal(new ClaimsIdentity()));

        public ApiAuthenticationStateProvider(IJSRuntime js, HttpClient http)
        {
            _js = js;
            _http = http;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await GetTokenAsync();
            if (!IsProbablyJwt(token))
                return _anonymous;

            try
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var claims = ParseClaimsFromJwt(token!);
                var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
                return new AuthenticationState(user);
            }
            catch
            {
                await ClearTokenAsync();
                return _anonymous;
            }
        }

        public async Task SetTokenAsync(string token)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task<string?> GetTokenAsync()
            => await _js.InvokeAsync<string>("localStorage.getItem", TokenKey);

        public async Task ClearTokenAsync()
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            _http.DefaultRequestHeaders.Authorization = null;
            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
        }

        public async Task<bool> TryRestoreAsync()
        {
            var token = await GetTokenAsync();
            if (!IsProbablyJwt(token))
            {
                if (!string.IsNullOrWhiteSpace(token))
                    await ClearTokenAsync();
                return false;
            }

            try
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return true;
            }
            catch
            {
                await ClearTokenAsync();
                return false;
            }
        }

        private static bool IsProbablyJwt(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var parts = token.Split('.');
            return parts.Length == 3 && parts[0].Length > 5 && parts[1].Length > 5;
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var kv = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var claims = new List<Claim>();
            if (kv == null) return claims;

            foreach (var pair in kv)
            {
                if (pair.Key.Equals("role", StringComparison.OrdinalIgnoreCase))
                {
                    if (pair.Value is JsonElement e)
                    {
                        if (e.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in e.EnumerateArray())
                                claims.Add(new Claim(ClaimTypes.Role, item.ToString()));
                            continue;
                        }
                        claims.Add(new Claim(ClaimTypes.Role, e.ToString()));
                        continue;
                    }
                }

                if (pair.Value is JsonElement arr && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arr.EnumerateArray())
                        claims.Add(new Claim(pair.Key, item.ToString()));
                }
                else
                {
                    claims.Add(new Claim(pair.Key, pair.Value?.ToString() ?? ""));
                }
            }
            return claims;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}