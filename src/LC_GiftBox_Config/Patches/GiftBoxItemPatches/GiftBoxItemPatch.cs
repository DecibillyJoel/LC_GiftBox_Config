using BepInEx;
using UnityEngine;
using Steamworks;
using System;
using HarmonyLib;
using HarmonyLib.Tools;
using LC_GiftBox_Config.libs.HarmonyXExtensions;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using LC_GiftBox_Config.libs.ILStepper;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using LC_GiftBox_Config.libs.Probability;

using Object = UnityEngine.Object;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;
using Random = System.Random;
using UnityEngine.TextCore.Text;

namespace LC_GiftBox_Config.Patches.GiftBoxItemPatches;

[HarmonyPatch(typeof(GiftBoxItem))]
internal static class GiftBoxItemPatch
{
    #region GiftBox Item Reference
        internal const int GIFTBOX_ITEM_ID = 152767;
        internal static Item _GIFTBOX_ITEM = null!;
        internal static Item GIFTBOX_ITEM {
            get { 
                return _GIFTBOX_ITEM ??= StartOfRound.Instance.allItemsList.itemsList.First(item => item.itemId == GIFTBOX_ITEM_ID);
            }
        }
    #endregion
    
    #region GiftBox Behaviors
        internal static List<int> giftboxBehaviors = [0, 0, 0, 0, 0];
        internal const int DO_NOTHING = 0;
        internal const int SPAWN_STORE_ITEM = 1;
        internal const int SPAWN_SCRAP = 2;
        internal const int SPAWN_GIFTBOX = 3;
        internal const int SPAWN_NOTHING = 4;
    #endregion
    
    #region Scrap Value Spawn Behaviors
        internal const int IGNORE_ITEM_VALUE = -1;
        internal const int USE_GIFTBOX_VALUE = -2;
    #endregion

    #region Filtered Store Items
        internal static Terminal _terminal = null!;
        internal static Item[] _terminalBuyableItemsList = null!;
        internal static List<Item> _filteredStoreItems = [];
        internal static List<double> _filteredStoreItemWeights = [];
        internal static List<Item> filteredStoreItems {
            set {
                if (value == null) {
                    _filteredStoreItems.Clear();
                } else {
                    _filteredStoreItems = value;
                }
            }
            get {
                if (_terminal == null || !_terminal.isActiveAndEnabled) 
                {
                    _terminal = Object.FindAnyObjectByType<Terminal>();
                    _terminalBuyableItemsList = null!;
                }

                if (_terminalBuyableItemsList != _terminal.buyableItemsList)
                {
                    _terminalBuyableItemsList = _terminal.buyableItemsList;
                    _filteredStoreItems.Clear();
                }

                if (_filteredStoreItems.Count == 0) 
                {
                    _filteredStoreItems = _terminalBuyableItemsList.Where(item
                        => item.creditsWorth >= Plugin.storeItemPriceMin.Value
                        && item.creditsWorth <= Plugin.storeItemPriceMax.Value
                    ).ToList();
                }

                return _filteredStoreItems;
            }
        }
        internal static List<double> filteredStoreItemWeights {
            set {
                if (value == null) {
                    _filteredStoreItemWeights.Clear();
                } else {
                    _filteredStoreItemWeights = value;
                }
            }
            get {
                _ = filteredStoreItems;

                if (_filteredStoreItemWeights.Count == 0) 
                {
                    _filteredStoreItemWeights = _filteredStoreItems.Select(item
                        => Math.Pow(item.creditsWorth, Plugin.scrapValueInfluence.Value / 100.0)
                    ).ToList();
                }

                return _filteredStoreItemWeights;
            }
        }
    #endregion

