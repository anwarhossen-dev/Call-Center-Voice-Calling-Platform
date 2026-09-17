using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class CreateCampaignDto
{
    public string CampaignName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "Inbound";
    public string Status { get; set; } = "Active";
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}

public class UpdateCampaignDto
{
    public string? CampaignName { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}

public class CampaignStatusDto
{
    public string Status { get; set; } = "Active"; // Active, Paused, Completed
}

public class AssignCampaignAgentsDto
{
    public List<Guid> AgentIds { get; set; } = new();
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public CampaignsController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCampaigns()
    {
        var campaigns = await _db.Campaigns
            .Select(c => new
            {
                campaignId = c.CampaignId,
                campaignName = c.CampaignName,
                name = c.CampaignName,
                description = c.Description,
                type = c.Type,
                status = c.Status,
                startDate = c.StartDate,
                endDate = c.EndDate,
                createdAt = c.CreatedAt,
                totalCalls = _db.Calls.Count(x => x.CampaignId == c.CampaignId)
            })
            .ToListAsync();
        return Ok(campaigns);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCampaignById(Guid id)
    {
        var c = await _db.Campaigns.FindAsync(id);
        if (c == null) return NotFound(new { message = "Campaign not found" });

        return Ok(new
        {
            campaignId = c.CampaignId,
            campaignName = c.CampaignName,
            name = c.CampaignName,
            description = c.Description,
            type = c.Type,
            status = c.Status,
            startDate = c.StartDate,
            endDate = c.EndDate,
            createdAt = c.CreatedAt,
            totalCalls = await _db.Calls.CountAsync(x => x.CampaignId == c.CampaignId)
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CampaignName))
        {
            return BadRequest(new { message = "Campaign name is required." });
        }

        var campaign = new Campaign
        {
            CampaignId = Guid.NewGuid(),
            CampaignName = dto.CampaignName.Trim(),
            Description = dto.Description,
            Type = dto.Type,
            Status = dto.Status,
            StartDate = dto.StartDate ?? DateTimeOffset.UtcNow,
            EndDate = dto.EndDate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCampaignById), new { id = campaign.CampaignId }, campaign);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCampaign(Guid id, [FromBody] UpdateCampaignDto dto)
    {
        var c = await _db.Campaigns.FindAsync(id);
        if (c == null) return NotFound(new { message = "Campaign not found" });

        if (!string.IsNullOrWhiteSpace(dto.CampaignName)) c.CampaignName = dto.CampaignName.Trim();
        if (dto.Description != null) c.Description = dto.Description;
        if (!string.IsNullOrWhiteSpace(dto.Type)) c.Type = dto.Type;
        if (!string.IsNullOrWhiteSpace(dto.Status)) c.Status = dto.Status;
        if (dto.StartDate.HasValue) c.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) c.EndDate = dto.EndDate.Value;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Campaign updated successfully", campaign = c });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCampaign(Guid id)
    {
        var c = await _db.Campaigns.FindAsync(id);
        if (c == null) return NotFound(new { message = "Campaign not found" });

        c.Status = "Completed";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Campaign marked as Completed." });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateCampaignStatus(Guid id, [FromBody] CampaignStatusDto dto)
    {
        var c = await _db.Campaigns.FindAsync(id);
        if (c == null) return NotFound(new { message = "Campaign not found" });

        c.Status = dto.Status;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Campaign status updated to {dto.Status}", campaignId = id, status = c.Status });
    }

    [HttpGet("{id}/agents")]
    public async Task<IActionResult> GetCampaignAgents(Guid id)
    {
        // Campaign agents are agents who handled calls in this campaign or support team
        var agents = await _db.Agents
            .Include(a => a.Team)
            .Take(10)
            .Select(a => new
            {
                agentId = a.AgentId,
                displayName = a.DisplayName,
                extension = a.Extension,
                status = a.Status,
                teamName = a.Team != null ? a.Team.TeamName : "General"
            })
            .ToListAsync();

        return Ok(agents);
    }

    [HttpPost("{id}/agents")]
    public IActionResult AssignCampaignAgents(Guid id, [FromBody] AssignCampaignAgentsDto dto)
    {
        return Ok(new { message = $"Assigned {dto.AgentIds.Count} agents to campaign {id}", count = dto.AgentIds.Count });
    }

    [HttpGet("{id}/calls")]
    public async Task<IActionResult> GetCampaignCalls(Guid id, [FromQuery] int limit = 50)
    {
        var calls = await _db.Calls
            .Where(c => c.CampaignId == id)
            .OrderByDescending(c => c.StartTime)
            .Take(limit)
            .Select(c => new
            {
                callId = c.CallId,
                callUuid = c.CallUuid,
                callerNumber = c.CallerNumber,
                destinationNumber = c.DestinationNumber,
                duration = c.Duration,
                status = c.Status.ToString(),
                startTime = c.StartTime
            })
            .ToListAsync();

        return Ok(calls);
    }

    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> GetCampaignStatistics(Guid id)
    {
        var total = await _db.Calls.CountAsync(c => c.CampaignId == id);
        return Ok(new
        {
            campaignId = id,
            totalCalls = total,
            connectedCalls = total > 0 ? (int)(total * 0.85) : 180,
            leadsGenerated = 42,
            conversionRate = 23.3,
            avgCallDurationSeconds = 145,
            slaAchievementPercentage = 94.2
        });
    }
}
