using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MakrisPortfolio.Server.Data;

namespace MakrisPortfolio.Server.Controllers;
using Microsoft.EntityFrameworkCore;
using MakrisPortfolio.Server.Data.Entities;

[ApiController]
[Route("api/[controller]")]
public class PurchaseController : ControllerBase
{
    private readonly UserManager<AppUser> _userMgr;
    private readonly ApplicationDbContext _db;
    public PurchaseController(UserManager<AppUser> userMgr, ApplicationDbContext db)
    {
        _userMgr = userMgr;
        _db = db;
    }

    [Authorize]
    [HttpPost("demo")]
    public async Task<ActionResult> DemoPurchase()
    {
        var user = await _userMgr.GetUserAsync(User);
        if (user == null) return Unauthorized();
        var res = await _userMgr.AddToRoleAsync(user, "Premium");
        if (!res.Succeeded) return BadRequest(res.Errors);
        return Ok();
    }
    
    // --- Premium Request Flow (no payment) ---

// User requests premium (creates or returns latest request)
[Authorize]
[HttpPost("request")]
public async Task<ActionResult<object>> RequestPremium([FromBody] string? notes)
{
    var user = await _userMgr.GetUserAsync(User);
    if (user == null) return Unauthorized();

    // If user already has Premium, short-circuit
    if (await _userMgr.IsInRoleAsync(user, "Premium"))
        return Ok(new { status = "Approved" });

    // If there is an existing pending request, return it
    var existing = await _db.PremiumRequests
        .Where(r => r.UserId == user.Id)
        .OrderByDescending(r => r.Id)
        .FirstOrDefaultAsync();

    if (existing != null && existing.Status == PremiumRequestStatus.Pending)
        return Ok(new { status = "Pending" });

    var req = new PremiumRequest
    {
        UserId = user.Id,
        Email = user.Email!,
        CreatedUtc = DateTime.UtcNow,
        Status = PremiumRequestStatus.Pending,
        Notes = notes
    };
    _db.PremiumRequests.Add(req);
    await _db.SaveChangesAsync();
    return Ok(new { status = "Pending" });
}

// User can check own request status
[Authorize]
[HttpGet("my-request")]
public async Task<ActionResult<object>> MyRequest()
{
    var user = await _userMgr.GetUserAsync(User);
    if (user == null) return Unauthorized();

    // If already premium, report Approved
    if (await _userMgr.IsInRoleAsync(user, "Premium"))
        return Ok(new { status = "Approved" });

    var existing = await _db.PremiumRequests
        .Where(r => r.UserId == user.Id)
        .OrderByDescending(r => r.Id)
        .FirstOrDefaultAsync();

    if (existing == null) return Ok(new { status = "None" });
    return Ok(new { status = existing.Status.ToString() });
}

// Admin: list requests (optionally filter by status)
// Admin: list requests (optionally filter by status)
    [Authorize(Roles = "Admin")]
    [HttpGet("requests")]
    public async Task<ActionResult<IEnumerable<object>>> Requests([FromQuery] string? status = "Pending")
    {
        PremiumRequestStatus? filter = status?.ToLowerInvariant() switch
        {
            "pending" => PremiumRequestStatus.Pending,
            "approved" => PremiumRequestStatus.Approved,
            "denied"   => PremiumRequestStatus.Denied,
            _ => null
        };

        var query = _db.PremiumRequests.AsQueryable();
        if (filter.HasValue) query = query.Where(r => r.Status == filter.Value);

        var list = await query
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedUtc)
            .Select(r => new
            {
                Id = r.Id,
                Email = r.Email,
                Status = r.Status.ToString(),   // <— stringify enum so client can read it
                CreatedUtc = r.CreatedUtc
            })
            .ToListAsync();

        return Ok(list);
    }

// Admin: approve
[Authorize(Roles = "Admin")]
[HttpPost("approve/{id:int}")]
public async Task<ActionResult> Approve(int id)
{
    var req = await _db.PremiumRequests.FindAsync(id);
    if (req == null) return NotFound();

    var user = await _userMgr.FindByIdAsync(req.UserId);
    if (user == null) return NotFound("User missing");

    if (req.Status != PremiumRequestStatus.Approved)
    {
        req.Status = PremiumRequestStatus.Approved;
        req.ReviewedUtc = DateTime.UtcNow;
        req.ReviewedByUserId = User?.Identity?.Name;
        await _db.SaveChangesAsync();
    }

    // Grant role
    if (!await _userMgr.IsInRoleAsync(user, "Premium"))
        await _userMgr.AddToRoleAsync(user, "Premium");

    return Ok();
}

// Admin: deny
[Authorize(Roles = "Admin")]
[HttpPost("deny/{id:int}")]
public async Task<ActionResult> Deny(int id)
{
    var req = await _db.PremiumRequests.FindAsync(id);
    if (req == null) return NotFound();

    if (req.Status != PremiumRequestStatus.Denied)
    {
        req.Status = PremiumRequestStatus.Denied;
        req.ReviewedUtc = DateTime.UtcNow;
        req.ReviewedByUserId = User?.Identity?.Name;
        await _db.SaveChangesAsync();
    }
    return Ok();
}
}
