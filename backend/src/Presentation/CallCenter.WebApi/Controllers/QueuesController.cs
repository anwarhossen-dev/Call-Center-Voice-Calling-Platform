using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueuesController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public QueuesController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetQueues()
    {
        var queues = await _db.Queues
            .Include(q => q.AgentQueues)
            .Select(q => new
            {
                queueId = q.QueueId,
                queueName = q.QueueName,
                strategy = q.Strategy,
                slaThresholdSeconds = q.SLAThresholdSeconds,
                maxWaitTimeoutSeconds = q.MaxWaitTimeoutSeconds,
                isActive = q.IsActive,
                agentCount = q.AgentQueues.Count,
                waitingCalls = _db.Calls.Count(c => c.QueueId == q.QueueId && c.Status == Domain.Enums.CallStatus.Ringing)
            })
            .ToListAsync();

        return Ok(queues);
    }

    [HttpPost]
    public async Task<IActionResult> CreateQueue([FromBody] Queue queue)
    {
        if (queue.QueueId == Guid.Empty)
        {
            queue.QueueId = Guid.NewGuid();
        }
        _db.Queues.Add(queue);
        await _db.SaveChangesAsync();
        return Ok(queue);
    }
}
