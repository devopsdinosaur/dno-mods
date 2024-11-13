using Components;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Utility.EnumsStorage;
using Utility.InterfacesStorage;

public class StorageBuilding {
	public StorageManager m_manager;
	public Entity m_entity;
	public BuildingType m_building_type;
	public Dictionary<ResourceType, StorageResourceInfo> m_resources = new Dictionary<ResourceType, StorageResourceInfo>();

	public StorageBuilding(StorageManager manager, Entity entity, ComponentDataFromEntity<StorageBase> base_data, ComponentDataFromEntity<BuildingBase> building_data) {
		this.m_manager = manager;
		this.m_entity = entity;
		this.m_building_type = building_data[entity].value.Value.type;
		foreach (ResourceType key in Enum.GetValues(typeof(ResourceType))) {
			this.m_resources[key] = new StorageResourceInfo() {
				m_building = this,
				m_entity = entity,
				m_building_type = this.m_building_type,
				m_resource_type = key
			};
		}
		DDPlugin._debug_log($"Added new building - entity: {entity.GetHashCode()}, type: {this.m_building_type}, food_capacity: {base_data[entity].value.Value.foodCapacity}, wsi_capacity: {base_data[entity].value.Value.woodStoneIronCapacity}");
	}

	public void add_bonus_resources() {
		int wsi_value = this.m_resources[ResourceType.Wood].m_storage.m_current_value + this.m_resources[ResourceType.Wood].m_reserves.m_current_value + this.m_resources[ResourceType.Stone].m_storage.m_current_value + this.m_resources[ResourceType.Stone].m_reserves.m_current_value + this.m_resources[ResourceType.Iron].m_storage.m_current_value + this.m_resources[ResourceType.Iron].m_reserves.m_current_value;
		foreach (StorageResourceInfo resource in this.m_resources.Values) {
			resource.add_bonus_resources(wsi_value);
		}
	}

	public void update<TStorage, TReserve>(ResourceType resource_type, ComponentDataFromEntity<TStorage> storage_data, ComponentDataFromEntity<TReserve> reserve_data) where TStorage : struct, IComponentData, IResourceStorage where TReserve : struct, IComponentData, IResourceReserve {
		this.m_resources[resource_type].update(storage_data[this.m_entity], reserve_data[this.m_entity]);
	}
}
