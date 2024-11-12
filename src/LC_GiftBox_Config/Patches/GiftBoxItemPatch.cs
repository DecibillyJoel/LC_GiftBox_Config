using BepInEx;
using UnityEngine;
using Steamworks;
using System;
using HarmonyLib;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using LC_GiftBox_Config.libs.ILTools;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using LC_GiftBox_Config.libs.Probability;

using Object = UnityEngine.Object;
using Random = System.Random;
using LogLevel = BepInEx.Logging.LogLevel;

namespace LC_GiftBox_Config.Patches;

[HarmonyPatch(typeof(GiftBoxItem))]
internal static class GiftBoxItemPatch
{
    #region GiftBox Behaviors
        internal static List<int> giftboxBehaviors = new(4);
        const int DO_NOTHING = 0;
        const int SPAWN_STORE_ITEM = 1;
        const int SPAWN_SCRAP = 2;
        const int SPAWN_NOTHING = 3;
    #endregion
    
    #region Scrap Value Spawn Behaviors
        const int IGNORE_ITEM_VALUE = -1;
        const int USE_GIFTBOX_VALUE = -2;
    #endregion

    #region Filtered Store Items
        internal static Terminal _terminal = null!;
        internal static Item[] _terminalBuyableItemsList = null!;
        internal static List<Item> _filteredStoreItems = [];
        internal static List<double> filteredStoreItemWeights = [];
        internal static List<Item> filteredStoreItems {
            set { _filteredStoreItems = value; }
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

                    filteredStoreItemWeights = _filteredStoreItems.Select(item
                        => Math.Pow(item.creditsWorth, Plugin.scrapValueInfluence.Value / 100.0)
                    ).ToList();
                }

