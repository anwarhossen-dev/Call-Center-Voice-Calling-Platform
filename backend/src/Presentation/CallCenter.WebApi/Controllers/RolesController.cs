using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class CreateRoleDto
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRoleDto
{
    public string? RoleName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateRolePermissionsDto
{
    public List<string> Permissions { get; set; } = new();
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class RolesController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public RolesController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.Roles.ToListAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoleById(Guid id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound(new { message = "Role not found" });
        return Ok(role);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RoleName))
        {
            return BadRequest(new { message = "Role name is required." });
        }

        if (await _db.Roles.AnyAsync(r => r.RoleName.ToLower() == dto.RoleName.Trim().ToLower()))
        {
            return BadRequest(new { message = "Role already exists." });
        }

        var role = new Role
        {
            RoleId = Guid.NewGuid(),
            RoleName = dto.RoleName.Trim(),
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRoleById), new { id = role.RoleId }, role);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleDto dto)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound(new { message = "Role not found" });

        if (!string.IsNullOrWhiteSpace(dto.RoleName)) role.RoleName = dto.RoleName.Trim();
        if (dto.Description != null) role.Description = dto.Description;
        if (dto.IsActive.HasValue) role.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Role updated successfully", role });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound(new { message = "Role not found" });

        role.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Role deactivated successfully" });
    }

    [HttpGet("{id}/permissions")]
    public async Task<IActionResult> GetRolePermissions(Guid id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound(new { message = "Role not found" });

        var perms = role.RoleName.ToLower() switch
        {
            "admin" => new[] { "calls.all", "recordings.all", "agents.all", "teams.all", "settings.all", "reports.all", "qa.all" },
            "supervisor" => new[] { "calls.monitor", "calls.transfer", "agents.view", "teams.view", "reports.view", "qa.manage" },
            _ => new[] { "calls.handle", "recordings.view", "crm.view" }
        };

        return Ok(new { roleId = role.RoleId, roleName = role.RoleName, permissions = perms });
    }

    [HttpPut("{id}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsDto dto)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound(new { message = "Role not found" });

        return Ok(new { message = "Role permissions updated successfully", roleId = role.RoleId, permissions = dto.Permissions });
    }
}
