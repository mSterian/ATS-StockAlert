using Eremite;
using Eremite.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace StockAlert.Game
{
    public static class GameAPI
    {
        private static PropertyInfo _piMbSettings;
        private static PropertyInfo _piStorageService;
        private static PropertyInfo _piWorkshopsService;
        private static MethodInfo _miGetAmount;
        private static MethodInfo _miGetGlobalLimit;
        private static PropertyInfo _piWorkshopLimits;

        public static Settings GetSettings()
        {
            if (_piMbSettings == null)
            {
                _piMbSettings = typeof(MB).GetProperty("Settings", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piMbSettings?.GetValue(null, null) as Settings;
        }

        public static int GetStoredAmount(GoodModel model, string fallbackId)
        {
            if (model == null && string.IsNullOrWhiteSpace(fallbackId))
            {
                return 0;
            }

            try
            {
                if (_piStorageService == null)
                {
                    _piStorageService = typeof(GameMB).GetProperty("StorageService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                }

                var storage = _piStorageService?.GetValue(null, null);
                if (storage == null)
                {
                    return 0;
                }

                if (_miGetAmount == null)
                {
                    _miGetAmount = storage.GetType().GetMethod("GetAmount", new[] { typeof(string) });
                }

                if (_miGetAmount == null)
                {
                    return 0;
                }

                var goodName = model != null ? model.Name : fallbackId;
                return (int)_miGetAmount.Invoke(storage, new object[] { goodName });
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static Dictionary<string, int> GetProductionLimitsSnapshot()
        {
            var snapshot = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var workshopsService = GetWorkshopsService();
                if (workshopsService == null)
                {
                    return snapshot;
                }

                if (_piWorkshopLimits == null)
                {
                    _piWorkshopLimits = workshopsService.GetType().GetProperty("Limits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                var limits = _piWorkshopLimits?.GetValue(workshopsService, null) as IDictionary;
                if (limits == null)
                {
                    return snapshot;
                }

                foreach (DictionaryEntry entry in limits)
                {
                    if (entry.Key is string key && entry.Value is int value)
                    {
                        snapshot[key] = value;
                    }
                }
            }
            catch (Exception)
            {
            }

            return snapshot;
        }

        public static int GetProductionLimit(GoodModel model, string fallbackId)
        {
            if (string.IsNullOrWhiteSpace(fallbackId))
            {
                return 0;
            }

            try
            {
                var workshopsService = GetWorkshopsService();
                if (workshopsService == null)
                {
                    return 0;
                }

                if (_miGetGlobalLimit == null)
                {
                    _miGetGlobalLimit = workshopsService.GetType().GetMethod("GetGlobalLimitFor", new[] { typeof(string) });
                }

                if (_miGetGlobalLimit == null)
                {
                    return 0;
                }

                return (int)_miGetGlobalLimit.Invoke(workshopsService, new object[] { fallbackId });
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static object GetWorkshopsService()
        {
            if (_piWorkshopsService == null)
            {
                _piWorkshopsService = typeof(GameMB).GetProperty("WorkshopsService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piWorkshopsService?.GetValue(null, null);
        }
    }
}
