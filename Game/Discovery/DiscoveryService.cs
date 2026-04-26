using System;
using System.Collections.Generic;
using System.Text;
using Eremite.Model;
using StockAlert.Config;
using StockAlert.Core.Models;
using StockAlert.Infrastructure.Plugin;

namespace StockAlert.Game.Discovery
{
    public static class Discovery
    {
        public static List<GoodInfo> Goods { get; } = new List<GoodInfo>();

        public static void Initialize()
        {
            Plugin.Log("Discovery.Initialize()");

            Goods.Clear();

            var settings = GameAPI.GetSettings();
            if (settings == null)
            {
                Plugin.Log("Discovery.Initialize(): Settings is null");
                return;
            }

            var goods = settings.Goods;
            if (goods == null || goods.Length == 0)
            {
                Plugin.Log("Discovery.Initialize(): No goods found in Settings");
                return;
            }

            foreach (var model in goods)
            {
                if (model == null)
                {
                    continue;
                }

                var good = new GoodInfo
                {
                    Model = model,
                    Id = model.Name,
                    ConfigKey = BuildConfigKey(model.Name),
                    DisplayName = ResolveDisplayName(model),
                    Icon = model.icon,
                    CurrentAmount = 0
                };

                ConfigManager.EnsureGoodConfig(good);
                Goods.Add(good);
            }

            Goods.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            ConfigManager.RefreshGoodsFromProductionLimits(Goods);
            UpdateStock();

            Plugin.Log($"Discovery.Initialize(): Loaded {Goods.Count} goods");
        }

        private static string ResolveDisplayName(GoodModel model)
        {
            var localized = model.displayName?.ToString();
            if (!string.IsNullOrWhiteSpace(localized) &&
                localized.IndexOf("Missing key", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return localized;
            }

            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                return model.Name;
            }

            return "Unknown Good";
        }

        private static string BuildConfigKey(string rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
            {
                return "UnknownGood";
            }

            var builder = new StringBuilder(rawId.Length);
            foreach (var ch in rawId)
            {
                switch (ch)
                {
                    case '=':
                    case '\n':
                    case '\t':
                    case '\\':
                    case '"':
                    case '\'':
                    case '[':
                    case ']':
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        public static void UpdateStock()
        {
            foreach (var good in Goods)
            {
                if (good.Model == null)
                {
                    continue;
                }

                good.CurrentAmount = GameAPI.GetStoredAmount(good.Model, good.Id);
            }
        }
    }
}
