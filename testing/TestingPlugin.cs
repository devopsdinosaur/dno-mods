﻿using BepInEx;
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

	/*
	class ResourceFairy : MonoBehaviour {
		static DayCycleSystem m_daycycle_system = null;
		static bool m_is_running = false;
		static int m_prev_day_count = -1;

		private void Awake() {
			this.StartCoroutine(this.do_some_magical_fairy_stuff());
		}

		[HarmonyPatch(typeof(DayCycleSystem), "OnStartRunning")]
		class HarmonyPatch_DayCycleSystem_OnStartRunning {
			private static void Postfix(DayCycleSystem __instance) {
				m_daycycle_system = __instance;
				m_is_running = true;
				m_prev_day_count = GameState.CommonState.dayCount;
			}
		}

		private static void increase_resource_value<T>(int amount) where T : struct, IComponentData, IUserUIResource {
			ResourceUtility.ChangeCurrentResourceValue(m_daycycle_system.GetComponentDataFromEntity<T>(false), m_daycycle_system.GetSingletonEntity<T>(), amount);
		}

		private IEnumerator do_some_magical_fairy_stuff() {
			for (;;) {
				yield return new WaitForSeconds(1f);
				if (!m_is_running || m_prev_day_count == GameState.CommonState.dayCount) {
					continue;
				}
				m_prev_day_count = GameState.CommonState.dayCount;
				increase_resource_value<CurrentBones>(10);
				increase_resource_value<CurrentFood>(10);
				increase_resource_value<CurrentIron>(10);
				increase_resource_value<CurrentMoney>(10);
				increase_resource_value<CurrentSouls>(10);
				increase_resource_value<CurrentSpirit>(10);
				increase_resource_value<CurrentStone>(10);
				increase_resource_value<CurrentWood>(10);
			}
		}
	}

	[HarmonyPatch(typeof(PlayerInputManager), "Awake")]
	class HarmonyPatch_PlayerInputManager_Awake {
		private static void Postfix(PlayerInputManager __instance) {
			__instance.gameObject.AddComponent<ResourceFairy>();
		}
	}
	*/

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

	

	public static void testfunc() {
		DDPlugin._debug_log("**************** testfunc");
				
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
