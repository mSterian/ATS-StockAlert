using System;
using System.Collections.Generic;
using System.Linq;
using Eremite.Buildings;
using StockAlert.Game;
using TMPro;
using UnityEngine;

namespace StockAlert.UI.World
{
    internal static class WarehouseHaulerIndicators
    {
        private static readonly Dictionary<int, WarehouseHaulerIndicator> ActiveIndicators =
            new Dictionary<int, WarehouseHaulerIndicator>();
        private static Sprite _haulerSprite;

        public static void Refresh()
        {
            if (!GameAPI.IsGameActive())
            {
                Clear();
                return;
            }

            var seenStorages = new HashSet<int>();
            foreach (var storage in GameAPI.GetStorageBuildings())
            {
                if (storage?.BuildingView == null ||
                    storage.BuildingState == null ||
                    !storage.BuildingState.finished ||
                    storage.BuildingState.isSleeping)
                {
                    continue;
                }

                var haulers = storage.CountWorkers();
                if (haulers <= 0)
                {
                    continue;
                }

                seenStorages.Add(storage.Id);
                if (!ActiveIndicators.TryGetValue(storage.Id, out var indicator))
                {
                    indicator = new WarehouseHaulerIndicator(storage);
                    ActiveIndicators[storage.Id] = indicator;
                }

                indicator.Show(haulers);
            }

            var removedIds = ActiveIndicators.Keys.Where(id => !seenStorages.Contains(id)).ToList();
            foreach (var removedId in removedIds)
            {
                ActiveIndicators[removedId].Destroy();
                ActiveIndicators.Remove(removedId);
            }
        }

        public static void Clear()
        {
            foreach (var indicator in ActiveIndicators.Values)
            {
                indicator.Destroy();
            }

            ActiveIndicators.Clear();
        }

        private sealed class WarehouseHaulerIndicator
        {
            private const float MarkerHeightOffset = 0.45f;
            private readonly Storage _storage;
            private readonly GameObject _root;
            private readonly SpriteRenderer _iconRenderer;
            private readonly TextMeshPro _countText;

            public WarehouseHaulerIndicator(Storage storage)
            {
                _storage = storage;
                _root = new GameObject("StockAlertWarehouseHaulers");
                _root.transform.localScale = Vector3.one * 0.62f;
                _root.AddComponent<BillboardToCamera>();

                var iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(_root.transform, false);
                iconObject.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
                _iconRenderer = iconObject.AddComponent<SpriteRenderer>();
                _iconRenderer.material = new Material(Shader.Find("Sprites/Default"));
                _iconRenderer.sprite = GetHaulerSprite();
                _iconRenderer.sortingLayerName = "UI";
                _iconRenderer.sortingOrder = 5100;

                var textObject = new GameObject("Count");
                textObject.transform.SetParent(_root.transform, false);
                textObject.transform.localPosition = new Vector3(0.26f, -0.03f, 0f);
                _countText = textObject.AddComponent<TextMeshPro>();
                _countText.font = TMP_Settings.defaultFontAsset;
                _countText.alignment = TextAlignmentOptions.Center;
                _countText.textWrappingMode = TextWrappingModes.NoWrap;
                _countText.fontSize = 5f;
                _countText.fontStyle = FontStyles.Bold;
                _countText.color = Color.white;
                _countText.outlineColor = Color.black;
                _countText.outlineWidth = 0.25f;
                _countText.renderer.sortingLayerName = "UI";
                _countText.renderer.sortingOrder = 5101;
            }

            public void Show(int haulers)
            {
                if (_root == null)
                {
                    return;
                }

                if (_countText != null)
                {
                    _countText.text = haulers.ToString();
                    _countText.enabled = true;
                }

                _root.transform.position = GetMarkerWorldPosition(_storage);
                if (!_root.activeSelf)
                {
                    _root.SetActive(true);
                }
            }

