using UnityEngine;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using StaticNetcodeLib;
using ILUtils;
using ILUtils.HarmonyXtensions;
using LCUtils;

using LogLevel = BepInEx.Logging.LogLevel;
using Object = UnityEngine.Object;
using OpCodes = System.Reflection.Emit.OpCodes;
using Random = System.Random;

namespace LC_GiftBox_Config.Patches.GiftBoxItemPatches;

[StaticNetcode]
[HarmonyPatch(typeof(GiftBoxItem))]
public static class GiftBoxItemPatch
{
    #region Easter Egg Resources
        public static GameObject? _EGGSPLOSION = null;
        public static GameObject EGGSPLOSION => _EGGSPLOSION ??= Resources.FindObjectsOfTypeAll<GameObject>().First(obj => obj.name == "EasterEggExplosionParticle");
        public static AudioClip? _EGGPOP = null;
        public static AudioClip? EGGPOP => _EGGPOP ??= Resources.FindObjectsOfTypeAll<AudioClip>().First(clip => clip.name == "EasterEggPop");
    #endregion
    
    #region GiftBox Behaviors
        public static List<int> giftboxBehaviors = [0, 0, 0, 0, 0];
        public const int DO_NOTHING = 0;
        public const int SPAWN_STORE_ITEM = 1;
        public const int SPAWN_SCRAP = 2;
        public const int SPAWN_GIFTBOX = 3;
        public const int SPAWN_NOTHING = 4;
    #endregion

    #region Nested GiftBox
        public static GiftBoxModdedParams? parentGiftboxParams = null;
    #endregion

    #region Store Items and Weights
        private static Dictionary<Item, double>? _storeItemsAndWeights;
        
        public static Dictionary<Item, double> GetStoreItemsAndWeights()
        {
            if (_storeItemsAndWeights != null) return _storeItemsAndWeights;

            _storeItemsAndWeights = [];

            Item[] buyableItemsList = HUDManager.Instance?.terminalScript?.buyableItemsList ?? [];
            CompatibleNoun[] compatibleNouns = HUDManager.Instance?.terminalScript?.terminalNodes?.allKeywords?.FirstOrDefault(keyword => keyword.name == "Buy")?.compatibleNouns ?? [];
            
            // Store all items in buyableItemsList that are not excluded via configs, and their weights
            buyableItemsList.Do(item => 
            {
                if (item == null) return;
                if (_storeItemsAndWeights.ContainsKey(item)) return;
                if (Plugin.perItemConfigs.TryGetValue(item, out var itemConfig) && itemConfig.blacklisted.Value) return;
                if (item.creditsWorth < Plugin.storeItemPriceMin.Value || item.creditsWorth > Plugin.storeItemPriceMax.Value) return;
                
                _storeItemsAndWeights.Add(item, Math.Pow(item.creditsWorth, Plugin.storeItemPriceInfluence.Value / 100.0));
            });

            // Remove unbuyable items
            if (Plugin.storeItemMustBeBuyable.Value) 
            {
                var buyableItems = compatibleNouns.Select(noun => buyableItemsList.ElementAtOrDefault(noun.result.buyItemIndex));
                _storeItemsAndWeights.Keys.Except(buyableItemsList).Do(unbuyableItem => _storeItemsAndWeights.Remove(unbuyableItem));
            }

            return _storeItemsAndWeights;
        }

        public static Dictionary<Item, double> GetStoreItemsAndWeightsWithConfigRolls(Random random)
        {
            return GetStoreItemsAndWeights().ToDictionary(
                pair => pair.Key, 
                pair => {
                    if (!Plugin.perItemConfigs.TryGetValue(pair.Key, out var itemConfig)) return pair.Value; 

                    double weight = pair.Value;

                    // [Per-Item Config] - Selection Weight Multiplier
                    if (random.Next(0, 100) < itemConfig.selectionWeightMultiplierChance.Value)
                        weight *= random.Next(itemConfig.selectionWeightMultiplierMin.Value, itemConfig.selectionWeightMultiplierMax.Value + 1) / 100.0;

                    // [Per-Item Config] - Selection Weight Addition
                    if (random.Next(0, 100) < itemConfig.selectionWeightAdditionChance.Value)
                        weight += random.Next(itemConfig.selectionWeightAdditionMin.Value, itemConfig.selectionWeightAdditionMax.Value + 1);

                    return weight;
                }
            );
        }
    #endregion

    // TODO: Dynamically extract spawnableScrap from RoundManagerPatch
    #region Scrap Items and Weights
        private static Dictionary<Item, double>? _scrapItemsAndWeights;
        
