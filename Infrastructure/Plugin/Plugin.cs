using BepInEx;
using UnityEngine;

namespace StockAlert.Core.Models
{
    [BepInPlugin("StockAlert", "Stock Alert", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Log("StockAlert loaded");

            // Create bootstrapper here, after the plugin is initialized
            var go = new GameObject("StockAlertBootstrapper");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<StockAlertBootstrapper>();
        }

        public static void Log(string msg)
        {
            BepInEx.Logging.Logger.CreateLogSource("Stock Alert").LogInfo(msg);
        }

        public void OnGameReady()
        {
            Log("OnGameReady() fired — initializing mod");

            Discovery.Initialize();
            UI.Initialize();
            HUD.Initialize();
        }
    }
}
