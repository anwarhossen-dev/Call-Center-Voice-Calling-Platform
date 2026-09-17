using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = "123456";
    public string? Phone { get; set; }
    public string Role { get; set; } = "Agent";
    public string? Extension { get; set; }
}

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class UserStatusDto
{
    public bool IsActive { get; set; }
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
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
                fullName = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.Username,
                username = u.Username,
                email = u.Email,
                phone = u.Phone,
                role = u.Role != null ? u.Role.RoleName : "Agent",
                isActive = u.IsActive,
                extension = u.AgentProfile != null ? u.AgentProfile.Extension : null,
                agentId = u.AgentProfile != null ? u.AgentProfile.AgentId : (Guid?)null,
                lastLoginAt = u.LastLoginAt,
                createdAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var u = await _db.Users
            .Include(x => x.Role)
            .Include(x => x.AgentProfile)
            .FirstOrDefaultAsync(x => x.UserId == id);

        if (u == null) return NotFound(new { message = "User not found" });

        return Ok(new
        {
            userId = u.UserId,
            fullName = u.FullName,
            username = u.Username,
            email = u.Email,
            phone = u.Phone,
            role = u.Role?.RoleName ?? "Agent",
            isActive = u.IsActive,
            extension = u.AgentProfile?.Extension,
            agentId = u.AgentProfile?.AgentId,
            lastLoginAt = u.LastLoginAt,
            createdAt = u.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new { message = "Username and Email are required." });
        }

        var normalizedUsername = dto.Username.Trim().ToLower();
        var normalizedEmail = dto.Email.Trim().ToLower();

        if (await _db.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername || u.Email.ToLower() == normalizedEmail))
        {
            return BadRequest(new { message = "A user with this username or email already exists." });
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == dto.Role.ToLower())
            ?? await _db.Roles.FirstAsync(r => r.RoleName == "Agent");

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FullName = !string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName : dto.Username,
            Username = normalizedUsername,
            Email = normalizedEmail,
            Phone = dto.Phone,
            PasswordHash = PasswordHelper.HashPassword(dto.Password),
            RoleId = role.RoleId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);

        if (role.RoleName.Equals("Agent", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(dto.Extension))
        {
            var agent = new Agent
            {
                AgentId = Guid.NewGuid(),
                UserId = user.UserId,
                Extension = !string.IsNullOrEmpty(dto.Extension) ? dto.Extension : "10" + new Random().Next(10, 99),
                DisplayName = user.FullName,
                Status = "Available",
                CurrentState = Domain.Enums.AgentState.Available,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Agents.Add(agent);
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, new { message = "User created successfully", userId = user.UserId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        var user = await _db.Users.Include(u => u.Role).Include(u => u.AgentProfile).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound(new { message = "User not found" });

        if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName;
        if (!string.IsNullOrWhiteSpace(dto.Email)) user.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Phone)) user.Phone = dto.Phone;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == dto.Role.ToLower());
            if (role != null) user.RoleId = role.RoleId;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "User updated successfully" });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UserStatusDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found" });

        user.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"User status updated to {(dto.IsActive ? "Active" : "Inactive")}", isActive = user.IsActive });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "User not found" });

        // Soft delete per ERD best practice
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deactivated successfully (Soft Deleted)." });
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.Roles.ToListAsync();
        return Ok(roles);
    }
}
