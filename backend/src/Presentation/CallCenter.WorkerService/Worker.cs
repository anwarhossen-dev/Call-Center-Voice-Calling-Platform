using CallCenter.Application.Interfaces;

namespace CallCenter.WorkerService;

public class TelephonyWorker : BackgroundService
{
    private readonly ILogger<TelephonyWorker> _logger;
    private readonly IRecordingService _recordingService;

    public TelephonyWorker(ILogger<TelephonyWorker> logger, IRecordingService recordingService)
    {
        _logger = logger;
        _recordingService = recordingService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telephony Background Worker & Recording Offload Daemon started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Worker polls queue or keeps persistent socket connection
            await Task.Delay(5000, stoppingToken);
        }
    }
}
