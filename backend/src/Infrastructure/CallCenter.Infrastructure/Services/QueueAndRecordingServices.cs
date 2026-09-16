using CallCenter.Application.Interfaces;
using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CallCenter.Infrastructure.Services;

public class ACDQueueService : IACDQueueService
{
    private readonly IAgentStateService _agentStateService;
    private readonly ILogger<ACDQueueService> _logger;

    public ACDQueueService(IAgentStateService agentStateService, ILogger<ACDQueueService> logger)
    {
        _agentStateService = agentStateService;
        _logger = logger;
    }

    public async Task<Agent?> FindBestAgentForQueueAsync(Guid queueId, CancellationToken ct = default)
    {
        var allStates = await _agentStateService.GetAllAgentStatesAsync(ct);

        // Longest-Idle-Agent ACD Strategy:
        // Filter agents that are 'Available', order ascending by StateChangedAt (longest time spent idle)
        var longestIdle = allStates
            .Where(a => a.State == AgentState.Available)
            .OrderBy(a => a.StateChangedAt)
            .FirstOrDefault();

        if (longestIdle != null)
        {
            _logger.LogInformation("ACD matched longest idle agent: {Name} (Ext: {Ext}) idle for {Secs}s", 
                longestIdle.DisplayName, longestIdle.Extension, longestIdle.DurationInStateSeconds);

            return new Agent
            {
                AgentId = longestIdle.AgentId,
                DisplayName = longestIdle.DisplayName,
                Extension = longestIdle.Extension,
                CurrentState = longestIdle.State
            };
        }

        _logger.LogWarning("No available agents currently online for Queue {QueueId}", queueId);
        return null;
    }

    public Task EnqueueCallAsync(Call incomingCall, CancellationToken ct = default)
    {
        _logger.LogInformation("Enqueueing incoming call UUID {Uuid} from {Caller}", incomingCall.CallUuid, incomingCall.CallerNumber);
        return Task.CompletedTask;
    }
}

public class RecordingStorageService : IRecordingService
{
    private readonly ILogger<RecordingStorageService> _logger;

    public RecordingStorageService(ILogger<RecordingStorageService> logger)
    {
        _logger = logger;
    }

    public Task<CallRecording> ProcessRecordingAsync(string callUuid, string rawFilePath, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing audio recording for call {Uuid} from {Path}", callUuid, rawFilePath);

        var recording = new CallRecording
        {
            RecordingId = Guid.NewGuid(),
            StorageBucket = "call-center-recordings",
            StoragePath = $"v1/{DateTime.UtcNow:yyyy/MM/dd}/{callUuid}.opus",
            FileHashSHA256 = "SIMULATED_SHA256_HASH",
            FileSizeBytes = 1048576, // ~1MB
            DurationSeconds = 184,
            AudioChannels = "Stereo",
            IsArchived = true,
            OffloadedAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult(recording);
    }

    public Task<string> GetPlaybackUrlAsync(Guid recordingId, TimeSpan expiration, CancellationToken ct = default)
    {
        // Generates pre-signed MinIO / S3 playback URL
        var simulatedUrl = $"/api/recordings/{recordingId}/stream";
        return Task.FromResult(simulatedUrl);
    }
}
