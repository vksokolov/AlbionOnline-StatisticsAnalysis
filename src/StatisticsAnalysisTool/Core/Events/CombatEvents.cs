using StatisticsAnalysisTool.DamageMeter;
using StatisticsAnalysisTool.Enumerations;
using System;
using System.Collections.Generic;

namespace StatisticsAnalysisTool.Core.Events;

/// <summary>
/// Event raised when damage is dealt in combat
/// </summary>
public class DamageDealtEvent
{
    public long AffectedId { get; }
    public long CauserId { get; }
    public double HealthChange { get; }
    public double NewHealthValue { get; }
    public int CausingSpellIndex { get; }
    public DateTime Timestamp { get; }

    public DamageDealtEvent(long affectedId, long causerId, double healthChange, double newHealthValue, int causingSpellIndex, DateTime timestamp)
    {
        AffectedId = affectedId;
        CauserId = causerId;
        HealthChange = healthChange;
        NewHealthValue = newHealthValue;
        CausingSpellIndex = causingSpellIndex;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when damage is taken in combat
/// </summary>
public class DamageTakenEvent
{
    public long AffectedId { get; }
    public long CauserId { get; }
    public double HealthChange { get; }
    public double NewHealthValue { get; }
    public int CausingSpellIndex { get; }
    public DateTime Timestamp { get; }

    public DamageTakenEvent(long affectedId, long causerId, double healthChange, double newHealthValue, int causingSpellIndex, DateTime timestamp)
    {
        AffectedId = affectedId;
        CauserId = causerId;
        HealthChange = healthChange;
        NewHealthValue = newHealthValue;
        CausingSpellIndex = causingSpellIndex;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when damage meter UI needs to be updated
/// </summary>
public class DamageMeterUpdateEvent
{
    public List<DamageMeterFragment> DamageMeterFragments { get; }
    public DateTime Timestamp { get; }

    public DamageMeterUpdateEvent(List<DamageMeterFragment> damageMeterFragments, DateTime timestamp)
    {
        DamageMeterFragments = damageMeterFragments;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when damage meter is reset
/// </summary>
public class DamageMeterResetEvent
{
    public DateTime Timestamp { get; }

    public DamageMeterResetEvent(DateTime timestamp)
    {
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when combat mode changes for an entity
/// </summary>
public class CombatModeChangedEvent
{
    public long ObjectId { get; }
    public bool InActiveCombat { get; }
    public bool InPassiveCombat { get; }
    public DateTime Timestamp { get; }

    public CombatModeChangedEvent(long objectId, bool inActiveCombat, bool inPassiveCombat, DateTime timestamp)
    {
        ObjectId = objectId;
        InActiveCombat = inActiveCombat;
        InPassiveCombat = inPassiveCombat;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when a damage meter snapshot is taken
/// </summary>
public class DamageMeterSnapshotCreatedEvent
{
    public DamageMeterSnapshot Snapshot { get; }
    public DateTime Timestamp { get; }

    public DamageMeterSnapshotCreatedEvent(DamageMeterSnapshot snapshot, DateTime timestamp)
    {
        Snapshot = snapshot;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when damage meter snapshots are loaded from file
/// </summary>
public class DamageMeterSnapshotsLoadedEvent
{
    public int SnapshotsCount { get; }
    public DateTime Timestamp { get; }

    public DamageMeterSnapshotsLoadedEvent(int snapshotsCount, DateTime timestamp)
    {
        SnapshotsCount = snapshotsCount;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when damage meter snapshots are saved to file
/// </summary>
public class DamageMeterSnapshotsSavedEvent
{
    public int SnapshotsCount { get; }
    public DateTime Timestamp { get; }

    public DamageMeterSnapshotsSavedEvent(int snapshotsCount, DateTime timestamp)
    {
        SnapshotsCount = snapshotsCount;
        Timestamp = timestamp;
    }
}

