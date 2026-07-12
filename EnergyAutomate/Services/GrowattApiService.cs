using BlazorBootstrap;
using EnergyAutomate.Emulator;
using EnergyAutomate.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnergyAutomate.Services;

public class GrowattApiService : IHostedService
{
    private readonly SemaphoreSlim lifecycleLock = new(1, 1);
    private readonly EnergyAutomateService apiService;
    private readonly ApiQueueWatchdog<IDeviceQuery> growattDeviceQueryQueueWatchdog;
    private readonly GrowattApiClient growattApiClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GrowattApiService> logger;
    private readonly PythonWrapper pythonWrapper;
    private readonly Timer timer;
    private int timerCallbackRunning;
    private bool disposed;
    private bool isBusy;
    private bool isRunning;
    private string statusText = "Stopped";
    private string? lastError;

    private ThreadSafeObservableCollection<DeviceMinInfoData> growattDeviceMinInfoData = [];
    private ThreadSafeObservableCollection<DeviceMinLastData> growattDeviceMinLastData = [];
    private ThreadSafeObservableCollection<DeviceNoahInfoData> growattDeviceNoahInfoData = [];
    private ThreadSafeObservableCollection<DeviceNoahLastData> growattDeviceNoahLastData = [];
    private ThreadSafeObservableCollection<DeviceList> growattDevices = [];

    public GrowattApiService(IServiceProvider serviceProvider)
    {
        apiService = serviceProvider.GetRequiredService<EnergyAutomateService>();
        growattDeviceQueryQueueWatchdog = serviceProvider.GetRequiredService<ApiQueueWatchdog<IDeviceQuery>>();
        growattApiClient = serviceProvider.GetRequiredService<GrowattApiClient>();
        _configuration = serviceProvider.GetRequiredService<IConfiguration>();
        logger = serviceProvider.GetRequiredService<ILogger<GrowattApiService>>();
            pythonWrapper = serviceProvider.GetRequiredService<PythonWrapper>();
        timer = new Timer(TimerCallback, null, 1000, 1000);
    }

    private ILogger<GrowattApiService> Logger => logger;

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

