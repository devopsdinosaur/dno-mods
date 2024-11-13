using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using Utility.EnumsStorage;

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
    public static ConfigEntry<string> m_log_level;
    public static Dictionary<string, ConfigEntry<float>> m_string_keyed_resource_multipliers = new Dictionary<string, ConfigEntry<float>>();
    public static Dictionary<string, ConfigEntry<float>> m_string_keyed_capacity_multipliers = new Dictionary<string, ConfigEntry<float>>();
    public Dictionary<ResourceType, float> m_resource_multipliers = new Dictionary<ResourceType, float>();
    public Dictionary<ResourceType, float> m_capacity_multipliers = new Dictionary<ResourceType, float>();

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_log_level = this.m_plugin.Config.Bind<string>("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.");
        foreach (string key in new string[] { "Food", "Iron", "Money", "Souls", "Stone", "Wood" }) {
            m_string_keyed_resource_multipliers[key] = this.m_plugin.Config.Bind<float>("General", $"{key} - Multiplier", 1, $"Multiplier for the amount of {key} resource stored (float, default 1 [nothing added], > 1 more resources, < 1 is ignored).");
        }
        foreach (string key in new string[] { "Food", "Wood_Stone_Iron" }) {
            m_string_keyed_capacity_multipliers[key] = this.m_plugin.Config.Bind<float>("General", $"{key} - Maximum Capacity Multiplier", 1, $"Multiplier applied to the maximum storage capacity of {key} storage buildings (float, default 1 [no change], > 1 more capacity, < 1 less capacity).");
        }
        foreach (string key in Settings.m_string_keyed_resource_multipliers.Keys) {
            if (Enum.TryParse<ResourceType>(key, out ResourceType resource_type)) {
                this.m_resource_multipliers[resource_type] = Settings.m_string_keyed_resource_multipliers[key].Value;
                continue;
            }
            DDPlugin._error_log($"** Settings ERROR - unknown resource multiplier key '{key}'.");
        }
        foreach (string key in Settings.m_string_keyed_capacity_multipliers.Keys) {
            if (!Enum.TryParse<ResourceType>(key, out ResourceType resource_type)) {
                if (key == "Wood_Stone_Iron") {
                    resource_type = ResourceType.Wood;
                } else {
                    DDPlugin._error_log($"** Settings ERROR - unknown storage capacity multiplier key '{key}'.");
                    continue;
                }
            }
            this.m_capacity_multipliers[resource_type] = Settings.m_string_keyed_capacity_multipliers[key].Value;
        }
    }
}