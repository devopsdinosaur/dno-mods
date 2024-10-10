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
    public static Dictionary<string, ConfigEntry<string>> m_daily_method = new Dictionary<string, ConfigEntry<string>>();
    public static Dictionary<string, ConfigEntry<int>> m_daily_flat_amounts = new Dictionary<string, ConfigEntry<int>>();
    public static Dictionary<string, ConfigEntry<float>> m_daily_multipliers = new Dictionary<string, ConfigEntry<float>>();

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        foreach (string key in new string[] {"Bones", "Food", "Iron", "Money", "Souls", "Spirit", "Stone", "Wood"}) {
            m_daily_method[key] = this.m_plugin.Config.Bind<string>("General", $"Daily {key} - Method", "None", $"One of 'None', 'Amount', 'Multiplier' (case-insensitive) representing the method by which the daily {key} is calculated (string, default None).  'None' = nothing added, 'Amount' = a flat amount will be added [see below], 'Multiplier' = an amount equal to a multiple of the previous day's {key} will be added [see below].");
            m_daily_flat_amounts[key] = this.m_plugin.Config.Bind<int>("General", $"Daily {key} - Flat Amount", 0, $"If 'Daily {key} - Method' is set to 'Amount' then this amount of {key} will be granted each game day and the multiplier will be ignored (int, default 0).");
            m_daily_multipliers[key] = this.m_plugin.Config.Bind<float>("General", $"Daily {key} - Multiplier", 0, $"If 'Daily {key} - Method' is set to 'Multiplier' then this number will be multiplied by the amount of {key} resource that was gathered during the previous day and added [the flat amount will be ignored] (float, default 0).");
        }
    }
}