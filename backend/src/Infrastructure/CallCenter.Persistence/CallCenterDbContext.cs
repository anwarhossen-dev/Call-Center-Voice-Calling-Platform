using CallCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallCenter.Persistence;

public class CallCenterDbContext : DbContext
{
    public CallCenterDbContext(DbContextOptions<CallCenterDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentStateLog> AgentStateLogs => Set<AgentStateLog>();
    public DbSet<Queue> Queues => Set<Queue>();
    public DbSet<AgentQueue> AgentQueues => Set<AgentQueue>();
    public DbSet<Call> Calls => Set<Call>();
    public DbSet<CallSession> CallSessions => Set<CallSession>();
    public DbSet<CallRecording> CallRecordings => Set<CallRecording>();
    public DbSet<Disposition> Dispositions => Set<Disposition>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<AgentStatusHistory> AgentStatusHistories => Set<AgentStatusHistory>();
    public DbSet<PerformanceMetric> PerformanceMetrics => Set<PerformanceMetric>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CallEvent> CallEvents => Set<CallEvent>();
    public DbSet<CRMActivity> CRMActivities => Set<CRMActivity>();
    public DbSet<CustomerCRMMapping> CustomerCRMMappings => Set<CustomerCRMMapping>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<QAScorecard> QAScorecards => Set<QAScorecard>();
    public DbSet<QAReview> QAReviews => Set<QAReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Explicit Primary Keys
        modelBuilder.Entity<Role>().HasKey(r => r.RoleId);
        modelBuilder.Entity<UserRole>().HasKey(ur => ur.Id);
        modelBuilder.Entity<User>().HasKey(u => u.UserId);
        modelBuilder.Entity<Team>().HasKey(t => t.TeamId);
        modelBuilder.Entity<Agent>().HasKey(a => a.AgentId);
        modelBuilder.Entity<AgentStatusHistory>().HasKey(h => h.Id);
        modelBuilder.Entity<AgentStateLog>().HasKey(l => l.Id);
        modelBuilder.Entity<PerformanceMetric>().HasKey(pm => pm.MetricId);
        modelBuilder.Entity<Queue>().HasKey(q => q.QueueId);
        modelBuilder.Entity<AgentQueue>().HasKey(aq => new { aq.AgentId, aq.QueueId });
        modelBuilder.Entity<Campaign>().HasKey(c => c.CampaignId);
        modelBuilder.Entity<Disposition>().HasKey(d => d.DispositionId);
        modelBuilder.Entity<Customer>().HasKey(c => c.CustomerId);
        modelBuilder.Entity<Call>().HasKey(c => c.CallId);
        modelBuilder.Entity<CallEvent>().HasKey(ce => ce.EventId);
        modelBuilder.Entity<CallSession>().HasKey(s => s.SessionId);
        modelBuilder.Entity<CallRecording>().HasKey(r => r.RecordingId);
        modelBuilder.Entity<CRMActivity>().HasKey(ca => ca.ActivityId);
        modelBuilder.Entity<CustomerCRMMapping>().HasKey(m => m.MappingId);
        modelBuilder.Entity<AuditLog>().HasKey(al => al.LogId);
        modelBuilder.Entity<SystemSetting>().HasKey(ss => ss.SettingId);
        modelBuilder.Entity<QAScorecard>().HasKey(sc => sc.ScorecardId);
        modelBuilder.Entity<QAReview>().HasKey(qr => qr.ReviewId);

        // Precision configurations
        modelBuilder.Entity<PerformanceMetric>()
            .Property(p => p.Occupancy)
            .HasPrecision(5, 2);

        modelBuilder.Entity<QAReview>()
            .Property(q => q.Score)
            .HasPrecision(5, 2);

        // Prevent cycle / multiple cascade paths in SQL Server
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Indexes for high performance lookup
        modelBuilder.Entity<Customer>().HasIndex(c => c.Phone);
        modelBuilder.Entity<Customer>().HasIndex(c => c.Email);
        modelBuilder.Entity<SystemSetting>().HasIndex(s => s.SettingKey).IsUnique();
        modelBuilder.Entity<AuditLog>().HasIndex(al => al.CreatedAt);

        // Unique indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Agent>()
            .HasIndex(a => a.Extension)
            .IsUnique();

        modelBuilder.Entity<Call>()
            .HasIndex(c => c.CallUuid)
            .IsUnique();

        modelBuilder.Entity<Call>()
            .HasIndex(c => new { c.InitiatedAt, c.Status });

        modelBuilder.Entity<Call>()
            .HasIndex(c => c.CallerNumber);

        // Seed initial roles and dispositions with deterministic static GUIDs
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supervisorRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var agentRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = adminRoleId, RoleName = "Admin", Description = "System Administrator" },
            new Role { RoleId = supervisorRoleId, RoleName = "Supervisor", Description = "Call Center Team Lead / Supervisor" },
            new Role { RoleId = agentRoleId, RoleName = "Agent", Description = "Contact Center Agent" }
        );

        modelBuilder.Entity<Disposition>().HasData(
            new Disposition { DispositionId = Guid.Parse("d1111111-1111-1111-1111-111111111111"), Code = "RESOLVED", Description = "Query Resolved on Call", Category = "Support" },
            new Disposition { DispositionId = Guid.Parse("d2222222-2222-2222-2222-222222222222"), Code = "ESCALATED", Description = "Issue Escalated to Tier 2", Category = "Support", RequiresFollowup = true },
            new Disposition { DispositionId = Guid.Parse("d3333333-3333-3333-3333-333333333333"), Code = "CALLBACK_REQ", Description = "Customer Requested Callback", Category = "General", RequiresFollowup = true },
            new Disposition { DispositionId = Guid.Parse("d4444444-4444-4444-4444-444444444444"), Code = "INTERESTED", Description = "Prospect Interested in Offer", Category = "Sales" },
            new Disposition { DispositionId = Guid.Parse("d5555555-5555-5555-5555-555555555555"), Code = "NOT_INTERESTED", Description = "Prospect Declined Offer", Category = "Sales" }
        );
    }
}
