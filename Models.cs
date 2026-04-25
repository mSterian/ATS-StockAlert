using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Logging;

namespace StockAlert
{
    internal class ThresholdEntry
    {
        public string Id;
        public string Name;
        public float Threshold;
        public string ThresholdText;
        public string NameEdit;
    }

    internal static class Models
    {
        public static readonly List<ThresholdEntry> Thresholds = new List<ThresholdEntry>();
        public static readonly Dictionary<string, string> NameOverrideMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void LoadNameOverridesFromConfig()
        {
            NameOverrideMap.Clear();
            var raw = ConfigManager.NameOverrides.Value ?? string.Empty;
            var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2) continue;
                var id = parts[0].Trim();
                var name = parts[1].Trim();
                if (string.IsNullOrEmpty(id)) continue;
                NameOverrideMap[id] = name;
            }
        }

        public static void SaveNameOverridesToConfig()
        {
            var lines = NameOverrideMap.Select(kv => $"{kv.Key}={kv.Value}");
            ConfigManager.NameOverrides.Value = string.Join("\n", lines);
        }

        public static void LoadThresholdsFromConfig()
        {
            Thresholds.Clear();

            var saved = ConfigManager.SavedThresholds.Value ?? string.Empty;
            var lines = saved.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length == 0) continue;

                var id = parts[0].Trim();
                float thr = 0f;
                if (parts.Length > 1)
                    float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out thr);

                var entry = new ThresholdEntry
                {
                    Id = id,
                    Name = id,
                    Threshold = thr,
                    ThresholdText = thr.ToString(CultureInfo.InvariantCulture),
                    NameEdit = id
                };

                if (NameOverrideMap.TryGetValue(id, out var overrideName))
                {
                    entry.Name = overrideName;
                    entry.NameEdit = overrideName;
                }

                Thresholds.Add(entry);
            }

            // attempt to discover goods and merge friendly names
            Discovery.TryPopulateGoodsFromGame(Thresholds, NameOverrideMap, BepInEx.Logging.Logger.CreateLogSource("StockAlert"));
            // apply overrides again
            foreach (var e in Thresholds)
            {
                if (NameOverrideMap.TryGetValue(e.Id, out var overrideName))
                {
                    e.Name = overrideName;
                    e.NameEdit = overrideName;
                }
            }
        }

        public static void SaveThresholdsToConfig()
        {
            var lines = Thresholds.Select(t => $"{t.Id}:{t.Threshold.ToString(CultureInfo.InvariantCulture)}");
            var joined = string.Join("\n", lines);
            ConfigManager.SavedThresholds.Value = joined;
        }
    }
}
