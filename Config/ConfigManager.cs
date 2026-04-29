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
        private static ConfigEntry<bool> _showHud;
        private static ConfigEntry<bool> _movableHud;
        private static ConfigEntry<float> _hudPositionX;
        private static ConfigEntry<float> _hudPositionY;

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
            _showHud = _config.Bind(
                "HUD",
                "ShowHUD",
                true,
                "Show or hide the Stock Alert HUD."
            );
            _movableHud = _config.Bind(
                "HUD",
                "MovableHUD",
                false,
                "Allow dragging the Stock Alert HUD."
            );
            _hudPositionX = _config.Bind(
                "HUD",
                "PositionX",
                -1f,
                "Saved X position for the Stock Alert HUD. Negative uses the default bottom-right anchor."
            );
            _hudPositionY = _config.Bind(
                "HUD",
                "PositionY",
                -1f,
                "Saved Y position for the Stock Alert HUD. Negative uses the default bottom-right anchor."
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

        public static bool ShowHud
        {
            get
            {
                Load();
                return _showHud.Value;
            }
            set
            {
                Load();
                _showHud.Value = value;
                _config.Save();
            }
        }

        public static bool MovableHud
        {
            get
            {
                Load();
                return _movableHud.Value;
            }
            set
            {
                Load();
                _movableHud.Value = value;
                _config.Save();
            }
        }

        public static Vector2 HudPosition
        {
            get
            {
                Load();
                return new Vector2(_hudPositionX.Value, _hudPositionY.Value);
            }
            set
            {
                Load();
                _hudPositionX.Value = value.x;
                _hudPositionY.Value = value.y;
                _config.Save();
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
