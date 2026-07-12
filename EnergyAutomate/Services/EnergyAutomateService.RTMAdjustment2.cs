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

        private async Task TibberRTMAdjustment2(TibberRealTimeMeasurement value)
        {
            if (CurrentState.GrowattNoahTotalPPV < ApiSettingAvgPower)
            {
                if (CurrentState.IsGrowattBatteryEmpty)
                {
                    Logger.LogInformation($"Battery is empty, set power to 0");
                }
                else
                {
                    if (CurrentState.IsExpensiveRestrictionMode || ApiSettingAutoMode)
                    {
                        await TibberRTMAdjustment2PowerSet(value);
                    }
                    else
                    {
                        await TibberRTMDefaultLoadPriorityAvgAsync(value);
                    }
                }
            }
            else if (CurrentState.GrowattNoahTotalPPV > 840)
            {
                if (CurrentState.IsGrowattBatteryFull)
                {
                    Logger.LogInformation($"Battery is full, no action needed");

                    await TibberRTMDefaultBatteryPriorityAsync(value);
                }
                else
                {
                    //If cloudy
                    if (CurrentState.IsCloudy())
                    {
                        await TibberRTMAdjustment2PowerSet(value);
                        ApiSettingAvgPowerHysteresis = 10;
                        ApiSettingAvgPowerOffset = -25;
                    }
                    else
                    {
                        // If the battery is not full and the restriction mode is cheap load with
                        // full soloar power
                        await TibberRTMDefaultLoadPriorityMaxAsync(value);
                    }
                }
            }
            else
            {
                if (CurrentState.IsGrowattBatteryFull)
                {
                    if (CurrentState.IsExpensiveRestrictionMode || ApiSettingAutoMode)
                    {
                        await TibberRTMAdjustment2PowerSet(value);
                    }
                    else
                    {
                        await TibberRTMDefaultBatteryPriorityAsync(value);
                    }
                }
                else
                {
                    //If cloudy
                    if (CurrentState.IsCloudy())
                    {
                        if (CurrentState.IsExpensiveRestrictionMode || ApiSettingAutoMode)
                        {
                            await TibberRTMAdjustment2PowerSet(value);
                        }
                        else
                        {
                            await TibberRTMDefaultBatteryPriorityAsync(value);
                        }
                    }
                    else
                    {
                        if (CurrentState.IsExpensiveRestrictionMode || !CurrentState.IsBelowAvgPrice || ApiSettingAutoMode)
                        {
                            //Battery is not full and it's not not cloudy and expensive restriction mode so we force load
                            await TibberRTMAdjustment2PowerSet(value);
                        }
                        else
                        {
                            // If the price is not expensive and below avg price
                            await TibberRTMDefaultLoadPrioritySolarInputAsync(value);
                        }
                    }
                }
            }
        }

        private async Task TibberRTMAdjustment2PowerSet(TibberRealTimeMeasurement value)
        {
            var devices = GrowattGetDevicesNoahOnline();
            var totalCommited = devices.Sum(x => x.PowerValueCommited);

            int calcPowerValue = Math.Abs(value.TotalPower - ApiSettingAvgPowerOffset);
            int powerValue = Math.Abs(value.TotalPower - ApiSettingAvgPowerOffset);

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
                var avgPowerConsumption = value.PowerAvgConsumption;
                var avgPowerProduction = -value.PowerAvgProduction;

                // If the total power is greater than 0, it indicates power consumption
                if (value.TotalPower > 0 && value.TotalPower > upperlimit)
                {
                    consumptionDelta = Math.Abs(value.TotalPower - ApiSettingAvgPowerOffset);

                    calcPowerValue = device.PowerValueCommited + consumptionDelta / 2;
                }
                // If the total power is less than 0, it indicates power production
                else if (value.TotalPower < 0 && value.TotalPower < lowerlimit)
                {
                    productionDelta = Math.Abs(value.TotalPower - ApiSettingAvgPowerOffset);
                    calcPowerValue = device.PowerValueCommited - productionDelta / 2;
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
            }

            await Task.CompletedTask;
        }

        #endregion Private Methods
    }
}
