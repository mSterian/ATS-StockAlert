using Eremite.Model;
using Eremite.Model.State;
using Eremite.View.HUD;
using Eremite.View.HUD.Conditions;
using Eremite.View.HUD.Perks;
using HarmonyLib;
using StockAlert.UI.HUD;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch(typeof(PerkSlot), "SetUp")]
    public static class PerkSlotCountdownPatch
    {
        private static void Postfix(PerkSlot __instance, PerkState state)
        {
            if (__instance == null)
            {
                return;
            }

            var overlay = __instance.GetComponent<EventModifierCountdownOverlay>()
                          ?? __instance.gameObject.AddComponent<EventModifierCountdownOverlay>();
            overlay.SetUp(state);
        }
    }

    [HarmonyPatch(typeof(EffectSlot), "SetUp", typeof(EffectModel), typeof(bool))]
    public static class ConditionEffectSlotCountdownPatch
    {
        private static void Postfix(EffectSlot __instance, EffectModel model)
        {
            if (__instance == null || model == null)
            {
                return;
            }

            if (__instance.GetComponentInParent<ConditionsHUD>() == null)
            {
                return;
            }

            var overlay = __instance.GetComponent<EventModifierCountdownOverlay>()
                          ?? __instance.gameObject.AddComponent<EventModifierCountdownOverlay>();
            overlay.SetUp(model);
        }
    }
}
