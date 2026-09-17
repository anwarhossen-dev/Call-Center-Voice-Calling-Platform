using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class CreateQaReviewDto
{
    public Guid CallId { get; set; }
    public Guid AgentId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid? ScorecardId { get; set; }
    public decimal Score { get; set; }
    public string? Feedback { get; set; }
}

public class UpdateQaReviewDto
{
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
}

public class CreateScorecardDto
{
    public string Name { get; set; } = string.Empty;
    public string CriteriaJson { get; set; } = "[]";
    public int PassThreshold { get; set; } = 80;
}

public class UpdateScorecardDto
{
    public string? Name { get; set; }
    public string? CriteriaJson { get; set; }
    public int? PassThreshold { get; set; }
    public bool? IsActive { get; set; }
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class QaController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public QaController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet("calls")]
    public async Task<IActionResult> GetCallsForQa([FromQuery] int limit = 50)
    {
        var calls = await _db.Calls
            .Where(c => c.Status == CallStatus.Completed)
            .Include(c => c.Agent)
            .Include(c => c.Recording)
            .OrderByDescending(c => c.StartTime)
            .Take(limit)
            .Select(c => new
            {
                callId = c.CallId,
                callUuid = c.CallUuid,
                callerNumber = c.CallerNumber,
                destinationNumber = c.DestinationNumber,
                agentId = c.AgentId,
                agentName = c.Agent != null ? c.Agent.DisplayName : "Unassigned",
                duration = c.Duration,
                hasRecording = c.Recording != null,
                recordingUrl = c.Recording != null ? c.Recording.StoragePath : null,
                startTime = c.StartTime
            })
            .ToListAsync();

        return Ok(calls);
    }

    [HttpGet("calls/{id}")]
    public async Task<IActionResult> GetQaCallDetails(string id)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls
            .Include(c => c.Agent)
            .Include(c => c.Recording)
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));

        if (call == null) return NotFound(new { message = "Call not found." });

        return Ok(new
        {
            callId = call.CallId,
            callUuid = call.CallUuid,
            agentId = call.AgentId,
            agentName = call.Agent?.DisplayName,
            caller = call.CallerNumber,
            destination = call.DestinationNumber,
            duration = call.Duration,
            recordingUrl = call.Recording?.StoragePath,
            startTime = call.StartTime,
            events = call.Events
        });
    }

    [HttpPost("reviews")]
    public async Task<IActionResult> CreateReview([FromBody] CreateQaReviewDto dto)
    {
        var review = new QAReview
        {
            ReviewId = Guid.NewGuid(),
            CallId = dto.CallId,
            AgentId = dto.AgentId,
            ReviewerId = dto.ReviewerId,
            ScorecardId = dto.ScorecardId,
            Score = dto.Score,
            Feedback = dto.Feedback,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.QAReviews.Add(review);
        await _db.SaveChangesAsync();

        return Ok(new { message = "QA Review created successfully", reviewId = review.ReviewId, review });
    }

    [HttpGet("reviews/{id}")]
    public async Task<IActionResult> GetReviewById(Guid id)
    {
        var review = await _db.QAReviews
            .Include(r => r.Agent)
            .Include(r => r.Scorecard)
            .FirstOrDefaultAsync(r => r.ReviewId == id);

        if (review == null) return NotFound(new { message = "Review not found" });
        return Ok(review);
    }

    [HttpPut("reviews/{id}")]
    public async Task<IActionResult> UpdateReview(Guid id, [FromBody] UpdateQaReviewDto dto)
    {
        var r = await _db.QAReviews.FindAsync(id);
        if (r == null) return NotFound(new { message = "Review not found" });

        if (dto.Score.HasValue) r.Score = dto.Score.Value;
        if (dto.Feedback != null) r.Feedback = dto.Feedback;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Review updated successfully", review = r });
    }

    [HttpGet("agents/{id}")]
    public async Task<IActionResult> GetAgentQaHistory(Guid id)
    {
        var reviews = await _db.QAReviews
            .Where(r => r.AgentId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var avgScore = reviews.Any() ? reviews.Average(r => r.Score) : 85.0m;

        return Ok(new
        {
            agentId = id,
            totalReviews = reviews.Count,
            averageScore = Math.Round(avgScore, 1),
            history = reviews
        });
    }

    [HttpGet("scorecards")]
    public async Task<IActionResult> GetScorecards()
    {
        var cards = await _db.QAScorecards.ToListAsync();
        return Ok(cards);
    }

    [HttpPost("scorecards")]
    public async Task<IActionResult> CreateScorecard([FromBody] CreateScorecardDto dto)
    {
        var card = new QAScorecard
        {
            ScorecardId = Guid.NewGuid(),
            Name = dto.Name,
            CriteriaJson = dto.CriteriaJson,
            PassThreshold = dto.PassThreshold,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.QAScorecards.Add(card);
        await _db.SaveChangesAsync();
        return Ok(card);
    }

    [HttpPut("scorecards/{id}")]
    public async Task<IActionResult> UpdateScorecard(Guid id, [FromBody] UpdateScorecardDto dto)
    {
        var card = await _db.QAScorecards.FindAsync(id);
        if (card == null) return NotFound(new { message = "Scorecard not found" });

        if (!string.IsNullOrEmpty(dto.Name)) card.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.CriteriaJson)) card.CriteriaJson = dto.CriteriaJson;
        if (dto.PassThreshold.HasValue) card.PassThreshold = dto.PassThreshold.Value;
        if (dto.IsActive.HasValue) card.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Scorecard updated", card });
    }
}
