using UnityEngine;
using StockAlert.Config;
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
            private Rect _windowRect = new Rect(40f, 40f, 340f, 180f);

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
                GUILayout.Label("Use these settings to hide the HUD or make it draggable.");
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
            }
        }
    }
}
