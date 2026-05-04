using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eremite.Buildings;
using Eremite.Model;
using StockAlert.Config;
using StockAlert.Core.Models;
using StockAlert.Infrastructure.Plugin;

namespace StockAlert.Game.Discovery
{
    public static class Discovery
    {
        public static List<GoodInfo> Goods { get; } = new List<GoodInfo>();

        public static void Initialize()
        {
            Plugin.Log("Discovery.Initialize()");

            Goods.Clear();

            var settings = GameAPI.GetSettings();
            if (settings == null)
            {
                Plugin.Log("Discovery.Initialize(): Settings is null");
                return;
            }

            var goods = settings.Goods;
            if (goods == null || goods.Length == 0)
            {
                Plugin.Log("Discovery.Initialize(): No goods found in Settings");
                return;
            }

            foreach (var model in goods)
            {
                if (model == null)
                {
                    continue;
                }

                var good = new GoodInfo
                {
                    Model = model,
                    Id = model.Name,
                    ConfigKey = BuildConfigKey(model.Name),
                    DisplayName = ResolveDisplayName(model),
                    Icon = model.icon,
                    CurrentAmount = 0
                };

                ConfigManager.EnsureGoodConfig(good);
                Goods.Add(good);
            }

            Goods.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            ConfigManager.RefreshGoodsFromProductionLimits(Goods);
            UpdateStock();

            Plugin.Log($"Discovery.Initialize(): Loaded {Goods.Count} goods");
        }

        private static string ResolveDisplayName(GoodModel model)
        {
            var localized = model.displayName?.ToString();
            if (!string.IsNullOrWhiteSpace(localized) &&
                localized.IndexOf("Missing key", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return localized;
            }

            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                return model.Name;
            }

            return "Unknown Good";
        }

        private static string BuildConfigKey(string rawId)
        {
            if (string.IsNullOrWhiteSpace(rawId))
            {
                return "UnknownGood";
            }

            var builder = new StringBuilder(rawId.Length);
            foreach (var ch in rawId)
            {
                switch (ch)
                {
                    case '=':
                    case '\n':
                    case '\t':
                    case '\\':
                    case '"':
                    case '\'':
                    case '[':
                    case ']':
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
            }

            return builder.ToString();
        }

        public static void UpdateStock()
        {
            foreach (var good in Goods)
            {
                if (good.Model == null)
                {
                    continue;
                }

                good.CurrentAmount = GameAPI.GetStoredAmount(good.Model, good.Id);
            }

            UpdateIngredientWarnings();
        }

        private static void UpdateIngredientWarnings()
        {
            var settings = GameAPI.GetSettings();
            if (settings == null)
            {
                ClearIngredientWarnings();
                return;
            }

            var visibleGoods = Goods.Where(g => g.IsBelowThreshold).ToList();
            if (visibleGoods.Count == 0)
            {
                ClearIngredientWarnings();
                return;
            }

            var availableGlobalAmounts = Goods.ToDictionary(g => g.Id, g => g.CurrentAmount, StringComparer.OrdinalIgnoreCase);
            var bestGradeByGood = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var canContinueAtBestGrade = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var visibleGoodIds = new HashSet<string>(visibleGoods.Select(g => g.Id), StringComparer.OrdinalIgnoreCase);

            foreach (var workshop in GameAPI.GetRecipeBuildings())
            {
                if (workshop?.Recipes == null)
                {
                    continue;
                }

                foreach (var recipeState in workshop.Recipes)
                {
                    if (recipeState == null || !recipeState.active)
                    {
                        continue;
                    }

                    var recipeModel = settings.GetWorkshopRecipe(recipeState.model);
                    if (recipeModel == null || string.IsNullOrWhiteSpace(recipeModel.producedGood?.Name))
                    {
                        continue;
                    }

                    var productId = recipeModel.producedGood.Name;
                    if (!visibleGoodIds.Contains(productId))
                    {
                        continue;
                    }

                    var gradeLevel = recipeModel.grade?.level ?? 0;
                    var canContinue = CanRecipeContinue(workshop, recipeModel, availableGlobalAmounts);

                    if (!bestGradeByGood.TryGetValue(productId, out var currentBestGrade) || gradeLevel > currentBestGrade)
                    {
                        bestGradeByGood[productId] = gradeLevel;
                        canContinueAtBestGrade[productId] = canContinue;
                    }
                    else if (gradeLevel == currentBestGrade)
                    {
                        canContinueAtBestGrade[productId] = canContinueAtBestGrade[productId] || canContinue;
                    }
                }
            }

            foreach (var good in Goods)
            {
                good.IsIngredientBlocked = good.IsBelowThreshold
                    && bestGradeByGood.ContainsKey(good.Id)
                    && canContinueAtBestGrade.TryGetValue(good.Id, out var canContinue)
                    && !canContinue;
            }
        }

        private static bool CanRecipeContinue(IWorkshop workshop, WorkshopRecipeModel recipeModel, IReadOnlyDictionary<string, int> globalAmounts)
        {
            if (recipeModel?.requiredGoods == null || recipeModel.requiredGoods.Length == 0)
            {
                return true;
            }

            var availableAmounts = globalAmounts.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            var localIngredients = workshop?.IngredientsStorage?.goods?.goods;
            if (localIngredients != null)
            {
                foreach (var pair in localIngredients)
                {
                    if (pair.Value <= 0)
                    {
                        continue;
                    }

                    availableAmounts[pair.Key] = availableAmounts.TryGetValue(pair.Key, out var current)
                        ? current + pair.Value
                        : pair.Value;
                }
            }

            return CanSatisfyIngredientSets(recipeModel.requiredGoods, 0, availableAmounts);
        }

        private static bool CanSatisfyIngredientSets(GoodsSet[] ingredientSets, int setIndex, Dictionary<string, int> availableAmounts)
        {
            if (ingredientSets == null || setIndex >= ingredientSets.Length)
            {
                return true;
            }

            var set = ingredientSets[setIndex];
            if (set?.goods == null || set.goods.Length == 0)
            {
                return CanSatisfyIngredientSets(ingredientSets, setIndex + 1, availableAmounts);
            }

            foreach (var ingredient in set.goods)
            {
                if (ingredient?.good == null)
                {
                    continue;
                }

                var ingredientId = ingredient.good.Name;
                var requiredAmount = ingredient.amount;
                if (!availableAmounts.TryGetValue(ingredientId, out var currentAmount) || currentAmount < requiredAmount)
                {
                    continue;
                }

                availableAmounts[ingredientId] = currentAmount - requiredAmount;
                if (CanSatisfyIngredientSets(ingredientSets, setIndex + 1, availableAmounts))
                {
                    availableAmounts[ingredientId] = currentAmount;
                    return true;
                }

                availableAmounts[ingredientId] = currentAmount;
            }

            return false;
        }

        private static void ClearIngredientWarnings()
        {
            foreach (var good in Goods)
            {
                good.IsIngredientBlocked = false;
            }
        }
    }
}
