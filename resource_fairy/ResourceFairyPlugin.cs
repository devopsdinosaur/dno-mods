using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems;
using Utility.InterfacesStorage;
using Unity.Entities;
using Components.RawComponents;
using UI.New;
using Utility.EnumsStorage;
using Components;
using TMPro;

public static class PluginInfo {

	public const string TITLE = "Resource Fairy";
	public const string NAME = "resource_fairy";
	public const string SHORT_DESCRIPTION = "Configurable multipliers for changing the amount of resources received.  More options to come for increasing max storage/population/etc!";

	public const string VERSION = "0.0.3";

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
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
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

		[HarmonyPatch(typeof(GameCursorController), "StartInit")]
		class HarmonyPatch_GameCursorController_StartInit {
			private static bool Prefix(GameCursorController __instance) {
				__instance.gameObject.AddComponent<ResourceFairy>();
				return true;
			}
		}

		private void Awake() {
			this.StartCoroutine(this.main_routine());
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
			private static ComponentDataFromEntity<CurrentSouls> m_souls_ui_data;
			private static ComponentDataFromEntity<CurrentStone> m_stone_ui_data;
			private static ComponentDataFromEntity<CurrentWood> m_wood_ui_data;

			private static class UiResourceBar {
				public class UiResource {
					public ResourceType m_resource_type;
					public string m_key;
					public string m_parent_object_name;
					public Transform m_parent = null;
					public TextMeshProUGUI m_text;

					public UiResource(ResourceType resource_type, string key, string parent_object_name) {
						this.m_resource_type = resource_type;
						this.m_key = key;
						this.m_parent_object_name = parent_object_name;
					}

					public virtual void initialize(Transform parent) {
						this.m_parent = parent;
						this.m_text = this.m_parent.Find("Text")?.GetComponent<TextMeshProUGUI>();
					}

					public virtual void update(int value) {
						if (this.m_text == null) {
							return;
						}
						string format(int value) {
							return (value < 1000 ? value.ToString() : $"{((float) value / 1000.0f):#0.0}k");
						}
						this.m_text.text = (this.m_resource_type == ResourceType.Food ? $"{format(value)}/{format(ResourceData.storage_get_capacity(ResourceType.Food))}" : format(value));
					}
				}
				public class UiResource_WoodStoneIron : UiResource {
					public UiResource_WoodStoneIron(ResourceType resource_type, string key, string parent_object_name) :
						base(resource_type, key, parent_object_name) {
					}

					public override void initialize(Transform parent) {
						this.m_parent = parent;
						this.m_text = this.m_parent.Find(this.m_key + "Count")?.GetComponent<TextMeshProUGUI>();
					}
				}
				private static Dictionary<ResourceType, UiResource> m_resources = new Dictionary<ResourceType, UiResource>() {
					{ResourceType.People, new UiResource(ResourceType.People, "Population", "Population")},
					{ResourceType.Food, new UiResource(ResourceType.Food, "Food", "Food")},
					{ResourceType.Wood, new UiResource(ResourceType.Wood, "Wood", "WoodStoneIron")},
					{ResourceType.Stone, new UiResource(ResourceType.Stone, "Stone", "WoodStoneIron")},
					{ResourceType.Iron, new UiResource(ResourceType.Iron, "Iron", "WoodStoneIron")},
					{ResourceType.Souls, new UiResource(ResourceType.Souls, "Souls", "Souls")},
					{ResourceType.Money, new UiResource(ResourceType.Money, "Money", "Gold")}
				};
				private static Transform m_resource_bar_transform = null;

				public static void initialize() {
					if ((m_resource_bar_transform = UnityUtils.find_by_path("PrimeCanvas GameWorld/Content/PhotoModAffectedUI/TopPanel/ResoursesBar/")) == null) {
						DDPlugin._warn_log("* UiResourceBar.initialize WARNING - unable to locate resource bar GameObject at PrimeCanvas GameWorld/Content/PhotoModAffectedUI/TopPanel/ResoursesBar; UI-specific features (label/progressbar updates) will be disabled.");
						return;
					}
					foreach (Transform child in m_resource_bar_transform) {
						foreach (UiResource resource in m_resources.Values) {
							if (child.name == resource.m_parent_object_name) {
								resource.initialize(child);
							}
						}
					}
					foreach (UiResource resource in m_resources.Values) {
						if (resource.m_parent == null) {
							DDPlugin._warn_log($"* UiResourceBar.initialize WARNING - {resource.m_parent_object_name} GameObject not found; UI-specific features (label/progressbar) for this resource will be disabled.");
						}
					}
				}

				public static void update_resource(ResourceType resource_type, int value) {
					if (m_resources.TryGetValue(resource_type, out UiResource resource)) {
						resource.update(value);
					}
				}
			}

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
				m_souls_ui_data = system.GetComponentDataFromEntity<CurrentSouls>(false);
				m_stone_ui_data = system.GetComponentDataFromEntity<CurrentStone>(false);
				m_wood_ui_data = system.GetComponentDataFromEntity<CurrentWood>(false);
				UiResourceBar.initialize();
			}

