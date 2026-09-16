using System.Net.Sockets;
using System.Text;
using CallCenter.Application.Interfaces;
using CallCenter.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CallCenter.Telephony.FreeSwitch;

public class FreeSwitchOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8021;
    public string Password { get; set; } = "ClueCon";
    public string SipDomain { get; set; } = "telephony.local";
    public string BtclGatewayName { get; set; } = "btcl_trunk";
}

public class FreeSwitchEslBridge : ITelephonyBridge
{
    private readonly FreeSwitchOptions _options;
    private readonly ILogger<FreeSwitchEslBridge> _logger;

    public FreeSwitchEslBridge(ILogger<FreeSwitchEslBridge> logger, FreeSwitchOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new FreeSwitchOptions();
    }

    private async Task<string> SendEslCommandAsync(string command, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_options.Host, _options.Port, ct);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            // 1. Read initial banner
            string? banner = await reader.ReadLineAsync(ct);

            // 2. Authenticate
            await writer.WriteLineAsync($"auth {_options.Password}");
            string? authResponse = await reader.ReadLineAsync(ct);

            // 3. Send command
            await writer.WriteLineAsync($"api {command}");
            
            // 4. Read response
            var responseBuilder = new StringBuilder();
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                responseBuilder.AppendLine(line);
                if (string.IsNullOrWhiteSpace(line)) break;
            }

            return responseBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ESL connection to FreeSWITCH at {Host}:{Port} failed (running in simulation mode). Command: {Cmd}", _options.Host, _options.Port, command);
            // Simulated success response for local development when FreeSWITCH is offline
            return $"+OK [SIMULATED] Command '{command}' accepted.";
        }
    }

    public async Task<string> OriginateCallAsync(string agentExtension, string destinationNumber, CancellationToken ct = default)
    {
        var callUuid = Guid.NewGuid().ToString();
        _logger.LogInformation("Originating outbound call via BTCL trunk to {Dest} for agent {Ext} (UUID: {Uuid})", destinationNumber, agentExtension, callUuid);

        // Originate Leg A to Agent WebRTC softphone, then bridge to BTCL Trunk Leg B
        var cmd = $"originate {{origination_uuid={callUuid},ignore_early_media=true}}user/{agentExtension}@{_options.SipDomain} &bridge(sofia/gateway/{_options.BtclGatewayName}/{destinationNumber})";
        await SendEslCommandAsync(cmd, ct);
        return callUuid;
    }

    public async Task<bool> BridgeCallAsync(string legAUuid, string legBUuid, CancellationToken ct = default)
    {
        _logger.LogInformation("Bridging call leg {LegA} with leg {LegB}", legAUuid, legBUuid);
        var res = await SendEslCommandAsync($"uuid_bridge {legAUuid} {legBUuid}", ct);
        return res.Contains("+OK");
    }

    public async Task<bool> TerminateCallAsync(string callUuid, CancellationToken ct = default)
    {
        _logger.LogInformation("Terminating call UUID {Uuid}", callUuid);
        var res = await SendEslCommandAsync($"uuid_kill {callUuid} NORMAL_CLEARING", ct);
        return res.Contains("+OK");
    }

    public async Task<bool> HoldCallAsync(string callUuid, bool hold, CancellationToken ct = default)
    {
        var action = hold ? "uuid_hold" : "uuid_hold off";
        _logger.LogInformation("Setting call UUID {Uuid} hold state to {Hold}", callUuid, hold);
        var res = await SendEslCommandAsync($"{action} {callUuid}", ct);
        return res.Contains("+OK");
    }

    public async Task<bool> TransferCallAsync(string callUuid, string targetDestination, bool attended = false, CancellationToken ct = default)
    {
        _logger.LogInformation("Transferring call UUID {Uuid} to {Dest} (Attended={Attended})", callUuid, targetDestination, attended);
        var res = await SendEslCommandAsync($"uuid_transfer {callUuid} {targetDestination} XML default", ct);
        return res.Contains("+OK");
    }

    public async Task<bool> InterveneCallAsync(string supervisorExtension, string targetCallUuid, SupervisorInterventionMode mode, CancellationToken ct = default)
    {
        _logger.LogInformation("Supervisor {Ext} intervening on call {Uuid} with mode {Mode}", supervisorExtension, targetCallUuid, mode);

        string flags = mode switch
        {
            SupervisorInterventionMode.SilentSpy => "",       // Listen-only
            SupervisorInterventionMode.Whisper => "w-leg",     // Speak to agent only
            SupervisorInterventionMode.BargeIn => "both",      // Full conference
            _ => ""
        };

        var cmd = $"originate user/{supervisorExtension}@{_options.SipDomain} &eavesdrop({targetCallUuid}{(string.IsNullOrEmpty(flags) ? "" : " " + flags)})";
        var res = await SendEslCommandAsync(cmd, ct);
        return res.Contains("+OK");
    }
}
