using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using StockAlert.Core.Models;
using UnityEngine;

namespace StockAlert.Config
{
    public static class ConfigManager
    {
        private static ConfigFile _config;
        private static ConfigEntry<KeyboardShortcut> _toggleSettingsKey;

        private static readonly Dictionary<string, ConfigEntry<bool>> _enabled = new();
        private static readonly Dictionary<string, ConfigEntry<int>> _thresholds = new();

        public static void Load()
        {
            if (_config != null)
            {
                return;
            }

            _config = new ConfigFile(Paths.ConfigPath + "\\StockAlert.cfg", true);
            _toggleSettingsKey = _config.Bind(
                "General",
                "ToggleSettingsKey",
                new KeyboardShortcut(KeyCode.F8),
                "Key used to show or hide the Stock Alert settings window."
            );
        }

        public static KeyboardShortcut ToggleSettingsKey
        {
            get
            {
                Load();
                return _toggleSettingsKey.Value;
            }
        }

        public static void EnsureGoodConfig(GoodInfo g)
        {
            Load();

            var section = $"Good.{g.ConfigKey}";

            if (!_enabled.ContainsKey(g.ConfigKey))
            {
                _enabled[g.ConfigKey] = _config.Bind(
                    section, "Enabled", false,
                    $"Show {g.DisplayName} in HUD"
                );
            }

            if (!_thresholds.ContainsKey(g.ConfigKey))
            {
                _thresholds[g.ConfigKey] = _config.Bind(
                    section, "Threshold", 0,
                    $"Threshold for {g.DisplayName}"
                );
            }

            g.Threshold = _thresholds[g.ConfigKey].Value;
            g.Enabled = _enabled[g.ConfigKey].Value && g.Threshold > 0;
        }

        public static void UpdateGoodConfig(GoodInfo g)
        {
            Load();
            g.Enabled = g.Threshold > 0;
            _enabled[g.ConfigKey].Value = g.Enabled;
            _thresholds[g.ConfigKey].Value = g.Threshold;
            _config.Save();
        }
    }
}
