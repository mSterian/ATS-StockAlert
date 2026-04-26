using UnityEngine;
using StockAlert.Config;
using StockAlert.Game.Discovery;
using PanelUI = StockAlert.UI.Panels.UI;
using SAPlugin = StockAlert.Infrastructure.Plugin.Plugin;

namespace StockAlert.Infrastructure.Bootstrap
{
    internal sealed class StockAlertRuntime : MonoBehaviour
    {
        private static StockAlertRuntime _instance;
        private float _nextRefreshTime;

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
                return;
            }

            _nextRefreshTime = Time.unscaledTime + 0.5f;
            Discovery.UpdateStock();
        }
    }
}
