using System;
using System.Collections.Generic;
using System.Linq;

using LogLevel = BepInEx.Logging.LogLevel;

namespace LC_GiftBox_Config.libs.Probability;

public static class Probability
{
    public static int GetRandomWeightedIndex(List<double> weights, Random? randomSeed = null)
    {
        if (weights == null || weights.Count == 0) {
            Plugin.Log(LogLevel.Warning, "[libs.Probability.GetRandomWeightedIndex] Could not get random weighted index; array is empty or null.");
			return -1;
		}

        if (weights.Any(weight => weight < 0.0))
        {
            Plugin.Log(LogLevel.Warning, "[libs.Probability.GetRandomWeightedIndex] Could not get random weighted index; array contains negative weights.");
            return -1;
        }

        randomSeed ??= new();

        double totalWeight = weights.Sum();
        if (totalWeight <= 0.0) {
            return randomSeed.Next(0, weights.Count);
        }
        
        double randomValue = randomSeed.NextDouble() * weights.Sum();
        double accumulatedValue = 0.0;

        return weights.FindIndex((weight) => (accumulatedValue += weight) >= randomValue);
    }

    public static int GetRandomWeightedIndex(List<int> weights, Random? randomSeed = null)
    {
        if (weights == null || weights.Count == 0) {
            Plugin.Log(LogLevel.Warning, "[libs.Probability.GetRandomWeightedIndex] Could not get random weighted index; array is empty or null.");
			return -1;
		}

        if (weights.Any(weight => weight < 0))
        {
            Plugin.Log(LogLevel.Warning, "[libs.Probability.GetRandomWeightedIndex] Could not get random weighted index; array contains negative weights.");
            return -1;
        }

        randomSeed ??= new();

        int totalWeight = weights.Sum();
        if (totalWeight <= 0) {
            return randomSeed.Next(0, weights.Count);
        }
        
        int randomValue = randomSeed.Next(0, totalWeight);
        int accumulatedValue = 0;

        return weights.FindIndex((weight) => (accumulatedValue += weight) >= randomValue);
    }
}