using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public UsersController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.AgentProfile)
            .Select(u => new
            {
                userId = u.UserId,
                username = u.Username,
                email = u.Email,
                role = u.Role != null ? u.Role.RoleName : "Agent",
                isActive = u.IsActive,
                extension = u.AgentProfile != null ? u.AgentProfile.Extension : null,
                createdAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.Roles.ToListAsync();
        return Ok(roles);
    }
}
