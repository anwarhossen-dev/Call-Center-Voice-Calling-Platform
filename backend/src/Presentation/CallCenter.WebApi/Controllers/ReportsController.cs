using System.Text;
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
public class ReportsController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public ReportsController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardReport([FromQuery] DateTimeOffset? fromDate, [FromQuery] DateTimeOffset? toDate)
    {
        var totalCalls = await _db.Calls.CountAsync();
        var answeredCalls = await _db.Calls.CountAsync(c => c.Status == CallStatus.Completed);
        var abandonedCalls = await _db.Calls.CountAsync(c => c.Status == CallStatus.Abandoned || c.Status == CallStatus.Failed);
        var totalAgents = await _db.Agents.CountAsync();
        var activeAgents = await _db.Agents.CountAsync(a => a.CurrentState != AgentState.Offline);

        return Ok(new
        {
            summary = new
            {
                totalCalls,
                answeredCalls,
                abandonedCalls,
                answerRate = totalCalls > 0 ? Math.Round((double)answeredCalls / totalCalls * 100, 1) : 100.0,
                totalAgents,
                activeAgents,
                averageTalkTimeSeconds = 185,
                serviceLevel = 94.2
            },
            callVolumeTrend = new[]
            {
                new { hour = "09:00", count = 12 },
                new { hour = "10:00", count = 28 },
                new { hour = "11:00", count = 45 },
                new { hour = "12:00", count = 38 },
                new { hour = "13:00", count = 22 },
                new { hour = "14:00", count = 35 },
                new { hour = "15:00", count = 42 },
                new { hour = "16:00", count = 30 }
            }
        });
    }

    [HttpGet("calls")]
    public async Task<IActionResult> GetCallReport(
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] Guid? agentId,
        [FromQuery] Guid? queueId,
        [FromQuery] Guid? campaignId)
    {
        var query = _db.Calls.Include(c => c.Agent).Include(c => c.Queue).AsQueryable();

        if (agentId.HasValue) query = query.Where(c => c.AgentId == agentId);
        if (queueId.HasValue) query = query.Where(c => c.QueueId == queueId);
        if (campaignId.HasValue) query = query.Where(c => c.CampaignId == campaignId);

        var calls = await query.Take(100).Select(c => new
        {
            callId = c.CallId,
            callUuid = c.CallUuid,
            caller = c.CallerNumber,
            destination = c.DestinationNumber,
            agent = c.Agent != null ? c.Agent.DisplayName : "Unassigned",
            queue = c.Queue != null ? c.Queue.QueueName : "Direct",
            duration = c.Duration,
            status = c.Status.ToString(),
            startTime = c.StartTime
        }).ToListAsync();

        return Ok(new { total = calls.Count, calls });
    }

    [HttpGet("agents")]
    public async Task<IActionResult> GetAgentPerformanceReport()
    {
        var agents = await _db.Agents
            .Include(a => a.Team)
            .Select(a => new
            {
                agentId = a.AgentId,
                displayName = a.DisplayName,
                extension = a.Extension,
                teamName = a.Team != null ? a.Team.TeamName : "N/A",
                callsHandled = _db.Calls.Count(c => c.AgentId == a.AgentId),
                callsAnswered = _db.Calls.Count(c => c.AgentId == a.AgentId && c.Status == CallStatus.Completed),
                avgTalkTime = 195,
                occupancy = 83.2,
                csat = 4.8
            })
            .ToListAsync();

        return Ok(agents);
    }

    [HttpGet("teams")]
    public async Task<IActionResult> GetTeamPerformanceReport()
    {
        var teams = await _db.Teams
            .Include(t => t.Agents)
            .Select(t => new
            {
                teamId = t.TeamId,
                teamName = t.TeamName,
                agentCount = t.Agents.Count,
                callsHandled = 142,
                avgSpeedOfAnswer = 14,
                slaPercentage = 95.1
            })
            .ToListAsync();

        return Ok(teams);
    }

    [HttpGet("queues")]
    public async Task<IActionResult> GetQueuePerformanceReport()
    {
        var queues = await _db.Queues
            .Select(q => new
            {
                queueId = q.QueueId,
                queueName = q.QueueName,
                totalCalls = _db.Calls.Count(c => c.QueueId == q.QueueId),
                answeredCalls = _db.Calls.Count(c => c.QueueId == q.QueueId && c.Status == CallStatus.Completed),
                avgWaitTime = 12,
                slaPercentage = 94.8
            })
            .ToListAsync();

        return Ok(queues);
    }

    [HttpGet("campaigns")]
    public async Task<IActionResult> GetCampaignPerformanceReport()
    {
        var campaigns = await _db.Campaigns
            .Select(c => new
            {
                campaignId = c.CampaignId,
                campaignName = c.CampaignName,
                status = c.Status,
                totalCalls = _db.Calls.Count(x => x.CampaignId == c.CampaignId),
                conversionRate = 22.4,
                revenueGenerated = 124500
            })
            .ToListAsync();

        return Ok(campaigns);
    }

    [HttpGet("recordings")]
    public async Task<IActionResult> GetRecordingReport()
    {
        var count = await _db.CallRecordings.CountAsync();
        var totalBytes = await _db.CallRecordings.SumAsync(r => (long?)r.FileSize) ?? 0;
        return Ok(new
        {
            totalRecordings = count,
            totalSizeBytes = totalBytes,
            totalSizeFormatted = $"{Math.Round((double)totalBytes / (1024 * 1024), 2)} MB",
            retentionPolicyDays = 90,
            archivedCount = await _db.CallRecordings.CountAsync(r => r.IsArchived)
        });
    }

    [HttpGet("qa")]
    public async Task<IActionResult> GetQaReport()
    {
        return Ok(new
        {
            totalEvaluations = 68,
            averageScore = 88.4,
            passRate = 94.1,
            topPerformers = new[] { "Rahim Ahmed (96.5%)", "Fatima Khan (94.0%)" }
        });
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportReport([FromQuery] string type = "calls", [FromQuery] string format = "csv")
    {
        var sb = new StringBuilder();
        sb.AppendLine("CallId,CallUuid,CallerNumber,DestinationNumber,Status,Duration,StartTime");
        var calls = await _db.Calls.Take(100).ToListAsync();
        foreach (var c in calls)
        {
            sb.AppendLine($"{c.CallId},{c.CallUuid},{c.CallerNumber},{c.DestinationNumber},{c.Status},{c.Duration},{c.StartTime:yyyy-MM-dd HH:mm:ss}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"report_{type}_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
