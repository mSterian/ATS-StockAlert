using Eremite.View.HUD.Reputation;
using HarmonyLib;
using StockAlert.UI.HUD;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch(typeof(ReputationRewardButton), "Start")]
    internal static class BlueprintRewardZeroBuildingsPatch
    {
        [HarmonyPostfix]
        private static void StartPostfix(ReputationRewardButton __instance)
        {
            ZeroBuildingBlueprintHover.Attach(__instance);
        }
    }
}
