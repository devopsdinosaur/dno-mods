using Components;
using Components.RawComponents;
using Components.Structs;
using Components.SingletonComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Systems;
using Systems.ComponentSystemGroups;
using Systems.WorkersDeliverySystems;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Utility.EnumsStorage;
using Utility.InterfacesStorage;

[UpdateBefore(typeof(WorkersDeliverySystem))]
[UpdateInGroup(typeof(ResourceDeliveryGroup))]
public class TestSystem : SystemBaseSimulation {
	private const float UPDATE_FREQUENCY = 0.1f;

	public class SettingsWrapper {
		private static SettingsWrapper m_instance = null;
		public static SettingsWrapper Instance {
			get {
				if (m_instance == null) {
					m_instance = new SettingsWrapper();
				}
				return m_instance;
			}
		}
		public Dictionary<ResourceType, float> m_resource_multipliers = new Dictionary<ResourceType, float>();
        public Dictionary<ResourceType, float> m_capacity_multipliers = new Dictionary<ResourceType, float>();

		public SettingsWrapper() {
			foreach (string key in Settings.m_resource_multipliers.Keys) {
				if (Enum.TryParse<ResourceType>(key, out ResourceType resource_type)) {
					this.m_resource_multipliers[resource_type] = Settings.m_resource_multipliers[key].Value;
					continue;
				}
				DDPlugin._error_log($"** Settings ERROR - unknown resource multiplier key '{key}'.");
			}
            foreach (string key in Settings.m_capacity_multipliers.Keys) {
				if (!Enum.TryParse<ResourceType>(key, out ResourceType resource_type)) {
					if (key == "Wood_Stone_Iron") {
						resource_type = ResourceType.Wood;
					} else {
                        DDPlugin._error_log($"** Settings ERROR - unknown storage capacity multiplier key '{key}'.");
						continue;
                    }
				}
				this.m_capacity_multipliers[resource_type] = Settings.m_capacity_multipliers[key].Value;
            }
        }
    }

	private ResourceFairyPlugin m_plugin = null;
	private float m_last_update_time;
	private EntityQuery m_time_query;
	private EntityQuery m_food_storage_query;
	private EntityQuery m_wsi_storage_query;
	private ComponentDataFromEntity<BuildingBase> m_building_base_data;
	private ComponentDataFromEntity<StorageBase> m_storage_base_data;
	private ComponentDataFromEntity<FoodStorage> m_food_storage_data;
	private ComponentDataFromEntity<IronStorage> m_iron_storage_data;
	private ComponentDataFromEntity<StoneStorage> m_stone_storage_data;
	private ComponentDataFromEntity<WoodStorage> m_wood_storage_data;
	private StorageManager m_storage_manager;
	
	protected override void OnCreateSimulation() {
		if (this.m_plugin == null) {
			this.m_plugin = new ResourceFairyPlugin();
			this.m_plugin.ecs_load();
		}
		this.m_last_update_time = float.MinValue;
		this.m_time_query = GetEntityQuery(new EntityQueryDesc {
			All = new ComponentType[1] {ComponentType.ReadOnly<CurrentSessionTimeSingleton>()},
			Any = new ComponentType[0],
			None = new ComponentType[0],
			Options = EntityQueryOptions.Default
		});
		this.m_food_storage_query = GetEntityQuery(new EntityQueryDesc {
			All = new ComponentType[3] {
				ComponentType.ReadOnly<FoodStorage>(),
				ComponentType.ReadOnly<StorageBase>(),
				ComponentType.ReadOnly<BuildingBase>()
			},
			None = new ComponentType[6] {
				ComponentType.ReadOnly<CurrentBuildingForConstruction>(),
				ComponentType.ReadOnly<BuildingDestroyRequest>(),
				ComponentType.ReadOnly<DelayedDestroy>(),
				ComponentType.ReadOnly<IsFullStorage>(),
				ComponentType.ReadOnly<InConstruction>(),
				ComponentType.ReadOnly<Market>()
			}
		});
		this.m_wsi_storage_query = GetEntityQuery(new EntityQueryDesc {
			All = new ComponentType[5]
			{
				ComponentType.ReadOnly<WoodStorage>(),
				ComponentType.ReadOnly<StoneStorage>(),
				ComponentType.ReadOnly<IronStorage>(),
				ComponentType.ReadOnly<StorageBase>(),
				ComponentType.ReadOnly<BuildingBase>()
			},
			None = new ComponentType[6]
			{
				ComponentType.ReadOnly<CurrentBuildingForConstruction>(),
				ComponentType.ReadOnly<BuildingDestroyRequest>(),
				ComponentType.ReadOnly<DelayedDestroy>(),
				ComponentType.ReadOnly<IsFullStorage>(),
				ComponentType.ReadOnly<InConstruction>(),
				ComponentType.ReadOnly<Market>()
			}
		});
		this.m_building_base_data = this.GetComponentDataFromEntity<BuildingBase>();
		this.m_storage_base_data = this.GetComponentDataFromEntity<StorageBase>(false);
		this.m_food_storage_data = this.GetComponentDataFromEntity<FoodStorage>(false);
		this.m_iron_storage_data = this.GetComponentDataFromEntity<IronStorage>(false);
		this.m_stone_storage_data = this.GetComponentDataFromEntity<StoneStorage>(false);
		this.m_wood_storage_data = this.GetComponentDataFromEntity<WoodStorage>(false);
		this.RequireSingletonForUpdate<CurrentSessionTimeSingleton>();
		this.RequireSingletonForUpdate<GameRunningSingleton>();
		this.m_storage_manager = new StorageManager();
	}

