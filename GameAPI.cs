using UnityEngine;
using Eremite;
using System.Reflection;

namespace StockAlert
{
    public static class GameAPI
    {
        private static GameMB _game;

        private static FieldInfo _fiGameServices;
        private static FieldInfo _fiSettings;
        private static FieldInfo _fiStorage;

        private static void EnsureRefs()
        {
            if (_game == null)
                _game = Object.FindObjectOfType<GameMB>();

            if (_game == null)
                return;

            if (_fiGameServices == null)
                _fiGameServices = typeof(GameMB).GetField("GameServices", BindingFlags.NonPublic | BindingFlags.Instance);

            if (_fiSettings == null)
                _fiSettings = typeof(GameMB).GetField("Settings", BindingFlags.NonPublic | BindingFlags.Instance);

            if (_fiStorage == null)
                _fiStorage = typeof(GameMB).GetField("StorageService", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static Eremite.Services.IGameServices GetGameServices()
        {
            EnsureRefs();
            return _fiGameServices?.GetValue(_game) as Eremite.Services.IGameServices;
        }

        public static Eremite.Model.Settings GetSettings()
        {
            EnsureRefs();
            return _fiSettings?.GetValue(_game) as Eremite.Model.Settings;
        }

        public static Eremite.Services.IStorageService GetStorage()
        {
            EnsureRefs();
            return _fiStorage?.GetValue(_game) as Eremite.Services.IStorageService;
        }
    }
}
