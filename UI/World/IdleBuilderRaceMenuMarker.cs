using Eremite.Buildings.UI;
using StockAlert.Config;
using UnityEngine;
using UnityEngine.UI;

namespace StockAlert.UI.World
{
    internal sealed class IdleBuilderRaceMenuMarker : MonoBehaviour
    {
        private RacesMenuSlot _slot;
        private GameObject _markerObject;
        private Image _markerImage;
        private float _nextRefreshTime;

        public static void Attach(RacesMenuSlot slot)
        {
            if (slot == null)
            {
                return;
            }

            var marker = slot.GetComponent<IdleBuilderRaceMenuMarker>();
            if (marker == null)
            {
                marker = slot.gameObject.AddComponent<IdleBuilderRaceMenuMarker>();
                marker.Initialize(slot);
                return;
            }

            marker.Initialize(slot);
        }

        private void Initialize(RacesMenuSlot slot)
        {
            _slot = slot;
            CleanupLegacyMarkers();
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + 0.25f;
            Refresh();
        }

        private void Refresh()
        {
            if (_slot == null || !ConfigManager.ShowBuilderStatusIcons || !ConfigManager.ShowIdleBuilderStatusIcons)
            {
                Hide();
                return;
            }

            var race = _slot.GetRace();
            if (race == null || !BuilderStatusIndicators.HasIdleBuilderOfRace(race))
            {
                Hide();
                return;
            }

            var sprite = BuilderStatusIndicators.GetIdleBuilderSprite();
            if (sprite == null)
            {
                Hide();
                return;
            }

            EnsureMarker();
            if (_markerImage == null)
            {
                return;
            }

            _markerImage.sprite = sprite;
            _markerObject.SetActive(true);
        }

        private void EnsureMarker()
        {
            if (_markerObject != null || _slot == null)
            {
                return;
            }

            var parent = _slot.transform.Find("Content") as RectTransform;
            if (parent == null)
            {
                parent = _slot.GetComponent<RectTransform>();
            }

            if (parent == null)
            {
                return;
            }

            _markerObject = new GameObject("StockAlertIdleBuilderRaceMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _markerObject.transform.SetParent(parent, false);
            _markerImage = _markerObject.GetComponent<Image>();
            _markerImage.raycastTarget = false;
            _markerImage.preserveAspect = true;

            var rect = _markerObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(22f, 22f);
            rect.anchoredPosition = new Vector2(-2f, 0f);
            _markerObject.transform.SetAsLastSibling();
            _markerObject.SetActive(false);
        }

        private void CleanupLegacyMarkers()
        {
            if (_slot == null)
            {
                return;
            }

            var parent = _slot.transform.Find("Content");
            if (parent == null)
            {
                return;
            }

            DestroyChild(parent, "IdleBuilderRaceMarker");
            DestroyChild(parent, "StockAlertIdleBuilderRaceMarker");
            _markerObject = null;
            _markerImage = null;
        }

        private static void DestroyChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        private void Hide()
        {
            if (_markerObject != null)
            {
                _markerObject.SetActive(false);
            }
        }
    }
}
