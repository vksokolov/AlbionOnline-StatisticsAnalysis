using StatisticsAnalysisTool.Cluster;
using System;
using System.Collections.Generic;

namespace StatisticsAnalysisTool.Core.State;

/// <summary>
/// State container for cluster-related data (map/zone information)
/// </summary>
public class ClusterState
{
    /// <summary>
    /// Current cluster information
    /// </summary>
    public ClusterInfo CurrentCluster { get; } = new();

    /// <summary>
    /// History of entered clusters
    /// </summary>
    public List<ClusterInfo> ClusterHistory { get; } = new();

    /// <summary>
    /// Maximum number of clusters to keep in history
    /// </summary>
    public int MaxHistorySize { get; set; } = 500;
}

