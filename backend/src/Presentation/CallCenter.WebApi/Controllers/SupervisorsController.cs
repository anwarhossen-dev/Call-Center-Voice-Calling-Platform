using CallCenter.Application.DTOs;
using CallCenter.Application.Interfaces;
using CallCenter.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupervisorsController : ControllerBase
{
    private readonly IAgentStateService _agentStateService;
    private readonly ITelephonyBridge _telephony;

    public SupervisorsController(IAgentStateService agentStateService, ITelephonyBridge telephony)
    {
        _agentStateService = agentStateService;
        _telephony = telephony;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var agents = await _agentStateService.GetAllAgentStatesAsync();
        var dashboard = new SupervisorDashboardDto
        {
            TotalCallsToday = 248,
            AnsweredCallsToday = 226,
            AbandonedCallsToday = 22,
            AverageHandleTimeSeconds = 272, // 4m 32s
            AvailableAgents = agents.Count(a => a.State == AgentState.Available),
            OnCallAgents = agents.Count(a => a.State == AgentState.OnCall),
            BreakAgents = agents.Count(a => a.State == AgentState.Break || a.State == AgentState.Lunch),
            ACWAgents = agents.Count(a => a.State == AgentState.WrapUp),
            OfflineAgents = agents.Count(a => a.State == AgentState.Offline),
            Agents = agents,
            ActiveCalls = new List<ActiveCallDto>
            {
                new()
                {
                    CallUuid = "active-call-8842",
                    CallerNumber = "+880 1712 345678",
                    DestinationNumber = "09612345678",
                    AgentExtension = "1002",
                    AgentName = "Fatima Khan",
                    QueueName = "Inbound Support",
                    ConnectedAt = DateTimeOffset.UtcNow.AddMinutes(-3)
                }
            }
        };

        return Ok(dashboard);
    }

    [HttpPost("intervene")]
    public async Task<IActionResult> Intervene([FromBody] SupervisorInterventionRequest request)
    {
        var success = await _telephony.InterveneCallAsync(request.SupervisorExtension, request.TargetCallUuid, request.Mode);
        return Ok(new { Success = success, Mode = request.Mode.ToString() });
    }
}
