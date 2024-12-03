using System;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using LethalConfig;

namespace LC_GiftBox_Config.libs.LethalConfigNicerizer;

internal static class LethalConfigNicerizer
{
    internal const string LethalConfig_GUID = "ainavt.lc.lethalconfig";

	internal static bool CanHasNicerizationPlease {
        get => Chainloader.PluginInfos.ContainsKey(LethalConfig_GUID);
    }

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	internal static ConfigEntry<T> Nicerize<T>(ConfigEntry<T> entry, bool restartRequired = false)
	{
		if (entry is ConfigEntry<int>) {
			if (CanHasNicerizationPlease) AddConfigItem((entry as ConfigEntry<int>)!, restartRequired);
		} else if (entry is ConfigEntry<float>) {
			if (CanHasNicerizationPlease) AddConfigItem((entry as ConfigEntry<float>)!, restartRequired);
		} else if (entry is ConfigEntry<bool>) {
			if (CanHasNicerizationPlease) AddConfigItem((entry as ConfigEntry<bool>)!, restartRequired);
		} else if (entry is ConfigEntry<string>) {
			if (CanHasNicerizationPlease) AddConfigItem((entry as ConfigEntry<string>)!, restartRequired);
		} else if (entry is ConfigEntry<Enum>) {
			if (CanHasNicerizationPlease) AddConfigItem((entry as ConfigEntry<Enum>)!, restartRequired);
		} else {
			throw new ArgumentException($"[libs.LethalConfigNicerizer.Nicerize] Cannot Nicerize ConfigEntry<{typeof(T)}>!");
		}

		return entry;
	}

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddConfigItem(ConfigEntry<int> entry, bool restartRequired = false)
    {
		if(entry.Description.AcceptableValues != null) {
			LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.IntSliderConfigItem(entry, restartRequired));
		} 
		else {
			LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.IntInputFieldConfigItem(entry, restartRequired));
		}
    }

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddConfigItem(ConfigEntry<float> entry, bool restartRequired = false)
    {
		if(entry.Description.AcceptableValues != null) {
			LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.FloatSliderConfigItem(entry, restartRequired));
		} 
		else {
			LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.FloatInputFieldConfigItem(entry, restartRequired));
		}
    }

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddConfigItem(ConfigEntry<bool> entry, bool restartRequired = false)
    {
		LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.BoolCheckBoxConfigItem(entry, restartRequired));
    }

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddConfigItem(ConfigEntry<string> entry, bool restartRequired = false)
    {
		if(entry.Description.AcceptableValues != null) {
			LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.TextDropDownConfigItem(entry, restartRequired));
		}
		else
		{
			LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.TextInputFieldConfigItem(entry, restartRequired));
		}
    }

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddConfigItem(ConfigEntry<Enum> entry, bool restartRequired = false)
    {
		LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.EnumDropDownConfigItem<Enum>(entry, restartRequired));
    }
}