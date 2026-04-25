using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;

namespace StockAlert
{
    internal static class UI
    {
        private static Vector2 _savedThresholdsScroll = Vector2.zero;
        private static string _savedThresholdsEdit = null;
        private static string _filterText = string.Empty;
        private static Rect _settingsRect = new Rect(100, 100, 760, 460);
        private static bool _showSettings;
        private static bool _centreOnce = true;
        private static GUIStyle _boxStyle;
        private static GUIStyle _labelStyle;

        // Expose ordering helper to Discovery
        public static int GetOrderIndex(string id)
        {
            if (string.IsNullOrEmpty(id)) return int.MaxValue;
            var idx = ModelsOrder.CustomOrder.IndexOf(id);
            return idx >= 0 ? idx : int.MaxValue;
        }

        public static void ToggleSettings()
        {
            _showSettings = !_showSettings;
            if (_showSettings && _centreOnce)
            {
                _settingsRect.x = (Screen.width - _settingsRect.width) / 2f;
                _settingsRect.y = (Screen.height - _settingsRect.height) / 2f;
                _centreOnce = false;
            }
        }

        public static void OnGUI(ManualLogSource logger)
        {
            if (!IsInGame() && !_showSettings) return;

            InitStyles();

            if (_showSettings)
            {
                _settingsRect = GUI.Window(123456, _settingsRect, id => SettingsWindow(id, logger), "Stock Alert Settings");
            }

            if (IsInGame())
            {
                DrawAlertStrip();
            }
        }

        private static void InitStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 10, 10) };
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
            }
        }

        private static void SettingsWindow(int id, ManualLogSource logger)
        {
            if (_savedThresholdsEdit == null)
            {
                _savedThresholdsEdit = ConfigManager.SavedThresholds.Value ?? string.Empty;
            }

            GUILayout.BeginVertical();
            GUILayout.Label("Saved thresholds (ID:threshold). Edit Name to override display. Use search to filter:", _labelStyle);

            // Filter and controls
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(40));
            _filterText = GUILayout.TextField(_filterText, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Clear", GUILayout.Width(60))) _filterText = string.Empty;
            if (GUILayout.Button("Add custom ID", GUILayout.Width(120)))
            {
                var newId = "custom_" + Guid.NewGuid().ToString("N").Substring(0, 6);
                Models.Thresholds.Insert(0, new ThresholdEntry { Id = newId, Name = newId, Threshold = 0f, ThresholdText = "0", NameEdit = newId });
            }
            GUILayout.EndHorizontal();

            // Debug / dump buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Debug: Dump first discovered item", GUILayout.Width(300)))
            {
                var sample = Discovery.FindFirstEnumerableItem(logger, ConfigManager.VerboseAssemblyDiagnostics.Value);
                if (sample != null)
                {
                    Discovery.DumpItemForDebug(logger, sample, "SampleGood");
                    var resolved = Discovery.TryResolveNameFromItem(sample);
                    logger.LogInfo($"[StockAlert] SampleGood resolved name: {resolved ?? "<none>"}");
                }
                else
                {
                    logger.LogInfo("[StockAlert] Debug: no sample item found by heuristic.");
                }
            }

            if (GUILayout.Button("Dump thresholds to CSV", GUILayout.Width(220))) Dumps.DumpThresholdsToCsv(logger, Models.Thresholds);
            if (GUILayout.Button("Dump detailed goods to file", GUILayout.Width(220))) Dumps.DumpDetailedGoodsToFile(logger);
            if (GUILayout.Button("Dump GoodModel instances to file", GUILayout.Width(260))) Dumps.DumpGoodModelInstancesToFile(logger);

            GUILayout.EndHorizontal();

            // Column headers
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name (editable)", GUILayout.Width(360));
            GUILayout.Label("ID (canonical)", GUILayout.Width(200));
            GUILayout.Label("Threshold", GUILayout.Width(120));
            GUILayout.EndHorizontal();

            // Scrollable list
            _savedThresholdsScroll = GUILayout.BeginScrollView(_savedThresholdsScroll, GUILayout.Height(320));

            var filtered = string.IsNullOrEmpty(_filterText)
                ? Models.Thresholds
                : Models.Thresholds.Where(t => (t.Id ?? "").IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0
                                      || (t.Name ?? "").IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            foreach (var entry in filtered.ToList())
            {
                GUILayout.BeginHorizontal();

                entry.NameEdit = GUILayout.TextField(entry.NameEdit ?? entry.Name ?? entry.Id, GUILayout.Width(360));
                GUILayout.Label(Utils.CleanIdForDisplay(entry.Id), GUILayout.Width(200));
                entry.ThresholdText = GUILayout.TextField(entry.ThresholdText ?? entry.Threshold.ToString(CultureInfo.InvariantCulture), GUILayout.Width(120));

                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    Models.Thresholds.Remove(entry);
                    if (Models.NameOverrideMap.ContainsKey(entry.Id)) Models.NameOverrideMap.Remove(entry.Id);
                    break;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            // Save / Close
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
            {
                foreach (var e in Models.Thresholds)
                {
                    if (float.TryParse(e.ThresholdText, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                        e.Threshold = v;
                    else
                        e.Threshold = 0f;

                    var newName = string.IsNullOrWhiteSpace(e.NameEdit) ? e.Id : e.NameEdit.Trim();
                    if (!string.Equals(newName, e.Name, StringComparison.Ordinal))
                    {
                        Models.NameOverrideMap[e.Id] = newName;
                        e.Name = newName;
                    }
                }

                Models.SaveThresholdsToConfig();
                Models.SaveNameOverridesToConfig();
                logger.LogInfo("[StockAlert] Saved thresholds and name overrides.");
            }

            if (GUILayout.Button("Close")) _showSettings = false;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private static void DrawAlertStrip()
        {
            var rect = new Rect(10, 10, 300, 30);
            GUI.Box(rect, "Stock Alert Active", _boxStyle);
        }

        private static bool IsInGame()
        {
            return Application.isPlaying && !string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    // small ordering holder so UI and Discovery share the same list
    internal static class ModelsOrder
    {
        public static readonly List<string> CustomOrder = new List<string>
        {
            "Mushrooms","Roots","Vegetables","Fish","Meat","Eggs","Insects","Berries",
            "Jerky","Porridge","Paste","Skewers","Biscuits","Pie","PickledGoods",
            "Planks","Fabric","Bricks","Pipe","Parts","HearthParts","Fertilizer",
            "Boots","Coats","Ale","TrainingGear","Incense","Scrolls","TutorialScrolls","Wine","Tea",
            "Clay","Stone","PlantFibre","Reeds","Algae","Scales","Leather","Grain","Herbs","Resin","Salt","CopperOre","Sparkdew",
            "Flour","Pottery","Barrels","Waterskin","Pigment","CopperBar","CrystalizedDew",
            "Amber","PackofProvisions","PackofBuildingMaterials","PackofCrops","PackofLuxuryGoods","PackofTradeGoods","AncientTablet","Fuel Core","_MetaArtifacts","_MetaFoodStockpiles","_MetaMachinery",
            "Wood","Oil","Sea Marrow","Coal","SimpleTools","BlightFuel","Fuel Rod"
        };
    }
}
