using Serilog;
using StatisticsAnalysisTool.Cluster;
using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Core.EventBus;
using StatisticsAnalysisTool.Core.Events;
using StatisticsAnalysisTool.Core.State;
using StatisticsAnalysisTool.Dungeon.Models;
using StatisticsAnalysisTool.Enumerations;
using StatisticsAnalysisTool.Exceptions;
using StatisticsAnalysisTool.GameFileData;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Models.NetworkModel;
using StatisticsAnalysisTool.Network.Manager;
using StatisticsAnalysisTool.Properties;
using StatisticsAnalysisTool.ViewModels;
using StatisticsAnalysisTool.Views;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using StatisticsAnalysisTool.Diagnostics;
using Loot = StatisticsAnalysisTool.Dungeon.Models.Loot;
using ValueType = StatisticsAnalysisTool.Enumerations.ValueType;
// ReSharper disable PossibleMultipleEnumeration

namespace StatisticsAnalysisTool.Dungeon;

public sealed class DungeonController
{
    private readonly TrackingController _trackingController;
    private readonly IEventBus _eventBus;
    private readonly MainWindowViewModel _mainWindowViewModel;

    public DungeonState State { get; } = new();

    public DungeonController(TrackingController trackingController, IEventBus eventBus, MainWindowViewModel mainWindowViewModel)
    {
        _trackingController = trackingController;
        _eventBus = eventBus;
        _mainWindowViewModel = mainWindowViewModel;

        if (_mainWindowViewModel?.DungeonBindings?.Dungeons != null)
        {
            _mainWindowViewModel.DungeonBindings.Dungeons.CollectionChanged += OnCollectionChanged;
        }

        if (State.Dungeons != null)
        {
            State.Dungeons.CollectionChanged += OnStateCollectionChanged;
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _mainWindowViewModel?.DungeonBindings?.Stats.Set(_mainWindowViewModel?.DungeonBindings?.Dungeons);
    }

    private void OnStateCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _mainWindowViewModel?.DungeonBindings?.Stats.Set(State.Dungeons);
        _eventBus.Publish(new DungeonsUpdatedEvent(State.Dungeons.Count, DateTime.UtcNow));
    }

    public async Task AddDungeonAsync(MapType mapType, Guid? mapGuid)
    {
        if (!_trackingController.IsTrackingAllowedByMainCharacter())
        {
            return;
        }

        UpdateDungeonSaveTimerUi();

        State.CurrentGuid = mapGuid;

        // Last map is a dungeon, add new map
        if (IsDungeonCluster(mapType, mapGuid)
            && ExistDungeon(State.LastMapGuid)
            && mapType is not MapType.CorruptedDungeon
            && mapType is not MapType.HellGate
            && mapType is not MapType.Mists
            && mapType is not MapType.MistsDungeon)
        {
            if (AddClusterToExistDungeon(mapGuid, State.LastMapGuid, out var currentDungeon))
            {
                currentDungeon.AddTimer(DateTime.UtcNow);
            }
        }
        // Add new dungeon
        else if (IsDungeonCluster(mapType, mapGuid)
                 && !ExistDungeon(State.LastMapGuid)
                 && !ExistDungeon(State.CurrentGuid)
                 || (IsDungeonCluster(mapType, mapGuid)
                 && mapType is MapType.CorruptedDungeon or MapType.HellGate or MapType.Mists or MapType.MistsDungeon or MapType.AbyssalDepths))
        {
            UpdateDungeonSaveTimerUi(mapType);

            if (mapType is MapType.CorruptedDungeon or MapType.HellGate or MapType.Mists or MapType.MistsDungeon or MapType.AbyssalDepths)
            {
                var lastDungeon = GetDungeon(State.LastMapGuid);
                lastDungeon?.EndTimer();
            }

            State.Dungeons.Where(x => x.Status != DungeonStatus.Done).ToList().ForEach(x =>
            {
                x.Status = DungeonStatus.Done;
                _eventBus.Publish(new DungeonStatusChangedEvent(x.GuidList.FirstOrDefault(), DungeonStatus.Done, DateTime.UtcNow));
            });

            var newDungeon = CreateNewDungeon(mapType, ClusterController.CurrentCluster.MainClusterIndex, mapGuid);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                State.Dungeons.Insert(0, newDungeon);
                _mainWindowViewModel.DungeonBindings.Dungeons.Insert(0, newDungeon);
            });

