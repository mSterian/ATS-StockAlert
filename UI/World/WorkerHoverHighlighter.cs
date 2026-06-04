using System.Reflection;
using Eremite.Buildings;
using Eremite.Buildings.UI;
using Eremite.Characters;
using Eremite.Characters.Villagers;
using StockAlert.Config;
using StockAlert.Game;
using UnityEngine;
using UnityEngine.EventSystems;

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
        private static GameObject _ringObject;
        private static SpriteRenderer _renderer;
        private static Transform _target;
        private static Sprite _ringSprite;

        public static void Show(Villager villager)
        {
            var actorView = villager?.ActorView;
            if (actorView == null)
            {
                Hide();
                return;
            }

            EnsureRing();
            if (_ringObject == null || _renderer == null)
            {
                return;
            }

            _target = actorView.transform;
            _ringObject.SetActive(true);
            UpdateTransform();
        }

        public static void Hide()
        {
            _target = null;
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
            _ringObject = new GameObject("StockAlertWorkerHoverRing");
            _renderer = _ringObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = _ringSprite;
            _renderer.material = new Material(Shader.Find("Sprites/Default"));
            _renderer.sortingLayerName = "UI";
            _renderer.sortingOrder = 5200;
            _ringObject.AddComponent<WorkerHoverRingUpdater>();
            _ringObject.SetActive(false);
        }

        private static void UpdateTransform()
        {
            if (_ringObject == null || _target == null || _ringSprite == null)
            {
                return;
            }

            _ringObject.transform.position = _target.position + new Vector3(0f, 0.035f, 0f);
            _ringObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var maxDimension = Mathf.Max(_ringSprite.bounds.size.x, _ringSprite.bounds.size.y);
            var baseScale = maxDimension > 0.001f ? RingWorldSize / maxDimension : 1f;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.06f;
            _ringObject.transform.localScale = Vector3.one * baseScale * pulse;

            if (_renderer != null)
            {
                var alpha = 0.82f + Mathf.Sin(Time.unscaledTime * 6f) * 0.18f;
                _renderer.color = new Color(1f, 0.92f, 0.18f, alpha);
            }
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
