using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StockAlert
{
    [BepInPlugin("com.stockalert.ats", "Stock Alert", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        // ── Logging ─────────────────────────────────────────
        internal static ManualLogSource Log;

        // ── Config (saved automatically to BepInEx/config/) ─
        private ConfigEntry<string> _savedThresholds;
        private ConfigEntry<KeyCode> _toggleKey;

        // ── Runtime goods state ──────────────────────────────
        private readonly List<GoodInfo> _allGoods = new List<GoodInfo>();
        private readonly List<GoodInfo> _alertGoods = new List<GoodInfo>();

        // ── Thresholds dictionary: internal-name → threshold amount
        private readonly Dictionary<string, int> _thresholds = new Dictionary<string, int>();
        // Text-field edit buffers so partial typing doesn't break things
        private readonly Dictionary<string, string> _inputBuffer = new Dictionary<string, string>();

        // ── UI state ─────────────────────────────────────────
        private bool _showSettings = false;
        // Avoid using Screen.* or other Unity APIs in field initializers — initialize in Awake instead
        private Rect _alertRect = new Rect(0, 80, 260, 30);
        private Rect _settingsRect = new Rect(0, 0, 460, 520);
        private Vector2 _scrollPos = Vector2.zero;
        private bool _centreOnce = true;   // centre the settings window on first open

        // Polled once per second
        private float _nextCheck = 0f;

        // Custom label styles initialised lazily inside OnGUI
        private GUIStyle _styleRed;
        private GUIStyle _styleNormal;
        private bool _stylesReady = false;

        // ────────────────────────────────────────────────────
        //  Represents one good and its current state
        // ────────────────────────────────────────────────────
        private struct GoodInfo
        {
            public string Id;           // internal name used as dictionary key
            public string DisplayName;  // human-readable name shown in the UI
            public int Amount;          // current stock
            public int Threshold;       // alert threshold (0 = not monitored)
        }

        // ════════════════════════════════════════════════════
        //  Awake — runs once when the mod is loaded
        // ════════════════════════════════════════════════════
        private void Awake()
        {
            Log = Logger;

            // Assembly resolve probe to capture any loader failures at runtime (logs if requested)
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                try { Log.LogWarning($"[StockAlert] AssemblyResolve requested: {args.Name}"); } catch { }
                return null;
            };

            try
            {
                Log.LogInfo("[StockAlert] Awake start");

                // Initialize UI rects that depend on Screen size here (safe at runtime)
                try
                {
                    _alertRect.x = Mathf.Max(0, Screen.width - 280);
                    _alertRect.y = 80;
                    _alertRect.width = 260;
                    _alertRect.height = 30;

                    // centre settings rect on first open
                    _settingsRect.x = (Screen.width - _settingsRect.width) / 2f;
                    _settingsRect.y = (Screen.height - _settingsRect.height) / 2f;
                }
                catch (Exception exRect)
                {
                    // If Screen isn't available for some reason, keep defaults and log
                    Log.LogWarning($"[StockAlert] Could not initialize UI rects from Screen: {exRect.Message}");
                }

                // Register config entries
                _savedThresholds = Config.Bind(
                    "Data",
                    "Thresholds",
                    "",
                    "Threshold data — managed in-game via the settings panel (F8). Format: GoodId:value,GoodId:value"
                );

                _toggleKey = Config.Bind(
                    "General",
                    "ToggleSettingsKey",
                    KeyCode.F8,
                    "Key to open/close the Stock Alert settings panel."
                );

                LoadThresholds();
                Log.LogInfo($"[StockAlert] Stock Alert loaded. Press {_toggleKey.Value} in-game to open settings.");

                // Diagnostics called once at startup (safe, they use reflection only)
                LogConfigDiagnostics();
                LogAssemblyDiagnostics();
            }
            catch (ReflectionTypeLoadException rtle)
            {
                Log.LogError("[StockAlert] ReflectionTypeLoadException in Awake: " + rtle.Message);
                foreach (var ex in rtle.LoaderExceptions ?? new Exception[0])
                    Log.LogError("[StockAlert] LoaderException: " + (ex?.ToString() ?? "<null>"));
                throw;
            }
            catch (Exception ex)
            {
                Log.LogError("[StockAlert] Exception in Awake: " + ex.ToString());
                throw;
            }
        }

        // ════════════════════════════════════════════════════
        //  Update — runs every frame
        // ════════════════════════════════════════════════════
        private void Update()
        {
            // Toggle settings panel
            try
            {
                if (IsKeyPressed(_toggleKey.Value))
                {
                    _showSettings = !_showSettings;

                    // Centre the window the first time it's opened (ensure we have current Screen size)
                    if (_showSettings && _centreOnce)
                    {
                        try
                        {
                            _settingsRect.x = (Screen.width - _settingsRect.width) / 2f;
                            _settingsRect.y = (Screen.height - _settingsRect.height) / 2f;
                        }
                        catch { }
                        _centreOnce = false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogDebug($"[StockAlert] Input check failed: {e.Message}");
            }

            // Refresh goods list once per second (unscaled so pausing doesn't matter)
            if (Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 1f;
            RefreshGoods();
        }

        // ════════════════════════════════════════════════════
        //  RefreshGoods — reads current stock from the game
        // ════════════════════════════════════════════════════
        private void RefreshGoods()
        {
            _allGoods.Clear();
            _alertGoods.Clear();

            if (!IsInGame()) return;

            try
            {
                Type gameMbType = FindTypeByFullName("Eremite.GameMB") ?? FindTypeByName("GameMB");
                if (gameMbType == null)
                {
                    Log.LogWarning("[StockAlert] Could not find GameMB type in loaded assemblies.");
                    return;
                }

                object storageService = GetStaticMemberValue(gameMbType, "StorageService");
                object settingsObj = GetStaticMemberValue(gameMbType, "Settings");

                if (settingsObj == null)
                {
                    object instance = GetStaticMemberValue(gameMbType, "Instance");
                    if (instance != null)
                        settingsObj = GetInstanceMemberValue(instance, "Settings");
                }

                if (settingsObj == null)
                {
                    Log.LogWarning("[StockAlert] Could not find Settings object on GameMB.");
                    return;
                }

                IEnumerable goodsEnumerable = GetEnumerableMember(settingsObj, new[] { "Goods", "goods", "AllGoods" });
                if (goodsEnumerable == null)
                {
                    Log.LogWarning("[StockAlert] Could not find a goods list on Settings object.");
                    return;
                }

                foreach (var good in goodsEnumerable)
                {
                    if (good == null) continue;

                    string id = GetStringMember(good, new[] { "name", "Id", "id", "internalName" }) ?? "";
                    string display = GetDisplayName(good) ?? id;
                    int amount = GetStockForGood(storageService, good);

                    bool isMonitored = _thresholds.TryGetValue(id, out int threshold) && threshold > 0;

                    if (amount == 0 && !isMonitored) continue;

                    var info = new GoodInfo
                    {
                        Id = id,
                        DisplayName = display,
                        Amount = amount,
                        Threshold = isMonitored ? threshold : 0
                    };

                    _allGoods.Add(info);

                    if (isMonitored && amount < threshold)
                        _alertGoods.Add(info);
                }

                _allGoods.Sort((a, b) =>
                {
                    int monA = a.Threshold > 0 ? 0 : 1;
                    int monB = b.Threshold > 0 ? 0 : 1;
                    return monA != monB ? monA.CompareTo(monB) : string.Compare(a.DisplayName, b.DisplayName);
                });
            }
            catch (Exception e)
            {
                Log.LogError($"[StockAlert] Error reading goods: {e.Message}");
                _nextCheck = Time.unscaledTime + 5f;
            }
        }

        // ════════════════════════════════════════════════════
        //  IsInGame — safe check so we don't run outside a settlement
        // ════════════════════════════════════════════════════
        private bool IsInGame()
        {
            try
            {
                Type gameMbType = FindTypeByFullName("Eremite.GameMB") ?? FindTypeByName("GameMB");
                if (gameMbType == null) return false;

                object gameStateService = GetStaticMemberValue(gameMbType, "GameStateService");
                if (gameStateService == null)
                {
                    object instance = GetStaticMemberValue(gameMbType, "Instance");
                    if (instance != null)
                        gameStateService = GetInstanceMemberValue(instance, "GameStateService");
                }

                if (gameStateService == null) return false;

                MethodInfo isActive = gameStateService.GetType().GetMethod("IsGameActive", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (isActive != null)
                {
                    object res = isActive.Invoke(gameStateService, null);
                    if (res is bool b) return b;
                }

                return GetStaticMemberValue(gameMbType, "StorageService") != null;
            }
            catch { return false; }
        }

        // ════════════════════════════════════════════════════
        //  OnGUI — Unity's immediate-mode GUI, called every frame
        // ════════════════════════════════════════════════════
        private void OnGUI()
        {
            if (!IsInGame()) return;
            InitStyles();

            if (_alertGoods.Count > 0)
            {
                _alertRect.height = 28 + _alertGoods.Count * 22f;
                _alertRect = GUI.Window(9001, _alertRect, DrawAlertWindow, "⚠  Stock Alert");
            }

            if (_showSettings)
            {
                _settingsRect = GUI.Window(9002, _settingsRect, DrawSettingsWindow,
                    $"Stock Alert Settings  |  drag to move  |  {_toggleKey.Value} to close");
            }
        }

        private void DrawAlertWindow(int id)
        {
            foreach (var good in _alertGoods)
                GUILayout.Label($"  {good.DisplayName}:  {good.Amount} / {good.Threshold}", _styleRed);

            GUI.DragWindow();
        }

        private void DrawSettingsWindow(int id)
        {
            GUILayout.Space(4);
            GUILayout.Label("Type a number in the box and press Set. Use 0 to stop monitoring a good.",
                _styleNormal);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Good", _styleNormal, GUILayout.Width(190));
            GUILayout.Label("In stock", _styleNormal, GUILayout.Width(70));
            GUILayout.Label("Alert at", _styleNormal, GUILayout.Width(70));
            GUILayout.Label("", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUILayout.Space(2);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

            foreach (var good in _allGoods)
            {
                bool isCritical = good.Threshold > 0 && good.Amount < good.Threshold;
                var labelStyle = isCritical ? _styleRed : _styleNormal;

                GUILayout.BeginHorizontal();

                GUILayout.Label(good.DisplayName, labelStyle, GUILayout.Width(190));
                GUILayout.Label(good.Amount.ToString(), labelStyle, GUILayout.Width(70));

                if (!_inputBuffer.ContainsKey(good.Id))
                    _inputBuffer[good.Id] = good.Threshold.ToString();

                _inputBuffer[good.Id] = GUILayout.TextField(
                    _inputBuffer[good.Id], maxLength: 6, GUILayout.Width(60));

                if (GUILayout.Button("Set", GUILayout.Width(45)))
                {
                    if (int.TryParse(_inputBuffer[good.Id], out int newValue) && newValue >= 0)
                    {
                        if (newValue == 0)
                            _thresholds.Remove(good.Id);
                        else
                            _thresholds[good.Id] = newValue;

                        SaveThresholds();
                        Log.LogInfo($"[StockAlert] {good.DisplayName} threshold → {newValue}");
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(4);
            if (GUILayout.Button("Close"))
                _showSettings = false;

            GUI.DragWindow();
        }

        private void InitStyles()
        {
            if (_stylesReady) return;

            _styleNormal = new GUIStyle(GUI.skin.label);

            _styleRed = new GUIStyle(GUI.skin.label);
            _styleRed.normal.textColor = new Color(1f, 0.35f, 0.35f);
            _styleRed.fontStyle = FontStyle.Bold;

            _stylesReady = true;
        }

        private void LoadThresholds()
        {
            _thresholds.Clear();
            string raw = _savedThresholds.Value;
            if (string.IsNullOrWhiteSpace(raw)) return;

            foreach (string pair in raw.Split(','))
            {
                string[] parts = pair.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int val) && val > 0)
                    _thresholds[parts[0]] = val;
            }

            Log.LogInfo($"[StockAlert] Loaded {_thresholds.Count} saved threshold(s).");
        }

        private void SaveThresholds()
        {
            _savedThresholds.Value = string.Join(",",
                _thresholds
                    .Where(kv => kv.Value > 0)
                    .Select(kv => $"{kv.Key}:{kv.Value}"));

            Config.Save();
        }

        // ─────────────────────────────────────────────────────
        // Reflection helpers
        // ─────────────────────────────────────────────────────
        private static Type FindTypeByFullName(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(fullName, false);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private static Type FindTypeByName(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = null;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                    if (t.Name == name) return t;
            }
            return null;
        }

        private static object GetStaticMemberValue(Type type, string memberName)
        {
            if (type == null) return null;
            var p = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (p != null) return p.GetValue(null);
            var f = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (f != null) return f.GetValue(null);
            return null;
        }

        private static object GetInstanceMemberValue(object instance, string memberName)
        {
            if (instance == null) return null;
            var t = instance.GetType();
            var p = t.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return p.GetValue(instance);
            var f = t.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return f.GetValue(instance);
            return null;
        }

        private static IEnumerable GetEnumerableMember(object obj, string[] candidateNames)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var name in candidateNames)
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null)
                {
                    var val = p.GetValue(obj);
                    if (val is IEnumerable e) return e;
                }
                var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null)
                {
                    var val = f.GetValue(obj);
                    if (val is IEnumerable e) return e;
                }
            }
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType))
                {
                    var val = p.GetValue(obj);
                    if (val is IEnumerable e) return e;
                }
            }
            return null;
        }

        private static string GetStringMember(object obj, string[] candidateNames)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var name in candidateNames)
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null)
                {
                    var val = p.GetValue(obj);
                    if (val != null) return val.ToString();
                }
                var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null)
                {
                    var val = f.GetValue(obj);
                    if (val != null) return val.ToString();
                }
            }
            return null;
        }

        private static string GetDisplayName(object good)
        {
            if (good == null) return null;
            var t = good.GetType();

            var disp = GetInstanceMemberValue(good, "displayName") ?? GetInstanceMemberValue(good, "DisplayName");
            if (disp != null)
            {
                var txt = GetInstanceMemberValue(disp, "Text") ?? GetInstanceMemberValue(disp, "text") ?? GetInstanceMemberValue(disp, "Value");
                if (txt != null) return txt.ToString();
            }

            var direct = GetStringMember(good, new[] { "displayName", "DisplayName", "label", "title", "name" });
            if (!string.IsNullOrEmpty(direct)) return direct;

            return null;
        }

        private static int GetStockForGood(object storageService, object good)
        {
            if (storageService == null || good == null) return 0;
            var sType = storageService.GetType();

            string[] candidates = new[] { "GetCurrentStock", "GetStock", "GetAmount", "GetAmountForGood", "GetGoodAmount" };
            foreach (var name in candidates)
            {
                var m = sType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (m != null)
                {
                    try
                    {
                        object res = null;
                        var parameters = m.GetParameters();
                        if (parameters.Length == 1)
                            res = m.Invoke(storageService, new object[] { good });
                        else if (parameters.Length == 0)
                            res = m.Invoke(storageService, null);
                        else
                        {
                            var id = GetStringMember(good, new[] { "name", "Id", "id" });
                            if (id != null)
                            {
                                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                                    res = m.Invoke(storageService, new object[] { id });
                            }
                        }

                        if (res is int i) return i;
                        if (res is long l) return (int)l;
                        if (res != null && int.TryParse(res.ToString(), out int parsed)) return parsed;
                    }
                    catch { }
                }
            }

            foreach (var p in sType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType))
                {
                    try
                    {
                        var val = p.GetValue(storageService);
                        if (val is IEnumerable e)
                        {
                            foreach (var item in e)
                            {
                                var id = GetStringMember(item, new[] { "name", "Id", "id" });
                                var goodId = GetStringMember(good, new[] { "name", "Id", "id" });
                                if (!string.IsNullOrEmpty(id) && id == goodId)
                                {
                                    var amt = GetStringMember(item, new[] { "amount", "Amount", "count", "Count" });
                                    if (int.TryParse(amt, out int parsed)) return parsed;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            return 0;
        }

        // ─────────────────────────────────────────────────────
        // Diagnostics
        // ─────────────────────────────────────────────────────
        private void LogConfigDiagnostics()
        {
            try
            {
                Log.LogInfo("[StockAlert] Config diagnostics start");

                if (Config == null)
                {
                    Log.LogWarning("[StockAlert] Config is null");
                    return;
                }

                var cfgType = Config.GetType();
                var entriesProp = cfgType.GetProperty("Entries", BindingFlags.Public | BindingFlags.Instance);
                if (entriesProp != null)
                {
                    var entries = entriesProp.GetValue(Config) as System.Collections.IEnumerable;
                    if (entries == null)
                    {
                        Log.LogWarning("[StockAlert] Config.Entries is null or not enumerable");
                    }
                    else
                    {
                        foreach (var e in entries)
                        {
                            try
                            {
                                var et = e.GetType();
                                var defProp = et.GetProperty("Definition");
                                var valProp = et.GetProperty("BoxedValue") ?? et.GetProperty("Value");
                                var descProp = et.GetProperty("Description");

                                string def = defProp?.GetValue(e)?.ToString() ?? "<no Definition>";
                                string val = valProp?.GetValue(e)?.ToString() ?? "<no Value>";
                                string desc = descProp?.GetValue(e)?.ToString() ?? "<no Description>";

                                Log.LogInfo($"[StockAlert] ConfigEntry: Definition={def}; Value={val}; Description={desc}");
                            }
                            catch (Exception exEntry)
                            {
                                Log.LogError($"[StockAlert] Error reading config entry: {exEntry.Message}");
                            }
                        }
                    }
                }
                else
                {
                    var cfgFileField = cfgType.GetField("ConfigFile", BindingFlags.NonPublic | BindingFlags.Instance)
                                      ?? cfgType.GetField("configFile", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (cfgFileField != null)
                    {
                        var cfgFile = cfgFileField.GetValue(Config);
                        if (cfgFile != null)
                        {
                            var cfType = cfgFile.GetType();
                            var keysProp = cfType.GetProperty("Keys") ?? cfType.GetProperty("Configs");
                            if (keysProp != null)
                            {
                                var keys = keysProp.GetValue(cfgFile) as System.Collections.IEnumerable;
                                if (keys != null)
                                {
                                    foreach (var k in keys)
                                        Log.LogInfo($"[StockAlert] ConfigFile entry: {k}");
                                }
                            }
                        }
                    }
                    Log.LogWarning("[StockAlert] Could not find Config.Entries property; attempted fallback.");
                }

                Log.LogInfo("[StockAlert] Config diagnostics end");
            }
            catch (Exception e)
            {
                Log.LogError($"[StockAlert] LogConfigDiagnostics failed: {e.Message}");
            }
        }

        private void LogAssemblyDiagnostics()
        {
            try
            {
                Log.LogInfo("[StockAlert] Assembly diagnostics start");
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string name = asm.GetName().Name;
                    if (name.IndexOf("API", StringComparison.OrdinalIgnoreCase) >= 0
                     || name.IndexOf("Against", StringComparison.OrdinalIgnoreCase) >= 0
                     || name.IndexOf("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) >= 0
                     || name.IndexOf("Eremite", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log.LogInfo($"[StockAlert] Assembly: {name}");
                        Type[] types = null;
                        try { types = asm.GetTypes(); } catch { continue; }
                        foreach (var t in types)
                        {
                            if (t.Name.IndexOf("Storage", StringComparison.OrdinalIgnoreCase) >= 0
                             || t.Name.IndexOf("GameMB", StringComparison.OrdinalIgnoreCase) >= 0
                             || t.Name.IndexOf("Good", StringComparison.OrdinalIgnoreCase) >= 0
                             || t.Name.IndexOf("Settings", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Log.LogInfo($"[StockAlert] Type: {t.FullName}");
                                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                                {
                                    if (m.Name.IndexOf("Get", StringComparison.OrdinalIgnoreCase) >= 0
                                     || m.Name.IndexOf("Stock", StringComparison.OrdinalIgnoreCase) >= 0)
                                        Log.LogInfo($"[StockAlert]   Method: {m.Name} ({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
                                }
                                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                                {
                                    if (p.Name.IndexOf("Goods", StringComparison.OrdinalIgnoreCase) >= 0
                                     || p.Name.IndexOf("Settings", StringComparison.OrdinalIgnoreCase) >= 0)
                                        Log.LogInfo($"[StockAlert]   Property: {p.Name} ({p.PropertyType.Name})");
                                }
                            }
                        }
                    }
                }
                Log.LogInfo("[StockAlert] Assembly diagnostics end");
            }
            catch (Exception e)
            {
                Log.LogError($"[StockAlert] LogAssemblyDiagnostics failed: {e.Message}");
            }
        }
    }
}
