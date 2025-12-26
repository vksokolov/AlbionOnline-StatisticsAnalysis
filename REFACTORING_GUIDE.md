# Controller Refactoring Guide

## Overview

This guide describes the consistent pattern for refactoring controllers in the Statistics Analysis Tool to decouple them from the UI layer (MainWindowViewModel) while maintaining backward compatibility. The goal is to make controllers reusable as library components that can work in both WPF applications and headless environments (CLI, web services, etc.).

## Architecture Pattern

### Before Refactoring (Coupled Architecture)
```
┌─────────────────────────────────────┐
│      MainWindowViewModel            │
│  (Holds all UI state)               │
└─────────────────────────────────────┘
                  ▲
                  │ Direct Dependency
                  │
┌─────────────────┴───────────────────┐
│         SomeController              │
│  - Receives MainWindowViewModel    │
│  - Directly modifies ViewModel      │
│    properties                       │
└─────────────────────────────────────┘
```

**Problems:**
- ❌ Controllers tightly coupled to UI
- ❌ Cannot use controllers without WPF
- ❌ Hard to test controllers
- ❌ Cannot build CLI or library versions

### After Refactoring (Decoupled Architecture)
```
┌─────────────────────────────────────────────────┐
│           MainWindowViewModel                    │
│  - Initializes TrackingViewModels               │
│  - Subscribes to EventBus                       │
│  - Syncs state for backward compatibility       │
└─────────────────────────────────────────────────┘
                     │
                     │ EventBus (decoupled)
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
┌──────────────────┐    ┌──────────────────┐
│  SomeController  │    │  SomeController  │
│  - Has State     │    │  - Has State     │
│  - Publishes     │    │  - Publishes     │
│    Events        │    │    Events        │
└────────┬─────────┘    └────────┬─────────┘
         │                       │
         ▼                       ▼
┌──────────────────┐    ┌──────────────────┐
│   SomeState      │    │   SomeState      │
│  (Pure Data)     │    │  (Pure Data)     │
└──────────────────┘    └──────────────────┘
         │                       │
         ▼                       ▼
┌──────────────────┐    ┌──────────────────┐
│ SomeTracking     │    │ SomeTracking     │
│   ViewModel      │    │   ViewModel      │
│ (Subscribes to   │    │ (Subscribes to   │
│  EventBus)       │    │  EventBus)       │
└──────────────────┘    └──────────────────┘
```

**Benefits:**
- ✅ Controllers independent of UI
- ✅ Can be used in CLI/library mode
- ✅ Easy to test
- ✅ Clear separation of concerns

---

## Step-by-Step Refactoring Process

### Step 1: Create State Class

**Location:** `Core/State/[ControllerName]State.cs`

**Purpose:** Hold pure data that the controller manages (no UI logic, no INPC).

**Pattern:**
```csharp
using System.Collections.Generic;

namespace StatisticsAnalysisTool.Core.State;

/// <summary>
/// State container for [feature]-related data
/// </summary>
public class [ControllerName]State
{
    /// <summary>
    /// Main state property - describe what it holds
    /// </summary>
    public SomeType CurrentData { get; } = new();

    /// <summary>
    /// Collection of historical data (if applicable)
    /// </summary>
    public List<SomeType> History { get; } = new();

    /// <summary>
    /// Configuration/limits
    /// </summary>
    public int MaxHistorySize { get; set; } = 500;
}
```

**Example:** `ClusterState.cs`, `EntityState.cs`

**Rules:**
- ✅ Use auto-properties with getters only for collections
- ✅ Use simple types (no ViewModels)
- ✅ No INotifyPropertyChanged
- ✅ No WPF dependencies
- ✅ Document each property

---

### Step 2: Create Event Classes

**Location:** `Core/Events/[ControllerName]Events.cs`

**Purpose:** Define events that the controller publishes to notify about state changes.

**Pattern:**
```csharp
using System;

namespace StatisticsAnalysisTool.Core.Events;

/// <summary>
/// Event raised when [describe the trigger]
/// </summary>
public class [Feature]ChangedEvent
{
    public SomeType Data { get; }
    public DateTime Timestamp { get; }

    public [Feature]ChangedEvent(SomeType data, DateTime timestamp)
    {
        Data = data;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Event raised when [describe another trigger]
/// </summary>
public class [Feature]UpdatedEvent
{
    public int UpdatedCount { get; }
    public DateTime Timestamp { get; }

    public [Feature]UpdatedEvent(int updatedCount, DateTime timestamp)
    {
        UpdatedCount = updatedCount;
        Timestamp = timestamp;
    }
}
```

