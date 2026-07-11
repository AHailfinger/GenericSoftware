using BlazorBootstrap;
using EnergyAutomate.Definitions;
using EnergyAutomate.Extentions;
using EnergyAutomate.Services.BackgroundServices;
using EnergyAutomate.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EnergyAutomate.Services
{
    public partial class EnergyAutomateService : IDisposable
    {
        #region Fields

        private readonly Lock lockAdjustPower = new();
        private readonly Lock lockLoadBalance = new();

        private readonly string messageTemplatePowerSet = "{CurrentState.UtcNow} {Type} ({device}) PowerValue: {powerValue} W";
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private int _adjustmentWaitCycles = 0;
        private int _timerCallbackRunning;
        private bool _disposed;

        #endregion Fields

        #region Public Constructors

        public EnergyAutomateService(IServiceProvider serviceProvider)
        {
            DistributionManager = new DistributionManager(serviceProvider);
            CurrentState = new ApiState(serviceProvider, this);
            ServiceProvider = serviceProvider;
            _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            GrowattDeviceQueryQueueWatchdog.OnItemDequeued += GrowattDeviceQueryQueueWatchdog_OnItemDequeued;
        }

        #endregion Public Constructors

        #region Events

        public event EventHandler? StateHasChanged;

        #endregion Events

        #region Properties

        private bool isEnabled;
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    _ = TibberBackgroundService?.RestartServiceAsync();
                }
            }
        }

        public bool ApiSettingAutoMode { get; set; }
        public int ApiSettingAvgPower { get; set; } = 200;
        public List<APiTraceValue> ApiSettingAvgPowerAdjustmentTraceValues { get; set; } = [];
        public int ApiSettingAvgPowerHysteresis { get; set; } = 40;
        public int ApiSettingAvgPowerLoadSeconds { get; set; } = 60;
        public int ApiSettingAvgPowerOffset { get; set; } = 25;
        public bool ApiSettingBatteryPriorityMode { get; set; } = false;
        public int ApiSettingExtentionAvgPower { get; set; } = 300;
        public TimeSpan ApiSettingExtentionExclusionFrom { get; set; } = new TimeSpan(7, 0, 0);
        public TimeSpan ApiSettingExtentionExclusionUntil { get; set; } = new TimeSpan(18, 0, 0);
        public bool ApiSettingExtentionMode { get; set; } = true;
        public int ApiSettingMaxPower { get; set; } = 800;
        public int ApiSettingPowerAdjustmentFactor { get; set; } = 50;
        public int ApiSettingPowerAdjustmentWaitCycles { get; set; } = 3;
        public bool ApiSettingRestrictionMode { get; set; } = false;
        public int ApiSettingSocMax { get; set; } = 90;
        public int ApiSettingSocMin { get; set; } = 10;
        public int ApiSettingTimeOffset { get; set; } = DateTimeOffset.Now.Offset.Hours;
        public ApiState CurrentState { get; set; }
        public DistributionManager DistributionManager { get; init; }
        public int GrowattDeviceQueryQueueWatchdogCount => GrowattDeviceQueryQueueWatchdog.Count;
        public Guid? TibberHomeId { get; set; }
        public string ActiveCalculationTemplateKey { get; set; } = "calculation.average-power";
        public string ActiveAdjustmentTemplateKey { get; set; } = "adjustment.auto-mode";
        public string ActiveDistributionTemplateKey { get; set; } = "distribution.equal";
        public string ActiveDistributionManagerTemplateKey { get; set; } = "distribution-manager.default";
        private GrowattApiClient GrowattApiClient => ServiceProvider.GetRequiredService<GrowattApiClient>();
        private TibberBackgroundService TibberBackgroundService => ServiceProvider.GetRequiredService<TibberBackgroundService>();
        private ThreadSafeObservableCollection<DeviceMinInfoData> GrowattDeviceMinInfoData { get; set; } = [];
        private ThreadSafeObservableCollection<DeviceMinLastData> GrowattDeviceMinLastData { get; set; } = [];
        private ThreadSafeObservableCollection<DeviceNoahInfoData> GrowattDeviceNoahInfoData { get; set; } = [];
        private ThreadSafeObservableCollection<DeviceNoahLastData> GrowattDeviceNoahLastData { get; set; } = [];
        private ApiQueueWatchdog<IDeviceQuery> GrowattDeviceQueryQueueWatchdog => ServiceProvider.GetRequiredService<ApiQueueWatchdog<IDeviceQuery>>();
        private ThreadSafeObservableCollection<DeviceList> GrowattDevices { get; set; } = [];
        private ILogger Logger => ServiceProvider.GetRequiredService<ILogger<EnergyAutomateService>>();
        private ILogger LoggerRTM => ServiceProvider.GetRequiredService<ILogger<RealTimeMeasurement>>();
        private IServiceProvider ServiceProvider { get; set; }
        private ThreadSafeObservableCollection<TibberPrice> TibberPrices { get; set; } = [];
        private ThreadSafeObservableCollection<TibberRealTimeMeasurement> TibberRealTimeMeasurement { get; set; } = [];

        #endregion Properties

        #region Public Methods

        public void ApiInvokeStateHasChanged()
        {
            StateHasChanged?.Invoke(this, new EventArgs());
        }

        public async Task ApiLoadDataFromDatabase()
        {
            var dbContext = ApiGetDbContext();

            await ApiLoadRuntimeSettingsFromDatabaseAsync(dbContext);
            await GrowattApiService.LoadDataFromDatabaseAsync(dbContext);

            var realTimeMeasurements = await dbContext.TibberRealTimeMeasurements.OrderByDescending(x => x.TS).Take(100).ToListAsync();
            TibberRealTimeMeasurement.Clear();
            foreach (var measurement in realTimeMeasurements)
            {
                TibberRealTimeMeasurement.Add(measurement);
            }

            var prices = await dbContext.TibberPrices.OrderByDescending(x => x.StartsAt).Take(48).ToListAsync();
            TibberPrices.Clear();
            foreach (var price in prices.OrderBy(x => x.StartsAt))
            {
                TibberPrices.Add(price);
            }
        }

        public async Task ApiLoadRuntimeSettingsFromDatabaseAsync()
        {
            var dbContext = ApiGetDbContext();
            await ApiLoadRuntimeSettingsFromDatabaseAsync(dbContext);
        }

        public async Task ApiSaveRuntimeSettingsToDatabaseAsync()
        {
            var dbContext = ApiGetDbContext();
            var settings = await dbContext.ApiRuntimeSettings.FindAsync(ApiRuntimeSettings.DefaultId);
            if (settings is null)
            {
                settings = new ApiRuntimeSettings();
                dbContext.ApiRuntimeSettings.Add(settings);
            }

            ApplyRuntimeSettingsToEntity(settings);
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync();
            Logger.LogInformation("Runtime settings saved to database");
        }

        public async Task ApiStartAsync(CancellationToken cancellationToken)
        {
            await ApiLoadDataFromDatabase();
        }

        public async Task ApiStopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            GrowattDeviceQueryQueueWatchdog.OnItemDequeued -= GrowattDeviceQueryQueueWatchdog_OnItemDequeued;
        }

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

        public async Task TibberGetDataFromWeb()
        {
            try
            {
                var tibberApiClient = ServiceProvider.GetRequiredService<TibberApiClient>();

                var basicData = await tibberApiClient.GetBasicData();
                TibberHomeId = basicData.Data.Viewer.Homes.FirstOrDefault()?.Id;

                if (TibberHomeId.HasValue)
                {
                    var consumption = await tibberApiClient.GetHomeConsumption(TibberHomeId.Value, EnergyResolution.Monthly);

                    var customQueryBuilder =
                        new TibberQueryBuilder()
                            .WithAllScalarFields()
                            .WithViewer(
                                new ViewerQueryBuilder()
                                    .WithAllScalarFields()
                                    .WithAccountType()
                                    .WithHome(
                                        new HomeQueryBuilder()
                                            .WithAllScalarFields()
                                            .WithAddress(new AddressQueryBuilder().WithAllFields())
                                            .WithCurrentSubscription(
                                                new SubscriptionQueryBuilder()
                                                    .WithAllScalarFields()
                                                    .WithSubscriber(new LegalEntityQueryBuilder().WithAllFields())
                                                    .WithPriceInfo(new PriceInfoQueryBuilder().WithCurrent(new PriceQueryBuilder().WithAllFields()))
                                            )
                                            .WithOwner(new LegalEntityQueryBuilder().WithAllFields())
                                            .WithFeatures(new HomeFeaturesQueryBuilder().WithAllFields())
                                            .WithMeteringPointData(new MeteringPointDataQueryBuilder().WithAllFields()),
                                        TibberHomeId
                                    )
                            );

                    var customQuery = customQueryBuilder.Build();
                    var result = await tibberApiClient.Query(customQuery);

                    var query = new TibberQueryBuilder().WithHomeConsumption(TibberHomeId.Value, EnergyResolution.Monthly, 12).Build();
                    var response = await tibberApiClient.Query(query);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<TibberPrice> TibberGetPriceDatas()
        {
            var items = TibberPrices.OrderByDescending(x => x.StartsAt).Take(48).ToList();
            return items.OrderBy(x => x.StartsAt).ToList();
        }

        public async Task TibberGetTomorrowPrices()
        {
            try
            {
                if (TibberHomeId.HasValue)
                {
                    // Erstellung der benutzerdefinierten Abfrage mit dem TibberQueryBuilder
                    var customQueryBuilder =
                    new TibberQueryBuilder()
                        .WithViewer(
                            new ViewerQueryBuilder()
                            .WithHome(
                                new HomeQueryBuilder()
                                .WithCurrentSubscription(
                                    new SubscriptionQueryBuilder()
                                        .WithPriceInfo(
                                            new PriceInfoQueryBuilder()
                                            .WithAllScalarFields()
                                            .WithToday(
                                                new PriceQueryBuilder()
                                                .WithAllScalarFields()
                                            )
                                            .WithTomorrow(
                                                new PriceQueryBuilder()
                                                .WithAllScalarFields()
                                            )
                                        )
                                ),
                                TibberHomeId
                            )
                        );

                    // Abfrage ausführen
                    var customQuery = customQueryBuilder.Build(); // Erzeugt den GraphQL-Abfragetext
                    var result = await ServiceProvider.GetRequiredService<TibberApiClient>().Query(customQuery);

                    var tomorrowList = result.Data.Viewer.Home.CurrentSubscription.PriceInfo.Tomorrow.Select(x => new TibberPrice()
                    {
                        StartsAt = DateTimeOffset.Parse(x.StartsAt).ToUniversalTime(),
                        Total = x.Total,
                        Level = x.Level
                    }).ToList();
                    await TibberSavePricesAsync(tomorrowList);

                    var todayList = result.Data.Viewer.Home.CurrentSubscription.PriceInfo.Today.Select(x => new TibberPrice()
                    {
                        StartsAt = DateTimeOffset.Parse(x.StartsAt).ToUniversalTime(),
                        Total = x.Total,
                        Level = x.Level
                    }).ToList();
                    await TibberSavePricesAsync(todayList);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task TibberStoreRealTimeMeasurementAsync(TibberRealTimeMeasurement tibberRealTimeMeasurement)
        {
            var dbContext = ApiGetDbContext();

            lock (TibberRealTimeMeasurement._syncRoot)
            {
                TibberRealTimeMeasurement.Add(tibberRealTimeMeasurement);
            }

            dbContext.TibberRealTimeMeasurements.Add(tibberRealTimeMeasurement);
            await dbContext.SaveChangesAsync();
        }

        public async Task TibberUpdateRealTimeMeasurementAsync(TibberRealTimeMeasurement tibberRealTimeMeasurement)
        {
            var dbContext = ApiGetDbContext();
            var dbEntity = await dbContext.TibberRealTimeMeasurements.FindAsync(tibberRealTimeMeasurement.TS);
            if (dbEntity != null)
            {
                dbContext.Entry(dbEntity).CurrentValues.SetValues(tibberRealTimeMeasurement);
                await dbContext.SaveChangesAsync();
            }
        }

        public List<TibberPrice> TibberListPrices()
        {
            lock (TibberPrices._syncRoot)
                return TibberPrices.ToList();
        }

        public List<TibberRealTimeMeasurement> TibberListRealTimeMeasurement()
        {
            lock (TibberRealTimeMeasurement._syncRoot)
                return TibberRealTimeMeasurement.ToList();
        }

        public void TibberRealTimeMeasurementRegisterOnCollectionChanged(object sender, Action callback) => TibberRealTimeMeasurement.RegisterOnCollectionChanged(sender, callback);

        public void TibberRealTimeMeasurementUnRegisterOnCollectionChanged(object sender) => TibberRealTimeMeasurement.UnRegisterOnCollectionChanged(sender);

        #endregion Public Methods

        #region Private Methods

        private async Task ApiAutoModeDisabledLoadBalanceRule(TibberRealTimeMeasurement tibberRealTimeMeasurement)
        {
            // If the automatic mode is disabled, the power value is set to 0
            await GrowattClearSetPowerAsync(tibberRealTimeMeasurement.TS);

            if (ApiSettingBatteryPriorityMode)
            {
                Logger.LogTrace("LoadBalanced: Set BattPriority");
                //If loadbalance is active the battety priority is set
                await GrowattQueryBattPriorityDeviceNoahTimeSegmentsAsync();
            }
            else
            {
                Logger.LogTrace("LoadBalanced: Set LoadPriority");

                //If loadbalance is not active the load priority is set
                // Calc avg power value
                await GrowattQueryLoadPriorityDeviceNoahTimeSegmentsAsync();
            }
        }

        private async Task ApiAutoModeEnabledLoadBalanceRule(TibberRealTimeMeasurement tibberRealTimeMeasurement)
        {
            // If the automatic mode is enabled and the restriction is not active, the power value
            // is set to 0
            await GrowattClearSetPowerAsync(tibberRealTimeMeasurement.TS);
        }

        private ApplicationDbContext ApiGetDbContext()
        {
            return _serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }

        private async Task<ApiException?> ExecuteWithExceptionHandlingAsync(IDeviceQuery item, DeviceList? device, Func<Task> action)
        {
            try
            {
                await action();
                return null; // Operation erfolgreich
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 5 && device != null)
                {
                    GrowattSetOfflineState(this.ApiGetDbContext(), device.DeviceSn, CurrentState.UtcNow);
                    item.Force = false;
                }
                return ex; // Operation fehlgeschlagen
            }
            catch (Exception ex)
            {
                throw new ApiException("Exception", 1, ex);
            }
        }

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

        private async Task TibberSavePricesAsync(IList<TibberPrice> prices)
        {
            var dbContext = ApiGetDbContext();
            var topPrices = prices.OrderByDescending(x => x.Total).Take(10).ToList();
            var avg = topPrices.Average(x => x.Total);

            // Verarbeitung der Ergebnisse
            foreach (var price in prices)
            {
                price.AutoModeRestriction = price.Total > avg;

                Logger.LogTrace("Tibber price {StartsAt}: {Total} EUR/kWh", price.StartsAt, price.Total);

                //Prüfe ob es denn eintrag schon gibt und falls ja mach ein update
                if (!TibberPrices.Any(x => x.StartsAt == price.StartsAt))
                {
                    TibberPrices.Add(price);
                }

                var exists = await dbContext.TibberPrices.AnyAsync(x => x.StartsAt == price.StartsAt);
                // Prüfe, ob der Datensatz bereits existiert
                if (!exists)
                {
                    // Füge den neuen Datensatz hinzu
                    dbContext.TibberPrices.Add(price);
                }
            }
            await dbContext.SaveChangesAsync(); // Änderungen speichern
        }

        private async Task ExecuteCalculationTemplateAsync(TibberRealTimeMeasurement measurement)
        {
            List<TibberRealTimeMeasurement> measurements;
            lock (TibberRealTimeMeasurement._syncRoot)
            {
                measurements = TibberRealTimeMeasurement.ToList();
            }

            var eventData = new EnergyCalculationEvent(
                measurement.TS,
                (int)measurement.Power,
                measurement.PowerProduction.HasValue ? (int?)measurement.PowerProduction.Value : null,
                measurement.TotalPower);

            var factory = new EnergyCalculationScriptFactory(eventData, measurement, measurements, LoggerRTM);
            await ServiceProvider.GetRequiredService<RuntimeCodeTemplateExecutor>().ExecuteAsync(ActiveCalculationTemplateKey, factory);
        }

        private async Task ExecuteAdjustmentTemplateAsync(TibberRealTimeMeasurement measurement)
        {
            var onlineDevices = GrowattGetDevicesNoahOnline();
            var eventData = new EnergyAdjustmentEvent(
                measurement.TS,
                measurement.TotalPower,
                measurement.PowerAvgConsumption ?? 0,
                measurement.PowerAvgProduction ?? 0,
                CurrentState.IsGrowattOnline,
                CurrentState.IsExpensiveRestrictionMode);

            var factory = new EnergyAdjustmentScriptFactory(eventData, this, GrowattDeviceQueryQueueWatchdog, onlineDevices, LoggerRTM);
            await ServiceProvider.GetRequiredService<RuntimeCodeTemplateExecutor>().ExecuteAsync(ActiveAdjustmentTemplateKey, factory);
        }

        private async Task ApiLoadRuntimeSettingsFromDatabaseAsync(ApplicationDbContext dbContext)
        {
            var settings = await dbContext.ApiRuntimeSettings.FindAsync(ApiRuntimeSettings.DefaultId);
            if (settings is null)
            {
                settings = new ApiRuntimeSettings();
                dbContext.ApiRuntimeSettings.Add(settings);
                await dbContext.SaveChangesAsync();
                Logger.LogInformation("Default runtime settings inserted into database");
            }

            ApplyRuntimeSettingsFromEntity(settings);
            Logger.LogInformation("Runtime settings loaded from database");
        }

        private void ApplyRuntimeSettingsFromEntity(ApiRuntimeSettings settings)
        {
            IsEnabled = settings.IsEnabled;
            ApiSettingAutoMode = settings.ApiSettingAutoMode;
            ApiSettingAvgPower = settings.ApiSettingAvgPower;
            ApiSettingAvgPowerHysteresis = settings.ApiSettingAvgPowerHysteresis;
            ApiSettingAvgPowerLoadSeconds = settings.ApiSettingAvgPowerLoadSeconds;
            ApiSettingAvgPowerOffset = settings.ApiSettingAvgPowerOffset;
            ApiSettingBatteryPriorityMode = settings.ApiSettingBatteryPriorityMode;
            ApiSettingExtentionMode = settings.ApiSettingExtentionMode;
            ApiSettingExtentionAvgPower = settings.ApiSettingExtentionAvgPower;
            ApiSettingExtentionExclusionFrom = settings.ApiSettingExtentionExclusionFrom;
            ApiSettingExtentionExclusionUntil = settings.ApiSettingExtentionExclusionUntil;
            ApiSettingMaxPower = settings.ApiSettingMaxPower;
            ApiSettingPowerAdjustmentFactor = settings.ApiSettingPowerAdjustmentFactor;
            ApiSettingPowerAdjustmentWaitCycles = settings.ApiSettingPowerAdjustmentWaitCycles;
            ApiSettingRestrictionMode = settings.ApiSettingRestrictionMode;
            ApiSettingSocMax = settings.ApiSettingSocMax;
            ApiSettingSocMin = settings.ApiSettingSocMin;
            ApiSettingTimeOffset = settings.ApiSettingTimeOffset;
            ActiveCalculationTemplateKey = settings.ActiveCalculationTemplateKey;
            ActiveAdjustmentTemplateKey = settings.ActiveAdjustmentTemplateKey;
            ActiveDistributionTemplateKey = settings.ActiveDistributionTemplateKey;
            ActiveDistributionManagerTemplateKey = settings.ActiveDistributionManagerTemplateKey;
        }

        private void ApplyRuntimeSettingsToEntity(ApiRuntimeSettings settings)
        {
            settings.IsEnabled = IsEnabled;
            settings.ApiSettingAutoMode = ApiSettingAutoMode;
            settings.ApiSettingAvgPower = ApiSettingAvgPower;
            settings.ApiSettingAvgPowerHysteresis = ApiSettingAvgPowerHysteresis;
            settings.ApiSettingAvgPowerLoadSeconds = ApiSettingAvgPowerLoadSeconds;
            settings.ApiSettingAvgPowerOffset = ApiSettingAvgPowerOffset;
            settings.ApiSettingBatteryPriorityMode = ApiSettingBatteryPriorityMode;
            settings.ApiSettingExtentionMode = ApiSettingExtentionMode;
            settings.ApiSettingExtentionAvgPower = ApiSettingExtentionAvgPower;
            settings.ApiSettingExtentionExclusionFrom = ApiSettingExtentionExclusionFrom;
            settings.ApiSettingExtentionExclusionUntil = ApiSettingExtentionExclusionUntil;
            settings.ApiSettingMaxPower = ApiSettingMaxPower;
            settings.ApiSettingPowerAdjustmentFactor = ApiSettingPowerAdjustmentFactor;
            settings.ApiSettingPowerAdjustmentWaitCycles = ApiSettingPowerAdjustmentWaitCycles;
            settings.ApiSettingRestrictionMode = ApiSettingRestrictionMode;
            settings.ApiSettingSocMax = ApiSettingSocMax;
            settings.ApiSettingSocMin = ApiSettingSocMin;
            settings.ApiSettingTimeOffset = ApiSettingTimeOffset;
            settings.ActiveCalculationTemplateKey = ActiveCalculationTemplateKey;
            settings.ActiveAdjustmentTemplateKey = ActiveAdjustmentTemplateKey;
            settings.ActiveDistributionTemplateKey = ActiveDistributionTemplateKey;
            settings.ActiveDistributionManagerTemplateKey = ActiveDistributionManagerTemplateKey;
        }

        #endregion Private Methods

        public static IEnumerable<TickMark> GenerateTickTickMarks(int start, int end, int step) => GrowattApiService.GenerateTickTickMarks(start, end, step);

        public int GrowattGetBatteryLevel() => GrowattApiService.GrowattGetBatteryLevel();

        public int GrowattGetBatteryMaxSoc() => GrowattApiService.GrowattGetBatteryMaxSoc();

        private static bool GrowattNearofBatterySocEmpty(DeviceNoahLastData deviceNoahLastData) => GrowattApiService.GrowattNearofBatterySocEmpty(deviceNoahLastData);

        private static bool GrowattNearofBatterySocFull(DeviceNoahLastData deviceNoahLastData) => GrowattApiService.GrowattNearofBatterySocFull(deviceNoahLastData);

    }
}
