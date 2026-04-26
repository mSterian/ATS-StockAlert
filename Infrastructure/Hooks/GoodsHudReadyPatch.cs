using Eremite.View.HUD;
using HarmonyLib;
using SAPlugin = StockAlert.Infrastructure.Plugin.Plugin;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch(typeof(GoodsHUD), "SetUp")]
    internal static class GoodsHudReadyPatch
    {
        private static void Postfix(GoodsHUD __instance)
        {
            SAPlugin.Log("GoodsHUD.SetUp detected");
            SAPlugin.Instance?.OnGameReady();
        }
    }
}
