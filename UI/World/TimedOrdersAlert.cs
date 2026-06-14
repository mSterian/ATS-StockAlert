using System;
using System.Collections.Generic;
using System.Linq;
using Eremite.Model;
using Eremite.Services.Monitors;
using StockAlert.Config;
using StockAlert.Game;

namespace StockAlert.UI.World
{
    internal static class TimedOrdersAlert
    {
        private const int OneMinuteThreshold = 60;
        private const int ThirtySecondsThreshold = 30;

        private static readonly HashSet<string> FiredAlerts = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, News> ActiveNews = new Dictionary<string, News>(StringComparer.Ordinal);

        public static void Refresh()
        {
            if (!ConfigManager.TimedOrdersAlert || !GameAPI.IsGameActive())
            {
                Clear();
                return;
            }

            var activeOrders = GameAPI.GetActiveTimedOrders();
            var activeOrderIds = new HashSet<int>(activeOrders.Select(order => order.Id));
            ClearInactive(activeOrderIds);

            foreach (var order in activeOrders)
            {
                if (order.TimeLeft <= ThirtySecondsThreshold)
                {
                    Fire(order, ThirtySecondsThreshold, "30 seconds", AlertSeverity.Critical);
                    continue;
                }

                if (order.TimeLeft <= OneMinuteThreshold)
                {
                    Fire(order, OneMinuteThreshold, "1 minute", AlertSeverity.Warning);
                }
            }
        }

        public static void Clear()
        {
            foreach (var news in ActiveNews.Values.ToList())
            {
                GameAPI.RemoveNews(news);
            }

            ActiveNews.Clear();
            FiredAlerts.Clear();
        }

        private static void Fire(GameAPI.TimedOrderInfo order, int threshold, string label, AlertSeverity severity)
        {
            var key = GetKey(order.Id, threshold);
            if (!FiredAlerts.Add(key))
            {
                return;
            }

            if (threshold == ThirtySecondsThreshold)
            {
                RemoveNews(GetKey(order.Id, OneMinuteThreshold));
            }

            var content = $"Timed order: {order.DisplayName} has {label} left.";
            ActiveNews[key] = GameAPI.PublishNews(content, null, severity);
        }

        private static void ClearInactive(HashSet<int> activeOrderIds)
        {
            foreach (var key in ActiveNews.Keys.ToList())
            {
                if (TryGetOrderId(key, out var orderId) && activeOrderIds.Contains(orderId))
                {
                    continue;
                }

                RemoveNews(key);
            }

            FiredAlerts.RemoveWhere(key => !TryGetOrderId(key, out var orderId) || !activeOrderIds.Contains(orderId));
        }

        private static void RemoveNews(string key)
        {
            if (!ActiveNews.TryGetValue(key, out var news))
            {
                return;
            }

            GameAPI.RemoveNews(news);
            ActiveNews.Remove(key);
        }

        private static string GetKey(int orderId, int threshold)
        {
            return orderId + ":" + threshold;
        }

        private static bool TryGetOrderId(string key, out int orderId)
        {
            orderId = 0;
            var separatorIndex = key?.IndexOf(':') ?? -1;
            return separatorIndex > 0 && int.TryParse(key.Substring(0, separatorIndex), out orderId);
        }
    }
}
