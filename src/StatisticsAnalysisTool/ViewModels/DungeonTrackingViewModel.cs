using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Core.EventBus;
using StatisticsAnalysisTool.Core.Events;
using StatisticsAnalysisTool.Dungeon.Models;
using System.Windows;

namespace StatisticsAnalysisTool.ViewModels;

/// <summary>
/// Represents the view state for dungeon tracking
/// </summary>
public class DungeonTrackingViewModel : BaseViewModel
{
    private ObservableRangeCollection<DungeonBaseFragment> _dungeons = new();
    private Visibility _saveTimerVisibility = Visibility.Collapsed;

    public DungeonTrackingViewModel(IEventBus eventBus)
    {
        // Subscribe to dungeon events
        eventBus.Subscribe<DungeonAddedEvent>(OnDungeonAdded);
        eventBus.Subscribe<DungeonStatusChangedEvent>(OnDungeonStatusChanged);
        eventBus.Subscribe<DungeonsUpdatedEvent>(OnDungeonsUpdated);
        eventBus.Subscribe<DungeonRemovedEvent>(OnDungeonRemoved);
        eventBus.Subscribe<DungeonsClearedEvent>(OnDungeonsCleared);
        eventBus.Subscribe<DungeonSaveTimerVisibilityChangedEvent>(OnSaveTimerVisibilityChanged);
        eventBus.Subscribe<DungeonsLoadedEvent>(OnDungeonsLoaded);
        eventBus.Subscribe<DungeonsSavedEvent>(OnDungeonsSaved);
    }

    private void OnDungeonAdded(DungeonAddedEvent evt)
    {
        // Event handled by controller updating state directly
        // UI binds to controller state
    }

    private void OnDungeonStatusChanged(DungeonStatusChangedEvent evt)
    {
        // Event handled by controller updating state directly
    }

    private void OnDungeonsUpdated(DungeonsUpdatedEvent evt)
    {
        // Trigger any UI-specific updates if needed
    }

    private void OnDungeonRemoved(DungeonRemovedEvent evt)
    {
        // Event handled by controller updating state directly
    }

    private void OnDungeonsCleared(DungeonsClearedEvent evt)
    {
        // Event handled by controller updating state directly
    }

    private void OnSaveTimerVisibilityChanged(DungeonSaveTimerVisibilityChangedEvent evt)
    {
        SaveTimerVisibility = evt.IsVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnDungeonsLoaded(DungeonsLoadedEvent evt)
    {
        // Can be used for UI notifications
    }

    private void OnDungeonsSaved(DungeonsSavedEvent evt)
    {
        // Can be used for UI notifications
    }

    public ObservableRangeCollection<DungeonBaseFragment> Dungeons
    {
        get => _dungeons;
        set
        {
            _dungeons = value;
            OnPropertyChanged();
        }
    }

    public Visibility SaveTimerVisibility
    {
        get => _saveTimerVisibility;
        set
        {
            _saveTimerVisibility = value;
            OnPropertyChanged();
        }
    }
}

