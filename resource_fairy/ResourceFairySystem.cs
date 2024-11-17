using Components.SingletonComponents;
using System;
using System.Collections.Generic;
using Systems;
using Systems.ComponentSystemGroups;
using Systems.WorkersDeliverySystems;
using Unity.Entities;

[UpdateAfter(typeof(WorkersDeliverySystem))]
[UpdateInGroup(typeof(ResourceDeliveryGroup))]
public class ResourceFairySystem : SystemBaseSimulation {
	private const float UPDATE_FREQUENCY = 0.1f;

	private float m_last_update_time;
	private EntityQuery m_time_query;
	private StorageManager m_storage_manager;
	
	protected override void OnCreateSimulation() {
		this.m_last_update_time = float.MinValue;
		this.m_time_query = GetEntityQuery(new EntityQueryDesc {
			All = new ComponentType[1] {ComponentType.ReadOnly<CurrentSessionTimeSingleton>()},
			Any = new ComponentType[0],
			None = new ComponentType[0],
			Options = EntityQueryOptions.Default
		});
		this.RequireSingletonForUpdate<CurrentSessionTimeSingleton>();
		this.RequireSingletonForUpdate<GameRunningSingleton>();
		this.m_storage_manager = new StorageManager(this);
	}

	public EntityQuery get_entity_query(params EntityQueryDesc[] query_descs) {
		return this.GetEntityQuery(query_descs);
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
			this.m_storage_manager.update();		
		} catch (Exception e) {
			DDPlugin._error_log("** OnUpdateSimulation ERROR - " + e);
		}
	}

}

