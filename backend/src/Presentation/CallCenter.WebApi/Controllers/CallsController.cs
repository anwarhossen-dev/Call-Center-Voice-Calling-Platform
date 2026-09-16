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

[ApiController]
[Route("api/[controller]")]
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
}
