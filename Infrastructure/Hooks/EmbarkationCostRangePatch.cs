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
        private const string RangeLabelName = "StockAlertCostRangeLabel";
        private static FieldInfo _fiGoodCostText;
        private static FieldInfo _fiRewardCostText;

        [HarmonyPatch(typeof(GoodPickSlot), "SetUp")]
        [HarmonyPostfix]
        private static void GoodPickSlotSetUpPostfix(GoodPickSlot __instance, GoodPickState state)
        {
            if (__instance == null)
            {
                return;
            }

            _fiGoodCostText ??= typeof(GoodPickSlot).GetField("costText", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(_fiGoodCostText?.GetValue(__instance) is TMP_Text text))
            {
                return;
            }

            if (!ConfigManager.ShowEmbarkationCostRanges || state == null)
            {
                RemoveRangeLabel(__instance.transform);
                return;
            }

            var costRange = GetGoodCostRange(state);
            if (!costRange.HasValue)
            {
                RemoveRangeLabel(__instance.transform);
                return;
            }

            ApplyCostDisplay(__instance.transform, text, state.cost, costRange.Value.Min, costRange.Value.Max);
        }

        [HarmonyPatch(typeof(RewardPickSlot), "SetUp")]
        [HarmonyPostfix]
        private static void RewardPickSlotSetUpPostfix(RewardPickSlot __instance, ConditionPickState state)
        {
            if (__instance == null)
            {
                return;
            }

            _fiRewardCostText ??= typeof(RewardPickSlot).GetField("costText", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(_fiRewardCostText?.GetValue(__instance) is TMP_Text text))
            {
                return;
            }

            if (!ConfigManager.ShowEmbarkationCostRanges || state == null)
            {
                RemoveRangeLabel(__instance.transform);
                return;
            }

            var costRange = GetConditionCostRange(state);
            if (!costRange.HasValue)
            {
                RemoveRangeLabel(__instance.transform);
                return;
            }

            ApplyCostDisplay(__instance.transform, text, state.cost, costRange.Value.Min, costRange.Value.Max);
        }

        private static void ApplyCostDisplay(Transform slotTransform, TMP_Text currentCostText, int currentCost, int minCost, int maxCost)
        {
            ApplyCurrentCostText(currentCostText, currentCost, minCost, maxCost);
            ApplyRangeText(slotTransform, currentCostText, minCost, maxCost);
        }

        private static void ApplyCurrentCostText(TMP_Text text, int currentCost, int minCost, int maxCost)
        {
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true;
            text.fontSizeMax = Mathf.Min(text.fontSizeMax > 0f ? text.fontSizeMax : text.fontSize, text.fontSize);
            text.fontSizeMin = Mathf.Min(8f, text.fontSizeMax);
            text.richText = true;
            text.text = $"<color=#{GetCostColorHex(currentCost, minCost, maxCost)}>{currentCost}</color>";
        }

        private static void ApplyRangeText(Transform slotTransform, TMP_Text template, int minCost, int maxCost)
        {
            var label = GetOrCreateRangeLabel(slotTransform, template);
            if (label == null)
            {
                return;
            }

            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMax = Mathf.Min(template.fontSizeMax > 0f ? template.fontSizeMax : template.fontSize, template.fontSize);
            label.fontSizeMin = Mathf.Min(8f, label.fontSizeMax);
            label.richText = false;
            label.color = new Color(0.72f, 1f, 0.45f, 1f);
            label.text = $"{minCost}-{maxCost}";
            label.gameObject.SetActive(true);
        }

        private static TMP_Text GetOrCreateRangeLabel(Transform slotTransform, TMP_Text template)
        {
            if (slotTransform == null || template == null)
            {
                return null;
            }

            var existing = slotTransform.Find(RangeLabelName);
            if (existing != null)
            {
                return existing.GetComponent<TMP_Text>();
            }

            var labelObject = UnityEngine.Object.Instantiate(template.gameObject, slotTransform, false);
            labelObject.name = RangeLabelName;
            labelObject.SetActive(true);

            var label = labelObject.GetComponent<TMP_Text>();
            var rect = labelObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(42f, 22f);
                rect.anchoredPosition = new Vector2(12f, -10f);
            }

            return label;
        }

        private static void RemoveRangeLabel(Transform slotTransform)
        {
            if (slotTransform == null)
            {
                return;
            }

            var existing = slotTransform.Find(RangeLabelName);
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }
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
