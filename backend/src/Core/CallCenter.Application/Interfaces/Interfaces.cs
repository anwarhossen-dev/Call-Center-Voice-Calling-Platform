using CallCenter.Application.DTOs;
using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;

namespace CallCenter.Application.Interfaces;

public interface ITelephonyBridge
{
    Task<string> OriginateCallAsync(string agentExtension, string destinationNumber, CancellationToken ct = default);
    Task<bool> BridgeCallAsync(string legAUuid, string legBUuid, CancellationToken ct = default);
    Task<bool> TerminateCallAsync(string callUuid, CancellationToken ct = default);
    Task<bool> HoldCallAsync(string callUuid, bool hold, CancellationToken ct = default);
    Task<bool> TransferCallAsync(string callUuid, string targetDestination, bool attended = false, CancellationToken ct = default);
    Task<bool> InterveneCallAsync(string supervisorExtension, string targetCallUuid, SupervisorInterventionMode mode, CancellationToken ct = default);
}

public interface IAgentStateService
{
    Task<AgentStateDto> SetAgentStateAsync(Guid agentId, AgentState newState, string? subReason = null, CancellationToken ct = default);
    Task<AgentStateDto?> GetAgentStateAsync(Guid agentId, CancellationToken ct = default);
    Task<List<AgentStateDto>> GetAllAgentStatesAsync(CancellationToken ct = default);
}

public interface IACDQueueService
{
    Task<Agent?> FindBestAgentForQueueAsync(Guid queueId, CancellationToken ct = default);
    Task EnqueueCallAsync(Call incomingCall, CancellationToken ct = default);
}

public interface ICrmService
{
    Task<CustomerProfileDto> LookupCustomerAsync(string phoneNumber, CancellationToken ct = default);
    Task<bool> SyncCallLogAsync(Call callRecord, CancellationToken ct = default);
}

public interface IRecordingService
{
    Task<CallRecording> ProcessRecordingAsync(string callUuid, string rawFilePath, CancellationToken ct = default);
    Task<string> GetPlaybackUrlAsync(Guid recordingId, TimeSpan expiration, CancellationToken ct = default);
}

public interface IRealtimeNotifier
{
    Task NotifyScreenPopAsync(string agentExtension, ScreenPopDto screenPop, CancellationToken ct = default);
    Task BroadcastAgentStateChangedAsync(AgentStateDto state, CancellationToken ct = default);
    Task BroadcastQueueMetricsAsync(object metrics, CancellationToken ct = default);
}
