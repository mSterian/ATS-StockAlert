using Eremite.Buildings;
using Eremite.Characters.Villagers;
using Eremite.Services;
using HarmonyLib;
using StockAlert.Config;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch(typeof(WorkersPriorityCalculator), "GetFreeWorkersSortingOrder")]
    internal static class WorkerAssignmentPriorityPatch
    {
        private const int CarryingGoodsPenalty = 10;

        private static void Postfix(Villager v, ProductionBuilding building, ref int __result)
        {
            var isCarrying = IsCarryingGoods(v);

            if (ConfigManager.AvoidAssigningCarryingBuilders && isCarrying)
            {
                __result += CarryingGoodsPenalty;
            }
        }

        private static bool IsCarryingGoods(Villager villager)
        {
            var carried = villager?.ActorState?.carriedGood ?? default;
            return carried.amount > 0 && !string.IsNullOrWhiteSpace(carried.name);
        }
    }
}
