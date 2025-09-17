using System.Security.Claims;
using MakrisPortfolio.Server.Auth;
using MakrisPortfolio.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace MakrisPortfolio.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtTokenService _jwt;

    public AuthController(UserManager<AppUser> userManager,
                          SignInManager<AppUser> signInManager,
                          JwtTokenService jwt)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
    }

    public record LoginRequest(string Email, string Password);
    public record RegisterRequest(string Email, string Password, string? DisplayName);
    public record AuthResponse(string Token);

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null) return Unauthorized();

        var ok = await _userManager.CheckPasswordAsync(user, req.Password);
        if (!ok) return Unauthorized();

        return Ok(new AuthResponse(await IssueTokenAsync(user)));
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        var user = new AppUser
        {
            Email = req.Email,
            UserName = req.Email,
            DisplayName = req.DisplayName ?? req.Email
        };
        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        // default role - regular user
        if (!await _userManager.IsInRoleAsync(user, "User"))
            await _userManager.AddToRoleAsync(user, "User");

        return Ok(new AuthResponse(await IssueTokenAsync(user)));
    }

    // Returns a fresh token (used by client Refresh)
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponse>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        return Ok(new AuthResponse(await IssueTokenAsync(user)));
    }

    private async Task<string> IssueTokenAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? ""),
            new(ClaimTypes.Email, user.Email ?? "")
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var r in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, r)); // server-side [Authorize(Roles=...)]
            claims.Add(new Claim("role", r));          // client-side AuthorizeView
        }

        // optional convenience claim for Premium policy
        if (roles.Contains("Premium"))
            claims.Add(new Claim("HasPremium", "true"));

        return _jwt.CreateToken(claims, TimeSpan.FromHours(12));
    }
}