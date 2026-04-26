using Eremite.Services;
using HarmonyLib;
using StockAlert.Config;
using StockAlert.Game.Discovery;
using PanelUI = StockAlert.UI.Panels.UI;
using SAPlugin = StockAlert.Infrastructure.Plugin.Plugin;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch(typeof(WorkshopsService), "SetGlobalLimitFor")]
    internal static class WorkshopsServiceSetGlobalLimitForPatch
    {
        private static void Postfix(string goodModel, int limit)
        {
            SAPlugin.Log($"Production limit updated: {goodModel} -> {limit}");
            ProductionLimitSync.SyncThresholds();
        }
    }

    [HarmonyPatch(typeof(WorkshopsService), "LoadLimits")]
    internal static class WorkshopsServiceLoadLimitsPatch
    {
        private static void Postfix()
        {
            SAPlugin.Log("Workshop global limits loaded");
            ProductionLimitSync.SyncThresholds();
        }
    }

    internal static class ProductionLimitSync
    {
        public static void SyncThresholds()
        {
            ConfigManager.RefreshGoodsFromProductionLimits(Discovery.Goods);
            PanelUI.Refresh();
        }
    }
}
