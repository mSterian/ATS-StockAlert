using System.Collections.Generic;
using System.Linq;
using Eremite.Model;
using StockAlert.Config;
using StockAlert.Game;

namespace StockAlert.UI.World
{
    internal static class IdleBuildersAlert
    {
        private static News _activeNews;
        private static int _lastCount;

        public static void Refresh()
        {
            if (!ConfigManager.ShowIdleBuildersAlert || !GameAPI.IsGameActive())
            {
                Clear();
                return;
            }

            var count = BuilderStatusIndicators.GetIdleBuilderCount();
            if (count <= 0)
            {
                Clear();
                return;
            }

            var content = $"You have idle builders: {count}";
            var currentNews = GameAPI.GetCurrentNews();
            var existingNews = currentNews.FirstOrDefault(news => news != null && news.content == content);

            if (existingNews != null)
            {
                _activeNews = existingNews;
                _lastCount = count;
                return;
            }

            if (_activeNews != null)
            {
                GameAPI.RemoveNews(_activeNews);
                _activeNews = null;
            }

            if (_lastCount == count && currentNews.Any(news => news != null && news.content != null && news.content.StartsWith("You have idle builders:")))
            {
                return;
            }

            _activeNews = GameAPI.PublishNews(content);
            _lastCount = count;
        }

        public static void Clear()
        {
            if (_activeNews != null)
            {
                GameAPI.RemoveNews(_activeNews);
                _activeNews = null;
            }

            _lastCount = 0;
        }
    }
}
