using Components;
using Components.RawComponents;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Utility.EnumsStorage;
using Utility.InterfacesStorage;

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

    public void add_bonus_resources<TUi>(ComponentDataFromEntity<TUi> ui_data) where TUi : struct, IComponentData, IUserUIResource {
        this.m_current_value = ui_data[this.m_entity].CurrentAmount();
        this.m_positive_delta = (this.m_current_value > 0 && this.m_previous_value > -1 ? Mathf.Max(0, this.m_current_value - this.m_previous_value) : 0);
        this.m_previous_value = this.m_current_value;
        if (this.m_positive_delta <= 0) {
            return;
        }
        int bonus = Mathf.CeilToInt(this.m_positive_delta * Settings.Instance.m_resource_multipliers[this.m_resource_type]) - this.m_positive_delta;
        DDPlugin._debug_log($"[{this.m_resource_type}] entity: {this.m_entity.GetHashCode()}, positive_delta: {this.m_positive_delta}, bonus: {bonus}");
        if (bonus <= 0) {
            return;
        }
        Utility.ResourceUtility.ChangeCurrentResourceValue(ui_data, this.m_entity, bonus);
        this.m_current_value = this.m_previous_value = ui_data[this.m_entity].CurrentAmount();
        DDPlugin._debug_log($"Increased entity {this.m_entity.GetHashCode()}'s {this.m_resource_type} value by {bonus} to {ui_data[this.m_entity].CurrentAmount()}.");
    }
}
