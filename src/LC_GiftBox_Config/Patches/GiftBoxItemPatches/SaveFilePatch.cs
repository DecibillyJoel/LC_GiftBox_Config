using BepInEx;
using UnityEngine;
using Steamworks;
using System;
using HarmonyLib;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using LC_GiftBox_Config.libs.ILStepper;
using LC_GiftBox_Config.libs.HarmonyXExtensions;
using System.Reflection;
using System.Reflection.Emit;
using Unity.Netcode;
using Newtonsoft.Json.Serialization;
using LC_GiftBox_Config.libs.Probability;

using LogLevel = BepInEx.Logging.LogLevel;
using Object = UnityEngine.Object;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;
using Random = System.Random;

namespace LC_GiftBox_Config.Patches.GiftBoxItemPatches;
using GiftBoxModdedParams = GiftBoxItemPatch.GiftBoxModdedParams;
using GiftBoxModdedBehavior = GiftBoxItemPatch.GiftBoxModdedBehavior;

internal static class SaveFilePatch
{
    #region Savefile Keys
        internal static readonly string GiftBoxModdedParamsSaveKey = $"{Plugin.harmony.Id}.giftboxModdedParamsDict";
    #endregion
    
    internal static Dictionary<int, GiftBoxModdedParams> GetGiftBoxModdedParamsDict()
    {
        if (!ES3.KeyExists(GiftBoxModdedParamsSaveKey, GameNetworkManager.Instance.currentSaveFileName)) return [];

        return ES3.Load<Dictionary<int, GiftBoxModdedParams>>(GiftBoxModdedParamsSaveKey, GameNetworkManager.Instance.currentSaveFileName);
    }

    internal static void LoadGiftBoxModdedParams(Dictionary<int, GiftBoxModdedParams> dict, int index, GrabbableObject grabbable)
    {
        if (!dict.ContainsKey(index)) return;

        grabbable.gameObject.AddComponent<GiftBoxModdedBehavior>().Params = dict[index];
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> LoadShipGrabbableItems(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // LoadShipGrabbableItems() destination: Debug.Log("Ship grabbable yadda yadda"); ** **
        stepper.GotoIL(code => code.LoadsString("Ship grabbable items list loaded. Count: {0}"), errorMessage: "[Patches.GiftBoxItemPatches.SaveFilePatch.LoadShipGrabbableItems] String \"Ship grabbable items list loaded. Count: {0}\" not found");
        stepper.GotoIL(ILPatterns.NextEmptyStack(startSize: 0),  errorMessage: "[Patches.GiftBoxItemPatches.SaveFilePatch.LoadShipGrabbableItems] Call Debug.Log not found");
        stepper.GotoIndex(offset: 1);

        // LoadShipGrabbableItems() insertion: Dict<int, GiftBoxModdedParams> giftboxModdedParamsDict = SaveFilePatch.GetGiftBoxModdedParamsDict();
        LocalVariableInfo GiftBoxModdedParamsDictLocal = stepper.DeclareLocal(typeof(Dictionary<int, GiftBoxModdedParams>));
        stepper.InsertIL([
            CodeInstructionPolyfills.Call(type: typeof(SaveFilePatch), name: nameof(GetGiftBoxModdedParamsDict)),
            CodeInstructionPolyfills.StoreLocal(GiftBoxModdedParamsDictLocal)
        ]);

        // LoadShipGrabbableItems() destination: GrabbableObject component = Object.Instantiate(yadda yadda); ** **
        stepper.GotoIL(code => code.StoresLocal(index: 0));
        stepper.GotoIndex(offset: 1);

        // LoadShipGrabbableItems() insertion: SaveFilePatch.LoadGiftBoxModdedParams(giftboxModdedParamsDict, index, component);
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadLocal(GiftBoxModdedParamsDictLocal), // giftboxModdedParamsDict
            CodeInstructionPolyfills.LoadLocal(index: 9), // giftboxModdedParamsDict, i
            CodeInstructionPolyfills.LoadLocal(index: 0), // giftboxModdedParamsDict, i, component
            CodeInstructionPolyfills.Call(type: typeof(SaveFilePatch), name: nameof(LoadGiftBoxModdedParams)) // SaveFilePatch.LoadGiftBoxModdedParams(giftboxModdedParamsDict, index, component);
        ]);

