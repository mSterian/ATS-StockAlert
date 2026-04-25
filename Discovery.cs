using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Eremite;

namespace StockAlert
{
    public static class Discovery
    {
        private static readonly List<GoodInfo> _goods = new();
        private static bool _loaded = false;

        public static IReadOnlyList<GoodInfo> Goods => _goods;

        public static void Initialize()
        {
            new GameObject("StockAlertGameReadyHook")
                .AddComponent<GameReadyHook>()
                .Init(LoadGoods);
        }

        private static void LoadGoods()
        {
            if (_loaded) return;
            _loaded = true;

            var settings = GameAPI.Instance.GetSettings();

            foreach (var gm in settings.Goods)
            {
                if (ReferenceEquals(gm, null)) continue;

                var info = new GoodInfo
                {
                    Id = gm.Name,
                    DisplayName = gm.displayName?.Text ?? gm.Name,
                    Icon = Utils.SpriteToTexture(gm.icon),
                    CurrentAmount = 0
                };

                ConfigManager.EnsureGoodConfig(info);
                _goods.Add(info);
            }
        }

        public static void UpdateStock()
        {
            if (!_loaded) return;

            var storage = GameAPI.Instance.GetStorage();

            foreach (var g in _goods)
                g.CurrentAmount = storage.GetAmount(g.Id);
        }

        public static IEnumerable<GoodInfo> GetCriticalGoods()
        {
            return _goods
                .Where(g => g.IsBelowThreshold)
                .OrderBy(g => (float)g.CurrentAmount / g.Threshold)
                .ThenBy(g => g.DisplayName);
        }
    }

    public class GameReadyHook : MonoBehaviour
    {
        private System.Action _callback;

        public void Init(System.Action cb)
        {
            _callback = cb;
            GameAPI.Instance.GetGameServices().AddLoadingCallback(OnGameReady);
        }

        private void OnGameReady()
        {
            _callback?.Invoke();
            Destroy(gameObject);
        }
    }
}
