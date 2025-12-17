using StatisticsAnalysisTool.Cluster;
using StatisticsAnalysisTool.Dungeon.Models;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Models.NetworkModel;
using System;
using DungeonValueType = StatisticsAnalysisTool.Enumerations.ValueType;

namespace StatisticsAnalysisTool.Core.Events;

/// <summary>
/// Event raised when a new dungeon is added to tracking
/// </summary>
public class DungeonAddedEvent
{
    public DungeonBaseFragment Dungeon { get; }
    public DateTime Timestamp { get; }

    public DungeonAddedEvent(DungeonBaseFragment dungeon, DateTime timestamp)
    {
        Dungeon = dungeon;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when a dungeon status changes (Active, Done, etc.)
/// </summary>
public class DungeonStatusChangedEvent
{
    public Guid DungeonGuid { get; }
    public DungeonStatus Status { get; }
    public DateTime Timestamp { get; }

    public DungeonStatusChangedEvent(Guid dungeonGuid, DungeonStatus status, DateTime timestamp)
    {
        DungeonGuid = dungeonGuid;
        Status = status;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when dungeon collection is updated (refresh needed)
/// </summary>
public class DungeonsUpdatedEvent
{
    public int TotalDungeons { get; }
    public DateTime Timestamp { get; }

    public DungeonsUpdatedEvent(int totalDungeons, DateTime timestamp)
    {
        TotalDungeons = totalDungeons;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when a dungeon is removed
/// </summary>
public class DungeonRemovedEvent
{
    public string DungeonHash { get; }
    public DateTime Timestamp { get; }

    public DungeonRemovedEvent(string dungeonHash, DateTime timestamp)
    {
        DungeonHash = dungeonHash;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when dungeons are cleared/reset
/// </summary>
public class DungeonsClearedEvent
{
    public DateTime Timestamp { get; }

    public DungeonsClearedEvent(DateTime timestamp)
    {
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when loot is added to current dungeon
/// </summary>
public class DungeonLootAddedEvent
{
    public Guid DungeonGuid { get; }
    public DiscoveredItem LootItem { get; }
    public DateTime Timestamp { get; }

    public DungeonLootAddedEvent(Guid dungeonGuid, DiscoveredItem lootItem, DateTime timestamp)
    {
        DungeonGuid = dungeonGuid;
        LootItem = lootItem;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when a value (fame, silver, etc.) is added to a dungeon
/// </summary>
public class DungeonValueAddedEvent
{
    public Guid DungeonGuid { get; }
    public double Value { get; }
    public DungeonValueType ValueType { get; }
    public DateTime Timestamp { get; }

    public DungeonValueAddedEvent(Guid dungeonGuid, double value, DungeonValueType valueType, DateTime timestamp)
    {
        DungeonGuid = dungeonGuid;
        Value = value;
        ValueType = valueType;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when a chest is opened in a dungeon
/// </summary>
public class DungeonChestOpenedEvent
{
    public Guid DungeonGuid { get; }
    public int ChestId { get; }
    public DateTime Timestamp { get; }

    public DungeonChestOpenedEvent(Guid dungeonGuid, int chestId, DateTime timestamp)
    {
        DungeonGuid = dungeonGuid;
        ChestId = chestId;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when dungeon save timer visibility should change
/// </summary>
public class DungeonSaveTimerVisibilityChangedEvent
{
    public bool IsVisible { get; }
    public MapType MapType { get; }
    public DateTime Timestamp { get; }

    public DungeonSaveTimerVisibilityChangedEvent(bool isVisible, MapType mapType, DateTime timestamp)
    {
        IsVisible = isVisible;
        MapType = mapType;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when dungeons are loaded from file
/// </summary>
public class DungeonsLoadedEvent
{
    public int DungeonsCount { get; }
    public DateTime Timestamp { get; }

    public DungeonsLoadedEvent(int dungeonsCount, DateTime timestamp)
    {
        DungeonsCount = dungeonsCount;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when dungeons are saved to file
/// </summary>
public class DungeonsSavedEvent
{
    public int DungeonsCount { get; }
    public DateTime Timestamp { get; }

    public DungeonsSavedEvent(int dungeonsCount, DateTime timestamp)
    {
        DungeonsCount = dungeonsCount;
        Timestamp = timestamp;
    }
}

