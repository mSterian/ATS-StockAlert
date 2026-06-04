using Eremite.View.HUD.Rainpunk;
using HarmonyLib;
using StockAlert.UI.HUD;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch]
    internal static class BlueprintRewardZeroBuildingsPatch
    {
        [HarmonyPatch(typeof(BlightHUD), "Start")]
        [HarmonyPostfix]
        private static void BlightHudStartPostfix(BlightHUD __instance)
        {
            ZeroBuildingBlueprintHover.Attach(__instance);
        }
    }
}
