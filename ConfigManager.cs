using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace StockAlert
{
    internal static class ConfigManager
    {
        public static ConfigEntry<KeyCode> ToggleKey;
        public static ConfigEntry<string> SavedThresholds;
        public static ConfigEntry<string> NameOverrides;
        public static ConfigEntry<bool> VerboseAssemblyDiagnostics;

        public static void Init(ConfigFile cfg, ManualLogSource logger,
            out ConfigEntry<KeyCode> toggleKey,
            out ConfigEntry<string> savedThresholds,
            out ConfigEntry<string> nameOverrides,
            out ConfigEntry<bool> verboseAssemblyDiagnostics)
        {
            toggleKey = cfg.Bind("General", "ToggleKey", KeyCode.F8, "Key to toggle settings");
            savedThresholds = cfg.Bind("General", "SavedThresholds", string.Empty, "Saved thresholds (format: ID:threshold per line)");
            nameOverrides = cfg.Bind("General", "NameOverrides", string.Empty, "Optional name overrides (format: ID=Name per line)");
            verboseAssemblyDiagnostics = cfg.Bind("Diagnostics", "VerboseAssemblyLogging", false,
                "If true, log full assembly->type->method details. Disable to avoid huge logs.");

            // mirror to static holders
            ToggleKey = toggleKey;
            SavedThresholds = savedThresholds;
            NameOverrides = nameOverrides;
            VerboseAssemblyDiagnostics = verboseAssemblyDiagnostics;
        }
    }
}
