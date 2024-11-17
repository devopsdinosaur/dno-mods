using System.Collections.Generic;
using Unity.Entities;

public class PersistentSystem : SystemBase {
	private static PersistentSystem m_instance = null;
	public static PersistentSystem Instance => m_instance;
	private static ResourceFairyPlugin m_plugin = null;
	public static ResourceFairyPlugin Plugin => m_plugin;
	private static List<StorageBuilding> m_capacity_modified_building_types = new List<StorageBuilding>();
	public static List<StorageBuilding> CapacityModifiedBuildingTypes => m_capacity_modified_building_types;

	protected override void OnCreate() {
		m_instance = this;
		m_plugin = new ResourceFairyPlugin();
		m_plugin.ecs_load();
	}

	protected override void OnUpdate() {
	}
}