using BlazorBootstrap;
using EnergyAutomate.Definitions;
using EnergyAutomate.Extentions;
using EnergyAutomate.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EnergyAutomate.Services
{
    public partial class EnergyAutomateService : IHostedService, IDisposable
    {
        #region Fields

        private readonly Lock lockAdjustPower = new();
        private readonly Lock lockLoadBalance = new();

        private readonly string messageTemplatePowerSet = "{CurrentState.UtcNow} {Type} ({device}) PowerValue: {powerValue} W";
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private int _adjustmentWaitCycles = 0;
        private int _timerCallbackRunning;
        private bool _disposed;
        private bool isBusy;
        private bool isRunning;
        private string statusText = "Stopped";
        private string? lastError;

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
                    _ = TibberApiService.RestartServiceAsync();
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
        public Guid? TibberHomeId
        {
            get => TibberApiService.TibberHomeId;
            set => TibberApiService.TibberHomeId = value;
        }
        public string ActiveCalculationTemplateKey { get; set; } = "calculation.average-power";
        public string ActiveAdjustmentTemplateKey { get; set; } = "adjustment.auto-mode";
        public string ActiveDistributionTemplateKey { get; set; } = "distribution.equal";
        public string ActiveDistributionManagerTemplateKey { get; set; } = "distribution-manager.default";
        private GrowattApiClient GrowattApiClient => ServiceProvider.GetRequiredService<GrowattApiClient>();
        private TIbberApiService TibberApiService => ServiceProvider.GetRequiredService<TIbberApiService>();
        private ThreadSafeObservableCollection<DeviceMinInfoData> GrowattDeviceMinInfoData { get; set; } = [];
        private ThreadSafeObservableCollection<DeviceMinLastData> GrowattDeviceMinLastData { get; set; } = [];
        private ThreadSafeObservableCollection<DeviceNoahInfoData> GrowattDeviceNoahInfoData { get; set; } = [];
        private ThreadSafeObservableCollection<DeviceNoahLastData> GrowattDeviceNoahLastData { get; set; } = [];
        private ApiQueueWatchdog<IDeviceQuery> GrowattDeviceQueryQueueWatchdog => ServiceProvider.GetRequiredService<ApiQueueWatchdog<IDeviceQuery>>();
        private ThreadSafeObservableCollection<DeviceList> GrowattDevices { get; set; } = [];
        private ILogger Logger => ServiceProvider.GetRequiredService<ILogger<EnergyAutomateService>>();
        private ILogger LoggerRTM => ServiceProvider.GetRequiredService<ILogger<RealTimeMeasurement>>();
        private IServiceProvider ServiceProvider { get; set; }
        private ThreadSafeObservableCollection<TibberPrice> TibberPrices => TibberApiService.TibberPrices;
        private ThreadSafeObservableCollection<TibberRealTimeMeasurement> TibberRealTimeMeasurement => TibberApiService.TibberRealTimeMeasurement;

        public bool IsBusy => isBusy;
        public bool IsRunning => isRunning;
        public string StatusText => statusText;
        public string? LastError => lastError;

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
            isBusy = true;
            statusText = "Starting";
            lastError = null;

            try
            {
                await ApiLoadDataFromDatabase();
                isRunning = true;
                statusText = "Running";
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                statusText = "Failed";
                Logger.LogError(ex, "API service startup failed");
            }
            finally
            {
                isBusy = false;
            }
        }

        public async Task ApiStopAsync(CancellationToken cancellationToken)
        {
            isBusy = true;
            statusText = "Stopping";
            lastError = null;

            try
            {
                isRunning = false;
                statusText = "Stopped";
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                statusText = "Failed";
                Logger.LogError(ex, "API service stop failed");
            }
            finally
            {
                isBusy = false;
            }
        }

        public async Task RestartServiceAsync(CancellationToken cancellationToken = default)
        {
            await ApiStopAsync(cancellationToken);
            await ApiStartAsync(cancellationToken);
        }

        public async Task StartServiceAsync(CancellationToken cancellationToken = default)
        {
            await ApiStartAsync(cancellationToken);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ApiStartAsync(cancellationToken);
        }

        public async Task StopServiceAsync(CancellationToken cancellationToken = default)
        {
            await ApiStopAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await ApiStopAsync(cancellationToken);
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

        public static IEnumerable<TickMark> GenerateTickTickMarks(int start, int end, int step) => GrowattApiService.GenerateTickTickMarks(start, end, step); 

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

    }
}
