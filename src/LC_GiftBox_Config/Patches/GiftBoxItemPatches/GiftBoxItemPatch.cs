using BepInEx;
using UnityEngine;
using Steamworks;
using System;
using HarmonyLib;
using HarmonyLib.Tools;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using LC_GiftBox_Config.libs.ILStepper;
using LC_GiftBox_Config.libs.HarmonyXExtensions;
using LC_GiftBox_Config.libs.Probability;
using LC_GiftBox_Config.libs.UnityUtils;

using LogLevel = BepInEx.Logging.LogLevel;
using Object = UnityEngine.Object;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;
using Random = System.Random;

namespace LC_GiftBox_Config.Patches.GiftBoxItemPatches;

[HarmonyPatch(typeof(GiftBoxItem))]
internal static class GiftBoxItemPatch
{
    #region GiftBox Item Reference
        internal const int GIFTBOX_ITEM_ID = 152767;
        internal static Item? _GIFTBOX_ITEM = null;
        internal static Item? GIFTBOX_ITEM => _GIFTBOX_ITEM ??= StartOfRound.Instance?.allItemsList?.itemsList?.ToList()?.First(item => item.itemId == GIFTBOX_ITEM_ID);
    #endregion

    #region Easter Egg Resources
        internal static GameObject? _EGGSPLOSION = null;
        internal static GameObject EGGSPLOSION => _EGGSPLOSION ??= Resources.FindObjectsOfTypeAll<GameObject>().First(obj => obj.name == "EasterEggExplosionParticle");
        internal static AudioClip? _EGGPOP = null;
        internal static AudioClip? EGGPOP => _EGGPOP ??= Resources.FindObjectsOfTypeAll<AudioClip>().First(clip => clip.name == "EasterEggPop");
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
        internal static Terminal? _terminal = null;
        internal static Item[] _terminalBuyableItemsList = [];
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
                if (_terminal == null || _terminal.isActiveAndEnabled != true) 
                {
                    _terminal = Object.FindAnyObjectByType<Terminal>();
                    _terminalBuyableItemsList = [];
                }

