using MakrisPortfolio.Server.Data;
using MakrisPortfolio.Server.Data.Entities;
using MakrisPortfolio.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakrisPortfolio.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles="Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<AppUser> _userMgr;
    public AdminController(ApplicationDbContext db, UserManager<AppUser> userMgr)
    { _db = db; _userMgr = userMgr; }

    [HttpPost("resource")]
    public async Task<ActionResult<ResourceDto>> CreateResource(CreateResourceRequest req)
    {
        var entity = new ResourceItem { Title = req.Title, Url = req.Url, IsPremium = req.IsPremium };
        _db.Resources.Add(entity);
        await _db.SaveChangesAsync();
        return new ResourceDto(entity.Id, entity.Title, entity.Url, entity.IsPremium);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<object>>> Users()
    {
        var users = await _userMgr.Users.Select(u => new { u.Email, u.DisplayName }).ToListAsync();
        return Ok(users);
    }

    [HttpPost("grant-premium/{userEmail}")]
    public async Task<ActionResult> GrantPremium(string userEmail)
    {
        var user = await _userMgr.FindByEmailAsync(userEmail);
        if (user == null) return NotFound();
        var res = await _userMgr.AddToRoleAsync(user, "Premium");
        if (!res.Succeeded) return BadRequest(res.Errors);
        return Ok();
    }
}
