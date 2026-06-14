using System.Reflection;
using Eremite.Buildings;
using Eremite.Buildings.UI;
using Eremite.Characters;
using Eremite.Characters.Villagers;
using StockAlert.Config;
using StockAlert.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace StockAlert.UI.World
{
    internal sealed class WorkerSlotHoverHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static FieldInfo _fiActor;

        private BuildingWorkerSlot _slot;

        public static void Attach(BuildingWorkerSlot slot)
        {
            if (!ConfigManager.ShowWorkerHoverHighlight ||
                slot == null ||
                slot.GetComponent<WorkerSlotHoverHighlighter>() != null)
            {
                return;
            }

            slot.gameObject.AddComponent<WorkerSlotHoverHighlighter>()._slot = slot;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!ConfigManager.ShowWorkerHoverHighlight)
            {
                return;
            }

            _fiActor ??= typeof(BuildingWorkerSlot).GetField("actor", BindingFlags.Instance | BindingFlags.NonPublic);
            var actor = _fiActor?.GetValue(_slot) as Actor;
            if (actor is Villager villager)
            {
                WorkerHoverRing.Show(villager);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            WorkerHoverRing.Hide();
        }

        private void OnDisable()
        {
            WorkerHoverRing.Hide();
        }
    }

    internal sealed class WorkerRaceMenuHoverHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static FieldInfo _fiBuilding;

        private RacesMenuSlot _slot;

        public static void Attach(RacesMenuSlot slot)
        {
            if (!ConfigManager.ShowWorkerHoverHighlight ||
                slot == null ||
                slot.GetComponent<WorkerRaceMenuHoverHighlighter>() != null)
            {
                return;
            }

            slot.gameObject.AddComponent<WorkerRaceMenuHoverHighlighter>()._slot = slot;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowCandidate(_slot);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            WorkerHoverRing.Hide();
        }

        private void OnDisable()
        {
            WorkerHoverRing.Hide();
        }

        public static void ShowCandidate(RacesMenuSlot slot)
        {
            if (!ConfigManager.ShowWorkerHoverHighlight)
            {
                WorkerHoverRing.Hide();
                return;
            }

            var race = slot?.GetRace();
            if (race == null)
            {
                return;
            }

            _fiBuilding ??= typeof(RacesMenuSlot).GetField("building", BindingFlags.Instance | BindingFlags.NonPublic);
            var building = _fiBuilding?.GetValue(slot) as ProductionBuilding;
            var villager = GameAPI.GetDefaultProfessionVillager(race.Name, building);
            if (villager != null)
            {
                WorkerHoverRing.Show(villager);
            }
        }
    }

    internal static class WorkerHoverRing
    {
        private const float RingWorldSize = 0.9f;
        private const float ColumnWorldWidth = 0.38f;
        private const float ColumnWorldHeight = 3.1f;
        private const float ColumnBaseOffset = 0.08f;
        private static GameObject _ringObject;
        private static Transform _ringTransform;
        private static Transform _columnTransform;
        private static SpriteRenderer _ringRenderer;
        private static SpriteRenderer _columnRenderer;
        private static Transform _target;
        private static Sprite _ringSprite;
        private static Sprite _columnSprite;
        private static Renderer[] _targetRenderers;

        public static void Show(Villager villager)
        {
            var actorView = villager?.ActorView;
            if (actorView == null)
            {
                Hide();
                return;
            }

            EnsureRing();
            if (_ringObject == null || _ringRenderer == null || _columnRenderer == null)
            {
                return;
            }

            _target = actorView.transform;
            _targetRenderers = actorView.GetComponentsInChildren<Renderer>(true);
            _ringObject.SetActive(true);
            UpdateTransform();
        }

        public static void Hide()
        {
            _target = null;
            _targetRenderers = null;
            if (_ringObject != null)
            {
                _ringObject.SetActive(false);
            }
        }

        private static void EnsureRing()
        {
            if (_ringObject != null)
            {
                return;
            }

            _ringSprite ??= CreateRingSprite();
            _columnSprite ??= CreateColumnSprite();
            _ringObject = new GameObject("StockAlertWorkerHoverMarker");
            _ringTransform = CreateRendererChild("Ring", _ringSprite, 5200, out _ringRenderer);
            _columnTransform = CreateRendererChild("Column", _columnSprite, 5201, out _columnRenderer);
            _columnRenderer.material = CreateAlwaysVisibleColumnMaterial();
            _ringObject.AddComponent<WorkerHoverRingUpdater>();
            _ringObject.SetActive(false);
        }

        private static void UpdateTransform()
        {
            if (_ringObject == null || _target == null || _ringSprite == null || _columnSprite == null)
            {
                return;
            }

            _ringObject.transform.position = _target.position;

            if (_ringTransform != null)
            {
                _ringTransform.localPosition = new Vector3(0f, 0.035f, 0f);
                _ringTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                var maxDimension = Mathf.Max(_ringSprite.bounds.size.x, _ringSprite.bounds.size.y);
                var baseScale = maxDimension > 0.001f ? RingWorldSize / maxDimension : 1f;
                var ringPulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f;
                _ringTransform.localScale = Vector3.one * baseScale * ringPulse;
            }

            var camera = Camera.main;
            if (_columnTransform != null)
            {
                _columnTransform.position = _target.position + Vector3.up * ColumnBaseOffset;
                if (camera != null)
                {
                    _columnTransform.forward = camera.transform.forward;
                }

                var bounds = _columnSprite.bounds;
                var widthScale = bounds.size.x > 0.001f ? ColumnWorldWidth / bounds.size.x : 1f;
                var heightScale = bounds.size.y > 0.001f ? ColumnWorldHeight / bounds.size.y : 1f;
                var columnPulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f;
                _columnTransform.localScale = new Vector3(widthScale * columnPulse, heightScale, 1f);
                ApplyColumnSortingBehindTarget();
            }

            var alpha = 0.82f + Mathf.Sin(Time.unscaledTime * 6f) * 0.18f;
            if (_ringRenderer != null)
            {
                _ringRenderer.color = new Color(1f, 0.92f, 0.18f, alpha);
            }

            if (_columnRenderer != null)
            {
                _columnRenderer.color = new Color(1f, 0.92f, 0.18f, alpha * 0.75f);
            }
        }

        private static Transform CreateRendererChild(string name, Sprite sprite, int sortingOrder, out SpriteRenderer renderer)
        {
            var child = new GameObject("StockAlertWorkerHover" + name);
            child.transform.SetParent(_ringObject.transform, false);
            renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.sortingLayerName = "UI";
            renderer.sortingOrder = sortingOrder;
            return child.transform;
        }

        private static void ApplyColumnSortingBehindTarget()
        {
            if (_columnRenderer == null || _targetRenderers == null || _targetRenderers.Length == 0)
            {
                return;
            }

            Renderer targetRenderer = null;
            foreach (var candidate in _targetRenderers)
            {
                if (candidate == null || candidate == _columnRenderer)
                {
                    continue;
                }

                if (targetRenderer == null || IsBefore(candidate, targetRenderer))
                {
                    targetRenderer = candidate;
                }
            }

            if (targetRenderer == null)
            {
                return;
            }

            _columnRenderer.sortingLayerID = targetRenderer.sortingLayerID;
            _columnRenderer.sortingOrder = targetRenderer.sortingOrder - 1;
        }

        private static bool IsBefore(Renderer left, Renderer right)
        {
            var leftLayer = SortingLayer.GetLayerValueFromID(left.sortingLayerID);
            var rightLayer = SortingLayer.GetLayerValueFromID(right.sortingLayerID);
            return leftLayer < rightLayer || leftLayer == rightLayer && left.sortingOrder < right.sortingOrder;
        }

        private static Material CreateAlwaysVisibleColumnMaterial()
        {
            var material = new Material(Shader.Find("GUI/Text Shader") ?? Shader.Find("Sprites/Default"));
            material.renderQueue = (int)RenderQueue.Overlay;
            material.SetInt("_ZTest", (int)CompareFunction.Always);
            material.SetInt("_ZWrite", 0);
            return material;
        }

        private static Sprite CreateRingSprite()
        {
            const int size = 128;
            const float outerRadius = 56f;
            const float innerRadius = 43f;
            const float glowRadius = 62f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "StockAlertWorkerHoverRingTexture";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    var ringAlpha = Mathf.Clamp01(Mathf.Min(distance - innerRadius, outerRadius - distance) / 2.5f);
                    var glowAlpha = Mathf.Clamp01((glowRadius - distance) / (glowRadius - outerRadius)) * 0.35f;
                    var alpha = distance >= innerRadius && distance <= outerRadius
                        ? Mathf.Max(ringAlpha, glowAlpha)
                        : glowAlpha;

                    pixels[y * size + x] = new Color(1f, 0.88f, 0.12f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateColumnSprite()
        {
            const int width = 64;
            const int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "StockAlertWorkerHoverColumnTexture";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            var centerX = (width - 1) * 0.5f;
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var normalizedY = y / (float)(height - 1);
                var widthAtY = Mathf.Lerp(18f, 7f, normalizedY);
                var verticalFade = Mathf.Sin(normalizedY * Mathf.PI);
                var topCap = Mathf.Clamp01((1f - normalizedY) / 0.18f);
                var bottomCap = Mathf.Clamp01(normalizedY / 0.08f);
                for (var x = 0; x < width; x++)
                {
                    var horizontalDistance = Mathf.Abs(x - centerX);
                    var core = Mathf.Clamp01((widthAtY - horizontalDistance) / 3.5f);
                    var glow = Mathf.Clamp01((widthAtY + 9f - horizontalDistance) / 9f) * 0.35f;
                    var alpha = Mathf.Max(core, glow) * verticalFade * topCap * bottomCap;

                    pixels[y * width + x] = new Color(1f, 0.88f, 0.12f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), height);
        }

        private sealed class WorkerHoverRingUpdater : MonoBehaviour
        {
            private void LateUpdate()
            {
                if (_target == null)
                {
                    if (_ringObject != null)
                    {
                        _ringObject.SetActive(false);
                    }

                    return;
                }

                UpdateTransform();
            }
        }
    }
}
