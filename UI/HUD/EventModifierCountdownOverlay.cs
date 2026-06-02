using System;
using Eremite;
using Eremite.Model;
using Eremite.Model.Effects;
using Eremite.Model.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StockAlert.UI.HUD
{
    public sealed class EventModifierCountdownOverlay : GameMB
    {
        private const string RootName = "StockAlertEventModifierCountdown";
        private const float BadgeHeight = 16f;

        private PerkState _state;
        private GameObject _root;
        private TMP_Text _text;
        private string _lastText;
        private EffectModel _effect;

        public void SetUp(PerkState state)
        {
            _state = state;
            _effect = null;
            EnsureView();
            ClearCountdown();
            UpdateCountdown(true);
        }

        public void SetUp(EffectModel effect)
        {
            _state = null;
            _effect = effect;
            EnsureView();
            ClearCountdown();
            UpdateCountdown(true);
        }

        private void Update()
        {
            UpdateCountdown(false);
        }

        private void OnDisable()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void EnsureView()
        {
            if (_root != null)
            {
                return;
            }

            var parent = transform as RectTransform;
            if (parent == null)
            {
                return;
            }

            _root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _root.transform.SetParent(parent, false);
            _root.transform.SetAsLastSibling();

            var rect = _root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, BadgeHeight);

            var image = _root.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.72f);
            image.raycastTarget = false;

            var canvasGroup = _root.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(_root.transform, false);

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _text = textObject.GetComponent<TextMeshProUGUI>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = Color.white;
            _text.enableAutoSizing = true;
            _text.fontSizeMin = 8f;
            _text.fontSizeMax = 13f;
            _text.fontStyle = FontStyles.Bold;
            _text.margin = new Vector4(1f, 0f, 1f, 0f);
            _text.raycastTarget = false;
        }

        private void UpdateCountdown(bool force)
        {
            if (_root == null || _text == null)
            {
                return;
            }

            if (!TryGetCountdown(out var secondsLeft))
            {
                ClearCountdown();
                return;
            }

            var text = FormatTimer(secondsLeft);
            if (!force && string.Equals(_lastText, text, StringComparison.Ordinal))
            {
                return;
            }

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _text.text = text;
            _lastText = text;
        }

        private void ClearCountdown()
        {
            _lastText = null;

            if (_text != null)
            {
                _text.text = string.Empty;
            }

            if (_root != null)
            {
                _root.SetActive(false);
            }

            ClearCountdownChildren(transform);
        }

        private void ClearCountdownChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (string.Equals(child.name, RootName, StringComparison.Ordinal)
                    && (_root == null || child.gameObject != _root))
                {
                    Destroy(child.gameObject);
                    continue;
                }

                ClearCountdownChildren(child);
            }
        }

        private bool TryGetCountdown(out float secondsLeft)
        {
            secondsLeft = 0f;

            try
            {
                var effect = _effect;
                if (effect == null)
                {
                    if (_state == null || string.IsNullOrWhiteSpace(_state.name))
                    {
                        return false;
                    }

                    effect = GameModelService.GetEffect(_state.name);
                    if (effect == null)
                    {
                        return false;
                    }
                }

                if (effect is not HookedEffectModel hookedEffect)
                {
                    return false;
                }

                if (IsSeasonalEffect(effect) || IsSeasonalEffect(hookedEffect))
                {
                    return false;
                }

                if (!MentionsRepeatingTimer(effect))
                {
                    return false;
                }

                var activeState = FindActiveState(effect.Name);
                if (activeState?.hooks == null)
                {
                    return false;
                }

                var found = false;
                var best = float.MaxValue;
                var hookCount = Mathf.Min(hookedEffect.hooks?.Length ?? 0, activeState.hooks.Length);
                for (var i = 0; i < hookCount; i++)
                {
                    if (hookedEffect.hooks[i] is not GameTimePassedHook hook || hook.seconds < 15f)
                    {
                        continue;
                    }

                    var hookState = activeState.hooks[i];
                    if (hookState == null || HasReachedLimit(hook, hookState))
                    {
                        continue;
                    }

                    var remaining = Mathf.Max(0f, hook.seconds - hookState.currentFloatAmount);
                    if (remaining < best)
                    {
                        best = remaining;
                        found = true;
                    }
                }

                secondsLeft = best;
                return found;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsSeasonalEffect(EffectModel effect)
        {
            try
            {
                if (effect == null)
                {
                    return false;
                }

                var seasonalEffects = HostilityService?.AllSimpleEffects;
                if (seasonalEffects == null)
                {
                    return false;
                }

                for (var i = 0; i < seasonalEffects.Count; i++)
                {
                    var seasonalEffect = seasonalEffects[i];
                    if (IsSameEffect(seasonalEffect?.effect, effect)
                        || IsSameName(seasonalEffect?.Name, effect.Name)
                        || IsSameName(seasonalEffect?.DisplayName, effect.DisplayName))
                    {
                        return true;
                    }
                }

                var conditionalEffects = HostilityService?.AllConditionalEffects;
                if (conditionalEffects != null)
                {
                    for (var i = 0; i < conditionalEffects.Count; i++)
                    {
                        var seasonalEffect = conditionalEffects[i];
                        if (IsSameName(seasonalEffect?.Name, effect.Name)
                            || IsSameName(seasonalEffect?.DisplayName, effect.DisplayName))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool MentionsRepeatingTimer(EffectModel effect)
        {
            if (effect == null)
            {
                return false;
            }

            return MentionsRepeatingTimer(effect.Description);
        }

        private static bool MentionsRepeatingTimer(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.IndexOf("every", StringComparison.OrdinalIgnoreCase) >= 0
                   && text.IndexOf("seconds", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSameEffect(EffectModel left, EffectModel right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return ReferenceEquals(left, right)
                   || IsSameName(left.Name, right.Name)
                   || IsSameName(left.DisplayName, right.DisplayName);
        }

        private static bool IsSameName(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left)
                   && !string.IsNullOrWhiteSpace(right)
                   && string.Equals(left, right, StringComparison.Ordinal);
        }

        private static bool HasReachedLimit(HookLogic hook, HookState hookState)
        {
            if (hook.limit <= 0)
            {
                return false;
            }

            return hook.isStaticLimit
                ? hookState.totalFiredAmount >= hook.limit
                : hookState.firedAmount >= hook.limit;
        }

        private static string FormatTimer(float seconds)
        {
            var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            var span = TimeSpan.FromSeconds(totalSeconds);
            return span.TotalHours < 1.0
                ? span.ToString("mm\\:ss")
                : span.ToString("hh\\:mm\\:ss");
        }

        private HookedEffectState FindActiveState(string modelName)
        {
            var states = StateService?.HookedEffects?.activeEffects;
            if (states == null)
            {
                return null;
            }

            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state != null
                    && !state.isRemovalHook
                    && state.model == modelName)
                {
                    return state;
                }
            }

            return null;
        }
    }
}