        public static Dictionary<Item, double> GetScrapItemsAndWeights()
        {
            if (_scrapItemsAndWeights != null) return _scrapItemsAndWeights;

            _scrapItemsAndWeights = [];
            double scrapValueMultiplier = RoundManager.Instance?.scrapValueMultiplier ?? 0.4;
            Dictionary<Item, double> scrapRarities = SpawnableScrapUtils.GetSpawnableScrapSafely().ToDictionary(item => item.spawnableItem, item => (double)item.rarity);

            // Store all scrap items that are not excluded via configs, and their weights
            ItemUtils.AllItems.Do(item => 
            {
                if (item == null) return;
                if (!item.isScrap) return;
                if (_scrapItemsAndWeights.ContainsKey(item)) return;
                if (Plugin.perItemConfigs.TryGetValue(item, out var itemConfig) && itemConfig.blacklisted.Value) return;
                if ((item.maxValue * scrapValueMultiplier < Plugin.scrapValueMin.Value) || (item.minValue * scrapValueMultiplier > Plugin.scrapValueMax.Value)) return;
                if (scrapRarities.GetValueOrDefault(item) < Plugin.scrapRarityMin.Value || scrapRarities.GetValueOrDefault(item) > Plugin.scrapRarityMax.Value) return;
                
                _scrapItemsAndWeights.Add(item, 
                    Math.Pow((item.minValue + item.maxValue) / 2.0 * scrapValueMultiplier, Plugin.scrapValueInfluence.Value / 100.0)
                    + Math.Pow(scrapRarities.GetValueOrDefault(item), Plugin.scrapRarityInfluence.Value / 100.0)
                );
            });

            return _scrapItemsAndWeights;
        }

        public static Dictionary<Item, double> GetScrapItemsAndWeightsWithConfigRolls(Random random)
        {
            return GetScrapItemsAndWeights().ToDictionary(
                pair => pair.Key, 
                pair => {
                    if (!Plugin.perItemConfigs.TryGetValue(pair.Key, out var itemConfig)) return pair.Value; 

                    double weight = pair.Value;

                    // [Per-Item Config] - Selection Weight Multiplier
                    if (random.Next(0, 100) < itemConfig.selectionWeightMultiplierChance.Value)
                        weight *= random.Next(itemConfig.selectionWeightMultiplierMin.Value, itemConfig.selectionWeightMultiplierMax.Value + 1) / 100.0;

                    // [Per-Item Config] - Selection Weight Addition
                    if (random.Next(0, 100) < itemConfig.selectionWeightAdditionChance.Value)
                        weight += random.Next(itemConfig.selectionWeightAdditionMin.Value, itemConfig.selectionWeightAdditionMax.Value + 1);

                    return weight;
                }
            );
        }
    #endregion

    // Update caches on selectablelevel change
    [HarmonyPatch] // at least one Harmony annotation makes Harmony not skip this patch class when calling PatchAll()
    public static class SelectableLevelUpdatedPatch
    {
        // here, inside the patch class, you can place the auxiliary patch methods
        // for example TargetMethod:

        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return [
                AccessTools.Method(type: typeof(RoundManager), name: nameof(RoundManager.GenerateNewLevelClientRpc)),
                AccessTools.Method(type: typeof(RoundManager), name: nameof(RoundManager.LoadNewLevel)),
                AccessTools.Method(type: typeof(StartOfRound), name: nameof(StartOfRound.ChangeLevel))
            ];
        }

