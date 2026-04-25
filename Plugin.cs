using BepInEx;
using UnityEngine;

namespace StockAlert
{
    [BepInPlugin("com.yourname.stockalert", "Stock Alert", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            ConfigManager.Init(Config, Logger, out var toggleKey, out var savedThresholds, out var nameOverrides, out var verboseAssemblyDiagnostics);
            // store config entries in static holders for other modules
            ConfigManager.ToggleKey = toggleKey;
            ConfigManager.SavedThresholds = savedThresholds;
            ConfigManager.NameOverrides = nameOverrides;
            ConfigManager.VerboseAssemblyDiagnostics = verboseAssemblyDiagnostics;

            Logger.LogInfo("[StockAlert] Awake");

            Diagnostics.LogAssemblies(Logger, ConfigManager.VerboseAssemblyDiagnostics.Value);

            // Load persisted data
            Models.LoadNameOverridesFromConfig();
            Models.LoadThresholdsFromConfig();

            Logger.LogInfo($"[StockAlert] Loaded {Models.Thresholds.Count} threshold entries and {Models.NameOverrideMap.Count} name overrides.");
        }

        private void Update()
        {
            try
            {
                if (InputHelpers.IsKeyPressed(ConfigManager.ToggleKey.Value))
                {
                    UI.ToggleSettings();
                }

                // optional hotkey for targeted dump (F9)
                if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
                {
                    Dumps.DumpGoodModelInstancesToFile(Logger);
                    Logger.LogInfo("[StockAlert] Triggered DumpGoodModelInstancesToFile via F9.");
                }
            }
            catch (System.Exception e)
            {
                Logger.LogDebug($"[StockAlert] Input check failed: {e.Message}");
            }
        }

        private void OnGUI()
        {
            UI.OnGUI(Logger);
        }
    }
}
