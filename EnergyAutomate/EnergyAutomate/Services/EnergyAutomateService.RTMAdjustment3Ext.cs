namespace EnergyAutomate.Services
{
    /// <summary>
    /// Partial class for handling real-time power adjustments based on Tibber's real-time measurements.
    /// </summary>
    public partial class EnergyAutomateService
    {
        #region Private Methods

        /// <summary>
        /// Handles automatic power adjustment when in auto mode or under expensive restriction mode.
        /// </summary>
        /// <param name="value">The real-time measurement data from Tibber.</param>
        private async Task TibberRTMAdjustment3AutoMode(TibberRealTimeMeasurement value)
        {
            // Check and clear conditions for load priority adjustment.
            await TibberRTMCheckConditionAsync("LoadPriority_SetPower", [ new( async () =>
                    {
                        // Clear all time segments for Noah devices.
                        await GrowattClearAllDeviceNoahTimeSegments();
                    }, () =>
                    {
                        // Check if any time segments are enabled.
                        var anyEnabledTimesegments = GrowattLatestNoahInfoDatas().Any(x => x!.TimeSegments.Any(x => x.Enable == "1"));
                        return Task.FromResult(!anyEnabledTimesegments);
                    })
            ]);

            // Adjust power distribution.
            await TibberRTMAdjustment3SetPower(value);
        }

        /// <summary>Checks if tomorrow morning has forecasted high prices</summary>
        private bool TibberRTMAdjustment3CheckTomorrowMorningPriceForecast()
        {
            try
            {
                var tomorrowPrices = TibberGetPriceDatas().Skip(24).Take(24).ToList(); ;

                var morningPrices = tomorrowPrices
                    .Where(p => p.StartsAt.Hour >= 6 && p.StartsAt.Hour <= 12)
                    .ToList();

                // Check if any of the morning prices are in the expensive range
                return morningPrices?.Any(p => (int)(p.Level ?? PriceLevel.Normal) > 2) ?? false;
            }
            catch (Exception ex)
            {
                LoggerRTM.LogError($"Error checking tomorrow's price forecast: {ex.Message}");
                // Default to false if we can't get forecast data
                return false;
            }
        }

        /// <summary>Checks if tomorrow has forecasted poor weather (cloudy/rainy)</summary>
        private bool TibberRTMAdjustment3CheckTomorrowWeatherForecast()
        {
            try
            {
                // Check if weather conditions indicate poor solar production
                return CurrentState.IsCloudy(CurrentState.WeatherForecastTomorrow);
            }
            catch (Exception ex)
            {
                LoggerRTM.LogError($"Error checking tomorrow's weather forecast: {ex.Message}");
                // Default to false if we can't get forecast data
                return false;
            }
        }

        /// <summary>Determines if extension mode is currently active</summary>
        private bool TibberRTMAdjustment3IsExtensionModeActive()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            return ApiSettingExtentionMode && (currentTime < ApiSettingExtentionExclusionFrom ||
                                              currentTime > ApiSettingExtentionExclusionUntil);
        }

        /// <summary>Checks if we're approaching morning hours (1-5 AM)</summary>
        private bool TibberRTMAdjustment3IsMorningApproaching()
        {
            var currentHour = DateTime.Now.Hour;
            return currentHour >= 1 && currentHour <= 5;
        }

        /// <summary>
        /// Adjusts power distribution based on real-time measurements, ensuring power limits and
        /// hysteresis are respected.
        /// </summary>
        /// <param name="value">The real-time measurement data from Tibber.</param>
        private async Task TibberRTMAdjustment3SetPower(TibberRealTimeMeasurement value)
        {
            // Log the start of the power adjustment process.
            LoggerRTM.LogTrace("Starting TibberRTMAdjustment3SetPower. TotalPower: {TotalPower}, ApiSettingAvgPower: {TargetPower}, ApiSettingAvgPowerOffset: {TargetOffset}, ApiSettingMaxPower: {MaxPower}",
                value.TotalPower, ApiSettingAvgPower, ApiSettingAvgPowerOffset, ApiSettingMaxPower);

            // Handle wait cycles for power adjustment.
            if (_adjustmentWaitCycles < ApiSettingPowerAdjustmentWaitCycles)
            {
                _adjustmentWaitCycles++;
                LoggerRTM.LogTrace("Wait because WaitCycles: {WaitCycles} < {PowerAdjustmentWaitCycles}",
                    _adjustmentWaitCycles, ApiSettingPowerAdjustmentWaitCycles);
                return;
            }

            // Reset wait cycles after the required number of cycles.
            _adjustmentWaitCycles = 0;
            LoggerRTM.LogTrace("Reset adjustment wait cycles to 0.");

            // Get the committed power value, ensuring it's non-negative.
            var powerValueTotalCommited = Math.Max(0, CurrentState?.PowerValueTotalCommited ?? 0);

            // Calculate the delta and limits for power adjustment.
            int deltaTotalPower = value.TotalPower - ApiSettingAvgPowerOffset;
            var upperLimit = ApiSettingAvgPowerOffset + (ApiSettingAvgPowerHysteresis / 2);
            var lowerLimit = ApiSettingAvgPowerOffset - (ApiSettingAvgPowerHysteresis / 2);

            LoggerRTM.LogTrace("Calculated limits. DeltaTotalPower: {DeltaTotalPower}, UpperLimit: {UpperLimit}, LowerLimit: {LowerLimit}, CommitedTotalPower: {CommitedTotalPower}",
                deltaTotalPower, upperLimit, lowerLimit, powerValueTotalCommited);

            // Get the list of online devices.
            var onlineDevices = GrowattGetDevicesNoahOnline();
            if (onlineDevices == null || !onlineDevices.Any())
            {
                LoggerRTM.LogTrace("No devices available for adjustment.");
                return;
            }

            LoggerRTM.LogTrace("Found {DeviceCount} online devices", onlineDevices.Count);
            int maxPowerPerDevice = onlineDevices.Count > 0 ? ApiSettingMaxPower / onlineDevices.Count : 0;

            // Handle cases where the total power exceeds the maximum allowed power.
            if (Math.Abs(value.TotalPower) > ApiSettingMaxPower)
            {
                if (value.TotalPower < 0)
                {
                    // If the total power is negative and exceeds the limit, set all devices to 0.
                    LoggerRTM.LogTrace("Negative TotalPower ({TotalPower}) exceeds ApiSettingMaxPower ({MaxPower}). Setting all device power values to 0 W.",
                        value.TotalPower, ApiSettingMaxPower);

                    await DistributionManager.SetAllDevicesToPower(onlineDevices, 0, value.TS);
                    return;
                }
                if (value.TotalPower > 0)
                {
                    // If the total power is positive and exceeds the limit, distribute power evenly.
                    LoggerRTM.LogTrace("Positive TotalPower ({TotalPower}) exceeds ApiSettingMaxPower ({MaxPower}). Setting all device power values to {PowerPerDevice} W.",
                    value.TotalPower, ApiSettingMaxPower, maxPowerPerDevice);

                    await DistributionManager.SetAllDevicesToPower(onlineDevices, maxPowerPerDevice, value.TS);
                    return;
                }
            }

            // Handle cases where the total power is within the hysteresis range.
            if (value.TotalPower >= lowerLimit && value.TotalPower <= upperLimit)
            {
                LoggerRTM.LogTrace("TotalPower within hysteresis range ({Lower} to {Upper}). Performing load balancing.",
                    lowerLimit, upperLimit);
                await DistributionManager.PerformLoadBalancing(onlineDevices, value.TS);
                return;
            }

            // Adjust power based on the delta value.
            double adjustmentFactor = ApiSettingPowerAdjustmentFactor / 100.0;
            int adjustedDelta = (int)(deltaTotalPower * adjustmentFactor);
            LoggerRTM.LogTrace("Adjusted delta (after adjustment factor {Factor}%): {AdjustedDelta}",
                ApiSettingPowerAdjustmentFactor, adjustedDelta);

            if (deltaTotalPower > 0)
            {
                // If the delta is positive, increase the total power.
                int desiredTotalPower = Math.Min(ApiSettingMaxPower, powerValueTotalCommited + adjustedDelta);

                LoggerRTM.LogTrace("Positive delta detected. Change total power from {CurrentPower} to {DesiredPower} based on adjusted delta {AdjustedDelta}\"",
                    powerValueTotalCommited, desiredTotalPower, adjustedDelta);

                LoggerRTM.LogTrace("Distributing total power of {TotalPower}W with high SoC prioritization", desiredTotalPower);
                await DistributionManager.HandleDistributionAsync(onlineDevices, desiredTotalPower, prioritizeHighSoc: true, value.TS);
            }
            else if (deltaTotalPower < 0)
            {
                // If the delta is negative, decrease the total power.
                int desiredTotalPower = Math.Max(0, powerValueTotalCommited + adjustedDelta);

                LoggerRTM.LogTrace("Negative delta detected. Change total power from {CurrentPower} to {DesiredPower} based on adjusted delta {AdjustedDelta}",
                    powerValueTotalCommited, desiredTotalPower, adjustedDelta);

                LoggerRTM.LogTrace("Distributing total power of {TotalPower}W with low SoC prioritization", desiredTotalPower);
                await DistributionManager.HandleDistributionAsync(onlineDevices, desiredTotalPower, prioritizeHighSoc: false, value.TS);
            }
            else
            {
                // If the delta is zero, maintain the current power or perform load balancing.
                LoggerRTM.LogTrace("Delta is exactly 0. Performing load balancing.");
                await DistributionManager.PerformLoadBalancing(onlineDevices, value.TS);
            }

            LoggerRTM.LogTrace("Power adjustment procedure completed.");
        }

        /// <summary>
        /// Determines if battery should be preserved for morning based on forecasted data
        /// </summary>
        private bool TibberRTMAdjustment3ShouldPreserveBatteryForMorning()
        {
            // Access tomorrow's price forecast
            bool highPricesForecastTomorrow = TibberRTMAdjustment3CheckTomorrowMorningPriceForecast();

            // Access tomorrow's weather forecast
            bool poorWeatherForecastTomorrow = TibberRTMAdjustment3CheckTomorrowWeatherForecast();

            LoggerRTM.LogInformation("Morning forecast check: High prices tomorrow: {0}, Poor weather tomorrow: {1}",
                highPricesForecastTomorrow, poorWeatherForecastTomorrow);

            return highPricesForecastTomorrow || poorWeatherForecastTomorrow;
        }

        #endregion Private Methods
    }
}