			public static int storage_get_capacity(ResourceType resource_type) {
				switch (resource_type) {
					case ResourceType.Food:
						return m_storage_base_data[m_storage_entities[resource_type]].value.Value.foodCapacity;
					case ResourceType.Iron:
					case ResourceType.Stone:
					case ResourceType.Wood:
						return m_storage_base_data[m_storage_entities[resource_type]].value.Value.woodStoneIronCapacity;
					case ResourceType.Money:
					case ResourceType.Souls:
						return int.MaxValue;
				}
				return 0;
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
						return storage_get_capacity(resource_type) - m_food_storage_data[m_storage_entities[ResourceType.Food]].stored - m_food_reserve_data[m_storage_entities[ResourceType.Food]].reserved;
					case ResourceType.Iron:
					case ResourceType.Stone:
					case ResourceType.Wood:
						return storage_get_capacity(resource_type) -
							m_wood_storage_data[m_storage_entities[ResourceType.Wood]].stored - m_wood_reserve_data[m_storage_entities[ResourceType.Wood]].reserved -
							m_stone_storage_data[m_storage_entities[ResourceType.Stone]].stored - m_stone_reserve_data[m_storage_entities[ResourceType.Stone]].reserved -
							m_iron_storage_data[m_storage_entities[ResourceType.Iron]].stored - m_iron_reserve_data[m_storage_entities[ResourceType.Iron]].reserved;
				}
				return storage_get_capacity(resource_type);
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
					UiResourceBar.update_resource(resource_type, ui_stat.CurrentAmount());
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
					case ResourceType.Souls:
						increase_value_ui_only<CurrentSouls>(m_souls_ui_data);
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
				int original_delta = this.get_value_positive_delta();
				if (original_delta == 0 || Settings.m_resource_multipliers[this.m_key].Value <= 1) {
					return;
				}
				int added_delta = Mathf.FloorToInt((float) original_delta * Settings.m_resource_multipliers[this.m_key].Value) - original_delta;
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

		private IEnumerator main_routine() {
			ResourceData.initialize(m_daycycle_system);
			Dictionary<ResourceType, ResourceAccessor> resources = new Dictionary<ResourceType, ResourceAccessor>() {
				{ResourceType.Food, new ResourceAccessor("Food", ResourceType.Food)},
				{ResourceType.Iron, new ResourceAccessor("Iron", ResourceType.Iron)},
				{ResourceType.Money, new ResourceAccessor("Money", ResourceType.Money)},
				{ResourceType.Souls, new ResourceAccessor("Souls", ResourceType.Souls)},
				{ResourceType.Stone, new ResourceAccessor("Stone", ResourceType.Stone)},
				{ResourceType.Wood, new ResourceAccessor("Wood", ResourceType.Wood)},
			};
			//foreach (ResourceAccessor resource in resources.Values) {
			//	ResourceData.storage_set_capacity(resource.m_resource_type, 999999);
			//}
			for (; ; ) {
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
}
