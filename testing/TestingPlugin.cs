using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
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

	[HarmonyPatch(typeof(PeoplePopulationSystem), "GetBuildingCapacity")]
	class HarmonyPatch_PeoplePopulationSystem_GetBuildingCapacity {
		private static bool Prefix(Entity houseEntity, in HouseBase houseBaseData, BufferFromEntity<MaxHousematesModifier> maxHousematesModifiersBuffersRO, int __result) {
			DDPlugin._debug_log($"HarmonyPatch_PeoplePopulationSystem_GetBuildingCapacity - result: {__result}");
			return true;
		}
	}

	/*
	class TestThing : MonoBehaviour {
		private void Awake() {
			this.StartCoroutine(this.test_routine());
		}

		private IEnumerator test_routine() {
			for (;;) {
				yield return new WaitForSeconds(1f);
				DDPlugin._debug_log(Time.time);
			}
		}
	}
	
	[HarmonyPatch(typeof(GameCursorController), "StartInit")]
	class HarmonyPatch_1 {
		private static bool Prefix(GameCursorController __instance) {
			__instance.gameObject.AddComponent<TestThing>();
			return true;
		}
	}
	*/
	/*
	[HarmonyPatch(typeof(UpdatePopulationJob), "Run")]
	class HarmonyPatch_2 {
		private static bool Prefix() {
			DDPlugin._debug_log("2");
			return true;
		}
	}
	*/

	public static void testfunc() {
		DDPlugin._debug_log("**************** testfunc");
		//EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		
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
