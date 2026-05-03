using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Eremite.Buildings;
using Eremite.Characters.Villagers;
using Eremite.Model;
using StockAlert.Infrastructure.Plugin;

namespace StockAlert.Game
{
    internal static class WorkerAssignmentQueue
    {
        private readonly struct QueueKey : IEquatable<QueueKey>
        {
            public QueueKey(int buildingId, int workplaceIndex)
            {
                BuildingId = buildingId;
                WorkplaceIndex = workplaceIndex;
            }

            public int BuildingId { get; }
            public int WorkplaceIndex { get; }

            public bool Equals(QueueKey other)
            {
                return BuildingId == other.BuildingId && WorkplaceIndex == other.WorkplaceIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is QueueKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (BuildingId * 397) ^ WorkplaceIndex;
                }
            }
        }

        private sealed class QueueEntry
        {
            public QueueKey Key;
            public ProductionBuilding Building;
            public WorkplaceModel Workplace;
            public string RaceId;
            public long Order;
        }

        private static readonly Dictionary<QueueKey, QueueEntry> Entries = new Dictionary<QueueKey, QueueEntry>();
        private static PropertyInfo _piIsRemoved;
        private static PropertyInfo _piReactiveValue;
        private static long _nextOrder;

        public static void SetQueuedRace(ProductionBuilding building, WorkplaceModel workplace, RaceModel race)
        {
            if (building == null || workplace == null || race == null)
            {
                return;
            }

            SetQueuedRace(building, building.GetIndexOf(workplace), workplace, race.Name);
        }

        public static void SetQueuedRace(ProductionBuilding building, int workplaceIndex, WorkplaceModel workplace, string raceId)
        {
            if (building == null || workplaceIndex < 0 || string.IsNullOrWhiteSpace(raceId))
            {
                return;
            }

            var key = new QueueKey(building.Id, workplaceIndex);
            if (!Entries.TryGetValue(key, out var entry))
            {
                entry = new QueueEntry
                {
                    Key = key,
                    Building = building,
                    Workplace = workplace,
                    Order = ++_nextOrder
                };
                Entries[key] = entry;
            }

            entry.Building = building;
            entry.Workplace = workplace ?? entry.Workplace;
            entry.RaceId = raceId;
            Plugin.Log($"Queued {raceId} for {building.ModelName} workplace {workplaceIndex}");
        }

        public static void ClearQueuedRace(ProductionBuilding building, WorkplaceModel workplace)
        {
            if (building == null || workplace == null)
            {
                return;
            }

            ClearQueuedRace(building, building.GetIndexOf(workplace));
        }

        public static void ClearQueuedRace(ProductionBuilding building, int workplaceIndex)
        {
            if (building == null || workplaceIndex < 0)
            {
                return;
            }

            Entries.Remove(new QueueKey(building.Id, workplaceIndex));
        }

        public static RaceModel GetQueuedRace(ProductionBuilding building, WorkplaceModel workplace)
        {
            if (building == null || workplace == null)
            {
                return null;
            }

            return GetQueuedRace(building, building.GetIndexOf(workplace));
        }

        public static RaceModel GetQueuedRace(ProductionBuilding building, int workplaceIndex)
        {
            var raceId = GetQueuedRaceId(building, workplaceIndex);
            if (string.IsNullOrWhiteSpace(raceId))
            {
                return null;
            }

            var settings = GameAPI.GetSettings();
            return settings?.Races?.FirstOrDefault(r => string.Equals(r?.Name, raceId, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetQueuedRaceId(ProductionBuilding building, int workplaceIndex)
        {
            if (building == null || workplaceIndex < 0)
            {
                return null;
            }

            return Entries.TryGetValue(new QueueKey(building.Id, workplaceIndex), out var entry)
                ? entry.RaceId
                : null;
        }

        public static void ClearAll()
        {
            Entries.Clear();
        }

        public static void Process()
        {
            if (!GameAPI.IsGameActive() || Entries.Count == 0)
            {
                return;
            }

            foreach (var entry in Entries.Values.OrderBy(e => e.Order).ToList())
            {
                if (!IsEntryValid(entry))
                {
                    Entries.Remove(entry.Key);
                    continue;
                }

                var workplaceIndex = entry.Key.WorkplaceIndex;
                if (!entry.Building.IsWorkplaceFree(workplaceIndex))
                {
                    continue;
                }

                if (GameAPI.GetDefaultProfessionAmount(entry.RaceId) <= 0)
                {
                    continue;
                }

                Villager villager = GameAPI.GetDefaultProfessionVillager(entry.RaceId, entry.Building);
                if (villager == null)
                {
                    continue;
                }

                GameAPI.AssignVillagerToWorkplace(villager, entry.Building, workplaceIndex);
                Plugin.Log($"Assigned queued {entry.RaceId} to {entry.Building.ModelName} workplace {workplaceIndex}");
                Entries.Remove(entry.Key);
            }
        }

        private static bool IsEntryValid(QueueEntry entry)
        {
            return entry != null
                && entry.Building != null
                && !IsBuildingRemoved(entry.Building)
                && entry.Key.WorkplaceIndex >= 0
                && entry.Key.WorkplaceIndex < entry.Building.Workplaces.Length;
        }

        private static bool IsBuildingRemoved(ProductionBuilding building)
        {
            try
            {
                _piIsRemoved ??= typeof(Building).GetProperty("IsRemoved");
                var reactiveProperty = _piIsRemoved?.GetValue(building, null);
                if (reactiveProperty == null)
                {
                    return false;
                }

                _piReactiveValue ??= reactiveProperty.GetType().GetProperty("Value");
                var value = _piReactiveValue?.GetValue(reactiveProperty, null);
                return value is bool removed && removed;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