	public class StorageBuilding {

		public class ResourceInfo {
			public Entity m_entity;
			public BuildingType m_building_type;
			public ResourceType m_resource_type;
			public int m_current_value = 0;
			public int m_previous_value = -1;
			public int m_positive_delta = 0;
			public int m_capacity = 0;

			public void add_bonus_resources(int wsi_value) {
				if (this.m_positive_delta <= 0) {
					return;
				}
				switch (this.m_resource_type) {
					case ResourceType.Wood:
					case ResourceType.Stone:
					case ResourceType.Iron:
						this.m_capacity -= wsi_value;
						break;
					default:
						this.m_capacity -= this.m_current_value;
						break;
				}
                DDPlugin._debug_log($"[{this.m_resource_type}] entity: {this.m_entity.GetHashCode()}, count: {this.m_current_value}, positive_delta: {this.m_positive_delta}, capacity: {this.m_capacity}");
            }

            public void update<TStorage>(TStorage data, StorageBase base_data) where TStorage : struct, IComponentData, IResourceStorage {
				this.m_current_value = data.CurrentAmount();
				this.m_positive_delta = (this.m_current_value > 0 && this.m_previous_value > -1 ? Mathf.Max(0, this.m_current_value - this.m_previous_value) : 0);
				this.m_previous_value = this.m_current_value;
                if (this.m_positive_delta == 0) {
                    return;
                }
                this.m_capacity = 0;
				switch (this.m_resource_type) {
					case ResourceType.Food:
						this.m_capacity = base_data.value.Value.foodCapacity;
						break;
					case ResourceType.Iron:
					case ResourceType.Stone:
					case ResourceType.Wood:
						this.m_capacity = base_data.value.Value.woodStoneIronCapacity;
						break;
				}
			}
		}

		public Entity m_entity;
		public BuildingType m_building_type;
		public Dictionary<ResourceType, ResourceInfo> m_resources = new Dictionary<ResourceType, ResourceInfo>();
		
		public StorageBuilding(Entity entity, ComponentDataFromEntity<StorageBase> base_data, ComponentDataFromEntity<BuildingBase> building_data) {
			this.m_entity = entity;
			this.m_building_type = building_data[entity].value.Value.type;
			foreach (ResourceType key in Enum.GetValues(typeof(ResourceType))) {
				this.m_resources[key] = new ResourceInfo() {
					m_entity = entity,
					m_building_type = this.m_building_type,
					m_resource_type = key
				};
			}
			DDPlugin._debug_log($"Added new building - entity: {entity.GetHashCode()}, type: {this.m_building_type}, food_capacity: {base_data[entity].value.Value.foodCapacity}, wsi_capacity: {base_data[entity].value.Value.woodStoneIronCapacity}");
		}

        public void add_bonus_resources() {
			int wsi_value = this.m_resources[ResourceType.Wood].m_current_value + this.m_resources[ResourceType.Stone].m_current_value + this.m_resources[ResourceType.Iron].m_current_value;
			foreach (ResourceInfo resource in this.m_resources.Values) {
				resource.add_bonus_resources(wsi_value);
            }
        }

        public void update<TStorage>(ResourceType resource_type, ComponentDataFromEntity<TStorage> storage_data, ComponentDataFromEntity<StorageBase> base_data) where TStorage : struct, IComponentData, IResourceStorage {
			this.m_resources[resource_type].update<TStorage>(storage_data[this.m_entity], base_data[this.m_entity]);
		}
	}

	public class StorageManager {
		private Dictionary<Entity, StorageBuilding> m_buildings = new Dictionary<Entity, StorageBuilding>();
		private List<BuildingType> m_capacity_modified_building_types = new List<BuildingType>();

		public void add_bonus_resources() {
			foreach (StorageBuilding building in this.m_buildings.Values) {
				building.add_bonus_resources();
			}
		}

