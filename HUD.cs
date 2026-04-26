using UnityEngine;

namespace StockAlert
{
    public static class HUD
    {
        private static GameObject _hudRoot;

        public static void Initialize()
        {
            Plugin.Log("HUD.Initialize()");

            _hudRoot = new GameObject("StockAlertHUD");
            Object.DontDestroyOnLoad(_hudRoot);

            // Add HUD drawing logic later
        }
    }
}
