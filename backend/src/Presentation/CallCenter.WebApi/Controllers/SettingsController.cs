using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class UpdateSettingDto
{
    public string SettingValue { get; set; } = string.Empty;
    public string? Description { get; set; }
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public SettingsController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSettings()
    {
        var settings = await _db.SystemSettings.ToListAsync();
        return Ok(settings);
    }

    [HttpGet("telephony")]
    public async Task<IActionResult> GetTelephonySettings()
    {
        var settings = await _db.SystemSettings
            .Where(s => s.SettingKey.StartsWith("telephony."))
            .ToListAsync();

        return Ok(settings);
    }

    [HttpGet("call-routing")]
    public async Task<IActionResult> GetCallRoutingSettings()
    {
        var settings = await _db.SystemSettings
            .Where(s => s.SettingKey.StartsWith("routing."))
            .ToListAsync();

        return Ok(settings);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetSetting(string key)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey.ToLower() == key.ToLower());
        if (setting == null) return NotFound(new { message = $"Setting '{key}' not found" });

        return Ok(setting);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingDto dto)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey.ToLower() == key.ToLower());
        if (setting == null)
        {
            setting = new SystemSetting
            {
                SettingId = Guid.NewGuid(),
                SettingKey = key,
                SettingValue = dto.SettingValue,
                Description = dto.Description,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.SystemSettings.Add(setting);
        }
        else
        {
            setting.SettingValue = dto.SettingValue;
            if (dto.Description != null) setting.Description = dto.Description;
            setting.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Setting '{key}' updated successfully", setting });
    }
}