**Example:** `ClusterEvents.cs`, `EntityEvents.cs`

**Rules:**
- ✅ Immutable properties (get-only)
- ✅ Always include Timestamp
- ✅ Clear, descriptive names
- ✅ Document when the event is raised
- ✅ Group related events in same file

---

### Step 3: Create Tracking ViewModel

**Location:** `ViewModels/[ControllerName]TrackingViewModel.cs`

**Purpose:** Hold UI-specific state and subscribe to controller events via EventBus.

**Pattern:**
```csharp
using StatisticsAnalysisTool.Core.EventBus;
using StatisticsAnalysisTool.Core.Events;
using System.Collections.ObjectModel;

namespace StatisticsAnalysisTool.ViewModels;

/// <summary>
/// Represents the view state for [feature] tracking
/// </summary>
public class [ControllerName]TrackingViewModel : BaseViewModel
{
    private int _someProperty;
    private ObservableCollection<SomeType> _items = [];

    public [ControllerName]TrackingViewModel(IEventBus eventBus)
    {
        // Subscribe to relevant events
        eventBus.Subscribe<[Feature]ChangedEvent>(On[Feature]Changed);
        eventBus.Subscribe<[Feature]UpdatedEvent>(On[Feature]Updated);
    }

    private void On[Feature]Changed([Feature]ChangedEvent evt)
    {
        // Update UI properties based on event
        SomeProperty = evt.Data.Value;
    }

    private void On[Feature]Updated([Feature]UpdatedEvent evt)
    {
        // Update UI properties based on event
        // Use dispatcher if needed for UI thread
    }

    public int SomeProperty
    {
        get => _someProperty;
        set
        {
            _someProperty = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<SomeType> Items
    {
        get => _items;
        set
        {
            _items = value;
            OnPropertyChanged();
        }
    }
}
```

**Example:** `ClusterTrackingViewModel.cs`, `EntityTrackingViewModel.cs`

**Rules:**
- ✅ Inherits from BaseViewModel
- ✅ Receives IEventBus in constructor
- ✅ Subscribes to events in constructor
- ✅ Uses ObservableCollection for UI-bound lists
- ✅ Implements INotifyPropertyChanged via BaseViewModel
- ✅ No direct controller references

---

### Step 4: Refactor Controller

**Location:** `Network/Manager/[ControllerName].cs` or appropriate location

**Changes:**

#### 4.1: Update Constructor
**Before:**
```csharp
public SomeController(TrackingController trackingController, MainWindowViewModel mainWindowViewModel)
{
    _trackingController = trackingController;
    _mainWindowViewModel = mainWindowViewModel;
}
```

**After:**
```csharp
private readonly IEventBus _eventBus;

public SomeController(TrackingController trackingController, IEventBus eventBus)
{
    _trackingController = trackingController;
    _eventBus = eventBus;
}
```

#### 4.2: Add State Property
```csharp
public [ControllerName]State State { get; } = new();
```

#### 4.3: Remove MainWindowViewModel Field
```csharp
// Remove this:
// private readonly MainWindowViewModel _mainWindowViewModel;
```

#### 4.4: Replace Direct ViewModel Updates with Event Publishing
**Before:**
```csharp
_mainWindowViewModel.SomeProperty = newValue;
_mainWindowViewModel.SomeCollection.Add(item);
```

**After:**
```csharp
// Update state
State.SomeProperty = newValue;
State.SomeCollection.Add(item);

// Publish event
_eventBus.Publish(new SomeChangedEvent(newValue, DateTime.UtcNow));
```

#### 4.5: Update Method That Modified UI State
**Pattern:**
```csharp
private async Task UpdateUiStateAsync()
{
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        // Clear and rebuild state collections
        State.SomeCollection.Clear();
        
        // Populate from internal data
        foreach (var item in _internalData)
        {
            State.SomeCollection.Add(item);
        }
        
        // Update state properties
        State.SomeProperty = calculatedValue;
        
        // Publish event to notify UI
        _eventBus.Publish(new SomeUpdatedEvent(State.SomeCollection.Count, DateTime.UtcNow));
    });
}
```

**Example:** `SetPartyMemberUiAsync()` in EntityController

---

### Step 5: Update TrackingController

**Location:** `Network/Manager/TrackingController.cs`

**Changes:**
```csharp
public TrackingController(MainWindowViewModel mainWindowViewModel)
{
    _mainWindowViewModel = mainWindowViewModel;
    _eventBus = ServiceLocator.Resolve<IEventBus>();
    
    // Update controller instantiation - add IEventBus parameter
    SomeController = new SomeController(this, _eventBus);
    
    // Keep other controllers as-is until refactored
    OtherController = new OtherController(this, mainWindowViewModel);
    
    _ = InitTrackingAsync();
}
```

