using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Eremite;
using Eremite.Characters;
using Eremite.Characters.Villagers;
using Eremite.Model;
using Eremite.View.HUD;
using StockAlert.Config;
using StockAlert.Game;
using UnityEngine;
using UnityEngine.UI;

namespace StockAlert.UI.World
{
    internal static class BuilderStatusIndicators
    {
        public readonly struct BuilderDemandCounts
        {
            public BuilderDemandCounts(int openSlots, int constructionSites)
            {
                OpenSlots = openSlots;
                ConstructionSites = constructionSites;
            }

            public int OpenSlots { get; }
            public int ConstructionSites { get; }
        }

        private static readonly Dictionary<int, BuilderIndicator> ActiveIndicators = new Dictionary<int, BuilderIndicator>();

        private static PropertyInfo _piVillagersService;
        private static PropertyInfo _piVillagers;
        private static FieldInfo _fiActorBrain;
        private static FieldInfo _fiBrainStack;
        private static Sprite _idleSprite;
        private static Sprite _builderSprite;

        public static void Refresh()
        {
            if (!ConfigManager.ShowBuilderStatusIcons || !GameAPI.IsGameActive())
            {
                Clear();
                return;
            }

            if (!EnsureSprites())
            {
                return;
            }

            var seenVillagers = new HashSet<int>();
            foreach (var villager in GetBuilderVillagers())
            {
                if (villager == null || !villager.IsAlive())
                {
                    continue;
                }

                var actorView = villager.ActorView;
                if (actorView == null)
                {
                    continue;
                }

                var isIdle = IsVillagerIdle(villager);
                var sprite = GetStatusSprite(isIdle);
                if (sprite == null)
                {
                    continue;
                }

                seenVillagers.Add(villager.Id);
                if (!ActiveIndicators.TryGetValue(villager.Id, out var indicator))
                {
                    indicator = new BuilderIndicator(villager);
                    ActiveIndicators[villager.Id] = indicator;
                }

                indicator.Show(sprite);
            }

            var removedIds = ActiveIndicators.Keys.Where(id => !seenVillagers.Contains(id)).ToList();
            foreach (var removedId in removedIds)
            {
                ActiveIndicators[removedId].Destroy();
                ActiveIndicators.Remove(removedId);
            }
        }

        private static Sprite GetStatusSprite(bool isIdle)
        {
            if (isIdle)
            {
                return ConfigManager.ShowIdleBuilderStatusIcons ? _idleSprite : null;
            }

            return ConfigManager.ShowBusyBuilderStatusIcons ? _builderSprite : null;
        }

        public static void Clear()
        {
            foreach (var indicator in ActiveIndicators.Values)
            {
                indicator.Destroy();
            }

            ActiveIndicators.Clear();
        }

        public static int GetIdleBuilderCount()
        {
            return GetBuilderVillagers().Count(villager => villager != null && villager.IsAlive() && IsVillagerIdle(villager));
        }

        public static BuilderDemandCounts GetBuilderDemandCounts()
        {
            var openSlots = 0;
            var constructionSites = 0;

            foreach (var building in GameAPI.GetConstructionBuildings())
            {
                if (building?.BuildingState == null || building.BuildingModel == null)
                {
                    continue;
                }

                if (building.IsFinished() || building.HasInternalBuilders)
                {
                    continue;
                }

                if (!GameAPI.CanConstructionUseBuilders(building))
                {
                    continue;
                }

                var state = building.BuildingState;
                var maxBuilders = building.BuildingModel.maxBuilders;
                if (maxBuilders <= 0)
                {
                    continue;
                }

                var openBuildingSlots = maxBuilders - Math.Max(0, state.builders);
                if (openBuildingSlots > 0)
                {
                    constructionSites++;
                    openSlots += openBuildingSlots;
                }
            }

            return new BuilderDemandCounts(openSlots, constructionSites);
        }

        public static bool HasIdleBuilderOfRace(string raceName)
        {
            if (string.IsNullOrWhiteSpace(raceName))
            {
                return false;
            }

            return GetIdleBuilderRaceIds().Contains(raceName);
        }

        public static bool HasIdleBuilderOfRace(RaceModel race)
        {
            if (race == null || string.IsNullOrWhiteSpace(race.Name))
            {
                return false;
            }

            return GetIdleBuilderRaceIds().Contains(race.Name);
        }

        public static Sprite GetIdleBuilderSprite()
        {
            EnsureSprites();
            return _idleSprite;
        }

        public static IReadOnlyList<string> GetIdleBuilderRaceSummaries()
        {
            return GetBuilderVillagers()
                .Where(villager => villager != null && villager.IsAlive() && IsVillagerIdle(villager))
                .GroupBy(villager => villager.Race)
                .OrderBy(group => GetRaceDisplayName(group.First(), group.Count()))
                .Select(group => $"{GetRaceDisplayName(group.First(), 1)}: {group.Count()}")
                .ToList();
        }

        private static HashSet<string> GetIdleBuilderRaceIds()
        {
            var raceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var villager in GetBuilderVillagers())
            {
                if (villager == null || !villager.IsAlive() || !IsVillagerIdle(villager))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(villager.Race))
                {
                    raceIds.Add(villager.Race);
                }

                if (!string.IsNullOrWhiteSpace(villager.raceModel?.Name))
                {
                    raceIds.Add(villager.raceModel.Name);
                }
            }

