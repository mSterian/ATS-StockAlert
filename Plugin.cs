using BepInEx;
using UnityEngine;

namespace StockAlert
{
    [BepInPlugin("StockAlert", "Stock Alert", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

        static Plugin()
        {
            var go = new GameObject("StockAlertBootstrapper");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<StockAlertBootstrapper>();
        }

        private void Awake()
        {
            Instance = this;
            Log("StockAlert loaded");
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