---

### Step 6: Update MainWindowViewModel

**Location:** `ViewModels/MainWindowViewModel.cs`

#### 6.1: Add Tracking ViewModel Field
```csharp
private [ControllerName]TrackingViewModel _[controllerName]TrackingViewModel;
```

#### 6.2: Initialize in Constructor
```csharp
public MainWindowViewModel()
{
    UpgradeSettings();
    SetUiElements();
    Translation = new MainWindowTranslation();
    
    // Initialize tracking view models with event bus
    var eventBus = ServiceLocator.Resolve<IEventBus>();
    _[controllerName]TrackingViewModel = new [ControllerName]TrackingViewModel(eventBus);
    
    // Wire up for backward compatibility
    _[controllerName]TrackingViewModel.PropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(_[controllerName]TrackingViewModel.SomeProperty))
        {
            // Sync to old properties if still used by UI
            SomeProperty = _[controllerName]TrackingViewModel.SomeProperty;
        }
    };
}
```

#### 6.3: Add WireUp Method (if needed)
```csharp
public void WireUp[ControllerName](SomeController controller)
{
    // Sync observable collections from controller state to view model
    // This is needed when the UI binds directly to MainWindowViewModel properties
    SomeCollection = controller.State.SomeCollection;
}
```

#### 6.4: Call WireUp in App.xaml.cs
```csharp
private void RegisterServicesLate()
{
    _trackingController = new TrackingController(_mainWindowViewModel);
    ServiceLocator.Register<TrackingController>(_trackingController);
    
    // Wire up refactored controllers
    _mainWindowViewModel.WireUp[ControllerName](_trackingController.SomeController);
}
```

---

## Verification Checklist

After refactoring a controller, verify:

### ✅ State Class
- [ ] Located in `Core/State/`
- [ ] No UI dependencies (no WPF namespaces)
- [ ] No INotifyPropertyChanged
- [ ] Well-documented properties
- [ ] Uses simple types and collections

### ✅ Event Classes
- [ ] Located in `Core/Events/`
- [ ] Immutable properties (get-only)
- [ ] Includes Timestamp property
- [ ] Clear, descriptive names
- [ ] Well-documented

### ✅ Tracking ViewModel
- [ ] Located in `ViewModels/`
- [ ] Inherits from BaseViewModel
- [ ] Receives IEventBus in constructor
- [ ] Subscribes to events
- [ ] No direct controller references
- [ ] Uses ObservableCollection for UI binding

### ✅ Controller
- [ ] Removes MainWindowViewModel dependency
- [ ] Adds IEventBus dependency
- [ ] Has State property
- [ ] Publishes events instead of updating ViewModel
- [ ] No WPF-specific logic (except Dispatcher for thread safety)

### ✅ TrackingController
- [ ] Passes IEventBus to refactored controller
- [ ] Still works with non-refactored controllers

### ✅ MainWindowViewModel
- [ ] Initializes tracking ViewModel in constructor
- [ ] Subscribes to tracking ViewModel changes
- [ ] Has WireUp method if needed
- [ ] Maintains backward compatibility

### ✅ App.xaml.cs
- [ ] Calls WireUp method after TrackingController creation

### ✅ Build
- [ ] Solution builds without errors
- [ ] No new warnings introduced
- [ ] All tests pass (if applicable)

---

## Common Patterns

### Pattern 1: ObservableCollection Sync
When UI binds to MainWindowViewModel's ObservableCollection:

```csharp
// In MainWindowViewModel.WireUp method:
public void WireUpSomeController(SomeController controller)
{
    // Direct reference sharing - both point to same collection
    SomeCollection = controller.State.SomeCollection;
}
```

### Pattern 2: Property Value Sync
When UI binds to MainWindowViewModel's properties:

```csharp
// In MainWindowViewModel constructor:
_someTrackingViewModel.PropertyChanged += (_, e) =>
{
    if (e.PropertyName == nameof(_someTrackingViewModel.SomeValue))
    {
        SomeValue = _someTrackingViewModel.SomeValue;
    }
};
```

### Pattern 3: Dispatcher for UI Thread
When updating ObservableCollections from controller:

```csharp
private async Task UpdateStateAsync()
{
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        State.Items.Clear();
        foreach (var item in _internalData)
        {
            State.Items.Add(item);
        }
        
        _eventBus.Publish(new ItemsUpdatedEvent(State.Items.Count, DateTime.UtcNow));
    });
}
```