            try
            {
                Logger.LogTrace("Stopping Growatt API worker");
                pythonWrapper.StopPythonClient();
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                isRunning = false;
                statusText = "Stopped";
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                statusText = "Failed";
                Logger.LogError(ex, "Growatt API worker failed to stop");
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

            if (respectConfiguration && !(_configuration.GetSection("BackgroundServices").GetValue("GrowattApi", false)))
            {
                statusText = "Disabled by configuration";
                return;
            }

            isBusy = true;
            statusText = "Starting";
            lastError = null;

            try
            {
                Logger.LogTrace("Starting Growatt API worker");
                pythonWrapper.StartPythonClient();
                timer.Change(1000, 1000);
                isRunning = true;
                statusText = "Running";
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                statusText = "Failed";
                Logger.LogError(ex, "Growatt API worker failed to start");
            }
        }
        finally
        {
            isBusy = false;
            lifecycleLock.Release();
        }
    }

    public List<DeviceList> GrowattAllNoahDevices()
    {
        lock (growattDevices._syncRoot)
            return growattDevices.Where(x => x.DeviceType == "noah").ToList();
    }

    public List<DeviceList> GrowattGetDeviceLists()
    {
        lock (growattDevices._syncRoot)
            return growattDevices.ToList();
    }

    public List<DeviceList> GrowattGetDevicesNoahOnline()
    {
        lock (growattDevices._syncRoot)
            return growattDevices.Where(x => x.DeviceType == "noah" && x.IsOfflineSince == null).ToList();
    }

    public DeviceNoahInfoData? GrowattGetNoahInfoDataPerDevice(string? deviceSn)
    {
        lock (growattDeviceNoahInfoData._syncRoot)
            return growattDeviceNoahInfoData.FirstOrDefault(x => x.DeviceSn == deviceSn);
    }

    public List<DeviceNoahInfoData> GrowattGetNoahInfoDatas()
    {
        lock (growattDeviceNoahInfoData._syncRoot)
            return growattDeviceNoahInfoData.ToList();
    }

    public DeviceNoahLastData? GrowattGetNoahLastDataPerDevice(string? deviceSn)
    {
        lock (growattDeviceNoahLastData._syncRoot)
            return growattDeviceNoahLastData.Where(x => x.deviceSn == deviceSn).OrderByDescending(x => x.TS).FirstOrDefault();
    }

    public List<DeviceNoahLastData> GrowattGetNoahLastDatas()
    {
        lock (growattDeviceNoahLastData._syncRoot)
            return growattDeviceNoahLastData.ToList();
    }

    public List<DeviceNoahInfoData> GrowattLatestNoahInfoDatas()
    {
        List<DeviceNoahInfoData> result = [];
        lock (growattDeviceNoahInfoData._syncRoot)
        {
            foreach (var device in growattDevices.Where(x => x.DeviceType == "noah"))
            {
                var infoData = growattDeviceNoahInfoData.Where(x => x.DeviceSn == device.DeviceSn).OrderByDescending(x => x.TS).FirstOrDefault();
                if (infoData != null)
                    result.Add(infoData);
            }
        }

        return result;
    }

    public List<DeviceNoahLastData> GrowattLatestNoahLastDatas()
    {
        List<DeviceNoahLastData> result = [];
        lock (growattDeviceNoahLastData._syncRoot)
        {
            foreach (var device in growattDevices.Where(x => x.DeviceType == "noah"))
            {
                var lastData = growattDeviceNoahLastData.Where(x => x.deviceSn == device.DeviceSn).OrderByDescending(x => x.TS).FirstOrDefault();
                if (lastData != null)
                    result.Add(lastData);
            }
        }

        return result;
    }

    public async Task GrowattInverterMaxSetPower(int value)
    {
        var device = GrowattGetDeviceLists().FirstOrDefault(x => x.DeviceType == "min");
        if (device != null)
        {
            var query = new DeviceNoahSetPowerQuery
            {
                DeviceType = device.DeviceType,
                DeviceSn = device.DeviceSn,
                Value = value,
                Force = true
            };

            await growattApiClient.ExecuteDeviceQueryAsync(query);
        }
    }

    public async Task GrowattInvokeBattPriorityDeviceNoah()
    {
        await apiService.GrowattInvokeBattPriorityDeviceNoah();
    }

    public async Task GrowattInvokeClearDeviceNoahTimeSegments()
    {
        await apiService.GrowattInvokeClearDeviceNoahTimeSegments();
    }

    public async Task GrowattInvokeLoadPriorityDeviceNoah()
    {
        await apiService.GrowattInvokeLoadPriorityDeviceNoah();
    }

    public async Task GrowattInvokeRefreshDeviceList()
    {
        await GrowattQueryDevice(true);
        await GrowattQueryDeviceNoahInfo(true);
        await GrowattQueryDeviceNoahLastData(true);
    }

    public async Task GrowattInvokeRefreshNoahs()
    {
        await GrowattQueryDeviceNoahInfo(true);
        await GrowattQueryDeviceNoahLastData(true);
    }

    public async Task GrowattInvokeRefreshNoahsLastData()
    {
        await GrowattQueryDeviceNoahLastData(true);
    }

    public async Task GrowattQueryDevice(bool force = false)
    {
        await growattDeviceQueryQueueWatchdog.EnqueueAsync(new DeviceListQuery() { Force = force });
    }

    public async Task GrowattQueryDeviceMinInfo(bool force = false)
    {
        if (apiService.CurrentState.IsGrowattOnline)
        {
            var item = new DeviceMinInfoDataQuery()
            {
                Force = force,
                DeviceType = "Min",
                DeviceSn = string.Join(",", GrowattGetDeviceMinSnList()),
            };

            await growattDeviceQueryQueueWatchdog.EnqueueAsync(item);
        }
    }

    public async Task GrowattQueryDeviceMinLastData(bool force = false)
    {
        if (apiService.CurrentState.IsGrowattOnline)
        {
            var item = new DeviceMinLastDataQuery()
            {
                DeviceType = "Min",
                DeviceSn = string.Join(",", GrowattGetDeviceMinSnList()),
                Force = force
            };

            await growattDeviceQueryQueueWatchdog.EnqueueAsync(item);
        }
    }

    public async Task GrowattQueryDeviceNoahInfo(bool force = false)
    {
        var item = new DeviceNoahInfoDataQuery()
        {
            Force = force,
            DeviceType = "noah",
                DeviceSn = string.Join(",", GrowattGetDeviceNoahSnList()),
        };

        await growattDeviceQueryQueueWatchdog.EnqueueAsync(item);
    }

    public async Task GrowattQueryDeviceNoahLastData(bool force = false)
    {
        if (apiService.CurrentState.IsGrowattOnline)
        {
            var item = new DeviceNoahLastDataQuery()
            {
                DeviceType = "noah",
                DeviceSn = string.Join(",", GrowattGetDeviceNoahSnList()),
                Force = force
            };

            await growattDeviceQueryQueueWatchdog.EnqueueAsync(item);
        }
    }

    public async Task LoadDataFromDatabaseAsync(ApplicationDbContext dbContext)
    {
        var devices = await dbContext.GrowattDevices.ToListAsync();
        growattDevices.Clear();
        foreach (var device in devices)
        {
            growattDevices.Add(device);
        }

        var deviceNoahInfoList = await dbContext.GrowattDeviceNoahInfoData.ToListAsync();
        growattDeviceNoahInfoData.Clear();
        foreach (var info in deviceNoahInfoList)
        {
            growattDeviceNoahInfoData.Add(info);
        }

        var deviceNoahLastDataList = await dbContext.GrowattDeviceNoahLastData.ToListAsync();
        growattDeviceNoahLastData.Clear();
        foreach (var lastData in deviceNoahLastDataList)
        {
            growattDeviceNoahLastData.Add(lastData);
        }

        var deviceMinInfoList = await dbContext.GrowattDeviceMinInfoData.ToListAsync();
        growattDeviceMinInfoData.Clear();
        foreach (var info in deviceMinInfoList)
        {
            growattDeviceMinInfoData.Add(info);
        }

        var deviceMinLastDataList = await dbContext.GrowattDeviceMinLastData.ToListAsync();
        growattDeviceMinLastData.Clear();
        foreach (var lastData in deviceMinLastDataList)
        {
            growattDeviceMinLastData.Add(lastData);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        timer.Dispose();
    }

    public static IEnumerable<TickMark> GenerateTickTickMarks(int start, int end, int step)
    {
        var tickMarks = new List<TickMark>();
        for (int i = start; i <= end; i += step)
        {
            tickMarks.Add(new TickMark { Label = i.ToString(), Value = i.ToString() });
        }

        return tickMarks;
    }

    public int GrowattGetBatteryLevel()
    {
        var lastData = GrowattLatestNoahLastDatas().FirstOrDefault();
        return lastData?.totalBatteryPackSoc ?? 0;
    }

    public int GrowattGetBatteryMaxSoc()
    {
        return (int)(GrowattLatestNoahLastDatas().Any() ? GrowattLatestNoahLastDatas().Average(x => x.chargeSocLimit) : 100);
    }

    public static bool GrowattNearofBatterySocEmpty(DeviceNoahLastData deviceNoahLastData)
    {
        return Math.Abs(deviceNoahLastData.totalBatteryPackSoc - deviceNoahLastData.dischargeSocLimit) < 2;
    }

    public static bool GrowattNearofBatterySocFull(DeviceNoahLastData deviceNoahLastData)
    {
        return deviceNoahLastData.totalBatteryPackChargingStatus == 0
            ? Math.Abs(deviceNoahLastData.totalBatteryPackSoc - deviceNoahLastData.chargeSocLimit) < 6
            : false;
    }

    private List<string> GrowattGetDeviceMinSnList()
    {
        lock (growattDevices._syncRoot)
        {
            return growattDevices.Where(x => x.DeviceType == "min").Select(x => x.DeviceSn).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()!;
        }
    }

    private List<string> GrowattGetDeviceNoahSnList()
    {
        lock (growattDevices._syncRoot)
        {
            return growattDevices.Where(x => x.DeviceType == "noah").Select(x => x.DeviceSn).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()!;
        }
    }

    private async void TimerCallback(object? state)
    {
        if (disposed || Interlocked.Exchange(ref timerCallbackRunning, 1) == 1)
        {
            return;
        }

        try
        {
            await GrowattQueryDevice();
            await GrowattQueryDeviceNoahInfo();
            await GrowattQueryDeviceNoahLastData();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GrowattApiService timer callback.");
        }
        finally
        {
            Interlocked.Exchange(ref timerCallbackRunning, 0);
        }
    }

}
