using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using Steamworks;
using System;
using HarmonyLib;

using LC_GiftBox_Config.libs.LethalConfigNicerizer;

/*
    Here are some basic resources on code style and naming conventions to help
    you in your first CSharp plugin!

    https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
    https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
    https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces
*/

namespace LC_GiftBox_Config;

[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, $"{LCMPluginInfo.PLUGIN_TS_TEAM}.{LCMPluginInfo.PLUGIN_NAME}", LCMPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalConfigNicerizer.LethalConfig_GUID, BepInDependency.DependencyFlags.SoftDependency)]

public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource PluginLogger = null!;
    public static ConfigEntry<bool> giftboxMechanicsDisabled = null!;
    public static ConfigEntry<int> spawnStoreItemChance = null!;
    public static ConfigEntry<int> spawnScrapChance = null!;
    public static ConfigEntry<int> spawnGiftBoxChance = null!;
    public static ConfigEntry<int> spawnNothingChance = null!;
    public static ConfigEntry<int> doNothingChance = null!;

    public static ConfigEntry<int> scrapValueMin = null!;
    public static ConfigEntry<int> scrapValueMax = null!;
    public static ConfigEntry<int> scrapValueInfluence = null!;
    public static ConfigEntry<int> scrapRarityMin = null!;
    public static ConfigEntry<int> scrapRarityMax = null!;
    public static ConfigEntry<int> scrapRarityInfluence = null!;

    public static ConfigEntry<int> scrapValueIsGiftBoxChance = null!;
    public static ConfigEntry<int> scrapValueAdditionChance = null!;
    public static ConfigEntry<int> scrapValueAdditionMin = null!;
    public static ConfigEntry<int> scrapValueAdditionMax = null!;
    public static ConfigEntry<int> scrapValueMultiplierChance = null!;
    public static ConfigEntry<int> scrapValueMultiplierMin = null!;
    public static ConfigEntry<int> scrapValueMultiplierMax = null!;

    public static ConfigEntry<int> giftboxValueAdditionChance = null!;
    public static ConfigEntry<int> giftboxValueAdditionMin = null!;
    public static ConfigEntry<int> giftboxValueAdditionMax = null!;
    public static ConfigEntry<int> giftboxValueMultiplierChance = null!;
    public static ConfigEntry<int> giftboxValueMultiplierMin = null!;
    public static ConfigEntry<int> giftboxValueMultiplierMax = null!;

    public static ConfigEntry<int> giftboxRarityAdditionChance = null!;
    public static ConfigEntry<int> giftboxRarityAdditionMin = null!;
    public static ConfigEntry<int> giftboxRarityAdditionMax = null!;
    public static ConfigEntry<int> giftboxRarityMultiplierChance = null!;
    public static ConfigEntry<int> giftboxRarityMultiplierMin = null!;
    public static ConfigEntry<int> giftboxRarityMultiplierMax = null!;

    public static ConfigEntry<int> giftboxSpawnChance = null!;
    public static ConfigEntry<int> giftboxSpawnMin = null!;
    public static ConfigEntry<int> giftboxSpawnMax = null!;

    public static ConfigEntry<int> storeItemPriceMin = null!;
    public static ConfigEntry<int> storeItemPriceMax = null!;
    public static ConfigEntry<int> storeItemPriceInfluence = null!;

    internal static readonly Harmony harmony = new($"{LCMPluginInfo.PLUGIN_TS_TEAM}.{LCMPluginInfo.PLUGIN_NAME}");

    public static void Log(LogLevel logLevel, string logMessage)
    {
        PluginLogger.Log(logLevel, $"{logMessage}");
    }

    public static void Log(string logMessage)
    {
        Log(LogLevel.Info, logMessage);
    }

    private void ValidateMinMaxOrder(ConfigEntry<int> minEntry, ConfigEntry<int> maxEntry) 
    {
        if (minEntry.Value > maxEntry.Value) {
            Log(LogLevel.Warning, $"|{minEntry.Definition.Key}| is greater than |{maxEntry.Definition.Key}! Swapping values...");
            (minEntry.Value, maxEntry.Value) = (maxEntry.Value, minEntry.Value);
        }
    }

    private void ValidateConfigAndApplyPatches()
    {
        Log(LogLevel.Debug, "Validating config...");

        Config.SettingChanged -= ScheduleValidateConfigAndApplyPatches;

        if (spawnStoreItemChance.Value == 0 && spawnScrapChance.Value == 0 && spawnGiftBoxChance.Value == 0 && spawnNothingChance.Value == 0 && doNothingChance.Value == 0) {
            int maxChance = (doNothingChance.Description.AcceptableValues as AcceptableValueRange<int>)!.MaxValue;

            Log(LogLevel.Error, $"All [{doNothingChance.Definition.Section}] config weights are 0! Setting |{doNothingChance.Definition.Key}| to {maxChance}...");
            doNothingChance.Value = maxChance;
        }

		ValidateMinMaxOrder(              scrapValueMin,  scrapValueMax);
		ValidateMinMaxOrder(             scrapRarityMin,  scrapRarityMax);
		ValidateMinMaxOrder(      scrapValueAdditionMin,  scrapValueAdditionMax);
		ValidateMinMaxOrder(    scrapValueMultiplierMin,  scrapValueMultiplierMax);
		ValidateMinMaxOrder(          storeItemPriceMin,  storeItemPriceMax);
		ValidateMinMaxOrder(   giftboxRarityAdditionMin,  giftboxRarityAdditionMax);
		ValidateMinMaxOrder( giftboxRarityMultiplierMin,  giftboxRarityMultiplierMax);
		ValidateMinMaxOrder(    giftboxValueAdditionMin,  giftboxValueAdditionMax);
		ValidateMinMaxOrder(  giftboxValueMultiplierMin,  giftboxValueMultiplierMax);
		ValidateMinMaxOrder(            giftboxSpawnMin,  giftboxSpawnMax);
        
        Config.SettingChanged += ScheduleValidateConfigAndApplyPatches;

        Log(LogLevel.Debug, "Unpatching...");
        harmony.UnpatchSelf();

        Log(LogLevel.Debug, "Patching...");
        harmony.PatchAll();

        Log(LogLevel.Debug, "Finished config validation and patching!");
    }

    private void ScheduleValidateConfigAndApplyPatches(object? eventSender = null, SettingChangedEventArgs? eventArgs = null)
    {
        Log(LogLevel.Debug, "Scheduling config validation...");

        string validationMethodName = nameof(ValidateConfigAndApplyPatches);
        CancelInvoke(validationMethodName);
        Invoke(validationMethodName, 0.33f);
    }

    private void Awake()
    {
        PluginLogger = Logger;
        Log($"[v{LCMPluginInfo.PLUGIN_VERSION}] Loading...");

        giftboxMechanicsDisabled = LethalConfigNicerizer.Nicerize(Config.Bind("Toggle", "Disable All Modifications", false, new ConfigDescription("Toggle this setting to disable the modded gift box mechanics")));

        giftboxValueAdditionChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Addition Chance (%)", 100, new ConfigDescription("The likelihood (% chance) of the gift box receiving an addition to its scrap value    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxValueAdditionMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Addition Minimum", 30, new ConfigDescription("The minimum possible value of the addition applied to the gift box's scrap value    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), [])));
        giftboxValueAdditionMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Addition Maximum", 60, new ConfigDescription("The maximum possible value of the addition applied to the gift box's scrap value    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), [])));
        giftboxValueMultiplierChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Multiplier Chance (%)", 35, new ConfigDescription("The likelihood (% chance) of the gift box receiving a multiplier to its scrap value    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxValueMultiplierMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Multiplier Minimum (%)", 120, new ConfigDescription("The minimum possible value of the multiplier applied to the gift box's scrap value    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));
        giftboxValueMultiplierMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Scrap Value", "Multiplier Maximum (%)", 150, new ConfigDescription("The maximum possible value of the multiplier applied to the gift box's scrap value    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));

        giftboxRarityAdditionChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Weight", "Addition Chance (%)", 100, new ConfigDescription("The likelihood (% chance) of gift boxes receiving an addition to their spawn weight within the current level    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxRarityAdditionMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Chance", "Addition Minimum", 30, new ConfigDescription("The minimum possible value of the addition applied to gift boxes' spawn weight within the current level    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), [])));
        giftboxRarityAdditionMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Chance", "Addition Maximum", 60, new ConfigDescription("The maximum possible value of the addition applied to gift boxes' spawn weight within the current level    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(-1000, 1000), [])));
        giftboxRarityMultiplierChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Chanceg", "Multiplier Chance (%)", 35, new ConfigDescription("The likelihood (% chance) of gift boxes receiving a multiplier to their spawn weight within the current level    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxRarityMultiplierMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Chance", "Multiplier Minimum", 120, new ConfigDescription("The minimum possible value of the multiplier applied to gift boxes' spawn weight within the current level    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));
        giftboxRarityMultiplierMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Chance", "Multiplier Maximum", 150, new ConfigDescription("The maximum possible value of the multiplier applied to gift boxes' spawn weight within the current level    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));

        giftboxSpawnChance = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Anomaly", "Anomalous Spawning Chance (%)", 100, new ConfigDescription("The likelihood (% chance) of gift boxes anomalously spawning in the current level, separate from the level's natural scrap pool mechanics    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxSpawnMin = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Anomaly", "Minimum Gift Boxes", 2, new ConfigDescription("The minimum possible number of gift boxes to be anomalously spawned    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 100), [])));
        giftboxSpawnMax = LethalConfigNicerizer.Nicerize(Config.Bind("Gift Box Spawn Anomaly", "Maximum Gift Boxes", 5, new ConfigDescription("The maximum possible number of gift boxes to be anomalously spawned    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 100), [])));

        spawnStoreItemChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type Selection", "Store Item Chance (Selection Weight)", 60, new ConfigDescription("The selection weight of a gift box containing a store item.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 1000), [])));
        spawnScrapChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type Selection", "Scrap Item Chance (Selection Weight)", 35, new ConfigDescription("The selection weight of a gift box containing a scrap item.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \n[Vanilla Value: 100]", new AcceptableValueRange<int>(0, 1000), [])));
        spawnGiftBoxChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type Selection", "Gift Box Chance (Selection Weight)", 1, new ConfigDescription("The selection weight of a gift box containing another gift box.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 1000), [])));
        spawnNothingChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type Selection", "Empty Chance (Selection Weight)", 4, new ConfigDescription("The selection weight of a gift box being empty.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 1000), [])));
        doNothingChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Item Type Selection", "Unmodified Chance (Selection Weight)", 0, new ConfigDescription("The selection weight of a gift box not being modified by this mod, i.e. so another gift box mod's effects can function instead.     \n0 = will not happen    \nLarger selection weight = more likely to happen    \n    \nIf you do not have any other gift box mods that function by transpiling OpenGiftBoxServerRpc(), I recommend leaving this value at 0. Otherwise, I recommend setting their probability values to 100% and this probability value to the weight you'd like to assign to the other mod, so that whenever this mod selects this hands-off behavior, the other mod's functionality will have a 100% chance to occur rather than simply using vanilla behavior", new AcceptableValueRange<int>(0, 1000), [])));

        scrapValueMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Selection", "Scrap Value Minimum", 0, new ConfigDescription("The minimum value required for a scrap item to be selected by the gift box    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        scrapValueMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Selection", "Scrap Value Maximum", int.MaxValue, new ConfigDescription("The maximum value required for a scrap item to be selected by the gift box    \n    \n[Vanilla Value: infinity]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        scrapValueInfluence = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Selection", "Scrap Value Influence (%)", -50, new ConfigDescription("How much influence a scrap item's value has over its selection weight.     \n0 = scrap item's value does not influence its selection weight    \nLarger influence percentage = high-value scrap items are more likely than low-value scrap items    \nNegative influence percentage = high-value scrap items are less likely than low-value scrap items    \n    \nEach selectable scrap item is given a selection weight equal to their scrap value raised to the power of this percentage (i.e. 100% = 100 / 10    \n0 = 1, so the exponent is 1). e.g. if this percentage is set to 200%, a scrap item with a value of 2 has a selection weight of 4 (2 ^ 200% = 2 ^ 2 = 4), which is four times the selection weight of a scrap item with a value of 1 and therefore a selection weight of 1 (1 ^ 200% = 1 ^ 2 = 1). If this value is negative, then the selection weights are inverted - e.g. -100% results in a scrap item with a value of 2 receiving a selection weight of 0.5 (2 ^ -100% = 2 ^ -1 = 1 / (2 ^ 1) = 1 / 2 = 0.5)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(-1000, 1000), [])));
        scrapRarityMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Selection", "Spawn Weight Minimum", 0, new ConfigDescription("The minimum spawn weight required for a scrap item to be selected by the gift box    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        scrapRarityMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Selection", "Spawn Weight Maximum", int.MaxValue, new ConfigDescription("The maximum value required for a scrap item to be selected by the gift box    \n    \n[Vanilla Value: infinity]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        scrapRarityInfluence = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Selection", "Spawn Weight Influence (%)", 50, new ConfigDescription("How much influence a scrap item's spawn weight within the current level has over its selection weight.     \n0 = scrap item's spawn weight does not influence its selection weight    \nLarger influence percentage = common scrap items are more likely than rare scrap items    \nNegative influence percentage = common scrap items are less likely than rare scrap items    \n    \nEach selectable scrap item is given a selection weight equal to their spawn weight raised to the power of this percentage (i.e. 100% = 100 / 10    \n0 = 1, so the exponent is 1). e.g. if this percentage is set to 200%, a scrap item with a spawn weight of 2 has a selection weight of 4 (2 ^ 200% = 2 ^ 2 = 4), which is four times the selection weight of a scrap item with a spawn weight of 1 and therefore a selection weight of 1 (1 ^ 200% = 1 ^ 2 = 1). If this value is negative, then the selection weights are inverted - e.g. -100% results in a scrap item with a spawn weight of 2 receiving a selection weight of 0.5 (2 ^ -100% = 2 ^ -1 = 1 / (2 ^ 1) = 1 / 2 = 0.5)    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(-1000, 1000), [])));

        scrapValueIsGiftBoxChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value Modification", "Inherit Gift Box Value Chance (%)", 15, new ConfigDescription("The likelihood (% chance) of the selected scrap item having the same scrap value as the gift box itself    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        scrapValueAdditionChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value Modification", "Addition Chance (%)", 100, new ConfigDescription("The likelihood (% chance) of the selected scrap item receiving an addition to its scrap value (if the scrap item inherits the gift box's scrap value, this addition will not be applied)    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 100), [])));
        scrapValueAdditionMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value Modification", "Addition Minimum", 30, new ConfigDescription("The minimum possible value of the addition applied to the selected scrap item's scrap value    \n    \n[Vanilla Value: 25]", new AcceptableValueRange<int>(-1000, 1000), [])));
        scrapValueAdditionMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value Modification", "Addition Maximum", 60, new ConfigDescription("The maximum possible value of the addition applied to the selected scrap item's scrap value    \n    \n[Vanilla Value: 35]", new AcceptableValueRange<int>(-1000, 1000), [])));
        scrapValueMultiplierChance = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value Modification", "Multiplier Chance (%)", 35, new ConfigDescription("The likelihood (% chance) of the selected scrap item receiving a multiplier to its scrap value (if the scrap item inherits the gift box's scrap value, this multiplier will not be applied)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(0, 100), [])));
        scrapValueMultiplierMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value Modification", "Multiplier Minimum (%)", 120, new ConfigDescription("The minimum possible value of the multiplier applied to the selected scrap item's scrap value    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));
        scrapValueMultiplierMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Scrap Value Modification", "Multiplier Maximum (%)", 150, new ConfigDescription("The maximum possible value of the multiplier applied to the selected scrap item's scrap value    \n    \n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), [])));

        storeItemPriceMin = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item Selection", "Price Minimum", 0, new ConfigDescription("The minimum store item price required for an item to be selected by the gift box    \n    \n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        storeItemPriceMax = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item Selection", "Price Maximum", int.MaxValue, new ConfigDescription("The maximum store item price required for an item to be selected by the gift box    \n    \n[Vanilla Value: infinity]", new AcceptableValueRange<int>(0, int.MaxValue), [])));
        storeItemPriceInfluence = LethalConfigNicerizer.Nicerize(Config.Bind("Contained Store Item Selection", "Price Influence (%)", -100, new ConfigDescription("How much influence a store item's price has over its selection weight.     \n0 = store item's price does not influence its selection weight    \nLarger influence percentage = expensive store items are more likely than cheap store items    \nNegative influence percentage = expensive store items are less likely than cheap store items    \n    \nEach selectable store item is given a selection weight equal to their store price raised to the power of this percentage (i.e. 100% = 100 / 10    \n0 = 1, so the exponent is 1). e.g. if this percentage is set to 200%, a store item with a price of 2 has a selection weight of 4 (2 ^ 200% = 2 ^ 2 = 4), which is four times the selection weight of a store item with a price of 1 and therefore a selection weight of 1 (1 ^ 200% = 1 ^ 2 = 1). If this value is negative, then the selection weights are inverted - e.g. -100% results in a store item with a price of 2 receiving a selection weight of 0.5 (2 ^ -100% = 2 ^ -1 = 1 / (2 ^ 1) = 1 / 2 = 0.5)    \n    \n[Vanilla Value: 0%]", new AcceptableValueRange<int>(-1000, 1000), [])));

        // Migrate old names to new names
        MigrateEntry("Gift Box - Toggle", "Disable modded mechanics", giftboxMechanicsDisabled);

        MigrateEntry("Gift Box - Gift Box Value", "Chance for the gift box to receive scrap value addition (%)", giftboxValueAdditionChance);
        MigrateEntry("Gift Box - Gift Box Value", "Minimum gift box value addition", giftboxValueAdditionMin);
        MigrateEntry("Gift Box - Gift Box Value", "Maximum gift box value addition", giftboxValueAdditionMax);
        MigrateEntry("Gift Box - Gift Box Value", "Chance for gift box to receive scrap value multiplier (%)", giftboxValueMultiplierChance);
        MigrateEntry("Gift Box - Gift Box Value", "Minimum gift box value multiplier (%)", giftboxValueMultiplierMin);
        MigrateEntry("Gift Box - Gift Box Value", "Maximum gift box value multiplier (%)", giftboxValueMultiplierMax);

        MigrateEntry("Gift Box - Gift Box Natural Spawn Chance", "Chance for gift boxes to receive spawn weight addition (%)", giftboxRarityAdditionChance);
        MigrateEntry("Gift Box - Gift Box Natural Spawn Chance", "Minimum gift box spawn weight addition", giftboxRarityAdditionMin);
        MigrateEntry("Gift Box - Gift Box Natural Spawn Chance", "Maximum gift box spawn weight addition", giftboxRarityAdditionMax);
        MigrateEntry("Gift Box - Gift Box Natural Spawn Chance", "Chance for gift boxes to receive spawn weight multiplier (%)", giftboxRarityMultiplierChance);
        MigrateEntry("Gift Box - Gift Box Natural Spawn Chance", "Minimum gift box spawn weight multiplier (%)", giftboxRarityMultiplierMin);
        MigrateEntry("Gift Box - Gift Box Natural Spawn Chance", "Maximum gift box spawn weight multiplier (%)", giftboxRarityMultiplierMax);

        MigrateEntry("Gift Box - Gift Box Anomalous Spawning", "Chance for gift boxes to anomalously spawn (%)", giftboxSpawnChance);
        MigrateEntry("Gift Box - Gift Box Anomalous Spawning", "Minimum number of anomalously spawned gift boxes", giftboxSpawnMin);
        MigrateEntry("Gift Box - Gift Box Anomalous Spawning", "Maximum number of anomalously spawned gift boxes", giftboxSpawnMax);

        MigrateEntry("Gift Box - Behavior Selection", "Chance to select a store item (Selection Weight)", spawnStoreItemChance);
        MigrateEntry("Gift Box - Behavior Selection", "Chance to select a scrap item (Selection Weight)", spawnScrapChance);
        MigrateEntry("Gift Box - Behavior Selection", "Chance to select another gift box (Selection Weight)", spawnGiftBoxChance);
        MigrateEntry("Gift Box - Behavior Selection", "Chance to select no item (Selection Weight)", spawnNothingChance);
        MigrateEntry("Gift Box - Behavior Selection", "Chance to leave gift box unmodified (Selection Weight)", doNothingChance);

        MigrateEntry("Gift Box - Scrap Selection", "Minimum selectable scrap value", scrapValueMin);
        MigrateEntry("Gift Box - Scrap Selection", "Maximum selectable scrap value", scrapValueMax);
        MigrateEntry("Gift Box - Scrap Selection", "Scrap value influence percentage (%)", scrapValueInfluence);
        MigrateEntry("Gift Box - Scrap Selection", "Minimum selectable scrap spawn weight", scrapRarityMin);
        MigrateEntry("Gift Box - Scrap Selection", "Maximum selectable scrap spawn weight", scrapRarityMax);
        MigrateEntry("Gift Box - Scrap Selection", "Scrap spawn weight influence percentage (%)", scrapRarityInfluence);

        MigrateEntry("Gift Box - Scrap Item Value", "Chance for scrap item to inherit gift box value (%)", scrapValueIsGiftBoxChance);
        MigrateEntry("Gift Box - Scrap Item Value", "Chance for scrap item to receive scrap value addition (%)", scrapValueAdditionChance);
        MigrateEntry("Gift Box - Scrap Item Value", "Minimum scrap item value addition", scrapValueAdditionMin);
        MigrateEntry("Gift Box - Scrap Item Value", "Maximum scrap item value addition", scrapValueAdditionMax);
        MigrateEntry("Gift Box - Scrap Item Value", "Chance for scrap item to receive scrap value multiplier (%)", scrapValueMultiplierChance);
        MigrateEntry("Gift Box - Scrap Item Value", "Minimum scrap item value multiplier (%)", scrapValueMultiplierMin);
        MigrateEntry("Gift Box - Scrap Item Value", "Maximum scrap item value multiplier (%)", scrapValueMultiplierMax);

        MigrateEntry("Gift Box - Store Item Selection", "Minimum selectable store item price", storeItemPriceMin);
        MigrateEntry("Gift Box - Store Item Selection", "Maximum selectable store item price", storeItemPriceMax);
        MigrateEntry("Gift Box - Store Item Selection", "Store item price influence percentage (%)", storeItemPriceInfluence);

        ValidateConfigAndApplyPatches();

        Log($"[v{LCMPluginInfo.PLUGIN_VERSION}] Finished loading!");
    }

    private void MigrateEntry<T>(string oldSection, string oldKey, ConfigEntry<T> newEntry)
    {
        if (!Config.TryGetEntry(oldSection, oldKey, out ConfigEntry<T> oldEntry)) return;

        Log($"Migrating [{oldSection}].[{oldKey}] to [{newEntry.Definition.Section}].[{newEntry.Definition.Key}]...");

        newEntry.Value = oldEntry.Value;
        Config.Remove(oldEntry.Definition);
    }

}