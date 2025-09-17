using System.Net.Http.Json;
using MakrisPortfolio.Shared.Models;

namespace MakrisPortfolio.Client.Services
{
    public class ResourceService
    {
        private readonly HttpClient _http;
        public ResourceService(HttpClient http) { _http = http; }

        public async Task<IEnumerable<ResourceDto>> GetPublicAsync()
        {
            var list = await _http.GetFromJsonAsync<IEnumerable<ResourceDto>>("api/resources/public");
            return list ?? Enumerable.Empty<ResourceDto>();
        }

        public async Task<IEnumerable<ResourceDto>> GetPremiumAsync()
        {
            var res = await _http.GetAsync("api/resources/premium");
            if (!res.IsSuccessStatusCode) return Enumerable.Empty<ResourceDto>();
            var list = await res.Content.ReadFromJsonAsync<IEnumerable<ResourceDto>>();
            return list ?? Enumerable.Empty<ResourceDto>();
        }
        public class PremiumRequestVm
        {
            public int Id { get; set; }
            public string? Email { get; set; }
            public string? Status { get; set; }
            public DateTime CreatedUtc { get; set; }
        }

        public async Task<string?> RequestPremiumAsync(string? notes = null)
        {
            var res = await _http.PostAsJsonAsync("api/purchase/request", notes);
            if (!res.IsSuccessStatusCode) return null;
            var obj = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return obj != null && obj.TryGetValue("status", out var s) ? s : null;
        }

        public async Task<string?> MyPremiumRequestStatusAsync()
        {
            var res = await _http.GetAsync("api/purchase/my-request");
            if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return "None"; // not logged in or token missing
            if (!res.IsSuccessStatusCode) return null;
            var obj = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return obj != null && obj.TryGetValue("status", out var s) ? s : null;
        }

        

// Admin
        public async Task<IEnumerable<PremiumRequestVm>> GetPendingRequestsAsync()
        {
            try
            {
                var res = await _http.GetAsync("api/purchase/requests?status=Pending");
                res.EnsureSuccessStatusCode();
                var list = await res.Content.ReadFromJsonAsync<IEnumerable<PremiumRequestVm>>();
                return list ?? Enumerable.Empty<PremiumRequestVm>();
            }
            catch
            {
                return Enumerable.Empty<PremiumRequestVm>();
            }
        }
        
        
        public async Task<bool> ApproveRequestAsync(int id)
        {
            var res = await _http.PostAsync($"api/purchase/approve/{id}", null);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> DenyRequestAsync(int id)
        {
            var res = await _http.PostAsync($"api/purchase/deny/{id}", null);
            return res.IsSuccessStatusCode;
        }
    }
}
public class PremiumRequestVm
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? Status { get; set; }       // "Pending" | "Approved" | "Denied"
    public DateTime CreatedUtc { get; set; }
}
