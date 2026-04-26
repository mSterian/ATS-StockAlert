using Eremite;
using Eremite.Model;
using System;
using System.Reflection;

namespace StockAlert.Game
{
    public static class GameAPI
    {
        private static PropertyInfo _piMbSettings;
        private static PropertyInfo _piStorageService;
        private static MethodInfo _miGetAmount;

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
    }
}