            private static Vector3 GetMarkerWorldPosition(Storage storage)
            {
                var anchor = storage?.BuildingView?.transform?.Find("ToRotate/UI/NoWorkersIcon");
                if (anchor != null)
                {
                    return anchor.position;
                }

                var renderers = storage?.BuildingView != null
                    ? storage.BuildingView.GetComponentsInChildren<Renderer>(true)
                        .Where(renderer => renderer != null && !IsHaulerIndicatorRenderer(renderer.transform))
                        .ToArray()
                    : Array.Empty<Renderer>();
                if (renderers.Length <= 0)
                {
                    return storage?.BuildingView != null
                        ? storage.BuildingView.transform.position + new Vector3(0f, 2.4f, 0f)
                        : Vector3.zero;
                }

                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                return new Vector3(bounds.center.x, bounds.max.y + MarkerHeightOffset, bounds.center.z);
            }

            private static bool IsHaulerIndicatorRenderer(Transform transform)
            {
                while (transform != null)
                {
                    if (string.Equals(transform.name, "StockAlertWarehouseHaulers", StringComparison.Ordinal))
                    {
                        return true;
                    }

                    transform = transform.parent;
                }

                return false;
            }

            public void Destroy()
            {
                if (_root != null)
                {
                    UnityEngine.Object.Destroy(_root);
                }
            }
        }

        private static Sprite GetHaulerSprite()
        {
            if (_haulerSprite != null)
            {
                return _haulerSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            texture.SetPixels(Enumerable.Repeat(clear, size * size).ToArray());
            DrawHaulerIcon(texture);
            texture.Apply(false, true);
            _haulerSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return _haulerSprite;
        }

        private static void DrawHaulerIcon(Texture2D texture)
        {
            var outline = new Color(0.03f, 0.02f, 0.015f, 1f);
            var skin = new Color(1f, 0.82f, 0.48f, 1f);
            var coat = new Color(0.98f, 0.62f, 0.13f, 1f);
            var sack = new Color(0.84f, 0.56f, 0.27f, 1f);
            var highlight = new Color(1f, 0.92f, 0.52f, 1f);

            FillCircle(texture, 28, 44, 7, outline);
            FillCircle(texture, 28, 44, 5, skin);
            FillEllipse(texture, 38, 41, 12, 9, outline);
            FillEllipse(texture, 39, 42, 9, 7, sack);
            FillCircle(texture, 35, 45, 2, highlight);
            DrawLine(texture, 30, 38, 22, 24, 8, outline);
            DrawLine(texture, 30, 38, 22, 24, 5, coat);
            DrawLine(texture, 24, 34, 38, 40, 5, outline);
            DrawLine(texture, 25, 35, 37, 40, 3, skin);
            DrawLine(texture, 23, 25, 14, 11, 5, outline);
            DrawLine(texture, 23, 25, 14, 11, 3, coat);
            DrawLine(texture, 23, 25, 32, 11, 5, outline);
            DrawLine(texture, 23, 25, 32, 11, 3, coat);
            DrawLine(texture, 14, 11, 9, 8, 4, outline);
            DrawLine(texture, 32, 11, 38, 8, 4, outline);
        }

        private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            FillEllipse(texture, centerX, centerY, radius, radius, color);
        }

        private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (var y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (var x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    var dx = (x - centerX) / (float)radiusX;
                    var dy = (y - centerY) / (float)radiusY;
                    if (dx * dx + dy * dy <= 1f)
                    {
                        SetPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            var steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps <= 0)
            {
                FillCircle(texture, x0, y0, Mathf.Max(1, thickness / 2), color);
                return;
            }

            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                var y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                FillCircle(texture, x, y, Mathf.Max(1, thickness / 2), color);
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private sealed class BillboardToCamera : MonoBehaviour
        {
            private void LateUpdate()
            {
                var main = Camera.main;
                if (main != null)
                {
                    transform.forward = main.transform.forward;
                }
            }
        }
    }
}
