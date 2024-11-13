using Components;
using Components.RawComponents;
using UnityEngine;
using Unity.Entities;
using Utility.EnumsStorage;
using Utility.InterfacesStorage;

public class StorageResourceInfo {
	public StorageBuilding m_building;
	public Entity m_entity;
	public BuildingType m_building_type;
	public ResourceType m_resource_type;
	public class __Storage__ {
		public int m_current_value = 0;
		public int m_previous_value = -1;
		public int m_positive_delta = 0;
	}
	public __Storage__ m_storage = new __Storage__();
	public class __Reserve__ {
		public int m_current_value = 0;
	}
	public __Reserve__ m_reserves = new __Reserve__();
	public int m_capacity = 0;

	public void add_bonus_resources(int wsi_value) {
		if (this.m_storage.m_positive_delta <= 0) {
			return;
		}
		switch (this.m_resource_type) {
			case ResourceType.Wood:
			case ResourceType.Stone:
			case ResourceType.Iron:
				this.m_capacity -= wsi_value;
				break;
			default:
				this.m_capacity -= this.m_reserves.m_current_value - this.m_storage.m_current_value;
				break;
		}
		int bonus = Mathf.Min(this.m_capacity, Mathf.CeilToInt(this.m_storage.m_positive_delta * Settings.Instance.m_resource_multipliers[this.m_resource_type]) - this.m_storage.m_positive_delta);
		DDPlugin._debug_log($"[{this.m_resource_type}] entity: {this.m_entity.GetHashCode()}, storage: {this.m_storage.m_current_value}, reserve: {this.m_reserves.m_current_value}, positive_delta: {this.m_storage.m_positive_delta}, capacity: {this.m_capacity}, bonus: {bonus}");
		if (bonus <= 0) {
			return;
		}
		switch (this.m_resource_type) {
			case ResourceType.Food:
				this.increase_value(this.m_building.m_manager.m_data.m_food_storage, this.m_building.m_manager.m_data.m_food_ui, this.m_building.m_manager.m_ui_entities[ResourceType.Food], bonus);
				break;
			case ResourceType.Iron:
				this.increase_value(this.m_building.m_manager.m_data.m_iron_storage, this.m_building.m_manager.m_data.m_iron_ui, this.m_building.m_manager.m_ui_entities[ResourceType.Iron], bonus);
				break;
			case ResourceType.Stone:
				this.increase_value(this.m_building.m_manager.m_data.m_stone_storage, this.m_building.m_manager.m_data.m_stone_ui, this.m_building.m_manager.m_ui_entities[ResourceType.Stone], bonus);
				break;
			case ResourceType.Wood:
				this.increase_value(this.m_building.m_manager.m_data.m_wood_storage, this.m_building.m_manager.m_data.m_wood_ui, this.m_building.m_manager.m_ui_entities[ResourceType.Wood], bonus);
				break;
		}
	}

	private void increase_value<TStorage, TUi>(ComponentDataFromEntity<TStorage> storage_data, ComponentDataFromEntity<TUi> ui_data, Entity ui_entity, int delta) where TStorage : struct, IComponentData, IResourceStorage where TUi : struct, IComponentData, IUserUIResource {
		TStorage stat = storage_data[this.m_entity];
		stat.IncreaseAmount(delta);
		storage_data[this.m_entity] = stat;
		if (ui_data.HasComponent(ui_entity)) {
			Utility.ResourceUtility.ChangeCurrentResourceValue(ui_data, ui_entity, delta);
		}
		this.m_storage.m_current_value = this.m_storage.m_previous_value = storage_data[this.m_entity].CurrentAmount();
		DDPlugin._debug_log($"Increased entity {this.m_entity.GetHashCode()}'s {this.m_resource_type} storage value by {delta} to {storage_data[this.m_entity].CurrentAmount()}.");
	}

	public void update<TStorage, TReserve>(ComponentDataFromEntity<TStorage> storage_data, ComponentDataFromEntity<TReserve> reserve_data) where TStorage : struct, IComponentData, IResourceStorage where TReserve : struct, IComponentData, IResourceReserve {
		this.m_storage.m_current_value = storage_data[this.m_entity].CurrentAmount();
        this.m_storage.m_positive_delta = (this.m_storage.m_current_value > 0 && this.m_storage.m_previous_value > -1 ? Mathf.Max(0, this.m_storage.m_current_value - this.m_storage.m_previous_value) : 0);
        this.m_storage.m_previous_value = this.m_storage.m_current_value;
		if (reserve_data.HasComponent(this.m_entity)) {
			this.m_reserves.m_current_value = reserve_data[this.m_entity].CurrentReserve();
		}
        if (this.m_storage.m_positive_delta == 0) {
            return;
        }
        this.m_capacity = 0;
        switch (this.m_resource_type) {
            case ResourceType.Food:
                this.m_capacity = this.m_building.m_manager.m_data.m_storage_base[this.m_entity].value.Value.foodCapacity;
                break;
            case ResourceType.Iron:
            case ResourceType.Stone:
            case ResourceType.Wood:
                this.m_capacity = this.m_building.m_manager.m_data.m_storage_base[this.m_entity].value.Value.woodStoneIronCapacity;
                break;
        }
    }
}