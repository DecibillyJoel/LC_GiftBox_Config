using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

using LCUtils;
using ILUtils;
using ILUtils.HarmonyXtensions;

namespace LC_GiftBox_Config.Patches.GiftBoxItemPatches;

[HarmonyPatch(typeof(RoundManager))]
internal static class RoundManagerPatch
{
    internal static void AnomalouslySpawnGiftBoxes(RoundManager roundmanager, List<Item> ScrapToSpawn, int spawnOneItemIndex)
    {
        // Early return if giftbox could not be referenced
        Item? giftboxItem = Plugin.GIFTBOX_ITEM?.Item;
        if (giftboxItem == null) return;

        // Don't perform gift box anomaly if the "spawn one item" anomaly is already occuring and the anomalous item is something other than the gift box
        Item? spawnOneItem = SpawnableScrapUtils.SpawnableScrapList.ElementAtOrDefault<SpawnableItemWithRarity?>(spawnOneItemIndex)?.spawnableItem;
        if (spawnOneItem != null && !Plugin.GIFTBOX_ITEM.LooselyEquals(spawnOneItem)) return;

        Random AnomalyRandom = roundmanager.AnomalyRandom;

        // Gift Box Anomalous Spawning
        if (AnomalyRandom.Next(0, 100) >= Plugin.giftboxSpawnChance.Value) return;

        int giftboxCount = AnomalyRandom.Next(Plugin.giftboxSpawnMin.Value, Plugin.giftboxSpawnMax.Value + 1);
        ScrapToSpawn.AddRange(Enumerable.Repeat(giftboxItem, giftboxCount).ToList());
    }

    internal static void AdjustGiftBoxSpawnWeight(RoundManager roundmanager, int[] weights)
    {
        // Early return if giftbox could not be referenced
        if (Plugin.GIFTBOX_ITEM?.Item == null) return;

        Random AnomalyRandom = roundmanager.AnomalyRandom;

        if (weights.Length != SpawnableScrapUtils.SpawnableScrapList.Count)
            Plugin.LogError("[Patches.RoundManagerPatch.AdjustGiftBoxSpawnWeight] weights length does not match spawnableScrap length! Wonkiness may occur!");

        for (int j = 0; j < Math.Min(SpawnableScrapUtils.SpawnableScrapList.Count, weights.Length); j++) {
            if (!Plugin.GIFTBOX_ITEM.LooselyEquals(SpawnableScrapUtils.SpawnableScrapList[j].spawnableItem)) continue;

            // Gift Box Rarity Multiplier
            if (AnomalyRandom.Next(0, 100) < Plugin.giftboxRarityMultiplierChance.Value)
                weights[j] = AnomalyRandom.Next((weights[j] * Plugin.giftboxRarityMultiplierMin.Value + 50) / 100, (weights[j] * Plugin.giftboxRarityMultiplierMax.Value + 50) / 100 + 1);
        
            // Gift Box Rarity Addition
            if (AnomalyRandom.Next(0, 100) < Plugin.giftboxRarityAdditionChance.Value)
                weights[j] += AnomalyRandom.Next(Plugin.giftboxRarityAdditionMin.Value, Plugin.giftboxRarityAdditionMax.Value + 1);
        }
    }

    internal static void AdjustGiftBoxValue(RoundManager roundmanager, GrabbableObject component, List<int> scrapValues)
    {
        if (!component.itemProperties.LooselyEquals(Plugin.GIFTBOX_ITEM)) return;

        Random AnomalyRandom = roundmanager.AnomalyRandom;

        // Gift Box Value Multiplier
        if (AnomalyRandom.Next(0, 100) < Plugin.giftboxValueMultiplierChance.Value)
            scrapValues[^1] = AnomalyRandom.Next((scrapValues[^1] * Plugin.giftboxValueMultiplierMin.Value + 50) / 100, (scrapValues[^1] * Plugin.giftboxValueMultiplierMax.Value + 50) / 100 + 1);
    
        // Gift Box Value Addition
        if (AnomalyRandom.Next(0, 100) < Plugin.giftboxValueAdditionChance.Value)
            scrapValues[^1] += AnomalyRandom.Next(Plugin.giftboxValueAdditionMin.Value, Plugin.giftboxValueAdditionMax.Value + 1);
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

        // SpawnScrapInLevel() destination: compilerClosureObj.ScrapToSpawn = new List<Item>(); ** **
        stepper.GotoIL(code => code.StoresField(type: stepper.GetLocal(0).LocalType, name: "ScrapToSpawn"), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Store Field compilerClosureObj.ScrapToSpawn not found");
        stepper.GotoIndex(offset: 1);

        // SpawnScrapInLevel() insertion: ** RoundManagerPatch.AnomalouslySpawnGiftBoxes(this, compilerClosureObj.ScrapToSpawn, num3); **
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadLocal(index: 0), // this, compilerClosureObj
            CodeInstructionPolyfills.LoadField(type: stepper.GetLocal(0).LocalType, name: "ScrapToSpawn"), // this, compilerClosureObj.ScrapToSpawn
            CodeInstructionPolyfills.LoadLocal(index: 2), // this, compilerClosureObj.ScrapToSpawn, num3
            CodeInstructionPolyfills.Call(type: typeof(RoundManagerPatch), name: nameof(AnomalouslySpawnGiftBoxes)) // RoundManagerPatch.AnomalouslySpawnGiftBoxes(this, compilerClosureObj.ScrapToSpawn, num3);
        ]);
        
        // SpawnScrapInLevel() destination: int[] weights = list2.ToArray(); ** **
        stepper.GotoIL(code => code.StoresLocal(index: 6), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Store Local 6 (weights) not found");
        stepper.GotoIndex(offset: 1);

        // SpawnScrapInLevel() insertion: RoundManagerPatch.AdjustGiftBoxSpawnWeight(this, weights);
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadLocal(index: 6), // this, weights
            CodeInstructionPolyfills.Call(type: typeof(RoundManagerPatch), name: nameof(AdjustGiftBoxSpawnWeight)) // RoundManagerPatch.AdjustGiftBoxSpawnWeight(this, weights)
        ]);

        // SpawnScrapInLevel() destination: ** ** num4 += list[list.Count - 1]; component.scrapValue = list[list.Count - 1];
        stepper.GotoIL(code => code.StoresField(type: typeof(GrabbableObject), name: "scrapValue"), errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Store field GrabbableObject.scrapValue not found");
        stepper.GotoIL(code => code.StoresLocal(index: 4), reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Store Local 4 (num4) not found");
        stepper.GotoIL(ILPatterns.NextEmptyStack(startSize: 0), offset: 1, reverse: true, errorMessage: "[Patches.GiftBoxItemPatches.RoundManagerPatch.SpawnScrapInLevel] Load Local 4 (num4) not found");

        // SpawnScrapInLevel() insertion: ** RoundManagerPatch.AdjustGiftBoxValue(this, component, list); ** num4 += list[list.Count - 1]; component.scrapValue = list[list.Count - 1];
        stepper.InsertIL([
            CodeInstructionPolyfills.LoadArgument(index: 0), // this
            CodeInstructionPolyfills.LoadLocal(index: 18), // this, component
            CodeInstructionPolyfills.LoadLocal(index: 3), // this, component, list
            CodeInstructionPolyfills.Call(type: typeof(RoundManagerPatch), name: nameof(AdjustGiftBoxValue)) // RoundManagerPatch.AdjustGiftBoxValue(this, component, list);
        ]);

        return stepper.Instructions;
    }
}