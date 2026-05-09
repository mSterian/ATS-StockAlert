using System;
using System.Collections.Generic;
using System.Linq;
using Eremite;
using Eremite.Buildings;
using Eremite.Model;
using HarmonyLib;
using StockAlert.Config;
using StockAlert.Game;
using StockAlert.Game.Discovery;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StockAlert.UI.World
{
    internal static class BuildingAlertIndicators
    {
        private static readonly Color WhiteColor = Color.white;
        private static readonly Color RedColor = new Color(1f, 0.25f, 0.25f, 1f);
        private static readonly Color YellowColor = new Color(1f, 0.85f, 0.2f, 1f);

        private static readonly Dictionary<int, IndicatorSnapshot> LastApplied =
            new Dictionary<int, IndicatorSnapshot>();

        public static void Refresh()
        {
            if (!ConfigManager.ShowBuildingAlertIndicators)
            {
                RestoreVanilla();
                return;
            }

            var settings = GameAPI.GetSettings();
            var lowGoods = new HashSet<string>(
                Discovery.Goods.Where(g => g.IsBelowThreshold).Select(g => g.Id),
                StringComparer.OrdinalIgnoreCase
            );
            var blockedIngredientGoods = new HashSet<string>(Discovery.BlockedIngredientGoods, StringComparer.OrdinalIgnoreCase);

            var seenBuildings = new HashSet<int>();
            foreach (var workshop in GetRecipeBuildings())
            {
                var building = workshop.Base;
                if (building == null)
                {
                    continue;
                }

                seenBuildings.Add(building.Id);
                ApplyIndicatorState(building, workshop, lowGoods, blockedIngredientGoods, settings);
            }

            foreach (var building in GameAPI.GetGatheringSourceBuildings())
            {
                if (building == null)
                {
                    continue;
                }

                seenBuildings.Add(building.Id);
                ApplyGatheringIndicatorState(building, blockedIngredientGoods);
            }

            var removed = LastApplied.Keys.Where(id => !seenBuildings.Contains(id)).ToList();
            foreach (var id in removed)
            {
                LastApplied.Remove(id);
            }
        }

        public static void RefreshForView(ProductionBuildingView buildingView)
        {
            if (!ConfigManager.ShowBuildingAlertIndicators || buildingView == null)
            {
                return;
            }

            var workshop = GetRecipeBuildings().FirstOrDefault(w => ReferenceEquals(w?.Base?.BuildingView, buildingView));
            if (workshop?.Base == null)
            {
                return;
            }

            var settings = GameAPI.GetSettings();
            var lowGoods = new HashSet<string>(
                Discovery.Goods.Where(g => g.IsBelowThreshold).Select(g => g.Id),
                StringComparer.OrdinalIgnoreCase
            );
            var blockedIngredientGoods = new HashSet<string>(Discovery.BlockedIngredientGoods, StringComparer.OrdinalIgnoreCase);

            ApplyIndicatorState(workshop.Base, workshop, lowGoods, blockedIngredientGoods, settings);
        }

        public static void RestoreVanilla()
        {
            foreach (var workshop in GetRecipeBuildings())
            {
                var building = workshop.Base;
                if (building == null)
                {
                    continue;
                }

                var icon = building.BuildingView?.transform?.Find("ToRotate/UI/NoWorkersIcon")?.gameObject;
                if (icon == null)
                {
                    continue;
                }

                MakeIconClickThrough(icon);
                var active = building.BuildingState != null &&
                             building.BuildingState.finished &&
                             !building.BuildingState.isSleeping &&
                             building.CountWorkers() == 0;

                icon.SetActive(active);
                foreach (var spriteRenderer in icon.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    spriteRenderer.color = WhiteColor;
                }
            }

            LastApplied.Clear();
        }

        private static IEnumerable<IWorkshop> GetRecipeBuildings()
        {
            foreach (var workshop in GameAPI.GetRecipeBuildings())
            {
                yield return workshop;
            }
        }

        private static void ApplyIndicatorState(
            ProductionBuilding building,
            IWorkshop workshop,
            HashSet<string> lowGoods,
            HashSet<string> blockedIngredientGoods,
            Settings settings)
        {
            var icon = building.BuildingView?.transform?.Find("ToRotate/UI/NoWorkersIcon")?.gameObject;
            if (icon == null)
            {
                return;
            }

            MakeIconClickThrough(icon);
            var workerCount = building.CountWorkers();
            var maxWorkers = building.Workplaces?.Length ?? 0;
            var hasRelevantLowRecipe = workshop.Recipes.Any(r => IsEnabledLowRecipe(r, lowGoods));
            var hasRelevantIngredientSupplyRecipe = workshop.Recipes.Any(r => IsEnabledGatheringSupplyRecipe(r, blockedIngredientGoods, settings));
            var hasRelevantWork = hasRelevantLowRecipe || hasRelevantIngredientSupplyRecipe;

            var active = false;
            var color = WhiteColor;

            if (workerCount == 0)
            {
                active = true;
                color = hasRelevantWork ? RedColor : WhiteColor;
            }
            else if (hasRelevantWork && workerCount < maxWorkers)
            {
                active = true;
                color = YellowColor;
            }

            ApplySnapshot(building.Id, icon, active, color);
        }

        private static void ApplyGatheringIndicatorState(ProductionBuilding building, HashSet<string> blockedIngredientGoods)
        {
            var icon = building.BuildingView?.transform?.Find("ToRotate/UI/NoWorkersIcon")?.gameObject;
            if (icon == null)
            {
                return;
            }

            MakeIconClickThrough(icon);
            var workerCount = building.CountWorkers();
            var maxWorkers = building.Workplaces?.Length ?? 0;
            var hasRelevantGathering = HasRelevantGatheringRecipe(building, blockedIngredientGoods);

            var active = false;
            var color = WhiteColor;

            if (workerCount == 0)
            {
                active = true;
                color = hasRelevantGathering ? RedColor : WhiteColor;
            }
            else if (hasRelevantGathering && workerCount < maxWorkers)
            {
                active = true;
                color = YellowColor;
            }

            ApplySnapshot(building.Id, icon, active, color);
        }

        private static bool HasRelevantGatheringRecipe(ProductionBuilding building, HashSet<string> blockedIngredientGoods)
        {
            if (building?.BuildingModel == null ||
                blockedIngredientGoods == null ||
                blockedIngredientGoods.Count == 0)
            {
                return false;
            }

            var recipes = GetRecipeStates(building);
            if (recipes == null || recipes.Count == 0)
            {
                return false;
            }

            foreach (var blockedGood in blockedIngredientGoods)
            {
                if (string.IsNullOrWhiteSpace(blockedGood))
                {
                    continue;
                }

                var goodModel = GameAPI.GetSettings()?.GetGood(blockedGood);
                if (goodModel == null || !building.BuildingModel.IsSourceOf(goodModel))
                {
                    continue;
                }

                foreach (var recipeState in recipes)
                {
                    if (recipeState == null || !recipeState.active)
                    {
                        continue;
                    }

                    if (RecipeProducesGood(building, recipeState, blockedGood))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<RecipeState> GetRecipeStates(ProductionBuilding building)
        {
            switch (building)
            {
                case GathererHut gathererHut:
                    return gathererHut.state?.recipes;
                case Camp camp:
                    return camp.state?.recipes;
                default:
                    return null;
            }
        }

        private static bool RecipeProducesGood(ProductionBuilding building, RecipeState recipeState, string blockedGood)
        {
            switch (building)
            {
                case GathererHut gathererHut:
                    return string.Equals(gathererHut.GetRecipeModel(recipeState)?.GetProducedGood(), blockedGood, StringComparison.OrdinalIgnoreCase);
                case Camp camp:
                    return string.Equals(camp.GetRecipeModel(recipeState)?.GetProducedGood(), blockedGood, StringComparison.OrdinalIgnoreCase);
                default:
                    return false;
            }
        }

        private static void ApplySnapshot(int buildingId, GameObject icon, bool active, Color color)
        {
            var snapshot = new IndicatorSnapshot(active, color);
            var currentActive = icon.activeSelf;
            var currentColor = GetCurrentIconColor(icon);
            if (LastApplied.TryGetValue(buildingId, out var previous)
                && previous.Equals(snapshot)
                && currentActive == active
                && ColorsEqual(currentColor, color))
            {
                return;
            }

            icon.SetActive(active);
            foreach (var spriteRenderer in icon.GetComponentsInChildren<SpriteRenderer>(true))
            {
                spriteRenderer.color = color;
            }

            LastApplied[buildingId] = snapshot;
        }

        private static Color GetCurrentIconColor(GameObject icon)
        {
            if (icon == null)
            {
                return Color.clear;
            }

            var spriteRenderer = icon.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault();
            return spriteRenderer != null ? spriteRenderer.color : Color.clear;
        }

        private static bool ColorsEqual(Color a, Color b)
        {
            return Mathf.Approximately(a.r, b.r)
                && Mathf.Approximately(a.g, b.g)
                && Mathf.Approximately(a.b, b.b)
                && Mathf.Approximately(a.a, b.a);
        }

        private static void MakeIconClickThrough(GameObject icon)
        {
            if (icon == null)
            {
                return;
            }

            foreach (var graphic in icon.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            foreach (var selectable in icon.GetComponentsInChildren<Selectable>(true))
            {
                selectable.interactable = false;
            }

            foreach (var raycaster in icon.GetComponentsInChildren<GraphicRaycaster>(true))
            {
                raycaster.enabled = false;
            }

            foreach (var trigger in icon.GetComponentsInChildren<EventTrigger>(true))
            {
                trigger.enabled = false;
            }

            foreach (var component in icon.GetComponentsInChildren<Component>(true))
            {
                var type = component?.GetType();
                if (type == null)
                {
                    continue;
                }

                var typeName = type.Name;
                if (typeName.IndexOf("Collider", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                var enabledProperty = type.GetProperty("enabled");
                if (enabledProperty != null && enabledProperty.CanWrite)
                {
                    enabledProperty.SetValue(component, false, null);
                }
            }
        }

        private static bool IsEnabledLowRecipe(WorkshopRecipeState recipeState, HashSet<string> lowGoods)
        {
            if (recipeState == null ||
                !recipeState.active ||
                string.IsNullOrWhiteSpace(recipeState.productName) ||
                !lowGoods.Contains(recipeState.productName))
            {
                return false;
            }
            return true;
        }

        private static bool IsEnabledGatheringSupplyRecipe(
            WorkshopRecipeState recipeState,
            HashSet<string> blockedIngredientGoods,
            Settings settings)
        {
            if (recipeState == null ||
                !recipeState.active ||
                blockedIngredientGoods == null ||
                blockedIngredientGoods.Count == 0 ||
                string.IsNullOrWhiteSpace(recipeState.productName) ||
                !blockedIngredientGoods.Contains(recipeState.productName))
            {
                return false;
            }

            var recipeModel = settings?.GetWorkshopRecipe(recipeState.model);
            return recipeModel?.requiredGoods == null || recipeModel.requiredGoods.Length == 0;
        }

        private readonly struct IndicatorSnapshot : IEquatable<IndicatorSnapshot>
        {
            public IndicatorSnapshot(bool active, Color color)
            {
                Active = active;
                Color = color;
            }

            public bool Active { get; }

            public Color Color { get; }

            public bool Equals(IndicatorSnapshot other)
            {
                return Active == other.Active && Color.Equals(other.Color);
            }
        }
    }

    [HarmonyPatch(typeof(ProductionBuildingView), "ShowWorkers")]
    internal static class BuildingAlertIndicatorsViewPatch
    {
        [HarmonyPostfix]
        private static void ShowWorkersPostfix(ProductionBuildingView __instance)
        {
            if (__instance == null || !GameAPI.IsGameActive())
            {
                return;
            }

            BuildingAlertIndicators.RefreshForView(__instance);
        }
    }
}
