using CallCenter.Domain.Entities;
using CallCenter.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CallCenter.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(CallCenterDbContext db, ILogger logger)
    {
        try
        {
            // 1. Ensure Roles
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var supervisorRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var agentRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            if (!await db.Roles.AnyAsync())
            {
                db.Roles.AddRange(
                    new Role { RoleId = adminRoleId, RoleName = "Admin", Description = "System Administrator" },
                    new Role { RoleId = supervisorRoleId, RoleName = "Supervisor", Description = "Call Center Supervisor" },
                    new Role { RoleId = agentRoleId, RoleName = "Agent", Description = "Contact Center Agent" }
                );
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded Roles into MS SQL Server");
            }

            // 2. Ensure Users & Agents
            if (!await db.Users.AnyAsync())
            {
                var userAdmin = new User
                {
                    UserId = Guid.Parse("01111111-1111-1111-1111-111111111111"),
                    Username = "admin",
                    Email = "admin@btcl-voice.bd",
                    PasswordHash = "AQAAAAEAACcQAAAAEHASH...",
                    RoleId = adminRoleId,
                    IsActive = true
                };

                var userSupervisor = new User
                {
                    UserId = Guid.Parse("02222222-2222-2222-2222-222222222222"),
                    Username = "supervisor",
                    Email = "supervisor@btcl-voice.bd",
                    PasswordHash = "AQAAAAEAACcQAAAAEHASH...",
                    RoleId = supervisorRoleId,
                    IsActive = true
                };

                var userAgent1 = new User
                {
                    UserId = Guid.Parse("03333333-1111-1111-1111-111111111111"),
                    Username = "rahim.ahmed",
                    Email = "rahim@btcl-voice.bd",
                    PasswordHash = "AQAAAAEAACcQAAAAEHASH...",
                    RoleId = agentRoleId,
                    IsActive = true
                };

                var userAgent2 = new User
                {
                    UserId = Guid.Parse("03333333-2222-2222-2222-222222222222"),
                    Username = "fatima.khan",
                    Email = "fatima@btcl-voice.bd",
                    PasswordHash = "AQAAAAEAACcQAAAAEHASH...",
                    RoleId = agentRoleId,
                    IsActive = true
                };

                var userAgent3 = new User
                {
                    UserId = Guid.Parse("03333333-3333-3333-3333-333333333333"),
                    Username = "tanvir.hasan",
                    Email = "tanvir@btcl-voice.bd",
                    PasswordHash = "AQAAAAEAACcQAAAAEHASH...",
                    RoleId = agentRoleId,
                    IsActive = true
                };

                var userAgent4 = new User
                {
                    UserId = Guid.Parse("03333333-4444-4444-4444-444444444444"),
                    Username = "salma.akter",
                    Email = "salma@btcl-voice.bd",
                    PasswordHash = "AQAAAAEAACcQAAAAEHASH...",
                    RoleId = agentRoleId,
                    IsActive = true
                };

                var userAgent5 = new User
                {
                    UserId = Guid.Parse("03333333-5555-5555-5555-555555555555"),
                    Username = "nazmul.hossain",
                    Email = "nazmul@btcl-voice.bd",
                    PasswordHash = "AQAAAAEAACcQAAAAEHASH...",
                    RoleId = agentRoleId,
                    IsActive = true
                };

                db.Users.AddRange(userAdmin, userSupervisor, userAgent1, userAgent2, userAgent3, userAgent4, userAgent5);
                await db.SaveChangesAsync();

                // Agents
                var agent1 = new Agent
                {
                    AgentId = Guid.Parse("aaaa1111-1111-1111-1111-111111111111"),
                    UserId = userAgent1.UserId,
                    Extension = "1001",
                    DisplayName = "Rahim Ahmed",
                    CurrentState = AgentState.Available,
                    StateChangedAt = DateTimeOffset.UtcNow.AddMinutes(-15)
                };

                var agent2 = new Agent
                {
                    AgentId = Guid.Parse("aaaa2222-2222-2222-2222-222222222222"),
                    UserId = userAgent2.UserId,
                    Extension = "1002",
                    DisplayName = "Fatima Khan",
                    CurrentState = AgentState.OnCall,
                    StateChangedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
                };

                var agent3 = new Agent
                {
                    AgentId = Guid.Parse("aaaa3333-3333-3333-3333-333333333333"),
                    UserId = userAgent3.UserId,
                    Extension = "1003",
                    DisplayName = "Tanvir Hasan",
                    CurrentState = AgentState.Available,
                    StateChangedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
                };

                var agent4 = new Agent
                {
                    AgentId = Guid.Parse("aaaa4444-4444-4444-4444-444444444444"),
                    UserId = userAgent4.UserId,
                    Extension = "1004",
                    DisplayName = "Salma Akter",
                    CurrentState = AgentState.Break,
                    StateChangedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
                };

                var agent5 = new Agent
                {
                    AgentId = Guid.Parse("aaaa5555-5555-5555-5555-555555555555"),
                    UserId = userAgent5.UserId,
                    Extension = "1005",
                    DisplayName = "Nazmul Hossain",
                    CurrentState = AgentState.WrapUp,
                    StateChangedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
                };

                db.Agents.AddRange(agent1, agent2, agent3, agent4, agent5);
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded Users and Agents into MS SQL Server");
            }

            // 3. Ensure Queues
            var q1Id = Guid.Parse("baaa1111-1111-1111-1111-111111111111");
            var q2Id = Guid.Parse("baaa2222-2222-2222-2222-222222222222");
            var q3Id = Guid.Parse("baaa3333-3333-3333-3333-333333333333");
            var q4Id = Guid.Parse("baaa4444-4444-4444-4444-444444444444");

            if (!await db.Queues.AnyAsync())
            {
                var q1 = new Queue { QueueId = q1Id, QueueName = "Sales Campaign", Strategy = "LongestIdle", SLAThresholdSeconds = 20, MaxWaitTimeoutSeconds = 300, IsActive = true };
                var q2 = new Queue { QueueId = q2Id, QueueName = "Technical Support", Strategy = "LongestIdle", SLAThresholdSeconds = 20, MaxWaitTimeoutSeconds = 300, IsActive = true };
                var q3 = new Queue { QueueId = q3Id, QueueName = "Billing & Invoicing", Strategy = "RoundRobin", SLAThresholdSeconds = 30, MaxWaitTimeoutSeconds = 300, IsActive = true };
                var q4 = new Queue { QueueId = q4Id, QueueName = "VIP Retention", Strategy = "RingAll", SLAThresholdSeconds = 15, MaxWaitTimeoutSeconds = 180, IsActive = true };

                db.Queues.AddRange(q1, q2, q3, q4);
                await db.SaveChangesAsync();

                // Map AgentQueues
                var agent1Id = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");
                var agent2Id = Guid.Parse("aaaa2222-2222-2222-2222-222222222222");
                var agent3Id = Guid.Parse("aaaa3333-3333-3333-3333-333333333333");

                db.AgentQueues.AddRange(
                    new AgentQueue { AgentId = agent1Id, QueueId = q2Id, SkillLevel = 10 },
                    new AgentQueue { AgentId = agent2Id, QueueId = q2Id, SkillLevel = 8 },
                    new AgentQueue { AgentId = agent3Id, QueueId = q3Id, SkillLevel = 9 },
                    new AgentQueue { AgentId = agent1Id, QueueId = q1Id, SkillLevel = 7 }
                );
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded Queues and AgentQueues into MS SQL Server");
            }

            // 4. Ensure Campaigns
            if (!await db.Campaigns.AnyAsync())
            {
                db.Campaigns.AddRange(
                    new Campaign { CampaignId = Guid.Parse("caaa1111-1111-1111-1111-111111111111"), Name = "National Fiber Sales", Type = "Outbound", Status = "Active", StartDate = DateTimeOffset.UtcNow.AddDays(-10) },
                    new Campaign { CampaignId = Guid.Parse("caaa2222-2222-2222-2222-222222222222"), Name = "General Helpdesk Support", Type = "Inbound", Status = "Active", StartDate = DateTimeOffset.UtcNow.AddDays(-30) },
                    new Campaign { CampaignId = Guid.Parse("caaa3333-3333-3333-3333-333333333333"), Name = "Corporate Retention", Type = "Outbound", Status = "Paused", StartDate = DateTimeOffset.UtcNow.AddDays(-5) },
                    new Campaign { CampaignId = Guid.Parse("caaa4444-4444-4444-4444-444444444444"), Name = "Broadband Satisfaction Survey", Type = "Outbound", Status = "Active", StartDate = DateTimeOffset.UtcNow.AddDays(-2) }
                );
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded Campaigns into MS SQL Server");
            }

            // 5. Ensure Dispositions
            var dispCodes = new[] { "RESOLVED", "ESCALATED", "CALLBACK_REQ", "INTERESTED", "NOT_INTERESTED", "CANCELLED", "NO_ANSWER" };
            foreach (var code in dispCodes)
            {
                if (!await db.Dispositions.AnyAsync(d => d.Code == code))
                {
                    db.Dispositions.Add(new Disposition
                    {
                        DispositionId = Guid.NewGuid(),
                        Code = code,
                        Description = code switch
                        {
                            "RESOLVED" => "Query Resolved on Call",
                            "ESCALATED" => "Issue Escalated to Tier 2",
                            "CALLBACK_REQ" => "Customer Requested Callback",
                            "INTERESTED" => "Prospect Interested in Offer",
                            "NOT_INTERESTED" => "Prospect Declined Offer",
                            "CANCELLED" => "Call Cancelled by Agent",
                            "NO_ANSWER" => "No Answer / Busy",
                            _ => code
                        },
                        Category = code.Contains("INTERESTED") ? "Sales" : "Support",
                        IsActive = true
                    });
                }
            }
            await db.SaveChangesAsync();

            // 6. Ensure CallRecordings for Calls
            if (!await db.CallRecordings.AnyAsync())
            {
                var completedCalls = await db.Calls.Where(c => c.Status == CallStatus.Completed).Take(5).ToListAsync();
                foreach (var call in completedCalls)
                {
                    db.CallRecordings.Add(new CallRecording
                    {
                        RecordingId = Guid.NewGuid(),
                        CallId = call.CallId,
                        StorageBucket = "btcl-voice-recordings",
                        StoragePath = $"/recordings/2026/09/{call.CallUuid}.webm",
                        FileHashSHA256 = Guid.NewGuid().ToString("N"),
                        FileSizeBytes = 1024 * 350,
                        DurationSeconds = call.TalkDurationSeconds > 0 ? call.TalkDurationSeconds : 15,
                        AudioChannels = "Stereo Dual-Track",
                        IsArchived = false,
                        OffloadedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
                    });
                }
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded CallRecordings into MS SQL Server");
            }

            // 7. Ensure Teams
            var teamSupportId = Guid.Parse("71111111-1111-1111-1111-111111111111");
            var teamSalesId = Guid.Parse("72222222-2222-2222-2222-222222222222");
            var teamVipId = Guid.Parse("73333333-3333-3333-3333-333333333333");
            if (!await db.Teams.AnyAsync())
            {
                var supervisor = await db.Users.FirstOrDefaultAsync(u => u.Username == "supervisor");
                db.Teams.AddRange(
                    new Team { TeamId = teamSupportId, TeamName = "Technical Support Team", Description = "L1/L2 Technical Support & Fiber Troubleshooting", ManagerId = supervisor?.UserId, IsActive = true },
                    new Team { TeamId = teamSalesId, TeamName = "Outbound Sales Team", Description = "Enterprise Connectivity & Package Upgrades", ManagerId = supervisor?.UserId, IsActive = true },
                    new Team { TeamId = teamVipId, TeamName = "VIP Priority Team", Description = "Dedicated support for Corporate & Enterprise Accounts", ManagerId = supervisor?.UserId, IsActive = true }
                );
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded Teams into MS SQL Server");

                // Assign Agents to Teams & Set AgentCode
                var agents = await db.Agents.ToListAsync();
                for (int i = 0; i < agents.Count; i++)
                {
                    agents[i].TeamId = (i % 2 == 0) ? teamSupportId : teamSalesId;
                    agents[i].AgentCode = $"AGT-{1000 + i + 1}";
                }
                await db.SaveChangesAsync();
            }

            // 8. Ensure Customers
            var cust1Id = Guid.Parse("81111111-1111-1111-1111-111111111111");
            if (!await db.Customers.AnyAsync())
            {
                db.Customers.AddRange(
                    new Customer { CustomerId = cust1Id, Name = "Rahim Ahmed", Phone = "+880 1712 345678", Email = "rahim.ahmed@example.com", Address = "Gulshan-2, Dhaka", CRMId = "C-1001" },
                    new Customer { CustomerId = Guid.Parse("82222222-2222-2222-2222-222222222222"), Name = "Karim Ullah", Phone = "+880 1819 876543", Email = "karim.ullah@example.com", Address = "Agrabad C/A, Chittagong", CRMId = "C-1002" },
                    new Customer { CustomerId = Guid.Parse("83333333-3333-3333-3333-333333333333"), Name = "Nusrat Jahan", Phone = "01777498421", Email = "nusrat.jahan@example.com", Address = "Dhanmondi 27, Dhaka", CRMId = "C-1003" },
                    new Customer { CustomerId = Guid.Parse("84444444-4444-4444-4444-444444444444"), Name = "Tanvir Rahman", Phone = "01819876543", Email = "tanvir.r@example.com", Address = "Sylhet Sadar, Sylhet", CRMId = "C-1004" },
                    new Customer { CustomerId = Guid.Parse("85555555-5555-5555-5555-555555555555"), Name = "Farzana Yasmin", Phone = "01799887766", Email = "farzana.y@example.com", Address = "Uttara Sector 7, Dhaka", CRMId = "C-1005" }
                );
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded Customers into MS SQL Server");

                // Link existing calls to customers
                var calls = await db.Calls.ToListAsync();
                foreach (var c in calls)
                {
                    c.CustomerId = cust1Id;
                }
                await db.SaveChangesAsync();
            }

            // 9. Ensure SystemSettings
            if (!await db.SystemSettings.AnyAsync())
            {
                db.SystemSettings.AddRange(
                    new SystemSetting { SettingId = Guid.NewGuid(), SettingKey = "telephony.sip.provider", SettingValue = "BTCL SIP Trunk (Carrier Grade)", Description = "Primary Carrier Telephony Link" },
                    new SystemSetting { SettingId = Guid.NewGuid(), SettingKey = "telephony.sip.server", SettingValue = "sip.btcl.com.bd:5060", Description = "SIP Gateway Host Address" },
                    new SystemSetting { SettingId = Guid.NewGuid(), SettingKey = "routing.strategy.default", SettingValue = "LongestIdle", Description = "Default Dynamic ACD Distribution Strategy" },
                    new SystemSetting { SettingId = Guid.NewGuid(), SettingKey = "routing.sla.threshold.seconds", SettingValue = "20", Description = "Service Level Agreement Answer Threshold (Seconds)" },
                    new SystemSetting { SettingId = Guid.NewGuid(), SettingKey = "recordings.retention.days", SettingValue = "90", Description = "Audio Recording Retention Policy (Days)" },
                    new SystemSetting { SettingId = Guid.NewGuid(), SettingKey = "qa.auto_eval.enabled", SettingValue = "true", Description = "Automated AI / Supervisor QA Evaluation Enabled" }
                );
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded SystemSettings into MS SQL Server");
            }

            // 10. Ensure QAScorecards
            if (!await db.QAScorecards.AnyAsync())
            {
                db.QAScorecards.Add(new QAScorecard
                {
                    ScorecardId = Guid.NewGuid(),
                    Name = "General Customer Service Evaluation",
                    PassThreshold = 80,
                    IsActive = true,
                    CriteriaJson = "[{\"criterion\":\"Greeting & Professionalism\",\"weight\":25},{\"criterion\":\"Problem Identification\",\"weight\":25},{\"criterion\":\"Resolution Accuracy\",\"weight\":30},{\"criterion\":\"Call Wrap-up & Courtesy\",\"weight\":20}]"
                });
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded QAScorecards into MS SQL Server");
            }

            // 11. Ensure AuditLogs
            if (!await db.AuditLogs.AnyAsync())
            {
                db.AuditLogs.Add(new AuditLog
                {
                    LogId = Guid.NewGuid(),
                    Action = "SYSTEM_INITIALIZE",
                    Entity = "Database",
                    OldValue = null,
                    NewValue = "Seeded 18 Enterprise ERD Tables",
                    IPAddress = "127.0.0.1",
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded initial AuditLogs into MS SQL Server");
            }

            // 12. Ensure UserRoles
            if (!await db.UserRoles.AnyAsync())
            {
                var users = await db.Users.ToListAsync();
                foreach (var user in users)
                {
                    db.UserRoles.Add(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.UserId,
                        RoleId = user.RoleId,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded UserRoles into MS SQL Server");
            }

            // 13. Ensure AgentStatusHistories & PerformanceMetrics
            if (!await db.AgentStatusHistories.AnyAsync())
            {
                var agents = await db.Agents.ToListAsync();
                foreach (var ag in agents)
                {
                    db.AgentStatusHistories.AddRange(
                        new AgentStatusHistory { Id = Guid.NewGuid(), AgentId = ag.AgentId, Status = "Offline", StartTime = DateTimeOffset.UtcNow.AddHours(-4), EndTime = DateTimeOffset.UtcNow.AddHours(-2) },
                        new AgentStatusHistory { Id = Guid.NewGuid(), AgentId = ag.AgentId, Status = "Available", StartTime = DateTimeOffset.UtcNow.AddHours(-2), EndTime = DateTimeOffset.UtcNow.AddMinutes(-10) },
                        new AgentStatusHistory { Id = Guid.NewGuid(), AgentId = ag.AgentId, Status = ag.CurrentState.ToString(), StartTime = DateTimeOffset.UtcNow.AddMinutes(-10), EndTime = null }
                    );

                    db.PerformanceMetrics.Add(new PerformanceMetric
                    {
                        MetricId = Guid.NewGuid(),
                        AgentId = ag.AgentId,
                        Date = DateTime.UtcNow.Date,
                        TotalCalls = 28,
                        AnsweredCalls = 26,
                        MissedCalls = 2,
                        AvgTalkTime = 142,
                        Occupancy = 84.50m
                    });
                }
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded AgentStatusHistories & PerformanceMetrics into MS SQL Server");
            }

            // 14. Ensure CallEvents
            if (!await db.CallEvents.AnyAsync())
            {
                var sampleCalls = await db.Calls.Take(5).ToListAsync();
                foreach (var call in sampleCalls)
                {
                    db.CallEvents.AddRange(
                        new CallEvent { EventId = Guid.NewGuid(), CallId = call.CallId, EventType = "CALL_INITIATED", EventTime = call.InitiatedAt, Description = "SIP INVITE received from carrier trunk" },
                        new CallEvent { EventId = Guid.NewGuid(), CallId = call.CallId, EventType = "CALL_RINGING", EventTime = call.InitiatedAt.AddSeconds(2), Description = "180 Ringing dispatched to agent extension" },
                        new CallEvent { EventId = Guid.NewGuid(), CallId = call.CallId, EventType = "CALL_ANSWERED", EventTime = call.AnsweredAt ?? call.InitiatedAt.AddSeconds(5), Description = "200 OK 2-way RTP audio connected" },
                        new CallEvent { EventId = Guid.NewGuid(), CallId = call.CallId, EventType = "CALL_ENDED", EventTime = call.EndedAt ?? call.InitiatedAt.AddSeconds(45), Description = "BYE session terminated" }
                    );
                }
                await db.SaveChangesAsync();
                logger.LogInformation("✓ Seeded CallEvents into MS SQL Server");
            }

            // 15. Ensure CRMActivities & CustomerCRMMappings
            if (!await db.CRMActivities.AnyAsync())
            {
                var sampleCust = await db.Customers.FirstOrDefaultAsync();
                var sampleCall = await db.Calls.FirstOrDefaultAsync();
                if (sampleCust != null)
                {
                    db.CRMActivities.AddRange(
                        new CRMActivity { ActivityId = Guid.NewGuid(), CustomerId = sampleCust.CustomerId, CallId = sampleCall?.CallId, ActivityType = "Inbound Call", Notes = "Customer reported intermittent connection loss in Gulshan-2 area. Line ping checked and escalated to field technician.", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) },
                        new CRMActivity { ActivityId = Guid.NewGuid(), CustomerId = sampleCust.CustomerId, CallId = null, ActivityType = "Support Ticket", Notes = "Field team assigned ticket #TK-8491 for optical cable splicing.", CreatedAt = DateTimeOffset.UtcNow.AddHours(-6) }
                    );

                    db.CustomerCRMMappings.Add(new CustomerCRMMapping
                    {
                        MappingId = Guid.NewGuid(),
                        CustomerId = sampleCust.CustomerId,
                        CRMId = "SF-CRM-01",
                        ExternalCustomerId = "SF-ACCT-984210",
                        LastSyncAt = DateTimeOffset.UtcNow.AddHours(-1),
                        IsActive = true
                    });
                    await db.SaveChangesAsync();
                    logger.LogInformation("✓ Seeded CRMActivities & CustomerCRMMappings into MS SQL Server");
                }
            }

            // 16. Ensure QAReviews
            if (!await db.QAReviews.AnyAsync())
            {
                var sampleCall = await db.Calls.FirstOrDefaultAsync();
                var sampleScorecard = await db.QAScorecards.FirstOrDefaultAsync();
                var supervisor = await db.Users.FirstOrDefaultAsync(u => u.Username == "supervisor");
                var agent = await db.Agents.FirstOrDefaultAsync();
                if (sampleCall != null && sampleScorecard != null && agent != null)
                {
                    db.QAReviews.Add(new QAReview
                    {
                        ReviewId = Guid.NewGuid(),
                        CallId = sampleCall.CallId,
                        ScorecardId = sampleScorecard.ScorecardId,
                        ReviewerId = supervisor != null ? supervisor.UserId : agent.UserId,
                        AgentId = sampleCall.AgentId ?? agent.AgentId,
                        Score = 92.50m,
                        Feedback = "Excellent greeting, fast troubleshooting step execution, polite wrap-up.",
                        CreatedAt = DateTimeOffset.UtcNow.AddHours(-3)
                    });
                    await db.SaveChangesAsync();
                    logger.LogInformation("✓ Seeded QAReviews into MS SQL Server");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding database tables in MS SQL Server");
        }
    }
}
