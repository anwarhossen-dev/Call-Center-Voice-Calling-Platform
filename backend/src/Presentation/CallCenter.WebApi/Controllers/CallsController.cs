using System.Text;
using System.Net.Http.Headers;
using CallCenter.Application.DTOs;
using CallCenter.Application.Interfaces;
using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class TwilioConfigDto
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}

public class TransferCallRequest
{
    public string TargetExtension { get; set; } = string.Empty;
    public string TransferMode { get; set; } = "Blind";
    public string? Reason { get; set; }
    public string? CustomerName { get; set; }
    public string? DestinationNumber { get; set; }
}

public class SaveDispositionRequest
{
    public Guid? DispositionId { get; set; }
    public string? DispositionCode { get; set; }
    public string? Notes { get; set; }
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class CallsController : ControllerBase
{
    private static TwilioConfigDto _twilioConfig = new()
    {
        AccountSid = "",
        AuthToken = "",
        FromPhoneNumber = "",
        Enabled = false
    };

    private readonly ITelephonyBridge _telephony;
    private readonly ICrmService _crm;
    private readonly IACDQueueService _acd;
    private readonly IRealtimeNotifier _notifier;
    private readonly CallCenterDbContext _db;
    private readonly ILogger<CallsController> _logger;

    public CallsController(
        ITelephonyBridge telephony,
        ICrmService crm,
        IACDQueueService acd,
        IRealtimeNotifier notifier,
        CallCenterDbContext db,
        ILogger<CallsController> logger)
    {
        _telephony = telephony;
        _crm = crm;
        _acd = acd;
        _notifier = notifier;
        _db = db;
        _logger = logger;
    }

    [HttpPost("originate")]
    public async Task<IActionResult> OriginateOutbound([FromBody] OriginateCallRequest request)
    {
        var callUuid = !string.IsNullOrWhiteSpace(request.CallUuid) ? request.CallUuid : Guid.NewGuid().ToString();
        var customerProfile = await _crm.LookupCustomerAsync(request.DestinationNumber);
        string carrierStatus = "Ringing";
        string carrierNotice = "Internal PBX / FreeSWITCH Leg";

        // 1. If Twilio Carrier is enabled and configured, dispatch live telephone call to real mobile network
        if (_twilioConfig.Enabled && !string.IsNullOrWhiteSpace(_twilioConfig.AccountSid) && !string.IsNullOrWhiteSpace(_twilioConfig.AuthToken))
        {
            try
            {
                using var httpClient = new HttpClient();
                var authBytes = Encoding.ASCII.GetBytes($"{_twilioConfig.AccountSid}:{_twilioConfig.AuthToken}");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

                var twimlPayload = "<Response><Say voice=\"alice\">Hello! This is a live voice test from your Enterprise Call Center Voice Platform. Your real-time telephony setup is working perfectly. Have a great day!</Say><Record maxLength=\"30\"/></Response>";
                var formValues = new Dictionary<string, string>
                {
                    { "To", request.DestinationNumber },
                    { "From", _twilioConfig.FromPhoneNumber },
                    { "Twiml", twimlPayload }
                };

                var response = await httpClient.PostAsync($"https://api.twilio.com/2010-04-01/Accounts/{_twilioConfig.AccountSid}/Calls.json", new FormUrlEncodedContent(formValues));
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Real phone call dispatched via Twilio to {Phone}: {Resp}", request.DestinationNumber, responseContent);
                    carrierNotice = "Live Call placed via Twilio carrier network to destination phone.";
                }
                else
                {
                    _logger.LogWarning("Twilio dispatch returned {Code}: {Body}", response.StatusCode, responseContent);
                    carrierNotice = $"Twilio Notice ({response.StatusCode}): Running in interactive local mode.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Twilio dispatch exception");
                carrierNotice = "Twilio exception: " + ex.Message;
            }
        }
        else
        {
            var bridgeUuid = await _telephony.OriginateCallAsync(request.AgentExtension, request.DestinationNumber);
            if (string.IsNullOrWhiteSpace(request.CallUuid))
            {
                callUuid = bridgeUuid;
            }
        }
        
        var call = new Call
        {
            CallUuid = callUuid,
            Direction = CallDirection.Outbound,
            CallerNumber = request.AgentExtension,
            DestinationNumber = request.DestinationNumber,
            Status = CallStatus.Ringing,
            CRMContactId = !string.IsNullOrEmpty(request.CustomerId) ? request.CustomerId : customerProfile.CustomerId
        };

        _db.Calls.Add(call);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            CallUuid = callUuid,
            Status = carrierStatus,
            Customer = customerProfile,
            CarrierNotice = carrierNotice,
            TwilioEnabled = _twilioConfig.Enabled
        });
    }

    [HttpGet("twilio/config")]
    public IActionResult GetTwilioConfig()
    {
        return Ok(new
        {
            AccountSid = _twilioConfig.AccountSid,
            HasAuthToken = !string.IsNullOrEmpty(_twilioConfig.AuthToken),
            FromPhoneNumber = _twilioConfig.FromPhoneNumber,
            Enabled = _twilioConfig.Enabled
        });
    }

    [HttpPost("twilio/config")]
    public IActionResult UpdateTwilioConfig([FromBody] TwilioConfigDto config)
    {
        _twilioConfig = config;
        _logger.LogInformation("Twilio settings updated. Enabled={Enabled}, From={From}", config.Enabled, config.FromPhoneNumber);
        return Ok(new
        {
            Message = "Twilio configuration updated successfully.",
            Config = new { config.AccountSid, config.FromPhoneNumber, config.Enabled }
        });
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> LookupCustomer([FromQuery] string phoneNumber)
    {
        var profile = await _crm.LookupCustomerAsync(phoneNumber);
        return Ok(profile);
    }

    [HttpPost("simulate-inbound")]
    public async Task<IActionResult> SimulateInbound([FromQuery] string callerNumber = "+8801712345678", [FromQuery] string did = "09612345678")
    {
        var callUuid = Guid.NewGuid().ToString();
        _logger.LogInformation("Simulating inbound call from {Caller} to DID {Did}", callerNumber, did);

        // 1. CRM Lookup
        var customerProfile = await _crm.LookupCustomerAsync(callerNumber);

        // 2. ACD Route selection
        var defaultQueueId = Guid.NewGuid();
        var agent = await _acd.FindBestAgentForQueueAsync(defaultQueueId);
        var targetExtension = agent?.Extension ?? "1001";

        // 3. Screen-Pop push via SignalR
        var screenPop = new ScreenPopDto
        {
            CallUuid = callUuid,
            CallerNumber = callerNumber,
            QueueName = "Inbound Support",
            Customer = customerProfile
        };

        await _notifier.NotifyScreenPopAsync(targetExtension, screenPop);

        var call = new Call
        {
            CallUuid = callUuid,
            Direction = CallDirection.Inbound,
            CallerNumber = callerNumber,
            DestinationNumber = did,
            Status = CallStatus.Ringing,
            CRMContactId = customerProfile.CustomerId
        };

        _db.Calls.Add(call);
        await _db.SaveChangesAsync();

        return Ok(new { CallUuid = callUuid, TargetAgent = targetExtension, Customer = customerProfile });
    }

    [HttpPost("{callUuid}/hold")]
    public async Task<IActionResult> HoldCall(string callUuid, [FromQuery] bool hold = true)
    {
        var result = await _telephony.HoldCallAsync(callUuid, hold);
        return Ok(new { CallUuid = callUuid, Hold = hold, Success = result });
    }

    [HttpPost("{callUuid}/terminate")]
    public async Task<IActionResult> TerminateCall(string callUuid)
    {
        var result = await _telephony.TerminateCallAsync(callUuid);
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == callUuid);
        if (call != null)
        {
            call.Status = (call.Status == CallStatus.Ringing || call.Status == CallStatus.Initiated) 
                ? CallStatus.Abandoned 
                : CallStatus.Completed;
            call.EndedAt = DateTimeOffset.UtcNow;
            if (call.AnsweredAt.HasValue)
            {
                call.TalkDurationSeconds = (int)(call.EndedAt.Value - call.AnsweredAt.Value).TotalSeconds;
            }
            call.TotalDurationSeconds = (int)(call.EndedAt.Value - call.InitiatedAt).TotalSeconds;
            await _db.SaveChangesAsync();
        }
        return Ok(new { CallUuid = callUuid, Status = call?.Status.ToString() ?? "Completed", Success = result });
    }

    [HttpPost("{callUuid}/transfer")]
    public async Task<IActionResult> TransferCall(string callUuid, [FromBody] TransferCallRequest request)
    {
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == callUuid);
        if (call == null)
        {
            call = new Call
            {
                CallUuid = callUuid,
                Direction = CallDirection.Outbound,
                CallerNumber = "1001",
                DestinationNumber = !string.IsNullOrWhiteSpace(request.DestinationNumber) ? request.DestinationNumber : request.TargetExtension,
                CRMContactId = !string.IsNullOrWhiteSpace(request.CustomerName) ? request.CustomerName : "Customer",
                InitiatedAt = DateTimeOffset.UtcNow.AddSeconds(-10)
            };
            _db.Calls.Add(call);
        }

        call.Status = CallStatus.Transferred;
        call.EndedAt = DateTimeOffset.UtcNow;
        if (call.AnsweredAt.HasValue)
        {
            call.TalkDurationSeconds = (int)(call.EndedAt.Value - call.AnsweredAt.Value).TotalSeconds;
        }
        call.TotalDurationSeconds = (int)(call.EndedAt.Value - call.InitiatedAt).TotalSeconds;
        var transferLog = $"[Transferred] Handed over to Ext: {request.TargetExtension} ({request.TransferMode} Transfer). {request.Reason}";
        call.AgentNotes = string.IsNullOrWhiteSpace(call.AgentNotes) ? transferLog : $"{call.AgentNotes} | {transferLog}";

        await _db.SaveChangesAsync();
        _logger.LogInformation("Call {Uuid} successfully transferred to {Ext} ({Mode}) and persisted to DB", callUuid, request.TargetExtension, request.TransferMode);

        return Ok(new
        {
            Success = true,
            CallUuid = callUuid,
            Status = "Transferred",
            TargetExtension = request.TargetExtension,
            TransferMode = request.TransferMode
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetCallHistory([FromQuery] int limit = 100, [FromQuery] string? search = null, [FromQuery] string? direction = null)
    {
        var query = _db.Calls
            .Include(c => c.Disposition)
            .AsNoTracking()
            .OrderByDescending(c => c.InitiatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => 
                c.DestinationNumber.ToLower().Contains(s) ||
                c.CallerNumber.ToLower().Contains(s) ||
                (c.AgentNotes != null && c.AgentNotes.ToLower().Contains(s)) ||
                (c.Disposition != null && c.Disposition.Code.ToLower().Contains(s)) ||
                (c.CRMContactId != null && c.CRMContactId.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(direction) && Enum.TryParse<CallDirection>(direction, true, out var dirEnum))
        {
            query = query.Where(c => c.Direction == dirEnum);
        }

        var list = await query.Take(limit).ToListAsync();

        var result = list.Select(c => new
        {
            id = c.CallId.ToString(),
            callUuid = c.CallUuid,
            number = c.Direction == CallDirection.Inbound ? c.CallerNumber : c.DestinationNumber,
            callerNumber = c.CallerNumber,
            destinationNumber = c.DestinationNumber,
            direction = c.Direction.ToString(),
            durationSeconds = c.TotalDurationSeconds > 0 ? c.TotalDurationSeconds : (c.TalkDurationSeconds > 0 ? c.TalkDurationSeconds : 0),
            status = c.Status.ToString(),
            statusCode = (int)c.Status,
            timestamp = c.InitiatedAt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt"),
            initiatedAt = c.InitiatedAt,
            endedAt = c.EndedAt,
            disposition = c.Disposition != null ? c.Disposition.Code : (c.Status == CallStatus.Abandoned ? "CANCELLED" : "COMPLETED"),
            dispositionDescription = c.Disposition?.Description ?? "",
            notes = c.AgentNotes ?? "",
            customerId = c.CRMContactId ?? "General Caller",
            customerName = !string.IsNullOrEmpty(c.CRMContactId) ? c.CRMContactId : "Direct Caller"
        });

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> SearchCalls(
        [FromQuery] string? status,
        [FromQuery] Guid? agentId,
        [FromQuery] Guid? queueId,
        [FromQuery] Guid? campaignId,
        [FromQuery] string? callType,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Calls
            .Include(c => c.Agent)
            .Include(c => c.Queue)
            .Include(c => c.Campaign)
            .Include(c => c.Disposition)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CallStatus>(status, true, out var st))
        {
            query = query.Where(c => c.Status == st);
        }

        if (agentId.HasValue) query = query.Where(c => c.AgentId == agentId.Value);
        if (queueId.HasValue) query = query.Where(c => c.QueueId == queueId.Value);
        if (campaignId.HasValue) query = query.Where(c => c.CampaignId == campaignId.Value);
        if (fromDate.HasValue) query = query.Where(c => c.StartTime >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(c => c.StartTime <= toDate.Value);

        var totalCount = await query.CountAsync();
        var calls = await query
            .OrderByDescending(c => c.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                callId = c.CallId,
                callUuid = c.CallUuid,
                callerNumber = c.CallerNumber,
                destinationNumber = c.DestinationNumber,
                direction = c.Direction.ToString(),
                type = c.Type,
                status = c.Status.ToString(),
                duration = c.Duration > 0 ? c.Duration : c.TotalDurationSeconds,
                talkDuration = c.TalkDurationSeconds,
                agentId = c.AgentId,
                agentName = c.Agent != null ? c.Agent.DisplayName : null,
                queueName = c.Queue != null ? c.Queue.QueueName : null,
                campaignName = c.Campaign != null ? c.Campaign.CampaignName : null,
                disposition = c.Disposition != null ? c.Disposition.DispositionName : null,
                crmContactId = c.CRMContactId,
                startTime = c.StartTime,
                endTime = c.EndTime
            })
            .ToListAsync();

        return Ok(new { total = totalCount, page, pageSize, data = calls });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCallDetails(string id)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls
            .Include(c => c.Agent)
            .Include(c => c.Queue)
            .Include(c => c.Campaign)
            .Include(c => c.Disposition)
            .Include(c => c.Recording)
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));

        if (call == null) return NotFound(new { message = "Call not found." });

        return Ok(new
        {
            callId = call.CallId,
            callUuid = call.CallUuid,
            callerNumber = call.CallerNumber,
            destinationNumber = call.DestinationNumber,
            direction = call.Direction.ToString(),
            type = call.Type,
            status = call.Status.ToString(),
            duration = call.Duration,
            talkDuration = call.TalkDurationSeconds,
            holdDuration = call.HoldDurationSeconds,
            waitDuration = call.WaitDurationSeconds,
            agentId = call.AgentId,
            agentName = call.Agent?.DisplayName,
            queueId = call.QueueId,
            queueName = call.Queue?.QueueName,
            campaignId = call.CampaignId,
            campaignName = call.Campaign?.CampaignName,
            disposition = call.Disposition?.DispositionName,
            crmContactId = call.CRMContactId,
            agentNotes = call.AgentNotes,
            startTime = call.StartTime,
            answeredAt = call.AnsweredAt,
            endTime = call.EndTime,
            hasRecording = call.Recording != null,
            eventsCount = call.Events.Count
        });
    }

    [HttpPost("outbound")]
    public async Task<IActionResult> InitiateOutboundCall([FromBody] OriginateCallRequest request)
    {
        return await OriginateOutbound(request);
    }

    [HttpPost("{id}/answer")]
    public async Task<IActionResult> AnswerCallById(string id)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));
        if (call != null)
        {
            call.Status = CallStatus.InProgress;
            call.AnsweredAt = DateTimeOffset.UtcNow;
            _db.CallEvents.Add(new CallEvent { EventId = Guid.NewGuid(), CallId = call.CallId, EventType = "ANSWERED", EventTime = DateTimeOffset.UtcNow, Description = "Call answered by agent" });
            await _db.SaveChangesAsync();
            return Ok(new { message = "Call answered", callUuid = call.CallUuid, status = "InProgress" });
        }
        return Ok(new { message = "Call answered", callUuid = id, status = "InProgress" });
    }

    [HttpPost("{id}/hangup")]
    public async Task<IActionResult> HangupCallById(string id, [FromQuery] string? reason)
    {
        return await TerminateCall(id);
    }

    [HttpPost("{id}/resume")]
    public async Task<IActionResult> ResumeCall(string id)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));
        if (call != null)
        {
            _db.CallEvents.Add(new CallEvent { EventId = Guid.NewGuid(), CallId = call.CallId, EventType = "RESUME", EventTime = DateTimeOffset.UtcNow, Description = "Call resumed from hold" });
            await _db.SaveChangesAsync();
        }
        return Ok(new { message = "Call resumed", callId = id, isHold = false });
    }

    [HttpPost("{id}/mute")]
    public async Task<IActionResult> MuteCall(string id)
    {
        return Ok(new { message = "Call muted", callId = id, isMuted = true });
    }

    [HttpPost("{id}/unmute")]
    public async Task<IActionResult> UnmuteCall(string id)
    {
        return Ok(new { message = "Call unmuted", callId = id, isMuted = false });
    }

    [HttpPost("{id}/disposition")]
    public async Task<IActionResult> SaveCallDisposition(string id, [FromBody] SaveDispositionRequest request)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));
        if (call == null) return NotFound(new { message = "Call not found." });

        if (request.DispositionId.HasValue)
        {
            call.DispositionId = request.DispositionId;
        }
        else if (!string.IsNullOrWhiteSpace(request.DispositionCode))
        {
            var disp = await _db.Dispositions.FirstOrDefaultAsync(d => d.Code == request.DispositionCode || d.DispositionName == request.DispositionCode);
            if (disp != null) call.DispositionId = disp.DispositionId;
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            call.AgentNotes = string.IsNullOrWhiteSpace(call.AgentNotes) ? request.Notes : $"{call.AgentNotes} | {request.Notes}";
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Disposition saved successfully", callId = call.CallId, dispositionId = call.DispositionId });
    }

    [HttpGet("{id}/events")]
    public async Task<IActionResult> GetCallEvents(string id)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));
        if (call == null) return NotFound(new { message = "Call not found." });

        var events = await _db.CallEvents
            .Where(e => e.CallId == call.CallId)
            .OrderBy(e => e.EventTime)
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id}/recording")]
    public async Task<IActionResult> GetCallRecording(string id)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls.Include(c => c.Recording).FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));
        if (call == null) return NotFound(new { message = "Call not found." });

        if (call.Recording != null)
        {
            return Ok(call.Recording);
        }

        var rec = await _db.CallRecordings.FirstOrDefaultAsync(r => r.CallId == call.CallId);
        if (rec != null) return Ok(rec);

        return NotFound(new { message = "No recording found for this call." });
    }
}
