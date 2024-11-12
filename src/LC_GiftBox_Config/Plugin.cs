using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using Steamworks;
using System;
using HarmonyLib;
using System.Reflection;

/*
  Here are some basic resources on code style and naming conventions to help
  you in your first CSharp plugin!

  https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
  https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
  https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces
*/

namespace LC_GiftBox_Config;

[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, $"{LCMPluginInfo.PLUGIN_TS_TEAM}.{LCMPluginInfo.PLUGIN_NAME}", LCMPluginInfo.PLUGIN_VERSION)]

public class Plugin : BaseUnityPlugin
{
  public static ManualLogSource PluginLogger = null!;
  public static ConfigEntry<bool> giftBoxMechanicsDisabled = null!;
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

  private void ValidateConfigAndApplyPatches(object? eventSender = null, SettingChangedEventArgs? eventArgs = null)
  {
    Log(LogLevel.Debug, "Validating config...");

    Config.SettingChanged -= ValidateConfigAndApplyPatches;

    if (spawnStoreItemChance.Value == 0 && spawnScrapChance.Value == 0 && spawnGiftBoxChance.Value == 0 && spawnNothingChance.Value == 0 && doNothingChance.Value == 0) {
      Log(LogLevel.Error, $"All [{spawnScrapChance.Definition.Section}] config weights are 0! Setting |{giftBoxMechanicsDisabled.Definition.Key}| to true...");
      giftBoxMechanicsDisabled.Value = true;
    }

    if (scrapValueMin.Value > scrapValueMax.Value) {
      Log(LogLevel.Warning, $"|{scrapValueMin.Definition.Key}| is greater than |{scrapValueMax.Definition.Key}! Swapping values...");
      (scrapValueMin.Value, scrapValueMax.Value) = (scrapValueMax.Value, scrapValueMin.Value);
    }

    if (scrapRarityMin.Value > scrapRarityMax.Value) {
      Log(LogLevel.Warning, $"|{scrapRarityMin.Definition.Key}| is greater than |{scrapRarityMax.Definition.Key}! Swapping values...");
      (scrapRarityMin.Value, scrapRarityMax.Value) = (scrapRarityMax.Value, scrapRarityMin.Value);
    }

    if (scrapValueAdditionMin.Value > scrapValueAdditionMax.Value) {
      Log(LogLevel.Warning, $"|{scrapValueAdditionMin.Definition.Key}| is greater than |{scrapValueAdditionMax.Definition.Key}! Swapping values...");
      (scrapValueAdditionMin.Value, scrapValueAdditionMax.Value) = (scrapValueAdditionMax.Value, scrapValueAdditionMin.Value);
    }

    if (scrapValueMultiplierMin.Value > scrapValueMultiplierMax.Value) {
      Log(LogLevel.Warning, $"|{scrapValueMultiplierMin.Definition.Key}| is greater than |{scrapValueMultiplierMax.Definition.Key}! Swapping values...");
      (scrapValueMultiplierMin.Value, scrapValueMultiplierMax.Value) = (scrapValueMultiplierMax.Value, scrapValueMultiplierMin.Value);
    }

    if (storeItemPriceMin.Value > storeItemPriceMax.Value) {
      Log(LogLevel.Warning, $"|{storeItemPriceMin.Definition.Key}| is greater than |{storeItemPriceMax.Definition.Key}! Swapping values...");
      (storeItemPriceMin.Value, storeItemPriceMax.Value) = (storeItemPriceMax.Value, storeItemPriceMin.Value);
    }

    Config.SettingChanged += ValidateConfigAndApplyPatches;

    Log(LogLevel.Debug, "Unpatching...");
    harmony.UnpatchSelf();

    Log(LogLevel.Debug, "Patching...");
    harmony.PatchAll(Assembly.GetExecutingAssembly());

    Log(LogLevel.Debug, "Finished config validation and patching!");
  }

