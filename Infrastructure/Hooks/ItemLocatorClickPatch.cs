using System;
using Eremite;
using Eremite.Buildings;
using Eremite.Buildings.UI;
using Eremite.Model;
using Eremite.View.HUD;
using HarmonyLib;
using StockAlert.UI.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch]
    internal static class ItemLocatorClickPatch
    {
        [HarmonyPatch(typeof(GoodSlot), "SetUp")]
        [HarmonyPostfix]
        private static void GoodSlotSetUpPostfix(GoodSlot __instance, Good good)
        {
            TrackedGoodClickTarget.Attach(__instance, good.name);
        }

        [HarmonyPatch(typeof(GoodSlotButton), "OnClick")]
        [HarmonyPrefix]
        private static bool GoodSlotButtonOnClickPrefix(GoodSlotButton __instance)
        {
            if (!IsModifierPressed())
            {
                return true;
            }

            var current = __instance.GetGood();
            if (string.IsNullOrWhiteSpace(current.name))
            {
                return true;
            }

            ItemLocatorOverlay.Toggle(current.name);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(BuildingStorageProductSlot), "OnClick")]
        [HarmonyPrefix]
        private static bool BuildingStorageProductSlotOnClickPrefix(BuildingStorageProductSlot __instance)
        {
            if (!IsModifierPressed())
            {
                return true;
            }

            var good = Traverse.Create(__instance).Field("good").GetValue<Good>();
            if (string.IsNullOrWhiteSpace(good.name))
            {
                return true;
            }

            ItemLocatorOverlay.Toggle(good.name);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(IngredientsStorageSlot), "OnClick")]
        [HarmonyPrefix]
        private static bool IngredientsStorageSlotOnClickPrefix(IngredientsStorageSlot __instance)
        {
            if (!IsModifierPressed())
            {
                return true;
            }

            var good = Traverse.Create(__instance).Field("good").GetValue<Good>();
            if (string.IsNullOrWhiteSpace(good.name))
            {
                return true;
            }

            ItemLocatorOverlay.Toggle(good.name);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(ReorderableStorageSlot), "OnClick")]
        [HarmonyPrefix]
        private static bool ReorderableStorageSlotOnClickPrefix(ReorderableStorageSlot __instance)
        {
            if (!IsModifierPressed())
            {
                return true;
            }

            var good = Traverse.Create(__instance).Field("good").GetValue<Good>();
            if (string.IsNullOrWhiteSpace(good.name))
            {
                return true;
            }

            ItemLocatorOverlay.Toggle(good.name);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(RangeGoodSlot), "SetUp")]
        [HarmonyPostfix]
        private static void RangeGoodSlotSetUpPostfix(RangeGoodSlot __instance, Good good)
        {
            TrackedGoodClickTarget.Attach(__instance, good.name);
        }

        [HarmonyPatch(typeof(GoodsSetSlot), "OnClick")]
        [HarmonyPrefix]
        private static bool GoodsSetSlotOnClickPrefix(GoodsSetSlot __instance)
        {
            if (!IsModifierPressed())
            {
                return true;
            }

            var current = Traverse.Create(__instance).Field("current").GetValue<Good>();
            if (string.IsNullOrWhiteSpace(current.name))
            {
                return true;
            }

            ItemLocatorOverlay.Toggle(current.name);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(IngredientSlot), "OnClick")]
        [HarmonyPrefix]
        private static bool IngredientSlotOnClickPrefix(IngredientSlot __instance)
        {
            if (!IsModifierPressed())
            {
                return true;
            }

            var getGoodMethod = AccessTools.Method(typeof(IngredientSlot), "GetGood");
            var goodModel = getGoodMethod?.Invoke(__instance, null) as GoodModel;
            if (goodModel == null || string.IsNullOrWhiteSpace(goodModel.Name))
            {
                return true;
            }

            ItemLocatorOverlay.Toggle(goodModel.Name);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(IngredientsMenuSlot), "OnClick")]
        [HarmonyPrefix]
        private static bool IngredientsMenuSlotOnClickPrefix(IngredientsMenuSlot __instance)
        {
            if (!IsModifierPressed())
            {
                return true;
            }

            var state = Traverse.Create(__instance).Field("state").GetValue<IngredientState>();
            if (state?.good == null || string.IsNullOrWhiteSpace(state.good.name))
            {
                return true;
            }

            ItemLocatorOverlay.Toggle(state.good.name);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(WorkshopRecipeSlot), "OpenRecipesPopup")]
        [HarmonyPrefix]
        private static bool WorkshopRecipeSlotOpenRecipesPopupPrefix(WorkshopRecipeSlot __instance)
        {
            if (!IsModifierPressed())
            {
                return true;
            }

            var model = Traverse.Create(__instance).Field("model").GetValue<WorkshopRecipeModel>();
            var goodName = model?.producedGood?.good?.Name;
            if (string.IsNullOrWhiteSpace(goodName))
            {
                return true;
            }

            ItemLocatorOverlay.Toggle(goodName);
            PlayButtonSound();
            return false;
        }

        [HarmonyPatch(typeof(BlightPostRecipeSlot), "OpenRecipesPopup")]
        [HarmonyPrefix]
        private static bool BlightPostRecipeSlotOpenRecipesPopupPrefix(BlightPostRecipeSlot __instance)
        {
            if (!IsModifierPressed())
            {
                return true;
            }

            var model = Traverse.Create(__instance).Field("model").GetValue<WorkshopRecipeModel>();
            var goodName = model?.producedGood?.good?.Name;
            if (string.IsNullOrWhiteSpace(goodName))
            {
                return true;
            }

            ItemLocatorOverlay.Toggle(goodName);
            PlayButtonSound();
            return false;
        }

        private static bool IsModifierPressed()
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
                    .GetProperty("SoundsManager", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?
                    .GetValue(null, null);

                soundsManager?.GetType()
                    .GetMethod("PlayButtonSound", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(soundsManager, null);
            }
            catch (Exception)
            {
            }
        }
    }
}