        [HarmonyPriority(priority: int.MaxValue)]
        [HarmonyPrefix]
        public static void SelectableLevelUpdate_Prefix()
        {
            Plugin.Log(LogLevel.Debug, "Clearing selectable level caches");
            
            // Clear caches on selectable level change
            _storeItemsAndWeights = null;
            _scrapItemsAndWeights = null;
        }
    }

    public class GiftBoxModdedParams
    {
        [ES3Serializable]
        public bool CanEggsplode = false;

        [ES3Serializable]
        public bool ScrapHasGiftBoxValue = false;

        [ES3Serializable]
        public int ScrapValue = 0;

        [ES3Serializable]
        public int NestedScrapId = -1;

        [ES3Serializable]
        public int SpawnCount = 1;
    }

    // TODO: SetScrapValue() before changing totalScrapValue in waitForGiftPresentToSpawnOnClient()
    public class GiftBoxModdedBehavior : MonoBehaviour 
    {
        public GiftBoxModdedParams Params;
        public GiftBoxModdedBehavior()
        {
            Params = new GiftBoxModdedParams();
        }
        public GiftBoxModdedBehavior(GiftBoxModdedParams _Params)
        {
            Params = _Params;
        }

        public static implicit operator GiftBoxModdedParams?(GiftBoxModdedBehavior? component) => component?.Params;
    }

    [HarmonyPatch(nameof(GiftBoxItem.GetItemDataToSave))]
    [HarmonyPriority(priority: int.MinValue)]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> GetItemDataToSave(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // GetItemDataToSave() destination: return 0;
        stepper.GotoIndex(index: stepper.Instructions.Count);
        stepper.GotoIL(code => code.LoadsConstant(0), reverse: true);

        // GetItemDataToSave() overwrite: return int.MaxValue;
        stepper.OverwriteIL(CodeInstructionPolyfills.LoadConstant(int.MaxValue));

        return stepper.Instructions;
    }

    public static bool OverrideLoadItemSaveData(GiftBoxItem giftbox, int saveData)
    {
        GiftBoxModdedParams? moddedParams = giftbox.GetComponent<GiftBoxModdedBehavior>();
        if (moddedParams == null) return false;

        giftbox.objectInPresentItem = saveData == int.MaxValue ? null : StartOfRound.Instance.allItemsList.itemsList.ElementAtOrDefault(saveData);
        giftbox.objectInPresent = giftbox.objectInPresentItem?.spawnPrefab;
        giftbox.objectInPresentValue = moddedParams.ScrapValue;

        giftbox.loadedItemFromSave = true;
        return true;
    }

    [HarmonyPatch(nameof(GiftBoxItem.LoadItemSaveData))]
    [HarmonyPriority(priority: int.MinValue)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> LoadItemSaveData(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // LoadItemSaveData() destination: base.LoadItemSaveData(saveData); ** **
        stepper.GotoIL(code => code.Calls(type: typeof(GrabbableObject), name: "LoadItemSaveData"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.Start] Call GrabbableObject.LoadItemSaveData() not found!");
        stepper.GotoIndex(offset: 1);

        // LoadItemSaveData() insertion: ** if (GiftBoxItemPatch.OverrideLoadItemSaveData(this, saveData)) { return; } **
        Label EarlyReturnLabel = stepper.DeclareLabel();
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadArgument(index: 1), // this, saveData
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(OverrideLoadItemSaveData)), // GiftBoxItemPatch.OverrideLoadItemSaveData(this, saveData)
            new CodeInstruction(OpCodes.Brtrue, EarlyReturnLabel), // if (GiftBoxItemPatch.OverrideLoadItemSaveData(this, saveData)) { undefined; }
        ]);
        // LoadItemSaveData() destination: this.loadedItemFromSave = true; ** **
        stepper.GotoIL(code => code.StoresField(type: typeof(GiftBoxItem), name: "loadedItemFromSave"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.Start] Store field GrabbableObject.loadedItemFromSave not found!");
        stepper.GotoIndex(offset: 1);
        stepper.InsertIL(new CodeInstruction(OpCodes.Nop).WithLabels(EarlyReturnLabel)); // if (GiftBoxItemPatch.OverrideLoadItemSaveData(this, saveData)) { return; }

        return stepper.Instructions;
    }
    
    public static bool InitGiftboxModdedBehavior(GiftBoxItem giftbox, ref Random giftboxBehaviorSeed, ref Random valueSeed)
    {
        // Determine first modded behavior to attempt
        BlendedRandom giftboxBehaviorSeed2 = new(giftboxBehaviorSeed, null, Plugin.positionRNGInfluence.Value / 100.0);
        int behaviorIndex = Probability.GetRandomWeightedIndex(giftboxBehaviors, giftboxBehaviorSeed2);
        if (behaviorIndex == DO_NOTHING) return false; // Gift Box - Do Nothing

        // Overwrite giftbox random seeds with blended randomseeds
        giftboxBehaviorSeed = giftboxBehaviorSeed2;
        valueSeed = new BlendedRandom(valueSeed, null, Plugin.positionRNGInfluence.Value / 100.0);

        // Create modded behavior component
        GiftBoxModdedParams moddedParams = giftbox.gameObject.AddComponent<GiftBoxModdedBehavior>().Params;
        moddedParams.CanEggsplode = giftboxBehaviorSeed.Next(0, 100) < Plugin.giftboxEggsplosionChance.Value;

        // Remove DO_NOTHING from the list of behaviors to try
        List<int> giftboxRemainingBehaviors = giftboxBehaviors.ToList();
        giftboxRemainingBehaviors[DO_NOTHING] = 0;
        
        // Keep trying until a behavior succeeds
        while (giftboxRemainingBehaviors.Sum() > 0) {
            switch (behaviorIndex) {
                case SPAWN_NOTHING: // Gift Box - Spawn Nothing
                    return true; 
                case SPAWN_STORE_ITEM: // Gift Box - Spawn Store Item 
                    var storeItemsAndWeights = GetStoreItemsAndWeightsWithConfigRolls(giftboxBehaviorSeed);

                    int storeItemIndex = Probability.GetRandomWeightedIndex(storeItemsAndWeights.Values.ToList(), giftboxBehaviorSeed);
                    Item? storeItem = storeItemsAndWeights.Keys.ElementAtOrDefault(storeItemIndex);
                    
                    if (storeItem == null) break;

                    giftbox.objectInPresentItem = storeItem;
                    giftbox.objectInPresent = storeItem.spawnPrefab;

                    // Store Item Extra Spawn Chances \\
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.storeItemSpawn1ExtrasChance.Value)
                        moddedParams.SpawnCount += 1;
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.storeItemSpawn2ExtrasChance.Value)
                        moddedParams.SpawnCount += 2;

                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.storeItemSpawn4ExtrasChance.Value)
                        moddedParams.SpawnCount += 4;

                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.storeItemSpawn8ExtrasChance.Value)
                        moddedParams.SpawnCount += 8;

                    // Per-Item Config Extra Spawn Chances \\
                    
                    var storeItemConfig = Plugin.perItemConfigs.GetValueOrDefault(storeItem);
                    
                    if (storeItemConfig.ignoreGlobalSpawnExtraChance.Value)
                        moddedParams.SpawnCount = 1;

                    if (giftboxBehaviorSeed.Next(0, 100) < storeItemConfig.spawn1ExtraChance.Value)
                        moddedParams.SpawnCount += 1;
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < storeItemConfig.spawn2ExtraChance.Value)
                        moddedParams.SpawnCount += 2;
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < storeItemConfig.spawn4ExtraChance.Value)
                        moddedParams.SpawnCount += 4;
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < storeItemConfig.spawn8ExtraChance.Value)
                        moddedParams.SpawnCount += 8;

                    return true;
                case SPAWN_GIFTBOX: // Gift Box - Spawn Gift Box
                    if (parentGiftboxParams != null) // Copy parent's nested scrap id
                    {
                        moddedParams.NestedScrapId = parentGiftboxParams.NestedScrapId;
                    } 
                    else // Randomly select a nested scrap id
                    {
                        var scrapItemsAndWeights = GetScrapItemsAndWeightsWithConfigRolls(giftboxBehaviorSeed);

                        int nestedScrapIndex = Probability.GetRandomWeightedIndex(scrapItemsAndWeights.Values.ToList(), giftboxBehaviorSeed);
                        Item? nestedScrapItem = scrapItemsAndWeights.Keys.ElementAtOrDefault(nestedScrapIndex);

                        if (nestedScrapItem != null)
                            moddedParams.NestedScrapId = StartOfRound.Instance.allItemsList.itemsList.FindIndex(item => item == nestedScrapItem);
                    }

                    // Contained Gift Box Extra Spawn Chances
                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.giftboxRecursionSpawn1ExtrasChance.Value)
                        moddedParams.SpawnCount += 1;
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.giftboxRecursionSpawn2ExtrasChance.Value)
                        moddedParams.SpawnCount += 2;

                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.giftboxRecursionSpawn4ExtrasChance.Value)
                        moddedParams.SpawnCount += 4;

                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.giftboxRecursionSpawn8ExtrasChance.Value)
                        moddedParams.SpawnCount += 8;

                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.giftboxRecursionSpawn16ExtrasChance.Value)
                        moddedParams.SpawnCount += 16;

                    goto case SPAWN_SCRAP;
                case SPAWN_SCRAP: // Gift Box - Spawn Scrap
                    Item? scrap;
                    if (behaviorIndex == SPAWN_GIFTBOX) // Spawn gift box
                    {
                        scrap = giftbox.itemProperties;
                    }
                    else if (parentGiftboxParams?.NestedScrapId != null) // Spawn parent's nested scrap item
                    {
                        scrap = StartOfRound.Instance.allItemsList.itemsList.ElementAtOrDefault(parentGiftboxParams.NestedScrapId);
                    } 
                    else // Spawn random scrap
                    {
                        var scrapItemsAndWeights = GetScrapItemsAndWeightsWithConfigRolls(giftboxBehaviorSeed);

                        int scrapIndex = parentGiftboxParams?.NestedScrapId ?? Probability.GetRandomWeightedIndex(scrapItemsAndWeights.Values.ToList(), giftboxBehaviorSeed);
                        scrap = scrapItemsAndWeights.Keys.ElementAtOrDefault(scrapIndex);
                    } 
                        
                    if (scrap == null) break;

                    giftbox.objectInPresentItem = scrap;
                    giftbox.objectInPresent = scrap.spawnPrefab;

                    // Max value is unreachable, but this is vanilla behavior soooooo...
                    giftbox.objectInPresentValue = valueSeed.Next(giftbox.objectInPresentItem.minValue, giftbox.objectInPresentItem.maxValue);

                    // Apply RoundManager scrap value multiplier
                    giftbox.objectInPresentValue = (int)(giftbox.objectInPresentValue * RoundManager.Instance.scrapValueMultiplier);

                    // Gift Box - Scrap Value Multiplier
                    if (valueSeed.Next(0, 100) < Plugin.scrapValueMultiplierChance.Value)
                        giftbox.objectInPresentValue = valueSeed.Next((giftbox.objectInPresentValue * Plugin.scrapValueMultiplierMin.Value + 50) / 100, (giftbox.objectInPresentValue * Plugin.scrapValueMultiplierMax.Value + 50) / 100 + 1);

                    // Gift Box - Scrap Value Addition
                    if (valueSeed.Next(0, 100) < Plugin.scrapValueAdditionChance.Value)
                        giftbox.objectInPresentValue += valueSeed.Next(Plugin.scrapValueAdditionMin.Value, Plugin.scrapValueAdditionMax.Value + 1);

                    // Gift Box - Inherit Gift Box Value (if host disables mod behaviors mid-round, above values will be used instead)
                    if (valueSeed.Next(0, 100) < Plugin.scrapValueIsGiftBoxChance.Value)
                        moddedParams.ScrapHasGiftBoxValue = true;
                    
                    moddedParams.ScrapValue = giftbox.objectInPresentValue;

                    // Store Item Extra Spawn Chances \\
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.scrapSpawn1ExtrasChance.Value)
                        moddedParams.SpawnCount += 1;
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.scrapSpawn2ExtrasChance.Value)
                        moddedParams.SpawnCount += 2;

                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.scrapSpawn4ExtrasChance.Value)
                        moddedParams.SpawnCount += 4;

                    if (giftboxBehaviorSeed.Next(0, 100) < Plugin.scrapSpawn8ExtrasChance.Value)
                        moddedParams.SpawnCount += 8;

                    // Per-Item Config Extra Spawn Chances \\
                    
                    var scrapConfig = Plugin.perItemConfigs.GetValueOrDefault(scrap);

                    if (scrapConfig.ignoreGlobalSpawnExtraChance.Value)
                        moddedParams.SpawnCount = 1;

                    if (giftboxBehaviorSeed.Next(0, 100) < scrapConfig.spawn1ExtraChance.Value)
                        moddedParams.SpawnCount += 1;
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < scrapConfig.spawn2ExtraChance.Value)
                        moddedParams.SpawnCount += 2;
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < scrapConfig.spawn4ExtraChance.Value)
                        moddedParams.SpawnCount += 4;
                    
                    if (giftboxBehaviorSeed.Next(0, 100) < scrapConfig.spawn8ExtraChance.Value)
                        moddedParams.SpawnCount += 8;

                    return true;
                default:
                    throw new Exception("[Patches.GiftBoxItemPatches.GiftBoxItemPatch.InitGiftboxModdedBehavior] Giftbox Behavior selection failed due to invalid index! This should never happen!");
            }

            // Remove the behavior we just tried from the list of behaviors to try
            giftboxRemainingBehaviors[behaviorIndex] = 0;

            // Determine next modded behavior to attempt
            behaviorIndex = Probability.GetRandomWeightedIndex(giftboxRemainingBehaviors, giftboxBehaviorSeed);
        }

        return true;
    }

    [HarmonyPatch(nameof(GiftBoxItem.Start))]
    [HarmonyPriority(priority: int.MinValue)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Start(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        _storeItemsAndWeights = null;
        _scrapItemsAndWeights = null;

        giftboxBehaviors[DO_NOTHING] = Plugin.doNothingChance.Value;
        giftboxBehaviors[SPAWN_STORE_ITEM] = Plugin.spawnStoreItemChance.Value;
        giftboxBehaviors[SPAWN_SCRAP] = Plugin.spawnScrapChance.Value;
        giftboxBehaviors[SPAWN_GIFTBOX] = Plugin.giftboxRecursionChance.Value;
        giftboxBehaviors[SPAWN_NOTHING] = Plugin.spawnNothingChance.Value;

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // Start() destination: if (base.IsServer ** **) 
        stepper.GotoIL(code => code.LoadsProperty(type: typeof(NetworkBehaviour), name: "IsServer"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.Start] Property NetworkBehaviour.IsServer not found!");
        stepper.GotoIndex(offset: 1);

        // Start() insertion: ** && !GiftBoxItemPatch.InitGiftboxModdedBehavior(this, ref randomSeed, ref random) **
        stepper.InsertIL(codeRange: [
            CodeInstructionPolyfills.LoadArgument(0), // this
            CodeInstructionPolyfills.LoadLocal(0, useAddress: true), // this, ref randomSeed
            CodeInstructionPolyfills.LoadLocal(1, useAddress: true), // this, ref randomSeed, ref random
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(InitGiftboxModdedBehavior)), // GiftBoxItemPatch.InitGiftboxModdedBehavior(this, ref randomSeed, ref random)
            new CodeInstruction(OpCodes.Not), // !GiftBoxItemPatch.InitGiftboxModdedBehavior(this, ref randomSeed, ref random)
            new CodeInstruction(OpCodes.And) // && !GiftBoxItemPatch.InitGiftboxModdedBehavior(this, ref randomSeed, ref random)
        ]);
        
        return stepper.Instructions;
    }

    public static void NestedGiftboxFun(GiftBoxItem giftbox, GrabbableObject? spawnedObj)
    {
        // Check if spawned object is nested giftbox
        if (spawnedObj == null || spawnedObj.itemProperties.LooselyEquals(Plugin.GIFTBOX_ITEM)) return;

        // Shrink and rename nested giftbox
        spawnedObj.transform.localScale = giftbox.transform.localScale * 0.925f;
        spawnedObj.name = "Nested " + giftbox.name;

        // Modify nested giftbox scan node
        ScanNodeProperties giftboxNode = giftbox.GetComponentInChildren<ScanNodeProperties>();
        ScanNodeProperties spawnedObjNode = spawnedObj.GetComponentInChildren<ScanNodeProperties>();
        if (giftboxNode != null && spawnedObjNode != null)
        {
            spawnedObjNode.headerText = "Nested " + giftboxNode.headerText;
        } else Plugin.Log(LogLevel.Warning, "Failed to prepare nested giftbox scan node :(");
    }

    public static bool OverrideOpenGiftBox(GiftBoxItem giftbox)
    {
        // this is no longer dumb
        GiftBoxModdedParams? moddedParams = giftbox.GetComponent<GiftBoxModdedBehavior>();
        if (moddedParams == null) return false;

        // Gift Box - Inherit Gift Box Scrap Value
        if (moddedParams.ScrapHasGiftBoxValue) giftbox.objectInPresentValue = giftbox.scrapValue;

        do {
            // Use reverse patched vanilla logic to spawn the item
            parentGiftboxParams = moddedParams;
            GrabbableObject? spawnedObj = SpawnGiftItem(giftbox);
            parentGiftboxParams = null;

            // Apply nested giftbox fun if applicable
            NestedGiftboxFun(giftbox, spawnedObj);

            // Empty Gift Box Eggsplosion Chance
            if (spawnedObj == null && moddedParams.CanEggsplode)
                EggsplosionClientRpc(giftbox.gameObject);
        } while (--moddedParams.SpawnCount > 0);

        return true;
    }

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
    [HarmonyReversePatch]
    public static GrabbableObject? SpawnGiftItem(GiftBoxItem giftbox)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase)
        {
            ILStepper stepper = new(methodIL, methodGenerator, methodBase);

            // OpenGiftBoxServerRpc() destination: ** ** GameObject gameObject = null;
            stepper.GotoIL(code => code.StoresLocal(index: 0), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Store Local 0 (gameObject) not found");
            stepper.GotoIL(ILPatterns.NextEmptyStack(startSize: 0), offset: 1, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] OpCodes.Ldnull not found");

            // Remove everything before this point
            stepper.RemoveIL(startIndex: 0, endIndex: stepper.CurrentIndex, pinLabels: false, pinBlocks: false);

            // OpenGiftBoxServerRpc() destination: ** Debug.LogError("Error: There is no object in gift box!"); **
            stepper.GotoIL(code => code.LoadsString("Error: There is no object in gift box!"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] \"no object\" error message not found");
            
            // OpenGiftBoxServerRpc() deletion: ** Debug.LogError("Error: There is no object in gift box!"); **
            stepper.RemoveIL(
                endIndex: 1 + stepper.FindIL(ILPatterns.NextEmptyStack(startSize: 0), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Call Debug.LogError(object) not found")
            );

            // OpenGiftBoxServerRpc() destination: component.SetScrapValue(num); ** **
            stepper.GotoIL(code => code.Calls(type: typeof(GrabbableObject), name: "SetScrapValue", parameters: [typeof(int)]), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Call GrabbableObject.SetScrapValue(int) not found");
            stepper.GotoIndex(offset: 1);

            // Remove component.SetScrapValue(num); and copy into upcoming insertion
            List<CodeInstruction> SetScrapValueIL = stepper.RemoveIL(pinLabels: false, pinBlocks: false, 
                endIndex: stepper.FindIL(ILPatterns.NextEmptyStack(startSize: 0), reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] 1st Load Local 4 (component) not found")
            );

            // OpenGiftBoxServerRpc() insertion: ** if (component.itemProperties.isScrap) { component.SetScrapValue(num); } **
            Label SkipScrapValueLabel = stepper.DeclareLabel();
            List<CodeInstruction> Insertion = stepper.InsertIL([
                CodeInstructionPolyfills.LoadLocal(index: 4), // component
                CodeInstructionPolyfills.LoadField(type: typeof(GrabbableObject), name: "itemProperties"), // component.itemProperties
                CodeInstructionPolyfills.LoadField(type: typeof(Item), name: "isScrap"), // component.itemProperties.isScrap
                new CodeInstruction(OpCodes.Brfalse, SkipScrapValueLabel), // if (component.itemProperties.isScrap) {} else { undefined; }
                ..SetScrapValueIL, // if (component.itemProperties.isScrap) { component.SetScrapValue(num); } else { undefined; }
                new CodeInstruction(OpCodes.Nop).WithLabels(SkipScrapValueLabel) // if (component.itemProperties.isScrap) { component.SetScrapValue(num); }
            ]);

            // OpenGiftBoxServerRpc() destination: this.OpenGiftBoxClientRpc(gameObject.GetComponent<NetworkObject>(), num, vector); ** **
            stepper.GotoIL(code => code.Calls(type: typeof(GiftBoxItem), name: "OpenGiftBoxClientRpc"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc_DuplicateSoundsBugfix] Call GiftBoxItem.OpenGiftBoxClientRpc not found!");
            stepper.GotoIndex(offset: 1);

            // OpenGiftBoxServerRpc() insertion: ** return component; **
            stepper.InsertIL([
                CodeInstructionPolyfills.LoadLocal(index: 4), // component
                new CodeInstruction(OpCodes.Ret), // return component;
            ], pinLabels: false, pinBlocks: false);

            // OpenGiftBoxServerRpc() end insertion: ** return null; **
            stepper.GotoIndex(index: stepper.Instructions.Count);
            stepper.GotoIL(code => code.opcode == OpCodes.Ret, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc_DuplicateSoundsBugfix] Final return not found!");
            stepper.InsertIL(CodeInstructionPolyfills.LoadNull());

            return stepper.Instructions;
        }

        _ = Transpiler(null!, null!, null!);
        return null;
    }

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
    [HarmonyPriority(priority: int.MinValue)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> OpenGiftBoxServerRpc(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // OpenGiftBoxServerRpc() destination: ** ** GameObject gameObject = null;
        stepper.GotoIL(code => code.StoresLocal(index: 0), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc] Store Local 0 (gameObject) not found");
        stepper.GotoIL(ILPatterns.NextEmptyStack(startSize: 0), offset: 1, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc] Beginning of Store Local 0 (gameObject) not found");

        // OpenGiftBoxServerRpc() insertion: ** if (GiftBoxItemPatch.OverrideOpenGiftBox(this)) { return; } **
        Label EarlyReturnLabel = stepper.DeclareLabel();
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(OverrideOpenGiftBox)), // GiftBoxItemPatch.OverrideOpenGiftBox(this)
            new CodeInstruction(OpCodes.Brtrue, EarlyReturnLabel), // if (GiftBoxItemPatch.OverrideOpenGiftBox(this)) { undefined; }
        ]);
        // OpenGiftBoxServerRpc() destination: this.OpenGiftBoxNoPresentClientRpc(); ** **
        stepper.GotoIL(code => code.Calls(type: typeof(GiftBoxItem), name: "OpenGiftBoxNoPresentClientRpc"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc] Call GiftBoxItem.OpenGiftBoxNoPresentClientRpc not found!");
        stepper.GotoIndex(offset: 1);
        stepper.InsertIL(new CodeInstruction(OpCodes.Nop).WithLabels(EarlyReturnLabel)); // if (GiftBoxItemPatch.OverrideOpenGiftBox(this)) { return; })

        return stepper.Instructions;
    }

    [ClientRpc]
    public static void EggsplosionClientRpc(NetworkObjectReference giftboxNGO)
    {
        GiftBoxItem? giftbox = ((GameObject)giftboxNGO)?.GetComponent<GiftBoxItem>();
        if (giftbox == null) return;

        if (EGGSPLOSION != null)
        {
            Transform parent = giftbox.isInElevator ? StartOfRound.Instance.elevatorTransform : RoundManager.Instance.mapPropsContainer.transform;
            Object.Instantiate(EGGSPLOSION, giftbox.transform.position, Quaternion.identity, parent);
        } 
        else Plugin.Log(LogLevel.Warning, "EGGSPLOSION VFX not found!");

        if (EGGPOP != null) 
        {
            giftbox.presentAudio.PlayOneShot(EGGPOP, volumeScale: 0.67f);
            WalkieTalkie.TransmitOneShotAudio(giftbox.presentAudio, EGGPOP, vol: 0.67f);
            RoundManager.Instance.PlayAudibleNoise(giftbox.presentAudio.transform.position, noiseRange: 15f, noiseLoudness: 0.67f, timesPlayedInSameSpot: 1, noiseIsInsideClosedShip: giftbox.isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
        }
        else Plugin.Log(LogLevel.Warning, "EGGSPLOSION SFX not found!");
    }

    [HarmonyPatch(nameof(GiftBoxItem.waitForGiftPresentToSpawnOnClient), MethodType.Enumerator)]
    [HarmonyPriority(priority: int.MinValue)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> waitForGiftPresentToSpawnOnClient(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // waitForGiftPresentToSpawnOnClient() destination: component.reachedFloorTarget = false; ** **
        stepper.GotoIL(code => code.StoresField(type: typeof(GrabbableObject), name: "reachedFloorTarget"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.waitForGiftPresentToSpawnOnClient] Store Field GrabbableObject.reachedFloorTarget not found");
        stepper.GotoIndex(offset: 1);

        // waitForGiftPresentToSpawnOnClient() insertion: ** GiftBoxItemPatch.NestedGiftboxFun(giftBoxItem, component); **
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadLocal(index: 1), // giftBoxItem
            CodeInstructionPolyfills.LoadLocal(index: 2), // component
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(NestedGiftboxFun)) // GiftBoxItemPatch.NestedGiftboxFun(giftBoxItem, component);
        ]);

        return stepper.Instructions;
    }

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
    [HarmonyPriority(priority: int.MinValue)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> OpenGiftBoxServerRpc_DuplicateSoundsBugfix(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxDupeSoundsBugFixDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // OpenGiftBoxServerRpc() destination: this.OpenGiftBoxClientRpc(gameObject.GetComponent<NetworkObject>(), num, vector); ** **
        stepper.GotoIL(code => code.Calls(type: typeof(GiftBoxItem), name: "OpenGiftBoxClientRpc"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc_DuplicateSoundsBugfix] Call GiftBoxItem.OpenGiftBoxClientRpc not found!");
        stepper.GotoIndex(offset: 1);

        // OpenGiftBoxServerRpc() insertion: ** } else { **
        Label SkipNoPresentRpcLabel = stepper.DeclareLabel();
        stepper.InsertIL(new CodeInstruction(OpCodes.Br, SkipNoPresentRpcLabel), pinLabels: false, pinBlocks: false);
        stepper.GotoIL(code => code.Calls(type: typeof(GiftBoxItem), name: "OpenGiftBoxNoPresentClientRpc"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc_DuplicateSoundsBugfix] Call GiftBoxItem.OpenGiftBoxNoPresentClientRpc not found!");
        stepper.GotoIndex(offset: 1);
        stepper.InsertIL(new CodeInstruction(OpCodes.Nop).WithLabels(SkipNoPresentRpcLabel));

        return stepper.Instructions;
    }

    [HarmonyPatch(nameof(GiftBoxItem.waitForGiftPresentToSpawnOnClient), MethodType.Enumerator)]
    [HarmonyPriority(priority: int.MinValue)]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> waitForGiftPresentToSpawnOnClient_ToolBugfix(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxToolScrapValueBugfixDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // waitForGiftPresentToSpawnOnClient_ToolBugfix() destination: RoundManager.Instance.totalScrapValueInLevel -= this.scrapValue; ** **
        stepper.GotoIL(code => code.StoresField(type: typeof(RoundManager), name: "totalScrapValueInLevel"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.waitForGiftPresentToSpawnOnClient_ToolBugfix] Store Field RoundManager.totalScrapValueInLevel not found");
        stepper.GotoIndex(offset: 1);

        // waitForGiftPresentToSpawnOnClient_ToolBugfix() insertion: ** if (component.itemProperties.isScrap) { doScrapThings() } **
        Label SkipScrapValueLabel = stepper.DeclareLabel();
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadLocal(index: 2), // component
            CodeInstructionPolyfills.LoadField(type: typeof(GrabbableObject), name: "itemProperties"), // component.itemProperties
            CodeInstructionPolyfills.LoadField(type: typeof(Item), name: "isScrap"), // component.itemProperties.isScrap
            new CodeInstruction(OpCodes.Brfalse, SkipScrapValueLabel), // if (component.itemProperties.isScrap) {} else { undefined; }
            .. stepper.RemoveIL(pinLabels: false, pinBlocks: false,
                endIndex: 1 + stepper.FindIL(code => code.Calls(type: typeof(GrabbableObject), name: "SetScrapValue"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.waitForGiftPresentToSpawnOnClient_ToolBugfix] Call GrabbableObject.SetScrapValue not found")
            ),
            new CodeInstruction(OpCodes.Nop).WithLabels(SkipScrapValueLabel) // if (component.itemProperties.isScrap) { doScrapThings(); }
        ]);

        return stepper.Instructions;
    }
}