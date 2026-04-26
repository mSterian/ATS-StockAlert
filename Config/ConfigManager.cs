using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using StockAlert.Core.Models;
using StockAlert.Infrastructure.Plugin;
using UnityEngine;

namespace StockAlert.Config
{
    public static class ConfigManager
    {
        private static ConfigFile _config;
        private static ConfigEntry<KeyboardShortcut> _toggleSettingsKey;

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
            ApplyProductionLimit(g);
        }

        public static bool RefreshGoodsFromProductionLimits(IEnumerable<GoodInfo> goods)
        {
            var changed = false;
            foreach (var good in goods)
            {
                changed |= ApplyProductionLimit(good);
            }

            return changed;
        }

        public static bool RefreshGoodFromProductionLimit(GoodInfo good)
        {
            return ApplyProductionLimit(good);
        }

        private static bool ApplyProductionLimit(GoodInfo g)
        {
            var previous = g.Threshold;
            var threshold = Game.GameAPI.GetProductionLimit(g.Model, g.Id);
            g.Threshold = Mathf.Max(0, threshold);
            g.Enabled = g.Threshold > 0;

            if (previous != g.Threshold)
            {
                Plugin.Log($"Synced threshold for {g.Id} => {g.Threshold}");
                return true;
            }

            return false;
        }
    }
}
