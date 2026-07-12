using EnergyAutomate.Definitions;
using EnergyAutomate.Extentions;

namespace EnergyAutomate.Services
{
    /// <summary>
    /// Partial class for handling real-time power adjustments based on Tibber's real-time measurements.
    /// </summary>
    public partial class EnergyAutomateService
    {

        #region Public Methods

        public async Task TibberGetDataFromWeb() => await TibberApiService.TibberGetDataFromWeb();

        public List<TibberPrice> TibberGetPriceDatas() => TibberApiService.TibberGetPriceDatas();

        public async Task TibberGetTomorrowPrices() => await TibberApiService.TibberGetTomorrowPrices();

        public async Task TibberStoreRealTimeMeasurementAsync(TibberRealTimeMeasurement tibberRealTimeMeasurement) => await TibberApiService.TibberStoreRealTimeMeasurementAsync(tibberRealTimeMeasurement);

        public async Task TibberUpdateRealTimeMeasurementAsync(TibberRealTimeMeasurement tibberRealTimeMeasurement) => await TibberApiService.TibberUpdateRealTimeMeasurementAsync(tibberRealTimeMeasurement);

        public List<TibberPrice> TibberListPrices() => TibberApiService.TibberListPrices();

        public List<TibberRealTimeMeasurement> TibberListRealTimeMeasurement() => TibberApiService.TibberListRealTimeMeasurement();

        public void TibberRealTimeMeasurementRegisterOnCollectionChanged(object sender, Action callback) => TibberApiService.TibberRealTimeMeasurementRegisterOnCollectionChanged(sender, callback);

        public void TibberRealTimeMeasurementUnRegisterOnCollectionChanged(object sender) => TibberApiService.TibberRealTimeMeasurementUnRegisterOnCollectionChanged(sender);

        #endregion Public Methods

        #region Private Methods

