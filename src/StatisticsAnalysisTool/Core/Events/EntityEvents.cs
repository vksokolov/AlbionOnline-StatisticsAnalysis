using System;

namespace StatisticsAnalysisTool.Core.Events;

/// <summary>
/// Event raised when party members are updated (added, removed, or modified)
/// </summary>
public class PartyMembersUpdatedEvent
{
    public int PartyMemberCount { get; }
    public DateTime Timestamp { get; }

    public PartyMembersUpdatedEvent(int partyMemberCount, DateTime timestamp)
    {
        PartyMemberCount = partyMemberCount;
        Timestamp = timestamp;
    }
}

