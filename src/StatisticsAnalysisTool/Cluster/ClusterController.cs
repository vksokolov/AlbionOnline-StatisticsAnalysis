using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Core.EventBus;
using StatisticsAnalysisTool.Core.Events;
using StatisticsAnalysisTool.Core.State;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.GameFileData;
using StatisticsAnalysisTool.Network.Manager;
using System;
using System.Diagnostics;

namespace StatisticsAnalysisTool.Cluster;

public sealed class ClusterController
{
    private readonly TrackingController _trackingController;
    private readonly IEventBus _eventBus;

    public ClusterState State { get; } = new();
    
    /// <summary>
    /// Static accessor for backward compatibility - will be removed in future
    /// </summary>
    [Obsolete("Use State.CurrentCluster instead")]
    public static ClusterInfo CurrentCluster => _instance?.State.CurrentCluster ?? new ClusterInfo();
    
    private static ClusterController _instance;

    public ClusterController(TrackingController trackingController, IEventBus eventBus)
    {
        _trackingController = trackingController;
        _eventBus = eventBus;
        _instance = this;

        CreateRandomClusterInfosForTracking(0);
    }

    public void RegisterEvents()
    {
        OnChangeCluster += UpdateClusterTracking;
        OnChangeCluster += SetAndResetValues;
        OnChangeCluster += SaveUserData;
    }

    public void UnregisterEvents()
    {
        OnChangeCluster -= UpdateClusterTracking;
        OnChangeCluster -= SetAndResetValues;
        OnChangeCluster -= SaveUserData;
    }

    public event Action<ClusterInfo> OnChangeCluster;

    public void ChangeClusterInformation(MapType mapType, Guid? mapGuid, string clusterIndex, string instanceName, string worldMapDataType, byte[] dungeonInformation, string mainClusterIndex, Tier mistsDungeonTier)
    {
        State.CurrentCluster.ClusterInfoFullyAvailable = false;
        State.CurrentCluster.SetClusterInfo(mapType, mapGuid, clusterIndex, instanceName, worldMapDataType, dungeonInformation, mainClusterIndex, mistsDungeonTier);
    }

    public void SetJoinClusterInformation(string index, string mainClusterIndex, Guid? mapGuid)
    {
        State.CurrentCluster.SetJoinClusterInfo(index, mainClusterIndex, mapGuid);
        State.CurrentCluster.ClusterInfoFullyAvailable = true;

        if (_trackingController.IsTrackingAllowedByMainCharacter())
        {
            OnChangeCluster?.Invoke(State.CurrentCluster);
            
            // Publish event for UI layer
            _eventBus.Publish(new ClusterChangedEvent(State.CurrentCluster, DateTime.UtcNow));
        }

        Debug.Print($"[StateHandler] Changed cluster to: Index: '{State.CurrentCluster.Index}' UniqueName: '{State.CurrentCluster.UniqueName}' ClusterType: '{State.CurrentCluster.ClusterMode}' MapType: '{State.CurrentCluster.MapType}'");
    }

    public void SetAndResetValues(ClusterInfo currentCluster)
    {
        _trackingController.TradeController.ResetCraftingBuildingInfo();
        _trackingController.CombatController.ResetDamageMeterByClusterChange();
        _trackingController.VaultController.ResetDiscoveredItems();
        _trackingController.VaultController.ResetInternalVaultContainer();
        _trackingController.VaultController.ResetCurrentInternalVault();
        _trackingController.TreasureController.RemoveTemporaryTreasures();
        _trackingController.TreasureController.UpdateLootedChestsDashboardUi();
        _trackingController.LootController.ResetLocalPlayerDiscoveredLoot();
        _trackingController.LootController.ResetIdentifiedBodies();
        _ = _trackingController.TradeController.RemoveTradesByDaysInSettingsAsync();
        _ = _trackingController.GatheringController.SetGatheredResourcesClosedAsync();
        _trackingController.PartyController.UpdateIsPlayerInspectedToFalse();
    }

    public static string ComposingMapInfoString(string index, MapType mapType, string instanceName)
    {
        var currentMapName = WorldData.GetUniqueNameOrDefault(index);

        if (string.IsNullOrEmpty(currentMapName))
        {
            currentMapName = WorldData.GetMapNameByMapType(mapType);
        }

        string islandName = !string.IsNullOrEmpty(instanceName) ? $"({instanceName})" : string.Empty;

        return $"{currentMapName} {islandName}";
    }

    #region Cluster history

    private void UpdateClusterTracking(ClusterInfo currentCluster)
    {
        var newCluster = new ClusterInfo(currentCluster);
        State.ClusterHistory.Insert(0, newCluster);
        RemovesClusterIfMoreThanLimit();
        
        // Publish event for UI updates
        _eventBus.Publish(new ClusterHistoryUpdatedEvent(newCluster, DateTime.UtcNow));
    }

    private void RemovesClusterIfMoreThanLimit()
    {
        if (State.ClusterHistory.Count > State.MaxHistorySize)
        {
            State.ClusterHistory.RemoveAt(State.ClusterHistory.Count - 1);
        }
    }

    #endregion



    #region Save UserData

    private static void SaveUserData(ClusterInfo currentCluster)
    {
        RateLimitedAction.Run(CriticalData.Save);
    }

    #endregion

    #region Test methods

    private static readonly Random Random = new();

    private static T RandomEnumValue<T>()
    {
        var v = Enum.GetValues(typeof(T));
        return (T) v.GetValue(Random.Next(v.Length));
    }

    private void CreateRandomClusterInfosForTracking(int runs)
    {
        for (var i = 0; i < runs; i++)
        {
            var clusterInfo = new ClusterInfo();
            var value = RandomEnumValue<MapType>();

            clusterInfo.SetClusterInfo(value, Guid.NewGuid(), "3000", "Meine Super Insel", "@ISLAND", null, "4001", Tier.T7);

            UpdateClusterTracking(clusterInfo);
        }
    }

    #endregion
}