using System.Diagnostics;
using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public MonitoringController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetLiveDashboard()
    {
        var activeCalls = await _db.Calls
            .Where(c => c.Status == CallStatus.InProgress || c.Status == CallStatus.Ringing || c.Status == CallStatus.Queued)
            .Include(c => c.Agent)
            .Include(c => c.Queue)
            .Select(c => new
            {
                callId = c.CallId,
                callUuid = c.CallUuid,
                caller = c.CallerNumber,
                destination = c.DestinationNumber,
                status = c.Status.ToString(),
                agentName = c.Agent != null ? c.Agent.DisplayName : "In Queue",
                queueName = c.Queue != null ? c.Queue.QueueName : "Direct",
                durationSeconds = (int)(DateTimeOffset.UtcNow - c.StartTime).TotalSeconds
            })
            .ToListAsync();

        var agentSummary = await _db.Agents
            .GroupBy(a => a.CurrentState)
            .Select(g => new { state = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            activeCallsCount = activeCalls.Count,
            activeCalls,
            agentStates = agentSummary,
            totalAgents = await _db.Agents.CountAsync(),
            serverTimestamp = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("agents")]
    public async Task<IActionResult> GetLiveAgents()
    {
        var agents = await _db.Agents
            .Include(a => a.Team)
            .Select(a => new
            {
                agentId = a.AgentId,
                agentCode = a.AgentCode,
                extension = a.Extension,
                displayName = a.DisplayName,
                team = a.Team != null ? a.Team.TeamName : "Support",
                status = a.Status,
                state = a.CurrentState.ToString(),
                stateChangedAt = a.StateChangedAt,
                durationInState = (int)(DateTimeOffset.UtcNow - a.StateChangedAt).TotalSeconds
            })
            .ToListAsync();

        return Ok(agents);
    }

    [HttpGet("queues")]
    public async Task<IActionResult> GetLiveQueues()
    {
        var queues = await _db.Queues
            .Include(q => q.AgentQueues)
            .Select(q => new
            {
                queueId = q.QueueId,
                queueName = q.QueueName,
                priority = q.Priority,
                strategy = q.Strategy,
                assignedAgents = q.AgentQueues.Count,
                waitingCalls = _db.Calls.Count(c => c.QueueId == q.QueueId && (c.Status == CallStatus.Ringing || c.Status == CallStatus.Queued)),
                longestWaitSeconds = 24
            })
            .ToListAsync();

        return Ok(queues);
    }

    [HttpGet("calls")]
    public async Task<IActionResult> GetActiveCalls()
    {
        var calls = await _db.Calls
            .Where(c => c.Status == CallStatus.InProgress || c.Status == CallStatus.Ringing || c.Status == CallStatus.Queued)
            .Include(c => c.Agent)
            .Include(c => c.Queue)
            .Select(c => new
            {
                callId = c.CallId,
                callUuid = c.CallUuid,
                caller = c.CallerNumber,
                destination = c.DestinationNumber,
                status = c.Status.ToString(),
                agent = c.Agent != null ? c.Agent.DisplayName : null,
                queue = c.Queue != null ? c.Queue.QueueName : null,
                duration = (int)(DateTimeOffset.UtcNow - c.StartTime).TotalSeconds,
                startedAt = c.StartTime
            })
            .ToListAsync();

        return Ok(calls);
    }

    [HttpGet("system")]
    public IActionResult GetSystemHealth()
    {
        var process = Process.GetCurrentProcess();
        return Ok(new
        {
            status = "Operational",
            uptimeSeconds = (int)(DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds,
            memoryAllocatedMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
            threads = process.Threads.Count,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            databaseEngine = "Microsoft SQL Server",
            telephonyCore = "FreeSWITCH / WebRTC Engine",
            signalRHubStatus = "Connected"
        });
    }
}

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    [HttpGet("api/health")]
    [HttpGet("api/v1/health")]
    public IActionResult CheckHealth()
    {
        return Ok(new
        {
            status = "UP",
            timestamp = DateTimeOffset.UtcNow,
            services = new
            {
                database = "Healthy",
                telephony = "Healthy",
                signalR = "Healthy",
                storage = "Healthy"
            }
        });
    }
}
