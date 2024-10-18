using BepInEx.Configuration;
using System.Collections.Generic;

public class Settings {
    private static Settings m_instance = null;
    public static Settings Instance {
        get {
            if (m_instance == null) {
                m_instance = new Settings();
            }
            return m_instance;
        }
    }
    private DDPlugin m_plugin = null;

    // General
    public static ConfigEntry<bool> m_enabled;
    public static Dictionary<string, ConfigEntry<float>> m_resource_multipliers = new Dictionary<string, ConfigEntry<float>>();

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        foreach (string key in new string[] {"Food", "Iron", "Money", "Souls", "Stone", "Wood"}) {
            m_resource_multipliers[key] = this.m_plugin.Config.Bind<float>("General", $"{key} - Multiplier", 1, $"Multiplier for the amount of {key} resource stored (float, default 1 [nothing added], > 1 more resources, < 1 is ignored).");
        }
    }
}