using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems;
using Utility.InterfacesStorage;
using Systems.GameStateSystems;
using Unity.Entities;
using Components.RawComponents;
using UI.New;

public static class PluginInfo {

	public const string TITLE = "Resource Fairy";
	public const string NAME = "resource_fairy";
	public const string SHORT_DESCRIPTION = "Every morning the resource fairy leaves a configurable amount of resources under your town's pillow--no teeth required!";

	public const string VERSION = "0.0.2";

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
			this.plugin_info = PluginInfo.to_dict();
			DDPlugin.m_log_level = (this.get_nexus_dir() != null ? LogLevel.Debug : LogLevel.Info);
			Settings.Instance.load(this);
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			DDPlugin._info_log($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	class ResourceFairy : MonoBehaviour {
		static DayCycleSystem m_daycycle_system = null;
		static bool m_is_running = false;
		static int m_prev_day_count = -1;

		[HarmonyPatch(typeof(GameCursorController), "StartInit")]
		class HarmonyPatch_GameCursorController_StartInit {
			private static void Postfix(GameCursorController __instance) {
				try {
					//__instance.gameObject.AddComponent<ResourceFairy>();
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_GameCursorController_StartInit.Postfix ERROR - " + e);
				}
			}
		}

		private void Awake() {
			this.StartCoroutine(this.do_some_magical_fairy_stuff());
		}

		[HarmonyPatch(typeof(DayCycleSystem), "OnStartRunning")]
		class HarmonyPatch_DayCycleSystem_OnStartRunning {
			private static void Postfix(DayCycleSystem __instance) {
				try {
					m_daycycle_system = __instance;
					m_is_running = true;
					m_prev_day_count = GameState.CommonState.dayCount;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_DayCycleSystem_OnStartRunning.Postfix ERROR - " + e);
				}
			}
		}

		class ResourceAcessor<T> where T : struct, IComponentData, IUserUIResource {
			private string m_key;
			private Entity m_entity;
			private ComponentDataFromEntity<T> m_component_data;
			private int m_day_gain;
			private int m_previous_value;
			
			public ResourceAcessor(string key) {
				this.m_key = key;
				this.m_entity = m_daycycle_system.GetSingletonEntity<T>();
				this.m_component_data = m_daycycle_system.GetComponentDataFromEntity<T>(false);
				this.m_day_gain = 0;
				this.m_previous_value = -1;
			}

			public void check_value_delta() {
				int current_value = this.m_component_data[this.m_entity].CurrentAmount();
				if (this.m_previous_value == -1) {
					this.m_previous_value = current_value;
					return;
				}
				int delta = current_value - this.m_previous_value;
				if (delta > 0) {
					this.m_day_gain += delta;
					if (Settings.m_enabled.Value) {
						DDPlugin._debug_log($"{this.m_key}.check_value_delta() - delta: {delta}, day_gain: {this.m_day_gain}");
					}
				}
				this.m_previous_value = current_value;
			}
			
			private int get_daily_amount() {
				switch (Settings.m_daily_method[this.m_key].Value.ToLower()) {
				case "amount":
					return Settings.m_daily_flat_amounts[this.m_key].Value;
				case "multiplier":
					return Mathf.FloorToInt((float) this.m_day_gain * Settings.m_daily_multipliers[this.m_key].Value);
				}
				return 0;
			}

			public void new_day_change_value() {
				T stat = this.m_component_data[this.m_entity];
				int delta = this.get_daily_amount();
				this.m_day_gain = 0;
				if (Settings.m_enabled.Value && delta > 0) {
					stat.IncreaseAmount(delta);
					DDPlugin._debug_log($"{this.m_key}.change_value({delta}) = {stat.CurrentAmount()}");
					this.m_component_data[this.m_entity] = stat;
				}
				this.m_previous_value = stat.CurrentAmount();
			}
		}
		
		private IEnumerator do_some_magical_fairy_stuff() {
			ResourceAcessor<CurrentBones> bones = new ResourceAcessor<CurrentBones>("Bones");
			ResourceAcessor<CurrentFood> food = new ResourceAcessor<CurrentFood>("Food");
			ResourceAcessor<CurrentIron> iron = new ResourceAcessor<CurrentIron>("Iron");
			ResourceAcessor<CurrentMoney> money = new ResourceAcessor<CurrentMoney>("Money");
			ResourceAcessor<CurrentSouls> souls = new ResourceAcessor<CurrentSouls>("Souls");
			ResourceAcessor<CurrentSpirit> spirit = new ResourceAcessor<CurrentSpirit>("Spirit");
			ResourceAcessor<CurrentStone> stone = new ResourceAcessor<CurrentStone>("Stone");
			ResourceAcessor<CurrentWood> wood = new ResourceAcessor<CurrentWood>("Wood");
			Dictionary<string, int> prev_values = new Dictionary<string, int>();
			for (;;) {
				yield return new WaitForSeconds(0.1f);
				if (!m_is_running) {
					continue;
				}
				bones.check_value_delta();
				food.check_value_delta();
				iron.check_value_delta();
				money.check_value_delta();
				souls.check_value_delta();
				spirit.check_value_delta();
				stone.check_value_delta();
				wood.check_value_delta();
				if (m_prev_day_count == GameState.CommonState.dayCount) {
					continue;
				}
				DDPlugin._debug_log($"Day Change - prev_day: {m_prev_day_count}, cur_day: {GameState.CommonState.dayCount}");
				m_prev_day_count = GameState.CommonState.dayCount;
				bones.new_day_change_value();
				food.new_day_change_value();
				iron.new_day_change_value();
				money.new_day_change_value();
				souls.new_day_change_value();
				spirit.new_day_change_value();
				stone.new_day_change_value();
				wood.new_day_change_value();
			}
		}
	}
}
