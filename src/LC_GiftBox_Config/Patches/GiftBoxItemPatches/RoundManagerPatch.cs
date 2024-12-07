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
using LC_GiftBox_Config.libs.Probability;

using LogLevel = BepInEx.Logging.LogLevel;
using Object = UnityEngine.Object;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;
using Random = System.Random;
using Newtonsoft.Json.Serialization;

namespace LC_GiftBox_Config.Patches.GiftBoxItemPatches;

[HarmonyPatch(typeof(RoundManager))]
internal static class RoundManagerPatch
{
    internal static void AnomalouslySpawnGiftBoxes(RoundManager roundmanager, List<Item> ScrapToSpawn, int spawnOneItemIndex, List<SpawnableItemWithRarity> spawnableScrap)
    {
        // Don't perform gift box anomaly if the "spawn one item" anomaly is already occuring and the anomalous item is something other than the gift box
        if (spawnOneItemIndex != -1 && spawnableScrap[spawnOneItemIndex].spawnableItem.itemId != GiftBoxItemPatch.GIFTBOX_ITEM_ID) return;
        if (GiftBoxItemPatch.GIFTBOX_ITEM == null) return;

        Random AnomalyRandom = roundmanager.AnomalyRandom;

        // Gift Box Anomalous Spawning
        if (AnomalyRandom.Next(0, 100) >= Plugin.giftboxSpawnChance.Value) return;

        int giftboxCount = AnomalyRandom.Next(Plugin.giftboxSpawnMin.Value, Plugin.giftboxSpawnMax.Value + 1);
        ScrapToSpawn.AddRange(Enumerable.Repeat(GiftBoxItemPatch.GIFTBOX_ITEM, giftboxCount).ToList());
    }

    internal static void AdjustGiftBoxSpawnWeight(RoundManager roundmanager, int[] weights, List<SpawnableItemWithRarity> spawnableScrap)
    {
        Random AnomalyRandom = roundmanager.AnomalyRandom;

        for (int j = 0; j < weights.Length; j++) {
            if (spawnableScrap[j].spawnableItem.itemId != GiftBoxItemPatch.GIFTBOX_ITEM_ID) continue;
            
            // Gift Box Rarity Addition
            if (AnomalyRandom.Next(0, 100) < Plugin.giftboxRarityAdditionChance.Value)
            {
                weights[j] += AnomalyRandom.Next(Plugin.giftboxRarityAdditionMin.Value, Plugin.giftboxRarityAdditionMax.Value + 1);
            }

            // Gift Box Rarity Multiplier
            if (AnomalyRandom.Next(0, 100) < Plugin.giftboxRarityMultiplierChance.Value)
            {
                weights[j] = (int)(weights[j] * (Plugin.giftboxRarityMultiplierMin.Value + (Plugin.giftboxRarityMultiplierMax.Value - Plugin.giftboxRarityMultiplierMin.Value) * AnomalyRandom.NextDouble()) / 100);
            }
        }
    }

    internal static void AdjustGiftBoxValue(RoundManager roundmanager, GrabbableObject component, List<int> scrapValues)
    {
        if (component.itemProperties.itemId != GiftBoxItemPatch.GIFTBOX_ITEM_ID) return;

        Random AnomalyRandom = roundmanager.AnomalyRandom;

        // Gift Box Value Addition
        if (AnomalyRandom.Next(0, 100) < Plugin.giftboxValueAdditionChance.Value)
        {
            scrapValues[^1] += AnomalyRandom.Next(Plugin.giftboxValueAdditionMin.Value, Plugin.giftboxValueAdditionMax.Value + 1);
        }

        // Gift Box Value Multiplier
        if (AnomalyRandom.Next(0, 100) < Plugin.giftboxValueMultiplierChance.Value)
        {
            scrapValues[^1] = (int)(scrapValues[^1] * (Plugin.giftboxValueMultiplierMin.Value + (Plugin.giftboxValueMultiplierMax.Value - Plugin.giftboxValueMultiplierMin.Value) * AnomalyRandom.NextDouble()) / 100);
        }
    }
    
    [HarmonyPatch(nameof(RoundManager.SpawnScrapInLevel))]
    [HarmonyPriority(priority: int.MinValue)]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> SpawnScrapInLevel(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator, MethodBase methodBase){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator, methodBase);

