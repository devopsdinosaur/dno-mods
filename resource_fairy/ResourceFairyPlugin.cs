using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems;
using Utility;
using Systems.InputSystems;
using Utility.InterfacesStorage;
using Systems.GameStateSystems;
using Unity.Entities;
using Components.RawComponents;

public static class PluginInfo {

	public const string TITLE = "Resource Fairy";
	public const string NAME = "resource_fairy";
	public const string SHORT_DESCRIPTION = "Every morning the resource fairy leaves a configurable amount of resources under your town's pillow--no teeth required!";

	public const string VERSION = "0.0.1";

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

	private void Awake() {
		logger = this.Logger;
		try {
			Settings.Instance.load(this);
			this.plugin_info = PluginInfo.to_dict();
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	class ResourceFairy : MonoBehaviour {
		static DayCycleSystem m_daycycle_system = null;
		static bool m_is_running = false;
		static int m_prev_day_count = -1;

		[HarmonyPatch(typeof(PlayerInputManager), "Awake")]
		class HarmonyPatch_PlayerInputManager_Awake {
			private static void Postfix(PlayerInputManager __instance) {
				__instance.gameObject.AddComponent<ResourceFairy>();
			}
		}

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
			for (; ; ) {
				yield return new WaitForSeconds(1f);
				if (!m_is_running || m_prev_day_count == GameState.CommonState.dayCount) {
					continue;
				}
				m_prev_day_count = GameState.CommonState.dayCount;
				if (!Settings.m_enabled.Value) {
					continue;
				}
				increase_resource_value<CurrentBones>(Settings.m_daily_amounts["Bones"].Value);
				increase_resource_value<CurrentFood>(Settings.m_daily_amounts["Food"].Value);
				increase_resource_value<CurrentIron>(Settings.m_daily_amounts["Iron"].Value);
				increase_resource_value<CurrentMoney>(Settings.m_daily_amounts["Money"].Value);
				increase_resource_value<CurrentSouls>(Settings.m_daily_amounts["Souls"].Value);
				increase_resource_value<CurrentSpirit>(Settings.m_daily_amounts["Spirit"].Value);
				increase_resource_value<CurrentStone>(Settings.m_daily_amounts["Stone"].Value);
				increase_resource_value<CurrentWood>(Settings.m_daily_amounts["Wood"].Value);
			}
		}
	}
}
