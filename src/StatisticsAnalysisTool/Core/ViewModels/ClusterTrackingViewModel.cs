using StatisticsAnalysisTool.Cluster;
using StatisticsAnalysisTool.Core.EventBus;
using StatisticsAnalysisTool.Core.Events;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Models.BindingModel;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace StatisticsAnalysisTool.Core.ViewModels;

/// <summary>
/// ViewModel for cluster tracking UI - subscribes to cluster events from ClusterController
/// </summary>
public class ClusterTrackingViewModel : BaseViewModel, IDisposable
{
    private readonly IEventBus _eventBus;
    private readonly IDisposable _clusterChangedSubscription;
    private readonly IDisposable _clusterHistorySubscription;
    private readonly UserTrackingBindings _userTrackingBindings;

    public ObservableCollection<ClusterInfo> EnteredClusters { get; } = new();

    public ClusterTrackingViewModel(IEventBus eventBus, UserTrackingBindings userTrackingBindings)
    {
        _eventBus = eventBus;
        _userTrackingBindings = userTrackingBindings;

        // Subscribe to cluster events
        _clusterChangedSubscription = _eventBus.Subscribe<ClusterChangedEvent>(OnClusterChanged);
        _clusterHistorySubscription = _eventBus.Subscribe<ClusterHistoryUpdatedEvent>(OnClusterHistoryUpdated);
    }

    private void OnClusterChanged(ClusterChangedEvent evt)
    {
        // Update current map info on UI thread
        Application.Current?.Dispatcher?.InvokeAsync(() =>
        {
            _userTrackingBindings.CurrentMapInfoBinding.Tier = evt.Cluster.TierString;
            _userTrackingBindings.CurrentMapInfoBinding.ClusterMode = evt.Cluster.ClusterMode;
            _userTrackingBindings.CurrentMapInfoBinding.ComposingMapInfoString(evt.Cluster);
        });
    }

    private void OnClusterHistoryUpdated(ClusterHistoryUpdatedEvent evt)
    {
        // Update cluster history on UI thread
        Application.Current?.Dispatcher?.InvokeAsync(() =>
        {
            EnteredClusters.Insert(0, evt.Cluster);
        });
    }

    public void Dispose()
    {
        _clusterChangedSubscription?.Dispose();
        _clusterHistorySubscription?.Dispose();
    }
}

