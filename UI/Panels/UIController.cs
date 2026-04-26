using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StockAlert.Config;
using StockAlert.Core.Models;
using StockAlert.Game.Discovery;
using StockAlert.Infrastructure.Plugin;

namespace StockAlert.UI.Panels
{
    public static class UI
    {
        private static GameObject _uiRoot;
        private static SettingsPanelBehaviour _panel;

        public static void Initialize()
        {
            if (_uiRoot != null)
            {
                return;
            }

            Plugin.Log("UI.Initialize()");

            _uiRoot = new GameObject("StockAlertUI");
            Object.DontDestroyOnLoad(_uiRoot);
            _panel = _uiRoot.AddComponent<SettingsPanelBehaviour>();
        }

        public static void Toggle()
        {
            if (_panel == null)
            {
                return;
            }

            _panel.Toggle();
            Plugin.Log("UI toggled: " + _panel.Visible);
        }

        private sealed class SettingsPanelBehaviour : MonoBehaviour
        {
            private readonly Dictionary<string, string> _thresholdInputs = new Dictionary<string, string>();
            private Rect _windowRect = new Rect(40f, 40f, 480f, 640f);
            private Vector2 _scrollPosition;

            public bool Visible { get; private set; }

            public void Toggle()
            {
                Visible = !Visible;
            }

            private void OnGUI()
            {
                if (!Visible)
                {
                    return;
                }

                _windowRect = GUILayout.Window(
                    GetInstanceID(),
                    _windowRect,
                    DrawWindow,
                    "Stock Alert Settings"
                );
            }

            private void DrawWindow(int windowId)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Toggle key: " + ConfigManager.ToggleSettingsKey);
                GUILayout.Label("Set a threshold. 0 disables alerts for that good.");
                GUILayout.Space(8f);

                if (Discovery.Goods.Count == 0)
                {
                    GUILayout.Label("No goods discovered yet.");
                }

                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
                foreach (var good in Discovery.Goods.OrderBy(g => g.DisplayName))
                {
                    DrawGoodRow(good);
                }
                GUILayout.EndScrollView();

                GUILayout.Space(8f);
                if (GUILayout.Button("Close"))
                {
                    Visible = false;
                }

                GUILayout.EndVertical();
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
            }

            private void DrawGoodRow(GoodInfo good)
            {
                if (!_thresholdInputs.TryGetValue(good.Id, out var currentInput))
                {
                    currentInput = good.Threshold.ToString();
                    _thresholdInputs[good.Id] = currentInput;
                }

                GUILayout.BeginHorizontal("box");

                GUILayout.Label(good.DisplayName, GUILayout.Width(220f));
                GUILayout.Label("Stock: " + good.CurrentAmount, GUILayout.Width(90f));
                GUILayout.Label("Threshold", GUILayout.Width(60f));

                var nextInput = GUILayout.TextField(currentInput, GUILayout.Width(70f));
                if (nextInput != currentInput)
                {
                    _thresholdInputs[good.Id] = nextInput;
                }

                if (GUILayout.Button("Save", GUILayout.Width(60f)))
                {
                    ApplyThreshold(good);
                }

                GUILayout.EndHorizontal();
            }

            private void ApplyThreshold(GoodInfo good)
            {
                if (!_thresholdInputs.TryGetValue(good.Id, out var text))
                {
                    text = good.Threshold.ToString();
                }

                if (!int.TryParse(text, out var threshold))
                {
                    threshold = good.Threshold;
                }

                good.Threshold = Mathf.Max(0, threshold);
                good.Enabled = good.Threshold > 0;
                _thresholdInputs[good.Id] = good.Threshold.ToString();
                ConfigManager.UpdateGoodConfig(good);
            }
        }
    }
}
