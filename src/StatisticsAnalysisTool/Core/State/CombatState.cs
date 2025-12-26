using StatisticsAnalysisTool.DamageMeter;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace StatisticsAnalysisTool.Core.State;

/// <summary>
/// State container for combat and damage meter tracking data
/// </summary>
public class CombatState
{
    /// <summary>
    /// Collection of damage meter fragments for current combat session
    /// </summary>
    public ObservableCollection<DamageMeterFragment> DamageMeter { get; } = new();

    /// <summary>
    /// List of saved damage meter snapshots
    /// </summary>
    public ObservableCollection<DamageMeterSnapshot> DamageMeterSnapshots { get; } = new();

    /// <summary>
    /// Last recorded health values for players (for overheal detection)
    /// </summary>
    public ConcurrentDictionary<Guid, double> LastPlayersHealth { get; } = new();

    /// <summary>
    /// Tracks if the combat mode was in "combat over" state
    /// </summary>
    public bool CombatModeWasCombatOver { get; set; }

    /// <summary>
    /// Last time the damage UI was updated (for throttling)
    /// </summary>
    public DateTime LastDamageUiUpdate { get; set; }

    /// <summary>
    /// Flag indicating if UI update is currently active
    /// </summary>
    public bool IsUiUpdateActive { get; set; }

    /// <summary>
    /// Whether to count only damage to players (not NPCs)
    /// </summary>
    public bool OnlyDamageToPlayersCounts { get; set; }

    /// <summary>
    /// Whether to reset damage meter when map changes
    /// </summary>
    public bool IsDamageMeterResetByMapChangeActive { get; set; }

    /// <summary>
    /// Whether to reset damage meter before combat starts
    /// </summary>
    public bool IsDamageMeterResetBeforeCombatActive { get; set; }
}

