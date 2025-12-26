using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Core.EventBus;
using StatisticsAnalysisTool.Core.Events;
using StatisticsAnalysisTool.DamageMeter;
using System.Collections.ObjectModel;

namespace StatisticsAnalysisTool.ViewModels;

/// <summary>
/// Represents the view state for combat and damage meter tracking
/// </summary>
public class CombatTrackingViewModel : BaseViewModel
{
    private ObservableCollection<DamageMeterFragment> _damageMeter = new();
    private ObservableCollection<DamageMeterSnapshot> _damageMeterSnapshots = new();

    public CombatTrackingViewModel(IEventBus eventBus)
    {
        // Subscribe to combat events
        eventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        eventBus.Subscribe<DamageTakenEvent>(OnDamageTaken);
        eventBus.Subscribe<DamageMeterUpdateEvent>(OnDamageMeterUpdate);
        eventBus.Subscribe<DamageMeterResetEvent>(OnDamageMeterReset);
        eventBus.Subscribe<CombatModeChangedEvent>(OnCombatModeChanged);
        eventBus.Subscribe<DamageMeterSnapshotCreatedEvent>(OnSnapshotCreated);
        eventBus.Subscribe<DamageMeterSnapshotsLoadedEvent>(OnSnapshotsLoaded);
        eventBus.Subscribe<DamageMeterSnapshotsSavedEvent>(OnSnapshotsSaved);
    }

    private void OnDamageDealt(DamageDealtEvent evt)
    {
        // Event handled by controller updating state directly
    }

    private void OnDamageTaken(DamageTakenEvent evt)
    {
        // Event handled by controller updating state directly
    }

    private void OnDamageMeterUpdate(DamageMeterUpdateEvent evt)
    {
        // UI-specific updates if needed
    }

    private void OnDamageMeterReset(DamageMeterResetEvent evt)
    {
        // Event handled by controller updating state directly
    }

    private void OnCombatModeChanged(CombatModeChangedEvent evt)
    {
        // Event handled by controller updating state directly
    }

    private void OnSnapshotCreated(DamageMeterSnapshotCreatedEvent evt)
    {
        // Event handled by controller updating state directly
    }

    private void OnSnapshotsLoaded(DamageMeterSnapshotsLoadedEvent evt)
    {
        // Can be used for UI notifications
    }

    private void OnSnapshotsSaved(DamageMeterSnapshotsSavedEvent evt)
    {
        // Can be used for UI notifications
    }

    public ObservableCollection<DamageMeterFragment> DamageMeter
    {
        get => _damageMeter;
        set
        {
            _damageMeter = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<DamageMeterSnapshot> DamageMeterSnapshots
    {
        get => _damageMeterSnapshots;
        set
        {
            _damageMeterSnapshots = value;
            OnPropertyChanged();
        }
    }
}

