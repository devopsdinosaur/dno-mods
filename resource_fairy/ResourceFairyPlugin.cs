using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;

public static class PluginInfo {

	public const string TITLE = "Resource Fairy";
	public const string NAME = "resource_fairy";
	public const string SHORT_DESCRIPTION = "Configurable multipliers for changing the amount of resources received.  More options to come for increasing max storage/population/etc!";

	public const string VERSION = "0.0.6";

	public const string AUTHOR = "devopsdinosaur";
	public const string GAME_TITLE = "Diplomacy is Not an Option";
	public const string GAME = "dno";
	public const string GUID = AUTHOR + "." + GAME + "." + NAME;
	public const string REPO = "dno-mods";

	public static Dictionary<string, string> to_dict() {
		Dictionary<string, string> info = new Dictionary<string, string>();
		foreach (FieldInfo field in typeof(PluginInfo).GetFields((BindingFlags) 0xFFFFFFF)) {
			info[field.Name.ToLower()] = (string) field.GetValue(null);
		}
		return info;
	}
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class ResourceFairyPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);
	
	public void ecs_load() {
		logger = this.Logger;
		try {
			logger.LogInfo($"Loading ECS plugin [{PluginInfo.TITLE} {PluginInfo.VERSION}]");
			this.m_plugin_info = PluginInfo.to_dict();
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
			this.create_nexus_page();
			DDPlugin._info_log($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** load FATAL - " + e);
		}
	}
}
