using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class CreateTeamDto
{
    public string TeamName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
}

public class UpdateTeamDto
{
    public string? TeamName { get; set; }
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
    public bool? IsActive { get; set; }
}

public class AddAgentToTeamDto
{
    public Guid AgentId { get; set; }
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public TeamsController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeams()
    {
        var teams = await _db.Teams
            .Include(t => t.Manager)
            .Include(t => t.Agents)
            .Select(t => new
            {
                teamId = t.TeamId,
                teamName = t.TeamName,
                description = t.Description,
                managerId = t.ManagerId,
                managerName = t.Manager != null ? t.Manager.Username : null,
                isActive = t.IsActive,
                agentCount = t.Agents.Count,
                createdAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(teams);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeamById(Guid id)
    {
        var t = await _db.Teams
            .Include(x => x.Manager)
            .Include(x => x.Agents)
            .FirstOrDefaultAsync(x => x.TeamId == id);

        if (t == null) return NotFound(new { message = "Team not found" });

        return Ok(new
        {
            teamId = t.TeamId,
            teamName = t.TeamName,
            description = t.Description,
            managerId = t.ManagerId,
            managerName = t.Manager?.Username,
            isActive = t.IsActive,
            agentCount = t.Agents.Count,
            createdAt = t.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TeamName))
        {
            return BadRequest(new { message = "Team name is required." });
        }

        var team = new Team
        {
            TeamId = Guid.NewGuid(),
            TeamName = dto.TeamName.Trim(),
            Description = dto.Description,
            ManagerId = dto.ManagerId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTeamById), new { id = team.TeamId }, team);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] UpdateTeamDto dto)
    {
        var team = await _db.Teams.FindAsync(id);
        if (team == null) return NotFound(new { message = "Team not found" });

        if (!string.IsNullOrWhiteSpace(dto.TeamName)) team.TeamName = dto.TeamName.Trim();
        if (dto.Description != null) team.Description = dto.Description;
        if (dto.ManagerId.HasValue) team.ManagerId = dto.ManagerId.Value;
        if (dto.IsActive.HasValue) team.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Team updated successfully", team });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        var team = await _db.Teams.FindAsync(id);
        if (team == null) return NotFound(new { message = "Team not found" });

        team.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Team deactivated successfully (Soft Deleted)." });
    }

    [HttpGet("{id}/agents")]
    public async Task<IActionResult> GetTeamAgents(Guid id)
    {
        var agents = await _db.Agents
            .Where(a => a.TeamId == id)
            .Select(a => new
            {
                agentId = a.AgentId,
                agentCode = a.AgentCode,
                extension = a.Extension,
                displayName = a.DisplayName,
                status = a.Status,
                isActive = a.IsActive,
                hireDate = a.HireDate
            })
            .ToListAsync();

        return Ok(agents);
    }

    [HttpPost("{id}/agents")]
    public async Task<IActionResult> AddAgentToTeam(Guid id, [FromBody] AddAgentToTeamDto dto)
    {
        var team = await _db.Teams.FindAsync(id);
        if (team == null) return NotFound(new { message = "Team not found" });

        var agent = await _db.Agents.FindAsync(dto.AgentId);
        if (agent == null) return NotFound(new { message = "Agent not found" });

        agent.TeamId = id;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Agent {agent.DisplayName} assigned to team {team.TeamName}" });
    }

    [HttpDelete("{id}/agents/{agentId}")]
    public async Task<IActionResult> RemoveAgentFromTeam(Guid id, Guid agentId)
    {
        var agent = await _db.Agents.FirstOrDefaultAsync(a => a.AgentId == agentId && a.TeamId == id);
        if (agent == null) return NotFound(new { message = "Agent not found in this team" });

        agent.TeamId = null;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Agent unassigned from team" });
    }
}
