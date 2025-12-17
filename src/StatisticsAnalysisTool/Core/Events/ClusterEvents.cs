using StatisticsAnalysisTool.Cluster;
using System;

namespace StatisticsAnalysisTool.Core.Events;

/// <summary>
/// Event raised when the player changes clusters (maps/zones)
/// </summary>
public class ClusterChangedEvent
{
    public ClusterInfo Cluster { get; }
    public DateTime Timestamp { get; }

    public ClusterChangedEvent(ClusterInfo cluster, DateTime timestamp)
    {
        Cluster = cluster;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when cluster history is updated
/// </summary>
public class ClusterHistoryUpdatedEvent
{
    public ClusterInfo Cluster { get; }
    public DateTime Timestamp { get; }

    public ClusterHistoryUpdatedEvent(ClusterInfo cluster, DateTime timestamp)
    {
        Cluster = cluster;
        Timestamp = timestamp;
    }
}

