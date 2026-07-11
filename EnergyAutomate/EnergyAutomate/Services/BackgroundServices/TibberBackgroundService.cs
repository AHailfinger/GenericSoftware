using EnergyAutomate.Definitions;
using EnergyAutomate.Extentions;
using EnergyAutomate.Services.CodeFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnergyAutomate.Services.BackgroundServices;

public class TibberBackgroundService : IHostedService, IDisposable
{
    private readonly SemaphoreSlim lifecycleLock = new(1, 1);
    private readonly EnergyAutomateService apiService;
    private readonly TIbberApiService realTimeMeasurementWatchdog;
    private readonly IConfiguration configuration;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<TibberBackgroundService> logger;
    private readonly ILogger<RealTimeMeasurement> rtmLogger;
    private readonly ApiQueueWatchdog<IDeviceQuery> growattDeviceQueryQueueWatchdog;
    private bool isBusy;
    private bool isRunning;
    private string statusText = "Stopped";
    private string? lastError;

    public TibberBackgroundService(EnergyAutomateService apiService, TIbberApiService realTimeMeasurementWatchdog, IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<TibberBackgroundService> logger, ILogger<RealTimeMeasurement> rtmLogger, ApiQueueWatchdog<IDeviceQuery> growattDeviceQueryQueueWatchdog)
    {
        this.apiService = apiService;
        this.realTimeMeasurementWatchdog = realTimeMeasurementWatchdog;
        this.configuration = configuration;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
        this.rtmLogger = rtmLogger;
        this.growattDeviceQueryQueueWatchdog = growattDeviceQueryQueueWatchdog;
    }

    public bool IsBusy => isBusy;
    public bool IsRunning => isRunning;
    public string StatusText => statusText;
    public string? LastError => lastError;

    public async Task RestartServiceAsync(CancellationToken cancellationToken = default)
    {
        await StopServiceAsync(cancellationToken);
        await StartServiceAsync(cancellationToken, respectConfiguration: false);
    }

    public async Task StartServiceAsync(CancellationToken cancellationToken = default)
    {
        await StartServiceAsync(cancellationToken, respectConfiguration: false);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartServiceAsync(cancellationToken, respectConfiguration: true);
    }

    public async Task StopServiceAsync(CancellationToken cancellationToken = default)
    {
        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (!isRunning && !isBusy)
            {
                statusText = "Stopped";
                return;
            }

            isBusy = true;
            statusText = "Stopping";
            lastError = null;

            logger.LogTrace("Stopping TibberBackgroundService");

            try
            {
                await realTimeMeasurementWatchdog.StopAsync(cancellationToken);
                isRunning = false;
                statusText = "Stopped";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                statusText = "Stopped";
                logger.LogInformation("TibberBackgroundService stop was canceled");
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                statusText = "Failed";
                logger.LogError(ex, "TibberBackgroundService stop failed");
            }
        }
        finally
        {
            isBusy = false;
            lifecycleLock.Release();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return StopServiceAsync(cancellationToken);
    }

    public async Task ProcessRealTimeMeasurementAsync(RealTimeMeasurement value)
    {
        try
        {
            var tibberRealTimeMeasurement = new TibberRealTimeMeasurement(value);

            await apiService.TibberStoreRealTimeMeasurementAsync(tibberRealTimeMeasurement);

            if (apiService.IsEnabled)
            {
                await ExecuteCalculationTemplateAsync(tibberRealTimeMeasurement);

                apiService.ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 1, Key = "GrowattNoahTotalPPV", Value = apiService.CurrentState.GrowattNoahTotalPPV.ToString() });
                apiService.ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue() { Index = 2, Key = "WeatherIsCloudy", Value = apiService.CurrentState.IsCloudy().ToString() });

                if (!apiService.TibberListPrices().Where(x => x.StartsAt.UtcDateTime.Date == apiService.CurrentState.UtcNow.Date).Any() || (apiService.CurrentState.UtcNow.Hour > 13 && apiService.CurrentState.UtcNow.AddDays(1).Date != apiService.TibberListPrices().Max(x => x.StartsAt.UtcDateTime).Date))
                {
                    if (apiService.CurrentState.CheckTibberPricesCondition($"GetTomorrowPrices_{apiService.CurrentState.UtcNow.Hour}"))
                    {
                        await apiService.TibberGetTomorrowPrices();
                    }
                }

                var firstTime = apiService.CurrentState.WeatherForecastToday?.Hourly?.Time?.FirstOrDefault();

                if (firstTime == null || DateTime.Parse(firstTime).Date != apiService.CurrentState.UtcNow.Date)
                {
                    apiService.CurrentState.WeatherForecastToday = await apiService.CurrentState.GetWeatherForecastAsync();
                    apiService.CurrentState.WeatherForecastTomorrow = await apiService.CurrentState.GetWeatherForecastAsync(DateTime.Today.AddDays(1));

                    (apiService.CurrentState.BatteryChargeStart, apiService.CurrentState.BatteryChargeEnd) = apiService.CurrentState.CalculateBatteryChargingWindow();
                }

