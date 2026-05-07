using System.Reflection;
using Eremite.Buildings;
using Eremite.Model;
using Eremite.View.HUD;
using HarmonyLib;
using StockAlert.Config;
using StockAlert.Game;
using TMPro;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch]
    internal static class IngredientsMenuStockPatch
    {
        private static FieldInfo _fiCounter;
        private static FieldInfo _fiSlot;

        [HarmonyPatch(typeof(IngredientsMenuSlot), "SetUp")]
        [HarmonyPostfix]
        private static void IngredientsMenuSlotSetUpPostfix(IngredientsMenuSlot __instance, IngredientState state)
        {
            TryApplyBuildingStockCounter(__instance, state);
        }

        [HarmonyPatch(typeof(IngredientsMenuSlot), "UpdateState")]
        [HarmonyPostfix]
        private static void IngredientsMenuSlotUpdateStatePostfix(IngredientsMenuSlot __instance)
        {
            var stateField = AccessTools.Field(typeof(IngredientsMenuSlot), "state");
            if (!(stateField?.GetValue(__instance) is IngredientState state))
            {
                return;
            }

            TryApplyBuildingStockCounter(__instance, state);
        }

        private static void TryApplyBuildingStockCounter(IngredientsMenuSlot menuSlot, IngredientState state)
        {
            if (!ConfigManager.ShowIngredientWheelBuildingStock
                || menuSlot == null
                || state?.good == null
                || !GameAPI.IsGameActive()
                || string.IsNullOrWhiteSpace(state.good.name))
            {
                return;
            }

            _fiSlot ??= typeof(IngredientsMenuSlot).GetField("slot", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(_fiSlot?.GetValue(menuSlot) is RangeGoodSlot slot))
            {
                return;
            }

            _fiCounter ??= typeof(RangeGoodSlot).GetField("counter", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(_fiCounter?.GetValue(slot) is TMP_Text counter))
            {
                return;
            }

            var currentText = counter.text ?? string.Empty;
            var baseText = currentText;
            var parenIndex = currentText.IndexOf(" (", System.StringComparison.Ordinal);
            if (parenIndex >= 0)
            {
                baseText = currentText.Substring(0, parenIndex);
            }

            var buildingAmount = GameAPI.GetAmountInNonWarehouseBuildings(state.good.name);
            counter.text = $"{baseText} ({buildingAmount})";
        }
    }
}
