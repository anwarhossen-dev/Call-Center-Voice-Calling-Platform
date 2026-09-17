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

// Root redirects cleanly to Swagger API documentation
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
