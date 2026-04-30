using System;
using System.Collections.Generic;
using System.Linq;
using Eremite.Buildings;
using StockAlert.Config;
using StockAlert.Game;
using StockAlert.Game.Discovery;
using UnityEngine;

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

            var lowGoods = new HashSet<string>(
                Discovery.Goods.Where(g => g.IsBelowThreshold).Select(g => g.Id),
                StringComparer.OrdinalIgnoreCase
            );

            var seenBuildings = new HashSet<int>();
            foreach (var workshop in GetRecipeBuildings())
            {
                var building = workshop.Base;
                if (building == null)
                {
                    continue;
                }

                seenBuildings.Add(building.Id);
                ApplyIndicatorState(building, workshop, lowGoods);
            }

            var removed = LastApplied.Keys.Where(id => !seenBuildings.Contains(id)).ToList();
            foreach (var id in removed)
            {
                LastApplied.Remove(id);
            }
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

        private static void ApplyIndicatorState(ProductionBuilding building, IWorkshop workshop, HashSet<string> lowGoods)
        {
            var icon = building.BuildingView?.transform?.Find("ToRotate/UI/NoWorkersIcon")?.gameObject;
            if (icon == null)
            {
                return;
            }

            var workerCount = building.CountWorkers();
            var maxWorkers = building.Workplaces?.Length ?? 0;
            var hasRelevantLowRecipe = workshop.Recipes.Any(r => r != null && !string.IsNullOrWhiteSpace(r.productName) && lowGoods.Contains(r.productName));

            var active = false;
            var color = WhiteColor;

            if (workerCount == 0)
            {
                active = true;
                color = hasRelevantLowRecipe ? RedColor : WhiteColor;
            }
            else if (hasRelevantLowRecipe && workerCount < maxWorkers)
            {
                active = true;
                color = YellowColor;
            }

            var snapshot = new IndicatorSnapshot(active, color);
            if (LastApplied.TryGetValue(building.Id, out var previous) && previous.Equals(snapshot))
            {
                return;
            }

            icon.SetActive(active);
            foreach (var spriteRenderer in icon.GetComponentsInChildren<SpriteRenderer>(true))
            {
                spriteRenderer.color = color;
            }

            LastApplied[building.Id] = snapshot;
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
}
