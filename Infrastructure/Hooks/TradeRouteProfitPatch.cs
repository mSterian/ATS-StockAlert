using System;
using System.Reflection;
using Eremite;
using Eremite.Model;
using Eremite.Model.State;
using Eremite.View.HUD.TradeRoutes;
using HarmonyLib;
using StockAlert.Config;
using StockAlert.Game;
using TMPro;
using UnityEngine;

namespace StockAlert.Infrastructure.Hooks
{
    [HarmonyPatch]
    internal static class TradeRouteProfitPatch
    {
        private static readonly FieldInfo StateField = AccessTools.Field(typeof(TownOfferSlot), "state");
        private static readonly FieldInfo TimeTextField = AccessTools.Field(typeof(TownOfferSlot), "timeText");
        private static readonly PropertyInfo EffectsServiceProperty = AccessTools.Property(typeof(GameMB), "EffectsService");
        private static readonly PropertyInfo TradeServiceProperty = AccessTools.Property(typeof(GameMB), "TradeService");
        private static MethodInfo _getTradeRoutesRewardsMethod;
        private static MethodInfo _getTradeRoutesFuelMethod;
        private static MethodInfo _getValueInCurrencyMethod;

        [HarmonyPatch(typeof(TownOfferSlot), "SetUpTexts")]
        [HarmonyPostfix]
        private static void AddProfitToTravelTime(TownOfferSlot __instance)
        {
            if (!ConfigManager.ShowTradeRouteProfit || __instance == null)
            {
                return;
            }

            try
            {
                var state = StateField?.GetValue(__instance) as TownOfferState;
                var timeText = TimeTextField?.GetValue(__instance) as TMP_Text;
                if (state == null || timeText == null || !TryCalculateProfitPerGood(state, out var profitPerGood))
                {
                    return;
                }

                timeText.richText = true;
                timeText.text = $"{timeText.text}  <color={GetProfitColor(profitPerGood)}>Profit/good {FormatProfit(profitPerGood)}</color>";
            }
            catch (Exception)
            {
            }
        }

        private static bool TryCalculateProfitPerGood(TownOfferState state, out float profitPerGood)
        {
            profitPerGood = 0f;
            var settings = GameAPI.GetSettings();
            var config = settings?.tradeRoutesConfig;
            var goodModel = settings?.GetGood(state.good.name);
            var currency = settings?.tradeCurrency;
            var fuelModel = config?.fuel;
            if (settings == null || config == null || goodModel == null || currency == null || fuelModel == null)
            {
                return false;
            }

            if (state.amount <= 0 || state.good.amount <= 0 || state.price < 0 || state.fuel < 0)
            {
                return false;
            }

            var materialAmount = state.good.amount * state.amount;
            if (materialAmount <= 0)
            {
                return false;
            }

            var reward = ApplyTradeRouteRewardEffects(new Good(currency.Name, state.price));
            var fuel = ApplyTradeRouteFuelEffects(new Good(fuelModel.Name, state.fuel));
            var materialValue = GetTraderSellValueInAmber(state.good.name, materialAmount);
            var fuelValue = GetTraderSellValueInAmber(fuel.name, fuel.amount * state.amount);
            var rewardValue = reward.amount * state.amount;
            var totalProfit = rewardValue - materialValue - fuelValue;

            profitPerGood = totalProfit / materialAmount;
            return true;
        }

        private static float GetTraderSellValueInAmber(string goodName, int amount)
        {
            if (string.IsNullOrWhiteSpace(goodName) || amount <= 0)
            {
                return 0f;
            }

            var tradeService = TradeServiceProperty?.GetValue(null, null);
            if (tradeService == null)
            {
                return 0f;
            }

            _getValueInCurrencyMethod ??= tradeService.GetType().GetMethod("GetValueInCurrency", new[] { typeof(string), typeof(int) });
            return _getValueInCurrencyMethod?.Invoke(tradeService, new object[] { goodName, amount }) is float result
                ? result
                : 0f;
        }

        private static Good ApplyTradeRouteRewardEffects(Good reward)
        {
            var effectsService = EffectsServiceProperty?.GetValue(null, null);
            if (effectsService == null)
            {
                return reward;
            }

            _getTradeRoutesRewardsMethod ??= effectsService.GetType().GetMethod("GetTradeRoutesRewards", new[] { typeof(Good) });
            return _getTradeRoutesRewardsMethod?.Invoke(effectsService, new object[] { reward }) is Good result
                ? result
                : reward;
        }

        private static Good ApplyTradeRouteFuelEffects(Good fuel)
        {
            var effectsService = EffectsServiceProperty?.GetValue(null, null);
            if (effectsService == null)
            {
                return fuel;
            }

            _getTradeRoutesFuelMethod ??= effectsService.GetType().GetMethod("GetTradeRoutesFuel", new[] { typeof(Good) });
            return _getTradeRoutesFuelMethod?.Invoke(effectsService, new object[] { fuel }) is Good result
                ? result
                : fuel;
        }

        private static string FormatProfit(float profit)
        {
            return profit > 0f ? $"+{profit:0.#}" : profit.ToString("0.#");
        }

        private static string GetProfitColor(float profit)
        {
            if (profit > 0f)
            {
                return "#7CFF6B";
            }

            if (profit < 0f)
            {
                return "#FF6B5E";
            }

            return "#FFD95A";
        }
    }
}
