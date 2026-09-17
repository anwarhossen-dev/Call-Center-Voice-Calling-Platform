using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class UploadRecordingDto
{
    public string CallUuid { get; set; } = string.Empty;
    public string CustomerName { get; set; } = "Direct Caller";
    public string PhoneNumber { get; set; } = "N/A";
    public int DurationSeconds { get; set; }
    public IFormFile? AudioFile { get; set; }
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class RecordingsController : ControllerBase
{
    private readonly CallCenterDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<RecordingsController> _logger;

    public RecordingsController(CallCenterDbContext db, IWebHostEnvironment env, ILogger<RecordingsController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRecordings([FromQuery] int limit = 100)
    {
        var recordings = await _db.CallRecordings
            .Include(r => r.Call)
            .OrderByDescending(r => r.OffloadedAt)
            .Take(limit)
            .ToListAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var result = recordings.Select(r => new
        {
            id = r.RecordingId.ToString(),
            callUuid = r.Call?.CallUuid ?? r.StoragePath.Replace(".webm", "").Replace("/recordings/", ""),
            customerName = !string.IsNullOrEmpty(r.Call?.CRMContactId) ? r.Call.CRMContactId : "Direct Caller",
            phoneNumber = r.Call != null ? (r.Call.Direction == CallDirection.Inbound ? r.Call.CallerNumber : r.Call.DestinationNumber) : "N/A",
            durationSeconds = r.DurationSeconds > 0 ? r.DurationSeconds : 1,
            audioUrl = r.StoragePath.StartsWith("http") ? r.StoragePath : $"{baseUrl}{r.StoragePath}",
            timestamp = r.OffloadedAt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt"),
            fileSizeBytes = r.FileSizeBytes,
            channels = r.AudioChannels
        });

        return Ok(result);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadRecording([FromForm] UploadRecordingDto dto)
    {
        if (dto.AudioFile == null || dto.AudioFile.Length == 0)
        {
            return BadRequest(new { Message = "Audio file is required." });
        }

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var recordingsDir = Path.Combine(webRoot, "recordings");
        if (!Directory.Exists(recordingsDir))
        {
            Directory.CreateDirectory(recordingsDir);
        }

        var safeUuid = !string.IsNullOrWhiteSpace(dto.CallUuid) ? dto.CallUuid : ("call-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var fileName = $"{safeUuid}.webm";
        var filePath = Path.Combine(recordingsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.AudioFile.CopyToAsync(stream);
        }

        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == safeUuid);
        if (call == null)
        {
            call = new Call
            {
                CallUuid = safeUuid,
                Direction = CallDirection.Outbound,
                CallerNumber = "1001",
                DestinationNumber = dto.PhoneNumber,
                CRMContactId = dto.CustomerName,
                Status = CallStatus.Completed,
                TotalDurationSeconds = dto.DurationSeconds,
                InitiatedAt = DateTimeOffset.UtcNow.AddSeconds(-dto.DurationSeconds),
                EndedAt = DateTimeOffset.UtcNow
            };
            _db.Calls.Add(call);
            await _db.SaveChangesAsync();
        }

        var recording = await _db.CallRecordings.FirstOrDefaultAsync(r => r.CallId == call.CallId);
        if (recording == null)
        {
            recording = new CallRecording
            {
                RecordingId = Guid.NewGuid(),
                CallId = call.CallId,
                StorageBucket = "local-storage",
                StoragePath = $"/recordings/{fileName}",
                FileSizeBytes = dto.AudioFile.Length,
                DurationSeconds = dto.DurationSeconds,
                AudioChannels = "Stereo (Left/Right)",
                IsArchived = true,
                OffloadedAt = DateTimeOffset.UtcNow
            };
            _db.CallRecordings.Add(recording);
        }
        else
        {
            recording.StoragePath = $"/recordings/{fileName}";
            recording.FileSizeBytes = dto.AudioFile.Length;
            recording.DurationSeconds = dto.DurationSeconds;
            recording.OffloadedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var permanentUrl = $"{baseUrl}/recordings/{fileName}";

        _logger.LogInformation("Audio recording permanently saved to disk at {Path} with URL {Url}", filePath, permanentUrl);

        return Ok(new
        {
            Success = true,
            RecordingId = recording.RecordingId,
            CallUuid = safeUuid,
            AudioUrl = permanentUrl,
            DurationSeconds = dto.DurationSeconds,
            FileSizeBytes = dto.AudioFile.Length,
            Timestamp = recording.OffloadedAt.ToLocalTime().ToString("dd MMM yyyy, hh:mm tt")
        });
    }

    [HttpGet("{recordingId}")]
    public async Task<IActionResult> GetRecording(string recordingId)
    {
        Guid? recGuid = Guid.TryParse(recordingId, out var g) ? g : null;
        var rec = await _db.CallRecordings
            .Include(r => r.Call)
            .FirstOrDefaultAsync(r => (recGuid.HasValue && r.RecordingId == recGuid.Value) || (r.Call != null && r.Call.CallUuid == recordingId));
        if (rec == null) return NotFound(new { message = "Recording not found" });

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new
        {
            recordingId = rec.RecordingId,
            callId = rec.CallId,
            callUuid = rec.Call?.CallUuid,
            filePath = rec.FilePath,
            fileName = rec.FileName,
            duration = rec.Duration,
            fileSize = rec.FileSize,
            storageUrl = rec.StoragePath.StartsWith("http") ? rec.StoragePath : $"{baseUrl}{rec.StoragePath}",
            createdAt = rec.CreatedAt
        });
    }

    [HttpGet("{recordingId}/stream")]
    public async Task<IActionResult> StreamRecording(string recordingId)
    {
        Guid? recGuid = Guid.TryParse(recordingId, out var g) ? g : null;
        var rec = await _db.CallRecordings
            .Include(r => r.Call)
            .FirstOrDefaultAsync(r => (recGuid.HasValue && r.RecordingId == recGuid.Value) || (r.Call != null && r.Call.CallUuid == recordingId));
        if (rec == null) return NotFound(new { message = "Recording not found" });

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var path = Path.Combine(webRoot, rec.StoragePath.TrimStart('/'));
        if (System.IO.File.Exists(path))
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "audio/webm", enableRangeProcessing: true);
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Redirect(rec.StoragePath.StartsWith("http") ? rec.StoragePath : $"{baseUrl}{rec.StoragePath}");
    }

    [HttpGet("{recordingId}/download")]
    public async Task<IActionResult> DownloadRecording(string recordingId)
    {
        Guid? recGuid = Guid.TryParse(recordingId, out var g) ? g : null;
        var rec = await _db.CallRecordings
            .Include(r => r.Call)
            .FirstOrDefaultAsync(r => (recGuid.HasValue && r.RecordingId == recGuid.Value) || (r.Call != null && r.Call.CallUuid == recordingId));
        if (rec == null) return NotFound(new { message = "Recording not found" });

        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var path = Path.Combine(webRoot, rec.StoragePath.TrimStart('/'));
        if (System.IO.File.Exists(path))
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "application/octet-stream", $"{rec.Call?.CallUuid ?? recordingId}.webm");
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Redirect(rec.StoragePath.StartsWith("http") ? rec.StoragePath : $"{baseUrl}{rec.StoragePath}");
    }

    [HttpDelete("{recordingId}")]
    public async Task<IActionResult> DeleteRecording(string recordingId)
    {
        Guid? recGuid = Guid.TryParse(recordingId, out var g) ? g : null;
        var rec = await _db.CallRecordings
            .Include(r => r.Call)
            .FirstOrDefaultAsync(r => (recGuid.HasValue && r.RecordingId == recGuid.Value) || (r.Call != null && r.Call.CallUuid == recordingId));
        if (rec == null) return NotFound(new { message = "Recording not found" });

        rec.IsArchived = true;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Recording deleted/archived successfully." });
    }
}
