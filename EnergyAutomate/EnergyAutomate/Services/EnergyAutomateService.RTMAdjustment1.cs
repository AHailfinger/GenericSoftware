using EnergyAutomate.Definitions;
using EnergyAutomate.Extentions;

namespace EnergyAutomate.Services
{
    /// <summary>
    /// Partial class for handling real-time power adjustments based on Tibber's real-time measurements.
    /// </summary>
    public partial class EnergyAutomateService
    {
        #region Private Methods

        private async Task TibberRTMAdjustment1(TibberRealTimeMeasurement value)
        {
            if (ApiSettingAutoMode && CurrentState.IsGrowattOnline)
            {
                // If the automatic mode is enabled, the power value is adjusted
                if (!CurrentState.IsRTMAutoModeRunning)
                {
                    //Clear all time segments
                    await GrowattClearAllDeviceNoahTimeSegments();

                    Logger.LogTrace("Not in grace periode: Running LoadBalanceRule one time");
                    //If loadbalance is active the battety priority is set
                    await ApiAutoModeEnabledLoadBalanceRule(value);
                }

                // If the automatic mode is enabled
                if (ApiSettingRestrictionMode)
                {
                    value.SettingAutoMode = ApiSettingAutoMode;
                    value.SettingRestrictionState = ApiSettingRestrictionMode;
                    value.SettingBatteryPriorityMode = ApiSettingBatteryPriorityMode;

                    // If the automatic mode is enabled and the restriction is active, the power
                    // value is adjusted
                    if (CurrentState.IsExpensiveRestrictionMode)
                    {
                        if (!CurrentState.IsRTMRestrictionModeRunning)
                        {
                            Logger.LogTrace("Are in grace periode: Running SetNoDeviceNoahTimeSegments one time");

                            //Clear all time segments
                            await GrowattClearAllDeviceNoahTimeSegments();
                        }

                        if (ApiSettingBatteryPriorityMode)
                        {
                            await TibberRTMAdjustment1PowerSet(value);
                        }
                        else
                        {
                            await TibberRTMDefaultBatteryPriorityAsync(value);
                        }

                        CurrentState.IsRTMRestrictionModeRunning = true;
                    }
                    // If the automatic mode is enabled and the restriction is not active, the power
                    // value is set to 0
                    else
                    {
                        if (CurrentState.IsRTMRestrictionModeRunning)
                        {
                            //Clear all querys
                            await GrowattDeviceQueryQueueWatchdog.ClearAsync();

                            Logger.LogTrace("AutoMode enabled, not in grace periode: Running LoadBalanceRule one time");

                            //If loadbalance is active the battety priority is set
                            //await ApiAutoModeDisabledLoadBalanceRule();

                            CurrentState.IsRTMRestrictionModeRunning = false;
                        }

                        Logger.LogTrace("Not in grace periode: Nothing to do");

                        CurrentState.IsRTMRestrictionModeRunning = false;
                    }
                }
                else
                {
                    // If the automatic mode is enabled and the restriction is not active, the power
                    // value is set to 0

                    if (ApiSettingBatteryPriorityMode)
                    {
                        await TibberRTMDefaultBatteryPriorityAsync(value);
                    }
                    else
                    {
                        await TibberRTMAdjustment1PowerSet(value);
                    }
                }
                CurrentState.IsRTMAutoModeRunning = true;
            }
            else
            {
                if (CurrentState.IsGrowattOnline)
                {
                    // If the automatic mode is disabled, the power value is set to 0
                    if (CurrentState.IsRTMAutoModeRunning)
                    {
                        await GrowattDeviceQueryQueueWatchdog.ClearAsync();

                        Logger.LogTrace("AutoMode disabled: Running LoadBalanceRule one time");

                        //If loadbalance is active the battety priority is set
                        await ApiAutoModeDisabledLoadBalanceRule(value);

                        CurrentState.IsRTMAutoModeRunning = false;
                    }
                }
            }
        }

        private async Task TibberRTMAdjustment1PowerSet(TibberRealTimeMeasurement value)
        {
            int calcPowerValue = Math.Abs(value.TotalPower - ApiSettingAvgPowerOffset);
            int powerValue = Math.Abs(value.TotalPower - ApiSettingAvgPowerOffset);

            var devices = GrowattGetDevicesNoahOnline();

            DeviceList? device = null;

            var upperlimit = ApiSettingAvgPowerOffset + (ApiSettingAvgPowerHysteresis / 2);
            var lowerlimit = ApiSettingAvgPowerOffset - (ApiSettingAvgPowerHysteresis / 2);

            int consumptionDelta = Math.Abs(value.TotalPower - ApiSettingAvgPowerOffset);
            int productionDelta = Math.Abs(value.TotalPower - ApiSettingAvgPowerOffset);

            ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 201, Key = "TotalPowerDelta", Value = productionDelta.ToString() });

            if (value.TotalPower > 0)
                device = devices.OrderBy(x => x.PowerValueCommited).FirstOrDefault();

