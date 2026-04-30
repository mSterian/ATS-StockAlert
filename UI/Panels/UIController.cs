using UnityEngine;
using StockAlert.Config;
using StockAlert.Game;
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

        public static void Refresh()
        {
            _panel?.RefreshInputs();
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
            private Rect _windowRect = new Rect(40f, 40f, 380f, 250f);
            private string _multiplierInput = "2.0";

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
                GUILayout.Label("Use these settings to control the HUD and optional production automation.");
                GUILayout.Space(8f);

                var showHud = GUILayout.Toggle(ConfigManager.ShowHud, "Show HUD");
                if (showHud != ConfigManager.ShowHud)
                {
                    ConfigManager.ShowHud = showHud;
                }

                var movableHud = GUILayout.Toggle(ConfigManager.MovableHud, "Movable HUD");
                if (movableHud != ConfigManager.MovableHud)
                {
                    ConfigManager.MovableHud = movableHud;
                }

                GUILayout.Space(10f);

                var autoAdjust = GUILayout.Toggle(
                    ConfigManager.AutoAdjustProductionLimits,
                    "Auto-adjust production limits from consumers"
                );
                if (autoAdjust != ConfigManager.AutoAdjustProductionLimits)
                {
                    ConfigManager.AutoAdjustProductionLimits = autoAdjust;
                    if (autoAdjust)
                    {
                        AutoProductionLimits.ApplyCurrentTargets();
                    }
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Multiplier", GUILayout.Width(70f));
                var nextInput = GUILayout.TextField(_multiplierInput ?? string.Empty, GUILayout.Width(60f));
                if (nextInput != _multiplierInput)
                {
                    _multiplierInput = nextInput;
                    if (TryParseMultiplier(_multiplierInput, out var multiplier))
                    {
                        ConfigManager.AutoAdjustMultiplier = multiplier;
                        _multiplierInput = ConfigManager.AutoAdjustMultiplier.ToString("0.0");
                        if (ConfigManager.AutoAdjustProductionLimits)
                        {
                            AutoProductionLimits.ApplyCurrentTargets();
                        }
                    }
                }
                GUILayout.Label("(1.0 - 9.0)");
                GUILayout.EndHorizontal();

                GUILayout.Space(8f);
                if (GUILayout.Button("Close"))
                {
                    Visible = false;
                }

                GUILayout.EndVertical();
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
            }

            public void RefreshInputs()
            {
                _multiplierInput = ConfigManager.AutoAdjustMultiplier.ToString("0.0");
            }

            private static bool TryParseMultiplier(string raw, out float value)
            {
                value = 0f;
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return false;
                }

                var normalized = raw.Trim().Replace(',', '.');
                if (!float.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                {
                    return false;
                }

                value = Mathf.Clamp(Mathf.Round(value * 10f) / 10f, 1f, 9f);
                return true;
            }
        }
    }
}
