using StatisticsAnalysisTool.Models.NetworkModel;
using System.Collections.ObjectModel;

namespace StatisticsAnalysisTool.Core.State;

/// <summary>
/// State container for entity-related data (players, party members, etc.)
/// </summary>
public class EntityState
{
    /// <summary>
    /// Collection of party member circles for UI display
    /// </summary>
    public ObservableCollection<PartyMemberCircle> PartyMemberCircles { get; } = [];

    /// <summary>
    /// Number of party members
    /// </summary>
    public int PartyMemberNumber { get; set; }
}

