using Components;
using Components.RawComponents;
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

[UpdateBefore(typeof(WorkersDeliverySystem))]
[UpdateInGroup(typeof(ResourceDeliveryGroup))]
public class TestSystem : SystemBaseSimulation {
    private ResourceFairyPlugin m_plugin;
	private EntityQuery m_session_time_query;
    private EntityQuery m_busy_workers_query;
	private ComponentDataFromEntity<WorkerCatchResource> m_catch_resource_data;

	protected override void OnCreateSimulation() {
		this.m_plugin = new ResourceFairyPlugin();
		this.m_plugin.ecs_load();
		this.m_session_time_query = this.GetEntityQuery(new EntityQueryDesc {
			All = new ComponentType[] {ComponentType.ReadOnly<CurrentSessionTimeSingleton>()}
		});
		this.m_busy_workers_query = this.GetEntityQuery(new EntityQueryDesc {
			All = new ComponentType[] {
				ComponentType.ReadOnly<UnitSizeBase>(),
				ComponentType.ReadOnly<WorkerTarget>(),
				ComponentType.ReadOnly<Worker>()
			},
			Any = new ComponentType[] {
				ComponentType.ReadOnly<BerryPicker>(),
				ComponentType.ReadOnly<Gravedigger>(),
				ComponentType.ReadOnly<Woodcutter>(),
				ComponentType.ReadOnly<StoneCutter>(),
				ComponentType.ReadOnly<Steelmaker>(),
				ComponentType.ReadOnly<SoulBlacksmith>(),
				ComponentType.ReadOnly<Forester>(),
				ComponentType.ReadOnly<Builder>(),
				ComponentType.ReadOnly<Farmer>(),
				ComponentType.ReadOnly<Fisher>()
			},
			None = new ComponentType[] {
				ComponentType.ReadOnly<DeadInProgress>(),
				ComponentType.ReadOnly<DelayedDestroy>(),
				ComponentType.ReadOnly<Unemployed>(),
				ComponentType.ReadOnly<Exploded>(),
				ComponentType.ReadOnly<InFear>(),
				ComponentType.ReadOnly<Enemy>(),
				ComponentType.ReadOnly<Dead>()
			}
		});
		this.m_catch_resource_data = this.GetComponentDataFromEntity<WorkerCatchResource>();
		this.RequireSingletonForUpdate<CurrentSessionTimeSingleton>();
		this.RequireSingletonForUpdate<GameRunningSingleton>();
	}

	protected override void OnUpdateSimulation() {
		try {
			//DDPlugin._debug_log(Time.ElapsedTime);
			if (this.HasSingleton<WinLoseSingleton>()) {
				return;
			}
			EntityTypeHandle entity_handle = this.GetEntityTypeHandle();
			ComponentTypeHandle<BerryPicker> berry_picker_handle = this.GetComponentTypeHandle<BerryPicker>(isReadOnly: true);
			ComponentTypeHandle<WorkerCatchResource> worker_catch_resource_handle = this.GetComponentTypeHandle<WorkerCatchResource>(isReadOnly: false);
			NativeArray<ArchetypeChunk> chunks = this.m_busy_workers_query.CreateArchetypeChunkArray(Allocator.Temp);
			foreach (ArchetypeChunk chunk in chunks) {
				NativeArray<Entity> entities = chunk.GetNativeArray(entity_handle);
				NativeArray<WorkerCatchResource> resources = chunk.GetNativeArray(worker_catch_resource_handle);
				if (entities.Length == 0 || resources.Length == 0 || resources[0].count == 0) {
					continue;
				}
				DDPlugin._debug_log($"entities: {entities.Length}, resources: {resources.Length}, type: {resources[0].type}, count: {resources[0].count}");
				int new_count = -1;
				switch (resources[0].type) {
					case ResourceType.Food:
						if (resources[0].count < Settings.m_resource_amounts["Food"].Value && chunk.Has<BerryPicker>(berry_picker_handle)) {
							new_count = Settings.m_resource_amounts["Food"].Value;
						}
						break;
				}
				if (new_count > -1) {
					DDPlugin._debug_log($"new_count: {new_count}");
					for (int index = 0; index < entities.Length; index++) {
						this.m_catch_resource_data[entities[index]] = new WorkerCatchResource() {
							type = resources[0].type,
							count = new_count
						};
					}
					//chunk.SetChunkComponentData<WorkerCatchResource>(worker_catch_resource_handle, new WorkerCatchResource() {
					//	type = resources[0].type,
					//	count = new_count
					//});
				}
			}
			//chunks.Dispose();
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
