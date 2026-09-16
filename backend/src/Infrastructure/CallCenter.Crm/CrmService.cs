using CallCenter.Application.DTOs;
using CallCenter.Application.Interfaces;
using CallCenter.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CallCenter.Crm;

public class CrmService : ICrmService
{
    private readonly ILogger<CrmService> _logger;

    public CrmService(ILogger<CrmService> logger)
    {
        _logger = logger;
    }

    public Task<CustomerProfileDto> LookupCustomerAsync(string phoneNumber, CancellationToken ct = default)
    {
        _logger.LogInformation("Looking up CRM profile for phone number {Phone}", phoneNumber);

        // Simulated customer lookup matching standard enterprise CRM records
        if (phoneNumber.Contains("01712345678") || phoneNumber.Contains("1712345678"))
        {
            return Task.FromResult(new CustomerProfileDto
            {
                CustomerId = "CRM-BD-9042",
                Name = "Rahim Ahmed",
                PhoneNumber = "+880 1712 345678",
                Email = "rahim@example.com",
                Tier = "Platinum VIP",
                OpenTickets = 1,
                RecentNotes = new List<string>
                {
                    "Requested billing clarification on previous invoice.",
                    "Active Fiber Broadband subscriber."
                }
            });
        }

        return Task.FromResult(new CustomerProfileDto
        {
            CustomerId = $"CRM-{Math.Abs(phoneNumber.GetHashCode()) % 100000}",
            Name = "Corporate Client",
            PhoneNumber = phoneNumber,
            Email = "client@example.com",
            Tier = "Gold Corporate",
            OpenTickets = 0,
            RecentNotes = new List<string> { "Inbound inquiry regarding enterprise trunking." }
        });
    }

    public Task<bool> SyncCallLogAsync(Call callRecord, CancellationToken ct = default)
    {
        _logger.LogInformation("Pushing Call Detail Record to CRM for Call UUID {Uuid} (Duration: {Dur}s, Disposition: {Disp})",
            callRecord.CallUuid, callRecord.TotalDurationSeconds, callRecord.Disposition?.Code ?? "UNTAGGED");

        // Simulates REST POST /api/v1/activities/calls
        return Task.FromResult(true);
    }
}
