using System.Text;
using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public AuditLogsController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> SearchAuditLogs(
        [FromQuery] string? entity,
        [FromQuery] string? action,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.AuditLogs.Include(l => l.User).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(l => l.Entity.ToLower().Contains(entity.ToLower()));
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(l => l.Action.ToLower().Contains(action.ToLower()));
        if (fromDate.HasValue) query = query.Where(l => l.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(l => l.CreatedAt <= toDate.Value);

        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                logId = l.LogId,
                userId = l.UserId,
                username = l.User != null ? l.User.Username : "System",
                action = l.Action,
                entity = l.Entity,
                entityId = l.EntityId,
                oldValue = l.OldValue,
                newValue = l.NewValue,
                ipAddress = l.IPAddress,
                createdAt = l.CreatedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, data = logs });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuditLogDetail(Guid id)
    {
        var log = await _db.AuditLogs.Include(l => l.User).FirstOrDefaultAsync(l => l.LogId == id);
        if (log == null) return NotFound(new { message = "Audit log not found" });

        return Ok(new
        {
            logId = log.LogId,
            userId = log.UserId,
            username = log.User?.Username ?? "System",
            action = log.Action,
            entity = log.Entity,
            entityId = log.EntityId,
            oldValue = log.OldValue,
            newValue = log.NewValue,
            ipAddress = log.IPAddress,
            createdAt = log.CreatedAt
        });
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAuditLogs()
    {
        var logs = await _db.AuditLogs.OrderByDescending(l => l.CreatedAt).Take(200).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("LogId,Action,Entity,EntityId,IPAddress,CreatedAt");
        foreach (var l in logs)
        {
            sb.AppendLine($"{l.LogId},{l.Action},{l.Entity},{l.EntityId},{l.IPAddress},{l.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"audit_logs_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
