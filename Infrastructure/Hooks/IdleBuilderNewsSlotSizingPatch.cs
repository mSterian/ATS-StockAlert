using System;
using System.Collections.Generic;
using System.Reflection;
using Eremite.View.HUD.Monitors;
using HarmonyLib;
using StockAlert.UI.World;
using TMPro;
using UnityEngine;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch(typeof(BaseAlertHUDSlot), "SetUp")]
    internal static class IdleBuilderNewsSlotSizingPatch
    {
        private const float VerticalPadding = 22f;
        private static readonly FieldInfo ContentField = AccessTools.Field(typeof(BaseAlertHUDSlot), "content");
        private static readonly FieldInfo ContentTextField = AccessTools.Field(typeof(BaseAlertHUDSlot), "contentText");
        private static readonly Dictionary<int, Vector2> OriginalSlotSizes = new Dictionary<int, Vector2>();
        private static readonly Dictionary<int, Vector2> OriginalContentSizes = new Dictionary<int, Vector2>();

        [HarmonyPostfix]
        private static void ResizeIdleBuilderNewsSlot(BaseAlertHUDSlot __instance)
        {
            if (__instance == null)
            {
                return;
            }

            try
            {
                var text = ContentTextField?.GetValue(__instance) as TMP_Text;
                var content = ContentField?.GetValue(__instance) as RectTransform;
                var slot = __instance.RectTransform;
                if (text == null || content == null || slot == null)
                {
                    return;
                }

                RememberOriginalSize(slot, OriginalSlotSizes);
                RememberOriginalSize(content, OriginalContentSizes);

                if (!IsIdleBuilderAlert(text.text))
                {
                    RestoreSize(slot, OriginalSlotSizes);
                    RestoreSize(content, OriginalContentSizes);
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    return;
                }

                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Overflow;

                var originalSlotSize = OriginalSlotSizes[slot.GetInstanceID()];
                var originalContentSize = OriginalContentSizes[content.GetInstanceID()];
                var preferredHeight = text.GetPreferredValues(text.text, text.rectTransform.rect.width, 0f).y + VerticalPadding;
                var targetSlotHeight = Mathf.Max(originalSlotSize.y, preferredHeight);
                var targetContentHeight = Mathf.Max(originalContentSize.y, preferredHeight);

                SetHeight(slot, targetSlotHeight);
                SetHeight(content, targetContentHeight);
            }
            catch (Exception)
            {
            }
        }

        private static bool IsIdleBuilderAlert(string text)
        {
            return !string.IsNullOrWhiteSpace(text) && text.StartsWith(IdleBuildersAlert.AlertPrefix, StringComparison.Ordinal);
        }

        private static void RememberOriginalSize(RectTransform rect, Dictionary<int, Vector2> sizes)
        {
            var id = rect.GetInstanceID();
            if (!sizes.ContainsKey(id))
            {
                sizes[id] = rect.rect.size;
            }
        }

        private static void RestoreSize(RectTransform rect, Dictionary<int, Vector2> sizes)
        {
            if (sizes.TryGetValue(rect.GetInstanceID(), out var size))
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            }
        }

        private static void SetHeight(RectTransform rect, float height)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }
}
