using System.Collections.Concurrent;
using CallCenter.Application.DTOs;
using CallCenter.Application.Interfaces;
using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CallCenter.Infrastructure.Services;

public class AgentStateService : IAgentStateService
{
    private readonly ConcurrentDictionary<Guid, AgentStateDto> _agentStates = new();
    private readonly ILogger<AgentStateService> _logger;

    public AgentStateService(ILogger<AgentStateService> logger)
    {
        _logger = logger;
        // Seed default agents for immediate testing
        SeedDefaultAgents();
    }

    private void SeedDefaultAgents()
    {
        var agent1 = new AgentStateDto
        {
            AgentId = Guid.Parse("aaaa1111-1111-1111-1111-111111111111"),
            DisplayName = "Rahim Ahmed",
            Extension = "1001",
            State = AgentState.Available,
            StateChangedAt = DateTimeOffset.UtcNow.AddMinutes(-15)
        };
        var agent2 = new AgentStateDto
        {
            AgentId = Guid.Parse("aaaa2222-2222-2222-2222-222222222222"),
            DisplayName = "Fatima Khan",
            Extension = "1002",
            State = AgentState.OnCall,
            StateChangedAt = DateTimeOffset.UtcNow.AddMinutes(-3),
            CurrentCallUuid = Guid.NewGuid().ToString()
        };
        var agent3 = new AgentStateDto
        {
            AgentId = Guid.Parse("aaaa3333-3333-3333-3333-333333333333"),
            DisplayName = "Tanvir Hasan",
            Extension = "1003",
            State = AgentState.Break,
            StateChangedAt = DateTimeOffset.UtcNow.AddMinutes(-8)
        };

        _agentStates[agent1.AgentId] = agent1;
        _agentStates[agent2.AgentId] = agent2;
        _agentStates[agent3.AgentId] = agent3;
    }

    public Task<AgentStateDto> SetAgentStateAsync(Guid agentId, AgentState newState, string? subReason = null, CancellationToken ct = default)
    {
        var state = _agentStates.AddOrUpdate(
            agentId,
            id => new AgentStateDto
            {
                AgentId = id,
                DisplayName = $"Agent {id.ToString()[..4]}",
                Extension = "1001",
                State = newState,
                StateChangedAt = DateTimeOffset.UtcNow
            },
            (id, existing) =>
            {
                existing.State = newState;
                existing.StateChangedAt = DateTimeOffset.UtcNow;
                if (newState != AgentState.OnCall)
                {
                    existing.CurrentCallUuid = null;
                }
                return existing;
            });

        _logger.LogInformation("Agent {AgentId} state transitioned to {State}", agentId, newState);
        return Task.FromResult(state);
    }

    public Task<AgentStateDto?> GetAgentStateAsync(Guid agentId, CancellationToken ct = default)
    {
        _agentStates.TryGetValue(agentId, out var state);
        return Task.FromResult(state);
    }

    public Task<List<AgentStateDto>> GetAllAgentStatesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_agentStates.Values.ToList());
    }
}
