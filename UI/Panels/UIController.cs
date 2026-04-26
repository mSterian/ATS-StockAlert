using UnityEngine;

namespace StockAlert.Core.Models
{
    public static class UI
    {
        private static GameObject _uiRoot;
        private static bool _visible = true;

        public static void Initialize()
        {
            Plugin.Log("UI.Initialize()");

            _uiRoot = new GameObject("StockAlertUI");
            Object.DontDestroyOnLoad(_uiRoot);

            // Add your UI drawing component here later
        }

        public static void Toggle()
        {
            _visible = !_visible;
            if (_uiRoot != null)
                _uiRoot.SetActive(_visible);

            Plugin.Log("UI toggled: " + _visible);
        }
    }
}
