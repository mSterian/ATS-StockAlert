using Eremite;
using Eremite.Model;
using Eremite.Model.State;
using Eremite.Services.Monitors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Eremite.Buildings;
using Eremite.Characters.Villagers;
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
        private static PropertyInfo _piBlightService;
        private static PropertyInfo _piLocalStorages;
        private static PropertyInfo _piIngredientsStorages;
        private static PropertyInfo _piIsGameActive;
        private static PropertyInfo _piCalendarService;
        private static PropertyInfo _piTimeScaleService;
        private static PropertyInfo _piNewsService;
        private static MethodInfo _miGetAmount;
        private static MethodInfo _miGetGlobalLimit;
        private static MethodInfo _miSetGlobalLimit;
        private static MethodInfo _miGetAliveRaceAmount;
        private static MethodInfo _miGetGlobalActiveCysts;
        private static MethodInfo _miGetDefaultProfessionAmount;
        private static MethodInfo _miGetDefaultProfessionVillager;
        private static MethodInfo _miSetProfession;
        private static MethodInfo _miGetTimeTillNextSeasonChange;
        private static MethodInfo _miPause;
        private static MethodInfo _miPublishNews;
        private static MethodInfo _miRemoveNews;
        private static PropertyInfo _piWorkshopLimits;
        private static PropertyInfo _piWorkshops;
        private static PropertyInfo _piBlightPosts;
        private static PropertyInfo _piGathererHuts;
        private static PropertyInfo _piCamps;
        private static FieldInfo _fiCurrentNews;
        private static PropertyInfo _piReactiveValue;

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

        public static int GetAmountInNonWarehouseBuildings(string goodId)
        {
            if (string.IsNullOrWhiteSpace(goodId))
            {
                return 0;
            }

            try
            {
                var storageService = GetStorageService();
                if (storageService == null)
                {
                    return 0;
                }

                var total = 0;

                _piLocalStorages ??= storageService.GetType().GetProperty("LocalStorages", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var localStorages = _piLocalStorages?.GetValue(storageService, null) as IDictionary;
                if (localStorages != null)
                {
                    foreach (DictionaryEntry entry in localStorages)
                    {
                        if (entry.Value is BuildingStorage storage)
                        {
                            total += storage.GetAmount(goodId);
                        }
                    }
                }

                _piIngredientsStorages ??= storageService.GetType().GetProperty("IngredientsStorages", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var ingredientStorages = _piIngredientsStorages?.GetValue(storageService, null) as IDictionary;
                if (ingredientStorages != null)
                {
                    foreach (DictionaryEntry entry in ingredientStorages)
                    {
                        if (entry.Value is BuildingIngredientsStorage storage && storage.goods != null)
                        {
                            total += storage.goods.GetAmount(goodId);
                        }
                    }
                }

                return total;
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
                var buildingsService = GetBuildingsService();
                if (buildingsService == null)
                {
                    return;
                }

                RefreshLimitsFromDictionary(buildingsService, "Workshops", ref _piWorkshops);
                RefreshLimitsFromDictionary(buildingsService, "BlightPosts", ref _piBlightPosts);
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

        public static int GetDefaultProfessionAmount(string raceId)
        {
            if (string.IsNullOrWhiteSpace(raceId))
            {
                return 0;
            }

            try
            {
                var villagersService = GetVillagersService();
                if (villagersService == null)
                {
                    return 0;
                }

                if (_miGetDefaultProfessionAmount == null)
                {
                    _miGetDefaultProfessionAmount = villagersService.GetType().GetMethod("GetDefaultProfessionAmount", new[] { typeof(string) });
                }

                if (_miGetDefaultProfessionAmount == null)
                {
                    return 0;
                }

                return (int)_miGetDefaultProfessionAmount.Invoke(villagersService, new object[] { raceId });
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static Villager GetDefaultProfessionVillager(string raceId, ProductionBuilding building)
        {
            if (string.IsNullOrWhiteSpace(raceId) || building == null)
            {
                return null;
            }

            try
            {
                var villagersService = GetVillagersService();
                if (villagersService == null)
                {
                    return null;
                }

                if (_miGetDefaultProfessionVillager == null)
                {
                    _miGetDefaultProfessionVillager = villagersService.GetType().GetMethod("GetDefaultProfessionVillager", new[] { typeof(string), typeof(ProductionBuilding) });
                }

                return _miGetDefaultProfessionVillager?.Invoke(villagersService, new object[] { raceId, building }) as Villager;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void AssignVillagerToWorkplace(Villager villager, ProductionBuilding building, int workplaceIndex)
        {
            if (villager == null || building == null || workplaceIndex < 0)
            {
                return;
            }

            try
            {
                var villagersService = GetVillagersService();
                if (villagersService == null)
                {
                    return;
                }

                if (_miSetProfession == null)
                {
                    _miSetProfession = villagersService.GetType().GetMethod("SetProfession", new[] { typeof(Villager), typeof(string), typeof(ProductionBuilding), typeof(int), typeof(bool) });
                }

                _miSetProfession?.Invoke(villagersService, new object[] { villager, building.Profession, building, workplaceIndex, true });
            }
            catch (Exception)
            {
            }
        }

        public static int GetGlobalActiveCysts()
        {
            try
            {
                var blightService = GetBlightService();
                if (blightService == null)
                {
                    return 0;
                }

                if (_miGetGlobalActiveCysts == null)
                {
                    _miGetGlobalActiveCysts = blightService.GetType().GetMethod("GetGlobalActiveCysts", Type.EmptyTypes);
                }

                if (_miGetGlobalActiveCysts == null)
                {
                    return 0;
                }

                return (int)_miGetGlobalActiveCysts.Invoke(blightService, null);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static float GetTimeTillNextSeasonChange()
        {
            try
            {
                var calendarService = GetCalendarService();
                if (calendarService == null)
                {
                    return float.MaxValue;
                }

                if (_miGetTimeTillNextSeasonChange == null)
                {
                    _miGetTimeTillNextSeasonChange = calendarService.GetType().GetMethod("GetTimeTillNextSeasonChange", Type.EmptyTypes);
                }

                if (_miGetTimeTillNextSeasonChange == null)
                {
                    return float.MaxValue;
                }

                return (float)_miGetTimeTillNextSeasonChange.Invoke(calendarService, null);
            }
            catch (Exception)
            {
                return float.MaxValue;
            }
        }

        public static GameDate GetCurrentGameDate()
        {
            try
            {
                var calendarService = GetCalendarService();
                var prop = calendarService?.GetType().GetProperty("GameDate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(calendarService, null) is GameDate date ? date : default;
            }
            catch (Exception)
            {
                return default;
            }
        }

        public static void PauseGame()
        {
            try
            {
                var timeScaleService = GetTimeScaleService();
                if (timeScaleService == null)
                {
                    return;
                }

                var isPausedMethod = timeScaleService.GetType().GetMethod("IsPaused", Type.EmptyTypes);
                if (isPausedMethod?.Invoke(timeScaleService, null) is bool isPaused && isPaused)
                {
                    return;
                }

                if (_miPause == null)
                {
                    _miPause = timeScaleService.GetType().GetMethod("Pause", new[] { typeof(bool) });
                }

                _miPause?.Invoke(timeScaleService, new object[] { false });
            }
            catch (Exception)
            {
            }
        }

        public static News PublishNews(string content, string description = null, AlertSeverity severity = AlertSeverity.Info)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                var newsService = GetNewsService();
                if (newsService == null)
                {
                    return null;
                }

                var before = new HashSet<News>(GetCurrentNews());
                if (_miPublishNews == null)
                {
                    _miPublishNews = newsService.GetType().GetMethod(
                        "PublishNews",
                        new[] { typeof(string), typeof(string), typeof(AlertSeverity), typeof(UnityEngine.Sprite), typeof(Eremite.IBroadcaster) }
                    );
                }

                _miPublishNews?.Invoke(newsService, new object[] { content, description, severity, null, null });
                return GetCurrentNews().FirstOrDefault(news => news != null && !before.Contains(news) && news.content == content);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void RemoveNews(News news)
        {
            if (news == null)
            {
                return;
            }

            try
            {
                var newsService = GetNewsService();
                if (newsService == null)
                {
                    return;
                }

                if (_miRemoveNews == null)
                {
                    _miRemoveNews = newsService.GetType().GetMethod("RemoveNews", new[] { typeof(News) });
                }

                _miRemoveNews?.Invoke(newsService, new object[] { news });
            }
            catch (Exception)
            {
            }
        }

        public static List<News> GetCurrentNews()
        {
            try
            {
                var newsService = GetNewsService();
                if (newsService == null)
                {
                    return new List<News>();
                }

                _fiCurrentNews ??= newsService.GetType().GetField("currentNews", BindingFlags.Instance | BindingFlags.NonPublic);
                var reactiveProperty = _fiCurrentNews?.GetValue(newsService);
                if (reactiveProperty == null)
                {
                    return new List<News>();
                }

                _piReactiveValue ??= reactiveProperty.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return _piReactiveValue?.GetValue(reactiveProperty, null) as List<News> ?? new List<News>();
            }
            catch (Exception)
            {
                return new List<News>();
            }
        }

        public static string GetBlightPostFuelId()
        {
            try
            {
                return GetSettings()?.blightConfig?.blightPostFuel?.Name ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
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

        public static List<ProductionBuilding> GetGatheringSourceBuildings()
        {
            var result = new List<ProductionBuilding>();

            try
            {
                var buildingsService = GetBuildingsService();
                if (buildingsService == null)
                {
                    return result;
                }

                AddProductionBuildingsFromDictionary(buildingsService, "GathererHuts", ref _piGathererHuts, result);
                AddProductionBuildingsFromDictionary(buildingsService, "Camps", ref _piCamps, result);
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

        private static object GetStorageService()
        {
            if (_piStorageService == null)
            {
                _piStorageService = typeof(GameMB).GetProperty("StorageService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piStorageService?.GetValue(null, null);
        }

        private static object GetBuildingsService()
        {
            if (_piBuildingsService == null)
            {
                _piBuildingsService = typeof(GameMB).GetProperty("BuildingsService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piBuildingsService?.GetValue(null, null);
        }

        private static object GetVillagersService()
        {
            if (_piVillagersService == null)
            {
                _piVillagersService = typeof(GameMB).GetProperty("VillagersService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piVillagersService?.GetValue(null, null);
        }

        private static object GetBlightService()
        {
            if (_piBlightService == null)
            {
                _piBlightService = typeof(GameMB).GetProperty("BlightService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piBlightService?.GetValue(null, null);
        }

        private static object GetCalendarService()
        {
            if (_piCalendarService == null)
            {
                _piCalendarService = typeof(GameMB).GetProperty("CalendarService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piCalendarService?.GetValue(null, null);
        }

        private static object GetTimeScaleService()
        {
            if (_piTimeScaleService == null)
            {
                _piTimeScaleService = typeof(GameMB).GetProperty("TimeScaleService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piTimeScaleService?.GetValue(null, null);
        }

        private static object GetNewsService()
        {
            if (_piNewsService == null)
            {
                _piNewsService = typeof(GameMB).GetProperty("NewsService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            return _piNewsService?.GetValue(null, null);
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

        private static void RefreshLimitsFromDictionary(object buildingsService, string propertyName, ref PropertyInfo propertyInfo)
        {
            propertyInfo ??= buildingsService.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var entries = propertyInfo?.GetValue(buildingsService, null) as IDictionary;
            if (entries == null)
            {
                return;
            }

            foreach (DictionaryEntry entry in entries)
            {
                var refreshMethod = entry.Value?.GetType().GetMethod("RefreshLimits", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                refreshMethod?.Invoke(entry.Value, null);
            }
        }

        private static void AddProductionBuildingsFromDictionary(object buildingsService, string propertyName, ref PropertyInfo propertyInfo, List<ProductionBuilding> buildings)
        {
            propertyInfo ??= buildingsService.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var entries = propertyInfo?.GetValue(buildingsService, null) as IDictionary;
            if (entries == null)
            {
                return;
            }

            foreach (DictionaryEntry entry in entries)
            {
                if (entry.Value is ProductionBuilding building)
                {
                    buildings.Add(building);
                }
            }
        }
    }
}
