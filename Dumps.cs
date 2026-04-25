using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace StockAlert
{
    internal static class Dumps
    {
        public static void DumpThresholdsToCsv(ManualLogSource logger, List<ThresholdEntry> thresholds)
        {
            try
            {
                var outDir = Path.Combine(Environment.CurrentDirectory, "BepInEx", "plugins");
                Directory.CreateDirectory(outDir);
                var file = Path.Combine(outDir, "stockalert_thresholds.csv");

                using (var sw = new StreamWriter(file, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("Id,Name,Threshold");
                    foreach (var t in thresholds)
                    {
                        var id = t.Id?.Replace("\"", "\"\"") ?? "";
                        var name = t.Name?.Replace("\"", "\"\"") ?? "";
                        var thr = t.Threshold.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        sw.WriteLine($"\"{id}\",\"{name}\",\"{thr}\"");
                    }
                }

                logger.LogInfo($"[StockAlert] Dumped {thresholds.Count} threshold entries to: {Path.Combine("BepInEx", "plugins", "stockalert_thresholds.csv")}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[StockAlert] DumpThresholdsToCsv failed: {ex.Message}");
            }
        }

        public static void DumpDetailedGoodsToFile(ManualLogSource logger, int maxItems = 500)
        {
            try
            {
                var outDir = Path.Combine(Environment.CurrentDirectory, "BepInEx", "plugins");
                Directory.CreateDirectory(outDir);
                var file = Path.Combine(outDir, "stockalert_detailed_goods.csv");

                using (var sw = new StreamWriter(file, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("Type,Id,Name,Assembly,SampleProps");

                    var written = 0;

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
                            return true;
                        })
                        .ToArray();

                    foreach (var asm in assemblies)
                    {
                        Type[] types;
                        try { types = asm.GetTypes(); } catch { continue; }
                        foreach (var t in types)
                        {
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

                                foreach (var item in enumer)
                                {
                                    if (item == null) continue;
                                    if (WriteItemLine(sw, item)) written++;
                                    if (written >= maxItems) break;
                                }
                                if (written >= maxItems) break;
                            }
                            if (written >= maxItems) break;

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
                                foreach (var item in enumer)
                                {
                                    if (item == null) continue;
                                    if (WriteItemLine(sw, item)) written++;
                                    if (written >= maxItems) break;
                                }
                                if (written >= maxItems) break;
                            }
                            if (written >= maxItems) break;
                        }
                        if (written >= maxItems) break;
                    }

                    // fallback: ScriptableObjects
                    try
                    {
                        var soType = typeof(UnityEngine.ScriptableObject);
                        var allSOs = UnityEngine.Resources.FindObjectsOfTypeAll(soType);
                        foreach (var so in allSOs)
                        {
                            if (so == null) continue;
                            if (WriteItemLine(sw, so)) written++;
                            if (written >= maxItems) break;
                        }
                    }
                    catch { /* ignore */ }

                    sw.Flush();
                }

                logger.LogInfo($"[StockAlert] Dumped detailed goods to: {Path.Combine("BepInEx", "plugins", "stockalert_detailed_goods.csv")}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[StockAlert] DumpDetailedGoodsToFile failed: {ex.Message}");
            }

            bool WriteItemLine(StreamWriter sw, object item)
            {
                try
                {
                    var t = item.GetType();
                    if (t == null) return false;
                    if (t.FullName != null && (t.FullName.StartsWith("UnityEngine.") || t.FullName.StartsWith("System.") || t.FullName.StartsWith("BepInEx.")))
                        return false;

                    string id = Discovery.TryGetStringProperty(item, "Id", "ID", "Key", "KeyName", "KeyId", "Identifier", "name", "consoleId");
                    string name = Discovery.TryGetStringProperty(item, "DisplayName", "LocalizedName", "Name", "Title", "label", "title", "displayName");
                    if (string.IsNullOrEmpty(name))
                    {
                        try { name = Discovery.TryResolveNameFromItem(item); } catch { }
                    }

                    var sampleProps = new List<string>();
                    var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Take(8);
                    foreach (var p in props)
                    {
                        try
                        {
                            var v = p.GetValue(item);
                            if (v == null) continue;
                            var s = v.ToString();
                            if (s.Length > 120) s = s.Substring(0, 120) + "...";
                            sampleProps.Add($"{p.Name}={s}");
                        }
                        catch { }
                    }

                    var typeName = t.FullName.Replace("\"", "\"\"");
                    var idEsc = (id ?? "").Replace("\"", "\"\"");
                    var nameEsc = (name ?? "").Replace("\"", "\"\"");
                    var asm = t.Assembly?.GetName()?.Name ?? "";
                    var sample = string.Join(";", sampleProps).Replace("\"", "\"\"");

                    sw.WriteLine($"\"{typeName}\",\"{idEsc}\",\"{nameEsc}\",\"{asm}\",\"{sample}\"");
                    return true;
                }
                catch { return false; }
            }
        }

        public static void DumpGoodModelInstancesToFile(ManualLogSource logger, int maxItems = 1000)
        {
            try
            {
                var outDir = Path.Combine(Environment.CurrentDirectory, "BepInEx", "plugins");
                Directory.CreateDirectory(outDir);
                var file = Path.Combine(outDir, "stockalert_goodmodel_dump.csv");

                using (var sw = new StreamWriter(file, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("Type,Id,Name,Assembly,SampleProps");
                    int written = 0;

                    try
                    {
                        var allObjects = UnityEngine.Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object));
                        foreach (var obj in allObjects)
                        {
                            if (obj == null) continue;
                            var t = obj.GetType();
                            if (t.FullName != null && (t.FullName.StartsWith("UnityEngine.") || t.FullName.StartsWith("System.") || t.FullName.StartsWith("BepInEx.")))
                                continue;

                            bool looksLikeGood = t.Name.IndexOf("Good", StringComparison.OrdinalIgnoreCase) >= 0
                                                 || t.GetProperty("consoleId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null
                                                 || t.GetProperty("displayName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) != null;

                            if (!looksLikeGood) continue;

                            string id = Discovery.TryGetStringProperty(obj, "consoleId", "consoleID", "Id", "ID", "Key", "name");
                            string name = null;
                            try
                            {
                                var dnProp = t.GetProperty("displayName", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                if (dnProp != null)
                                {
                                    var dnVal = dnProp.GetValue(obj);
                                    if (dnVal != null)
                                    {
                                        var getText = dnVal.GetType().GetMethod("GetText", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase, null, Type.EmptyTypes, null);
                                        if (getText != null)
                                        {
                                            name = getText.Invoke(dnVal, null) as string;
                                        }
                                        if (string.IsNullOrEmpty(name)) name = dnVal.ToString();
                                    }
                                }
                            }
                            catch { /* ignore */ }

                            if (string.IsNullOrEmpty(name))
                                name = Discovery.TryGetStringProperty(obj, "DisplayName", "LocalizedName", "Name", "Title");

                            var sampleProps = new List<string>();
                            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Take(6))
                            {
                                try
                                {
                                    var v = p.GetValue(obj);
                                    if (v == null) continue;
                                    var s = v.ToString();
                                    if (s.Length > 120) s = s.Substring(0, 120) + "...";
                                    sampleProps.Add($"{p.Name}={s}");
                                }
                                catch { }
                            }

                            var typeName = t.FullName.Replace("\"", "\"\"");
                            var idEsc = (id ?? "").Replace("\"", "\"\"");
                            var nameEsc = (name ?? "").Replace("\"", "\"\"");
                            var asm = t.Assembly?.GetName()?.Name ?? "";
                            var sample = string.Join(";", sampleProps).Replace("\"", "\"\"");

                            sw.WriteLine($"\"{typeName}\",\"{idEsc}\",\"{nameEsc}\",\"{asm}\",\"{sample}\"");
                            written++;
                            if (written >= maxItems) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"[StockAlert] GoodModel scan failed: {ex.Message}");
                    }

                    sw.Flush();
                }

                logger.LogInfo($"[StockAlert] Dumped GoodModel-like instances to: {Path.Combine("BepInEx", "plugins", "stockalert_goodmodel_dump.csv")}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[StockAlert] DumpGoodModelInstancesToFile failed: {ex.Message}");
            }
        }
    }
}
