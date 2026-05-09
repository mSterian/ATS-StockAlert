using System;
using System.Collections.Generic;
using System.Linq;
using Eremite.MapObjects.UI;
using Eremite.Model;
using Eremite.View.UI;
using StockAlert.Core.Models;
using StockAlert.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StockAlert.UI.World
{
    internal static class ItemLocatorOverlay
    {
        private const float MarkerHeightOffset = 0.55f;

        private static readonly Dictionary<string, GameObject> Markers = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        private static string _trackedGoodId;
        private static GameObject _template;
        private static string _lastToggledGoodId;
        private static int _lastToggleFrame = -1;

        public static bool IsTracking => !string.IsNullOrWhiteSpace(_trackedGoodId);

        public static void Toggle(string goodId)
        {
            if (string.IsNullOrWhiteSpace(goodId))
            {
                return;
            }

            if (_lastToggleFrame == Time.frameCount &&
                string.Equals(_lastToggledGoodId, goodId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _lastToggleFrame = Time.frameCount;
            _lastToggledGoodId = goodId;

            if (string.Equals(_trackedGoodId, goodId, StringComparison.OrdinalIgnoreCase))
            {
                Clear();
                return;
            }

            _trackedGoodId = goodId;
            Refresh();
        }

        public static void Clear()
        {
            _trackedGoodId = null;

            foreach (var marker in Markers.Values)
            {
                if (marker != null)
                {
                    UnityEngine.Object.Destroy(marker);
                }
            }

            Markers.Clear();
        }

        public static void Refresh()
        {
            if (!GameAPI.IsGameActive() || string.IsNullOrWhiteSpace(_trackedGoodId))
            {
                Clear();
                return;
            }

            var goodModel = GameAPI.GetSettings()?.GetGood(_trackedGoodId);
            var icon = goodModel?.icon;
            if (icon == null)
            {
                Clear();
                return;
            }

            var template = ResolveTemplate();
            if (template == null)
            {
                Clear();
                return;
            }

            var locations = GameAPI.GetTrackedItemLocations(_trackedGoodId);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var location in locations.Where(l => l != null && !string.IsNullOrWhiteSpace(l.Key)))
            {
                UpsertMarker(location, icon, template);
                seen.Add(location.Key);
            }

            var staleKeys = Markers.Keys.Where(key => !seen.Contains(key)).ToList();
            foreach (var key in staleKeys)
            {
                if (Markers.TryGetValue(key, out var marker) && marker != null)
                {
                    UnityEngine.Object.Destroy(marker);
                }

                Markers.Remove(key);
            }
        }

        private static void UpsertMarker(TrackedItemLocation location, Sprite icon, GameObject template)
        {
            var worldPosition = GetMarkerWorldPosition(location);

            if (!Markers.TryGetValue(location.Key, out var marker) || marker == null)
            {
                marker = UnityEngine.Object.Instantiate(template, template.transform.parent);
                marker.name = $"StockAlertItemLocator_{location.Key}";
                Markers[location.Key] = marker;
            }

            ApplyVisuals(marker, icon, location);
            ApplyTarget(marker, worldPosition);
            marker.SetActive(true);
        }

        private static void ApplyVisuals(GameObject marker, Sprite icon, TrackedItemLocation location)
        {
            var iconImage = marker.transform.Find("Icon")?.GetComponent<Image>()
                            ?? marker.transform.Find("Mask/Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
                iconImage.raycastTarget = false;
            }

            var grade = marker.transform.Find("Grade");
            if (grade != null)
            {
                grade.gameObject.SetActive(false);
            }

            var highlight = marker.transform.Find("BG")?.GetComponent<Image>();
            if (highlight != null)
            {
                highlight.raycastTarget = false;
            }

            foreach (var image in marker.GetComponentsInChildren<Image>(true))
            {
                image.raycastTarget = false;
            }

            ApplyAmountBadge(marker, location);
        }

        private static void ApplyAmountBadge(GameObject marker, TrackedItemLocation location)
        {
            var badge = GetOrCreateAmountBadge(marker);
            if (badge == null)
            {
                return;
            }

            var shouldShow = location != null
                && location.Amount > 0
                && location.Key.StartsWith("building:", StringComparison.OrdinalIgnoreCase);

            badge.SetActive(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            var text = badge.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = location.Amount.ToString();
            }
        }

        private static GameObject GetOrCreateAmountBadge(GameObject marker)
        {
            var existing = marker.transform.Find("StockAlertAmountBadge");
            if (existing != null)
            {
                return existing.gameObject;
            }

            var root = marker.GetComponent<RectTransform>() ?? marker.GetComponentInChildren<RectTransform>(true);
            if (root == null)
            {
                return null;
            }

            var badge = new GameObject("StockAlertAmountBadge", typeof(RectTransform));
            var badgeTransform = badge.GetComponent<RectTransform>();
            badgeTransform.SetParent(root, false);
            badgeTransform.anchorMin = new Vector2(1f, 0f);
            badgeTransform.anchorMax = new Vector2(1f, 0f);
            badgeTransform.pivot = new Vector2(1f, 0f);
            badgeTransform.anchoredPosition = new Vector2(8f, -8f);
            badgeTransform.sizeDelta = new Vector2(32f, 20f);

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textTransform = textObject.GetComponent<RectTransform>();
            textTransform.SetParent(badgeTransform, false);
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = Vector2.zero;
            textTransform.offsetMax = Vector2.zero;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 15f;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.99f, 0.9f, 1f);
            text.raycastTarget = false;
            text.margin = new Vector4(1f, 0f, 1f, 0f);
            text.fontSharedMaterial = text.fontMaterial;
            text.outlineWidth = 0.25f;
            text.outlineColor = new Color(0.05f, 0.05f, 0.05f, 1f);

            return badge;
        }

        private static void ApplyTarget(GameObject marker, Vector3 worldPosition)
        {
            var indicator = marker.GetComponent<WorldIndicator>();
            if (indicator == null)
            {
                indicator = marker.GetComponentInChildren<WorldIndicator>(true);
            }

            indicator?.SetTarget(worldPosition);
        }

        private static Vector3 GetMarkerWorldPosition(TrackedItemLocation location)
        {
            if (location.Source is Component component && component != null)
            {
                var renderers = component
                    .GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer != null && !IsLocatorRenderer(renderer.transform))
                    .ToArray();

                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    for (var i = 1; i < renderers.Length; i++)
                    {
                        bounds.Encapsulate(renderers[i].bounds);
                    }

                    return new Vector3(bounds.center.x, bounds.max.y + MarkerHeightOffset, bounds.center.z);
                }
            }

            return location.FallbackPosition + new Vector3(0f, MarkerHeightOffset, 0f);
        }

        private static bool IsLocatorRenderer(Transform transform)
        {
            while (transform != null)
            {
                if (transform.name.StartsWith("StockAlertItemLocator_", StringComparison.Ordinal))
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        private static GameObject ResolveTemplate()
        {
            if (_template != null)
            {
                return _template;
            }

            _template =
                Resources.FindObjectsOfTypeAll<DepositIndicator>().FirstOrDefault(IsSceneObject)?.gameObject ??
                Resources.FindObjectsOfTypeAll<LakeIndicator>().FirstOrDefault(IsSceneObject)?.gameObject ??
                Resources.FindObjectsOfTypeAll<OreIndicator>().FirstOrDefault(IsSceneObject)?.gameObject ??
                Resources.FindObjectsOfTypeAll<BuildingIndicator>().FirstOrDefault(IsSceneObject)?.gameObject;

            return _template;
        }

        private static bool IsSceneObject(Component component)
        {
            return component != null && component.gameObject.scene.IsValid();
        }
    }
}
