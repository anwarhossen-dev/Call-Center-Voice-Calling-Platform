using CallCenter.Domain.Entities;
using CallCenter.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CallCenter.WebApi.Controllers;

[ApiController]
[Route("api/v1/tables")]
[Route("api/tables")]
[Route("v1/tables")]
public class DataTablesController : ControllerBase
{
    private readonly CallCenterDbContext _db;
    private readonly ILogger<DataTablesController> _logger;

    public DataTablesController(CallCenterDbContext db, ILogger<DataTablesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns all 18 enterprise database tables with metadata and live row counts
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTablesSummary()
    {
        var tables = new List<object>
        {
            // Category: Users & Security
            new {
                id = "users",
                name = "Users",
                displayName = "Users Directory",
                category = "Users & Security",
                icon = "👤",
                description = "Platform user accounts, credentials & authentication profiles",
                rowCount = await _db.Users.CountAsync(),
                columns = new[] { "UserId", "Username", "Email", "Role", "IsActive", "CreatedAt" }
            },
            new {
                id = "roles",
                name = "Roles",
                displayName = "Security Roles",
                category = "Users & Security",
                icon = "🛡️",
                description = "Role-Based Access Control (Admin, Supervisor, Agent) definitions",
                rowCount = await _db.Roles.CountAsync(),
                columns = new[] { "RoleId", "RoleName", "Description", "CreatedAt" }
            },
            new {
                id = "user-roles",
                name = "UserRoles",
                displayName = "User Role Mappings",
                category = "Users & Security",
                icon = "🔗",
                description = "Many-to-many relationship mapping users to security roles",
                rowCount = await _db.UserRoles.CountAsync(),
                columns = new[] { "Id", "Username", "RoleName", "CreatedAt" }
            },

            // Category: Organization & Teams
            new {
                id = "teams",
                name = "Teams",
                displayName = "Call Center Teams",
                category = "Organization & Teams",
                icon = "🏢",
                description = "Enterprise teams configured for 500+ agent scalability",
                rowCount = await _db.Teams.CountAsync(),
                columns = new[] { "TeamId", "TeamName", "Description", "Manager", "IsActive", "CreatedAt" }
            },
            new {
                id = "agents",
                name = "Agents",
                displayName = "Contact Center Agents",
                category = "Organization & Teams",
                icon = "🎧",
                description = "Agent profiles, SIP extensions, live presence and team affiliations",
                rowCount = await _db.Agents.CountAsync(),
                columns = new[] { "AgentId", "DisplayName", "AgentCode", "Extension", "CurrentState", "TeamName", "StateChangedAt" }
            },
            new {
                id = "agent-status-histories",
                name = "AgentStatusHistories",
                displayName = "Agent State Audit Trail",
                category = "Organization & Teams",
                icon = "⏱️",
                description = "Complete chronological log of agent state transitions",
                rowCount = await _db.AgentStatusHistories.CountAsync(),
                columns = new[] { "Id", "AgentName", "Status", "StartTime", "EndTime" }
            },
            new {
                id = "performance-metrics",
                name = "PerformanceMetrics",
                displayName = "Agent KPIs & Performance",
                category = "Organization & Teams",
                icon = "📈",
                description = "Daily agent performance metrics, AHT, and occupancy percentages",
                rowCount = await _db.PerformanceMetrics.CountAsync(),
                columns = new[] { "MetricId", "AgentName", "Date", "TotalCalls", "AnsweredCalls", "MissedCalls", "AvgTalkTime", "Occupancy" }
            },

            // Category: Telephony & Queues
            new {
                id = "queues",
                name = "Queues",
                displayName = "ACD Call Queues",
                category = "Telephony & Queues",
                icon = "📋",
                description = "Automatic Call Distribution queues, routing strategies & SLAs",
                rowCount = await _db.Queues.CountAsync(),
                columns = new[] { "QueueId", "QueueName", "Strategy", "SLAThresholdSeconds", "MaxWaitTimeoutSeconds", "IsActive" }
            },
            new {
                id = "agent-queues",
                name = "AgentQueues",
                displayName = "Agent Queue Skills",
                category = "Telephony & Queues",
                icon = "🎯",
                description = "Skill level and queue assignments for agents",
                rowCount = await _db.AgentQueues.CountAsync(),
                columns = new[] { "AgentName", "QueueName", "SkillLevel" }
            },
            new {
                id = "campaigns",
                name = "Campaigns",
                displayName = "Calling Campaigns",
                category = "Telephony & Queues",
                icon = "📢",
                description = "Outbound & Inbound telecom outreach campaigns",
                rowCount = await _db.Campaigns.CountAsync(),
                columns = new[] { "CampaignId", "Name", "Type", "Status", "StartDate", "EndDate" }
            },
            new {
                id = "calls",
                name = "Calls",
                displayName = "Call Detail Records (CDR)",
                category = "Telephony & Queues",
                icon = "📞",
                description = "Complete call lifecycle records, timestamps, durations & outcomes",
                rowCount = await _db.Calls.CountAsync(),
                columns = new[] { "CallUuid", "CallerNumber", "DestinationNumber", "Status", "Direction", "InitiatedAt", "TalkDurationSeconds", "AgentName", "QueueName", "Disposition" }
            },
            new {
                id = "call-events",
                name = "CallEvents",
                displayName = "Telephony Signaling Events",
                category = "Telephony & Queues",
                icon = "⚡",
                description = "Real-time SIP signaling events (INVITE, Ringing, 200 OK, BYE)",
                rowCount = await _db.CallEvents.CountAsync(),
                columns = new[] { "EventId", "CallUuid", "EventType", "Description", "EventTime" }
            },
            new {
                id = "dispositions",
                name = "Dispositions",
                displayName = "Call Outcome Dispositions",
                category = "Telephony & Queues",
                icon = "🏷️",
                description = "Standardized call conclusion and wrap-up codes",
                rowCount = await _db.Dispositions.CountAsync(),
                columns = new[] { "DispositionId", "Code", "Description", "Category", "RequiresFollowup", "IsActive" }
            },
            new {
                id = "call-recordings",
                name = "CallRecordings",
                displayName = "Audio Call Recordings",
                category = "Telephony & Queues",
                icon = "🎙️",
                description = "Audio file storage metadata, stereo channels & durations",
                rowCount = await _db.CallRecordings.CountAsync(),
                columns = new[] { "RecordingId", "CallUuid", "StorageBucket", "StoragePath", "DurationSeconds", "AudioChannels", "OffloadedAt" }
            },

            // Category: Customers & CRM
            new {
                id = "customers",
                name = "Customers",
                displayName = "Customer Directory",
                category = "Customers & CRM",
                icon = "👥",
                description = "Subscribers, corporate clients and caller identities",
                rowCount = await _db.Customers.CountAsync(),
                columns = new[] { "CustomerId", "Name", "Phone", "Email", "Address", "CRMId", "CreatedAt" }
            },
            new {
                id = "crm-activities",
                name = "CRMActivities",
                displayName = "CRM Touchpoints & Notes",
                category = "Customers & CRM",
                icon = "📝",
                description = "Customer interaction history, call notes & support tickets",
                rowCount = await _db.CRMActivities.CountAsync(),
                columns = new[] { "ActivityId", "CustomerName", "ActivityType", "Notes", "CreatedAt" }
            },
            new {
                id = "customer-crm-mappings",
                name = "CustomerCRMMappings",
                displayName = "CRM System Mappings",
                category = "Customers & CRM",
                icon = "🗺️",
                description = "External CRM system cross-references (Salesforce, Zoho, HubSpot)",
                rowCount = await _db.CustomerCRMMappings.CountAsync(),
                columns = new[] { "MappingId", "CustomerName", "CRMId", "ExternalCustomerId", "LastSyncAt", "IsActive" }
            },

            // Category: System & Governance
            new {
                id = "audit-logs",
                name = "AuditLogs",
                displayName = "Security Audit Logs",
                category = "System & Governance",
                icon = "📜",
                description = "Compliance audit trails of all administrative and system events",
                rowCount = await _db.AuditLogs.CountAsync(),
                columns = new[] { "LogId", "Username", "Action", "Entity", "NewValue", "IPAddress", "CreatedAt" }
            },
            new {
                id = "system-settings",
                name = "SystemSettings",
                displayName = "System Settings",
                category = "System & Governance",
                icon = "⚙️",
                description = "Telephony SIP trunk, carrier links, ACD routing & retention policies",
                rowCount = await _db.SystemSettings.CountAsync(),
                columns = new[] { "SettingId", "SettingKey", "SettingValue", "Description", "UpdatedAt" }
            },
            new {
                id = "qa-scorecards",
                name = "QAScorecards",
                displayName = "QA Scorecards",
                category = "System & Governance",
                icon = "📋",
                description = "Quality Assurance evaluation scorecard templates and scoring weights",
                rowCount = await _db.QAScorecards.CountAsync(),
                columns = new[] { "ScorecardId", "Name", "PassThreshold", "CriteriaJson", "IsActive", "CreatedAt" }
            },
            new {
                id = "qa-reviews",
                name = "QAReviews",
                displayName = "QA Call Reviews",
                category = "System & Governance",
                icon = "⭐",
                description = "Call quality evaluations, supervisor scores and coaching notes",
                rowCount = await _db.QAReviews.CountAsync(),
                columns = new[] { "ReviewId", "CallUuid", "ScorecardName", "ReviewerId", "AgentName", "Score", "Feedback", "CreatedAt" }
            }
        };

        return Ok(new {
            totalTables = tables.Count,
            tables
        });
    }

    /// <summary>
    /// Returns live rows for a specific table with search and pagination
    /// </summary>
    [HttpGet("{tableName}")]
    public async Task<IActionResult> GetTableData(string tableName, [FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 15;
        if (pageSize > 100) pageSize = 100;

        var normalizedName = tableName.ToLowerInvariant().Replace("-", "").Replace("_", "");

        switch (normalizedName)
        {
            case "users":
            {
                var query = _db.Users.Include(u => u.Role).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new {
                        UserId = u.UserId,
                        Username = u.Username,
                        Email = u.Email,
                        Role = u.Role != null ? u.Role.RoleName : "None",
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "Users", total, page, pageSize, rows = items });
            }

            case "roles":
            {
                var query = _db.Roles.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(r => r.RoleName.Contains(search) || r.Description.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderBy(r => r.RoleName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new {
                        RoleId = r.RoleId,
                        RoleName = r.RoleName,
                        Description = r.Description,
                        CreatedAt = r.CreatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "Roles", total, page, pageSize, rows = items });
            }

            case "userroles":
            {
                var query = _db.UserRoles.Include(ur => ur.User).Include(ur => ur.Role).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(ur => ur.User.Username.Contains(search) || ur.Role.RoleName.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(ur => ur.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(ur => new {
                        Id = ur.Id,
                        Username = ur.User != null ? ur.User.Username : "N/A",
                        RoleName = ur.Role != null ? ur.Role.RoleName : "N/A",
                        CreatedAt = ur.CreatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "UserRoles", total, page, pageSize, rows = items });
            }

            case "teams":
            {
                var query = _db.Teams.Include(t => t.Manager).Include(t => t.Agents).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(t => t.TeamName.Contains(search) || (t.Description != null && t.Description.Contains(search)));

                var total = await query.CountAsync();
                var items = await query.OrderBy(t => t.TeamName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new {
                        TeamId = t.TeamId,
                        TeamName = t.TeamName,
                        Description = t.Description,
                        Manager = t.Manager != null ? t.Manager.Username : "Unassigned",
                        AgentCount = t.Agents.Count,
                        IsActive = t.IsActive,
                        CreatedAt = t.CreatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "Teams", total, page, pageSize, rows = items });
            }

            case "agents":
            {
                var query = _db.Agents.Include(a => a.Team).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(a => a.DisplayName.Contains(search) || a.Extension.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderBy(a => a.Extension)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new {
                        AgentId = a.AgentId,
                        DisplayName = a.DisplayName,
                        AgentCode = a.AgentCode ?? "AGT-" + a.Extension,
                        Extension = a.Extension,
                        CurrentState = a.CurrentState.ToString(),
                        TeamName = a.Team != null ? a.Team.TeamName : "Default Team",
                        StateChangedAt = a.StateChangedAt
                    }).ToListAsync();

                return Ok(new { tableName = "Agents", total, page, pageSize, rows = items });
            }

            case "agentstatushistories":
            {
                var query = _db.AgentStatusHistories.Include(h => h.Agent).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(h => (h.Agent != null && h.Agent.DisplayName.Contains(search)) || h.Status.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(h => h.StartTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new {
                        Id = h.Id,
                        AgentName = h.Agent != null ? h.Agent.DisplayName : "Unknown",
                        Status = h.Status,
                        StartTime = h.StartTime,
                        EndTime = h.EndTime
                    }).ToListAsync();

                return Ok(new { tableName = "AgentStatusHistories", total, page, pageSize, rows = items });
            }

            case "performancemetrics":
            {
                var query = _db.PerformanceMetrics.Include(m => m.Agent).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(m => m.Agent != null && m.Agent.DisplayName.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(m => m.Date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new {
                        MetricId = m.MetricId,
                        AgentName = m.Agent != null ? m.Agent.DisplayName : "Unknown",
                        Date = m.Date.ToString("yyyy-MM-dd"),
                        TotalCalls = m.TotalCalls,
                        AnsweredCalls = m.AnsweredCalls,
                        MissedCalls = m.MissedCalls,
                        AvgTalkTime = $"{m.AvgTalkTime}s",
                        Occupancy = $"{m.Occupancy:F1}%"
                    }).ToListAsync();

                return Ok(new { tableName = "PerformanceMetrics", total, page, pageSize, rows = items });
            }

            case "queues":
            {
                var query = _db.Queues.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(q => q.QueueName.Contains(search) || q.Strategy.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderBy(q => q.QueueName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(q => new {
                        QueueId = q.QueueId,
                        QueueName = q.QueueName,
                        Strategy = q.Strategy,
                        SLAThresholdSeconds = $"{q.SLAThresholdSeconds}s",
                        MaxWaitTimeoutSeconds = $"{q.MaxWaitTimeoutSeconds}s",
                        IsActive = q.IsActive
                    }).ToListAsync();

                return Ok(new { tableName = "Queues", total, page, pageSize, rows = items });
            }

            case "agentqueues":
            {
                var query = _db.AgentQueues.Include(aq => aq.Agent).Include(aq => aq.Queue).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(aq => (aq.Agent != null && aq.Agent.DisplayName.Contains(search)) || (aq.Queue != null && aq.Queue.QueueName.Contains(search)));

                var total = await query.CountAsync();
                var items = await query.OrderBy(aq => aq.SkillLevel)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(aq => new {
                        AgentName = aq.Agent != null ? aq.Agent.DisplayName : "N/A",
                        QueueName = aq.Queue != null ? aq.Queue.QueueName : "N/A",
                        SkillLevel = aq.SkillLevel
                    }).ToListAsync();

                return Ok(new { tableName = "AgentQueues", total, page, pageSize, rows = items });
            }

            case "campaigns":
            {
                var query = _db.Campaigns.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(c => c.Name.Contains(search) || c.Type.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(c => c.StartDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new {
                        CampaignId = c.CampaignId,
                        Name = c.Name,
                        Type = c.Type,
                        Status = c.Status,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate
                    }).ToListAsync();

                return Ok(new { tableName = "Campaigns", total, page, pageSize, rows = items });
            }

            case "calls":
            {
                var query = _db.Calls.Include(c => c.Agent).Include(c => c.Queue).Include(c => c.Disposition).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(c => c.CallerNumber.Contains(search) || c.DestinationNumber.Contains(search) || c.CallUuid.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(c => c.InitiatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new {
                        CallUuid = c.CallUuid,
                        CallerNumber = c.CallerNumber,
                        DestinationNumber = c.DestinationNumber,
                        Status = c.Status.ToString(),
                        Direction = c.Direction.ToString(),
                        InitiatedAt = c.InitiatedAt,
                        TalkDurationSeconds = $"{c.TalkDurationSeconds}s",
                        AgentName = c.Agent != null ? c.Agent.DisplayName : "Unassigned",
                        QueueName = c.Queue != null ? c.Queue.QueueName : "Direct",
                        Disposition = c.Disposition != null ? c.Disposition.Code : "PENDING"
                    }).ToListAsync();

                return Ok(new { tableName = "Calls", total, page, pageSize, rows = items });
            }

            case "callevents":
            {
                var query = _db.CallEvents.Include(e => e.Call).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(e => e.EventType.Contains(search) || (e.Description != null && e.Description.Contains(search)));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(e => e.EventTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new {
                        EventId = e.EventId,
                        CallUuid = e.Call != null ? e.Call.CallUuid : "N/A",
                        EventType = e.EventType,
                        Description = e.Description,
                        EventTime = e.EventTime
                    }).ToListAsync();

                return Ok(new { tableName = "CallEvents", total, page, pageSize, rows = items });
            }

            case "dispositions":
            {
                var query = _db.Dispositions.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(d => d.Code.Contains(search) || d.Description.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderBy(d => d.Code)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new {
                        DispositionId = d.DispositionId,
                        Code = d.Code,
                        Description = d.Description,
                        Category = d.Category,
                        RequiresFollowup = d.RequiresFollowup,
                        IsActive = d.IsActive
                    }).ToListAsync();

                return Ok(new { tableName = "Dispositions", total, page, pageSize, rows = items });
            }

            case "callrecordings":
            {
                var query = _db.CallRecordings.Include(r => r.Call).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(r => r.StoragePath.Contains(search) || (r.Call != null && r.Call.CallUuid.Contains(search)));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(r => r.OffloadedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new {
                        RecordingId = r.RecordingId,
                        CallUuid = r.Call != null ? r.Call.CallUuid : "N/A",
                        StorageBucket = r.StorageBucket,
                        StoragePath = r.StoragePath,
                        DurationSeconds = $"{r.DurationSeconds}s",
                        FileSizeBytes = $"{r.FileSizeBytes / 1024} KB",
                        AudioChannels = r.AudioChannels,
                        OffloadedAt = r.OffloadedAt
                    }).ToListAsync();

                return Ok(new { tableName = "CallRecordings", total, page, pageSize, rows = items });
            }

            case "customers":
            {
                var query = _db.Customers.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(c => c.Name.Contains(search) || c.Phone.Contains(search) || (c.Email != null && c.Email.Contains(search)));

                var total = await query.CountAsync();
                var items = await query.OrderBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new {
                        CustomerId = c.CustomerId,
                        Name = c.Name,
                        Phone = c.Phone,
                        Email = c.Email,
                        Address = c.Address,
                        CRMId = c.CRMId,
                        CreatedAt = c.CreatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "Customers", total, page, pageSize, rows = items });
            }

            case "crmactivities":
            {
                var query = _db.CRMActivities.Include(a => a.Customer).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(a => a.ActivityType.Contains(search) || (a.Notes != null && a.Notes.Contains(search)));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new {
                        ActivityId = a.ActivityId,
                        CustomerName = a.Customer != null ? a.Customer.Name : "General Customer",
                        ActivityType = a.ActivityType,
                        Notes = a.Notes,
                        CreatedAt = a.CreatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "CRMActivities", total, page, pageSize, rows = items });
            }

            case "customercrmmappings":
            {
                var query = _db.CustomerCRMMappings.Include(m => m.Customer).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(m => m.CRMId.Contains(search) || m.ExternalCustomerId.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(m => m.LastSyncAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new {
                        MappingId = m.MappingId,
                        CustomerName = m.Customer != null ? m.Customer.Name : "N/A",
                        CRMId = m.CRMId,
                        ExternalCustomerId = m.ExternalCustomerId,
                        LastSyncAt = m.LastSyncAt,
                        IsActive = m.IsActive
                    }).ToListAsync();

                return Ok(new { tableName = "CustomerCRMMappings", total, page, pageSize, rows = items });
            }

            case "auditlogs":
            {
                var query = _db.AuditLogs.Include(l => l.User).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(l => l.Action.Contains(search) || l.Entity.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new {
                        LogId = l.LogId,
                        Username = l.User != null ? l.User.Username : "System Service",
                        Action = l.Action,
                        Entity = l.Entity,
                        NewValue = l.NewValue,
                        IPAddress = l.IPAddress,
                        CreatedAt = l.CreatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "AuditLogs", total, page, pageSize, rows = items });
            }

            case "systemsettings":
            {
                var query = _db.SystemSettings.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(s => s.SettingKey.Contains(search) || s.SettingValue.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderBy(s => s.SettingKey)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new {
                        SettingId = s.SettingId,
                        SettingKey = s.SettingKey,
                        SettingValue = s.SettingValue,
                        Description = s.Description,
                        UpdatedAt = s.UpdatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "SystemSettings", total, page, pageSize, rows = items });
            }

            case "qascorecards":
            {
                var query = _db.QAScorecards.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(s => s.Name.Contains(search));

                var total = await query.CountAsync();
                var items = await query.OrderBy(s => s.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new {
                        ScorecardId = s.ScorecardId,
                        Name = s.Name,
                        PassThreshold = $"{s.PassThreshold}%",
                        CriteriaJson = s.CriteriaJson,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "QAScorecards", total, page, pageSize, rows = items });
            }

            case "qareviews":
            {
                var query = _db.QAReviews.Include(r => r.Call).Include(r => r.Scorecard).Include(r => r.Agent).AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                    query = query.Where(r => (r.Agent != null && r.Agent.DisplayName.Contains(search)) || (r.Feedback != null && r.Feedback.Contains(search)));

                var total = await query.CountAsync();
                var items = await query.OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new {
                        ReviewId = r.ReviewId,
                        CallUuid = r.Call != null ? r.Call.CallUuid : "N/A",
                        ScorecardName = r.Scorecard != null ? r.Scorecard.Name : "Default",
                        ReviewerId = r.ReviewerId,
                        AgentName = r.Agent != null ? r.Agent.DisplayName : "N/A",
                        Score = $"{r.Score:F1}/100",
                        Feedback = r.Feedback,
                        CreatedAt = r.CreatedAt
                    }).ToListAsync();

                return Ok(new { tableName = "QAReviews", total, page, pageSize, rows = items });
            }

            default:
                return NotFound(new { error = $"Table '{tableName}' not found in Enterprise ERD schema." });
        }
    }
}
