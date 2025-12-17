using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Dungeon.Models;
using System.Collections.Generic;
using StatisticsAnalysisTool.Models.NetworkModel;

namespace StatisticsAnalysisTool.Core.State;

/// <summary>
/// State container for dungeon tracking data
/// </summary>
public class DungeonState
{
    /// <summary>
    /// Collection of all tracked dungeons
    /// </summary>
    public ObservableRangeCollection<DungeonBaseFragment> Dungeons { get; } = new();

    /// <summary>
    /// List of discovered loot items in current dungeon
    /// </summary>
    public List<DiscoveredItem> DiscoveredLoot { get; } = new();

    /// <summary>
    /// GUIDs of dungeons where level has been recognized
    /// </summary>
    public List<System.Guid> LastGuidWithRecognizedLevel { get; set; } = new();

    /// <summary>
    /// Maximum number of dungeons to keep in history
    /// </summary>
    public int MaxDungeons { get; set; } = 9999;

    /// <summary>
    /// Number of dungeons to track before auto-saving
    /// </summary>
    public int NumberOfDungeonsUntilSaved { get; set; } = 1;

    /// <summary>
    /// Current dungeon GUID (if player is in a dungeon)
    /// </summary>
    public System.Guid? CurrentGuid { get; set; }

    /// <summary>
    /// Last map GUID (for dungeon continuation tracking)
    /// </summary>
    public System.Guid? LastMapGuid { get; set; }

    /// <summary>
    /// Counter for tracking when to auto-save
    /// </summary>
    public int AddDungeonCounter { get; set; }

    /// <summary>
    /// Current item container being interacted with
    /// </summary>
    public ItemContainerObject CurrentItemContainer { get; set; }
}

