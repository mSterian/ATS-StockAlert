using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace StockAlert
{
    internal static class Discovery
    {
        // Try to populate thresholds from game runtime objects
        public static void TryPopulateGoodsFromGame(List<ThresholdEntry> thresholds, Dictionary<string, string> overrides, ManualLogSource logger)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a =>
                    {
                        var n = a.GetName().Name ?? "";
                        if (n.StartsWith("Assembly-CSharp", StringComparison.OrdinalIgnoreCase)) return true;
                        if (n.StartsWith("Assembly-CSharp-firstpass", StringComparison.OrdinalIgnoreCase)) return true;
                        if (n.IndexOf("AgainstTheStorm", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                        if (n.IndexOf("Eremite", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                        if (n.StartsWith("System", StringComparison.OrdinalIgnoreCase)) return false;
                        if (n.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)) return false;
                        if (n.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase)) return false;
                        if (n.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)) return false;
                        if (n.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) return false;
                        return true;
                    })
                    .ToArray();

                if (assemblies.Length == 0)
                    assemblies = AppDomain.CurrentDomain.GetAssemblies().ToArray();

                // 1) static collections and instance managers
                foreach (var asm in assemblies)
                {
                    Type[] types;
                    try { types = asm.GetTypes(); } catch { continue; }
                    foreach (var t in types)
                    {
                        // static props
                        PropertyInfo[] props;
                        try { props = t.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); } catch { continue; }
                        foreach (var p in props)
                        {
                            if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType)) continue;
                            object val = null;
                            try { val = p.GetValue(null); } catch { continue; }
                            if (val == null) continue;
                            var enumer = val as System.Collections.IEnumerable;
                            if (enumer == null) continue;

                            foreach (var item in enumer) AddItemToThresholdsIfValid(item, thresholds, overrides, logger);

                            if (thresholds.Count > 0)
                            {
                                logger.LogInfo($"[StockAlert] Populated {thresholds.Count} entries from {t.FullName}.{p.Name} (assembly {asm.GetName().Name}).");
                                SortThresholds(thresholds);
                                return;
                            }
                        }

                        // static fields
                        FieldInfo[] fields;
                        try { fields = t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); } catch { continue; }
                        foreach (var f in fields)
                        {
                            if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(f.FieldType)) continue;
                            object val = null;
                            try { val = f.GetValue(null); } catch { continue; }
                            if (val == null) continue;
                            var enumer = val as System.Collections.IEnumerable;
                            if (enumer == null) continue;

                            foreach (var item in enumer) AddItemToThresholdsIfValid(item, thresholds, overrides, logger);

                            if (thresholds.Count > 0)
                            {
                                logger.LogInfo($"[StockAlert] Populated {thresholds.Count} entries from {t.FullName}.{f.Name} (assembly {asm.GetName().Name}).");
                                SortThresholds(thresholds);
                                return;
                            }
                        }

                        // instance manager pattern
                        try
                        {
                            var instProp = t.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                            if (instProp != null)
                            {
                                object inst = null;
                                try { inst = instProp.GetValue(null); } catch { inst = null; }
                                if (inst != null)
                                {
                                    var instType = inst.GetType();
                                    var instProps = instType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    foreach (var ip in instProps)
                                    {
                                        if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(ip.PropertyType)) continue;
                                        object val = null;
                                        try { val = ip.GetValue(inst); } catch { continue; }
                                        if (val == null) continue;
                                        var enumer = val as System.Collections.IEnumerable;
                                        if (enumer == null) continue;
                                        foreach (var item in enumer) AddItemToThresholdsIfValid(item, thresholds, overrides, logger);
                                        if (thresholds.Count > 0)
                                        {
                                            logger.LogInfo($"[StockAlert] Populated {thresholds.Count} entries from {t.FullName}.Instance.{ip.Name} (assembly {asm.GetName().Name}).");
                                            SortThresholds(thresholds);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        catch { /* ignore */ }
                    }
                }

                // 2) fallback: ScriptableObjects and scene MonoBehaviours
                try
                {
                    var soType = typeof(UnityEngine.ScriptableObject);
                    var allSOs = UnityEngine.Resources.FindObjectsOfTypeAll(soType);
                    foreach (var so in allSOs)
                    {
                        AddItemToThresholdsIfValid(so, thresholds, overrides, logger);
                        if (thresholds.Count > 0) break;
                    }

                    var monoType = typeof(UnityEngine.MonoBehaviour);
                    var allMBs = UnityEngine.Object.FindObjectsOfType(monoType);
                    foreach (var mb in allMBs)
                    {
                        var mbType = mb.GetType();
                        foreach (var prop in mbType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType)) continue;
                            object val = null;
                            try { val = prop.GetValue(mb); } catch { continue; }
                            if (val == null) continue;
                            var enumer = val as System.Collections.IEnumerable;
                            if (enumer == null) continue;
                            foreach (var it in enumer) AddItemToThresholdsIfValid(it, thresholds, overrides, logger);
                            if (thresholds.Count > 0) break;
                        }
                        if (thresholds.Count > 0) break;

                        foreach (var field in mbType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(field.FieldType)) continue;
                            object val = null;
                            try { val = field.GetValue(mb); } catch { continue; }
                            if (val == null) continue;
                            var enumer = val as System.Collections.IEnumerable;
                            if (enumer == null) continue;
                            foreach (var it in enumer) AddItemToThresholdsIfValid(it, thresholds, overrides, logger);
                            if (thresholds.Count > 0) break;
                        }
                        if (thresholds.Count > 0) break;
                    }

                    if (thresholds.Count > 0)
                    {
                        logger.LogInfo($"[StockAlert] Populated {thresholds.Count} entries from ScriptableObjects/scene objects.");
                        SortThresholds(thresholds);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"[StockAlert] Fallback ScriptableObject/scene scan failed: {ex.Message}");
                }

                logger.LogInfo("[StockAlert] TryPopulateGoodsFromGame: no goods collection found by heuristics.");
            }
            catch (Exception ex)
            {
                logger.LogDebug($"[StockAlert] TryPopulateGoodsFromGame failed: {ex.Message}");
            }
        }

        // GoodModel-aware extraction and generic fallbacks
        private static void AddItemToThresholdsIfValid(object item, List<ThresholdEntry> thresholds, Dictionary<string, string> overrides, ManualLogSource logger)
        {
            if (item == null) return;
            var t = item.GetType();

            if (t.IsPrimitive || t == typeof(string) || t.IsEnum || t.IsValueType) return;
            if (t.FullName != null && (t.FullName.StartsWith("UnityEngine.") || t.FullName.StartsWith("System.") || t.FullName.StartsWith("BepInEx."))) return;

            string id = null;
            string name = null;

            // GoodModel-specific extraction
            if (t.Name.Equals("GoodModel", StringComparison.OrdinalIgnoreCase) || t.Name.IndexOf("Good", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                id = TryGetStringProperty(item, "consoleId", "consoleID", "Id", "ID", "Key", "name");
                try
                {
                    var dnProp = t.GetProperty("displayName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (dnProp != null)
                    {
                        var dnVal = dnProp.GetValue(item);
                        if (dnVal != null)
                        {
                            var getText = dnVal.GetType().GetMethod("GetText", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase, null, Type.EmptyTypes, null);
                            if (getText != null)
                            {
                                var txt = getText.Invoke(dnVal, null) as string;
                                if (!string.IsNullOrEmpty(txt)) name = txt;
                            }
                            if (string.IsNullOrEmpty(name))
                            {
                                try { name = dnVal.ToString(); } catch { }
                            }
                        }
                    }
                }
                catch { /* ignore */ }
            }

            // generic fallbacks
            if (string.IsNullOrEmpty(id))
                id = TryGetStringProperty(item, "Id", "ID", "Key", "KeyName", "KeyId", "Identifier", "name");

            if (string.IsNullOrEmpty(name))
                name = TryGetStringProperty(item, "DisplayName", "LocalizedName", "Name", "Title", "label", "title");

            if (string.IsNullOrEmpty(name))
            {
                try { name = TryResolveNameFromItem(item); } catch { }
            }

            if (string.IsNullOrEmpty(id))
            {
                var f = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                         .FirstOrDefault(fi => fi.Name.Equals("id", StringComparison.OrdinalIgnoreCase) || fi.Name.IndexOf("key", StringComparison.OrdinalIgnoreCase) >= 0);
                if (f != null)
                {
                    try { id = f.GetValue(item)?.ToString(); } catch { }
                }
            }

            if (string.IsNullOrEmpty(id))
            {
                try
                {
                    var s = item.ToString();
                    if (!string.IsNullOrWhiteSpace(s) && s.Length > 1) id = s;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(id)) return;

            id = id.Trim();

            if (overrides.TryGetValue(id, out var overrideName))
            {
                name = overrideName;
            }

            if (string.IsNullOrEmpty(name)) name = id;

            if (thresholds.Any(x => string.Equals(x.Id, id, StringComparison.Ordinal))) return;

            thresholds.Add(new ThresholdEntry
            {
                Id = id,
                Name = name,
                Threshold = 0f,
                ThresholdText = "0",
                NameEdit = name
            });

            logger.LogInfo($"[StockAlert] Discovered item: type={t.FullName}; id='{id}'; name='{name}'");
        }

        private static void SortThresholds(List<ThresholdEntry> thresholds)
        {
            thresholds.Sort((a, b) =>
            {
                var ia = UI.GetOrderIndex(a.Id);
                var ib = UI.GetOrderIndex(b.Id);
                if (ia != ib) return ia.CompareTo(ib);
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }

        // Helpers used by dumps and discovery
        public static string TryGetStringProperty(object obj, params string[] propNames)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var pn in propNames)
            {
                try
                {
                    var p = t.GetProperty(pn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (p != null)
                    {
                        var v = p.GetValue(obj);
                        if (v != null) return v.ToString();
                    }

                    var m = t.GetMethod(pn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase, null, Type.EmptyTypes, null);
                    if (m != null)
                    {
                        var mv = m.Invoke(obj, null);
                        if (mv != null) return mv.ToString();
                    }
                }
                catch { }
            }
            return null;
        }

        public static string TryResolveNameFromItem(object item)
        {
            if (item == null) return null;
            var t = item.GetType();

            string[] nameProps = { "DisplayName", "LocalizedName", "Name", "Title", "Label", "displayName", "name", "title" };
            foreach (var pn in nameProps)
            {
                try
                {
                    var p = t.GetProperty(pn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (p != null)
                    {
                        var v = p.GetValue(item);
                        if (v != null) return v.ToString();
                    }

                    var m = t.GetMethod(pn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase, null, Type.EmptyTypes, null);
                    if (m != null)
                    {
                        var mv = m.Invoke(item, null);
                        if (mv != null) return mv.ToString();
                    }
                }
                catch { }
            }

            string[] keyProps = { "LocalizationKey", "LocalizationId", "Key", "Id", "ID", "NameKey", "DisplayKey", "consoleId" };
            foreach (var kp in keyProps)
            {
                try
                {
                    var p = t.GetProperty(kp, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    var key = p?.GetValue(item)?.ToString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        var locType = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
                            .FirstOrDefault(tt => tt.Name.IndexOf("Localization", StringComparison.OrdinalIgnoreCase) >= 0
                                               || tt.Name.IndexOf("LocalizationManager", StringComparison.OrdinalIgnoreCase) >= 0
                                               || tt.Name.IndexOf("Lang", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (locType != null)
                        {
                            string[] locMethods = { "Get", "GetText", "Translate", "Localize", "GetString" };
                            foreach (var lm in locMethods)
                            {
                                try
                                {
                                    var method = locType.GetMethod(lm, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase, null, new[] { typeof(string) }, null);
                                    if (method != null)
                                    {
                                        var res = method.Invoke(null, new object[] { key });
                                        if (res != null) return res.ToString();
                                    }
                                }
                                catch { }
                            }
                        }

                        var resolveMethod = t.GetMethod("GetDisplayName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (resolveMethod != null)
                        {
                            try
                            {
                                var res = resolveMethod.Invoke(item, null);
                                if (res != null) return res.ToString();
                            }
                            catch { }
                        }

                        if (!string.IsNullOrWhiteSpace(key)) return key;
                    }
                }
                catch { }
            }

            try
            {
                var s = item.ToString();
                if (!string.IsNullOrEmpty(s)) return s;
            }
            catch { }

            return null;
        }
    }
}
