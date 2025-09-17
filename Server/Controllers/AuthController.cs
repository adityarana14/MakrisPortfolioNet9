using System.Security.Claims;
using MakrisPortfolio.Server.Auth;
using MakrisPortfolio.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MakrisPortfolio.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly JwtTokenService _jwt;
    private readonly ILogger<AuthController> _log;

    public AuthController(
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn,
        JwtTokenService jwt,
        ILogger<AuthController> log)
    {
        _users = users;
        _signIn = signIn;
        _jwt   = jwt;
        _log   = log;
    }

    // POST: /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse("InvalidInput", string.Join("; ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))));

        var existing = await _users.FindByEmailAsync(req.Email);
        if (existing is not null)
            return Conflict(new ErrorResponse("UserExists", "An account with this email already exists."));

        var user = new AppUser
        {
            UserName    = req.Email,
            Email       = req.Email,
            DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? null : req.DisplayName
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            var details = string.Join("; ", result.Errors.Select(e => e.Description));
            _log.LogWarning("Registration failed for {Email}: {Details}", req.Email, details);
            return BadRequest(new ErrorResponse("RegistrationFailed", details));
        }

        // Build token with current roles (likely none at first)
        var token = await IssueJwtAsync(user);
        return Ok(new AuthResponse(token, user.Email!, user.DisplayName));
    }

    // POST: /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse("InvalidInput", "Check email and password formatting."));

        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null)
            return Unauthorized(new ErrorResponse("InvalidCredentials"));

        var signIn = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!signIn.Succeeded)
            return Unauthorized(new ErrorResponse("InvalidCredentials"));

        var token = await IssueJwtAsync(user);
        return Ok(new AuthResponse(token, user.Email!, user.DisplayName));
    }

    // GET: /api/auth/me
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var email = User.FindFirstValue("email") ?? User.Identity?.Name ?? "";
        var display = User.FindFirstValue("displayName");
        return Ok(new { email, display, roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray() });
    }

    // POST: /api/auth/refresh
    [Authorize]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var token = await IssueJwtAsync(user);
        return Ok(new AuthResponse(token, user.Email!, user.DisplayName));
    }

    /// <summary>Create a JWT for a user with up-to-date roles and convenience claims.</summary>
    private async Task<string> IssueJwtAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name,          user.UserName ?? user.Email ?? user.Id),
            new("email",                  user.Email ?? string.Empty),
            new("displayName",            user.DisplayName ?? user.Email ?? string.Empty)
        };

        var roles = await _users.GetRolesAsync(user);
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (roles.Any(r => string.Equals(r, "Premium", StringComparison.OrdinalIgnoreCase)))
            claims.Add(new Claim("HasPremium", "true"));

        return _jwt.CreateToken(claims);
    }
}