        private async Task TibberRTMCalculation1(TibberRealTimeMeasurement value)
        {
            lock (TibberRealTimeMeasurement._syncRoot)
            {
                var measurementsQuery = TibberRealTimeMeasurement;

                if (ApiSettingAvgPowerLoadSeconds > 0)
                    measurementsQuery.Where(m => m.TS.UtcDateTime >= CurrentState.UtcNow.AddSeconds(-ApiSettingAvgPowerLoadSeconds));

                var measurements = measurementsQuery.ToList();

                measurements.Add(value);

                var powerConsumptionCompleteMeasurements = measurements.OrderByDescending(m => m.TS).ToList().GetEnumerator();

                List<TibberRealTimeMeasurement> powerConsumptionUntilZero = [];

                while (powerConsumptionCompleteMeasurements.MoveNext())
                {
                    if (powerConsumptionCompleteMeasurements.Current.Power == 0) break;
                    powerConsumptionUntilZero.Add(powerConsumptionCompleteMeasurements.Current);
                }

                value.PowerAvgConsumption = value.Power > 0 ? (int)powerConsumptionUntilZero.Average(m => m.Power) : 0;

                var powerProductionCompleteMeasurements = measurements.OrderByDescending(m => m.TS).ToList().GetEnumerator();

                List<TibberRealTimeMeasurement> powerProductionUntilZero = [];

                while (powerProductionCompleteMeasurements.MoveNext())
                {
                    if (powerProductionCompleteMeasurements.Current.PowerProduction == 0) break;
                    powerProductionUntilZero.Add(powerProductionCompleteMeasurements.Current);
                }

                value.PowerAvgProduction = value.PowerProduction > 0 ? (int)powerProductionUntilZero.Average(m => m.PowerProduction ?? 0) : 0;
            }

            ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 101, Key = "PowerAvgConsumption", Value = value.PowerAvgConsumption.Value.ToString() });
            ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 102, Key = "PowerAvgProduction", Value = value.PowerAvgProduction.Value.ToString() });

            await Task.CompletedTask;
        }

        private async Task TibberRTMCalculation2(TibberRealTimeMeasurement value)
        {
            lock (TibberRealTimeMeasurement._syncRoot)
            {
                var measurementsQuery = TibberRealTimeMeasurement;

                if (ApiSettingAvgPowerLoadSeconds > 0)
                    measurementsQuery.Where(m => m.TS.UtcDateTime >= CurrentState.UtcNow.AddSeconds(-ApiSettingAvgPowerLoadSeconds));

                var measurements = measurementsQuery.ToList();

                measurements.Add(value);

                var powerConsumptionCompleteMeasurements = measurements.OrderByDescending(m => m.TS).ToList().GetEnumerator();

                List<TibberRealTimeMeasurement> powerConsumptionUntilZero = [];

                while (powerConsumptionCompleteMeasurements.MoveNext())
                {
                    if (powerConsumptionCompleteMeasurements.Current.Power == 0) break;
                    powerConsumptionUntilZero.Add(powerConsumptionCompleteMeasurements.Current);
                }

                value.PowerAvgConsumption = value.Power > 0 ? (int)powerConsumptionUntilZero.Average(m => m.Power) : 0;

                var powerProductionCompleteMeasurements = measurements.OrderByDescending(m => m.TS).ToList().GetEnumerator();

                List<TibberRealTimeMeasurement> powerProductionUntilZero = [];

                while (powerProductionCompleteMeasurements.MoveNext())
                {
                    if (powerProductionCompleteMeasurements.Current.PowerProduction == 0) break;
                    powerProductionUntilZero.Add(powerProductionCompleteMeasurements.Current);
                }

                value.PowerAvgProduction = value.PowerProduction > 0 ? (int)powerProductionUntilZero.Average(m => m.PowerProduction ?? 0) : 0;
            }

            ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 101, Key = "PowerAvgConsumption", Value = value.PowerAvgConsumption.Value.ToString() });
            ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 201, Key = "PowerAvgProduction", Value = value.PowerAvgProduction.Value.ToString() });

            await Task.CompletedTask;
        }

        private async Task TibberRTMCheckAdjustmentAsync(string condition, Func<Task> callback)
        {
            if (CurrentState.ActiveRTMAdjustment != condition)
            {
                CurrentState.ActiveRTMAdjustment = condition;
                ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 51, Key = "ActiveRTMAdjustment", Value = condition });
                LoggerRTM.LogTrace("CheckRTMAdjustment {condition}", condition);

                await callback.Invoke();
            }
        }

        private async Task TibberRTMCheckConditionAsync(string condition, List<ApiCondition> apiConditions)
        {
            foreach (var apiCondition in apiConditions)
            {
                if (CurrentState.ActiveRTMCondition != condition)
                {
                    CurrentState.ActiveRTMCondition = condition;
                    ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 51, Key = "ActiveRTMCondition", Value = condition });
                    LoggerRTM.LogTrace("CheckRTMCondition {condition}", condition);

                    if (apiCondition.Callback != null)
                        await apiCondition.Callback.Invoke();
                }
                else
                {
                    if (apiCondition.Validation != null)
                    {
                        var result = await apiCondition.Validation.Invoke();
                        if (result)
                        {
                            LoggerRTM.LogTrace("CheckRTMCondition {condition} validation success", condition);
                        }
                        else
                        {
                            LoggerRTM.LogTrace("CheckRTMCondition {condition} validation failed, set values again !", condition);
                            if (apiCondition.Callback != null)
                                await apiCondition.Callback.Invoke();
                        }
                    }
                }
            }
        }

        private async Task TibberRTMDefaultBatteryPriorityAsync(TibberRealTimeMeasurement value)
        {
            await TibberRTMCheckConditionAsync("BatteryPriority_SetPower_0", [
                new(
                    async () => {
                        await GrowattQueryBattPriorityDeviceNoahTimeSegmentsAsync();
                    },
                    async () => {
                        // Check if any time segments are enabled
                        var anyTimesegmentEnabled = GrowattLatestNoahInfoDatas()
                            .All(n => n.TimeSegments.Any(t => t.Equals(GrowattQueryDefaultBattPriorityDeviceNoahTimeSegment(n.DeviceSn, false))));
                        var allOtherTimesegmentsDisabled = GrowattLatestNoahInfoDatas()
                            .All(n => n.TimeSegments.Where(t => !t.Equals(GrowattQueryDefaultBattPriorityDeviceNoahTimeSegment(n.DeviceSn, false))).All(y => y.Enable == "0"));

                        LoggerRTM.LogTrace("allOtherTimesegmentsDisabled: {allOtherTimesegmentsDisabled}, anyTimesegmentEnabled: {anyTimesegmentEnabled}",
                            allOtherTimesegmentsDisabled, anyTimesegmentEnabled);

                        return await Task.FromResult(anyTimesegmentEnabled && allOtherTimesegmentsDisabled);
                    }
                ),
                new(
                    async () => {
                        await GrowattClearSetPowerAsync(value.TS, 0);
                    },
                    async () => {
                        // Check if any device are online
                        if(!GrowattGetDevicesNoahOnline().Any()) return true;

                        // Check if power default and commited values are equal to avg
                        var allDevicesConformCommited = GrowattGetDevicesNoahOnline().All(x => x.PowerValueCommited == 0);
                        var allDevicesConformDefault = GrowattGetDevicesNoahOnline().All(x => x.PowerValueDefault == 0);

                        LoggerRTM.LogTrace("allDevicesConformCommited: {allDevicesConformCommited}; allDevicesConformDefault: {allDevicesConformDefault};", allDevicesConformCommited, allDevicesConformDefault );

                        return await Task.FromResult( allDevicesConformCommited && allDevicesConformDefault);
                    }
                )
            ]);
        }

        private async Task TibberRTMDefaultLoadPriorityAvgAsync(TibberRealTimeMeasurement value)
        {
            // If the battery is not empty and the restriction mode is not expensive activate avg injection
            await TibberRTMCheckConditionAsync($"LoadPriority_SetPower_Avg_{ApiSettingAvgPower}", [
                new (
                    async () =>
                    {
                        await GrowattClearAllDeviceNoahTimeSegments();
                    },
                    async () =>
                    {
                        // Check if any time segments are enabled
                        var allTimesegmentsDisabled = GrowattLatestNoahInfoDatas().All(x => x!.TimeSegments.All(x => x.Enable == "0"));

                        LoggerRTM.LogTrace("allTimesegmentsDisabled: {allTimesegmentsDisabled}",
                            allTimesegmentsDisabled);

                        return await Task.FromResult(allTimesegmentsDisabled);
                    }
                ),
                new (
                    async () =>
                    {
                        await GrowattClearSetPowerAsync(value.TS, ApiSettingAvgPower);
                    },
                    async () =>
                    {
                        var avgPerDevice = ApiSettingAvgPower / GrowattGetDevicesNoahOnline().Count;

                        // Check if power default and commited values are equal to avg
                        var allDevicesConform = GrowattGetDevicesNoahOnline().All(x => x.PowerValueCommited == avgPerDevice|| x.PowerValueDefault == avgPerDevice);

                        LoggerRTM.LogTrace(" allDevicesConform: {allDevicesConform}",
                            allDevicesConform);

                        return await Task.FromResult(allDevicesConform);
                    }
                )
            ]);
        }

        private async Task TibberRTMDefaultLoadPriorityMaxAsync(TibberRealTimeMeasurement value, string? deviceSn = null)
        {
            deviceSn ??= GrowattGetDeviceNoahSnList();
            await TibberRTMCheckConditionAsync($"LoadPriority_SetPower_Max_{ApiSettingMaxPower}", [
                new (
                    async () => {
                        value.PowerValueNewRequested = ApiSettingMaxPower;
                        value.PowerValueNewDeviceSn = deviceSn;

                        await Task.CompletedTask;
                    }, null
                ),
                new (
                    async () => {
                        await GrowattClearAllDeviceNoahTimeSegments();
                    }, async () => {
                        // Check if any time segments are enabled
                        var anyEnabledTimesegments = GrowattLatestNoahInfoDatas().Any(x => x!.TimeSegments.Any(x => x.Enable == "1"));

                        LoggerRTM.LogTrace("anyEnabledTimesegments: {anyEnabledTimesegments}",
                            anyEnabledTimesegments);

                        return await Task.FromResult(!anyEnabledTimesegments);
                    }
                ),
                new (
                    async () => {
                        await GrowattClearSetPowerAsync(value.TS, ApiSettingMaxPower);
                    }, async () => {
                        var maxPerDevice = ApiSettingMaxPower / GrowattGetDevicesNoahOnline().Count;

                        // Check if power default and commited values are equal to avg
                        var allDevicesConform = GrowattGetDevicesNoahOnline().All(x => x.PowerValueCommited == maxPerDevice || x.PowerValueDefault == maxPerDevice);

                        LoggerRTM.LogTrace("allDevicesConform: {allDevicesConform}",
                            allDevicesConform);

                        return await Task.FromResult(allDevicesConform);
                    }
                )
            ]);
        }

        private async Task TibberRTMDefaultLoadPrioritySolarInputAsync(TibberRealTimeMeasurement value, int reduction = 0)
        {
            await TibberRTMCheckConditionAsync("BatteryPriority_SetPower_SolarInput", [
                new (
                    async () =>
                    {
                        await GrowattClearAllDeviceNoahTimeSegments();
                    },
                    async () =>
                    {
                        // Check if any time segments are enabled
                        var allTimesegmentsDisabled = GrowattLatestNoahInfoDatas().All(x => x!.TimeSegments.All(x => x.Enable == "0"));

                        LoggerRTM.LogTrace("allTimesegmentsDisabled: {allTimesegmentsDisabled}",
                            allTimesegmentsDisabled);

                        return await Task.FromResult(allTimesegmentsDisabled);
                    }
                )
            ]);

            Queue<IDeviceQuery> queue = [];

            var devices = GrowattDevices.Where(x => x.DeviceType == "noah").ToList();

            var totalPPV = 0;
            foreach (var device in devices)
            {
                var infoData = GrowattGetNoahInfoDataPerDevice(device.DeviceSn);
                var lastData = GrowattGetNoahLastDataPerDevice(device.DeviceSn);
                var powerValue = (int)(lastData?.ppv - reduction ?? 0);

                totalPPV += powerValue;
                var item = new DeviceNoahSetPowerQuery()
                {
                    DeviceType = "noah",
                    DeviceSn = device.DeviceSn,
                    Value = powerValue,
                    Force = true,
                    TS = value.TS
                };

                queue.Enqueue(item);
            }

            await TibberRTMCheckAdjustmentAsync($"LoadPriority_SolarInput_{totalPPV}", async () =>
            {
                await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(queue);
                ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 204, Key = "TotalPowerRequested", Value = totalPPV.ToString() });

                await Task.CompletedTask;
            });
        }

        private async Task TibberSavePricesAsync(IList<TibberPrice> prices) => await TibberApiService.TibberSavePricesAsync(prices);

        #endregion Private Methods
    }
}