    #region Filtered Scrap Items
        internal static List<SpawnableItemWithRarity> _currentLevelSpawnableScrap = [];
        internal static List<SpawnableItemWithRarity> _filteredScrapItems = [];
        internal static List<double> _filteredScrapItemWeights = [];
        internal static List<SpawnableItemWithRarity> filteredScrapItems
        {
            set {
                if (value == null) {
                    _filteredScrapItems.Clear();
                } else {
                    _filteredScrapItems = value;
                }
            }
            get {
                if (_currentLevelSpawnableScrap != RoundManager.Instance.currentLevel.spawnableScrap)
                {
                    _currentLevelSpawnableScrap = RoundManager.Instance.currentLevel.spawnableScrap;
                    _filteredScrapItems.Clear();
                    _filteredScrapItemWeights.Clear();
                }

                if (_filteredScrapItems.Count == 0)
                {
                    _filteredScrapItems = _currentLevelSpawnableScrap.Where(item
                        => item.spawnableItem.itemId != GIFTBOX_ITEM_ID
                        && item.spawnableItem.minValue >= Plugin.scrapValueMin.Value
                        && item.spawnableItem.maxValue <= Plugin.scrapValueMax.Value
                        && item.rarity >= Plugin.scrapRarityMin.Value
                        && item.rarity <= Plugin.scrapRarityMax.Value
                    ).ToList();
                }

                return _filteredScrapItems;
            }
        }
        internal static List<double> filteredScrapItemWeights {
            set {
                if (value == null) {
                    _filteredScrapItemWeights.Clear();
                } else {
                    _filteredScrapItemWeights = value;
                }
            }
            get {
                _ = filteredScrapItems;

                if(_filteredScrapItemWeights.Count == 0) 
                {
                    _filteredScrapItemWeights = _filteredScrapItems.Select(item
                        => Math.Pow((item.spawnableItem.minValue + item.spawnableItem.maxValue) / 2.0, Plugin.scrapValueInfluence.Value / 100.0)
                        + Math.Pow(item.rarity, Plugin.scrapRarityInfluence.Value / 100.0)
                    ).ToList();
                }

                return _filteredScrapItemWeights;
            }
        }
    #endregion
    
    internal static bool InsertObjectInPresentAndScrapValue(GiftBoxItem giftbox, Random giftboxBehaviorSeed, Random valueBehaviorSeed)
    {
        int behaviorIndex = Probability.GetRandomWeightedIndex(giftboxBehaviors, giftboxBehaviorSeed);
        if (behaviorIndex == DO_NOTHING) return false; // Gift Box - Do Nothing

        giftbox.objectInPresentValue = IGNORE_ITEM_VALUE;
        
        switch (behaviorIndex) {
            case SPAWN_NOTHING: // Gift Box - Spawn Nothing
                break;
            case SPAWN_STORE_ITEM: // Gift Box - Spawn Store Item
                int itemIndex = Probability.GetRandomWeightedIndex(filteredStoreItemWeights, giftboxBehaviorSeed);
                if (itemIndex == -1) {
                    break;
                }

                giftbox.objectInPresentItem = filteredStoreItems[itemIndex];
                giftbox.objectInPresent = giftbox.objectInPresentItem.spawnPrefab;

                break;
            case SPAWN_GIFTBOX: // Gift Box - Spawn Gift Box
                giftbox.objectInPresentItem = giftbox.itemProperties;
                giftbox.objectInPresent = giftbox.itemProperties.spawnPrefab;

                goto case SPAWN_SCRAP;
            case SPAWN_SCRAP: // Gift Box - Spawn Scrap
                if (behaviorIndex == SPAWN_SCRAP) {
                    int scrapIndex = Probability.GetRandomWeightedIndex(filteredScrapItemWeights, giftboxBehaviorSeed);
                    if (scrapIndex == -1) {
                        break;
                    }

                    giftbox.objectInPresentItem = filteredScrapItems[scrapIndex].spawnableItem;
                    giftbox.objectInPresent = giftbox.objectInPresentItem.spawnPrefab;
                }

                giftbox.objectInPresentValue = valueBehaviorSeed.Next(giftbox.objectInPresentItem.minValue, giftbox.objectInPresentItem.maxValue);

                // Gift Box - Inherit Gift Box Value
                if (valueBehaviorSeed.Next(0, 100) < Plugin.scrapValueIsGiftBoxChance.Value) {
                    giftbox.objectInPresentValue = USE_GIFTBOX_VALUE;
                    break;
                }

                // Gift Box - Scrap Value Addition
                if (valueBehaviorSeed.Next(0, 100) < Plugin.scrapValueAdditionChance.Value) {
                    giftbox.objectInPresentValue += valueBehaviorSeed.Next(Plugin.scrapValueAdditionMin.Value, Plugin.scrapValueAdditionMax.Value + 1);
                }

                // Gift Box - Scrap Value Multiplier
                if (valueBehaviorSeed.Next(0, 100) < Plugin.scrapValueMultiplierChance.Value) {
                    giftbox.objectInPresentValue = (int)(giftbox.objectInPresentValue * (Plugin.scrapValueMultiplierMin.Value + (Plugin.scrapValueMultiplierMax.Value - Plugin.scrapValueMultiplierMin.Value) * valueBehaviorSeed.NextDouble()) / 100);
                }

                // Clamp the scaled value and mark the gift box as modified so OverrideOpenGiftBox() knows not to ignore this giftbox (this is dumb)
                giftbox.objectInPresentValue = -Math.Max(0, (int)(giftbox.objectInPresentValue * RoundManager.Instance.scrapValueMultiplier)) - 100;
                
                break;
            default:
                throw new Exception("[Patches.GiftBoxItemPatches.GiftBoxItemPatch.InsertObjectInPresentAndScrapValue] Giftbox Behavior selection failed! This should never happen!");
        }

        return true;
    }