                if (_terminalBuyableItemsList != _terminal?.buyableItemsList)
                {
                    _terminalBuyableItemsList = _terminal?.buyableItemsList ?? [];
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
                if (_currentLevelSpawnableScrap != RoundManager.Instance?.currentLevel?.spawnableScrap)
                {
                    _currentLevelSpawnableScrap = RoundManager.Instance?.currentLevel?.spawnableScrap ?? [];
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
                giftbox.objectInPresent = giftbox.objectInPresentItem.spawnPrefab;

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

    internal class GiftBoxItemSeeds : MonoBehaviour 
    {
        internal Random? EggsplosionRandom = null;
    }

    internal static void AddRandomSeeds(GiftBoxItem giftbox)
    {
        GiftBoxItemSeeds seeds = giftbox.gameObject.AddComponent<GiftBoxItemSeeds>();
        seeds.EggsplosionRandom = new Random((int)giftbox.targetFloorPosition.x + (int)giftbox.targetFloorPosition.y);
    }

    [HarmonyPatch(nameof(GiftBoxItem.Start))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Start(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
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

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // Start() destination: base.Start(); ** **
        stepper.GotoIL(code => code.Calls(type: typeof(GrabbableObject), name: "Start"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.Start] Call GrabbableObject.Start() not found!");
        stepper.GotoIndex(offset: 1);

        // Start() insertion: GiftBoxItemPatch.AddRandomSeeds(this);
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(AddRandomSeeds))
        ]);

        // Start() destination: if (base.IsServer ** **) 
        stepper.GotoIL(code => code.LoadsProperty(type: typeof(NetworkBehaviour), name: "IsServer"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.Start] Property NetworkBehaviour.IsServer not found!");
        stepper.GotoIndex(offset: 1, rightBoundOffset: 1);

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

    internal static void NestedGiftboxFun(GiftBoxItem giftbox, GrabbableObject? spawnedObj)
    {
        if (spawnedObj == null || spawnedObj.itemProperties.itemId != GIFTBOX_ITEM_ID) return;

        spawnedObj.transform.localScale = giftbox.transform.localScale * 0.925f;
        spawnedObj.name = "Nested " + giftbox.name;

        ScanNodeProperties giftboxNode = giftbox.GetComponentInChildren<ScanNodeProperties>();
        ScanNodeProperties spawnedObjNode = spawnedObj.GetComponentInChildren<ScanNodeProperties>();
        if (giftboxNode != null && spawnedObjNode != null)
        {
            spawnedObjNode.headerText = "Nested " + giftboxNode.headerText;
        } else Plugin.Log(LogLevel.Warning, "Failed to prepare nested giftbox scannode :(");
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
        GrabbableObject? spawnedObj = SpawnGiftItem(giftbox);

        // Apply nested giftbox fun if applicable
        NestedGiftboxFun(giftbox, spawnedObj);

        return true;
    }

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
    [HarmonyReversePatch]
    internal static GrabbableObject? SpawnGiftItem(GiftBoxItem giftbox)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase)
        {
            ILStepper stepper = new(methodIL, methodGenerator, methodBase);

            // OpenGiftBoxServerRpc() destination: ** ** GameObject gameObject = null;
            stepper.GotoIL(code => code.StoresLocal(index: 0), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Store Local 0 (gameObject) not found");
            stepper.GotoIL(ILPatterns.NextEmptyStack, offset: 1, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] OpCodes.Ldnull not found");

            // Remove everything before this point
            stepper.RemoveIL(startIndex: 0, endIndex: stepper.CurrentIndex, pinLabels: false, pinBlocks: false);

            // OpenGiftBoxServerRpc() destination: ** Debug.LogError("Error: There is no object in gift box!"); **
            stepper.GotoIL(code => code.LoadsString("Error: There is no object in gift box!"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] \"no object\" error message not found");
            
            // OpenGiftBoxServerRpc() deletion: ** Debug.LogError("Error: There is no object in gift box!"); **
            stepper.RemoveIL(
                endIndex: 1 + stepper.FindIL(ILPatterns.NextEmptyStack, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Call Debug.LogError(object) not found")
            );

            // OpenGiftBoxServerRpc() destination: component.SetScrapValue(num); ** **
            stepper.GotoIL(code => code.Calls(type: typeof(GrabbableObject), name: "SetScrapValue", parameters: [typeof(int)]), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] Call GrabbableObject.SetScrapValue(int) not found");
            stepper.GotoIndex(offset: 1, rightBoundOffset: 1);

            // Remove component.SetScrapValue(num); and copy into upcoming insertion
            List<CodeInstruction> SetScrapValueIL = stepper.RemoveIL(pinLabels: false, pinBlocks: false, 
                endIndex: stepper.FindIL(ILPatterns.NextEmptyStack, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.SpawnGiftItem] 1st Load Local 4 (component) not found")
            );

            // OpenGiftBoxServerRpc() insertion: ** if (num >= 0) { component.SetScrapValue(num); } **
            Label SkipScrapValueLabel = stepper.DeclareLabel();
            List<CodeInstruction> Insertion = stepper.InsertIL([
                CodeInstructionPolyfills.LoadLocal(index: 1), // num
                CodeInstructionPolyfills.LoadConstant(0), // num, 0
                new CodeInstruction(OpCodes.Blt, SkipScrapValueLabel), // if (num >= 0) {} else { undefined; }
                ..SetScrapValueIL, // if (num >= 0) { component.SetScrapValue(num); } else { undefined; }
                new CodeInstruction(OpCodes.Nop).WithLabels(SkipScrapValueLabel) // if (num >= 0) { component.SetScrapValue(num); }
            ]);

            // OpenGiftBoxServerRpc() destination: this.OpenGiftBoxClientRpc(gameObject.GetComponent<NetworkObject>(), num, vector); ** **
            stepper.GotoIL(code => code.Calls(type: typeof(GiftBoxItem), name: "OpenGiftBoxClientRpc"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc_DuplicateSoundsBugfix] Call GiftBoxItem.OpenGiftBoxClientRpc not found!");
            stepper.GotoIndex(offset: 1, rightBoundOffset: 1);

            // OpenGiftBoxServerRpc() insertion: ** return component; **
            stepper.InsertIL([
                CodeInstructionPolyfills.LoadLocal(index: 4), // component
                new CodeInstruction(OpCodes.Ret), // return component;
            ], pinLabels: false, pinBlocks: false);

            // OpenGiftBoxServerRpc() end insertion: ** return null; **
            stepper.GotoIndex(index: stepper.Instructions.Count, rightBoundOffset: 1);
            stepper.GotoIL(code => code.opcode == OpCodes.Ret, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc_DuplicateSoundsBugfix] Final return not found!");
            stepper.InsertIL(CodeInstructionPolyfills.LoadNull());

            return stepper.Instructions;
        }

        _ = Transpiler(null!, null!, null!);
        return null;
    }

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OpenGiftBoxServerRpc(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // OpenGiftBoxServerRpc() destination: { return; } ** ** GameObject gameObject = null;
        stepper.GotoIL(code => code.StoresLocal(index: 0), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc] Store Local 0 (gameObject) not found");
        stepper.GotoIL(code => code.opcode == OpCodes.Ret, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc] OpCodes.Ret not found");
        stepper.GotoIndex(offset: 1, rightBoundOffset: 1);

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

    internal static void Eggsplosion(GiftBoxItem giftbox)
    {
        // Empty Gift Box Eggsplosion Chance
        Random? EggsplosionRandom = giftbox.GetComponent<GiftBoxItemSeeds>()?.EggsplosionRandom;
        if (EggsplosionRandom == null) return;
        if (EggsplosionRandom.Next(0, 100) >= Plugin.giftboxEggsplosionChance.Value) return;

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

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxNoPresentClientRpc))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OpenGiftBoxNoPresentClientRpc(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // OpenGiftBoxNoPresentClientRpc() destination: RoundManager.Instance.PlayAudibleNoise(...); ** **
        stepper.GotoIL(code => code.Calls(type: typeof(RoundManager), name: "PlayAudibleNoise"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxNoPresentClientRpc] Call RoundManager.PlayAudibleNoise not found");
        stepper.GotoIndex(offset: 1, rightBoundOffset: 1);

        // OpenGiftBoxNoPresentClientRpc() insertion: ** GiftBoxItemPatch.Eggsplosion(this); **
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(Eggsplosion)) // GiftBoxItemPatch.Eggsplosion((this);
        ]);

        return stepper.Instructions;
    }

    [HarmonyPatch(nameof(GiftBoxItem.waitForGiftPresentToSpawnOnClient), MethodType.Enumerator)]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> waitForGiftPresentToSpawnOnClient(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // waitForGiftPresentToSpawnOnClient() destination: component.reachedFloorTarget = false; ** **
        stepper.GotoIL(code => code.StoresField(type: typeof(GrabbableObject), name: "reachedFloorTarget"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.waitForGiftPresentToSpawnOnClient] Store Field GrabbableObject.reachedFloorTarget not found");
        stepper.GotoIndex(offset: 1, rightBoundOffset: 1);

        // waitForGiftPresentToSpawnOnClient() insertion: ** GiftBoxItemPatch.NestedGiftboxFun(giftBoxItem, component); **
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadLocal(index: 1), // giftBoxItem
            CodeInstructionPolyfills.LoadLocal(index: 2), // component
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(NestedGiftboxFun)) // GiftBoxItemPatch.NestedGiftboxFun(giftBoxItem, component);
        ]);

        return stepper.Instructions;
    }

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OpenGiftBoxServerRpc_DuplicateSoundsBugfix(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxDupeSoundsBugFixDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // OpenGiftBoxServerRpc() destination: this.OpenGiftBoxClientRpc(gameObject.GetComponent<NetworkObject>(), num, vector); ** **
        stepper.GotoIL(code => code.Calls(type: typeof(GiftBoxItem), name: "OpenGiftBoxClientRpc"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.OpenGiftBoxServerRpc_DuplicateSoundsBugfix] Call GiftBoxItem.OpenGiftBoxClientRpc not found!");
        stepper.GotoIndex(offset: 1, rightBoundOffset: 1);

        // OpenGiftBoxServerRpc() insertion: ** return; **
        stepper.InsertIL(new CodeInstruction(OpCodes.Ret), pinLabels: false, pinBlocks: false);

        return stepper.Instructions;
    }

    [HarmonyPatch(nameof(GiftBoxItem.waitForGiftPresentToSpawnOnClient), MethodType.Enumerator)]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> waitForGiftPresentToSpawnOnClient_ToolBugfix(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
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