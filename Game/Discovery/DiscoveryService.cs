using System.Collections.Generic;
using System.Reflection;
using Eremite.Model;
using Eremite.Services;
using StockAlert.Game;
using StockAlert.Game.Discovery;
using StockAlert.Game.Hooks;

namespace StockAlert.Game.Discovery
{
    public static class Discovery
    {
        public static List<Good> Goods = new List<Good>();

        public static void Initialize()
        {
            Plugin.Log("Discovery.Initialize()");

            Goods.Clear();

            var services = GameAPI.GetGameServices();
            if (services == null)
            {
                Plugin.Log("Discovery.Initialize(): GameServices is null");
                return;
            }

            // Find GoodsService via reflection
            GoodsService goodsService = null;

            var serviceFields = services.GetType().GetFields(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var f in serviceFields)
            {
                if (typeof(GoodsService).IsAssignableFrom(f.FieldType))
                {
                    goodsService = (GoodsService)f.GetValue(services);
                    break;
                }
            }

            if (goodsService == null)
            {
                Plugin.Log("Discovery.Initialize(): Could not find GoodsService");
                return;
            }

            // Now reflect the private list of goods inside GoodsService
            List<Good> internalGoods = null;

            var goodsFields = goodsService.GetType().GetFields(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var f in goodsFields)
            {
                if (typeof(List<Good>).IsAssignableFrom(f.FieldType))
                {
                    internalGoods = (List<Good>)f.GetValue(goodsService);
                    break;
                }
            }

            if (internalGoods == null)
            {
                Plugin.Log("Discovery.Initialize(): Could not find internal goods list");
                return;
            }

            Goods.AddRange(internalGoods);

            Plugin.Log($"Discovery.Initialize(): Loaded {Goods.Count} goods");
        }

        public static void UpdateStock()
        {
            // Stock update logic will go here later
        }
    }
}
