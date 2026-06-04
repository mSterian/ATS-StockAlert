using System;
using Eremite;
using System.Reflection;
using Eremite.Buildings;
using Eremite.Buildings.UI;
using Eremite.Characters;
using Eremite.Characters.Villagers;
using Eremite.Model;
using HarmonyLib;
using StockAlert.Config;
using StockAlert.Game;
using StockAlert.UI.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch]
    internal static class WorkerAssignmentQueuePatch
    {
        [HarmonyPatch(typeof(BuildingWorkerSlot), "SetUp")]
        [HarmonyPostfix]
        private static void BuildingWorkerSlotSetUpPostfix(BuildingWorkerSlot __instance)
        {
            QueuedWorkerSlotCompanion.Attach(__instance);
            WorkerSlotHoverHighlighter.Attach(__instance);
        }

        [HarmonyPatch(typeof(BuildingWorkerSlot), "OnRacePicked")]
        [HarmonyPrefix]
        private static bool BuildingWorkerSlotOnRacePickedPrefix(BuildingWorkerSlot __instance, RaceModel race)
        {
            if (!ConfigManager.EnableQueuedWorkerAssignments
                || __instance == null
                || race == null)
            {
                return true;
            }

            if (!IsQueueModifierPressed())
            {
                return true;
            }

            var building = Traverse.Create(__instance).Field("building").GetValue<ProductionBuilding>();
            var workplace = Traverse.Create(__instance).Field("workplace").GetValue<WorkplaceModel>();
            var actor = Traverse.Create(__instance).Field("actor").GetValue<Actor>();
            if (building == null || workplace == null)
            {
                return true;
            }

            if (actor is Villager villager && villager.raceModel == race)
            {
                return false;
            }

            WorkerAssignmentQueue.SetQueuedRace(building, workplace, race);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(BuildingWorkerSlot), "TryAssign")]
        [HarmonyPrefix]
        private static bool BuildingWorkerSlotTryAssignPrefix(BuildingWorkerSlot __instance, RaceModel race)
        {
            if (!ConfigManager.EnableQueuedWorkerAssignments
                || race == null
                || !IsQueueModifierPressed())
            {
                return true;
            }

            var building = Traverse.Create(__instance).Field("building").GetValue<ProductionBuilding>();
            var workplace = Traverse.Create(__instance).Field("workplace").GetValue<WorkplaceModel>();
            if (building == null || workplace == null)
            {
                return true;
            }

            WorkerAssignmentQueue.SetQueuedRace(building, workplace, race);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(RacesMenuSlot), "UpdateButton")]
        [HarmonyPostfix]
        private static void RacesMenuSlotUpdateButtonPostfix(RacesMenuSlot __instance)
        {
            if (!ConfigManager.EnableQueuedWorkerAssignments || __instance == null)
            {
                return;
            }

            var button = Traverse.Create(__instance).Field("button").GetValue<Button>();
            if (button != null)
            {
                button.interactable = true;
            }
        }

        [HarmonyPatch(typeof(RacesMenuSlot), "SetUp")]
        [HarmonyPostfix]
        private static void RacesMenuSlotSetUpPostfix(RacesMenuSlot __instance)
        {
            IdleBuilderRaceMenuMarker.Attach(__instance);
            WorkerRaceMenuHoverHighlighter.Attach(__instance);
        }

        [HarmonyPatch(typeof(RacesMenuSlot), "OnPointerEnter")]
        [HarmonyPostfix]
        private static void RacesMenuSlotOnPointerEnterPostfix(RacesMenuSlot __instance, PointerEventData eventData)
        {
            WorkerRaceMenuHoverHighlighter.ShowCandidate(__instance);
        }

        [HarmonyPatch(typeof(RacesMenuSlot), "OnPointerExit")]
        [HarmonyPostfix]
        private static void RacesMenuSlotOnPointerExitPostfix(RacesMenuSlot __instance, PointerEventData eventData)
        {
            WorkerHoverRing.Hide();
        }

        [HarmonyPatch(typeof(RacesMenu), "OnClicked")]
        [HarmonyPrefix]
        private static bool RacesMenuOnClickedPrefix(RacesMenu __instance, RacesMenuSlot pick)
        {
            if (!ConfigManager.EnableQueuedWorkerAssignments || __instance == null || pick == null)
            {
                return true;
            }

            var race = pick.GetRace();
            if (race == null || !IsQueueModifierPressed())
            {
                return true;
            }

            Traverse.Create(__instance).Field("lastClick").SetValue(Time.unscaledTime);
            var callback = Traverse.Create(__instance).Field("callback").GetValue<Action<RaceModel>>();
            callback?.Invoke(race);

            var hideMethod = typeof(Eremite.View.UI.RadialMenu).GetMethod("Hide", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            hideMethod?.Invoke(__instance, null);
            return false;
        }

        private static bool IsQueueModifierPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                return keyboard.leftCtrlKey.isPressed
                    || keyboard.rightCtrlKey.isPressed
                    || keyboard.ctrlKey.isPressed;
            }

            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private static void PlayButtonSound()
        {
            try
            {
                var soundsManager = typeof(MB)
                    .GetProperty("SoundsManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?
                    .GetValue(null, null);
                soundsManager?.GetType().GetMethod("PlayButtonSound", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.Invoke(soundsManager, null);
            }
            catch (Exception)
            {
            }
        }
    }
}
