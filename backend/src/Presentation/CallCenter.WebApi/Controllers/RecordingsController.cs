using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordingsController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public RecordingsController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRecordings([FromQuery] int limit = 50)
    {
        var recordings = await _db.CallRecordings
            .Include(r => r.Call)
            .OrderByDescending(r => r.OffloadedAt)
            .Take(limit)
            .ToListAsync();
        return Ok(recordings);
    }

    [HttpGet("{recordingId}")]
    public async Task<IActionResult> GetRecording(Guid recordingId)
    {
        var rec = await _db.CallRecordings.FindAsync(recordingId);
        if (rec == null) return NotFound();
        return Ok(rec);
    }
}
