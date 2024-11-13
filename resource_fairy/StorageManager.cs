using Components;
using Components.RawComponents;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Utility.EnumsStorage;
using Utility.InterfacesStorage;

public class StorageManager {
	public ResourceFairySystem m_system;
	public class __Queries__ {
		public StorageManager m_manager;
		public EntityQuery m_food_storage;
		public EntityQuery m_wsi_storage;

		public __Queries__(StorageManager manager) {
			this.m_manager = manager;
			this.m_food_storage = this.m_manager.m_system.get_entity_query(new EntityQueryDesc {
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
			this.m_wsi_storage = this.m_manager.m_system.get_entity_query(new EntityQueryDesc {
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
		}
	}
	public __Queries__ m_queries;
	public class __ComponentData__ {
		public StorageManager m_manager;
		public ComponentDataFromEntity<BuildingBase> m_building_base;
		public ComponentDataFromEntity<StorageBase> m_storage_base;
		public ComponentDataFromEntity<FoodReserve> m_food_reserve;
		public ComponentDataFromEntity<FoodStorage> m_food_storage;
		public ComponentDataFromEntity<CurrentFood> m_food_ui;
		public ComponentDataFromEntity<IronReserve> m_iron_reserve;
		public ComponentDataFromEntity<IronStorage> m_iron_storage;
		public ComponentDataFromEntity<CurrentIron> m_iron_ui;
		public ComponentDataFromEntity<StoneReserve> m_stone_reserve;
		public ComponentDataFromEntity<StoneStorage> m_stone_storage;
		public ComponentDataFromEntity<CurrentStone> m_stone_ui;
		public ComponentDataFromEntity<WoodReserve> m_wood_reserve;
		public ComponentDataFromEntity<WoodStorage> m_wood_storage;
		public ComponentDataFromEntity<CurrentWood> m_wood_ui;
		
		public __ComponentData__(StorageManager manager) {
			this.m_manager = manager;
			this.m_building_base = this.m_manager.m_system.GetComponentDataFromEntity<BuildingBase>();
			this.m_storage_base = this.m_manager.m_system.GetComponentDataFromEntity<StorageBase>(false);
			this.m_food_reserve = this.m_manager.m_system.GetComponentDataFromEntity<FoodReserve>(false);
			this.m_food_storage = this.m_manager.m_system.GetComponentDataFromEntity<FoodStorage>(false);
			this.m_food_ui = this.m_manager.m_system.GetComponentDataFromEntity<CurrentFood>(false);
			this.m_iron_reserve = this.m_manager.m_system.GetComponentDataFromEntity<IronReserve>(false);
			this.m_iron_storage = this.m_manager.m_system.GetComponentDataFromEntity<IronStorage>(false);
			this.m_iron_ui = this.m_manager.m_system.GetComponentDataFromEntity<CurrentIron>(false);
			this.m_stone_reserve = this.m_manager.m_system.GetComponentDataFromEntity<StoneReserve>(false);
			this.m_stone_storage = this.m_manager.m_system.GetComponentDataFromEntity<StoneStorage>(false);
			this.m_stone_ui = this.m_manager.m_system.GetComponentDataFromEntity<CurrentStone>(false);
			this.m_wood_reserve = this.m_manager.m_system.GetComponentDataFromEntity<WoodReserve>(false);
			this.m_wood_storage = this.m_manager.m_system.GetComponentDataFromEntity<WoodStorage>(true);
			this.m_wood_ui = this.m_manager.m_system.GetComponentDataFromEntity<CurrentWood>(false);
		}
	}
	public __ComponentData__ m_data;
	private Dictionary<Entity, StorageBuilding> m_buildings = new Dictionary<Entity, StorageBuilding>();
	private List<BuildingType> m_capacity_modified_building_types = new List<BuildingType>();
	public class StorageResourceInfoUiOnly {
		public StorageManager m_manager;
		public ResourceType m_resource_type;
		public Entity m_entity;
		public int m_current_value = 0;
		public int m_previous_value = -1;
		public int m_positive_delta = 0;

		public StorageResourceInfoUiOnly(StorageManager manager, ResourceType resource_type, Entity entity) {
			this.m_manager = manager;
			this.m_resource_type = resource_type;
			this.m_entity = entity;
		}
	}
	private Dictionary<Entity, StorageResourceInfoUiOnly> m_ui_only_resources = new Dictionary<Entity, StorageResourceInfoUiOnly>();

	public StorageManager(ResourceFairySystem system) {
		this.m_system = system;
		this.m_queries = new __Queries__(this);
		this.m_data = new __ComponentData__(this);
	}

	public void add_bonus_resources() {
		foreach (StorageBuilding building in this.m_buildings.Values) {
			building.add_bonus_resources();
		}
	}

	public void update() {
		this.update_resource(ResourceType.Food, this.m_queries.m_food_storage, this.m_data.m_food_storage, this.m_data.m_food_reserve, this.m_data.m_food_ui);
		this.update_resource(ResourceType.Iron, this.m_queries.m_wsi_storage, this.m_data.m_iron_storage, this.m_data.m_iron_reserve, this.m_data.m_iron_ui);
		this.update_resource(ResourceType.Stone, this.m_queries.m_wsi_storage, this.m_data.m_stone_storage, this.m_data.m_stone_reserve, this.m_data.m_stone_ui);
		this.update_resource(ResourceType.Wood, this.m_queries.m_wsi_storage, this.m_data.m_wood_storage, this.m_data.m_wood_reserve, this.m_data.m_wood_ui);
		this.add_bonus_resources();
	}

	private void update_resource<TStorage, TReserve, TUi>(ResourceType resource_type, EntityQuery query, ComponentDataFromEntity<TStorage> storage_data, ComponentDataFromEntity<TReserve> reserve_data, ComponentDataFromEntity<TUi> ui_data) where TStorage : struct, IComponentData, IResourceStorage where TReserve : struct, IComponentData, IResourceReserve where TUi : struct, IComponentData, IUserUIResource {
		foreach (Entity entity in query.ToEntityArray(Allocator.Temp)) {
			if (!this.m_buildings.TryGetValue(entity, out StorageBuilding building)) {
				building = this.m_buildings[entity] = new StorageBuilding(this, entity, this.m_data.m_storage_base, this.m_data.m_building_base);
				if (!this.m_capacity_modified_building_types.Contains(building.m_building_type)) {
					this.m_data.m_storage_base[entity].value.Value.foodCapacity = Mathf.CeilToInt(this.m_data.m_storage_base[entity].value.Value.foodCapacity * Settings.Instance.m_capacity_multipliers[ResourceType.Food]);
					this.m_data.m_storage_base[entity].value.Value.woodStoneIronCapacity = Mathf.CeilToInt(this.m_data.m_storage_base[entity].value.Value.woodStoneIronCapacity * Settings.Instance.m_capacity_multipliers[ResourceType.Wood]);
					DDPlugin._debug_log($"Changing building storage capacity - type: {building.m_building_type}, food_capacity: {this.m_data.m_storage_base[entity].value.Value.foodCapacity}, wsi_capacity: {this.m_data.m_storage_base[entity].value.Value.woodStoneIronCapacity}");
					this.m_capacity_modified_building_types.Add(building.m_building_type);
				}
			}
			building.update(resource_type, storage_data, reserve_data);
		}
	}

	private void update_ui_only_resource<TUi>(ResourceType resource_type, ComponentDataFromEntity<TUi> ui_data, Entity ui_entity) where TUi : struct, IComponentData, IUserUIResource {
		if (!this.m_ui_only_resources.TryGetValue(ui_entity, out StorageResourceInfoUiOnly resource)) {
			resource = this.m_ui_only_resources[ui_entity] = new StorageResourceInfoUiOnly(this, resource_type, ui_entity);
		}
		resource.update(resource_type, ui_data, ui_entity);
	}