        return stepper.Instructions;
    }

    internal static void SetGiftBoxModdedParamsInDict(Dictionary<int, GiftBoxModdedParams> dict, int index, GrabbableObject[] grabbables)
    {
        if (!grabbables[index].TryGetComponent(out GiftBoxModdedBehavior moddedBehavior)) return;

        dict.Add(index, moddedBehavior.Params);
    }

    internal static void SaveGiftBoxModdedParamsDict(Dictionary<int, GiftBoxModdedParams> dict)
    {
        ES3.Save(GiftBoxModdedParamsSaveKey, dict, GameNetworkManager.Instance.currentSaveFileName);
    }

    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveItemsInShip))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> SaveItemsInShip(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // SaveItemsInShip() destination: ** ** ES3.DeleteKey("shipGrabbableItemIDs", this.currentSaveFileName);
        stepper.GotoIL(code => code.LoadsString("shipGrabbableItemIDs"), errorMessage: "[Patches.GiftBoxItemPatches.SaveFilePatch.SaveItemsInShip] String \"shipGrabbableItemIDs\" not found");

        // SaveItemsInShip() insertion: ES3.DeleteKey(SaveFilePatch.GiftBoxModdedParamsSaveKey, this.currentSaveFileName);
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadString(GiftBoxModdedParamsSaveKey), // SaveFilePatch.GiftBoxModdedParamsSaveKey
            CodeInstructionPolyfills.LoadArgument(index: 0), // SaveFilePatch.GiftBoxModdedParamsSaveKey, this
            CodeInstructionPolyfills.LoadField(type: typeof(GameNetworkManager), name: "currentSaveFileName"), // SaveFilePatch.GiftBoxModdedParamsSaveKey, this.currentSaveFileName
            CodeInstructionPolyfills.Call(type: typeof(ES3), name: nameof(ES3.DeleteKey)) // ES3.DeleteKey(SaveFilePatch.GiftBoxModdedParamsSaveKey, this.currentSaveFileName);
        ]);

        // SaveItemsInShip() destination: List<int> list4 = new List<int>(); ** **
        stepper.GotoIL(code => code.StoresLocal(index: 4));
        stepper.GotoIndex(offset: 1);

        // SaveItemsInShip() insertion: List<GiftBoxModdedParams> giftboxModdedParamsDict = new();
        LocalVariableInfo GiftBoxModdedParamsDictLocal = stepper.DeclareLocal(typeof(Dictionary<int, GiftBoxModdedParams>));
        stepper.InsertIL([
            CodeInstructionPolyfills.CallConstructor(type: typeof(Dictionary<int, GiftBoxModdedParams>)), // new Dictionary<int, GiftBoxModdedParams>();
            CodeInstructionPolyfills.StoreLocal(GiftBoxModdedParamsDictLocal) // Dictionary<int, GiftBoxModdedParams> giftboxModdedParamsDict = new();
        ]);

        // SaveItemsInShip() destination: ** ** Debug.Log(string.Format("Saved data for item type: yadda yadda"));
        stepper.GotoIL(code => code.LoadsString("Saved data for item type: {0} - {1}"), errorMessage: "[Patches.GiftBoxItemPatches.SaveFilePatch.SaveItemsInShip] String \"Saved data for item type: {0} - {1}\" not found");
        
        // SaveItemsInShip() insertion: SaveFilePatch.SetGiftBoxModdedParamsInDict(giftboxModdedParamsDict, num2, array);
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadLocal(GiftBoxModdedParamsDictLocal), // giftboxModdedParamsDict
            CodeInstructionPolyfills.LoadLocal(index: 6), // giftboxModdedParamsDict, num2
            CodeInstructionPolyfills.LoadLocal(index: 0), // giftboxModdedParamsDict, num2, array
            CodeInstructionPolyfills.Call(type: typeof(SaveFilePatch), name: nameof(SetGiftBoxModdedParamsInDict)) // SaveFilePatch.SetGiftBoxModdedParamsInDict(giftboxModdedParamsDict, num2, array);
        ]);

        // SaveItemsInShip() destination: ** ** ES3.Save<Vector3[]>("shipGrabbableItemPos", list2.ToArray(), this.currentSaveFileName);
        stepper.GotoIL(code => code.LoadsString("shipGrabbableItemPos"), errorMessage: "[Patches.GiftBoxItemPatches.SaveFilePatch.SaveItemsInShip] String \"shipGrabbableItemPos\" not found");
        
        // SaveItemsInShip() insertion: SaveFilePatch.SaveGiftBoxModdedParamsDict(giftboxModdedParamsDict);
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadLocal(GiftBoxModdedParamsDictLocal), // giftboxModdedParamsDict
            CodeInstructionPolyfills.Call(type: typeof(SaveFilePatch), name: nameof(SaveGiftBoxModdedParamsDict)) // SaveFilePatch.SaveGiftBoxModdedParamsDict(giftboxModdedParamsDict);
        ]);

        return stepper.Instructions;
    }
}