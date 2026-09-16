using CallCenter.Application.DTOs;
using CallCenter.Application.Interfaces;
using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DispositionsController : ControllerBase
{
    private readonly CallCenterDbContext _db;
    private readonly ICrmService _crm;

    public DispositionsController(CallCenterDbContext db, ICrmService crm)
    {
        _db = db;
        _crm = crm;
    }

    [HttpGet]
    public async Task<IActionResult> GetDispositions()
    {
        var list = await _db.Dispositions.Where(d => d.IsActive).ToListAsync();
        return Ok(list);
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitDisposition([FromBody] CallDispositionRequest request)
    {
        // 1. Locate existing call record by CallUuid
        var call = await _db.Calls.Include(c => c.Disposition).FirstOrDefaultAsync(c => c.CallUuid == request.CallUuid);

        // Fallback: If not found by UUID, find the most recent matching call by destination or CRM Contact
        if (call == null && !string.IsNullOrWhiteSpace(request.DestinationNumber))
        {
            call = await _db.Calls
                .OrderByDescending(c => c.InitiatedAt)
                .FirstOrDefaultAsync(c => c.DestinationNumber == request.DestinationNumber || c.CRMContactId == request.CustomerId);
        }

        // If still null, create a new call record directly so no data is ever lost
        if (call == null)
        {
            call = new Call
            {
                CallUuid = string.IsNullOrWhiteSpace(request.CallUuid) ? Guid.NewGuid().ToString() : request.CallUuid,
                Direction = CallDirection.Outbound,
                CallerNumber = !string.IsNullOrWhiteSpace(request.AgentExtension) ? request.AgentExtension : "1001",
                DestinationNumber = !string.IsNullOrWhiteSpace(request.DestinationNumber) 
                    ? request.DestinationNumber 
                    : (!string.IsNullOrWhiteSpace(request.CustomerId) ? request.CustomerId : "Direct Outbound"),
                InitiatedAt = DateTimeOffset.UtcNow.AddSeconds(-15),
                Status = CallStatus.Completed,
                CRMContactId = request.CustomerId
            };
            _db.Calls.Add(call);
        }

        // 2. Ensure Call Status and Timestamps are accurately updated
        if (call.Status == CallStatus.Ringing || call.Status == CallStatus.Initiated)
        {
            var isCancelledOrNoAns = string.Equals(request.DispositionName, "NoAnswer", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.DispositionName, "Cancelled", StringComparison.OrdinalIgnoreCase);

            call.Status = isCancelledOrNoAns ? CallStatus.Abandoned : CallStatus.Completed;
        }
        else if (call.Status != CallStatus.Abandoned)
        {
            call.Status = CallStatus.Completed;
        }

        if (!call.EndedAt.HasValue)
        {
            call.EndedAt = DateTimeOffset.UtcNow;
            if (call.AnsweredAt.HasValue)
            {
                call.TalkDurationSeconds = (int)(call.EndedAt.Value - call.AnsweredAt.Value).TotalSeconds;
            }
            call.TotalDurationSeconds = (int)(call.EndedAt.Value - call.InitiatedAt).TotalSeconds;
        }

        if (!string.IsNullOrWhiteSpace(request.DestinationNumber) && 
            (string.IsNullOrWhiteSpace(call.DestinationNumber) || call.DestinationNumber == "Direct Outbound"))
        {
            call.DestinationNumber = request.DestinationNumber;
        }

        // 3. Resolve and Link Disposition ID
        if (request.DispositionId.HasValue && request.DispositionId.Value != Guid.Empty)
        {
            call.DispositionId = request.DispositionId;
        }
        else if (!string.IsNullOrWhiteSpace(request.DispositionName))
        {
            var dispName = request.DispositionName.Trim();
            var dispLower = dispName.ToLowerInvariant();
            var dispNormalized = dispLower.Replace("_", "").Replace(" ", "").Replace("-", "");

            var allDispositions = await _db.Dispositions.ToListAsync();
            var matched = allDispositions.FirstOrDefault(d => 
                d.Code.ToLowerInvariant() == dispLower ||
                d.Code.ToLowerInvariant().Replace("_", "") == dispNormalized ||
                d.Description.ToLowerInvariant().Contains(dispLower) ||
                d.Category.ToLowerInvariant() == dispLower);

            if (matched == null)
            {
                // Auto-create disposition if it doesn't exist yet
                matched = new Disposition
                {
                    DispositionId = Guid.NewGuid(),
                    Code = dispName.ToUpperInvariant().Replace(" ", "_"),
                    Description = dispName,
                    Category = "General",
                    IsActive = true
                };
                _db.Dispositions.Add(matched);
                await _db.SaveChangesAsync();
            }

            call.DispositionId = matched.DispositionId;
        }

        // 4. Save Agent Notes
        call.AgentNotes = request.Notes;

        // Commit all changes to Microsoft SQL Server
        await _db.SaveChangesAsync();

        // 5. Synchronize with CRM
        try
        {
            await _crm.SyncCallLogAsync(call);
        }
        catch { }

        return Ok(new
        {
            Message = "Call and disposition saved successfully in MS SQL Server.",
            CallId = call.CallId,
            CallUuid = call.CallUuid,
            DestinationNumber = call.DestinationNumber,
            Status = call.Status.ToString(),
            DispositionId = call.DispositionId,
            AgentNotes = call.AgentNotes
        });
    }
}
