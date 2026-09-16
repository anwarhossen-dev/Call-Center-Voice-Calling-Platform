using CallCenter.Application.Interfaces;
using CallCenter.Infrastructure.Services;
using CallCenter.Telephony.FreeSwitch;
using CallCenter.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ITelephonyBridge, FreeSwitchEslBridge>();
builder.Services.AddSingleton<IAgentStateService, AgentStateService>();
builder.Services.AddSingleton<IRecordingService, RecordingStorageService>();

builder.Services.AddHostedService<TelephonyWorker>();

var host = builder.Build();
host.Run();
