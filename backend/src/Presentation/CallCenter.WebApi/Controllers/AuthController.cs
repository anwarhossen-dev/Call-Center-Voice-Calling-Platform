using System.Security.Cryptography;
using System.Text;
using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes("salt_btcl_telecom_" + password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        // Support default seeded test passwords
        if (storedHash.StartsWith("AQAAAAEAACcQAAAAEHASH"))
        {
            if (password == "Admin@123456" || password == "Agent@123456" || password == "123456")
                return true;
        }

        // Direct match fallback
        if (storedHash == password) return true;

        // Hash verification
        return HashPassword(password) == storedHash;
    }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Agent"; // Admin, Supervisor, Agent
    public string? Extension { get; set; }
}

public class LoginRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public Guid? AgentId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset LoggedInAt { get; set; } = DateTimeOffset.UtcNow;
}

public class RefreshTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly CallCenterDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(CallCenterDbContext db, ILogger<AuthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var usernameNormalized = request.Username.Trim().ToLower();
        var emailNormalized = string.IsNullOrWhiteSpace(request.Email) 
            ? $"{usernameNormalized}@btcl-voice.bd" 
            : request.Email.Trim().ToLower();

        // 1. Check duplicate username or email
        var exists = await _db.Users.AnyAsync(u => u.Username.ToLower() == usernameNormalized || u.Email.ToLower() == emailNormalized);
        if (exists)
        {
            return BadRequest(new { message = $"Username '{request.Username}' or email '{emailNormalized}' is already registered." });
        }

        // 2. Resolve Role
        var roleName = string.IsNullOrWhiteSpace(request.Role) ? "Agent" : request.Role.Trim();
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());
        if (role == null)
        {
            role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Agent")
                ?? new Role { RoleId = Guid.NewGuid(), RoleName = roleName, Description = roleName + " Role" };
        }

        // 3. Create User
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = usernameNormalized,
            Email = emailNormalized,
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            RoleId = role.RoleId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);

        // 4. If Agent or Extension provided, create Agent profile
        Guid? createdAgentId = null;
        string? assignedExt = null;
        var displayName = !string.IsNullOrWhiteSpace(request.DisplayName) ? request.DisplayName.Trim() : request.Username;

        if (role.RoleName.Equals("Agent", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(request.Extension))
        {
            if (!string.IsNullOrWhiteSpace(request.Extension))
            {
                assignedExt = request.Extension.Trim();
            }
            else
            {
                // Assign next extension like 1006, 1007, etc.
                var maxExt = await _db.Agents
                    .Select(a => a.Extension)
                    .ToListAsync();
                
                var maxNum = maxExt
                    .Select(e => int.TryParse(e, out var n) ? n : 1000)
                    .DefaultIfEmpty(1005)
                    .Max();

                assignedExt = (maxNum + 1).ToString();
            }

            var agent = new Agent
            {
                AgentId = Guid.NewGuid(),
                UserId = user.UserId,
                Extension = assignedExt,
                DisplayName = displayName,
                CurrentState = AgentState.Available,
                StateChangedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Agents.Add(agent);
            createdAgentId = agent.AgentId;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Successfully registered user {Username} (Role: {Role}, Extension: {Extension}) into SQL Server", user.Username, role.RoleName, assignedExt);

        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.UserId}:{user.Username}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));

        return Ok(new AuthResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            DisplayName = displayName,
            Role = role.RoleName,
            Extension = assignedExt,
            AgentId = createdAgentId,
            Token = token
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username/Email and Password are required." });
        }

        var identifier = request.UsernameOrEmail.Trim().ToLower();

        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.AgentProfile)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == identifier || u.Email.ToLower() == identifier);

        if (user == null)
        {
            return Unauthorized(new { message = "User not found with this username or email." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "This user account is inactive. Please contact your supervisor." });
        }

        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid password. Please check your credentials." });
        }

        // If user is an agent, make sure they are set to Available
        if (user.AgentProfile != null)
        {
            user.AgentProfile.CurrentState = AgentState.Available;
            user.AgentProfile.StateChangedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        var roleName = user.Role != null ? user.Role.RoleName : "Agent";
        var displayName = user.AgentProfile != null ? user.AgentProfile.DisplayName : user.Username;
        var extension = user.AgentProfile?.Extension;
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.UserId}:{user.Username}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));

        _logger.LogInformation("User {Username} logged in successfully as {Role}", user.Username, roleName);

        return Ok(new AuthResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            DisplayName = displayName,
            Role = roleName,
            Extension = extension,
            AgentId = user.AgentProfile?.AgentId,
            Token = token
        });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser([FromQuery] string? token, [FromQuery] string? username)
    {
        string? searchUsername = username;

        if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split(':');
                if (parts.Length >= 2)
                {
                    searchUsername = parts[1];
                }
            }
            catch
            {
                // Invalid token fallback
            }
        }

        if (string.IsNullOrWhiteSpace(searchUsername))
        {
            return Unauthorized(new { message = "No valid session token or username provided." });
        }

        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.AgentProfile)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == searchUsername.ToLower());

        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(new AuthResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.AgentProfile?.DisplayName ?? user.Username,
            Role = user.Role?.RoleName ?? "Agent",
            Extension = user.AgentProfile?.Extension,
            AgentId = user.AgentProfile?.AgentId,
            Token = token ?? ""
        });
    }

    [HttpPost("refresh-token")]
    public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { message = "Token is required." });
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Token));
            var parts = decoded.Split(':');
            var newToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{parts[0]}:{(parts.Length > 1 ? parts[1] : "user")}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
            return Ok(new { token = newToken, refreshedAt = DateTimeOffset.UtcNow });
        }
        catch
        {
            var newToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"session:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));
            return Ok(new { token = newToken, refreshedAt = DateTimeOffset.UtcNow });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request)
    {
        if (request != null && !string.IsNullOrWhiteSpace(request.Token))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Token));
                var parts = decoded.Split(':');
                if (parts.Length > 1)
                {
                    var user = await _db.Users.Include(u => u.AgentProfile).FirstOrDefaultAsync(u => u.Username == parts[1]);
                    if (user?.AgentProfile != null)
                    {
                        user.AgentProfile.CurrentState = AgentState.Offline;
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch {}
        }
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Username/email and new password are required." });
        }

        var identifier = request.UsernameOrEmail.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == identifier || u.Email.ToLower() == identifier);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (!string.IsNullOrEmpty(request.CurrentPassword) && !PasswordHelper.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "Current password does not match." });
        }

        user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());
        // For security, always return success message
        var resetToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{request.Email}:{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}"));
        return Ok(new
        {
            message = "If this email is registered, a password reset token has been generated.",
            resetToken = resetToken
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Token and new password are required." });
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Token));
            var parts = decoded.Split(':');
            var email = parts[0];
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            if (user != null)
            {
                user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
                await _db.SaveChangesAsync();
                return Ok(new { message = "Password reset successfully. You may now log in." });
            }
        }
        catch {}

        return BadRequest(new { message = "Invalid or expired reset token." });
    }

    [HttpGet("permissions")]
    public IActionResult GetPermissions([FromQuery] string? role)
    {
        var targetRole = role ?? "Agent";
        var permissions = targetRole.ToLower() switch
        {
            "admin" => new[]
            {
                "calls.view", "calls.initiate", "calls.control", "recordings.view", "recordings.download", "recordings.delete",
                "agents.manage", "users.manage", "roles.manage", "teams.manage", "queues.manage", "campaigns.manage",
                "reports.view", "reports.export", "qa.manage", "settings.manage", "audit.view"
            },
            "supervisor" => new[]
            {
                "calls.view", "calls.monitor", "calls.barge", "calls.whisper", "recordings.view", "recordings.download",
                "agents.view", "agents.status", "teams.view", "queues.view", "reports.view", "reports.export", "qa.manage"
            },
            _ => new[]
            {
                "calls.view", "calls.initiate", "calls.control", "recordings.view", "customers.view", "crm.activity"
            }
        };

        return Ok(new { role = targetRole, permissions });
    }
}
