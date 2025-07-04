﻿using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;

using LCUtils;

namespace LC_GiftBox_Config;

[BepInPlugin(LCMProjectInfo.PROJECT_GUID, $"{LCMProjectInfo.PROJECT_AUTHORS}.{LCMProjectInfo.PROJECT_NAME}", LCMProjectInfo.PROJECT_VERSION)]
[BepInDependency(StaticNetcodeLib.StaticNetcodeLib.Guid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(LCUtils.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(ILUtils.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]

public class Plugin : BaseUnityPlugin
{
    #region Plugin Info
    /*
      Here, we make the plugin instance and info accessible anywhere
    */
    
    public static Plugin Instance { get; private set; } = null!;
    public const string PLUGIN_GUID = LCMProjectInfo.PROJECT_GUID;
    public const string PLUGIN_NAME = LCMProjectInfo.PROJECT_NAME;
    public const string PLUGIN_AUTHORS = LCMProjectInfo.PROJECT_AUTHORS;
    public const string PLUGIN_VERSION = LCMProjectInfo.PROJECT_VERSION;
  #endregion

  #region Log Methods
    /* 
      BepInEx makes you a ManualLogSource for free called "Logger"
      that is accessed via the BaseUnityPlugin instance. Your plugin's
      code can find it by using Plugin.Instance.Logger.

      For convenience, we define static logging functions here so that
      the logger's functions can be called via Plugin.LogInfo(...),
      Plugin.LogDebug(...), Plugin.Log(...), etc.
    */
  
    public static void Log(LogLevel level, object data) => Instance.Logger.Log(level, data);
    public static void LogFatal(object data) => Instance.Logger.LogFatal(data);
    public static void LogError(object data) => Instance.Logger.LogError(data);
    public static void LogWarning(object data) => Instance.Logger.LogWarning(data);
    public static void LogMessage(object data) => Instance.Logger.LogMessage(data);
    public static void LogInfo(object data) => Instance.Logger.LogInfo(data);
    public static void LogDebug(object data) => Instance.Logger.LogDebug(data);
  #endregion

    public const int GIFTBOX_ITEM_ID = 152767;
    public static PersistentItemReference GIFTBOX_ITEM {get; private set;} = null!;

    public static ConfigEntry<bool> giftboxMechanicsDisabled = null!;
    public static ConfigEntry<bool> giftboxDupeSoundsBugFixDisabled = null!;
    public static ConfigEntry<bool> giftboxToolScrapValueBugfixDisabled = null!;

    public static ConfigEntry<int> giftboxEggsplosionChance = null!;
    public static ConfigEntry<int> positionRNGInfluence = null!;

    public static ConfigEntry<int> spawnStoreItemChance = null!;
    public static ConfigEntry<int> spawnScrapChance = null!;
    public static ConfigEntry<int> giftboxRecursionChance = null!;
    public static ConfigEntry<int> spawnNothingChance = null!;
    public static ConfigEntry<int> doNothingChance = null!;

    public static ConfigEntry<int> scrapValueMin = null!;
    public static ConfigEntry<int> scrapValueMax = null!;
    public static ConfigEntry<int> scrapValueInfluence = null!;
    public static ConfigEntry<int> scrapRarityMin = null!;
    public static ConfigEntry<int> scrapRarityMax = null!;
    public static ConfigEntry<bool> scrapRarityCanBeZero = null!;
    public static ConfigEntry<int> scrapRarityInfluence = null!;
    public static ConfigEntry<int> scrapSpawn1ExtrasChance = null!;
    public static ConfigEntry<int> scrapSpawn2ExtrasChance = null!;
    public static ConfigEntry<int> scrapSpawn4ExtrasChance = null!;
    public static ConfigEntry<int> scrapSpawn8ExtrasChance = null!;

    public static ConfigEntry<int> scrapValueIsGiftBoxChance = null!;
    public static ConfigEntry<int> scrapValueMultiplierChance = null!;
    public static ConfigEntry<int> scrapValueMultiplierMin = null!;
    public static ConfigEntry<int> scrapValueMultiplierMax = null!;
    public static ConfigEntry<int> scrapValueAdditionChance = null!;
    public static ConfigEntry<int> scrapValueAdditionMin = null!;
    public static ConfigEntry<int> scrapValueAdditionMax = null!;

    public static ConfigEntry<int> giftboxValueMultiplierChance = null!;
    public static ConfigEntry<int> giftboxValueMultiplierMin = null!;
    public static ConfigEntry<int> giftboxValueMultiplierMax = null!;
    public static ConfigEntry<int> giftboxValueAdditionChance = null!;
    public static ConfigEntry<int> giftboxValueAdditionMin = null!;
    public static ConfigEntry<int> giftboxValueAdditionMax = null!;

    public static ConfigEntry<int> giftboxRarityMultiplierChance = null!;
    public static ConfigEntry<int> giftboxRarityMultiplierMin = null!;
    public static ConfigEntry<int> giftboxRarityMultiplierMax = null!;

    public static ConfigEntry<int> giftboxRarityAdditionChance = null!;
    public static ConfigEntry<int> giftboxRarityAdditionMin = null!;
    public static ConfigEntry<int> giftboxRarityAdditionMax = null!;

    public static ConfigEntry<int> giftboxRecursionSpawn1ExtrasChance = null!;
    public static ConfigEntry<int> giftboxRecursionSpawn2ExtrasChance = null!;
    public static ConfigEntry<int> giftboxRecursionSpawn4ExtrasChance = null!;
    public static ConfigEntry<int> giftboxRecursionSpawn8ExtrasChance = null!;
    public static ConfigEntry<int> giftboxRecursionSpawn16ExtrasChance = null!;

    public static ConfigEntry<int> giftboxSpawnChance = null!;
    public static ConfigEntry<int> giftboxSpawnMin = null!;
    public static ConfigEntry<int> giftboxSpawnMax = null!;

    public static ConfigEntry<int> storeItemPriceMin = null!;
    public static ConfigEntry<int> storeItemPriceMax = null!;
    public static ConfigEntry<int> storeItemPriceInfluence = null!;
    public static ConfigEntry<bool> storeItemMustBeBuyable = null!;
    public static ConfigEntry<int> storeItemSpawn1ExtrasChance = null!;
    public static ConfigEntry<int> storeItemSpawn2ExtrasChance = null!;
    public static ConfigEntry<int> storeItemSpawn4ExtrasChance = null!;
    public static ConfigEntry<int> storeItemSpawn8ExtrasChance = null!;

    public ConfigFile ItemConfig = null!;
    public struct PerItemConfig
    {
        public ConfigEntry<bool> blacklisted;
        public ConfigEntry<int> selectionWeightMultiplierChance;
        public ConfigEntry<int> selectionWeightMultiplierMin;
        public ConfigEntry<int> selectionWeightMultiplierMax;
        public ConfigEntry<int> selectionWeightAdditionChance;
        public ConfigEntry<int> selectionWeightAdditionMin;
        public ConfigEntry<int> selectionWeightAdditionMax;

        // don't @ me
        public ConfigEntry<bool> ignoreGlobalSpawnExtraChance;
        public ConfigEntry<int> spawn1ExtraChance;
        public ConfigEntry<int> spawn2ExtraChance;
        public ConfigEntry<int> spawn4ExtraChance;
        public ConfigEntry<int> spawn8ExtraChance;

    }
    public static Dictionary<PersistentItemReference, PerItemConfig> perItemConfigs = [];

    internal static readonly Harmony harmony = new($"{PLUGIN_AUTHORS}.{PLUGIN_NAME}");

    private void ValidateMinMaxOrder(ConfigEntry<int> minEntry, ConfigEntry<int> maxEntry) 
    {
        if (minEntry.Value > maxEntry.Value) {
            LogWarning($"|{minEntry.Definition.Key}| is greater than |{maxEntry.Definition.Key}! Swapping values...");
            (minEntry.Value, maxEntry.Value) = (maxEntry.Value, minEntry.Value);
        }
    }

    private void ValidateConfigAndApplyPatches()
    {
        LogDebug("Validating config...");

        // Cancel any scheduled validations since we're already validating
        CancelInvoke(nameof(ValidateConfigAndApplyPatches));

        // Unsubscribe from the event so we don't trigger ourselves
        Config.SettingChanged -= ScheduleValidateConfigAndApplyPatches;
        ItemConfig.SettingChanged -= ScheduleValidateConfigAndApplyPatches;

        // Warn if all behavior weights are 0
        if (spawnStoreItemChance.Value == 0 && spawnScrapChance.Value == 0 && giftboxRecursionChance.Value == 0 && spawnNothingChance.Value == 0 && doNothingChance.Value == 0) {
            LogWarning($"All [{doNothingChance.Definition.Section}] config weights are 0! This will cause the gift box to always be unmodified! Please set at least one of the weights to a non-zero value!");
        }

        // Validate min/max order for all global settings
		ValidateMinMaxOrder(                 scrapValueMin,  scrapValueMax);
		ValidateMinMaxOrder(                scrapRarityMin,  scrapRarityMax);
        ValidateMinMaxOrder(       scrapValueMultiplierMin,  scrapValueMultiplierMax);
		ValidateMinMaxOrder(         scrapValueAdditionMin,  scrapValueAdditionMax);
		ValidateMinMaxOrder(             storeItemPriceMin,  storeItemPriceMax);
        ValidateMinMaxOrder(    giftboxRarityMultiplierMin,  giftboxRarityMultiplierMax);
		ValidateMinMaxOrder(      giftboxRarityAdditionMin,  giftboxRarityAdditionMax);
        ValidateMinMaxOrder(     giftboxValueMultiplierMin,  giftboxValueMultiplierMax);
		ValidateMinMaxOrder(       giftboxValueAdditionMin,  giftboxValueAdditionMax);
		ValidateMinMaxOrder(               giftboxSpawnMin,  giftboxSpawnMax);

        // Validate scrap rarity if zero-rarity has not been explicitly opted into
        if (scrapRarityCanBeZero.Value == false) {
            scrapRarityMin.Value = Math.Max(scrapRarityMin.Value, 1);
            scrapRarityMax.Value = Math.Max(scrapRarityMax.Value, 1);
        }

        // Validate min/max order for all per-item settings
        perItemConfigs.Values.Do(itemConfig => {
            ValidateMinMaxOrder( itemConfig.selectionWeightMultiplierMin, itemConfig.selectionWeightMultiplierMax);
            ValidateMinMaxOrder(   itemConfig.selectionWeightAdditionMin, itemConfig.selectionWeightAdditionMax);
        });
        
        // Resubscribe and update the files
        Config.SettingChanged += ScheduleValidateConfigAndApplyPatches;
        ItemConfig.SettingChanged += ScheduleValidateConfigAndApplyPatches;
        Config.Save();
        ItemConfig.Save();

        // Harmony Repatch \\

        LogDebug("Unpatching...");
        harmony.UnpatchSelf();

        LogDebug("Patching...");
        try{
            harmony.PatchAll();
        } catch (Exception e) {
            LogError($"Patching failed! Unpatching! Exception:\n{e}");
            
            harmony.UnpatchSelf();
        }

        LogDebug("Finished config validation and patching!");
    }

    private void ScheduleValidateConfigAndApplyPatches(object? eventSender = null, SettingChangedEventArgs? eventArgs = null)
    {
        string validationMethodName = nameof(ValidateConfigAndApplyPatches);
        CancelInvoke(validationMethodName);
        Invoke(validationMethodName, 0.33f);
    }

    private void CreateItemConfig(PersistentItemReference itemRef)
    {
        // Skip if missing spawnPrefab or GrabbableObject
        if (itemRef.GrabbableObject == null) {
            LogDebug($"Skipping registration of item [{itemRef.configName}] due to missing a spawnPrefab or GrabbableObject component!");
            return;
        };

        // Skip vanilla giftbox item
        if (itemRef.grabbableObjectType == typeof(GiftBoxItem) && itemRef.itemId == GIFTBOX_ITEM_ID) {
            GIFTBOX_ITEM = itemRef;
            return;
        }

        // Skip if config entry somehow already exists (this shouldn't happen)
        if (perItemConfigs.ContainsKey(itemRef)) return;

        // Create config entry for item
        string sectionName = $"{{{itemRef.configName}}}";
        perItemConfigs.Add(itemRef, new(){
            blacklisted = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Item Is Blacklisted", false, new ConfigDescription("If true, this item will not be selectable by the gift box    \n    \n[Vanilla Value: false]"))),
            selectionWeightMultiplierChance = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Selection Weight Multiplier Chance (%)", 0, new ConfigDescription("The likelihood (% chance) of the selected item receiving a multiplier to its selection weight    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), []))),
            selectionWeightMultiplierMin = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Selection Weight Multiplier Minimum (%)", 100, new ConfigDescription("The minimum possible value of the multiplier applied to the selected item's selection weight    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), []))),
            selectionWeightMultiplierMax = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Selection Weight Multiplier Maximum (%)", 100, new ConfigDescription("The maximum possible value of the multiplier applied to the selected item's selection weight    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), []))),
            selectionWeightAdditionChance = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Selection Weight Addition Chance (%)", 0, new ConfigDescription("The likelihood (% chance) of the selected item receiving an addition to its selection weight    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), []))),
            selectionWeightAdditionMin = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Selection Weight Addition Minimum", 0, new ConfigDescription("The minimum possible value of the addition applied to the selected item's selection weight    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), []))),
            selectionWeightAdditionMax = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Selection Weight Addition Maximum", 0, new ConfigDescription("The maximum possible value of the addition applied to the selected item's selection weight    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), []))),
            spawn1ExtraChance = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Spawn 1 Extra Chance (%)", 0, new ConfigDescription("The likelihood (% chance) of the gift box spawning an extra instance of the item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), []))),
            spawn2ExtraChance = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Spawn 2 Extra Chance (%)", 0, new ConfigDescription("The likelihood (% chance) of the gift box spawning two extra instances of the item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), []))),
            spawn4ExtraChance = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Spawn 4 Extra Chance (%)", 0, new ConfigDescription("The likelihood (% chance) of the gift box spawning four extra instances of the item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), []))),
            spawn8ExtraChance = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Spawn 8 Extra Chance (%)", 0, new ConfigDescription("The likelihood (% chance) of the gift box spawning eight extra instances of the item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), []))),
            ignoreGlobalSpawnExtraChance = LethalConfigNicerizer.Nicerize(ItemConfig.Bind(sectionName, "Ignores Global Extra Spawn Chances", false, new ConfigDescription("If true, when this item is selected by a gift box, the gift box will ignore the global extra spawn chances and only roll the item-specific extra spawn chances    \n    \n[Vanilla Value: N/A]"))),
        });

        ScheduleValidateConfigAndApplyPatches();
    }

    private void Awake()
    {
        // Here we assign the static value pointing to the Plugin instance
        Instance = this;

        // Log our awake here so we can see it in LogOutput.txt file
        LogInfo($"[v{PLUGIN_VERSION}] Loading...");

        // Init config file for items
        ItemConfig = new($"{Config.ConfigFilePath[..^4]}.Items.cfg", saveOnInit: true, ownerMetadata: Info.Metadata);

        // Prevent config from auto-saving on every change. We will handle this ourselves
        Config.SaveOnConfigSet = false;
        ItemConfig.SaveOnConfigSet = false;

        spawnStoreItemChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type", "Store Item Chance (Selection Weight)", 50, new ConfigDescription("The selection weight of a gift box containing a store item.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 1000), [])));
        spawnScrapChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type", "Scrap Item Chance (Selection Weight)", 30, new ConfigDescription("The selection weight of a gift box containing a scrap item.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \n[Vanilla Value: 100]", new AcceptableValueRange<int>(0, 1000), [])));
        giftboxRecursionChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type", "Gift Box Chance (Selection Weight)", 5, new ConfigDescription("The selection weight of a gift box containing another gift box.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 1000), [])));
        spawnNothingChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type", "Empty Chance (Selection Weight)", 15, new ConfigDescription("The selection weight of a gift box being empty.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 1000), [])));
        doNothingChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type", "Unmodified Chance (Selection Weight)", 0, new ConfigDescription("The selection weight of a gift box not being modified by this mod, i.e. so another gift box mod's effects can function instead.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \nIf you do not have any other gift box mods that function by transpiling OpenGiftBoxServerRpc(), I recommend leaving this value at 0. Otherwise, I recommend setting their probability values to 100% and this probability value to the weight you'd like to assign to the other mod, so that whenever this mod selects this hands-off behavior, the other mod's functionality will have a 100% chance to occur rather than simply using vanilla behavior", new AcceptableValueRange<int>(0, 1000), [])));

        scrapValueMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Scrap Value Minimum", 0, new ConfigDescription("The minimum value required for a scrap item to be selected by the gift box    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        scrapValueMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Scrap Value Maximum", int.MaxValue, new ConfigDescription("The maximum value required for a scrap item to be selected by the gift box    \n    \n[Vanilla Value: infinity]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        scrapValueInfluence = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Scrap Value Influence (%)", -50, new ConfigDescription("How much influence a scrap item's value has over its selection weight.     \n0 = scrap item's value does not influence its selection weight    \nLarger influence percentage = high-value scrap items are more likely than low-value scrap items    \nNegative influence percentage = high-value scrap items are less likely than low-value scrap items    \n    \nEach selectable scrap item is given a selection weight equal to their scrap value raised to the power of this percentage (i.e. 100% = 100 / 10    \n0 = 1, so the exponent is 1). e.g. if this percentage is set to 200%, a scrap item with a value of 2 has a selection weight of 4 (2 ^ 200% = 2 ^ 2 = 4), which is four times the selection weight of a scrap item with a value of 1 and therefore a selection weight of 1 (1 ^ 200% = 1 ^ 2 = 1). If this value is negative, then the selection weights are inverted - e.g. -100% results in a scrap item with a value of 2 receiving a selection weight of 0.5 (2 ^ -100% = 2 ^ -1 = 1 / (2 ^ 1) = 1 / 2 = 0.5)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(-1000, 1000), [])));
        scrapRarityMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Spawn Weight Minimum", 1, new ConfigDescription("The minimum spawn weight required for a scrap item to be selected by the gift box    \n    \n[Vanilla Value: 1]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        scrapRarityMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Spawn Weight Maximum", int.MaxValue, new ConfigDescription("The maximum value required for a scrap item to be selected by the gift box    \n    \n[Vanilla Value: infinity]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        scrapRarityInfluence = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Spawn Weight Influence (%)", 50, new ConfigDescription("How much influence a scrap item's spawn weight within the current level has over its selection weight.     \n0 = scrap item's spawn weight does not influence its selection weight    \nLarger influence percentage = common scrap items are more likely than rare scrap items    \nNegative influence percentage = common scrap items are less likely than rare scrap items    \n    \nEach selectable scrap item is given a selection weight equal to their spawn weight raised to the power of this percentage (i.e. 100% = 100 / 10    \n0 = 1, so the exponent is 1). e.g. if this percentage is set to 200%, a scrap item with a spawn weight of 2 has a selection weight of 4 (2 ^ 200% = 2 ^ 2 = 4), which is four times the selection weight of a scrap item with a spawn weight of 1 and therefore a selection weight of 1 (1 ^ 200% = 1 ^ 2 = 1). If this value is negative, then the selection weights are inverted - e.g. -100% results in a scrap item with a spawn weight of 2 receiving a selection weight of 0.5 (2 ^ -100% = 2 ^ -1 = 1 / (2 ^ 1) = 1 / 2 = 0.5)    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(-1000, 1000), [])));
        scrapRarityCanBeZero = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Spawn Weight Can Be Zero", false, new ConfigDescription("If true, scrap items with a spawn weight of 0 will be selectable by the gift box, i.e. if its scrap value causes it to be selected. (If this is set to false, Spawn Weight Minimum and Maximum will be adjusted to be no less than 1)    \n    \n[Vanilla Value: false]")));
        scrapSpawn1ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Spawn 1 Extra Chance (%)", 3, new ConfigDescription("The likelihood (% chance) of the gift box spawning an extra instance of the selected scrap item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        scrapSpawn2ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Spawn 2 Extra Chance (%)", 2, new ConfigDescription("The likelihood (% chance) of the gift box spawning two extra instances of the selected scrap item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        scrapSpawn4ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Spawn 4 Extra Chance (%)", 1, new ConfigDescription("The likelihood (% chance) of the gift box spawning four extra instances of the selected scrap item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        scrapSpawn8ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Item", "Spawn 8 Extra Chance (%)", 0, new ConfigDescription("The likelihood (% chance) of the gift box spawning eight extra instances of the selected scrap item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        
        storeItemPriceMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item", "Price Minimum", 0, new ConfigDescription("The minimum store item price required for an item to be selected by the gift box    \n    \n[Vanilla Value: N/A]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        storeItemPriceMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item", "Price Maximum", int.MaxValue, new ConfigDescription("The maximum store item price required for an item to be selected by the gift box    \n    \n[Vanilla Value: N/A]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        storeItemPriceInfluence = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item", "Price Influence (%)", -100, new ConfigDescription("How much influence a store item's price has over its selection weight.     \n0 = store item's price does not influence its selection weight    \nLarger influence percentage = expensive store items are more likely than cheap store items    \nNegative influence percentage = expensive store items are less likely than cheap store items    \n    \nEach selectable store item is given a selection weight equal to their store price raised to the power of this percentage (i.e. 100% = 100 / 10    \n0 = 1, so the exponent is 1). e.g. if this percentage is set to 200%, a store item with a price of 2 has a selection weight of 4 (2 ^ 200% = 2 ^ 2 = 4), which is four times the selection weight of a store item with a price of 1 and therefore a selection weight of 1 (1 ^ 200% = 1 ^ 2 = 1). If this value is negative, then the selection weights are inverted - e.g. -100% results in a store item with a price of 2 receiving a selection weight of 0.5 (2 ^ -100% = 2 ^ -1 = 1 / (2 ^ 1) = 1 / 2 = 0.5)    \n    \n[Vanilla Value: N/A]", new AcceptableValueRange<int>(-1000, 1000), [])));
        storeItemMustBeBuyable = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item", "Must Be Buyable", true, new ConfigDescription("If true, only store items that are accessible through the terminal will be selectable by the gift box    \n    \n[Vanilla Value: N/A]")));
        storeItemSpawn1ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item", "Spawn 1 Extra Chance (%)", 3, new ConfigDescription("The likelihood (% chance) of the gift box spawning an extra instance of the selected store item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        storeItemSpawn2ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item", "Spawn 2 Extra Chance (%)", 2, new ConfigDescription("The likelihood (% chance) of the gift box spawning two extra instances of the selected store item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        storeItemSpawn4ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item", "Spawn 4 Extra Chance (%)", 1, new ConfigDescription("The likelihood (% chance) of the gift box spawning four extra instances of the selected store item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        storeItemSpawn8ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item", "Spawn 8 Extra Chance (%)", 0, new ConfigDescription("The likelihood (% chance) of the gift box spawning eight extra instances of the selected store item. (Note: This effect can stack with the other effects that spawn extras)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));

        giftboxRecursionSpawn1ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Gift Box", "Spawn 1 Extra Chance (%)", 50, new ConfigDescription("The likelihood (% chance) of the gift box spawning an extra instance of the contained gift box.    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxRecursionSpawn2ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Gift Box", "Spawn 2 Extra Chance (%)", 25, new ConfigDescription("The likelihood (% chance) of the gift box spawning two extra instances of the contained gift box.    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxRecursionSpawn4ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Gift Box", "Spawn 4 Extra Chance (%)", 10, new ConfigDescription("The likelihood (% chance) of the gift box spawning four extra instances of the contained gift box.    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxRecursionSpawn8ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Gift Box", "Spawn 8 Extra Chance (%)", 5, new ConfigDescription("The likelihood (% chance) of the gift box spawning eight extra instances of the contained gift box.    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxRecursionSpawn16ExtrasChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Gift Box", "Spawn 16 Extra Chance (%)", 0, new ConfigDescription("The likelihood (% chance) of the gift box spawning sixteen extra instances of the contained gift box.    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));

        scrapValueIsGiftBoxChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value", "Inherit Gift Box Value Chance (%)", 15, new ConfigDescription("The likelihood (% chance) of the selected scrap item having the same scrap value as the gift box itself    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        scrapValueMultiplierChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value", "Multiplier Chance (%)", 35, new ConfigDescription("The likelihood (% chance) of the selected scrap item receiving a multiplier to its scrap value (if the scrap item inherits the gift box's scrap value, this multiplier will not be applied)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        scrapValueMultiplierMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value", "Multiplier Minimum (%)", 120, new ConfigDescription("The minimum possible value of the multiplier applied to the selected scrap item's scrap value    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));
        scrapValueMultiplierMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value", "Multiplier Maximum (%)", 150, new ConfigDescription("The maximum possible value of the multiplier applied to the selected scrap item's scrap value    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));
        scrapValueAdditionChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value", "Addition Chance (%)", 100, new ConfigDescription("The likelihood (% chance) of the selected scrap item receiving an addition to its scrap value (if the scrap item inherits the gift box's scrap value, this addition will not be applied)    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 100), [])));
        scrapValueAdditionMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value", "Addition Minimum", 30, new ConfigDescription("The minimum possible value of the addition applied to the selected scrap item's scrap value    \n    \n[Vanilla Value: 25]", new AcceptableValueRange<int>(-1000, 1000), [])));
        scrapValueAdditionMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value", "Addition Maximum", 40, new ConfigDescription("The maximum possible value of the addition applied to the selected scrap item's scrap value    \n    \n[Vanilla Value: 35]", new AcceptableValueRange<int>(-1000, 1000), [])));
        
        giftboxValueMultiplierChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Multiplier Chance (%)", 35, new ConfigDescription("The likelihood (% chance) of the gift box receiving a multiplier to its scrap value    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxValueMultiplierMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Multiplier Minimum (%)", 120, new ConfigDescription("The minimum possible value of the multiplier applied to the gift box's scrap value    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));
        giftboxValueMultiplierMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Multiplier Maximum (%)", 150, new ConfigDescription("The maximum possible value of the multiplier applied to the gift box's scrap value    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));
        giftboxValueAdditionChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Addition Chance (%)", 100, new ConfigDescription("The likelihood (% chance) of the gift box receiving an addition to its scrap value    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxValueAdditionMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Addition Minimum", 30, new ConfigDescription("The minimum possible value of the addition applied to the gift box's scrap value    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), [])));
        giftboxValueAdditionMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Addition Maximum", 40, new ConfigDescription("The maximum possible value of the addition applied to the gift box's scrap value    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), [])));
        
        giftboxRarityMultiplierChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Weight", "Multiplier Chance (%)", 25, new ConfigDescription("The likelihood (% chance) of gift boxes receiving a multiplier to their spawn weight within the current level    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxRarityMultiplierMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Weight", "Multiplier Minimum", 120, new ConfigDescription("The minimum possible value of the multiplier applied to gift boxes' spawn weight within the current level    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));
        giftboxRarityMultiplierMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Weight", "Multiplier Maximum", 150, new ConfigDescription("The maximum possible value of the multiplier applied to gift boxes' spawn weight within the current level    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));
        giftboxRarityAdditionChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Weight", "Addition Chance (%)", 25, new ConfigDescription("The likelihood (% chance) of gift boxes receiving an addition to their spawn weight within the current level    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxRarityAdditionMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Weight", "Addition Minimum", 10, new ConfigDescription("The minimum possible value of the addition applied to gift boxes' spawn weight within the current level    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), [])));
        giftboxRarityAdditionMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Weight", "Addition Maximum", 15, new ConfigDescription("The maximum possible value of the addition applied to gift boxes' spawn weight within the current level    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), [])));

        giftboxSpawnChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Anomaly", "Anomalous Spawning Chance (%)", 65, new ConfigDescription("The likelihood (% chance) of gift boxes anomalously spawning in the current level, separate from the level's natural scrap pool mechanics    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxSpawnMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Anomaly", "Minimum Gift Boxes", 2, new ConfigDescription("The minimum possible number of gift boxes to be anomalously spawned    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxSpawnMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Anomaly", "Maximum Gift Boxes", 5, new ConfigDescription("The maximum possible number of gift boxes to be anomalously spawned    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 100), [])));

        giftboxEggsplosionChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Behaviors", "Empty Gift Box Eggsplosion Chance (%)", 100, new ConfigDescription("The likelihood (% chance) of an empty gift box non-harmfully eggsploding (it won't harm you, but it may attract enemies who will)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        positionRNGInfluence = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Behaviors", "Position-Based Randomness Influence (%)", 50, new ConfigDescription("How much influence position-based randomness has over all gift box randomness mechanics. Lowering this value increases the randomness felt between gift boxes in the same position; increasing this value decreases the randomness felt between gift boxes in the same position    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 100), [])));

        giftboxMechanicsDisabled = LethalConfigNicerizer.Nicerize(Config.Bind("Compatibility / Debugging", "Disable All Mod Mechanics", false, new ConfigDescription("WARNING: May cause unexpected game behaviors, desyncs, or loss / corruption of mod-related save data! Do not use this setting unless you know what you're doing!    \n    \nToggle this setting to disable the modded gift box mechanics")));
        giftboxDupeSoundsBugFixDisabled = LethalConfigNicerizer.Nicerize(Config.Bind("Compatibility / Debugging", "Disable Gift Box Duplicate Sounds Bugfix", false, new ConfigDescription("Toggle this setting to disable the gift box duplicate sounds bugfix")));
        giftboxToolScrapValueBugfixDisabled = LethalConfigNicerizer.Nicerize(Config.Bind("Compatibility / Debugging", "Disable Gift Box Setting Tool Scrap Value Bugfix", false, new ConfigDescription("Toggle this setting to disable the bugfix for the gift box setting a tool's scrap value")));

        // Migrate old names to new names
        MigrateOldEntries(
            ("Gift Box - Gift Box Value", "Chance for the gift box to receive scrap value addition (%)", giftboxValueAdditionChance),
            ("Gift Box - Gift Box Value", "Minimum gift box value addition", giftboxValueAdditionMin),
            ("Gift Box - Gift Box Value", "Maximum gift box value addition", giftboxValueAdditionMax),
            ("Gift Box - Gift Box Value", "Chance for gift box to receive scrap value multiplier (%)", giftboxValueMultiplierChance),
            ("Gift Box - Gift Box Value", "Minimum gift box value multiplier (%)", giftboxValueMultiplierMin),
            ("Gift Box - Gift Box Value", "Maximum gift box value multiplier (%)", giftboxValueMultiplierMax),

            ("Gift Box - Gift Box Natural Spawn Chance", "Chance for gift boxes to receive spawn weight addition (%)", giftboxRarityAdditionChance),
            ("Gift Box - Gift Box Natural Spawn Chance", "Minimum gift box spawn weight addition", giftboxRarityAdditionMin),
            ("Gift Box - Gift Box Natural Spawn Chance", "Maximum gift box spawn weight addition", giftboxRarityAdditionMax),
            ("Gift Box - Gift Box Natural Spawn Chance", "Chance for gift boxes to receive spawn weight multiplier (%)", giftboxRarityMultiplierChance),
            ("Gift Box - Gift Box Natural Spawn Chance", "Minimum gift box spawn weight multiplier (%)", giftboxRarityMultiplierMin),
            ("Gift Box - Gift Box Natural Spawn Chance", "Maximum gift box spawn weight multiplier (%)", giftboxRarityMultiplierMax),

            ("Gift Box - Gift Box Anomalous Spawning", "Chance for gift boxes to anomalously spawn (%)", giftboxSpawnChance),
            ("Gift Box - Gift Box Anomalous Spawning", "Minimum number of anomalously spawned gift boxes", giftboxSpawnMin),
            ("Gift Box - Gift Box Anomalous Spawning", "Maximum number of anomalously spawned gift boxes", giftboxSpawnMax),

            ("Gift Box - Behavior Selection", "Chance to select a store item (Selection Weight)", spawnStoreItemChance),
            ("Gift Box - Behavior Selection", "Chance to select a scrap item (Selection Weight)", spawnScrapChance),
            ("Gift Box - Behavior Selection", "Chance to select another gift box (Selection Weight)", giftboxRecursionChance),
            ("Gift Box - Behavior Selection", "Chance to select no item (Selection Weight)", spawnNothingChance),
            ("Gift Box - Behavior Selection", "Chance to leave gift box unmodified (Selection Weight)", doNothingChance),

            ("Gift Box - Scrap Selection", "Minimum selectable scrap value", scrapValueMin),
            ("Gift Box - Scrap Selection", "Maximum selectable scrap value", scrapValueMax),
            ("Gift Box - Scrap Selection", "Scrap value influence percentage (%)", scrapValueInfluence),
            ("Gift Box - Scrap Selection", "Minimum selectable scrap spawn weight", scrapRarityMin),
            ("Gift Box - Scrap Selection", "Maximum selectable scrap spawn weight", scrapRarityMax),
            ("Gift Box - Scrap Selection", "Scrap spawn weight influence percentage (%)", scrapRarityInfluence),

            ("Gift Box - Scrap Item Value", "Chance for scrap item to inherit gift box value (%)", scrapValueIsGiftBoxChance),
            ("Gift Box - Scrap Item Value", "Chance for scrap item to receive scrap value addition (%)", scrapValueAdditionChance),
            ("Gift Box - Scrap Item Value", "Minimum scrap item value addition", scrapValueAdditionMin),
            ("Gift Box - Scrap Item Value", "Maximum scrap item value addition", scrapValueAdditionMax),
            ("Gift Box - Scrap Item Value", "Chance for scrap item to receive scrap value multiplier (%)", scrapValueMultiplierChance),
            ("Gift Box - Scrap Item Value", "Minimum scrap item value multiplier (%)", scrapValueMultiplierMin),
            ("Gift Box - Scrap Item Value", "Maximum scrap item value multiplier (%)", scrapValueMultiplierMax),

            ("Gift Box - Store Item Selection", "Minimum selectable store item price", storeItemPriceMin),
            ("Gift Box - Store Item Selection", "Maximum selectable store item price", storeItemPriceMax),
            ("Gift Box - Store Item Selection", "Store item price influence percentage (%)", storeItemPriceInfluence),

            ("Gift Box Behaviors", "Disable All Mod Mechanics", giftboxMechanicsDisabled),
            ("Gift Box Behaviors", "Disable Gift Box Duplicate Sounds Bugfix", giftboxDupeSoundsBugFixDisabled),
            ("Gift Box Behaviors", "Disable Gift Box Setting Tool Scrap Value Bugfix", giftboxToolScrapValueBugfixDisabled)
        );

        // Generate config for all items
        ItemUtils.RegisterItemHandler(CreateItemConfig);

        ValidateConfigAndApplyPatches();

        LogInfo($"[v{PLUGIN_VERSION}] Finished loading!");
    }

    private void MigrateOldEntries(params (string oldSection, string oldKey, ConfigEntryBase newEntry)[] migrations)
    {
        Dictionary<ConfigDefinition, string> orphanedEntries = Config.GetOrphanedEntries();

        migrations.Do(migration =>
        {
            // Apparently this works since this is what is done internally anyway
            ConfigDefinition oldDefinition = new(migration.oldSection, migration.oldKey);
            if (!orphanedEntries.TryGetValue(oldDefinition, out string orphanedEntryText)) return;

            migration.newEntry.SetSerializedValue(orphanedEntryText);
            orphanedEntries.Remove(oldDefinition);

            LogInfo($"Migrated [{oldDefinition.Section}].[{oldDefinition.Key}] to [{migration.newEntry.Definition.Section}].[{migration.newEntry.Definition.Key}]");
        });
    }
}