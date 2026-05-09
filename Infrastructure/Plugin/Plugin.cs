using BepInEx;
using HarmonyLib;
using StockAlert.Config;
using StockAlert.Game;
using StockAlert.Game.Discovery;
using StockAlert.Infrastructure.Bootstrap;
using StockAlert.UI.HUD;
using PanelUI = StockAlert.UI.Panels.UI;

namespace StockAlert.Infrastructure.Plugin
{
    [BepInPlugin("StockAlert", "Stock Alert", StockAlertInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

        private Harmony _harmony;
        private bool _gameReadyInitialized;

        private void Awake()
        {
            Instance = this;
            Log("StockAlert loaded");
            ConfigManager.Load();

            _harmony = new Harmony("StockAlert.Harmony");
            _harmony.PatchAll();
            Log("Harmony patches applied");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        public static void Log(string msg)
        {
            BepInEx.Logging.Logger.CreateLogSource("Stock Alert")
                .LogInfo(msg);
        }

        public void OnGameReady()
        {
            if (_gameReadyInitialized)
            {
                Log("OnGameReady() skipped - already initialized");
                return;
            }

            _gameReadyInitialized = true;
            Log("OnGameReady() fired - initializing mod");
            Discovery.Initialize();
            AutoProductionLimits.Initialize();
            HUD.Initialize();
            PanelUI.Initialize();
            PanelUI.Refresh();
            StockAlertRuntime.Initialize();
            AutoProductionLimits.ApplyCurrentTargets();
        }

        public void ResetGameReady()
        {
            if (!_gameReadyInitialized)
            {
                return;
            }

            _gameReadyInitialized = false;
            Log("Game-ready state reset");
        }
    }
}
