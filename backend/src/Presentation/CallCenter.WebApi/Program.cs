using CallCenter.Application.Interfaces;
using CallCenter.Crm;
using CallCenter.Infrastructure.Services;
using CallCenter.Persistence;
using CallCenter.Telephony.FreeSwitch;
using CallCenter.WebApi.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers & Swagger
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Call Center Voice Calling Platform API", Version = "v1" });
});

// 2. Add Database (InMemory for instant zero-config launch & tests; easily switched to SQL Server via appsettings)
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CallCenterDbContext>(options =>
{
    if (!string.IsNullOrEmpty(sqlConnectionString))
    {
        options.UseSqlServer(sqlConnectionString);
    }
    else
    {
        options.UseInMemoryDatabase("CallCenterDb");
    }
});

// 3. Register Domain & Infrastructure Services
builder.Services.AddSingleton<ITelephonyBridge, FreeSwitchEslBridge>();
builder.Services.AddSingleton<IAgentStateService, AgentStateService>();
builder.Services.AddSingleton<IACDQueueService, ACDQueueService>();
builder.Services.AddSingleton<ICrmService, CrmService>();
builder.Services.AddSingleton<IRecordingService, RecordingStorageService>();
builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

// 4. Add SignalR Real-Time Engine
builder.Services.AddSignalR();

// 5. Configure CORS for Angular Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200", "http://127.0.0.1:4200", "https://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed initial database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CallCenterDbContext>();
    if (db.Database.IsSqlServer())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }

    // Seed all enterprise tables in SQL Server
    await DbInitializer.SeedAsync(db, app.Logger);
}

// 6. Configure Middleware Pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Call Center API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();
app.UseCors("AllowAngularApp");
app.UseAuthorization();

app.MapControllers();
app.MapHub<CallHub>("/hubs/calls");

// Friendly Root Landing Page on http://localhost:5000
app.MapGet("/", () => Results.Content(
    @"<!DOCTYPE html>
<html lang=""bn"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Call Center Telephony Core API</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }
        body { background: #0b1120; color: #e2e8f0; display: flex; align-items: center; justify-content: center; min-height: 100vh; padding: 20px; }
        .card { background: #1e293b; border: 1px solid #334155; border-radius: 16px; padding: 36px; max-width: 580px; width: 100%; box-shadow: 0 25px 50px -12px rgba(0,0,0,0.5); text-align: center; }
        .status-badge { display: inline-flex; align-items: center; gap: 8px; background: rgba(34, 197, 94, 0.15); color: #4ade80; border: 1px solid rgba(74, 222, 128, 0.3); padding: 6px 14px; border-radius: 999px; font-size: 13px; font-weight: 700; margin-bottom: 20px; }
        .pulse-dot { width: 10px; height: 10px; border-radius: 50%; background: #22c55e; box-shadow: 0 0 10px #22c55e; }
        h1 { font-size: 24px; font-weight: 800; color: #ffffff; margin-bottom: 8px; }
        p.subtitle { color: #94a3b8; font-size: 14px; margin-bottom: 24px; line-height: 1.5; }
        .services-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 12px; margin-bottom: 28px; text-align: left; }
        .srv-item { background: #0f172a; border: 1px solid #1e293b; border-radius: 10px; padding: 12px; }
        .srv-title { font-size: 11px; color: #64748b; text-transform: uppercase; font-weight: 700; }
        .srv-val { font-size: 13px; color: #38bdf8; font-weight: 600; margin-top: 4px; }
        .actions { display: flex; flex-direction: column; gap: 12px; }
        .btn { display: flex; align-items: center; justify-content: center; gap: 10px; padding: 14px 20px; border-radius: 10px; font-size: 14px; font-weight: 700; text-decoration: none; transition: all 0.2s; }
        .btn-primary { background: #2563eb; color: #ffffff; }
        .btn-primary:hover { background: #1d4ed8; }
        .btn-success { background: #16a34a; color: #ffffff; }
        .btn-success:hover { background: #15803d; }
        .btn-outline { background: transparent; border: 1px solid #475569; color: #cbd5e1; }
        .btn-outline:hover { background: #334155; }
        .footer-note { margin-top: 24px; font-size: 12px; color: #64748b; }
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""status-badge"">
            <span class=""pulse-dot""></span>
            <span>BACKEND API IS RUNNING LIVE (PORT 5000)</span>
        </div>
        <h1>Call Center Telephony Core</h1>
        <p class=""subtitle"">.NET 8 Core WebAPI Engine সফলভাবে চালু আছে। ডাটাবেজ, সিগন্যালআর এবং কলিং কন্ট্রোলার সম্পূর্ণ সক্রিয়।</p>
        
        <div class=""services-grid"">
            <div class=""srv-item"">
                <div class=""srv-title"">Database Engine</div>
                <div class=""srv-val"">Microsoft SQL Server</div>
            </div>
            <div class=""srv-item"">
                <div class=""srv-title"">Real-time WebSocket</div>
                <div class=""srv-val"">SignalR (/hubs/calls)</div>
            </div>
            <div class=""srv-item"">
                <div class=""srv-title"">Carrier Support</div>
                <div class=""srv-val"">BTCL E1 + Twilio REST</div>
            </div>
            <div class=""srv-item"">
                <div class=""srv-title"">Audio Architecture</div>
                <div class=""srv-val"">Dual-Track 16kHz Stereo</div>
            </div>
        </div>

        <div class=""actions"">
            <a class=""btn btn-success"" href=""http://localhost:4200"" target=""_blank"">
                <span>🎧</span>
                <span>Open Agent & Admin Portal (Port 4200)</span>
            </a>
            <a class=""btn btn-primary"" href=""/swagger"">
                <span>⚡</span>
                <span>Open Swagger API Explorer (/swagger)</span>
            </a>
        </div>

        <div class=""footer-note"">
            Call Center Voice Calling Platform • On-Premises Telephony Core
        </div>
    </div>
</body>
</html>",
    "text/html; charset=utf-8"
));

app.Run();
