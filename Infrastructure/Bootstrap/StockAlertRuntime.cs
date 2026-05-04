using UnityEngine;
using StockAlert.Config;
using StockAlert.Game;
using StockAlert.Game.Discovery;
using Eremite.Model.State;
using PanelUI = StockAlert.UI.Panels.UI;
using SAPlugin = StockAlert.Infrastructure.Plugin.Plugin;
using StockAlert.UI.World;

namespace StockAlert.Infrastructure.Bootstrap
{
    internal sealed class StockAlertRuntime : MonoBehaviour
    {
        private static StockAlertRuntime _instance;
        private float _nextRefreshTime;
        private float _nextAutoAdjustTime;
        private float _nextBuilderStatusRefreshTime;
        private float _nextWorkerQueueRefreshTime;
        private bool _seasonEndingAlertShown;
        private GameDate _seasonEndingAlertDate;

        public static void Initialize()
        {
            if (_instance != null)
            {
                _instance.enabled = true;
                return;
            }

            var go = new GameObject("StockAlertRuntime");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<StockAlertRuntime>();
        }

        private void Awake()
        {
            SAPlugin.Log("StockAlertRuntime ready");
        }

        private void Update()
        {
            if (ConfigManager.ToggleSettingsKey.IsDown())
            {
                PanelUI.Toggle();
            }

            if (!GameAPI.IsGameActive())
            {
                BuilderStatusIndicators.Clear();
                IdleBuildersAlert.Clear();
                WorkerAssignmentQueue.ClearAll();
                _seasonEndingAlertShown = false;
                _seasonEndingAlertDate = default;
                return;
            }

            UpdateSeasonEndingTradeRoutesAlert();

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + 1f;
                Discovery.UpdateStock();
                BuildingAlertIndicators.Refresh();
            }

            if (Time.unscaledTime >= _nextBuilderStatusRefreshTime)
            {
                _nextBuilderStatusRefreshTime = Time.unscaledTime + 0.5f;
                BuilderStatusIndicators.Refresh();
                IdleBuildersAlert.Refresh();
            }

            if (Time.unscaledTime >= _nextWorkerQueueRefreshTime)
            {
                _nextWorkerQueueRefreshTime = Time.unscaledTime + 0.5f;
                if (ConfigManager.EnableQueuedWorkerAssignments)
                {
                    WorkerAssignmentQueue.Process();
                }
                else
                {
                    WorkerAssignmentQueue.ClearAll();
                    QueuedWorkerSlotCompanion.ClearAll();
                }
            }

            if (!ConfigManager.AutoAdjustProductionLimits && !ConfigManager.AutoAdjustPurgingFire)
            {
                return;
            }

            if (Time.unscaledTime >= _nextAutoAdjustTime)
            {
                _nextAutoAdjustTime = Time.unscaledTime + 2f;
                AutoProductionLimits.ApplyCurrentTargets();
            }
        }

        private void UpdateSeasonEndingTradeRoutesAlert()
        {
            if (!ConfigManager.SeasonEndingTradeRoutesAlert)
            {
                _seasonEndingAlertShown = false;
                _seasonEndingAlertDate = default;
                return;
            }

            var currentDate = GameAPI.GetCurrentGameDate();
            if (!currentDate.Equals(_seasonEndingAlertDate))
            {
                _seasonEndingAlertDate = currentDate;
                _seasonEndingAlertShown = false;
            }

            if (_seasonEndingAlertShown)
            {
                return;
            }

            var timeLeft = GameAPI.GetTimeTillNextSeasonChange();
            if (timeLeft <= 0f || timeLeft > 3f)
            {
                return;
            }

            GameAPI.PublishNews("Season ending! Check trade routes.");
            GameAPI.PauseGame();
            _seasonEndingAlertShown = true;
        }
    }
}
