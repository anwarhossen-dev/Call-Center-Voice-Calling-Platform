using System.ComponentModel.DataAnnotations;
using CallCenter.Domain.Enums;

namespace CallCenter.Domain.Entities;

public class Role
{
    public Guid RoleId { get; set; } = Guid.NewGuid();
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;
    [MaxLength(255)]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class User
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(512)]
    public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(20)]
    public string? Phone { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Agent? AgentProfile { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class Team
{
    public Guid TeamId { get; set; } = Guid.NewGuid();
    [MaxLength(50)]
    public string TeamName { get; set; } = string.Empty;
    [MaxLength(255)]
    public string? Description { get; set; }
    public Guid? ManagerId { get; set; }
    public User? Manager { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Agent> Agents { get; set; } = new List<Agent>();
}

public class Agent
{
    public Guid AgentId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    [MaxLength(20)]
    public string AgentCode { get; set; } = string.Empty;
    [MaxLength(20)]
    public string Extension { get; set; } = string.Empty;
    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;
    [MaxLength(20)]
    public string Status { get; set; } = "Offline";
    public AgentState CurrentState { get; set; } = AgentState.Offline;
    public DateTime? HireDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset StateChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<AgentQueue> AgentQueues { get; set; } = new List<AgentQueue>();
    public ICollection<AgentStateLog> StateLogs { get; set; } = new List<AgentStateLog>();
    public ICollection<AgentStatusHistory> StatusHistories { get; set; } = new List<AgentStatusHistory>();
    public ICollection<PerformanceMetric> PerformanceMetrics { get; set; } = new List<PerformanceMetric>();
    public ICollection<Call> HandledCalls { get; set; } = new List<Call>();
}

public class AgentStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public Agent? Agent { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "Offline";
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndTime { get; set; }
}

public class AgentStateLog
{
    public long Id { get; set; }
    public Guid AgentId { get; set; }
    public Agent? Agent { get; set; }
    public AgentState State { get; set; }
    [MaxLength(100)]
    public string? SubReason { get; set; }
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndTime { get; set; }
    public int? DurationSeconds { get; set; }
}

public class PerformanceMetric
{
    public Guid MetricId { get; set; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public Agent? Agent { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public int TotalCalls { get; set; }
    public int AnsweredCalls { get; set; }
    public int MissedCalls { get; set; }
    public int AvgTalkTime { get; set; }
    public decimal Occupancy { get; set; }
}

public class Queue
{
    public Guid QueueId { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string QueueName { get; set; } = string.Empty;
    [MaxLength(255)]
    public string? Description { get; set; }
    public int Priority { get; set; } = 1;
    [MaxLength(50)]
    public string Strategy { get; set; } = "LongestIdle"; // LongestIdle, RoundRobin, RingAll
    public int SLAThresholdSeconds { get; set; } = 20;
    public int MaxWaitTimeoutSeconds { get; set; } = 300;
    public bool IsActive { get; set; } = true;

    public ICollection<AgentQueue> AgentQueues { get; set; } = new List<AgentQueue>();
    public ICollection<Call> Calls { get; set; } = new List<Call>();
}

public class AgentQueue
{
    public Guid AgentId { get; set; }
    public Agent? Agent { get; set; }
    public Guid QueueId { get; set; }
    public Queue? Queue { get; set; }
    public int SkillLevel { get; set; } = 1; // 1 - 10
}

public class Campaign
{
    public Guid CampaignId { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string CampaignName { get; set; } = string.Empty;
    public string Name { get => CampaignName; set => CampaignName = value; }
    [MaxLength(255)]
    public string? Description { get; set; }
    [MaxLength(50)]
    public string Type { get; set; } = "Inbound"; // Inbound, OutboundPreview, OutboundPredictive
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Paused, Completed
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Call> Calls { get; set; } = new List<Call>();
}

public class Disposition
{
    public Guid DispositionId { get; set; } = Guid.NewGuid();
    [MaxLength(50)]
    public string DispositionName { get; set; } = string.Empty;
    public string Code { get => DispositionName; set => DispositionName = value; }
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Category { get; set; } = "General";
    public bool RequiresFollowup { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class Customer
{
    public Guid CustomerId { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? Email { get; set; }
    public string? Address { get; set; }
    [MaxLength(50)]
    public string? CRMId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Call> Calls { get; set; } = new List<Call>();
    public ICollection<CRMActivity> CRMActivities { get; set; } = new List<CRMActivity>();
    public ICollection<CustomerCRMMapping> Mappings { get; set; } = new List<CustomerCRMMapping>();
}

public class Call
{
    public Guid CallId { get; set; } = Guid.NewGuid();
    [MaxLength(64)]
    public string CallUuid { get; set; } = string.Empty; // Telephony Core UUID
    [MaxLength(20)]
    public string CallerNumber { get; set; } = string.Empty;
    [MaxLength(50)]
    public string DestinationNumber { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }
    public Guid? QueueId { get; set; }
    public Queue? Queue { get; set; }
    public Guid? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }
    [MaxLength(10)]
    public string Type { get; set; } = "Inbound";
    public CallDirection Direction { get; set; } = CallDirection.Inbound;
    public CallStatus Status { get; set; } = CallStatus.Initiated;
    [MaxLength(20)]
    public string StatusText { get; set; } = "Initiated";
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset InitiatedAt { get => StartTime; set => StartTime = value; }
    public DateTimeOffset? AnsweredAt { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public DateTimeOffset? EndedAt { get => EndTime; set => EndTime = value; }
    public int Duration { get; set; }
    public int TotalDurationSeconds { get => Duration; set => Duration = value; }
    public int TalkDurationSeconds { get; set; }
    public int HoldDurationSeconds { get; set; }
    public int WaitDurationSeconds { get; set; }
    public Guid? DispositionId { get; set; }
    public Disposition? Disposition { get; set; }
    [MaxLength(100)]
    public string? CRMContactId { get; set; }
    [MaxLength(1000)]
    public string? AgentNotes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public CallRecording? Recording { get; set; }
    public ICollection<CallEvent> Events { get; set; } = new List<CallEvent>();
    public ICollection<CallSession> Sessions { get; set; } = new List<CallSession>();
    public ICollection<CRMActivity> CRMActivities { get; set; } = new List<CRMActivity>();
}

public class CallEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid CallId { get; set; }
    public Call? Call { get; set; }
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset EventTime { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(255)]
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class CallSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid CallId { get; set; }
    public Call? Call { get; set; }
    [MaxLength(20)]
    public string LegType { get; set; } = "A"; // A=Caller, B=Agent, C=Supervisor
    [MaxLength(64)]
    public string ChannelUuid { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndTime { get; set; }
    [MaxLength(100)]
    public string? HangupCause { get; set; }
}

public class CallRecording
{
    public Guid RecordingId { get; set; } = Guid.NewGuid();
    public Guid CallId { get; set; }
    public Call? Call { get; set; }
    [MaxLength(255)]
    public string FilePath { get; set; } = string.Empty;
    [MaxLength(100)]
    public string FileName { get; set; } = string.Empty;
    public int Duration { get; set; }
    public int DurationSeconds { get => Duration; set => Duration = value; }
    public long FileSize { get; set; }
    public long FileSizeBytes { get => FileSize; set => FileSize = value; }
    [MaxLength(255)]
    public string StorageUrl { get; set; } = string.Empty;
    [MaxLength(100)]
    public string StorageBucket { get; set; } = "call-center-recordings";
    [MaxLength(500)]
    public string StoragePath { get => string.IsNullOrEmpty(StorageUrl) ? FilePath : StorageUrl; set { StorageUrl = value; FilePath = value; } }
    [MaxLength(64)]
    public string FileHashSHA256 { get; set; } = string.Empty;
    [MaxLength(20)]
    public string AudioChannels { get; set; } = "Stereo";
    public bool IsArchived { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset OffloadedAt { get => CreatedAt; set => CreatedAt = value; }
}

public class CRMActivity
{
    public Guid ActivityId { get; set; } = Guid.NewGuid();
    public Guid? CallId { get; set; }
    public Call? Call { get; set; }
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    [MaxLength(50)]
    public string ActivityType { get; set; } = "Call";
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class CustomerCRMMapping
{
    public Guid MappingId { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    [MaxLength(50)]
    public string CRMId { get; set; } = string.Empty;
    [MaxLength(50)]
    public string ExternalCustomerId { get; set; } = string.Empty;
    public DateTimeOffset LastSyncAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class AuditLog
{
    public Guid LogId { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Entity { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    [MaxLength(45)]
    public string? IPAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class SystemSetting
{
    public Guid SettingId { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    [MaxLength(255)]
    public string? Description { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class QAScorecard
{
    public Guid ScorecardId { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public string CriteriaJson { get; set; } = "[]";
    public int PassThreshold { get; set; } = 80;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class QAReview
{
    public Guid ReviewId { get; set; } = Guid.NewGuid();
    public Guid CallId { get; set; }
    public Call? Call { get; set; }
    public Guid? ScorecardId { get; set; }
    public QAScorecard? Scorecard { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid AgentId { get; set; }
    public Agent? Agent { get; set; }
    public decimal Score { get; set; }
    public string? Feedback { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
