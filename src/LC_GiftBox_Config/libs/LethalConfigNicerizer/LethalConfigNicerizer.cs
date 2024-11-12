using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using LethalConfig;

namespace LC_GiftBox_Config.libs.LethalConfigNicerizer;

internal static class LethalConfigNicerizer
{
    internal const string LethalConfig_GUID = "ainavt.lc.lethalconfig";

    internal static bool IsActive {
        get => Chainloader.PluginInfos.ContainsKey(LethalConfig_GUID);
    }

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddConfigItem(ConfigEntry<int> entry, bool isSlider = true, bool restartRequired = false)
    {
		if(isSlider) 
		{
        	LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.IntSliderConfigItem(entry, restartRequired));
		} 
		else 
		{
			LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.IntInputFieldConfigItem(entry, restartRequired));
		}
    }

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddConfigItem(ConfigEntry<float> entry, bool isSlider = true, bool restartRequired = false)
    {
		if(isSlider) 
		{
        	LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.FloatSliderConfigItem(entry, restartRequired));
		} 
		else 
		{
			LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.FloatInputFieldConfigItem(entry, restartRequired));
		}
    }

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddConfigItem(ConfigEntry<bool> entry, bool restartRequired = false)
    {
		LethalConfigManager.AddConfigItem(new LethalConfig.ConfigItems.BoolCheckBoxConfigItem(entry, restartRequired));
    }

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void AddConfigItem(ConfigEntry<string> entry, bool isDropdown = false, bool restartRequired = false)
    {
		if (isDropdown)
		{
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