  private void Awake()
  {
    PluginLogger = Logger;
    Log($"[v{LCMPluginInfo.PLUGIN_VERSION}] Loading...");
    
    giftBoxMechanicsDisabled = Config.Bind("Gift Box - Toggle", "Disable modded mechanics", false, new ConfigDescription("Toggle this setting to disable the modded gift box mechanics"));

    spawnStoreItemChance = Config.Bind("Gift Box - Behavior Selection", "Chance to select a store item (Selection Weight)", 60, new ConfigDescription("The selection weight of a gift box containing a store item. 0 = will not happen, larger selection weight = more likely to happen\n\n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 1000), []));
    spawnScrapChance = Config.Bind("Gift Box - Behavior Selection", "Chance to select a scrap item (Selection Weight)", 35, new ConfigDescription("The selection weight of a gift box containing a scrap item. 0 = will not happen, larger selection weight = more likely to happen\n\n[Vanilla Value: 100]", new AcceptableValueRange<int>(0, 1000), []));
    spawnGiftBoxChance = Config.Bind("Gift Box - Behavior Selection", "Chance to select another gift box (Selection Weight)", 1, new ConfigDescription("The selection weight of a gift box containing another gift box. 0 = will not happen, larger selection weight = more likely to happen\n\n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 1000), []));
    spawnNothingChance = Config.Bind("Gift Box - Behavior Selection", "Chance to select no item (Selection Weight)", 4, new ConfigDescription("The selection weight of a gift box being empty. 0 = will not happen, larger selection weight = more likely to happen\n\n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 1000), []));
    doNothingChance = Config.Bind("Gift Box - Behavior Selection", "Chance to leave gift box unmodified (Selection Weight)", 0, new ConfigDescription("The selection weight of a gift box not being modified by this mod, i.e. so another gift box mod's effects can function instead. 0 = will not happen, larger selection weight = more likely to happen\n\nIf you do not have any other gift box mods that function by transpiling OpenGiftBoxServerRpc(), I recommend leaving this value at 0. Otherwise, I recommend setting their probability values to 100% and this probability value to the weight you'd like to assign to the other mod, so that whenever this mod selects this hands-off behavior, the other mod's functionality will have a 100% chance to occur rather than simply using vanilla behavior", new AcceptableValueRange<int>(0, 1000), []));

    scrapValueMin = Config.Bind("Gift Box - Scrap Selection", "Minimum selectable scrap value", 0, new ConfigDescription("The minimum value required for a scrap item to be selected by the gift box\n\n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, int.MaxValue), []));
    scrapValueMax = Config.Bind("Gift Box - Scrap Selection", "Maximum selectable scrap value", int.MaxValue, new ConfigDescription("The maximum value required for a scrap item to be selected by the gift box\n\n[Vanilla Value: infinity]", new AcceptableValueRange<int>(0, int.MaxValue), []));
    scrapValueInfluence = Config.Bind("Gift Box - Scrap Selection", "Scrap value influence percentage (%)", -50, new ConfigDescription("How much influence a scrap item's value has over its selection weight. 0 = scrap item's value does not influence its selection weight, larger influence percentage = high-value scrap items are more likely than low-value scrap items, negative influence percentage = high-value scrap items are less likely than low-value scrap items\n\nEach selectable scrap item is given a selection weight equal to their scrap value raised to the power of this percentage (i.e. 100% = 100 / 100 = 1, so the exponent is 1). e.g. if this percentage is set to 200%, a scrap item with a value of 2 has a selection weight of 4 (2 ^ 200% = 2 ^ 2 = 4), which is four times the selection weight of a scrap item with a value of 1 and therefore a selection weight of 1 (1 ^ 200% = 1 ^ 2 = 1). If this value is negative, then the selection weights are inverted - e.g. -100% results in a scrap item with a value of 2 receiving a selection weight of 0.5 (2 ^ -100% = 2 ^ -1 = 1 / (2 ^ 1) = 1 / 2 = 0.5)\n\n[Vanilla Value: 0%]", new AcceptableValueRange<int>(-1000, 1000), []));
    scrapRarityMin = Config.Bind("Gift Box - Scrap Selection", "Minimum selectable scrap spawn weight", 0, new ConfigDescription("The minimum spawn weight required for a scrap item to be selected by the gift box\n\n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, int.MaxValue), []));
    scrapRarityMax = Config.Bind("Gift Box - Scrap Selection", "Maximum selectable scrap spawn weight", int.MaxValue, new ConfigDescription("The maximum value required for a scrap item to be selected by the gift box\n\n[Vanilla Value: infinity]", new AcceptableValueRange<int>(0, int.MaxValue), []));
    scrapRarityInfluence = Config.Bind("Gift Box - Scrap Selection", "Scrap spawn weight influence percentage (%)", 50, new ConfigDescription("How much influence a scrap item's spawn weight within the current level has over its selection weight. 0 = scrap item's spawn weight does not influence its selection weight, larger influence percentage = common scrap items are more likely than rare scrap items, negative influence percentage = common scrap items are less likely than rare scrap items\n\nEach selectable scrap item is given a selection weight equal to their spawn weight raised to the power of this percentage (i.e. 100% = 100 / 100 = 1, so the exponent is 1). e.g. if this percentage is set to 200%, a scrap item with a spawn weight of 2 has a selection weight of 4 (2 ^ 200% = 2 ^ 2 = 4), which is four times the selection weight of a scrap item with a spawn weight of 1 and therefore a selection weight of 1 (1 ^ 200% = 1 ^ 2 = 1). If this value is negative, then the selection weights are inverted - e.g. -100% results in a scrap item with a spawn weight of 2 receiving a selection weight of 0.5 (2 ^ -100% = 2 ^ -1 = 1 / (2 ^ 1) = 1 / 2 = 0.5)\n\n[Vanilla Value: 100%]", new AcceptableValueRange<int>(-1000, 1000), []));

    scrapValueIsGiftBoxChance = Config.Bind("Gift Box - Scrap Item Value", "Chance for scrap item to inherit gift box value (%)", 15, new ConfigDescription("The likelihood (% chance) of the selected scrap item having the same scrap value as the gift box itself\n\n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 100), []));
    scrapValueAdditionChance = Config.Bind("Gift Box - Scrap Item Value", "Chance for scrap item to receive scrap value addition (%)", 100, new ConfigDescription("The likelihood (% chance) of the selected scrap item receiving an addition to its scrap value (if the scrap item inherits the gift box's scrap value, this addition will not be applied)\n\n[Vanilla Value: 100]", new AcceptableValueRange<int>(0, 100), []));
    scrapValueAdditionMin = Config.Bind("Gift Box - Scrap Item Value", "Minimum scrap item value addition", 30, new ConfigDescription("The minimum possible value of the multiplier applied to the selected scrap item's scrap value\n\n[Vanilla Value: 25]", new AcceptableValueRange<int>(-1000, 1000), []));
    scrapValueAdditionMax = Config.Bind("Gift Box - Scrap Item Value", "Maximum scrap item value addition", 60, new ConfigDescription("The maximum possible value of the multiplier applied to the selected scrap item's scrap value\n\n[Vanilla Value: 35]", new AcceptableValueRange<int>(-1000, 1000), []));
    scrapValueMultiplierChance = Config.Bind("Gift Box - Scrap Item Value", "Chance for scrap item to receive scrap value multiplier (%)", 35, new ConfigDescription("The likelihood (% chance) of the selected scrap item receiving a multiplier to its scrap value (if the scrap item inherits the gift box's scrap value, this multiplier will not be applied)\n\n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, 100), []));
    scrapValueMultiplierMin = Config.Bind("Gift Box - Scrap Item Value", "Minimum scrap item value multiplier (%)", 120, new ConfigDescription("The minimum possible value of the multiplier applied to the selected scrap item's scrap value\n\n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), []));
    scrapValueMultiplierMax = Config.Bind("Gift Box - Scrap Item Value", "Maximum scrap item value multiplier (%)", 150, new ConfigDescription("The maximum possible value of the multiplier applied to the selected scrap item's scrap value\n\n[Vanilla Value: 100%]", new AcceptableValueRange<int>(0, 1000), []));
    
    storeItemPriceMin = Config.Bind("Gift Box - Store Item Selection", "Minimum selectable store item price", 0, new ConfigDescription("The minimum store item price required for an item to be selected by the gift box\n\n[Vanilla Value: 0]", new AcceptableValueRange<int>(0, int.MaxValue), []));
    storeItemPriceMax = Config.Bind("Gift Box - Store Item Selection", "Maximum selectable store item price", int.MaxValue, new ConfigDescription("The maximum store item price required for an item to be selected by the gift box\n\n[Vanilla Value: infinity]", new AcceptableValueRange<int>(0, int.MaxValue), []));
    storeItemPriceInfluence = Config.Bind("Gift Box - Store Item Selection", "Store item price influence percentage (%)", -100, new ConfigDescription("How much influence a store item's price has over its selection weight. 0 = store item's price does not influence its selection weight, larger influence percentage = expensive store items are more likely than cheap store items, negative influence percentage = expensive store items are less likely than cheap store items\n\nEach selectable store item is given a selection weight equal to their store price raised to the power of this percentage (i.e. 100% = 100 / 100 = 1, so the exponent is 1). e.g. if this percentage is set to 200%, a store item with a price of 2 has a selection weight of 4 (2 ^ 200% = 2 ^ 2 = 4), which is four times the selection weight of a store item with a price of 1 and therefore a selection weight of 1 (1 ^ 200% = 1 ^ 2 = 1). If this value is negative, then the selection weights are inverted - e.g. -100% results in a store item with a price of 2 receiving a selection weight of 0.5 (2 ^ -100% = 2 ^ -1 = 1 / (2 ^ 1) = 1 / 2 = 0.5)\n\n[Vanilla Value: 0%]", new AcceptableValueRange<int>(-1000, 1000), []));

    ValidateConfigAndApplyPatches();

    Log($"[v{LCMPluginInfo.PLUGIN_VERSION}] Finished loading!");
  }

}