        // SDM / scrap injection compat (find what list is being used as spawnable scrap)
        stepper.GotoIL((code, index) => code.StoresLocal(index: 15) && index > 0 && stepper.Instructions[index - 1].LoadsConstant(0), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] For loop initialization (int j = 0;) not found");
        stepper.GotoIL(code => code.opcode.FlowControl == FlowControl.Branch, errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] For loop branch to control statement (j < this.currentLevel.spawnableScrap.Count;) not found");
        stepper.GotoIL(code => code.labels.Contains((Label)stepper.CurrentOperand!), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] For loop control statement (j < this.currentLevel.spawnableScrap.Count;) not found");
        stepper.GotoIL(code => code.LoadsProperty(type: typeof(List<SpawnableItemWithRarity>), name: "Count"), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Load Property List.Count (this.currentLevel.spawnableScrap.Count) not found");

        List<CodeInstruction> SpawnableScrapIL = stepper.GetIL(
            startIndex: stepper.FindIL(ILPatterns.NextEmptyStack(startSize: -1), reverse: true,  errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Spawnable Scrap List (this.currentLevel.spawnableScrap) not found)")
        )
        .Select(code => code.Clone()).ToList();

        // Reset stepper position
        stepper.GotoIndex(index: 0);

        // SpawnScrapInLevel() destination: compilerClosureObj.ScrapToSpawn = new List<Item>(); ** **
        stepper.GotoIL(code => code.StoresField(type: stepper.GetLocal(0).LocalType, name: "ScrapToSpawn"), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Store Field compilerClosureObj.ScrapToSpawn not found");
        stepper.GotoIndex(offset: 1);

        // SpawnScrapInLevel() insertion: ** RoundManagerPatch.AnomalouslySpawnGiftBoxes(this, compilerClosureObj.ScrapToSpawn, num3, this.currentLevel.spawnableScrap); **
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadLocal(index: 0), // this, compilerClosureObj
            CodeInstructionPolyfills.LoadField(type: stepper.GetLocal(0).LocalType, name: "ScrapToSpawn"), // this, compilerClosureObj.ScrapToSpawn
            CodeInstructionPolyfills.LoadLocal(index: 2), // this, compilerClosureObj.ScrapToSpawn, num3
            ..SpawnableScrapIL, // this, compilerClosureObj.ScrapToSpawn, num3, this.currentLevel.spawnableScrap
            CodeInstructionPolyfills.Call(type: typeof(RoundManagerPatch), name: nameof(AnomalouslySpawnGiftBoxes)) // RoundManagerPatch.AnomalouslySpawnGiftBoxes(this, compilerClosureObj.ScrapToSpawn, num3, this.currentLevel.spawnableScrap);
        ]);
        
        // SpawnScrapInLevel() destination: int[] weights = list2.ToArray(); ** **
        stepper.GotoIL(code => code.StoresLocal(index: 6), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Store Local 6 (weights) not found");
        stepper.GotoIndex(offset: 1);

        // SpawnScrapInLevel() insertion: RoundManagerPatch.AdjustGiftBoxSpawnWeight(this, weights, this.currentLevel.spawnableScrap);
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadLocal(index: 6), // this, weights
            ..SpawnableScrapIL, // this, weights, this.currentLevel.spawnableScrap
            CodeInstructionPolyfills.Call(type: typeof(RoundManagerPatch), name: nameof(AdjustGiftBoxSpawnWeight)) // RoundManagerPatch.AdjustGiftBoxSpawnWeight(this, weights, this.currentLevel.spawnableScrap)
        ]);

        // SpawnScrapInLevel() destination: ** ** num4 += list[list.Count - 1];
        stepper.GotoIL(code => code.StoresField(type: typeof(GrabbableObject), name: "scrapValue"), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Store field GrabbableObject.scrapValue not found");
        stepper.GotoIL(ILPatterns.NextEmptyStack(startSize: 0), offset: 1, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Load Local 4 (num4) not found");

        // SpawnScrapInLevel() insertion: ** RoundManagerPatch.AdjustGiftBoxValue(this, component, list); ** num4 += list[list.Count - 1];
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadLocal(index: 18), // this, component
            CodeInstructionPolyfills.LoadLocal(index: 3), // this, component, list
            CodeInstructionPolyfills.Call(type: typeof(RoundManagerPatch), name: nameof(AdjustGiftBoxValue)) // RoundManagerPatch.AdjustGiftBoxValue(this, component, list);
        ]);

        return stepper.Instructions;
    }
}