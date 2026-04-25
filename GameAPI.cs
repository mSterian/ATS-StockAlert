using UnityEngine;
using Eremite;
using Eremite.Model;
using Eremite.Services;

namespace StockAlert
{
    public class GameAPI : GameMB
    {
        private static GameAPI _instance;

        public static GameAPI Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("StockAlertGameAPI");
                    _instance = go.AddComponent<GameAPI>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public Settings GetSettings() => Settings;
        public IStorageService GetStorage() => StorageService;
        public IInputService GetInput() => InputService;
        public IGameServices GetGameServices() => GameServices;
    }
}
