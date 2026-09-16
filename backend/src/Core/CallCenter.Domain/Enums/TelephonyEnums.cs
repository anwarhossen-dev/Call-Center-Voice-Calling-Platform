namespace CallCenter.Domain.Enums;

public enum AgentState
{
    Offline = 0,
    Available = 1,
    Reserved = 2,
    OnCall = 3,
    WrapUp = 4, // ACW (After Call Work)
    Break = 5,
    Lunch = 6,
    Training = 7
}

public enum CallDirection
{
    Inbound = 1,
    Outbound = 2,
    Internal = 3
}

public enum CallStatus
{
    Initiated = 1,
    Ringing = 2,
    Answered = 3,
    Completed = 4,
    Abandoned = 5,
    Busy = 6,
    Failed = 7
}

public enum SupervisorInterventionMode
{
    SilentSpy = 1, // Listen-only
    Whisper = 2,   // Agent can hear, customer cannot
    BargeIn = 3    // Full 3-way conference
}
