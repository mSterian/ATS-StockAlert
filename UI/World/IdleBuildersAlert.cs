using System.Collections.Generic;
using System.Linq;
using Eremite.Model;
using StockAlert.Config;
using StockAlert.Game;

namespace StockAlert.UI.World
{
    internal static class IdleBuildersAlert
    {
        public const string AlertPrefix = "You have idle builders:";

        private static News _activeNews;
        private static string _lastContent;

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

            var content = BuildContent(count);
            var currentNews = GameAPI.GetCurrentNews();
            var existingNews = currentNews.FirstOrDefault(news => news != null && news.content == content);

            if (existingNews != null)
            {
                _activeNews = existingNews;
                _lastContent = content;
                return;
            }

            if (_activeNews != null)
            {
                GameAPI.RemoveNews(_activeNews);
                _activeNews = null;
            }

            if (_lastContent == content && currentNews.Any(news => news != null && news.content != null && news.content.StartsWith(AlertPrefix)))
            {
                return;
            }

            _activeNews = GameAPI.PublishNews(content);
            _lastContent = content;
        }

        public static void Clear()
        {
            if (_activeNews != null)
            {
                GameAPI.RemoveNews(_activeNews);
                _activeNews = null;
            }

            _lastContent = null;
        }

        private static string BuildContent(int count)
        {
            var raceSummaries = BuilderStatusIndicators.GetIdleBuilderRaceSummaries();
            if (raceSummaries.Count == 0)
            {
                return $"{AlertPrefix} {count}";
            }

            var species = string.Join("\n", raceSummaries);
            return $"{AlertPrefix} {count}\n{species}";
        }
    }
}