                return _filteredStoreItems;
            }
        }
    #endregion

    #region Filtered Scrap Items
        internal static List<SpawnableItemWithRarity> _currentLevelSpawnableScrap = [];
        internal static List<SpawnableItemWithRarity> _filteredScrapItems = [];
        internal static List<double> filteredScrapItemWeights = [];
        internal static List<SpawnableItemWithRarity> filteredScrapItems {
            set { _filteredScrapItems = value; }
            get {
                if (_currentLevelSpawnableScrap != RoundManager.Instance.currentLevel.spawnableScrap) 
                {
                    _currentLevelSpawnableScrap = RoundManager.Instance.currentLevel.spawnableScrap;
                    _filteredScrapItems.Clear();
                }

                if(_filteredScrapItems.Count == 0) 
                {
                    _filteredScrapItems = _currentLevelSpawnableScrap.Where(item
                        => item.spawnableItem.minValue >= Plugin.scrapValueMin.Value
                        && item.spawnableItem.maxValue <= Plugin.scrapValueMax.Value
                        && item.rarity >= Plugin.scrapRarityMin.Value
                        && item.rarity <= Plugin.scrapRarityMax.Value
                    ).ToList();

                    filteredScrapItemWeights = _filteredScrapItems.Select(item
                        => Math.Pow((item.spawnableItem.minValue + item.spawnableItem.maxValue) / 2.0, Plugin.scrapValueInfluence.Value / 100.0)
                         + Math.Pow(item.rarity, Plugin.scrapRarityInfluence.Value / 100.0)
                    ).ToList();
                }

                return _filteredScrapItems;
            }
        }
    #endregion
    
    internal static bool InsertObjectInPresentAndScrapValue(GiftBoxItem giftbox, Random giftboxBehaviorSeed, Random valueBehaviorSeed)
    {
        int behaviorIndex = Probability.GetRandomWeightedIndex(giftboxBehaviors, giftboxBehaviorSeed);
        
        switch (behaviorIndex) {
            case DO_NOTHING: // Gift Box - Do Nothing
                return false;
            case SPAWN_NOTHING: // Gift Box - Spawn Nothing
                giftbox.objectInPresentItem = null;
                giftbox.objectInPresent = null;

                giftbox.objectInPresentValue = IGNORE_ITEM_VALUE;

                break;
            case SPAWN_STORE_ITEM: // Gift Box - Spawn Store Item
                int itemIndex = Probability.GetRandomWeightedIndex(filteredStoreItemWeights, giftboxBehaviorSeed);

                giftbox.objectInPresentItem = filteredStoreItems[itemIndex];
                giftbox.objectInPresent = giftbox.objectInPresentItem.spawnPrefab;
                
                giftbox.objectInPresentValue = IGNORE_ITEM_VALUE;

                break;
            case SPAWN_SCRAP: // Gift Box - Spawn Scrap
                int scrapIndex = Probability.GetRandomWeightedIndex(filteredScrapItemWeights, giftboxBehaviorSeed);

                giftbox.objectInPresentItem = filteredScrapItems[scrapIndex].spawnableItem;
                giftbox.objectInPresent = giftbox.objectInPresentItem.spawnPrefab;

                giftbox.objectInPresentValue = (int)(valueBehaviorSeed.Next(giftbox.objectInPresentItem.minValue, giftbox.objectInPresentItem.maxValue) * RoundManager.Instance.scrapValueMultiplier);

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
                    giftbox.objectInPresentValue = (int)(giftbox.objectInPresentValue * (Plugin.scrapValueMultiplierMin.Value + (Plugin.scrapValueMultiplierMax.Value - Plugin.scrapValueMultiplierMin.Value) * valueBehaviorSeed.NextDouble()));
                }

                // Clamp the value and mark the gift box as modified so OverrideOpenGiftBox() knows not to ignore this giftbox (this is dumb)
                giftbox.objectInPresentValue = -Math.Max(0, giftbox.objectInPresentValue) - 100;
                
                break;
        }

        return true;
    }

    [HarmonyPatch(nameof(GiftBoxItem.Start))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Start(IEnumerable<CodeInstruction> methodIL){
        if (Plugin.giftBoxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        filteredStoreItems.Clear();

        giftboxBehaviors[DO_NOTHING] = Plugin.doNothingChance.Value;
        giftboxBehaviors[SPAWN_STORE_ITEM] = Plugin.spawnStoreItemChance.Value;
        giftboxBehaviors[SPAWN_SCRAP] = Plugin.spawnScrapChance.Value;
        giftboxBehaviors[SPAWN_NOTHING] = Plugin.spawnNothingChance.Value;

        List<CodeInstruction> moddedIL = new(methodIL);
        int moddedIndex = 0;

        // Start() destination: if (base.IsServer ** **) 
        MethodInfo method_get_IsServer = typeof(NetworkBehaviour).GetMethod("get_IsServer", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) 
            ?? throw new Exception("[Patches.GiftBoxItemPatch.Start] NetworkBehaviour.get_IsServer() MethodInfo not accessible");
        ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, method_get_IsServer, errorMessage: "[Patches.GiftBoxItemPatch.Start] method_get_IsServer not found");
        ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, OpCodes.Brfalse, errorMessage: "[Patches.GiftBoxItemPatch.Start] OpCodes.Brfalse not found");

        // Start() insertion: ** && !GiftBoxItemPatch.InsertObjectInPresentAndScrapValue(this, randomSeed, random) **
        MethodInfo method_InsertObjectInPresentAndScrapValue = typeof(GiftBoxItemPatch).GetMethod("InsertObjectInPresentAndScrapValue", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) 
            ?? throw new Exception($"[Patches.GiftBoxItemPatch.Start] GiftBoxItemPatch.InsertObjectInPresentAndScrapValue() MethodInfo not accessible");
        moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Ldarg_0)); // this
        moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Ldloc_0)); // this, randomSeed
        moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Ldloc_1)); // this, randomSeed, random
        moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Call, method_InsertObjectInPresentAndScrapValue)); // GiftBoxItemPatch.InsertObjectInPresentAndScrapValue(this, randomSeed, random)
        moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Not)); // !GiftBoxItemPatch.InsertObjectInPresentAndScrapValue(this, randomSeed, random)
        moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.And)); // && !GiftBoxItemPatch.InsertObjectInPresentAndScrapValue(this, randomSeed, random)

        return moddedIL.AsEnumerable();
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
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> methodIL, ILGenerator generator)
        {
            if (Plugin.giftBoxMechanicsDisabled.Value)
            {
                return methodIL;
            }

            List<CodeInstruction> moddedIL = new(methodIL);
            int moddedIndex = 0;

            // OpenGiftBoxServerRpc() destination: ** ** GameObject gameObject = null;
            MethodInfo method_get_zero = typeof(Vector3).GetMethod("get_zero", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) 
                ?? throw new Exception("[Patches.GiftBoxItemPatch.SpawnGiftItem] Vector3.get_zero() MethodInfo not accessible");
            ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, method_get_zero, errorMessage: "[Patches.GiftBoxItemPatch.SpawnGiftItem] method_get_zero not found");
            ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, OpCodes.Ldnull, reverse: true, errorMessage: "[Patches.GiftBoxItemPatch.SpawnGiftItem] OpCodes.Ldnull not found");

            // Remove everything before this point
            moddedIL.RemoveRange(0, moddedIndex);
            moddedIndex = 0;

            // OpenGiftBoxServerRpc() destination: ** Debug.LogError("Error: There is no object in gift box!"); **
            ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, "Error: There is no object in gift box!", errorMessage: "[Patches.GiftBoxItemPatch.SpawnGiftItem] \"no object\" error message not found");
            
            // OpenGiftBoxServerRpc() deletion: ** Debug.LogError("Error: There is no object in gift box!"); **
            moddedIL.RemoveRange(moddedIndex, 2);

            // OpenGiftBoxServerRpc() destination: ** ** component.SetScrapValue(num);
            ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, localIndex: 1, instructionIsStore: true, errorMessage: "[Patches.GiftBoxItemPatch.SpawnGiftItem] Store Local 1 (num) not found");
            ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, localIndex: 4, instructionIsStore: false, errorMessage: "[Patches.GiftBoxItemPatch.SpawnGiftItem] 1st Load Local 4 (component) not found");

            // OpenGiftBoxServerRpc() insertion: ** if (num >= 0) **
            Label skipScrapValue = generator.DefineLabel();
            moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Ldloc_1)); // num
            moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Ldc_I4_0)); // (int32) 0
            moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Blt, skipScrapValue)); // if (num < 0) goto skipScrapValue
            
            // OpenGiftBoxServerRpc() destination: ** ** component.NetworkObject.Spawn(false);
            ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, localIndex: 4, instructionIsStore: false, errorMessage: "[Patches.GiftBoxItemPatch.SpawnGiftItem] 2nd Load Local 4 (component) not found");
            
            // OpenGiftBoxServerRpc() insertion: ** skipScrapValue: **
            moddedIL.Insert(moddedIndex, new CodeInstruction(OpCodes.Nop));
            moddedIL[moddedIndex].labels.Add(skipScrapValue);

            return moddedIL;
        }

        _ = Transpiler(null!, null!);
    }

    [HarmonyPatch(nameof(GiftBoxItem.OpenGiftBoxServerRpc))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> OpenGiftBoxServerRpc(IEnumerable<CodeInstruction> methodIL){
        if (Plugin.giftBoxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        List<CodeInstruction> moddedIL = new(methodIL);
        int moddedIndex = 0;

        // OpenGiftBoxServerRpc() destination: if (this.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server || (!networkManager.IsServer && !networkManager.IsHost) ** **)
        MethodInfo method_get_IsHost = typeof(NetworkManager).GetMethod("get_IsHost", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) 
            ?? throw new Exception("[Patches.GiftBoxItemPatch.OpenGiftBoxServerRpc] NetworkManager.get_IsHost() MethodInfo not accessible");
        ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, method_get_IsHost, errorMessage: "[Patches.GiftBoxItemPatch.OpenGiftBoxServerRpc] 1st method_get_IsHost not found");
        moddedIndex++;
        ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, method_get_IsHost, errorMessage: "[Patches.GiftBoxItemPatch.OpenGiftBoxServerRpc] 2nd method_get_IsHost not found");
        ILTools.FindCodeInstruction(ref moddedIndex, ref moddedIL, OpCodes.Ret, errorMessage: "[Patches.GiftBoxItemPatch.Start] OpCodes.Ret not found");

        // OpenGiftBoxServerRpc() insertion: ** || GiftBoxItemPatch.OverrideOpenGiftBox(this) **
        MethodInfo method_InsertObjectInPresentAndScrapValue = typeof(GiftBoxItemPatch).GetMethod("OverrideOpenGiftBox", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public) 
            ?? throw new Exception("[Patches.GiftBoxItemPatch.OpenGiftBoxServerRpc] GiftBoxItemPatch.OverrideOpenGiftBox() MethodInfo not accessible");
        moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Ldarg_0)); // this
        moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Call, method_InsertObjectInPresentAndScrapValue)); // GiftBoxItemPatch.OverrideOpenGiftBox(this)
        moddedIL.Insert(moddedIndex++, new CodeInstruction(OpCodes.Or)); // || GiftBoxItemPatch.OverrideOpenGiftBox(this)

        return moddedIL.AsEnumerable();
    }
}