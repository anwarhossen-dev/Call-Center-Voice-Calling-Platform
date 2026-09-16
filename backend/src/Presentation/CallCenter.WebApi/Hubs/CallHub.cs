using CallCenter.Application.DTOs;
using CallCenter.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CallCenter.WebApi.Hubs;

public class CallHub : Hub
{
    private readonly ILogger<CallHub> _logger;

    public CallHub(ILogger<CallHub> logger)
    {
        _logger = logger;
    }

    public async Task RegisterAgent(string extension)
    {
        _logger.LogInformation("Agent extension {Ext} connected to CallHub (Connection: {Id})", extension, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"agent_{extension}");
        await Clients.Caller.SendAsync("AgentRegistered", new { Extension = extension, Status = "Connected" });
    }

    public async Task RegisterSupervisor()
    {
        _logger.LogInformation("Supervisor connected to live monitoring (Connection: {Id})", Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "supervisors");
    }
}

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<CallHub> _hubContext;
    private readonly ILogger<SignalRRealtimeNotifier> _logger;

    public SignalRRealtimeNotifier(IHubContext<CallHub> hubContext, ILogger<SignalRRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyScreenPopAsync(string agentExtension, ScreenPopDto screenPop, CancellationToken ct = default)
    {
        _logger.LogInformation("Pushing Screen-Pop to agent extension {Ext} for Call {Uuid}", agentExtension, screenPop.CallUuid);
        await _hubContext.Clients.Group($"agent_{agentExtension}").SendAsync("ReceiveScreenPop", screenPop, ct);
    }

    public async Task BroadcastAgentStateChangedAsync(AgentStateDto state, CancellationToken ct = default)
    {
        _logger.LogInformation("Broadcasting agent {Ext} state {State} to supervisors", state.Extension, state.State);
        await _hubContext.Clients.Group("supervisors").SendAsync("AgentStateChanged", state, ct);
    }

    public async Task BroadcastQueueMetricsAsync(object metrics, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group("supervisors").SendAsync("QueueMetricsUpdated", metrics, ct);
    }
}
