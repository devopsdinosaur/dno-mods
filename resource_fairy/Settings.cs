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
    public static Dictionary<string, ConfigEntry<int>> m_daily_amounts = new Dictionary<string, ConfigEntry<int>>();

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        foreach (string key in new string[] {"Bones", "Food", "Iron", "Money", "Souls", "Spirit", "Stone", "Wood"}) {
            m_daily_amounts[key] = this.m_plugin.Config.Bind<int>("General", "Daily " + key, 0, $"The amount of {key} granted each game day (int, default 0).");
        }
    }
}