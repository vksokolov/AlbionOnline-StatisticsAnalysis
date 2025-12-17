using StatisticsAnalysisTool.Core.EventBus;
using StatisticsAnalysisTool.Core.Events;
using StatisticsAnalysisTool.Models.NetworkModel;
using System.Collections.ObjectModel;

namespace StatisticsAnalysisTool.ViewModels;

/// <summary>
/// Represents the view state for entity tracking (party members, local entity, etc.)
/// </summary>
public class EntityTrackingViewModel : BaseViewModel
{
    private ObservableCollection<PartyMemberCircle> _partyMemberCircles = [];
    private int _partyMemberNumber;

    public EntityTrackingViewModel(IEventBus eventBus)
    {
        // Subscribe to entity tracking events
        eventBus.Subscribe<PartyMembersUpdatedEvent>(OnPartyMembersUpdated);
    }

    private void OnPartyMembersUpdated(PartyMembersUpdatedEvent evt)
    {
        // The actual party member circles will be managed by EntityController
        // and synchronized through its State property
        PartyMemberNumber = evt.PartyMemberCount;
    }

    public ObservableCollection<PartyMemberCircle> PartyMemberCircles
    {
        get => _partyMemberCircles;
        set
        {
            _partyMemberCircles = value;
            OnPropertyChanged();
        }
    }

    public int PartyMemberNumber
    {
        get => _partyMemberNumber;
        set
        {
            _partyMemberNumber = value;
            OnPropertyChanged();
        }
    }
}

