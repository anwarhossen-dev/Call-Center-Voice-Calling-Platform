using CallCenter.Domain.Enums;

namespace CallCenter.Application.DTOs;

public class CustomerProfileDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string Name { get; set; } = "Anonymous Caller";
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tier { get; set; } = "Standard";
    public int OpenTickets { get; set; }
    public List<string> RecentNotes { get; set; } = new();

    public static CustomerProfileDto Anonymous(string phone) => new()
    {
        CustomerId = $"ANON-{phone}",
        Name = "New Customer",
        PhoneNumber = phone,
        Tier = "Standard"
    };
}

public class ScreenPopDto
{
    public string CallUuid { get; set; } = string.Empty;
    public string CallerNumber { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public DateTimeOffset RingingAt { get; set; } = DateTimeOffset.UtcNow;
    public CustomerProfileDto Customer { get; set; } = new();
}

public class AgentStateDto
{
    public Guid AgentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public AgentState State { get; set; }
    public string StateText => State.ToString();
    public DateTimeOffset StateChangedAt { get; set; }
    public int DurationInStateSeconds => (int)(DateTimeOffset.UtcNow - StateChangedAt).TotalSeconds;
    public string? CurrentCallUuid { get; set; }
}

public class ActiveCallDto
{
    public string CallUuid { get; set; } = string.Empty;
    public string CallerNumber { get; set; } = string.Empty;
    public string DestinationNumber { get; set; } = string.Empty;
    public string AgentExtension { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public DateTimeOffset ConnectedAt { get; set; }
    public int DurationSeconds => (int)(DateTimeOffset.UtcNow - ConnectedAt).TotalSeconds;
}

public class SupervisorDashboardDto
{
    public int TotalCallsToday { get; set; }
    public int AnsweredCallsToday { get; set; }
    public int AbandonedCallsToday { get; set; }
    public double AverageHandleTimeSeconds { get; set; }
    public int AvailableAgents { get; set; }
    public int OnCallAgents { get; set; }
    public int BreakAgents { get; set; }
    public int ACWAgents { get; set; }
    public int OfflineAgents { get; set; }
    public List<AgentStateDto> Agents { get; set; } = new();
    public List<ActiveCallDto> ActiveCalls { get; set; } = new();
}

public class CallDispositionRequest
{
    public string CallUuid { get; set; } = string.Empty;
    public Guid? DispositionId { get; set; }
    public string? DispositionName { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? DestinationNumber { get; set; }
    public string? AgentExtension { get; set; }
}

public class OriginateCallRequest
{
    public string? CallUuid { get; set; }
    public string AgentExtension { get; set; } = string.Empty;
    public string DestinationNumber { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
}

public class SupervisorInterventionRequest
{
    public string SupervisorExtension { get; set; } = string.Empty;
    public string TargetCallUuid { get; set; } = string.Empty;
    public SupervisorInterventionMode Mode { get; set; }
}
