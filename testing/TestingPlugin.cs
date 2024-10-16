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
			this.plugin_info = PluginInfo.to_dict();
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

	[HarmonyPatch(typeof(PeoplePopulationSystem), "GetBuildingCapacity")]
	class HarmonyPatch_PeoplePopulationSystem_GetBuildingCapacity {
		private static bool Prefix(Entity houseEntity, in HouseBase houseBaseData, BufferFromEntity<MaxHousematesModifier> maxHousematesModifiersBuffersRO, int __result) {
			DDPlugin._debug_log($"HarmonyPatch_PeoplePopulationSystem_GetBuildingCapacity - result: {__result}");
			return true;
		}
	}

	class TestThing : MonoBehaviour {
		static DayCycleSystem m_daycycle_system = null;
		static bool m_is_running = false;
		
		private void Awake() {
			this.StartCoroutine(this.test_routine());
		}

		[HarmonyPatch(typeof(DayCycleSystem), "OnStartRunning")]
		class HarmonyPatch_DayCycleSystem_OnStartRunning {
			private static void Postfix(DayCycleSystem __instance) {
				try {
					m_daycycle_system = __instance;
					m_is_running = true;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_DayCycleSystem_OnStartRunning.Postfix ERROR - " + e);
				}
			}
		}

		class ResourceAccessor<TResource, TReserve, TUi> 
			where TResource : struct, IComponentData, IResourceStorage 
			where TReserve : struct, IComponentData, IResourceReserve 
			where TUi : struct, IComponentData, IUserUIResource
		{
			private string m_key;
			private ResourceType m_resource_type;
			private Entity m_entity;
			private ComponentDataFromEntity<TResource> m_component_data;
			private ComponentDataFromEntity<StorageBase> m_storage_data;
			private ComponentDataFromEntity<TReserve> m_reserve_data;
			private Entity m_ui_entity;
			private ComponentDataFromEntity<TUi> m_ui_data;
			private int m_previous_value;

			public ResourceAccessor(string key, ResourceType resource_type) {
				this.m_key = key;
				this.m_resource_type = resource_type;
				this.m_entity = m_daycycle_system.GetSingletonEntity<TResource>();
				this.m_component_data = m_daycycle_system.GetComponentDataFromEntity<TResource>(false);
				this.m_storage_data = m_daycycle_system.GetComponentDataFromEntity<StorageBase>(false);
				increase storage base?
				this.m_reserve_data = m_daycycle_system.GetComponentDataFromEntity<TReserve>(false);
				this.m_ui_entity = m_daycycle_system.GetSingletonEntity<TUi>();
				this.m_ui_data = m_daycycle_system.GetComponentDataFromEntity<TUi>(false);
				this.m_previous_value = -1;
			}

			public int get_value_positive_delta() {
				int current_value = this.m_component_data[this.m_entity].CurrentAmount();
				if (this.m_previous_value == -1) {
					this.m_previous_value = current_value;
					return 0;
				}
				int delta = Mathf.Max(0, current_value - this.m_previous_value);
				this.m_previous_value = current_value;
				return delta;
			}

			public int free_space_in_storage() {
				switch (this.m_resource_type) {
				case ResourceType.Food:
					return this.m_storage_data[this.m_entity].value.Value.foodCapacity - this.m_component_data[this.m_entity].CurrentAmount() - this.m_reserve_data[this.m_entity].CurrentReserve();
				case ResourceType.Corpse:
					return this.m_storage_data[this.m_entity].value.Value.corpseCapacity- this.m_component_data[this.m_entity].CurrentAmount() - this.m_reserve_data[this.m_entity].CurrentReserve();
				case ResourceType.Wood:
				case ResourceType.Stone:
				case ResourceType.Iron:
					return 0;
				}
				return 0;
			}

			public void adjust_changes() {
				const float MULTIPLIER = 5.0f;
				int original_delta = this.get_value_positive_delta();
				if (original_delta == 0) {
					return;
				}
				int added_delta = Mathf.FloorToInt((float) original_delta * MULTIPLIER) - original_delta;
				int free_space = this.free_space_in_storage();
				added_delta = Mathf.Min(added_delta, free_space);
				DDPlugin._debug_log($"original_delta: {original_delta}, modified_delta: {added_delta}, free: {free_space}");
				if (added_delta == 0) {
					return;
				}
				TResource stat = this.m_component_data[this.m_entity];
				stat.IncreaseAmount(added_delta);
				this.m_component_data[this.m_entity] = stat;
				TUi ui_stat = this.m_ui_data[this.m_ui_entity];
				ui_stat.IncreaseAmount(added_delta);
				this.m_ui_data[this.m_ui_entity] = ui_stat;
			}
		}

		private IEnumerator test_routine() {
			ResourceAccessor<FoodStorage, FoodReserve, CurrentFood> food = new ResourceAccessor<FoodStorage, FoodReserve, CurrentFood>("Food", ResourceType.Food);
			ResourceAccessor<WoodStorage, WoodReserve, CurrentWood> wood = new ResourceAccessor<WoodStorage, WoodReserve, CurrentWood>("Wood", ResourceType.Wood);	
			for (;;) {
				yield return new WaitForSeconds(0.1f);
				if (!Settings.m_enabled.Value || !m_is_running) {
					continue;
				}
				food.adjust_changes();
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

	[HarmonyPatch(typeof(WorkerDeliveryProcessJob), "FreeSpaceInStorage")]
	class HarmonyPatch_WorkerDeliveryProcessJob_FreeSpaceInStorage {
		private static void Postfix(Entity storageEntity, ResourceType resourceType) {
			DDPlugin._debug_log($"{storageEntity} {resourceType}");
		}
	}

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