            return raceIds;
        }

        private static string GetRaceDisplayName(Villager villager, int count)
        {
            return villager.raceModel != null
                ? villager.raceModel.GetDisplayNameFor(count)
                : villager.Race;
        }

        private static IEnumerable<Villager> GetBuilderVillagers()
        {
            var settings = GameAPI.GetSettings();
            var builderProfession = settings?.DefaultProfession?.Name;
            if (string.IsNullOrWhiteSpace(builderProfession))
            {
                yield break;
            }

            if (_piVillagersService == null)
            {
                _piVillagersService = typeof(GameMB).GetProperty("VillagersService", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            }

            var villagersService = _piVillagersService?.GetValue(null, null);
            if (villagersService == null)
            {
                yield break;
            }

            _piVillagers ??= villagersService.GetType().GetProperty("Villagers", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var villagers = _piVillagers?.GetValue(villagersService, null) as IDictionary;
            if (villagers == null)
            {
                yield break;
            }

            foreach (DictionaryEntry entry in villagers)
            {
                if (entry.Value is Villager villager &&
                    string.Equals(villager.Profession, builderProfession, StringComparison.OrdinalIgnoreCase))
                {
                    yield return villager;
                }
            }
        }

        private static bool IsVillagerIdle(Villager villager)
        {
            try
            {
                _fiActorBrain ??= typeof(Actor).GetField("brain", BindingFlags.Instance | BindingFlags.NonPublic);
                var brain = _fiActorBrain?.GetValue(villager);
                if (brain == null)
                {
                    return false;
                }

                _fiBrainStack ??= brain.GetType().GetField("stack", BindingFlags.Instance | BindingFlags.NonPublic);
                if (!(_fiBrainStack?.GetValue(brain) is Stack<ActorTask> stack) || stack.Count == 0)
                {
                    return false;
                }

                var activeTask = stack.Peek();
                return activeTask != null && activeTask.WorkStatus == ActorWorkStatus.Idle;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool EnsureSprites()
        {
            if (_idleSprite == null)
            {
                _idleSprite = FindHudSprite("/HUD/GoodsHUD/SacrificeMarker/Content")
                    ?? GameAPI.GetSettings()?.monitorsConfig?.noBuildersIcon;
            }

            if (_builderSprite == null)
            {
                _builderSprite = FindBuilderHudSprite() ?? GameAPI.GetSettings()?.monitorsConfig?.noBuildersIcon;
            }

            return _idleSprite != null && _builderSprite != null;
        }

        private static Sprite FindBuilderHudSprite()
        {
            var racesHud = UnityEngine.Object.FindObjectOfType<RacesStatsHUD>();
            if (racesHud == null)
            {
                return null;
            }

            var buildersIcon = FindDescendantByName(racesHud.transform, "BuildersIcon");
            if (buildersIcon == null)
            {
                return null;
            }

            var image = buildersIcon.GetComponentInChildren<Image>(true);
            return image?.sprite;
        }

        private static Sprite FindHudSprite(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var source = GameObject.Find(path);
            if (source == null)
            {
                return null;
            }

            var image = source.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                return image.sprite;
            }

            var childImage = source.GetComponentInChildren<Image>(true);
            return childImage?.sprite;
        }

        private static Transform FindDescendantByName(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }

                var nested = FindDescendantByName(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private sealed class BuilderIndicator
        {
            private readonly GameObject _iconObject;
            private readonly SpriteRenderer _renderer;
            private readonly Transform _transform;

            public BuilderIndicator(Villager villager)
            {
                _iconObject = new GameObject("BuilderStatusIcon");
                _renderer = _iconObject.AddComponent<SpriteRenderer>();
                _renderer.material = new Material(Shader.Find("Sprites/Default"));
                _renderer.sortingLayerName = "UI";
                _renderer.sortingOrder = 5000;

                _iconObject.transform.SetParent(villager.ActorView.transform, false);
                _iconObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                _transform = _iconObject.transform;
                _transform.localScale = Vector3.one;
                _iconObject.AddComponent<BillboardToCamera>();
            }

            public void Show(Sprite sprite)
            {
                if (_renderer == null || sprite == null)
                {
                    return;
                }

                if (_renderer.sprite != sprite)
                {
                    _renderer.sprite = sprite;
                }

                UpdateScale(sprite);

                if (!_renderer.enabled)
                {
                    _renderer.enabled = true;
                }
            }

            private void UpdateScale(Sprite sprite)
            {
                if (_transform == null || sprite == null)
                {
                    return;
                }

                var bounds = sprite.bounds.size;
                var maxDimension = Mathf.Max(bounds.x, bounds.y);
                if (maxDimension <= 0.001f)
                {
                    _transform.localScale = Vector3.one;
                    return;
                }

                const float targetWorldSize = 0.5f;
                var scale = targetWorldSize / maxDimension;
                _transform.localScale = Vector3.one * scale;
            }

            public void Destroy()
            {
                if (_iconObject != null)
                {
                    UnityEngine.Object.Destroy(_iconObject);
                }
            }
        }

        private sealed class BillboardToCamera : MonoBehaviour
        {
            private void LateUpdate()
            {
                var main = Camera.main;
                if (main != null)
                {
                    transform.forward = main.transform.forward;
                }
            }
        }
    }
}
