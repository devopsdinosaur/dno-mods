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

		public static class ResourceData {
			private static Dictionary<ResourceType, Entity> m_storage_entities;
			private static Dictionary<ResourceType, Entity> m_ui_entities;
			private static ComponentDataFromEntity<StorageBase> m_storage_base_data;
			private static ComponentDataFromEntity<FoodStorage> m_food_storage_data;
			private static ComponentDataFromEntity<IronStorage> m_iron_storage_data;
			private static ComponentDataFromEntity<StoneStorage> m_stone_storage_data;
			private static ComponentDataFromEntity<WoodStorage> m_wood_storage_data;
			private static ComponentDataFromEntity<FoodReserve> m_food_reserve_data;
			private static ComponentDataFromEntity<IronReserve> m_iron_reserve_data;
			private static ComponentDataFromEntity<StoneReserve> m_stone_reserve_data;
			private static ComponentDataFromEntity<WoodReserve> m_wood_reserve_data;
			private static ComponentDataFromEntity<CurrentFood> m_food_ui_data;
			private static ComponentDataFromEntity<CurrentIron> m_iron_ui_data;
			private static ComponentDataFromEntity<CurrentMoney> m_money_ui_data;
			private static ComponentDataFromEntity<CurrentStone> m_stone_ui_data;
			private static ComponentDataFromEntity<CurrentWood> m_wood_ui_data;

			public static void initialize(SystemBase system) {
				m_storage_entities = new Dictionary<ResourceType, Entity>();
				m_storage_entities[ResourceType.Food] = system.GetSingletonEntity<FoodStorage>();
				m_storage_entities[ResourceType.Iron] = system.GetSingletonEntity<IronStorage>();
				m_storage_entities[ResourceType.Stone] = system.GetSingletonEntity<StoneStorage>();
				m_storage_entities[ResourceType.Wood] = system.GetSingletonEntity<WoodStorage>();
				m_ui_entities = new Dictionary<ResourceType, Entity>();
				m_ui_entities[ResourceType.Food] = system.GetSingletonEntity<CurrentFood>();
				m_ui_entities[ResourceType.Iron] = system.GetSingletonEntity<CurrentIron>();
				m_ui_entities[ResourceType.Money] = system.GetSingletonEntity<CurrentMoney>();
				m_ui_entities[ResourceType.Stone] = system.GetSingletonEntity<CurrentStone>();
				m_ui_entities[ResourceType.Wood] = system.GetSingletonEntity<CurrentWood>();
				m_storage_base_data = system.GetComponentDataFromEntity<StorageBase>(false);
				m_food_storage_data = system.GetComponentDataFromEntity<FoodStorage>(false);
				m_iron_storage_data = system.GetComponentDataFromEntity<IronStorage>(false);
				m_stone_storage_data = system.GetComponentDataFromEntity<StoneStorage>(false);
				m_wood_storage_data = system.GetComponentDataFromEntity<WoodStorage>(false);
				m_food_reserve_data = system.GetComponentDataFromEntity<FoodReserve>(false);
				m_iron_reserve_data = system.GetComponentDataFromEntity<IronReserve>(false);
				m_stone_reserve_data = system.GetComponentDataFromEntity<StoneReserve>(false);
				m_wood_reserve_data = system.GetComponentDataFromEntity<WoodReserve>(false);
				m_food_ui_data = system.GetComponentDataFromEntity<CurrentFood>(false);
				m_iron_ui_data = system.GetComponentDataFromEntity<CurrentIron>(false);
				m_money_ui_data = system.GetComponentDataFromEntity<CurrentMoney>(false);
				m_stone_ui_data = system.GetComponentDataFromEntity<CurrentStone>(false);
				m_wood_ui_data = system.GetComponentDataFromEntity<CurrentWood>(false);
			}

			public static int storage_get_current_value(ResourceType resource_type) {
				switch (resource_type) {
				case ResourceType.Food:
					return m_food_storage_data[m_storage_entities[ResourceType.Food]].stored;
				case ResourceType.Iron:
					return m_iron_storage_data[m_storage_entities[ResourceType.Iron]].stored;
				case ResourceType.Money:
					return m_money_ui_data[m_ui_entities[ResourceType.Money]].CurrentAmount();
				case ResourceType.Stone:
					return m_stone_storage_data[m_storage_entities[ResourceType.Stone]].stored;
				case ResourceType.Wood:
					return m_wood_storage_data[m_storage_entities[ResourceType.Wood]].stored;
				}
				return 0;
			}

			public static int storage_get_free_space(ResourceType resource_type) {
				switch (resource_type) {
				case ResourceType.Food:
					return m_storage_base_data[m_storage_entities[resource_type]].value.Value.foodCapacity - m_food_storage_data[m_storage_entities[ResourceType.Food]].stored - m_food_reserve_data[m_storage_entities[ResourceType.Food]].reserved;
				case ResourceType.Iron:
				case ResourceType.Stone:
				case ResourceType.Wood:
					return m_storage_base_data[m_storage_entities[resource_type]].value.Value.woodStoneIronCapacity - 
						m_wood_storage_data[m_storage_entities[ResourceType.Wood]].stored - m_wood_reserve_data[m_storage_entities[ResourceType.Wood]].reserved -
						m_stone_storage_data[m_storage_entities[ResourceType.Stone]].stored - m_stone_reserve_data[m_storage_entities[ResourceType.Stone]].reserved -
						m_iron_storage_data[m_storage_entities[ResourceType.Iron]].stored - m_iron_reserve_data[m_storage_entities[ResourceType.Iron]].reserved;
				case ResourceType.Money:
					return int.MaxValue;
				}
				return 0;
			}

			public static void storage_increase_value(ResourceType resource_type, int delta) {
				void increase_value<TStorage, TUi>(ComponentDataFromEntity<TStorage> storage_data, ComponentDataFromEntity<TUi> ui_data) where TStorage : struct, IComponentData, IResourceStorage where TUi : struct, IComponentData, IUserUIResource {
					TStorage storage_stat = storage_data[m_storage_entities[resource_type]];
					storage_stat.IncreaseAmount(delta);
					storage_data[m_storage_entities[resource_type]] = storage_stat;
					increase_value_ui_only<TUi>(ui_data);
				}
				void increase_value_ui_only<TUi>(ComponentDataFromEntity<TUi> ui_data) where TUi : struct, IComponentData, IUserUIResource {
					TUi ui_stat = ui_data[m_ui_entities[resource_type]];
					ui_stat.IncreaseAmount(delta);
					ui_data[m_ui_entities[resource_type]] = ui_stat;
				}
				switch (resource_type) {
				case ResourceType.Food: 
					increase_value<FoodStorage, CurrentFood>(m_food_storage_data, m_food_ui_data);
					break;
				case ResourceType.Iron:
					increase_value<IronStorage, CurrentIron>(m_iron_storage_data, m_iron_ui_data);
					break;
				case ResourceType.Money:
					increase_value_ui_only<CurrentMoney>(m_money_ui_data);
					break;
				case ResourceType.Stone:
					increase_value<StoneStorage, CurrentStone>(m_stone_storage_data, m_stone_ui_data);
					break;
				case ResourceType.Wood:
					increase_value<WoodStorage, CurrentWood>(m_wood_storage_data, m_wood_ui_data);
					break;
				}
			}

			public static void storage_set_capacity(ResourceType resource_type, int value) {
				switch (resource_type) {
				case ResourceType.Food:
					StorageBase food_storage = m_storage_base_data[m_storage_entities[resource_type]];
					food_storage.value.Value.foodCapacity = value;
					m_storage_base_data[m_storage_entities[resource_type]] = food_storage;
					break;
				case ResourceType.Iron:
				case ResourceType.Stone:
				case ResourceType.Wood:
					StorageBase wsi_storage = m_storage_base_data[m_storage_entities[resource_type]];
					wsi_storage.value.Value.woodStoneIronCapacity = value;
					m_storage_base_data[m_storage_entities[resource_type]] = wsi_storage;
					break;
				}
			}
		}

		public class ResourceAccessor {
			public string m_key;
			public ResourceType m_resource_type;
			private int m_previous_value;

			public ResourceAccessor(string key, ResourceType resource_type) {
				this.m_key = key;
				this.m_resource_type = resource_type;
				this.m_previous_value = -1;
			}

			public int get_value_positive_delta() {
				int current_value = ResourceData.storage_get_current_value(this.m_resource_type);
				if (this.m_previous_value == -1) {
					this.m_previous_value = current_value;
					return 0;
				}
				int delta = Mathf.Max(0, current_value - this.m_previous_value);
				this.m_previous_value = current_value;
				return delta;
			}

			public void adjust_changes() {
				const float MULTIPLIER = 5.0f;
				int original_delta = this.get_value_positive_delta();
				if (original_delta == 0) {
					return;
				}
				int added_delta = Mathf.FloorToInt((float) original_delta * MULTIPLIER) - original_delta;
				int free_space = ResourceData.storage_get_free_space(this.m_resource_type);
				added_delta = Mathf.Min(added_delta, free_space);
				DDPlugin._debug_log($"[{this.m_key}] original_delta: {original_delta}, modified_delta: {added_delta}, free: {free_space}");
				if (added_delta <= 0) {
					return;
				}
				ResourceData.storage_increase_value(this.m_resource_type, added_delta);
				this.m_previous_value += added_delta;
			}
		}

		PrimeCanvas GameWorld/Content/PhotoModAffectedUI/TopPanel/ResoursesBar/Gold/

		private IEnumerator test_routine() {
			ResourceData.initialize(m_daycycle_system);
			Dictionary<ResourceType, ResourceAccessor> resources = new Dictionary<ResourceType, ResourceAccessor>() {
				{ResourceType.Food, new ResourceAccessor("Food", ResourceType.Food)},
				{ResourceType.Iron, new ResourceAccessor("Iron", ResourceType.Iron)},
				{ResourceType.Money, new ResourceAccessor("Money", ResourceType.Money)},
				{ResourceType.Stone, new ResourceAccessor("Stone", ResourceType.Stone)},
				{ResourceType.Wood, new ResourceAccessor("Wood", ResourceType.Wood)},
			};
			foreach (ResourceAccessor resource in resources.Values) {
				ResourceData.storage_set_capacity(resource.m_resource_type, 999999);
			}
			for (;;) {
				yield return new WaitForSeconds(0.1f);
				if (!Settings.m_enabled.Value || !m_is_running) {
					continue;
				}
				foreach (ResourceAccessor resource in resources.Values) {
					resource.adjust_changes();
				}
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
