using System;
using System.Linq;
using System.Reflection;
using Eremite;
using Eremite.Buildings;
using Eremite.Buildings.UI;
using Eremite.Model;
using StockAlert.Game;
using UnityEngine;
using UnityEngine.UI;

namespace StockAlert.UI.World
{
    internal sealed class QueuedWorkerSlotCompanion : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<QueuedWorkerSlotCompanion> Instances = new System.Collections.Generic.List<QueuedWorkerSlotCompanion>();
        private static FieldInfo _fiBuilding;
        private static FieldInfo _fiWorkplace;
        private static FieldInfo _fiRaceIcon;
        private static PropertyInfo _piIsRemoved;
        private static PropertyInfo _piReactiveValue;

        private BuildingWorkerSlot _slot;
        private GameObject _queuedIconObject;
        private Image _queuedIconImage;
        private Button _queuedIconButton;
        private float _nextRefreshTime;

        public static void Attach(BuildingWorkerSlot slot)
        {
            if (slot == null || slot.GetComponent<QueuedWorkerSlotCompanion>() != null)
            {
                return;
            }

            slot.gameObject.AddComponent<QueuedWorkerSlotCompanion>().Initialize(slot);
        }

        public static void ClearAll()
        {
            foreach (var instance in Instances.ToArray())
            {
                instance?.Hide();
            }
        }

        private void Initialize(BuildingWorkerSlot slot)
        {
            _slot = slot;
            Instances.Add(this);
            EnsureUi();
            Refresh();
        }

        private void OnDestroy()
        {
            Instances.Remove(this);
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
            if (_slot == null)
            {
                Hide();
                return;
            }

            if (!Config.ConfigManager.EnableQueuedWorkerAssignments)
            {
                Hide();
                return;
            }

            if (!TryGetContext(out var building, out var workplace, out var workplaceIndex))
            {
                Hide();
                return;
            }

            if (IsBuildingRemoved(building))
            {
                WorkerAssignmentQueue.ClearQueuedRace(building, workplaceIndex);
                Hide();
                return;
            }

            var queuedRace = WorkerAssignmentQueue.GetQueuedRace(building, workplaceIndex);
            if (queuedRace == null)
            {
                Hide();
                return;
            }

            EnsureUi();
            _queuedIconImage.sprite = queuedRace.roundIcon;
            _queuedIconImage.material = null;
            _queuedIconObject.SetActive(true);
        }

        private bool TryGetContext(out ProductionBuilding building, out WorkplaceModel workplace, out int workplaceIndex)
        {
            building = null;
            workplace = null;
            workplaceIndex = -1;

            if (_slot == null)
            {
                return false;
            }

            _fiBuilding ??= typeof(BuildingWorkerSlot).GetField("building", BindingFlags.Instance | BindingFlags.NonPublic);
            _fiWorkplace ??= typeof(BuildingWorkerSlot).GetField("workplace", BindingFlags.Instance | BindingFlags.NonPublic);

            building = _fiBuilding?.GetValue(_slot) as ProductionBuilding;
            workplace = _fiWorkplace?.GetValue(_slot) as WorkplaceModel;
            if (building == null || workplace == null)
            {
                return false;
            }

            workplaceIndex = building.GetIndexOf(workplace);
            return workplaceIndex >= 0;
        }

        private void EnsureUi()
        {
            if (_queuedIconObject != null)
            {
                return;
            }

            _fiRaceIcon ??= typeof(BuildingWorkerSlot).GetField("raceIcon", BindingFlags.Instance | BindingFlags.NonPublic);
            var sourceImage = _fiRaceIcon?.GetValue(_slot) as Image;
            if (sourceImage == null)
            {
                return;
            }

            _queuedIconObject = Instantiate(sourceImage.gameObject, sourceImage.transform.parent, false);
            _queuedIconObject.name = "QueuedWorkerIcon";
            _queuedIconImage = _queuedIconObject.GetComponent<Image>();
            _queuedIconButton = _queuedIconObject.GetComponent<Button>();
            if (_queuedIconButton == null)
            {
                _queuedIconButton = _queuedIconObject.AddComponent<Button>();
            }

            _queuedIconButton.onClick.RemoveAllListeners();
            _queuedIconButton.onClick.AddListener(OpenQueueMenu);

            var rect = _queuedIconObject.GetComponent<RectTransform>();
            var width = rect.rect.width > 0f ? rect.rect.width : Mathf.Max(32f, rect.sizeDelta.x);
            rect.anchoredPosition += new Vector2(-(width + 8f), 0f);
            _queuedIconObject.SetActive(false);
        }

        private void OpenQueueMenu()
        {
            if (!Config.ConfigManager.EnableQueuedWorkerAssignments)
            {
                return;
            }

            if (!TryGetContext(out var building, out var workplace, out _))
            {
                return;
            }

            var menu = FindRacesMenu();
            if (menu == null)
            {
                return;
            }

            menu.SetUp(
                _queuedIconObject.GetComponent<RectTransform>(),
                building,
                race => WorkerAssignmentQueue.SetQueuedRace(building, workplace, race),
                () => WorkerAssignmentQueue.ClearQueuedRace(building, workplace)
            );
        }

        private static RacesMenu FindRacesMenu()
        {
            return Resources
                .FindObjectsOfTypeAll<RacesMenu>()
                .FirstOrDefault(menu => menu != null && menu.gameObject.scene.IsValid());
        }

        private static bool IsBuildingRemoved(ProductionBuilding building)
        {
            try
            {
                _piIsRemoved ??= typeof(Building).GetProperty("IsRemoved", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var reactiveProperty = _piIsRemoved?.GetValue(building, null);
                if (reactiveProperty == null)
                {
                    return false;
                }

                _piReactiveValue ??= reactiveProperty.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var value = _piReactiveValue?.GetValue(reactiveProperty, null);
                return value is bool removed && removed;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Hide()
        {
            if (_queuedIconObject != null)
            {
                _queuedIconObject.SetActive(false);
            }
        }
    }
}
