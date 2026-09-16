using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly CallCenterDbContext _db;

    public CampaignsController(CallCenterDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCampaigns()
    {
        var campaigns = await _db.Campaigns.ToListAsync();
        return Ok(campaigns);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] Campaign campaign)
    {
        if (campaign.CampaignId == Guid.Empty)
        {
            campaign.CampaignId = Guid.NewGuid();
        }
        campaign.StartDate = DateTimeOffset.UtcNow;
        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();
        return Ok(campaign);
    }
}
