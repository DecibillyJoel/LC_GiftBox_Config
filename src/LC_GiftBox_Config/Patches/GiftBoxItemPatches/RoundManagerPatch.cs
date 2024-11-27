using BepInEx;
using UnityEngine;
using Steamworks;
using System;
using HarmonyLib;
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
using LC_GiftBox_Config.libs.HarmonyXExtensions;

namespace LC_GiftBox_Config.Patches.GiftBoxItemPatches;

[HarmonyPatch(typeof(RoundManager))]
internal static class RoundManagerPatch
{
    internal static void AnomalouslySpawnGiftBoxes(RoundManager roundmanager, List<Item> ScrapToSpawn, int spawnOneItemIndex)
    {
        // Don't perform gift box anomaly if the "spawn one item" anomaly is already occuring and the anomalous item is something other than the gift box
        if (spawnOneItemIndex != -1 && ScrapToSpawn[0].itemId != GiftBoxItemPatch.GIFTBOX_ITEM_ID) return;

        Random AnomalyRandom = roundmanager.AnomalyRandom;

        // Gift Box - Gift Box Anomalous Spawning
        if (AnomalyRandom.Next(0, 100) >= Plugin.giftboxSpawnChance.Value) return;

        int giftboxCount = AnomalyRandom.Next(Plugin.giftboxSpawnMin.Value, Plugin.giftboxSpawnMax.Value + 1);
        ScrapToSpawn.AddRange(Enumerable.Repeat(GiftBoxItemPatch.GIFTBOX_ITEM, giftboxCount));
    }

    internal static void AdjustGiftBoxSpawnWeight(RoundManager roundmanager, int[] weights)
    {
        Random AnomalyRandom = roundmanager.AnomalyRandom;
        List<SpawnableItemWithRarity> spawnableScrap = roundmanager.currentLevel.spawnableScrap;

        for (int j = 0; j < weights.Length; j++) {
            if (spawnableScrap[j].spawnableItem.itemId != GiftBoxItemPatch.GIFTBOX_ITEM_ID) continue;
            
            // Gift Box - Gift Box Rarity Addition
            if (AnomalyRandom.Next(0, 100) >= Plugin.giftboxRarityAdditionChance.Value)
            {
                weights[j] += AnomalyRandom.Next(Plugin.giftboxRarityAdditionMin.Value, Plugin.giftboxRarityAdditionMax.Value + 1);
            }

            // Gift Box - Gift Box Rarity Multiplier
            if (AnomalyRandom.Next(0, 100) >= Plugin.giftboxRarityMultiplierChance.Value)
            {
                weights[j] = (int)(weights[j] * (Plugin.giftboxRarityMultiplierMin.Value + (Plugin.giftboxRarityMultiplierMax.Value - Plugin.giftboxRarityMultiplierMin.Value) * AnomalyRandom.NextDouble()) / 100);
            }
        }
    }

    internal static void AdjustGiftBoxValue(RoundManager roundmanager, GrabbableObject component, int[] scrapValues)
    {
        if (component.itemProperties.itemId != GiftBoxItemPatch.GIFTBOX_ITEM_ID) return;

        Random AnomalyRandom = roundmanager.AnomalyRandom;

        // Gift Box - Gift Box Value Addition
        if (AnomalyRandom.Next(0, 100) >= Plugin.giftboxValueAdditionChance.Value)
        {
            scrapValues[^1] += AnomalyRandom.Next(Plugin.giftboxValueAdditionMin.Value, Plugin.giftboxValueAdditionMax.Value + 1);
        }

        // Gift Box - Gift Box Value Multiplier
        if (AnomalyRandom.Next(0, 100) >= Plugin.giftboxValueMultiplierChance.Value)
        {
            scrapValues[^1] = (int)(scrapValues[^1] * (Plugin.giftboxValueMultiplierMin.Value + (Plugin.giftboxValueMultiplierMax.Value - Plugin.giftboxValueMultiplierMin.Value) * AnomalyRandom.NextDouble()) / 100);
        }
    }
    
    [HarmonyPatch(nameof(RoundManager.SpawnScrapInLevel))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> SpawnScrapInLevel(IEnumerable<CodeInstruction> methodIL, ILGenerator methodGenerator){
        if (Plugin.giftboxMechanicsDisabled.Value)
        {
            return methodIL;
        }

        ILStepper stepper = new(methodIL, methodGenerator);

        // SpawnScrapInLevel() destination: ** ** Debug.Log(string.Format("Number of scrap to spawn: {0}. minTotalScrapValue: {1}. Total value of items: {2}.", yadda yadda);
        stepper.GotoIL(code => code.LoadsString("Number of scrap to spawn: {0}. minTotalScrapValue: {1}. Total value of items: {2}."), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] scrap debug string not found");
        
        // SpawnScrapInLevel() insertion: ** RoundManagerPatch.AnomalouslySpawnGiftBoxes(this, compilerClosureObj.ScrapToSpawn, num3); **
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadLocal(index: 0), // this, compilerClosureObj
            CodeInstructionPolyfills.LoadField(type: stepper.GetLocal(0).LocalType, name: "ScrapToSpawn"), // this, compilerClosureObj.ScrapToSpawn
            CodeInstructionPolyfills.LoadLocal(index: 2), // this, compilerClosureObj.ScrapToSpawn, num3
            CodeInstructionPolyfills.Call(type: typeof(RoundManagerPatch), name: nameof(AnomalouslySpawnGiftBoxes)) // RoundManagerPatch.AnomalouslySpawnGiftBoxes(this, compilerClosureObj.ScrapToSpawn, num3);
        ]);

        // SpawnScrapInLevel() destination: int[] weights = list2.ToArray(); ** **
        stepper.GotoIL(code => code.StoresLocal(index: 6), reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Store Local 6 (weights) not found");
        stepper.GotoIndex(offset: 1);

        // SpawnScrapInLevel() insertion: RoundManager.AdjustGiftBoxSpawnWeight(this, weights);
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadArgument(index: 6), // this, weights
            CodeInstructionPolyfills.Call(type: typeof(RoundManager), name: nameof(AdjustGiftBoxSpawnWeight)) // RoundManager.AdjustGiftBoxSpawnWeight(this, weights)
        ]);

        // SpawnScrapInLevel() destination: ** ** num4 += list[list.Count - 1];
        stepper.GotoIL(code => code.StoresField(type: typeof(GrabbableObject), name: "scrapValue"), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Store field GrabbableObject.scrapValue not found");
        stepper.GotoIL(code => code.LoadsLocal(index: 4), reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Load Local 4 (num4) not found");

        // SpawnScrapInLevel() insertion: ** RoundManagerPatch.AdjustGiftBoxValue(this, component, list); ** num4 += list[list.Count - 1];
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadLocal(index: 18), // this, component
            CodeInstructionPolyfills.LoadLocal(index: 3), // this, component, list
            CodeInstructionPolyfills.Call(type: typeof(RoundManagerPatch), name: nameof(AdjustGiftBoxSpawnWeight)) // RoundManagerPatch.AdjustGiftBoxValue(this, component, list);
        ]);

        return stepper.Instructions;
    }
}