            _eventBus.Publish(new DungeonAddedEvent(newDungeon, DateTime.UtcNow));
        }
        // Activate exist dungeon again
        else if (IsDungeonCluster(mapType, mapGuid)
                 && !ExistDungeon(State.LastMapGuid)
                 && ExistDungeon(State.CurrentGuid)
                 || IsDungeonCluster(mapType, mapGuid)
                 && mapType is MapType.CorruptedDungeon or MapType.HellGate or MapType.Mists or MapType.MistsDungeon or MapType.AbyssalDepths)
        {
            UpdateDungeonSaveTimerUi(mapType);

            var currentDungeon = GetDungeon(State.CurrentGuid);
            currentDungeon.Status = DungeonStatus.Active;
            currentDungeon.AddTimer(DateTime.UtcNow);
            _eventBus.Publish(new DungeonStatusChangedEvent(currentDungeon.GuidList.FirstOrDefault(), DungeonStatus.Active, DateTime.UtcNow));
        }
        // Make last dungeon done
        else if (mapGuid == null && ExistDungeon(State.LastMapGuid))
        {
            var lastDungeon = GetDungeon(State.LastMapGuid);
            lastDungeon.EndTimer();
            lastDungeon.Status = DungeonStatus.Done;
            _eventBus.Publish(new DungeonStatusChangedEvent(lastDungeon.GuidList.FirstOrDefault(), DungeonStatus.Done, DateTime.UtcNow));
            await SaveInFileAfterExceedingLimit(State.NumberOfDungeonsUntilSaved);
            State.LastGuidWithRecognizedLevel = [];
        }

        State.LastMapGuid = mapGuid;

        await RemoveDungeonsAfterCertainNumberAsync(State.Dungeons, State.MaxDungeons);
        await Application.Current.Dispatcher.InvokeAsync(_mainWindowViewModel.DungeonBindings.UpdateFilteredDungeonsAsync);
    }

    private static DungeonBaseFragment CreateNewDungeon(MapType mapType, string mainMapIndex, Guid? guid)
    {
        if (guid == null)
        {
            return null;
        }

        DungeonBaseFragment newDungeon;
        switch (mapType)
        {
            case MapType.RandomDungeon:
                var dungeonMode = DungeonData.GetDungeonMode(mainMapIndex);
                newDungeon = new RandomDungeonFragment((Guid) guid, mapType, dungeonMode, mainMapIndex);
                break;
            case MapType.CorruptedDungeon:
                newDungeon = new CorruptedFragment((Guid) guid, mapType, DungeonMode.Corrupted, mainMapIndex);
                break;
            case MapType.HellGate:
                newDungeon = new HellGateFragment((Guid) guid, mapType, DungeonMode.HellGate, mainMapIndex);
                break;
            case MapType.Expedition:
                newDungeon = new ExpeditionFragment((Guid) guid, mapType, DungeonMode.Expedition, mainMapIndex);
                break;
            case MapType.Mists:
                var tier = (Tier) Enum.ToObject(typeof(Tier), MistsData.GetTier(ClusterController.CurrentCluster.WorldMapDataType));
                newDungeon = new MistsFragment((Guid) guid, mapType, DungeonMode.Mists, mainMapIndex, ClusterController.CurrentCluster.MistsRarity, tier);
                break;
            case MapType.MistsDungeon:
                newDungeon = new MistsDungeonFragment((Guid) guid, mapType, DungeonMode.MistsDungeon, mainMapIndex, ClusterController.CurrentCluster.MistsDungeonTier);
                break;
            case MapType.AbyssalDepths:
                newDungeon = new AbyssalDepthsFragment((Guid) guid, mapType, DungeonMode.AbyssalDepths, mainMapIndex);
                break;
            default:
                newDungeon = null;
                break;
        }

        return newDungeon;
    }

    public void ResetDungeons()
    {
        State.Dungeons.Clear();
        Application.Current.Dispatcher.Invoke(() => { _mainWindowViewModel?.DungeonBindings?.Dungeons?.Clear(); });
        _eventBus.Publish(new DungeonsClearedEvent(DateTime.UtcNow));
    }

    public void ResetDungeonsByDateAscending(DateTime date)
    {
        var dungeonsToDelete = State.Dungeons?.Where(x => x.EnterDungeonFirstTime >= date).ToList();
        foreach (var dungeonObject in dungeonsToDelete ?? [])
        {
            State.Dungeons?.Remove(dungeonObject);
            _mainWindowViewModel.DungeonBindings.Dungeons?.Remove(dungeonObject);
            _eventBus.Publish(new DungeonRemovedEvent(dungeonObject.DungeonHash, DateTime.UtcNow));
        }
    }

    public void DeleteDungeonsWithZeroFame()
    {
        var dungeonsToDelete = State.Dungeons?.Where(x => x.Fame <= 0 && x.Status != DungeonStatus.Active).ToList();
        foreach (var dungeonObject in dungeonsToDelete ?? [])
        {
            State.Dungeons?.Remove(dungeonObject);
            _mainWindowViewModel.DungeonBindings.Dungeons?.Remove(dungeonObject);
            _eventBus.Publish(new DungeonRemovedEvent(dungeonObject.DungeonHash, DateTime.UtcNow));
        }
    }

    public void RemoveDungeon(string dungeonHash)
    {
        var dungeon = State.Dungeons.FirstOrDefault(x => x.DungeonHash.Contains(dungeonHash));

        if (dungeon == null)
        {
            return;
        }

        var dialog = new DialogWindow(LocalizationController.Translation("REMOVE_DUNGEON"), LocalizationController.Translation("SURE_YOU_WANT_TO_REMOVE_DUNGEON"));
        var dialogResult = dialog.ShowDialog();

        if (dialogResult is not true)
        {
            return;
        }

        State.Dungeons.Remove(dungeon);
        _mainWindowViewModel.DungeonBindings.Dungeons.Remove(dungeon);
        _eventBus.Publish(new DungeonRemovedEvent(dungeonHash, DateTime.UtcNow));
    }

    private async Task RemoveDungeonsAfterCertainNumberAsync(ICollection<DungeonBaseFragment> dungeons, int dungeonLimit)
    {
        try
        {
            var toDelete = dungeons?.Count - dungeonLimit;

            if (toDelete <= 0)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                for (var i = toDelete; i <= 0; i--)
                {
                    var dateTime = GetLowestDate(dungeons);
                    if (dateTime == null)
                    {
                        continue;
                    }

                    var removableItem = dungeons?.FirstOrDefault(x => x.EnterDungeonFirstTime == dateTime);
                    dungeons?.Remove(removableItem);
                }

                await _mainWindowViewModel.DungeonBindings.UpdateFilteredDungeonsAsync();
            });
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
        }
    }

    public async Task RemoveDungeonByHashAsync(IEnumerable<string> dungeonHash)
    {
        await foreach (var dungeons in State.Dungeons.ToList().ToAsyncEnumerable())
        {
            if (dungeonHash.Contains(dungeons.DungeonHash))
            {
                State.Dungeons.Remove(dungeons);
                _mainWindowViewModel.DungeonBindings.Dungeons.Remove(dungeons);
                _eventBus.Publish(new DungeonRemovedEvent(dungeons.DungeonHash, DateTime.UtcNow));
            }
        }

        await SaveInFileAsync();
    }

    private bool AddClusterToExistDungeon(Guid? currentGuid, Guid? lastGuid, out DungeonBaseFragment dungeon)
    {
        if (currentGuid != null && lastGuid != null && State.Dungeons?.Any(x => x.GuidList.Contains((Guid) currentGuid)) != true)
        {
            var dun = State.Dungeons?.FirstOrDefault(x => x.GuidList.Contains((Guid) lastGuid));
            dun?.GuidList.Add((Guid) currentGuid);

            dungeon = dun;

            return State.Dungeons?.Any(x => x.GuidList.Contains((Guid) currentGuid)) ?? false;
        }

        dungeon = null;
        return false;
    }

    public static DateTime? GetLowestDate(IEnumerable<DungeonBaseFragment> items)
    {
        if (items?.Count() <= 0)
        {
            return null;
        }

        try
        {
            return items?.Select(x => x.EnterDungeonFirstTime).Min();
        }
        catch (ArgumentNullException e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            return null;
        }
    }

    #region Dungeon object

    public void SetDungeonChestOpen(int id, List<Guid> allowedToOpen)
    {
        if (!_trackingController.EntityController.IsAnyEntityInParty(allowedToOpen))
        {
            return;
        }

        if (State.CurrentGuid != null)
        {
            try
            {
                var dun = GetDungeon((Guid) State.CurrentGuid);
                var chest = dun?.Events?.FirstOrDefault(x => x?.Id == id);
                if (chest != null)
                {
                    chest.Status = ChestStatus.Open;
                    chest.Opened = DateTime.UtcNow;
                    _eventBus.Publish(new DungeonChestOpenedEvent((Guid) State.CurrentGuid, id, DateTime.UtcNow));
                }
            }
            catch (Exception e)
            {
                DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
                Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            }
        }
    }

    private DungeonBaseFragment GetDungeon(Guid? guid)
    {
        return guid == null ? null : State.Dungeons.FirstOrDefault(x => x.GuidList.Contains((Guid) guid));
    }

    public async Task SetDungeonEventInformationAsync(int id, string uniqueName)
    {
        if (State.CurrentGuid == null || uniqueName == null)
        {
            return;
        }

        try
        {
            var dun = GetDungeon((Guid) State.CurrentGuid);
            if (dun == null || dun.Events?.Any(x => x.Id == id) == true)
            {
                return;
            }

            var eventObject = new PointOfInterest(id, uniqueName);
            await Application.Current.Dispatcher.InvokeAsync(() => { dun.Events?.Add(eventObject); });

            if (dun.Faction == Faction.Unknown)
            {
                dun.Faction = DungeonData.GetFaction(uniqueName);
            }

            if (dun.Mode == DungeonMode.Unknown)
            {
                dun.Mode = DungeonData.GetDungeonMode(uniqueName);
            }
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
        }
    }

    public void AddValueToDungeon(double value, ValueType valueType, CityFaction cityFaction = CityFaction.Unknown)
    {
        try
        {
            lock (State.Dungeons)
            {
                var dun = State.Dungeons?.FirstOrDefault(x => State.CurrentGuid != null && x.GuidList.Contains((Guid) State.CurrentGuid) && x.Status == DungeonStatus.Active);

                if (dun == null)
                {
                    return;
                }

                switch (dun)
                {
                    case RandomDungeonFragment standardDun:
                        standardDun.Add(value, valueType, cityFaction);
                        break;
                    case HellGateFragment hellGate:
                        hellGate.Add(value, valueType);
                        break;
                    case CorruptedFragment corrupted:
                        corrupted.Add(value, valueType);
                        break;
                    case ExpeditionFragment expedition:
                        expedition.Add(value, valueType);
                        break;
                    case MistsFragment mists:
                        mists.Add(value, valueType);
                        break;
                    case MistsDungeonFragment mistsDungeon:
                        mistsDungeon.Add(value, valueType);
                        break;
                    case AbyssalDepthsFragment abyssalDepths:
                        abyssalDepths.Add(value, valueType);
                        break;
                }

                _eventBus.Publish(new DungeonValueAddedEvent(dun.GuidList.FirstOrDefault(), value, valueType, DateTime.UtcNow));
            }
        }
        catch
        {
            // ignored
        }
    }

    public void SetDiedIfInDungeon(DiedObject dieObject)
    {
        if (State.CurrentGuid == null || _trackingController.EntityController.LocalUserData.Username == null)
        {
            return;
        }

        var dungeon = State.Dungeons.FirstOrDefault(x => x.GuidList.Contains((Guid) State.CurrentGuid));

        if (dungeon is null)
        {
            return;
        }

        if (dieObject.DiedName == _trackingController.EntityController.LocalUserData.Username)
        {
            dungeon.KillStatus = KillStatus.LocalPlayerDead;
        }
        else if (dieObject.KilledBy == _trackingController.EntityController.LocalUserData.Username)
        {
            dungeon.KillStatus = KillStatus.OpponentDead;
        }

        dungeon.DiedName = dieObject.DiedName;
        dungeon.KilledBy = dieObject.KilledBy;
    }

    #endregion

    #region Tier / Level recognize

    public void AddLevelToCurrentDungeon(int? mobIndex, double hitPointsMax)
    {
        if (State.CurrentGuid is not { } currentGuid)
        {
            return;
        }

        if (State.LastGuidWithRecognizedLevel.Contains(currentGuid))
        {
            return;
        }

        if (mobIndex is null || ClusterController.CurrentCluster.Guid != currentGuid)
        {
            return;
        }

        //if (ClusterController.CurrentCluster.MapType != MapType.Expedition
        //    && ClusterController.CurrentCluster.MapType != MapType.CorruptedDungeon
        //    && ClusterController.CurrentCluster.MapType != MapType.HellGate
        //    && ClusterController.CurrentCluster.MapType != MapType.RandomDungeon
        //    && ClusterController.CurrentCluster.MapType != MapType.Mists
        //    && ClusterController.CurrentCluster.MapType != MapType.MistsDungeon)
        //{
        //    return;
        //}

        try
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                var dun = State.Dungeons?.FirstOrDefault(x => x.GuidList.Contains(currentGuid) && x.Status == DungeonStatus.Active);
                if (dun is not RandomDungeonFragment randomDungeon)
                {
                    return;
                }

                randomDungeon.Level = randomDungeon.Level < 0 ? MobsData.GetMobLevelByIndex((int) mobIndex, hitPointsMax) : randomDungeon.Level;

                if (randomDungeon.Level > 0)
                {
                    State.LastGuidWithRecognizedLevel = dun.GuidList.ToList();
                }
            }));
        }
        catch
        {
            // ignored
        }
    }

    public async Task AddTierToCurrentDungeonAsync(int? mobIndex)
    {
        if (State.CurrentGuid is not { } currentGuid)
        {
            return;
        }

        if (mobIndex is null || ClusterController.CurrentCluster.Guid != currentGuid)
        {
            return;
        }

        //if (ClusterController.CurrentCluster.MapType != MapType.Expedition
        //    && ClusterController.CurrentCluster.MapType != MapType.CorruptedDungeon
        //    && ClusterController.CurrentCluster.MapType != MapType.HellGate
        //    && ClusterController.CurrentCluster.MapType != MapType.RandomDungeon)
        //{
        //    return;
        //}

        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var mobTier = (Tier) MobsData.GetMobTierByIndex((int) mobIndex);
                var dun = State.Dungeons?.FirstOrDefault(x => x.GuidList.Contains(currentGuid) && x.Status == DungeonStatus.Active);
                if (dun == null || dun.Tier >= mobTier)
                {
                    return;
                }

                dun.SetTier(mobTier);
            });
        }
        catch
        {
            // ignored
        }
    }

    #endregion

    #region Dungeon loot tracking

    public void SetCurrentItemContainer(ItemContainerObject itemContainerObject)
    {
        State.CurrentItemContainer = itemContainerObject;
    }

    public void AddDiscoveredItem(DiscoveredItem discoveredItem)
    {
        if (State.DiscoveredLoot.Any(x => x?.ObjectId == discoveredItem?.ObjectId))
        {
            return;
        }

        if (State.CurrentGuid == null)
        {
            return;
        }

        State.DiscoveredLoot.Add(discoveredItem);
    }

    public async Task AddNewLocalPlayerLootOnCurrentDungeonAsync(int containerSlot, Guid containerGuid, Guid userInteractGuid)
    {
        if (_trackingController.EntityController.LocalUserData.InteractGuid != userInteractGuid)
        {
            return;
        }

        if (State.CurrentItemContainer?.ContainerGuid != containerGuid)
        {
            return;
        }

        var itemObjectId = GetItemObjectIdFromContainer(containerSlot);
        var lootedItem = State.DiscoveredLoot.FirstOrDefault(x => x.ObjectId == itemObjectId);

        if (lootedItem == null)
        {
            return;
        }

        await AddLocalPlayerLootedItemToCurrentDungeonAsync(lootedItem);
    }

    private long GetItemObjectIdFromContainer(int containerSlot)
    {
        if (State.CurrentItemContainer == null || State.CurrentItemContainer?.SlotItemIds?.Count is null or <= 0 || State.CurrentItemContainer?.SlotItemIds?.Count <= containerSlot)
        {
            return 0;
        }

        return State.CurrentItemContainer!.SlotItemIds![containerSlot];
    }

    public async Task AddLocalPlayerLootedItemToCurrentDungeonAsync(DiscoveredItem discoveredItem)
    {
        if (State.CurrentGuid == null)
        {
            return;
        }

        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dun = GetDungeon((Guid) State.CurrentGuid);
                if (dun == null)
                {
                    return;
                }

                var uniqueItemName = ItemController.GetUniqueNameByIndex(discoveredItem.ItemIndex);
                if (uniqueItemName.Contains("SILVERBAG"))
                {
                    return;
                }

                dun.Loot.Add(new Loot()
                {
                    EstimatedMarketValueInternal = discoveredItem.EstimatedMarketValueInternal,
                    Quantity = discoveredItem.Quantity,
                    UniqueName = uniqueItemName,
                    UtcDiscoveryTime = discoveredItem.UtcDiscoveryTime
                });
                dun.UpdateTotalSilverValue();
                dun.UpdateMostValuableLoot();
                dun.UpdateMostValuableLootVisibility();

                _eventBus.Publish(new DungeonLootAddedEvent((Guid) State.CurrentGuid, discoveredItem, DateTime.UtcNow));
            });
        }
        catch (Exception e)
        {
            DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
            Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
        }
    }

    public void ResetLocalPlayerDiscoveredLoot()
    {
        State.DiscoveredLoot.Clear();
    }

    #endregion

    #region Dungeon timer

    private void UpdateDungeonSaveTimerUi(MapType mapType = MapType.Unknown)
    {
        var isVisible = mapType == MapType.RandomDungeon;
        _mainWindowViewModel.DungeonBindings.DungeonCloseTimer.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        _eventBus.Publish(new DungeonSaveTimerVisibilityChangedEvent(isVisible, mapType, DateTime.UtcNow));
    }

    #endregion

    #region Expedition

    public async Task UpdateCheckPointAsync(CheckPoint checkPoint)
    {
        if (State.CurrentGuid is not { } currentGuid)
        {
            return;
        }


        if (ClusterController.CurrentCluster.MapType != MapType.Expedition)
        {
            return;
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dun = State.Dungeons?.FirstOrDefault(x => x.GuidList.Contains(currentGuid) && x.Status == DungeonStatus.Active);
            if (dun is not ExpeditionFragment expedition)
            {
                return;
            }

            var foundCheckPoint = expedition.CheckPoints?.FirstOrDefault(x => x.Id == checkPoint.Id);
            if (foundCheckPoint is null)
            {
                expedition.CheckPoints?.Add(checkPoint);
            }
            else
            {
                foundCheckPoint.Status = checkPoint.Status;
            }

        });
    }

    #endregion

    #region Helper methods

    private bool ExistDungeon(Guid? mapGuid)
    {
        return mapGuid != null && State.Dungeons.Any(x => x.GuidList.Contains((Guid) mapGuid));
    }

    private static bool IsDungeonCluster(MapType mapType, Guid? mapGuid)
    {
        return mapGuid != null && mapType is MapType.RandomDungeon or MapType.CorruptedDungeon or MapType.HellGate or MapType.Expedition or MapType.Mists or MapType.MistsDungeon or MapType.AbyssalDepths;
    }

    #endregion

    #region Load / Save file data

    public async Task LoadDungeonFromFileAsync()
    {
        var dungeons = await FileController.LoadAsync<List<DungeonDto>>(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName, Settings.Default.DungeonRunsFileName));

        var dungeonsToAdd = new List<DungeonBaseFragment>();
        foreach (DungeonDto dungeonDto in dungeons)
        {
            try
            {
                dungeonsToAdd.Add(DungeonMapping.Mapping(dungeonDto));
            }
            catch (MappingException e)
            {
                DebugConsole.WriteError(MethodBase.GetCurrentMethod()?.DeclaringType, e);
                Log.Error(e, "{message}", MethodBase.GetCurrentMethod()?.DeclaringType);
            }
        }

        var orderedDungeons = dungeonsToAdd.OrderBy(x => x?.EnterDungeonFirstTime).ToList();
        State.Dungeons.AddRange(orderedDungeons);
        _mainWindowViewModel.DungeonBindings.Dungeons.AddRange(orderedDungeons);
        _mainWindowViewModel.DungeonBindings.InitListCollectionView();

        _eventBus.Publish(new DungeonsLoadedEvent(State.Dungeons.Count, DateTime.UtcNow));
    }

    public async Task SaveInFileAsync()
    {
        DirectoryController.CreateDirectoryWhenNotExists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName));
        var toSaveDungeons = State.Dungeons.Select(DungeonMapping.Mapping).ToList();
        await FileController.SaveAsync(toSaveDungeons, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.UserDataDirectoryName, Settings.Default.DungeonRunsFileName));
        Log.Information("Dungeons saved");
        _eventBus.Publish(new DungeonsSavedEvent(State.Dungeons.Count, DateTime.UtcNow));
    }

    private async Task SaveInFileAfterExceedingLimit(int limit)
    {
        if (++State.AddDungeonCounter < limit)
        {
            return;
        }

        await SaveInFileAsync();
        State.AddDungeonCounter = 0;
    }

    #endregion
}