            if (value.TotalPower < 0)
                device = devices.OrderByDescending(x => x.PowerValueCommited).FirstOrDefault();

            if (device != null)
            {
                int lastCommitedPowerValue = device.PowerValueCommited == 0 ? (int)(GrowattGetNoahLastDataPerDevice(device.DeviceSn)?.pac ?? 0) : device.PowerValueCommited;
                var avgPowerConsumption = value.PowerAvgConsumption ?? 0;
                var avgPowerProduction = -value.PowerAvgProduction ?? 0;

                // If the total power is greater than 0, it indicates power consumption
                if (value.TotalPower > 0)
                {
                    // If the average power consumption is greater than the upper limit
                    if (avgPowerConsumption > upperlimit)
                    {
                        // Calculate the difference between the average power consumption and the
                        // upper limit
                        consumptionDelta = Math.Abs(avgPowerConsumption - upperlimit);
                        // Add or update the trace value for the delta power value
                        ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 202, Key = "TotalPowerDelta", Value = consumptionDelta.ToString() });
                        // Calculate the new power value based on the consumption delta additional
                        // half for slower decreasing
                        calcPowerValue = lastCommitedPowerValue + (consumptionDelta / devices.Count);
                    }
                    // If the average power consumption is less than the lower limit
                    else if (avgPowerConsumption < lowerlimit)
                    {
                        // Calculate the difference between the lower limit and the average power consumption
                        consumptionDelta = Math.Abs(lowerlimit - avgPowerConsumption);
                        // Calculate the new power value based on the consumption delta
                        calcPowerValue = lastCommitedPowerValue - (consumptionDelta / devices.Count);
                    }
                }
                // If the total power is less than 0, it indicates power production
                else if (value.TotalPower < 0)
                {
                    // If the average power production is less than the lower limit
                    if (avgPowerProduction < lowerlimit)
                    {
                        // Calculate the difference between the average power production and the
                        // lower limit
                        productionDelta = Math.Abs(avgPowerProduction - lowerlimit);
                        // Add or update the trace value for the delta power value
                        ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 203, Key = "TotalPowerDelta", Value = productionDelta.ToString() });
                        // Calculate the new power value based on the production delta
                        calcPowerValue = lastCommitedPowerValue - (productionDelta / devices.Count);
                    }
                    // If the average power production is greater than the upper limit
                    else if (avgPowerProduction > upperlimit)
                    {
                        // Calculate the difference between the average power production and the
                        // upper limit
                        productionDelta = Math.Abs(avgPowerProduction - upperlimit);
                        // Calculate the new power value based on the production delta
                        calcPowerValue = lastCommitedPowerValue + (productionDelta / devices.Count);
                    }
                }

                var maxPower = ApiSettingMaxPower / devices.Count;

                powerValue = calcPowerValue > maxPower ? maxPower : calcPowerValue < 0 ? 0 : calcPowerValue;

                if (powerValue <= maxPower && powerValue > 0)
                {
                    if (device.PowerValueRequested != powerValue)
                    {
                        var item = new DeviceNoahSetPowerQuery()
                        {
                            DeviceType = "noah",
                            DeviceSn = device.DeviceSn,
                            Value = powerValue,
                            Force = true,
                            TS = value.TS
                        };

                        await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(item);
                        LoggerRTM.LogTrace(messageTemplatePowerSet, "Enqueued", CurrentState.UtcNow, device.DeviceSn, device.PowerValueRequested);

                        ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 204, Key = "TotalPowerRequested", Value = powerValue.ToString() });
                    }
                    else
                    {
                        Logger.LogTrace($"PowerValue {device.DeviceSn} already set to {powerValue}");
                    }
                }
                else
                {
                    if (value.TotalPower > 0)
                    {
                        Logger.LogTrace($"TotalPower: {value.TotalPower}, AvgPowerProduction: {avgPowerConsumption}, upperDelta: {consumptionDelta} = {avgPowerConsumption} - {upperlimit}");
                        Logger.LogTrace($"lastCommitedPowerValue: {lastCommitedPowerValue}, upperDelta: {consumptionDelta} = {avgPowerConsumption} - {upperlimit}, calcPowerValue: {calcPowerValue}, OffSet: {ApiSettingAvgPowerOffset}");
                    }
                    if (value.TotalPower < 0)
                    {
                        Logger.LogTrace($"TotalPower: {value.TotalPower}, AvgPowerProduction: {avgPowerProduction}, lowerDelta: {productionDelta} = {avgPowerProduction} - {lowerlimit}");
                        Logger.LogTrace($"lastCommitedPowerValue: {lastCommitedPowerValue} - lowerDelta: {productionDelta}, calcPowerValue: {calcPowerValue}, OffSet: {ApiSettingAvgPowerOffset}");
                    }
                }

                value.PowerValueNewRequested = powerValue;
                value.PowerValueNewCommited = 0;
#pragma warning disable CS8601
                value.PowerValueNewDeviceSn = device?.DeviceSn;
#pragma warning restore CS8601
            }

            await Task.CompletedTask;
        }

        #endregion Private Methods
    }
}
