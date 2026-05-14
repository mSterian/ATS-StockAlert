using System;
using System.Linq;
using System.Reflection;
using Eremite.Model.Meta;
using Eremite.Model.State;
using Eremite.View.Menu.Pick;
using HarmonyLib;
using StockAlert.Config;
using StockAlert.Game;
using TMPro;
using UnityEngine;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch]
    internal static class EmbarkationCostRangePatch
    {
        private static FieldInfo _fiGoodCostText;
        private static FieldInfo _fiRewardCostText;

        [HarmonyPatch(typeof(GoodPickSlot), "SetUp")]
        [HarmonyPostfix]
        private static void GoodPickSlotSetUpPostfix(GoodPickSlot __instance, GoodPickState state)
        {
            if (!ConfigManager.ShowEmbarkationCostRanges || __instance == null || state == null)
            {
                return;
            }

            var costRange = GetGoodCostRange(state);
            if (!costRange.HasValue)
            {
                return;
            }

            _fiGoodCostText ??= typeof(GoodPickSlot).GetField("costText", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_fiGoodCostText?.GetValue(__instance) is TMP_Text text)
            {
                ApplyCostRangeText(text, state.cost, costRange.Value.Min, costRange.Value.Max);
            }
        }

        [HarmonyPatch(typeof(RewardPickSlot), "SetUp")]
        [HarmonyPostfix]
        private static void RewardPickSlotSetUpPostfix(RewardPickSlot __instance, ConditionPickState state)
        {
            if (!ConfigManager.ShowEmbarkationCostRanges || __instance == null || state == null)
            {
                return;
            }

            var costRange = GetConditionCostRange(state);
            if (!costRange.HasValue)
            {
                return;
            }

            _fiRewardCostText ??= typeof(RewardPickSlot).GetField("costText", BindingFlags.Instance | BindingFlags.NonPublic);
            if (_fiRewardCostText?.GetValue(__instance) is TMP_Text text)
            {
                ApplyCostRangeText(text, state.cost, costRange.Value.Min, costRange.Value.Max);
            }
        }

        private static void ApplyCostRangeText(TMP_Text text, int currentCost, int minCost, int maxCost)
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMax = Mathf.Min(text.fontSizeMax > 0f ? text.fontSizeMax : text.fontSize, text.fontSize);
            text.fontSizeMin = Mathf.Min(8f, text.fontSizeMax);
            text.richText = true;
            text.text = $"<color=#{GetCostColorHex(currentCost, minCost, maxCost)}>{currentCost}</color>/{maxCost}";
        }

        private static string GetCostColorHex(int currentCost, int minCost, int maxCost)
        {
            if (currentCost <= minCost)
            {
                return "7ED957";
            }

            if (currentCost >= maxCost)
            {
                return "FF5A4D";
            }

            return "FFD84D";
        }

        private static CostRange? GetGoodCostRange(GoodPickState state)
        {
            try
            {
                var settings = GameAPI.GetSettings();
                var reward = settings?.metaRewards
                    ?.OfType<EmbarkGoodMetaRewardModel>()
                    .FirstOrDefault(model => model?.good != null
                        && string.Equals(model.good.Name, state.name, StringComparison.OrdinalIgnoreCase));

                return reward != null ? new CostRange(reward.costRange.x, reward.costRange.y) : (CostRange?)null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static CostRange? GetConditionCostRange(ConditionPickState state)
        {
            return GetEmbarkEffectCostRange(state) ?? GetBuildingCostRange(state);
        }

        private static CostRange? GetEmbarkEffectCostRange(ConditionPickState state)
        {
            try
            {
                var settings = GameAPI.GetSettings();
                var reward = settings?.metaRewards
                    ?.OfType<EmbarkEffectMetaRewardModel>()
                    .FirstOrDefault(model => model?.effect != null
                        && string.Equals(model.effect.Name, state.name, StringComparison.OrdinalIgnoreCase));

                return reward != null ? new CostRange(reward.costRange.x, reward.costRange.y) : (CostRange?)null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static CostRange? GetBuildingCostRange(ConditionPickState state)
        {
            try
            {
                var settings = GameAPI.GetSettings();
                if (settings == null || !settings.ContainsBuilding(state.name))
                {
                    return null;
                }

                var building = settings.GetBuilding(state.name);
                return building != null ? new CostRange(building.costRange.x, building.costRange.y) : (CostRange?)null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private readonly struct CostRange
        {
            public readonly int Min;
            public readonly int Max;

            public CostRange(int min, int max)
            {
                Min = min;
                Max = max;
            }
        }
    }
}
