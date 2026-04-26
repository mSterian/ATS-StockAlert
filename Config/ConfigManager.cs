using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;

namespace StockAlert.Core.Models
{
    public static class ConfigManager
    {
        private static ConfigFile _config;

        private static readonly Dictionary<string, ConfigEntry<bool>> _enabled = new();
        private static readonly Dictionary<string, ConfigEntry<int>> _thresholds = new();

        public static void Load()
        {
            _config = new ConfigFile(Paths.ConfigPath + "\\StockAlert.cfg", true);
        }

        public static void EnsureGoodConfig(GoodInfo g)
        {
            if (!_enabled.ContainsKey(g.Id))
            {
                _enabled[g.Id] = _config.Bind(
                    $"Good.{g.Id}", "Enabled", false,
                    $"Show {g.DisplayName} in HUD"
                );
            }

            if (!_thresholds.ContainsKey(g.Id))
            {
                _thresholds[g.Id] = _config.Bind(
                    $"Good.{g.Id}", "Threshold", 0,
                    $"Threshold for {g.DisplayName}"
                );
            }

            g.Enabled = _enabled[g.Id].Value;
            g.Threshold = _thresholds[g.Id].Value;
        }

        public static void UpdateGoodConfig(GoodInfo g)
        {
            _enabled[g.Id].Value = g.Enabled;
            _thresholds[g.Id].Value = g.Threshold;
            _config.Save();
        }
    }
}
