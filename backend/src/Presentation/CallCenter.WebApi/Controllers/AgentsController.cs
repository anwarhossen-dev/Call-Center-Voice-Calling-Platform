using CallCenter.Application.Interfaces;
using CallCenter.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly IAgentStateService _agentStateService;
    private readonly IRealtimeNotifier _notifier;
    private readonly CallCenter.Persistence.CallCenterDbContext _db;

    public AgentsController(IAgentStateService agentStateService, IRealtimeNotifier notifier, CallCenter.Persistence.CallCenterDbContext db)
    {
        _agentStateService = agentStateService;
        _notifier = notifier;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAgents()
    {
        var agents = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _db.Agents
                .Include(a => a.User)
                .OrderBy(a => a.Extension)
                .Select(a => new
                {
                    id = a.AgentId.ToString(),
                    agentId = a.AgentId,
                    extension = a.Extension,
                    displayName = a.DisplayName,
                    name = a.DisplayName,
                    role = a.Extension == "1001" ? "Support Specialist" : (a.Extension == "1002" ? "Senior Technical Agent" : "Call Center Agent"),
                    state = a.CurrentState.ToString(),
                    email = a.User != null ? a.User.Email : "",
                    stateChangedAt = a.StateChangedAt,
                    createdAt = a.CreatedAt
                })
        );
        return Ok(agents);
    }

    [HttpGet("states")]
    public async Task<IActionResult> GetAllStates()
    {
        var states = await _agentStateService.GetAllAgentStatesAsync();
        return Ok(states);
    }

    [HttpPut("{agentId}/state")]
    public async Task<IActionResult> UpdateState(Guid agentId, [FromQuery] AgentState newState, [FromQuery] string? subReason)
    {
        var updated = await _agentStateService.SetAgentStateAsync(agentId, newState, subReason);

        // Also update in SQL Server database
        var dbAgent = await _db.Agents.FindAsync(agentId);
        if (dbAgent != null)
        {
            dbAgent.CurrentState = newState;
            dbAgent.StateChangedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        await _notifier.BroadcastAgentStateChangedAsync(updated);
        return Ok(updated);
    }
}
