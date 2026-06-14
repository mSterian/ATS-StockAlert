using System;
using System.Collections.Generic;
using System.Reflection;
using Eremite.Buildings;
using Eremite.Characters;
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
        private const int IdleBuilderBonus = 1;

        private static FieldInfo _fiActorBrain;
        private static FieldInfo _fiBrainStack;

        private static void Postfix(Villager v, ProductionBuilding building, ref int __result)
        {
            var isCarrying = IsCarryingGoods(v);

            if (ConfigManager.AvoidAssigningCarryingBuilders && isCarrying)
            {
                __result += CarryingGoodsPenalty;
            }

            if (ConfigManager.AvoidAssigningCarryingBuilders && !isCarrying && IsVillagerIdle(v))
            {
                __result -= IdleBuilderBonus;
            }
        }

        private static bool IsCarryingGoods(Villager villager)
        {
            var carried = villager?.ActorState?.carriedGood ?? default;
            return carried.amount > 0 && !string.IsNullOrWhiteSpace(carried.name);
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
    }
}
