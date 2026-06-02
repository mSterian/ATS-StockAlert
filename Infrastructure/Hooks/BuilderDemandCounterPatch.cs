using System.Reflection;
using Eremite.View.HUD;
using HarmonyLib;
using StockAlert.Config;
using StockAlert.UI.World;
using TMPro;
using System.Collections.Generic;
using UnityEngine;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch(typeof(RacesStatsHUD))]
    internal static class BuilderDemandCounterPatch
    {
        private const float TextPadding = 3f;
        private const float MaxExtraWidth = 80f;

        private static readonly FieldInfo FreeWorkersCounterField =
            AccessTools.Field(typeof(RacesStatsHUD), "freeWorkersCounter");

        private static readonly FieldInfo CounterCurrentField =
            AccessTools.Field(typeof(CounterInt), "current");

        private static readonly FieldInfo CounterTextField =
            AccessTools.Field(typeof(CounterInt), "text");

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartPostfix(RacesStatsHUD __instance)
        {
            Apply(__instance);
        }

        [HarmonyPatch("SlowUpdate")]
        [HarmonyPostfix]
        private static void SlowUpdatePostfix(RacesStatsHUD __instance)
        {
            Apply(__instance);
        }

        private static void Apply(RacesStatsHUD hud)
        {
            if (!ConfigManager.ShowBuilderDemandCounter || hud == null)
            {
                return;
            }

            var counter = FreeWorkersCounterField?.GetValue(hud) as CounterInt;
            var text = CounterTextField?.GetValue(counter) as TMP_Text;
            if (counter == null || text == null)
            {
                return;
            }

            var available = CounterCurrentField?.GetValue(counter) is int current ? current : 0;
            var demand = BuilderStatusIndicators.GetBuilderDemandCounts();
            var value = $"{available}/{demand.OpenSlots}";
            text.text = value;
            FitCounterText(counter, text, hud, value);
        }

        private static void FitCounterText(CounterInt counter, TMP_Text text, RacesStatsHUD hud, string value)
        {
            var marker = counter.GetComponent<BuilderDemandCounterLayoutMarker>();
            if (marker == null)
            {
                marker = counter.gameObject.AddComponent<BuilderDemandCounterLayoutMarker>();
                marker.Capture(counter, text, hud);
            }

            marker.Apply(text.GetPreferredValues(value).x + TextPadding);
        }

        private sealed class BuilderDemandCounterLayoutMarker : MonoBehaviour
        {
            private RectState _counterRect;
            private RectState _textRect;
            private readonly List<RectState> _followingSiblings = new List<RectState>();
            private readonly List<RectState> _parents = new List<RectState>();

            public void Capture(CounterInt counter, TMP_Text text, RacesStatsHUD hud)
            {
                _counterRect = RectState.Capture(counter.transform as RectTransform);
                _textRect = RectState.Capture(text.rectTransform);

                var counterTransform = counter.transform;
                var parent = counterTransform.parent;
                if (parent != null)
                {
                    var counterIndex = counterTransform.GetSiblingIndex();
                    for (var i = counterIndex + 1; i < parent.childCount; i++)
                    {
                        var sibling = parent.GetChild(i) as RectTransform;
                        if (sibling != null)
                        {
                            _followingSiblings.Add(RectState.Capture(sibling));
                        }
                    }
                }

                while (parent != null && parent != hud.transform.parent)
                {
                    if (parent is RectTransform rect)
                    {
                        _parents.Add(RectState.Capture(rect));
                    }

                    parent = parent.parent;
                }
            }

            public void Apply(float preferredTextWidth)
            {
                if (!_counterRect.IsValid || !_textRect.IsValid)
                {
                    return;
                }

                var extraWidth = Mathf.Clamp(preferredTextWidth - _textRect.Width, 0f, MaxExtraWidth);
                _textRect.Apply(extraWidth, 0f);
                _counterRect.Apply(extraWidth, 0f);

                foreach (var sibling in _followingSiblings)
                {
                    sibling.Apply(0f, extraWidth);
                }

                foreach (var parent in _parents)
                {
                    parent.Apply(extraWidth, 0f);
                }
            }

            private readonly struct RectState
            {
                private readonly RectTransform _rect;
                private readonly Vector2 _sizeDelta;
                private readonly Vector2 _anchoredPosition;
                private readonly float _width;

                private RectState(RectTransform rect)
                {
                    _rect = rect;
                    _sizeDelta = rect != null ? rect.sizeDelta : Vector2.zero;
                    _anchoredPosition = rect != null ? rect.anchoredPosition : Vector2.zero;
                    _width = rect != null ? Mathf.Max(rect.rect.width, rect.sizeDelta.x) : 0f;
                }

                public bool IsValid => _rect != null;
                public float Width => _width;

                public static RectState Capture(RectTransform rect)
                {
                    return new RectState(rect);
                }

                public void Apply(float extraWidth, float shiftRight)
                {
                    if (_rect == null)
                    {
                        return;
                    }

                    _rect.anchoredPosition = _anchoredPosition + new Vector2(shiftRight, 0f);
                    if (_width > 0f)
                    {
                        _rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width + extraWidth);
                    }
                    else
                    {
                        _rect.sizeDelta = new Vector2(_sizeDelta.x + extraWidth, _sizeDelta.y);
                    }
                }
            }
        }
    }
}
