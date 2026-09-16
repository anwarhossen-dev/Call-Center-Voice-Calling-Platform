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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding database tables in MS SQL Server");
        }
    }
}
