using Eremite;
using Eremite.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Eremite.Buildings;
using System.Linq;

namespace StockAlert.Game
{
    public static class GameAPI
    {
        private static PropertyInfo _piMbSettings;
        private static PropertyInfo _piStorageService;
        private static PropertyInfo _piWorkshopsService;
        private static PropertyInfo _piVillagersService;
        private static PropertyInfo _piBuildingsService;
        private static PropertyInfo _piIsGameActive;
        private static MethodInfo _miGetAmount;
        private static MethodInfo _miGetGlobalLimit;
        private static MethodInfo _miSetGlobalLimit;
        private static MethodInfo _miGetAliveRaceAmount;
        private static PropertyInfo _piWorkshopLimits;
        private static PropertyInfo _piWorkshops;
        private static PropertyInfo _piBlightPosts;

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

        public static bool IsGameActive()
        {
            try
            {
                if (_piIsGameActive == null)
                {
                    _piIsGameActive = typeof(GameMB).GetProperty("IsGameActive", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                }

                if (_piIsGameActive == null)
                {
                    return false;
                }

                var value = _piIsGameActive.GetValue(null, null);
                return value is bool isActive && isActive;
            }
            catch (Exception)
            {
                return false;
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

        public static void SetProductionLimit(string goodId, int limit)
        {
            if (string.IsNullOrWhiteSpace(goodId))
            {
                return;
            }

            try
            {
                var workshopsService = GetWorkshopsService();
                if (workshopsService == null)
                {
                    return;
                }

                if (_miSetGlobalLimit == null)
                {
                    _miSetGlobalLimit = workshopsService.GetType().GetMethod("SetGlobalLimitFor", new[] { typeof(string), typeof(int) });
                }

                _miSetGlobalLimit?.Invoke(workshopsService, new object[] { goodId, limit });
            }
            catch (Exception)
            {
            }
        }

        public static void RefreshWorkshopLimits()
        {
            try
            {
                if (_piBuildingsService == null)
                {
                    _piBuildingsService = typeof(GameMB).GetProperty("BuildingsService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                }

                var buildingsService = _piBuildingsService?.GetValue(null, null);
                if (buildingsService == null)
                {
                    return;
                }

                if (_piWorkshops == null)
                {
                    _piWorkshops = buildingsService.GetType().GetProperty("Workshops", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }

                var workshops = _piWorkshops?.GetValue(buildingsService, null) as IDictionary;
                if (workshops == null)
                {
                    return;
                }

                foreach (DictionaryEntry entry in workshops)
                {
                    if (entry.Value is Workshop workshop)
                    {
                        workshop.RefreshLimits();
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static int GetAliveVillagerCount(string raceId)
        {
            if (string.IsNullOrWhiteSpace(raceId))
            {
                return 0;
            }

            try
            {
                if (_piVillagersService == null)
                {
                    _piVillagersService = typeof(GameMB).GetProperty("VillagersService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                }

                var villagersService = _piVillagersService?.GetValue(null, null);
                if (villagersService == null)
                {
                    return 0;
                }

                if (_miGetAliveRaceAmount == null)
                {
                    _miGetAliveRaceAmount = villagersService.GetType().GetMethod("GetAliveRaceAmount", new[] { typeof(string) });
                }

                if (_miGetAliveRaceAmount == null)
                {
                    return 0;
                }

                return (int)_miGetAliveRaceAmount.Invoke(villagersService, new object[] { raceId });
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static HashSet<string> GetAvailableRecipeGoods()
        {
            var goods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var buildingsService = GetBuildingsService();
                if (buildingsService == null)
                {
                    return goods;
                }

                AddRecipeGoodsFromDictionary(buildingsService, "Workshops", ref _piWorkshops, goods);
                AddRecipeGoodsFromDictionary(buildingsService, "BlightPosts", ref _piBlightPosts, goods);
            }
            catch (Exception)
            {
            }

            return goods;
        }

        public static List<IWorkshop> GetRecipeBuildings()
        {
            var result = new List<IWorkshop>();

            try
            {
                var buildingsService = GetBuildingsService();
                if (buildingsService == null)
                {
                    return result;
                }

                AddWorkshopsFromDictionary(buildingsService, "Workshops", ref _piWorkshops, result);
                AddWorkshopsFromDictionary(buildingsService, "BlightPosts", ref _piBlightPosts, result);
            }
            catch (Exception)
            {
            }

            return result;
        }

        private static object GetWorkshopsService()
        {
            if (_piWorkshopsService == null)
            {
                _piWorkshopsService = typeof(GameMB).GetProperty("WorkshopsService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piWorkshopsService?.GetValue(null, null);
        }

        private static object GetBuildingsService()
        {
            if (_piBuildingsService == null)
            {
                _piBuildingsService = typeof(GameMB).GetProperty("BuildingsService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piBuildingsService?.GetValue(null, null);
        }

        private static void AddRecipeGoodsFromDictionary(object buildingsService, string propertyName, ref PropertyInfo propertyInfo, HashSet<string> goods)
        {
            propertyInfo ??= buildingsService.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var entries = propertyInfo?.GetValue(buildingsService, null) as IDictionary;
            if (entries == null)
            {
                return;
            }

            foreach (DictionaryEntry entry in entries)
            {
                if (entry.Value is not IWorkshop workshop)
                {
                    continue;
                }

                foreach (var recipe in workshop.Recipes.Where(r => r != null && !string.IsNullOrWhiteSpace(r.productName)))
                {
                    goods.Add(recipe.productName);
                }
            }
        }

        private static void AddWorkshopsFromDictionary(object buildingsService, string propertyName, ref PropertyInfo propertyInfo, List<IWorkshop> workshops)
        {
            propertyInfo ??= buildingsService.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var entries = propertyInfo?.GetValue(buildingsService, null) as IDictionary;
            if (entries == null)
            {
                return;
            }

            foreach (DictionaryEntry entry in entries)
            {
                if (entry.Value is IWorkshop workshop)
                {
                    workshops.Add(workshop);
                }
            }
        }
    }
}
