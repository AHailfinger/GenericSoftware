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

        private GrowattApiService GrowattApiService => ServiceProvider.GetRequiredService<GrowattApiService>();

        public List<DeviceList> GrowattAllNoahDevices() => GrowattApiService.GrowattAllNoahDevices();

        public List<DeviceList> GrowattGetDeviceLists() => GrowattApiService.GrowattGetDeviceLists();

        public List<DeviceList> GrowattGetDevicesNoahOnline() => GrowattApiService.GrowattGetDevicesNoahOnline();

        public DeviceNoahInfoData? GrowattGetNoahInfoDataPerDevice(string? deviceSn) => GrowattApiService.GrowattGetNoahInfoDataPerDevice(deviceSn);

        public List<DeviceNoahInfoData> GrowattGetNoahInfoDatas() => GrowattApiService.GrowattGetNoahInfoDatas();

        public DeviceNoahLastData? GrowattGetNoahLastDataPerDevice(string? deviceSn) => GrowattApiService.GrowattGetNoahLastDataPerDevice(deviceSn);

        public List<DeviceNoahLastData> GrowattGetNoahLastDatas() => GrowattApiService.GrowattGetNoahLastDatas();

        public async Task GrowattInverterMaxSetPower(int value) => await GrowattApiService.GrowattInverterMaxSetPower(value);

        public async Task GrowattInvokeBattPriorityDeviceNoah() => await GrowattApiService.GrowattInvokeBattPriorityDeviceNoah();

        public async Task GrowattInvokeClearDeviceNoahTimeSegments() => await GrowattApiService.GrowattInvokeClearDeviceNoahTimeSegments();

        public async Task GrowattInvokeLoadPriorityDeviceNoah() => await GrowattApiService.GrowattInvokeLoadPriorityDeviceNoah();

        public async Task GrowattInvokeRefreshDeviceList() => await GrowattApiService.GrowattInvokeRefreshDeviceList();

        public async Task GrowattInvokeRefreshNoahs() => await GrowattApiService.GrowattInvokeRefreshNoahs();

        public async Task GrowattInvokeRefreshNoahsLastData() => await GrowattApiService.GrowattInvokeRefreshNoahsLastData();

        public List<DeviceNoahInfoData> GrowattLatestNoahInfoDatas() => GrowattApiService.GrowattLatestNoahInfoDatas();

        public List<DeviceNoahLastData> GrowattLatestNoahLastDatas() => GrowattApiService.GrowattLatestNoahLastDatas();

        public async Task GrowattQueryDevice(bool force = false) => await GrowattApiService.GrowattQueryDevice(force);

        public async Task GrowattQueryDeviceMinInfo(bool force = false) => await GrowattApiService.GrowattQueryDeviceMinInfo(force);

        public async Task GrowattQueryDeviceMinLastData(bool force = false) => await GrowattApiService.GrowattQueryDeviceMinLastData(force);

        public async Task GrowattQueryDeviceNoahInfo(bool force = false) => await GrowattApiService.GrowattQueryDeviceNoahInfo(force);

        public async Task GrowattQueryDeviceNoahLastData(bool force = false) => await GrowattApiService.GrowattQueryDeviceNoahLastData(force);

        #endregion Public Methods

        #region Private Methods

        private async Task GrowattClearAllDeviceNoahTimeSegments()
        {
            Queue<IDeviceQuery> DeviceTimeSegmentQueue = new();

            await GrowattQueryClearDeviceNoahTimeSegments(DeviceTimeSegmentQueue, GrowattGetDevicesNoahOnline());

            await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(DeviceTimeSegmentQueue);
        }

        private async Task GrowattClearSetPowerAsync(DateTimeOffset ts, int powerValue = 0)
        {
            await GrowattDeviceQueryQueueWatchdog.ClearAsync();

            var devices = GrowattGetDevicesNoahOnline();
            if (devices.Count == 0)
            {
                Logger.LogTrace("No online Noah devices available for setting power");
                return;
            }

            var powerValuePerDevice = powerValue / devices.Count;

            foreach (var device in devices)
            {
                var item = new DeviceNoahSetPowerQuery()
                {
                    DeviceType = "noah",
                    DeviceSn = device.DeviceSn,
                    Value = powerValuePerDevice,
                    Force = true,
                    TS = ts
                };

                await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(item);
                LoggerRTM.LogTrace(messageTemplatePowerSet, "Enqueued", CurrentState.UtcNow, device.DeviceSn, device.PowerValueRequested);

                ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 204, Key = "TotalPowerRequested", Value = powerValue.ToString() });
            }

            await Task.CompletedTask;
        }

        private async Task<ApiException?> GrowattDeviceQueryQueueWatchdog_OnItemDequeued(IDeviceQuery item, GrowattApiClient growattApiClient, ILogger logger)
        {
            if (item == null)
                return default;

            var dbContext = ApiGetDbContext();

            TibberRealTimeMeasurement? dataRealTimeMeasurementApiService = null;
            TibberRealTimeMeasurement? dataRealTimeMeasurementDbContext = null;
            DeviceList? device = null;

            if
            (item is DeviceListQuery ||
                !string.IsNullOrWhiteSpace(item.DeviceType) && !string.IsNullOrWhiteSpace(item.DeviceSn)
            )
            {
                switch (item)
                {
                    case DeviceNoahSetLowLimitSocQuery setLowLimitSoc:

                        var apiExceptionSetLowLimitSco = await ExecuteWithExceptionHandlingAsync(item, device, async () =>
                        {
                            await GrowattApiClient.ExecuteDeviceQueryAsync(item);
                        });

                        if (apiExceptionSetLowLimitSco != null)
                            return apiExceptionSetLowLimitSco;

                        logger.LogTrace($"Device {setLowLimitSoc.DeviceType} {setLowLimitSoc.DeviceSn} set LowLimitSoc to: {setLowLimitSoc.Value} W");

                        ApiInvokeStateHasChanged();

                        return apiExceptionSetLowLimitSco;
                    case DeviceNoahSetPowerQuery setPowerQuery:

                        device = GrowattDevices.FirstOrDefault(x => x.DeviceSn == setPowerQuery.DeviceSn);
                        if (setPowerQuery.TS.HasValue)
                        {
                            lock (TibberRealTimeMeasurement._syncRoot)
                                dataRealTimeMeasurementApiService = TibberRealTimeMeasurement.FirstOrDefault(x => x.TS == setPowerQuery.TS);

                            dataRealTimeMeasurementDbContext = dbContext.TibberRealTimeMeasurements.FirstOrDefault(x => x.TS == setPowerQuery.TS);
                        }

                        var apiExceptionSetPower = await ExecuteWithExceptionHandlingAsync(item, device, async () =>
                        {
                            if (device != null)
                            {
                                LoggerRTM.LogTrace(messageTemplatePowerSet, "Requested", CurrentState.UtcNow, device.DeviceSn, setPowerQuery.Value);

                                lock (GrowattDevices._syncRoot)
                                {
                                    device.PowerValueRequested = setPowerQuery.Value;
                                }

                                await growattApiClient.ExecuteDeviceQueryAsync(item);

                                LoggerRTM.LogTrace(messageTemplatePowerSet, "Commited", CurrentState.UtcNow, device.DeviceSn, setPowerQuery.Value);

                                lock (GrowattDevices._syncRoot)
                                {
                                    device.PowerValueLastChanged = CurrentState.UtcNow;
                                    device.PowerValueCommited = setPowerQuery.Value;
                                }
                            }
                        });

                        if (apiExceptionSetPower != null)
                            return apiExceptionSetPower;

                        if (dataRealTimeMeasurementApiService != null)
                        {
                            lock (TibberRealTimeMeasurement._syncRoot)
                            {
                                dataRealTimeMeasurementApiService.PowerValueNewCommited += setPowerQuery.Value;
                            }
                        }

                        if (dataRealTimeMeasurementDbContext != null)
                        {
                            dataRealTimeMeasurementDbContext.PowerValueNewCommited += setPowerQuery.Value;
                            await dbContext.SaveChangesAsync();
                        }

                        ApiInvokeStateHasChanged();

                        return apiExceptionSetPower;
                    case DeviceNoahSetTimeSegmentQuery timeSegmentQuery:
                        var apiExceptionTimeSegment = await ExecuteWithExceptionHandlingAsync(item, device, async () =>
                        {
                            await growattApiClient.ExecuteDeviceQueryAsync(item);
                        });

                        if (apiExceptionTimeSegment != null)
                            return apiExceptionTimeSegment;

                        GrowattGetNoahInfoDataPerDevice(timeSegmentQuery.DeviceSn)?.SetTimeSegment(timeSegmentQuery);

                        ApiInvokeStateHasChanged();

                        return apiExceptionTimeSegment;

                    case DeviceNoahLastDataQuery lastDataQuery:
                        return await ExecuteWithExceptionHandlingAsync(item, device, async () =>
                        {
                            //Refresh Last data ever minute
                            var deviceNoahLastDatas = await growattApiClient.GetDeviceLastDataAsync<DeviceNoahLastDataResponse>(lastDataQuery);
                            if (deviceNoahLastDatas?.Data?.Noah != null)
                            {
                                await GrowattSaveDeviceNoahLastData(deviceNoahLastDatas.Data.Noah);
                            }
                        });
                    case DeviceMinLastDataQuery lastDataQuery:
                        return await ExecuteWithExceptionHandlingAsync(item, device, async () =>
                        {
                            //Refresh Last data ever minute
                            var deviceNoahLastDatas = await growattApiClient.GetDeviceLastDataAsync<DeviceMinLastDataResponse>(lastDataQuery);
                            if (deviceNoahLastDatas?.Data?.Min != null)
                            {
                                await GrowattSaveDeviceMinLastData(deviceNoahLastDatas.Data.Min);
                            }
                        });

                    case DeviceNoahInfoDataQuery infoQuery:
                        return await ExecuteWithExceptionHandlingAsync(item, device, async () =>
                        {
                            //Refresh Last data ever minute
                            var deviceNoahInfos = await growattApiClient.GetDeviceInfoAsync<DeviceNoahInfoDataResponse>(infoQuery);
                            if (deviceNoahInfos?.Data?.Noah != null)
                            {
                                await GrowattSaveDeviceNoahInfoData(deviceNoahInfos.Data.Noah);
                            }
                        });
                    case DeviceMinInfoDataQuery infoQuery:
                        return await ExecuteWithExceptionHandlingAsync(item, device, async () =>
                        {
                            //Refresh Last data ever minute
                            var deviceNoahInfos = await growattApiClient.GetDeviceInfoAsync<DeviceMinInfoDataResponse>(infoQuery);
                            if (deviceNoahInfos?.Data?.Min != null)
                            {
                                await GrowattSaveDeviceMinInfoData(deviceNoahInfos.Data.Min);
                            }
                        });

                    case DeviceListQuery infoQuery:
                        List<DeviceList>? deviceLists = null;
                        try
                        {
                            deviceLists = await GrowattApiClient.GetDeviceListAsync();
                            if (deviceLists != null)
                            {
                                await GrowattSaveDeviceList(deviceLists);
                            }
                            return default; // Operation erfolgreich
                        }
                        catch (ApiException ex)
                        {
                            return ex; // Operation fehlgeschlagen
                        }
                    default:
                        return default;
                }
            }

            return default;
        }

        private string GrowattGetDeviceMinSnList()
        {
            return string.Join(",", GrowattDevices.Where(x => x.DeviceType == "min").Select(x => x.DeviceSn).ToList());
        }

        private string GrowattGetDeviceNoahSnList()
        {
            return string.Join(",", GrowattDevices.Where(x => x.DeviceType == "noah").Select(x => x.DeviceSn).ToList());
        }

        private async Task GrowattQueryBattPriorityDeviceNoahTimeSegmentsAsync()
        {
            Queue<IDeviceQuery> DeviceTimeSegmentQueue = new();

            var deviceLists = GrowattGetDevicesNoahOnline();

            var deviceSnList = deviceLists.Select(x => x.DeviceSn).ToList();

            foreach (var deviceSn in deviceSnList)
            {
                DeviceTimeSegmentQueue.Enqueue(GrowattQueryDefaultBattPriorityDeviceNoahTimeSegment(deviceSn));
            }

            await GrowattQueryClearDeviceNoahTimeSegments(DeviceTimeSegmentQueue, deviceLists, 1);

            await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(DeviceTimeSegmentQueue);
        }

        private async Task GrowattQueryClearDeviceNoahTimeSegments(Queue<IDeviceQuery> DeviceTimeSegmentQueue, List<DeviceList> deviceLists, int skip = 0)
        {
            var deviceSnList = deviceLists.Select(x => x.DeviceSn).ToList();

            foreach (var deviceSn in deviceSnList)
            {
                var data = GrowattDeviceNoahInfoData.FirstOrDefault(x => x.DeviceSn == deviceSn);
                if (data != null)
                {
                    var enabledSegments = data.TimeSegments.Where(x => x.Enable == "1").ToList();

                    int index = 1;

                    foreach (var segment in enabledSegments)
                    {
                        if (index > skip)
                        {
                            var request = new DeviceNoahSetTimeSegmentQuery
                            {
                                Force = true,
                                DeviceSn = deviceSn,
                                DeviceType = "noah",
                                Type = segment.Type,
                                StartTime = "00:00",
                                EndTime = "23:59",
                                Mode = "0",
                                Power = "0",
                                Enable = "0"
                            };

                            DeviceTimeSegmentQueue.Enqueue(request);
                        }
                        index++;
                    }
                }
            }

            await Task.CompletedTask;
        }

        private DeviceNoahSetTimeSegmentQuery GrowattQueryDefaultBattPriorityDeviceNoahTimeSegment(string? deviceSn, bool force = true) => new()
        {
            Force = force,
            DeviceSn = deviceSn,
            DeviceType = "noah",
            Type = "1",
            StartTime = "00:00",
            EndTime = "23:59",
            Mode = "1",
            Power = "0",
            Enable = "1"
        };

        private async Task GrowattQueryLoadPriorityDeviceNoahTimeSegmentsAsync(int powerValue = 0)
        {
            Queue<IDeviceQuery> DeviceTimeSegmentQueue = new();

            var deviceLists = GrowattGetDevicesNoahOnline();
            if (deviceLists.Count == 0)
            {
                Logger.LogTrace("No online Noah devices available for load priority time segments");
                return;
            }

            await GrowattQueryClearDeviceNoahTimeSegments(DeviceTimeSegmentQueue, deviceLists);

            var deviceSnList = deviceLists.Select(x => x.DeviceSn).ToList();

            var powerValuePerDevice = powerValue / deviceSnList.Count;

            foreach (var deviceSn in deviceSnList)
            {
                DeviceTimeSegmentQueue.Enqueue(new DeviceNoahSetTimeSegmentQuery()
                {
                    Force = true,
                    DeviceSn = deviceSn,
                    DeviceType = "noah",
                    Type = "1",
                    StartTime = "08:00",
                    EndTime = "23:59",
                    Mode = "0",
                    Power = powerValuePerDevice.ToString(),
                    Enable = "1"
                });
            }

            await GrowattDeviceQueryQueueWatchdog.EnqueueAsync(DeviceTimeSegmentQueue);
        }

        private async Task GrowattSaveDeviceList(List<DeviceList> deviceLists)
        {
            var dbContext = ApiGetDbContext();

            foreach (var deviceList in deviceLists)
            {
                lock (GrowattDevices._syncRoot)
                {
                    var apiServiceDeviceNoah = GrowattDevices.FirstOrDefault(x => x.DeviceSn == deviceList.DeviceSn);
                    if (apiServiceDeviceNoah == null)
                        GrowattDevices.Add(deviceList);
                }

                var existingDevice = await dbContext.GrowattDevices.FindAsync(deviceList.DeviceSn);
                if (existingDevice != null)
                {
                    dbContext.Entry(existingDevice).CurrentValues.SetValues(deviceList);
                }
                else
                {
                    dbContext.GrowattDevices.Add(deviceList);
                }
            }

            await dbContext.SaveChangesAsync();

            ApiInvokeStateHasChanged();
        }

        private async Task GrowattSaveDeviceMinInfoData(List<DeviceMinInfoData> deviceMinInfos)
        {
            var dbContext = ApiGetDbContext();

            foreach (var deviceMinInfo in deviceMinInfos)
            {
                var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(deviceMinInfo.lastUpdateTime).DateTime;
                var offset = TimeSpan.FromHours(-6); // Beispiel: Offset von 2 Stunden
                deviceMinInfo.TS = new DateTimeOffset(dateTime, offset).ToUniversalTime();

                lock (GrowattDeviceMinInfoData._syncRoot)
                {
                    var apiServiceDeviceMinInfo = GrowattDeviceMinInfoData.FirstOrDefault(x => x.serialNum == deviceMinInfo.serialNum);
                    if (apiServiceDeviceMinInfo != null) GrowattDeviceMinInfoData.Remove(apiServiceDeviceMinInfo);
                    GrowattDeviceMinInfoData.Add(deviceMinInfo);
                }

                var dbContextDeviceMinInfo = await dbContext.GrowattDeviceMinInfoData.FindAsync(deviceMinInfo.serialNum);
                if (dbContextDeviceMinInfo != null)
                {
                    dbContext.Entry(dbContextDeviceMinInfo).CurrentValues.SetValues(deviceMinInfo);
                }
                else
                {
                    dbContext.GrowattDeviceMinInfoData.Add(deviceMinInfo);
                }
            }

            // Save changes to the database
            await dbContext.SaveChangesAsync();

            ApiInvokeStateHasChanged();
        }

        private async Task GrowattSaveDeviceMinLastData(List<DeviceMinLastData> deviceMinLastDatas)
        {
            var dbContext = ApiGetDbContext();

            foreach (var deviceMinLastData in deviceMinLastDatas)
            {
                var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(deviceMinLastData.Calendar).DateTime;
                var offset = TimeSpan.FromHours(-6); // Beispiel: Offset von 2 Stunden
                deviceMinLastData.TS = new DateTimeOffset(dateTime, offset).ToUniversalTime();

                lock (GrowattDeviceMinLastData._syncRoot)
                {
                    GrowattDeviceMinLastData.Add(deviceMinLastData);
                }

                var dbContextDeviceMinLastData = await dbContext.GrowattDeviceMinLastData.FindAsync(deviceMinLastData.SerialNum, deviceMinLastData.Time);
                if (dbContextDeviceMinLastData != null)
                {
                    dbContext.Entry(dbContextDeviceMinLastData).CurrentValues.SetValues(deviceMinLastData);
                }
                else
                {
                    dbContext.GrowattDeviceMinLastData.Add(deviceMinLastData);
                }
            }

            await dbContext.SaveChangesAsync();

            ApiInvokeStateHasChanged();
        }

        private async Task GrowattSaveDeviceNoahInfoData(List<DeviceNoahInfoData>? deviceNoahInfoDatas)
        {
            if (deviceNoahInfoDatas != null)
            {
                var dbContext = ApiGetDbContext();
                foreach (var deviceNoahInfo in deviceNoahInfoDatas)
                {
                    var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(deviceNoahInfo.LastUpdateTime).DateTime;
                    var offset = TimeSpan.FromHours(-6); // Beispiel: Offset von 2 Stunden
                    deviceNoahInfo.TS = new DateTimeOffset(dateTime, offset).ToUniversalTime();

                    GrowattSetOfflineState(dbContext, deviceNoahInfo.DeviceSn, deviceNoahInfo.Lost ? new DateTime(deviceNoahInfo.LastUpdateTime) : null);

                    lock (GrowattDevices._syncRoot)
                    {
                        var device = GrowattDevices.FirstOrDefault(x => x.DeviceSn == deviceNoahInfo.DeviceSn);
                        if (device != null)
                        {
                            device.PowerValueDefault = (int)deviceNoahInfo.DefaultPower;
                        }
                    }

                    lock (GrowattDeviceNoahInfoData._syncRoot)
                    {
                        var apiServiceDeviceNoahInfo = GrowattDeviceNoahInfoData.FirstOrDefault(x => x.DeviceSn == deviceNoahInfo.DeviceSn);
                        if (apiServiceDeviceNoahInfo != null) GrowattDeviceNoahInfoData.Remove(apiServiceDeviceNoahInfo);
                        GrowattDeviceNoahInfoData.Add(deviceNoahInfo);
                    }

                    var dbContextDeviceNoahInfo = await dbContext.GrowattDeviceNoahInfoData.FindAsync(deviceNoahInfo.DeviceSn);
                    if (dbContextDeviceNoahInfo != null)
                    {
                        dbContext.Entry(dbContextDeviceNoahInfo).CurrentValues.SetValues(deviceNoahInfo);
                    }
                    else
                    {
                        dbContext.GrowattDeviceNoahInfoData.Add(deviceNoahInfo);
                    }
                }

                // Save changes to the database
                await dbContext.SaveChangesAsync();

                ApiInvokeStateHasChanged();
            }
        }

        private async Task GrowattSaveDeviceNoahLastData(List<DeviceNoahLastData> deviceNoahLastDatas)
        {
            var dbContext = ApiGetDbContext();

            foreach (var deviceNoahLastData in deviceNoahLastDatas)
            {
                var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(deviceNoahLastData.time).DateTime;
                var offset = TimeSpan.FromHours(-6); // Beispiel: Offset von 2 Stunden
                deviceNoahLastData.TS = new DateTimeOffset(dateTime, offset).ToUniversalTime();

                lock (GrowattDevices._syncRoot)
                {
                    var device = GrowattDevices.FirstOrDefault(x => x.DeviceSn == deviceNoahLastData.deviceSn);
                    if (device != null)
                    {
                        device.IsBatteryEmpty = GrowattNearofBatterySocEmpty(deviceNoahLastData);
                        device.IsBatteryFull = deviceNoahLastData.totalBatteryPackChargingStatus == 0 && GrowattNearofBatterySocFull(deviceNoahLastData);
                        device.Soc = deviceNoahLastData.totalBatteryPackSoc;
                        device.SocMin = deviceNoahLastData.dischargeSocLimit;
                        device.PowerValueSolar = (int)deviceNoahLastData.ppv;
                        device.PowerValueBatteryPower = (int)deviceNoahLastData.totalBatteryPackChargingPower;
                        device.PowerValueBatteryStatus = (int)deviceNoahLastData.totalBatteryPackChargingStatus;
                        device.PowerValueOutput = (int)deviceNoahLastData.pac;
                    }
                }

                lock (GrowattDeviceNoahLastData._syncRoot)
                {
                    GrowattDeviceNoahLastData.Add(deviceNoahLastData);
                }

                var deviceDbContext = await dbContext.GrowattDevices.FindAsync(deviceNoahLastData.deviceSn);
                if (deviceDbContext != null)
                {
                    deviceDbContext.IsBatteryEmpty = GrowattNearofBatterySocEmpty(deviceNoahLastData);
                    deviceDbContext.IsBatteryFull = deviceNoahLastData.totalBatteryPackChargingStatus == 0 && GrowattNearofBatterySocFull(deviceNoahLastData);
                    deviceDbContext.Soc = deviceNoahLastData.totalBatteryPackSoc;
                    deviceDbContext.SocMin = deviceNoahLastData.dischargeSocLimit;
                    deviceDbContext.PowerValueSolar = (int)deviceNoahLastData.ppv;
                    deviceDbContext.PowerValueBatteryPower = (int)deviceNoahLastData.totalBatteryPackChargingPower;
                    deviceDbContext.PowerValueBatteryStatus = (int)deviceNoahLastData.totalBatteryPackChargingStatus;
                    deviceDbContext.PowerValueOutput = (int)deviceNoahLastData.pac;
                }

                var dbContextDeviceNoahLastData = await dbContext.GrowattDeviceNoahLastData.FindAsync(deviceNoahLastData.deviceSn, deviceNoahLastData.time);
                if (dbContextDeviceNoahLastData != null)
                {
                    dbContext.Entry(dbContextDeviceNoahLastData).CurrentValues.SetValues(deviceNoahLastData);
                }
                else
                {
                    dbContext.GrowattDeviceNoahLastData.Add(deviceNoahLastData);
                }
            }

            await dbContext.SaveChangesAsync();

            ApiInvokeStateHasChanged();
        }

        private void GrowattSetOfflineState(ApplicationDbContext dbContext, string? deviceSn, DateTimeOffset? dateTimeOffset)
        {
            lock (GrowattDevices._syncRoot)
            {
                var deviceApiService = GrowattDevices.FirstOrDefault(x => x.DeviceType == "noah" && x.DeviceSn == deviceSn);
                if (deviceApiService != null)
                {
                    deviceApiService.IsOfflineSince = dateTimeOffset;
                }
            }

            var deviceDbContext = dbContext.GrowattDevices.FirstOrDefault(x => x.DeviceType == "noah" && x.DeviceSn == deviceSn);
            if (deviceDbContext != null)
            {
                deviceDbContext.IsOfflineSince = dateTimeOffset;
            }
        }

        public int GrowattGetBatteryLevel() => GrowattApiService.GrowattGetBatteryLevel();

        public int GrowattGetBatteryMaxSoc() => GrowattApiService.GrowattGetBatteryMaxSoc();

        private static bool GrowattNearofBatterySocEmpty(DeviceNoahLastData deviceNoahLastData) => GrowattApiService.GrowattNearofBatterySocEmpty(deviceNoahLastData);

        private static bool GrowattNearofBatterySocFull(DeviceNoahLastData deviceNoahLastData) => GrowattApiService.GrowattNearofBatterySocFull(deviceNoahLastData);


        #endregion Private Methods
    }
}