		public void update<TStorage>(ResourceType resource_type, EntityQuery query, ComponentDataFromEntity<TStorage> storage_data, ComponentDataFromEntity<StorageBase> base_data, ComponentDataFromEntity<BuildingBase> building_data) where TStorage : struct, IComponentData, IResourceStorage {
			foreach (Entity entity in query.ToEntityArray(Allocator.Temp)) {
				if (!this.m_buildings.TryGetValue(entity, out StorageBuilding building)) {
					building = this.m_buildings[entity] = new StorageBuilding(entity, base_data, building_data);
					if (!this.m_capacity_modified_building_types.Contains(building.m_building_type)) {
						base_data[entity].value.Value.foodCapacity = Mathf.CeilToInt(base_data[entity].value.Value.foodCapacity * SettingsWrapper.Instance.m_capacity_multipliers[ResourceType.Food]);
						base_data[entity].value.Value.woodStoneIronCapacity = Mathf.CeilToInt(base_data[entity].value.Value.woodStoneIronCapacity * SettingsWrapper.Instance.m_capacity_multipliers[ResourceType.Wood]);
						DDPlugin._debug_log($"Changing building storage capacity - type: {building.m_building_type}, food_capacity: {base_data[entity].value.Value.foodCapacity}, wsi_capacity: {base_data[entity].value.Value.woodStoneIronCapacity}");
						this.m_capacity_modified_building_types.Add(building.m_building_type);
					}
				}
				building.update<TStorage>(resource_type, storage_data, base_data);
			}
		}
	}

	protected override void OnUpdateSimulation() {
		try {
			if (this.HasSingleton<WinLoseSingleton>()) {
				return;
			}
			float elapsed = this.m_time_query.GetSingleton<CurrentSessionTimeSingleton>().elapsedTime;
			if (!(this.m_last_update_time > elapsed || elapsed - this.m_last_update_time > UPDATE_FREQUENCY)) {
				return;
			}
			this.m_last_update_time = elapsed;
			//DDPlugin._debug_log($"elapsed: {elapsed}");
			this.m_storage_manager.update<FoodStorage>(ResourceType.Food, this.m_food_storage_query, this.m_food_storage_data, this.m_storage_base_data, this.m_building_base_data);
			this.m_storage_manager.update<IronStorage>(ResourceType.Iron, this.m_wsi_storage_query, this.m_iron_storage_data, this.m_storage_base_data, this.m_building_base_data);
			this.m_storage_manager.update<StoneStorage>(ResourceType.Stone, this.m_wsi_storage_query, this.m_stone_storage_data, this.m_storage_base_data, this.m_building_base_data);
			this.m_storage_manager.update<WoodStorage>(ResourceType.Wood, this.m_wsi_storage_query, this.m_wood_storage_data, this.m_storage_base_data, this.m_building_base_data);
			this.m_storage_manager.add_bonus_resources();		
		} catch (Exception e) {
			DDPlugin._error_log("** OnUpdateSimulation ERROR - " + e);
		}
	}
}

public struct WorkerDeliveryIncreaseJob : IJobEntityBatch {
	public struct Data {
		public Entity m_worker_entity;
		public CitizenType m_worker_type;
		public bool m_has_resource;
		public WorkerCatchResource m_resource;

		public Data(WorkerDeliveryIncreaseJob job, ArchetypeChunk chunk) {
			this.m_worker_entity = Entity.Null;
			this.m_worker_type = (
				chunk.Has(job.m_berry_picker_handle) ? CitizenType.BerryPicker : 
				CitizenType.Unemployed
			);
			this.m_has_resource = chunk.Has(job.m_worker_catch_resource_handle);
			this.m_resource = default(WorkerCatchResource);
		}
	}
	private EntityTypeHandle m_entity_handle;
	private ComponentTypeHandle<BerryPicker> m_berry_picker_handle;
	private ComponentTypeHandle<WorkerCatchResource> m_worker_catch_resource_handle;

	public static JobHandle schedule(SystemBase system, EntityQuery query, JobHandle dependency) {
		DDPlugin._debug_log("1");
		WorkerDeliveryIncreaseJob job = default(WorkerDeliveryIncreaseJob);
		DDPlugin._debug_log("2");
		//job.m_entity_handle = system.GetEntityTypeHandle();
		DDPlugin._debug_log("3");
		//job.m_berry_picker_handle = system.GetComponentTypeHandle<BerryPicker>(isReadOnly: false);
		DDPlugin._debug_log("4");
		//job.m_worker_catch_resource_handle = system.GetComponentTypeHandle<WorkerCatchResource>(isReadOnly: false);
		DDPlugin._debug_log("5");
		return JobEntityBatchExtensions.Schedule(job, query, dependency);
	}

	public void Execute(ArchetypeChunk chunk, int batch_index) {
		DDPlugin._debug_log("7");
		Data data = new Data(this, chunk);
		DDPlugin._debug_log("8");
		NativeArray<Entity> entities = chunk.GetNativeArray(this.m_entity_handle);
		DDPlugin._debug_log("9");
		NativeArray<WorkerCatchResource> resources = chunk.GetNativeArray(this.m_worker_catch_resource_handle);
		DDPlugin._debug_log($"entities: {entities.Length}, resources: {resources.Length}");
		for (int index = 0; index < entities.Length; index++) {
			data.m_worker_entity = entities[index];
			data.m_resource = (resources.Length > 0 ? resources[index] : default(WorkerCatchResource));
			DDPlugin._debug_log($"index: {index}, type: {data.m_resource.type}, count: {data.m_resource.count}");
		}
	}
}
