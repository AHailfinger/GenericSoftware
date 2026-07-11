using EnergyAutomate.Definitions;
using EnergyAutomate.Extentions;

namespace EnergyAutomate.Services
{
    /// <summary>
    /// Partial class for handling real-time power adjustments based on Tibber's real-time measurements.
    /// </summary>
    public partial class EnergyAutomateService
    {
        #region Fields

        private const int CRITICAL_BATTERY_THRESHOLD = 20;
        private const int HIGH_BATTERY_THRESHOLD = 80;
        private const double HIGH_SOLAR_RATIO = 0.75;
        private const int LOW_BATTERY_THRESHOLD = 30;
        private const int MEDIUM_BATTERY_THRESHOLD = 50;
        private const double VERY_HIGH_SOLAR_RATIO = 0.8;

        #endregion Fields

        #region Private Methods

        /// <summary>
        /// Adjusts power distribution based on various conditions following the documented decision hierarchy.
        /// </summary>
        /// <param name="value">The real-time measurement data from Tibber.</param>
        private async Task TibberRTMAdjustment3(TibberRealTimeMeasurement value)
        {
            // 1. Auto Mode Check (Highest Priority)
            if (ApiSettingAutoMode)
            {
                LoggerRTM.LogInformation("Auto mode active: Using intelligent power adjustment");
                ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 1001, Key = "TibberAdjustment3Mode", Value = "ForceAuto" });
                await TibberRTMAdjustment3AutoMode(value);
                return;
            }

            // Cache common values to avoid redundant calculations
            var effectiveSolarPower = CurrentState.GrowattNoahGetAvgPpvLast5Minutes();
            var batteryLevel = GrowattGetBatteryLevel();
            var isBatteryEmpty = CurrentState.IsGrowattBatteryEmpty;

            // 2. Inactivity Check
            if (effectiveSolarPower <= 0 && isBatteryEmpty)
            {
                LoggerRTM.LogInformation("No PV power available and battery empty. No action required.");
                ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 1001, Key = "TibberAdjustment3Mode", Value = "Inactive" });
                await TibberRTMDefaultBatteryPriorityAsync(value);
                return;
            }

            bool lowBatteryLevel = batteryLevel < LOW_BATTERY_THRESHOLD;
            bool underThresholdBatteryLevel = batteryLevel < HIGH_BATTERY_THRESHOLD;
            bool isLowPrices = CurrentState.IsCheapRestrictionMode || CurrentState.IsBelowAvgPrice;
            bool isGoodWeather = !CurrentState.IsCloudy();
            bool isBadWeather = CurrentState.IsCloudy();
            var isBatteryFull = CurrentState.IsGrowattBatteryFull;
            var isBatteryChargingWindowActive = CurrentState.IsBatteryChargingWindowActive();

            // Special case from original code
            if (CurrentState.IsExpensiveRestrictionMode || !CurrentState.IsBelowAvgPrice)
            {
                LoggerRTM.LogInformation("Normal Mode: Expensive electricity prices, activating energy saving mode");
                await TibberRTMAdjustment3AutoMode(value);
                return;
            }

            // 1. Direct Solar Usage Optimization
            if
            (
                isBatteryFull &&
                CurrentState.GrowattNoahGetAvgPpvLast5Minutes() > ApiSettingAvgPower &&
                !CurrentState.IsCheapRestrictionMode &&
                !CurrentState.IsBelowAvgPrice
            )
            {
                LoggerRTM.LogInformation("Normal Mode: Not in cheap price mode, prioritizing direct solar usage");
                await TibberRTMDefaultLoadPrioritySolarInputAsync(value);
                return;
            }

            // Check all battery charging conditions
            if
            (
                isBatteryFull ||
                isBatteryChargingWindowActive ||
                isLowPrices ||
                (lowBatteryLevel && CurrentState.IsCloudy()) ||
                (underThresholdBatteryLevel && isGoodWeather)
            )
            {
                LoggerRTM.LogInformation(
                    "Normal Mode: BatteryChargingDecisions" +
                    " isBatteryFull({isBatteryFull}," +
                    " isBatteryChargingWindowActive({isBatteryChargingWindowActive})," +
                    " lowBatteryLevel({lowBatteryLevel})" +
                    " isLowPrices({cheapPrices})" +
                    " isBadWeather({badWeather})" +
                    " isGoodWeather({goodWeather})" +
                    " underThresholdBatteryLevel({underThresholdBatteryLevel})",
                    isBatteryFull, isBatteryChargingWindowActive, lowBatteryLevel, isLowPrices, isGoodWeather, isBadWeather, underThresholdBatteryLevel);
                await TibberRTMDefaultBatteryPriorityAsync(value);
                return;
            }

            // Default Fallback - when no other condition applies
            LoggerRTM.LogInformation("Normal Mode: No specific conditions met, using auto mode for optimal distribution");
            await TibberRTMAdjustment3AutoMode(value);
        }

        #endregion Private Methods
    }
}