    [HarmonyPatch(nameof(GiftBoxItem.Start))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Start(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        filteredStoreItems = null!;
        filteredScrapItems = null!;

        giftboxBehaviors[DO_NOTHING] = Plugin.doNothingChance.Value;
        giftboxBehaviors[SPAWN_STORE_ITEM] = Plugin.spawnStoreItemChance.Value;
        giftboxBehaviors[SPAWN_SCRAP] = Plugin.spawnScrapChance.Value;
        giftboxBehaviors[SPAWN_GIFTBOX] = Plugin.spawnGiftBoxChance.Value;
        giftboxBehaviors[SPAWN_NOTHING] = Plugin.spawnNothingChance.Value;

        ILStepper stepper = new(methodIL, methodGenerator);

        // Start() destination: if (base.IsServer ** **) 
        stepper.GotoIL(code => code.LoadsProperty(type: typeof(NetworkBehaviour), name: "IsServer"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.Start] Property NetworkBehaviour.IsServer not found!");
        stepper.GotoIndex(offset: 1);

        // Start() insertion: ** && !GiftBoxItemPatch.InsertObjectInPresentAndScrapValue(this, randomSeed, random) **
        stepper.InsertIL(codeRange: [
            CodeInstructionPolyfills.LoadArgument(0), // this
            CodeInstructionPolyfills.LoadLocal(0), // this, randomSeed
            CodeInstructionPolyfills.LoadLocal(1), // this, randomSeed, random
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(InsertObjectInPresentAndScrapValue)), // GiftBoxItemPatch.InsertObjectInPresentAndScrapValue(this, randomSeed, random)
            new CodeInstruction(OpCodes.Not), // !GiftBoxItemPatch.InsertObjectInPresentAndScrapValue(this, randomSeed, random)
            new CodeInstruction(OpCodes.And) // && !GiftBoxItemPatch.InsertObjectInPresentAndScrapValue(this, randomSeed, random)
        ]);
        
        return stepper.Instructions;
    }

    internal static bool OverrideOpenGiftBox(GiftBoxItem giftbox)
    {
        // this is still dumb
        if (giftbox.objectInPresentValue >= 0) 
        {
            return false;
        }

        switch (giftbox.objectInPresentValue) {
            case USE_GIFTBOX_VALUE:
                giftbox.objectInPresentValue = giftbox.scrapValue;
                break;
            case IGNORE_ITEM_VALUE:
                break;
            default:
                // Undo the extremely dumb giftbox value marking
                giftbox.objectInPresentValue = -(giftbox.objectInPresentValue + 100);
                break;
        }

        // Use reverse patched vanilla logic to spawn the item
        SpawnGiftItem(giftbox);

        return true;
    }

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
    [HarmonyReversePatch]
    internal static void SpawnGiftItem(GiftBoxItem giftbox)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator)
        {
            if (Plugin.giftboxMechanicsDisabled.Value)
            {
                return methodIL;
            }

            ILStepper stepper = new(methodIL, methodGenerator);

            // OpenGiftBoxServerRpc() destination: ** ** GameObject gameObject = null;
            stepper.GotoIL(code => code.StoresLocal(index: 0), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Store Local 0 (gameObject) not found");
            stepper.GotoIL(code => code.LoadsNull(), reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] OpCodes.Ldnull not found");

            // Remove everything before this point
            stepper.RemoveIL(startIndex: 0, endOffset: stepper.CurrentIndex, shiftCurrentIndex: true);

            // OpenGiftBoxServerRpc() destination: ** Debug.LogError("Error: There is no object in gift box!"); **
            stepper.GotoIL(code => code.LoadsString("Error: There is no object in gift box!"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] \"no object\" error message not found");
            
            // OpenGiftBoxServerRpc() deletion: ** Debug.LogError("Error: There is no object in gift box!"); **
            stepper.RemoveIL(
                endOffset: -stepper.CurrentIndex + stepper.FindIL(code => code.Calls(type: typeof(Debug), name: "LogError", parameters: [typeof(object)]), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Call Debug.LogError(object) not found")
            );

            // OpenGiftBoxServerRpc() destination: component.SetScrapValue(num); ** **
            stepper.GotoIL(code => code.Calls(type: typeof(GrabbableObject), name: "SetScrapValue", parameters: [typeof(int)]), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Call GrabbableObject.SetScrapValue(int) not found");
            stepper.GotoIndex(offset: 1);

            // Remove component.SetScrapValue(num); and copy into upcoming insertion
            List<CodeInstruction> SetScrapValueIL = stepper.RemoveIL(
                endOffset: -stepper.CurrentIndex + stepper.FindIL(code => code.LoadsLocal(index: 4), reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] 1st Load Local 4 (component) not found")
            );
            
            // OpenGiftBoxServerRpc() insertion: ** if (num >= 0) { component.SetScrapValue(num); } **
            Label SkipScrapValueLabel = stepper.DeclareLabel();
            stepper.InsertIL(([
                CodeInstructionPolyfills.LoadLocal(1), // num
                CodeInstructionPolyfills.LoadConstant(0), // num, 0
                new CodeInstruction(OpCodes.Blt, SkipScrapValueLabel), // if (num >= 0) {} else { undefined; }
                ..SetScrapValueIL, // if (num >= 0) { component.SetScrapValue(num); } else { undefined; }
                new CodeInstruction(OpCodes.Nop).WithLabels(SkipScrapValueLabel) // if (num >= 0) { component.SetScrapValue(num); }
            ]));

            return stepper.Instructions;
        }

        _ = Transpiler(null!, null!);
    }

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OpenGiftBoxServerRpc(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator);

        // OpenGiftBoxServerRpc() destination: { return; } ** ** GameObject gameObject = null;
        stepper.GotoIL(code => code.StoresLocal(index: 0), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Store Local 0 (gameObject) not found");
        stepper.GotoIL(code => code.opcode == OpCodes.Ret, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] OpCodes.Ret not found");
        stepper.GotoIndex(offset: 1);

        // OpenGiftBoxServerRpc() insertion: ** if (GiftBoxItemPatch.OverrideOpenGiftBox(this)) { return; } **
        Label SkipEarlyReturnLabel = stepper.DeclareLabel();
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(OverrideOpenGiftBox)), // GiftBoxItemPatch.OverrideOpenGiftBox(this)
            new CodeInstruction(OpCodes.Brfalse, SkipEarlyReturnLabel), // if (GiftBoxItemPatch.OverrideOpenGiftBox(this)) {} else { undefined; }
            new CodeInstruction(OpCodes.Ret), // if (GiftBoxItemPatch.OverrideOpenGiftBox(this)) { return; } else { undefined; }
            new CodeInstruction(OpCodes.Nop).WithLabels(SkipEarlyReturnLabel) // if (GiftBoxItemPatch.OverrideOpenGiftBox(this)) { return; }
        ]);

        return stepper.Instructions;
    }
}