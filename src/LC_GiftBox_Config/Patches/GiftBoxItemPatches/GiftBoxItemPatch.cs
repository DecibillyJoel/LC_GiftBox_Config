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
    #region GiftBox Item Reference
        public const int GIFTBOX_ITEM_ID = 152767;
        public static Item? _GIFTBOX_ITEM = null;
        public static Item GIFTBOX_ITEM => _GIFTBOX_ITEM ??= StartOfRound.Instance.allItemsList.itemsList.ToList().First(item => item.itemId == GIFTBOX_ITEM_ID);
    #endregion

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

    #region Filtered Store Items
        public static Terminal? _terminal = null;
        public static Item[] _terminalBuyableItemsList = [];
        public static List<Item> _filteredStoreItems = [];
        public static List<double> _filteredStoreItemWeights = [];
        public static List<Item> filteredStoreItems {
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
        public static List<double> filteredStoreItemWeights {
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
        public static List<SpawnableItemWithRarity> _currentLevelSpawnableScrap = [];
        public static List<SpawnableItemWithRarity> _filteredScrapItems = [];
        public static List<double> _filteredScrapItemWeights = [];
        public static List<SpawnableItemWithRarity> filteredScrapItems
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
        public static List<double> filteredScrapItemWeights {
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
    
    public static bool InitGiftboxModdedBehavior(GiftBoxItem giftbox, Random giftboxBehaviorSeed, Random valueBehaviorSeed)
    {
        int behaviorIndex = Probability.GetRandomWeightedIndex(giftboxBehaviors, giftboxBehaviorSeed);
        if (behaviorIndex == DO_NOTHING) return false; // Gift Box - Do Nothing

        GiftBoxModdedParams moddedParams = giftbox.gameObject.AddComponent<GiftBoxModdedBehavior>().Params;
        moddedParams.CanEggsplode = giftboxBehaviorSeed.Next(0, 100) < Plugin.giftboxEggsplosionChance.Value;
        
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

                if (parentGiftboxParams != null)
                {
                    moddedParams.NestedScrapId = parentGiftboxParams.NestedScrapId;
                } 
                else 
                {
                    int nestedScrapIndex = Probability.GetRandomWeightedIndex(filteredScrapItemWeights, giftboxBehaviorSeed);
                    Item? nestedScrapItem = filteredScrapItems.ElementAtOrDefault(nestedScrapIndex)?.spawnableItem;

                    if (nestedScrapItem)
                        moddedParams.NestedScrapId = StartOfRound.Instance.allItemsList.itemsList.FindIndex(item => item == nestedScrapItem);
                }

                goto case SPAWN_SCRAP;
            case SPAWN_SCRAP: // Gift Box - Spawn Scrap
                if (behaviorIndex == SPAWN_SCRAP) {
                    int scrapIndex = parentGiftboxParams?.NestedScrapId ?? Probability.GetRandomWeightedIndex(filteredScrapItemWeights, giftboxBehaviorSeed);
                    if (scrapIndex == -1) {
                        break;
                    }

                    giftbox.objectInPresentItem = filteredScrapItems[scrapIndex].spawnableItem;
                    giftbox.objectInPresent = giftbox.objectInPresentItem.spawnPrefab;
                }

                giftbox.objectInPresentValue = valueBehaviorSeed.Next(giftbox.objectInPresentItem.minValue, giftbox.objectInPresentItem.maxValue);

                // Gift Box - Scrap Value Addition
                if (valueBehaviorSeed.Next(0, 100) < Plugin.scrapValueAdditionChance.Value)
                    giftbox.objectInPresentValue += valueBehaviorSeed.Next(Plugin.scrapValueAdditionMin.Value, Plugin.scrapValueAdditionMax.Value + 1);

                // Gift Box - Scrap Value Multiplier
                if (valueBehaviorSeed.Next(0, 100) < Plugin.scrapValueMultiplierChance.Value)
                    giftbox.objectInPresentValue = (int)(giftbox.objectInPresentValue * (Plugin.scrapValueMultiplierMin.Value + (Plugin.scrapValueMultiplierMax.Value - Plugin.scrapValueMultiplierMin.Value) * valueBehaviorSeed.NextDouble()) / 100);
                
                // Gift Box - Inherit Gift Box Value (if host disables mod behaviors mid-round, above values will be used instead)
                if (valueBehaviorSeed.Next(0, 100) < Plugin.scrapValueIsGiftBoxChance.Value)
                    moddedParams.ScrapHasGiftBoxValue = true;

                break;
            default:
                throw new Exception("[Patches.GiftBoxItemPatches.GiftBoxItemPatch.InitGiftboxModdedBehavior] Giftbox Behavior selection failed! This should never happen!");
        }

        // Ensure scrap value is saved to file
        moddedParams.ScrapValue = giftbox.objectInPresentValue;

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

        filteredStoreItems = null!;
        filteredScrapItems = null!;

        giftboxBehaviors[DO_NOTHING] = Plugin.doNothingChance.Value;
        giftboxBehaviors[SPAWN_STORE_ITEM] = Plugin.spawnStoreItemChance.Value;
        giftboxBehaviors[SPAWN_SCRAP] = Plugin.spawnScrapChance.Value;
        giftboxBehaviors[SPAWN_GIFTBOX] = Plugin.spawnGiftBoxChance.Value;
        giftboxBehaviors[SPAWN_NOTHING] = Plugin.spawnNothingChance.Value;

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // Start() destination: if (base.IsServer ** **) 
        stepper.GotoIL(code => code.LoadsProperty(type: typeof(NetworkBehaviour), name: "IsServer"), errorMessage: "[Patches.GiftBoxItemPatches.GiftBoxItemPatch.Start] Property NetworkBehaviour.IsServer not found!");
        stepper.GotoIndex(offset: 1);

        // Start() insertion: ** && !GiftBoxItemPatch.InitGiftboxModdedBehavior(this, randomSeed, random) **
        stepper.InsertIL(codeRange: [
            CodeInstructionPolyfills.LoadArgument(0), // this
            CodeInstructionPolyfills.LoadLocal(0), // this, randomSeed
            CodeInstructionPolyfills.LoadLocal(1), // this, randomSeed, random
            CodeInstructionPolyfills.Call(type: typeof(GiftBoxItemPatch), name: nameof(InitGiftboxModdedBehavior)), // GiftBoxItemPatch.InitGiftboxModdedBehavior(this, randomSeed, random)
            new CodeInstruction(OpCodes.Not), // !GiftBoxItemPatch.InitGiftboxModdedBehavior(this, randomSeed, random)
            new CodeInstruction(OpCodes.And) // && !GiftBoxItemPatch.InitGiftboxModdedBehavior(this, randomSeed, random)
        ]);
        
        return stepper.Instructions;
    }

    public static void NestedGiftboxFun(GiftBoxItem giftbox, GrabbableObject? spawnedObj)
    {
        // Check if spawned object is nested giftbox
        if (spawnedObj == null || spawnedObj.itemProperties.itemId != GIFTBOX_ITEM_ID) return;

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

        // Set 
        GiftBoxModdedParams? moddedParams = spawnedObj.GetComponent<GiftBoxModdedBehavior>();
        if (moddedParams == null) return;
    }

    public static bool OverrideOpenGiftBox(GiftBoxItem giftbox)
    {
        // this is no longer dumb
        GiftBoxModdedParams? moddedParams = giftbox.GetComponent<GiftBoxModdedBehavior>();
        if (moddedParams == null) return false;

        // Gift Box - Inherit Gift Box Scrap Value
        if (moddedParams.ScrapHasGiftBoxValue) giftbox.objectInPresentValue = giftbox.scrapValue;

        // Use reverse patched vanilla logic to spawn the item
        parentGiftboxParams = moddedParams;
        GrabbableObject? spawnedObj = SpawnGiftItem(giftbox);
        parentGiftboxParams = null;

        // Apply nested giftbox fun if applicable
        NestedGiftboxFun(giftbox, spawnedObj);

        // Empty Gift Box Eggsplosion Chance
        if (spawnedObj == null && moddedParams.CanEggsplode)
            EggsplosionClientRpc(giftbox.gameObject);

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