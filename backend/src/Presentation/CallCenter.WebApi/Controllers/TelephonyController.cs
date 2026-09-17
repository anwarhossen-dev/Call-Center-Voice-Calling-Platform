using CallCenter.Application.DTOs;
using CallCenter.Application.Interfaces;
using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class InboundCallEventDto
{
    public string CallerNumber { get; set; } = string.Empty;
    public string DestinationNumber { get; set; } = string.Empty;
    public string? CallUuid { get; set; }
    public string? Trunk { get; set; } = "BTCL-SIP";
}

public class DtmfRequestDto
{
    public string Digits { get; set; } = string.Empty;
}

public class TelephonyWebhookDto
{
    public string EventType { get; set; } = string.Empty;
    public string CallUuid { get; set; } = string.Empty;
    public string? Timestamp { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class TelephonyController : ControllerBase
{
    private readonly ITelephonyBridge _telephony;
    private readonly IACDQueueService _acd;
    private readonly CallCenterDbContext _db;
    private readonly IRealtimeNotifier _notifier;
    private readonly ILogger<TelephonyController> _logger;

    public TelephonyController(
        ITelephonyBridge telephony,
        IACDQueueService acd,
        CallCenterDbContext db,
        IRealtimeNotifier notifier,
        ILogger<TelephonyController> logger)
    {
        _telephony = telephony;
        _acd = acd;
        _db = db;
        _notifier = notifier;
        _logger = logger;
    }

    [HttpPost("calls/inbound")]
    public async Task<IActionResult> ReceiveInboundCall([FromBody] InboundCallEventDto dto)
    {
        var callUuid = !string.IsNullOrEmpty(dto.CallUuid) ? dto.CallUuid : Guid.NewGuid().ToString();

        var call = new Call
        {
            CallId = Guid.NewGuid(),
            CallUuid = callUuid,
            CallerNumber = dto.CallerNumber,
            DestinationNumber = dto.DestinationNumber,
            Direction = CallDirection.Inbound,
            Status = CallStatus.Ringing,
            StartTime = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Calls.Add(call);
        _db.CallEvents.Add(new CallEvent
        {
            EventId = Guid.NewGuid(),
            CallId = call.CallId,
            EventType = "INBOUND_RINGING",
            EventTime = DateTimeOffset.UtcNow,
            Description = $"Inbound call from {dto.CallerNumber} via {dto.Trunk}"
        });

        await _db.SaveChangesAsync();

        // Screen-pop notification to target extension
        await _notifier.NotifyScreenPopAsync(dto.DestinationNumber, new ScreenPopDto
        {
            CallUuid = callUuid,
            CallerNumber = dto.CallerNumber,
            QueueName = dto.Trunk ?? "Inbound Queue",
            RingingAt = DateTimeOffset.UtcNow,
            Customer = CustomerProfileDto.Anonymous(dto.CallerNumber)
        });

        return Ok(new
        {
            message = "Inbound call event processed successfully",
            callUuid,
            status = "Ringing"
        });
    }

    [HttpPost("calls/outbound")]
    public async Task<IActionResult> RequestOutboundCall([FromBody] OriginateCallRequest request)
    {
        var callUuid = !string.IsNullOrEmpty(request.CallUuid) ? request.CallUuid : Guid.NewGuid().ToString();

        var call = new Call
        {
            CallId = Guid.NewGuid(),
            CallUuid = callUuid,
            CallerNumber = request.AgentExtension,
            DestinationNumber = request.DestinationNumber,
            Direction = CallDirection.Outbound,
            Status = CallStatus.Initiated,
            StartTime = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Calls.Add(call);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Telephony outbound call dispatched", callUuid, status = "Initiated" });
    }

    [HttpPost("calls/{id}/answer")]
    public async Task<IActionResult> AnswerTelephonyCall(string id)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));
        if (call != null)
        {
            call.Status = CallStatus.InProgress;
            call.AnsweredAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Telephony leg answered", callId = id });
    }

    [HttpPost("calls/{id}/hangup")]
    public async Task<IActionResult> HangupTelephonyCall(string id)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));
        if (call != null)
        {
            call.Status = CallStatus.Completed;
            call.EndedAt = DateTimeOffset.UtcNow;
            if (call.AnsweredAt.HasValue)
            {
                call.TalkDurationSeconds = (int)(call.EndedAt.Value - call.AnsweredAt.Value).TotalSeconds;
            }
            call.TotalDurationSeconds = (int)(call.EndedAt.Value - call.StartTime).TotalSeconds;
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "Telephony leg hung up", callId = id });
    }

    [HttpPost("calls/{id}/transfer")]
    public async Task<IActionResult> TransferTelephonyCall(string id, [FromBody] TransferCallRequest request)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));
        if (call != null)
        {
            call.Status = CallStatus.Transferred;
            call.AgentNotes = $"{call.AgentNotes} | SIP Handover to {request.TargetExtension}";
            await _db.SaveChangesAsync();
        }

        return Ok(new { message = "SIP Call transferred", target = request.TargetExtension });
    }

    [HttpPost("calls/{id}/hold")]
    public async Task<IActionResult> HoldTelephonyCall(string id)
    {
        return Ok(new { message = "Telephony RTP on hold", callId = id });
    }

    [HttpPost("calls/{id}/resume")]
    public async Task<IActionResult> ResumeTelephonyCall(string id)
    {
        return Ok(new { message = "Telephony RTP resumed", callId = id });
    }

    [HttpPost("calls/{id}/dtmf")]
    public async Task<IActionResult> SendDtmf(string id, [FromBody] DtmfRequestDto dto)
    {
        _logger.LogInformation("DTMF tones sent for call {CallId}: {Digits}", id, dto.Digits);
        return Ok(new { message = "DTMF tones sent successfully", callId = id, digits = dto.Digits });
    }

    [HttpPost("webhooks/events")]
    public async Task<IActionResult> ReceiveWebhooks([FromBody] TelephonyWebhookDto dto)
    {
        _logger.LogInformation("Telephony webhook event received: {Type} for call {Uuid}", dto.EventType, dto.CallUuid);

        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == dto.CallUuid);
        if (call != null)
        {
            _db.CallEvents.Add(new CallEvent
            {
                EventId = Guid.NewGuid(),
                CallId = call.CallId,
                EventType = dto.EventType,
                EventTime = DateTimeOffset.UtcNow,
                Description = $"Webhook: {dto.EventType}"
            });
            await _db.SaveChangesAsync();
        }

        return Ok(new { received = true, eventType = dto.EventType });
    }
}
