using MakrisPortfolio.Server.Data;
using MakrisPortfolio.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakrisPortfolio.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourcesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ResourcesController(ApplicationDbContext db) { _db = db; }

    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<ResourceDto>>> GetPublic()
    {
        var list = await _db.Resources.Where(r => !r.IsPremium)
            .Select(r => new ResourceDto(r.Id, r.Title, r.Url, r.IsPremium))
            .ToListAsync();
        return Ok(list);
    }

    [Authorize(Policy="PremiumPolicy")]
    [HttpGet("premium")]
    public async Task<ActionResult<IEnumerable<ResourceDto>>> GetPremium()
    {
        var list = await _db.Resources.Where(r => r.IsPremium)
            .Select(r => new ResourceDto(r.Id, r.Title, r.Url, r.IsPremium))
            .ToListAsync();
        return Ok(list);
    }
}
