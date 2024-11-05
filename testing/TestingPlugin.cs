using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Components.RawComponents;
using Utility.EnumsStorage;
using Systems;
using Utility;
using Systems.InputSystems;
using Utility.InterfacesStorage;
using Systems.GameStateSystems;
using Components;
using UI.New;
using Systems.WorkersDeliverySystems;
using ScriptableObjectDefinitions;

public static class PluginInfo {

	public const string TITLE = "Testing";
	public const string NAME = "testing";
	public const string SHORT_DESCRIPTION = "Just for debugging";

	public const string VERSION = "0.0.1";

	public const string AUTHOR = "devopsdinosaur";
	public const string GAME_TITLE = "TCG Shop Simulator";
	public const string GAME = "tcgshop";
	public const string GUID = AUTHOR + "." + GAME + "." + NAME;
	public const string REPO = "tcgshop-mods";

	public static Dictionary<string, string> to_dict() {
		Dictionary<string, string> info = new Dictionary<string, string>();
		foreach (FieldInfo field in typeof(PluginInfo).GetFields((BindingFlags) 0xFFFFFFF)) {
			info[field.Name.ToLower()] = (string) field.GetValue(null);
		}
		return info;
	}
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class TestingPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
		logger = this.Logger;
		try {
			this.m_plugin_info = PluginInfo.to_dict();
			DDPlugin.m_log_level = LogLevel.Debug;
			Settings.Instance.load(this);
			this.m_harmony.PatchAll();
			Hotkeys.load();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(PlayerInputManager), "Update")]
	class HarmonyPatch_PlayerInputManager_Update {
		private static void Postfix() {
			Hotkeys.Updaters.keypress_update();	
		}
	}

	[HarmonyPatch(typeof(UI.MainMenuScripts.MainMenu), "Awake")]
	class HarmonyPatch_UI_MainMenuScripts_MainMenu_Awake {
		private static void Postfix() {
			
		}
	}

	[HarmonyPatch(typeof(WorkerDeliveryProcessJob), "FreeSpaceInStorage")]
	class HarmonyPatch_WorkerDeliveryProcessJob_FreeSpaceInStorage {
		private static void Postfix(Entity storageEntity, ResourceType resourceType) {
			DDPlugin._debug_log($"{storageEntity} {resourceType}");
		}
	}

    /*
	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static bool Prefix() {
			
			return true;
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static void Postfix() {
			
		}
	}
	*/
}

public class TestSystem : SystemBase {
    private float m_elapsed = 0;
    private static StreamWriter m_log = null;

    public static void _debug_log(object text) {
        if (m_log == null) {
            string this_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string log_path = Path.Combine(this_dir, "system_test.log");
            m_log = new StreamWriter(log_path);
        }
        m_log.WriteLine(text.ToString());
    }

    protected override void OnUpdate() {
        if ((m_elapsed += Time.DeltaTime) < 1.0) {
            return;
        }
        _debug_log(Time.ElapsedTime);
    }
}
