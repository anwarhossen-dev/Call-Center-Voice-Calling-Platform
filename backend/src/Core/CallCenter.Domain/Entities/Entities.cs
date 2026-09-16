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

    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    [MaxLength(512)]
    public string PasswordHash { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Agent? AgentProfile { get; set; }
}

public class Agent
{
    public Guid AgentId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(20)]
    public string Extension { get; set; } = string.Empty;
    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;
    public AgentState CurrentState { get; set; } = AgentState.Offline;
    public DateTimeOffset StateChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<AgentQueue> AgentQueues { get; set; } = new List<AgentQueue>();
    public ICollection<AgentStateLog> StateLogs { get; set; } = new List<AgentStateLog>();
    public ICollection<Call> HandledCalls { get; set; } = new List<Call>();
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

public class Queue
{
    public Guid QueueId { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string QueueName { get; set; } = string.Empty;
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

public class Disposition
{
    public Guid DispositionId { get; set; } = Guid.NewGuid();
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    [MaxLength(100)]
    public string Category { get; set; } = "General";
    public bool RequiresFollowup { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class Call
{
    public Guid CallId { get; set; } = Guid.NewGuid();
    [MaxLength(64)]
    public string CallUuid { get; set; } = string.Empty; // Telephony Core UUID
    public CallDirection Direction { get; set; } = CallDirection.Inbound;
    [MaxLength(50)]
    public string CallerNumber { get; set; } = string.Empty;
    [MaxLength(50)]
    public string DestinationNumber { get; set; } = string.Empty;
    public Guid? QueueId { get; set; }
    public Queue? Queue { get; set; }
    public Guid? AgentId { get; set; }
    public Agent? Agent { get; set; }
    public Guid? DispositionId { get; set; }
    public Disposition? Disposition { get; set; }
    public CallStatus Status { get; set; } = CallStatus.Initiated;
    public DateTimeOffset InitiatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AnsweredAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public int TotalDurationSeconds { get; set; }
    public int TalkDurationSeconds { get; set; }
    public int HoldDurationSeconds { get; set; }
    public int WaitDurationSeconds { get; set; }
    [MaxLength(100)]
    public string? CRMContactId { get; set; }
    [MaxLength(1000)]
    public string? AgentNotes { get; set; }

    public CallRecording? Recording { get; set; }
    public ICollection<CallSession> Sessions { get; set; } = new List<CallSession>();
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
    [MaxLength(100)]
    public string StorageBucket { get; set; } = "call-center-recordings";
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;
    [MaxLength(64)]
    public string FileHashSHA256 { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int DurationSeconds { get; set; }
    [MaxLength(20)]
    public string AudioChannels { get; set; } = "Stereo";
    public bool IsArchived { get; set; } = false;
    public DateTimeOffset OffloadedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Campaign
{
    public Guid CampaignId { get; set; } = Guid.NewGuid();
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Type { get; set; } = "Inbound"; // Inbound, OutboundPreview
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Paused, Completed
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndDate { get; set; }
}
