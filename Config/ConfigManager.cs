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
        private static ConfigEntry<bool> _showBuildingAlertIndicators;
        private static ConfigEntry<bool> _showBuilderStatusIcons;
        private static ConfigEntry<bool> _autoAdjustProductionLimits;
        private static ConfigEntry<float> _autoAdjustMultiplier;
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
            _showBuildingAlertIndicators = _config.Bind(
                "HUD",
                "ShowBuildingAlertIndicators",
                true,
                "Show red and yellow worker markers over recipe buildings related to current shortages."
            );
            _showBuilderStatusIcons = _config.Bind(
                "HUD",
                "ShowBuilderStatusIcons",
                false,
                "Show idle and working icons over free builder villagers."
            );
            _autoAdjustProductionLimits = _config.Bind(
                "Automation",
                "AutoAdjustProductionLimits",
                false,
                "Automatically adjust global production limits based on the number of villagers that can consume each good."
            );
            _autoAdjustMultiplier = _config.Bind(
                "Automation",
                "ProductionLimitMultiplier",
                2f,
                new ConfigDescription(
                    "Multiplier applied per consuming villager when auto-adjusting production limits. Range: 1.0 to 9.0.",
                    new AcceptableValueRange<float>(1f, 9f)
                )
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

        public static bool ShowBuildingAlertIndicators
        {
            get
            {
                Load();
                return _showBuildingAlertIndicators.Value;
            }
            set
            {
                Load();
                _showBuildingAlertIndicators.Value = value;
                _config.Save();
            }
        }

        public static bool ShowBuilderStatusIcons
        {
            get
            {
                Load();
                return _showBuilderStatusIcons.Value;
            }
            set
            {
                Load();
                _showBuilderStatusIcons.Value = value;
                _config.Save();
            }
        }

        public static bool AutoAdjustProductionLimits
        {
            get
            {
                Load();
                return _autoAdjustProductionLimits.Value;
            }
            set
            {
                Load();
                _autoAdjustProductionLimits.Value = value;
                _config.Save();
            }
        }

        public static float AutoAdjustMultiplier
        {
            get
            {
                Load();
                return NormalizeMultiplier(_autoAdjustMultiplier.Value);
            }
            set
            {
                Load();
                _autoAdjustMultiplier.Value = NormalizeMultiplier(value);
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

        private static float NormalizeMultiplier(float value)
        {
            return Mathf.Clamp(Mathf.Round(value * 10f) / 10f, 1f, 9f);
        }
    }
}
