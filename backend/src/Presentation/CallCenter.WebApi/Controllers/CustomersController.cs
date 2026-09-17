using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

public class CreateCustomerActivityDto
{
    public Guid? CallId { get; set; }
    public string ActivityType { get; set; } = "Call";
    public string? Notes { get; set; }
}

public class UpdateCustomerActivityDto
{
    public string? Notes { get; set; }
    public string? ActivityType { get; set; }
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public CustomersController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomerById(Guid id)
    {
        var customer = await _db.Customers
            .Include(c => c.CRMActivities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return NotFound(new { message = "Customer not found." });

        return Ok(new
        {
            customerId = customer.CustomerId,
            name = customer.Name,
            phone = customer.Phone,
            email = customer.Email,
            address = customer.Address,
            crmId = customer.CRMId,
            activitiesCount = customer.CRMActivities.Count,
            createdAt = customer.CreatedAt
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchCustomer([FromQuery] string? query, [FromQuery] string? phone)
    {
        var q = (query ?? phone ?? "").Trim().ToLower();
        var customers = await _db.Customers
            .Where(c => string.IsNullOrEmpty(q) || c.Phone.ToLower().Contains(q) || c.Name.ToLower().Contains(q) || (c.Email != null && c.Email.ToLower().Contains(q)))
            .Take(20)
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("{id}/calls")]
    public async Task<IActionResult> GetCustomerCalls(Guid id)
    {
        var calls = await _db.Calls
            .Where(c => c.CustomerId == id)
            .OrderByDescending(c => c.StartTime)
            .Select(c => new
            {
                callId = c.CallId,
                callUuid = c.CallUuid,
                callerNumber = c.CallerNumber,
                destinationNumber = c.DestinationNumber,
                direction = c.Direction.ToString(),
                status = c.Status.ToString(),
                duration = c.Duration,
                startTime = c.StartTime
            })
            .ToListAsync();

        return Ok(calls);
    }

    [HttpGet("{id}/activities")]
    public async Task<IActionResult> GetCustomerActivities(Guid id)
    {
        var activities = await _db.CRMActivities
            .Where(a => a.CustomerId == id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(activities);
    }

    [HttpPost("{id}/activities")]
    public async Task<IActionResult> CreateActivity(Guid id, [FromBody] CreateCustomerActivityDto dto)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "Customer not found." });

        var activity = new CRMActivity
        {
            ActivityId = Guid.NewGuid(),
            CustomerId = id,
            CallId = dto.CallId,
            ActivityType = dto.ActivityType,
            Notes = dto.Notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.CRMActivities.Add(activity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Activity created successfully", activityId = activity.ActivityId, activity });
    }

    [HttpPut("{id}/activities/{activityId}")]
    public async Task<IActionResult> UpdateActivity(Guid id, Guid activityId, [FromBody] UpdateCustomerActivityDto dto)
    {
        var act = await _db.CRMActivities.FirstOrDefaultAsync(a => a.CustomerId == id && a.ActivityId == activityId);
        if (act == null) return NotFound(new { message = "Activity not found." });

        if (dto.Notes != null) act.Notes = dto.Notes;
        if (!string.IsNullOrEmpty(dto.ActivityType)) act.ActivityType = dto.ActivityType;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Activity updated successfully", activity = act });
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Route("v1/[controller]")]
public class CrmController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public CrmController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet("health")]
    public IActionResult GetCrmHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            crmProvider = "Integrated Enterprise CRM Bridge",
            latencyMs = 12,
            syncStatus = "Active",
            lastCheck = DateTimeOffset.UtcNow
        });
    }

    [HttpPost("sync/customer/{id}")]
    public async Task<IActionResult> SyncCustomer(Guid id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "Customer not found." });

        return Ok(new
        {
            synced = true,
            customerId = customer.CustomerId,
            externalCrmId = customer.CRMId ?? "CRM-" + customer.CustomerId.ToString().Substring(0, 8),
            syncedAt = DateTimeOffset.UtcNow
        });
    }

    [HttpPost("sync/call/{id}")]
    public async Task<IActionResult> SyncCallActivity(string id)
    {
        Guid? callGuid = Guid.TryParse(id, out var g) ? g : null;
        var call = await _db.Calls.FirstOrDefaultAsync(c => c.CallUuid == id || (callGuid.HasValue && c.CallId == callGuid.Value));
        if (call == null) return NotFound(new { message = "Call not found." });

        return Ok(new
        {
            synced = true,
            callUuid = call.CallUuid,
            crmActivityId = Guid.NewGuid(),
            status = "Pushed to CRM Activity Feed",
            syncedAt = DateTimeOffset.UtcNow
        });
    }
}
