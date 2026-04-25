using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StockAlert
{
    [BepInPlugin("com.marius.ats.stockalert", "ATS StockAlert", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private static Plugin _instance;
        private HUD _hud;
        private UI _ui;
        private bool _initialized = false;

        public static void Log(string msg) => _instance.Logger.LogInfo(msg);

        private void Awake()
        {
            _instance = this;
            Logger.LogInfo("[StockAlert] Plugin Awake()");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log($"[StockAlert] Scene loaded: {scene.name}");

            if (_initialized) return;

            if (scene.name == "Game" || scene.name.Contains("Game"))
            {
                Log("[StockAlert] Initializing after game scene load…");

                _initialized = true;

                Discovery.Initialize();
                CreateHUD();
                CreateUI();
            }
        }

        private void CreateHUD()
        {
            Log("[StockAlert] Creating HUD object…");

            var go = new GameObject("StockAlertHUD");
            _hud = go.AddComponent<HUD>();
            DontDestroyOnLoad(go);

            Log("[StockAlert] HUD created.");
        }

        private void CreateUI()
        {
            Log("[StockAlert] Creating UI object…");

            var go = new GameObject("StockAlertConfigUI");
            _ui = go.AddComponent<UI>();
            _ui.enabled = false;
            DontDestroyOnLoad(go);

            Log("[StockAlert] UI object created.");
        }

        private void Update()
        {
            if (!_initialized)
                return;

            var input = GameAPI.Instance.GetInput();
            if (input != null && input.GetKeyDown(KeyCode.F8))
            {
                _ui.enabled = !_ui.enabled;
                Log($"[StockAlert] Config UI → {(_ui.enabled ? "ON" : "OFF")}");
            }

            if (_hud == null) CreateHUD();
            if (_ui == null) CreateUI();
        }
    }
}