### Pattern 4: Event-Only Notifications
When only notifying about changes (no data sync needed):

```csharp
// Controller publishes event
_eventBus.Publish(new SomethingHappenedEvent(DateTime.UtcNow));

// Tracking ViewModel reacts
private void OnSomethingHappened(SomethingHappenedEvent evt)
{
    // Update UI state or trigger UI actions
    IsProcessing = false;
    StatusMessage = "Operation completed";
}
```

---

## Examples of Refactored Controllers

### Fully Refactored
1. **ClusterController** → `ClusterState`, `ClusterEvents`, `ClusterTrackingViewModel`
2. **EntityController** → `EntityState`, `EntityEvents`, `EntityTrackingViewModel`
3. **DungeonController** → `DungeonState`, `DungeonEvents`, `DungeonTrackingViewModel`
4. **CombatController** → `CombatState`, `CombatEvents`, `CombatTrackingViewModel`

### Not Yet Refactored
- LootController
- LootController
- StatisticController
- TreasureController
- MailController
- MarketController
- TradeController
- VaultController
- GatheringController
- PartyController
- GuildController

---

## Future Improvements

### Phase 1: Complete Controller Refactoring
Refactor all remaining controllers following this guide.

### Phase 2: Remove Backward Compatibility
Once all controllers are refactored:
1. Update UI bindings to use tracking ViewModels directly
2. Remove property sync code from MainWindowViewModel
3. Remove WireUp methods
4. Move tracking ViewModels to be public properties

### Phase 3: Extract Library
Create a separate library project:
```
StatisticsAnalysisTool.Core/
  - All Controllers
  - All State classes
  - EventBus
  - Models
  
StatisticsAnalysisTool.UI/
  - ViewModels (including tracking VMs)
  - Views
  - UI-specific code
  
StatisticsAnalysisTool.CLI/
  - Command-line interface
  - Uses controllers directly
```

---

## Anti-Patterns to Avoid

### ❌ Don't: Create Events in Controller File
```csharp
// Bad - event defined in controller
public class SomeController
{
    public event Action<SomeData> OnDataChanged;
}
```

### ✅ Do: Create Proper Event Classes
```csharp
// Good - event class in Core/Events
public class DataChangedEvent
{
    public SomeData Data { get; }
    public DateTime Timestamp { get; }
}
```

### ❌ Don't: Put UI Logic in State
```csharp
// Bad - State should be pure data
public class SomeState : INotifyPropertyChanged
{
    private string _displayText;
    public string DisplayText { get; set; }
}
```

### ✅ Do: Keep State Pure
```csharp
// Good - pure data only
public class SomeState
{
    public string RawData { get; set; }
}

// UI formatting in ViewModel
public class SomeTrackingViewModel : BaseViewModel
{
    public string DisplayText => FormatData(State.RawData);
}
```

### ❌ Don't: Reference ViewModel from Controller
```csharp
// Bad - creates tight coupling
public class SomeController
{
    public void DoSomething(MainWindowViewModel viewModel)
    {
        viewModel.SomeProperty = "value";
    }
}
```

### ✅ Do: Use Events
```csharp
// Good - decoupled via events
public class SomeController
{
    public void DoSomething()
    {
        State.SomeValue = "value";
        _eventBus.Publish(new ValueChangedEvent("value", DateTime.UtcNow));
    }
}
```

---

## Questions & Troubleshooting

### Q: When should I use Dispatcher?
**A:** Use `Application.Current.Dispatcher.InvokeAsync()` when:
- Modifying ObservableCollections from non-UI thread
- The controller method might be called from background thread
- You see "cross-thread" exceptions

### Q: Should State classes be in the Controller file?
**A:** No. State classes should be in `Core/State/` for:
- Clear separation
- Reusability
- Better organization

### Q: Can I have multiple State classes per controller?
**A:** Yes, if the controller manages multiple distinct concerns. Example:
- `PartyState` (party members)
- `PartyBuilderState` (party composition planning)

### Q: What if I need to pass data from Controller to UI immediately?
**A:** Controllers should still use events. For immediate updates:
1. Update State synchronously
2. Publish event
3. UI ViewModel subscribes and updates immediately

### Q: How do I handle complex UI state (like selected items)?
**A:** UI-specific state stays in ViewModels. Only domain/business state goes in Controller State.

---

## Conclusion

Following this guide ensures:
- **Consistency** across all refactored controllers
- **Testability** of business logic
- **Reusability** in different contexts (WPF, CLI, library)
- **Maintainability** through clear separation of concerns

When refactoring, work on one controller at a time and verify each step against this guide. Commit after each controller is fully refactored and tested.

