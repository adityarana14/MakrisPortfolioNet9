using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MakrisPortfolio.Client;
using MakrisPortfolio.Client.Auth;
using MakrisPortfolio.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
    DefaultRequestHeaders = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
});

builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<ApiAuthenticationStateProvider>());

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ResourceService>();

var host = builder.Build();

var authProvider = host.Services.GetRequiredService<ApiAuthenticationStateProvider>();
await authProvider.TryRestoreAsync();

await host.RunAsync();