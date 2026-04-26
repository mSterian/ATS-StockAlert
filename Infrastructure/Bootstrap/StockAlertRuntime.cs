using UnityEngine;
using StockAlert.Config;
using StockAlert.Game;
using StockAlert.Game.Discovery;
using PanelUI = StockAlert.UI.Panels.UI;
using SAPlugin = StockAlert.Infrastructure.Plugin.Plugin;

namespace StockAlert.Infrastructure.Bootstrap
{
    internal sealed class StockAlertRuntime : MonoBehaviour
    {
        private static StockAlertRuntime _instance;
        private float _nextRefreshTime;
        private float _nextThresholdSyncTime;
        private float _nextLimitDumpTime;

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

            if (Time.unscaledTime < _nextRefreshTime)
            {
                if (Time.unscaledTime < _nextThresholdSyncTime)
                {
                    return;
                }
            }

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + 0.5f;
                Discovery.UpdateStock();
            }

            if (Time.unscaledTime >= _nextThresholdSyncTime)
            {
                _nextThresholdSyncTime = Time.unscaledTime + 1f;
                ConfigManager.RefreshGoodsFromProductionLimits(Discovery.Goods);
                PanelUI.Refresh();
            }

            if (Time.unscaledTime >= _nextLimitDumpTime)
            {
                _nextLimitDumpTime = Time.unscaledTime + 5f;
                DumpActiveProductionLimits();
            }
        }

        private void DumpActiveProductionLimits()
        {
            var snapshot = GameAPI.GetProductionLimitsSnapshot();
            foreach (var pair in snapshot)
            {
                if (pair.Value > 0)
                {
                    SAPlugin.Log($"Live production limit: {pair.Key} => {pair.Value}");
                }
            }
        }
    }
}