                await ExecuteAdjustmentTemplateAsync(tibberRealTimeMeasurement);

                tibberRealTimeMeasurement.PowerValueTotalDefault = apiService.CurrentState.GrowattNoahTotalDefaultPower;
                apiService.ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue(tibberRealTimeMeasurement.TS, 3, "RTMTotalPowerDefaultNoah", (tibberRealTimeMeasurement.PowerValueTotalDefault ?? 0).ToString()));

                tibberRealTimeMeasurement.PowerValueTotalCommited = apiService.CurrentState.PowerValueTotalCommited;
                apiService.ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue(tibberRealTimeMeasurement.TS, 4, "RTMTotalPowerCommited", (tibberRealTimeMeasurement.PowerValueTotalCommited ?? 0).ToString()));

                tibberRealTimeMeasurement.PowerValueTotalRequested = apiService.CurrentState.PowerValueTotalRequested;
                apiService.ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue(tibberRealTimeMeasurement.TS, 5, "RTMTotalPowerRequested", (tibberRealTimeMeasurement.PowerValueTotalRequested ?? 0).ToString()));

                tibberRealTimeMeasurement.SettingPowerLoadSeconds = apiService.ApiSettingAvgPowerLoadSeconds;
                tibberRealTimeMeasurement.SettingOffSetAvg = apiService.ApiSettingAvgPowerOffset;
                tibberRealTimeMeasurement.SettingAvgPowerHysteresis = apiService.ApiSettingAvgPowerHysteresis;

                await apiService.TibberUpdateRealTimeMeasurementAsync(tibberRealTimeMeasurement);
            }

            apiService.ApiInvokeStateHasChanged();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }

    public void Dispose()
    {
        lifecycleLock.Dispose();
    }

    private async Task ExecuteCalculationTemplateAsync(TibberRealTimeMeasurement measurement)
    {
        var measurements = apiService.TibberListRealTimeMeasurement().ToList();

        var eventData = new EnergyCalculationEvent(
            measurement.TS,
            (int)measurement.Power,
            measurement.PowerProduction.HasValue ? (int?)measurement.PowerProduction.Value : null,
            measurement.TotalPower);

        var factory = new EnergyCalculationScriptFactory(eventData, measurement, measurements, rtmLogger);
        using var scope = serviceScopeFactory.CreateScope();
        var runtimeCodeTemplateExecutor = scope.ServiceProvider.GetRequiredService<RuntimeCodeTemplateExecutor>();
        await runtimeCodeTemplateExecutor.ExecuteAsync(apiService.ActiveCalculationTemplateKey, factory);
    }

    private async Task ExecuteAdjustmentTemplateAsync(TibberRealTimeMeasurement measurement)
    {
        var onlineDevices = apiService.GrowattGetDevicesNoahOnline();
        var eventData = new EnergyAdjustmentEvent(
            measurement.TS,
            measurement.TotalPower,
            measurement.PowerAvgConsumption ?? 0,
            measurement.PowerAvgProduction ?? 0,
            apiService.CurrentState.IsGrowattOnline,
            apiService.CurrentState.IsExpensiveRestrictionMode);

        var factory = new EnergyAdjustmentScriptFactory(eventData, apiService, growattDeviceQueryQueueWatchdog, onlineDevices, rtmLogger);
        using var scope = serviceScopeFactory.CreateScope();
        var runtimeCodeTemplateExecutor = scope.ServiceProvider.GetRequiredService<RuntimeCodeTemplateExecutor>();
        await runtimeCodeTemplateExecutor.ExecuteAsync(apiService.ActiveAdjustmentTemplateKey, factory);
    }

    private async Task StartServiceAsync(CancellationToken cancellationToken, bool respectConfiguration)
    {
        await lifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (isRunning)
            {
                statusText = "Running";
                return;
            }

            if (respectConfiguration && !configuration.GetSection("BackgroundServices").GetValue("TibberBackgroundService", true))
            {
                statusText = "Disabled by configuration";
                return;
            }

            var token = configuration["ApiSettings:TibberApiToken"];
            if (string.IsNullOrWhiteSpace(token) || token.Contains("your-api-token", StringComparison.OrdinalIgnoreCase))
            {
                statusText = "Missing Tibber API token";
                logger.LogWarning("Tibber API token is missing or placeholder. Tibber background service will not start.");
                return;
            }

            isBusy = true;
            statusText = "Starting";
            lastError = null;

            try
            {
                logger.LogTrace("Starting TibberBackgroundService");
                await realTimeMeasurementWatchdog.StartAsync(cancellationToken);

                if (apiService.TibberHomeId.HasValue)
                {
                    isRunning = true;
                    statusText = "Running";
                }
                else
                {
                    statusText = "Stopped";
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                statusText = "Stopped";
                logger.LogInformation("TibberBackgroundService startup was canceled");
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                statusText = "Failed";
                logger.LogError(ex, "TibberBackgroundService startup failed");
            }
        }
        finally
        {
            isBusy = false;
            lifecycleLock.Release();
        }
    }
}
