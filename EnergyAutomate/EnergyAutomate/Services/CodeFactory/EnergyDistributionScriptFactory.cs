namespace EnergyAutomate.Services.CodeFactory;

public sealed class EnergyDistributionScriptFactory : EnergyScriptFactoryBase<EnergyDistributionEvent>, IEnergyDistributionScriptFactory
{
    private readonly EnergyAutomateService _apiService;
    private readonly ApiQueueWatchdog<IDeviceQuery> _queueWatchdog;
    private readonly IReadOnlyList<DeviceList> _onlineNoahDevices;

    public EnergyDistributionScriptFactory(
        EnergyDistributionEvent eventData,
        EnergyAutomateService apiService,
        ApiQueueWatchdog<IDeviceQuery> queueWatchdog,
        IReadOnlyList<DeviceList> onlineNoahDevices,
        ILogger logger,
        CancellationToken cancellationToken = default)
        : base(eventData, logger, cancellationToken)
    {
        _apiService = apiService;
        _queueWatchdog = queueWatchdog;
        _onlineNoahDevices = onlineNoahDevices;
    }

    public IReadOnlyList<DeviceList> OnlineNoahDevices => _onlineNoahDevices;

    public int MaxPower => _apiService.ApiSettingMaxPower;

    public int GetCurrentCommittedPower(string deviceSn)
    {
        return _onlineNoahDevices.FirstOrDefault(device => device.DeviceSn == deviceSn)?.PowerValueCommited ?? 0;
    }

    public int GetCurrentRequestedPower(string deviceSn)
    {
        return _onlineNoahDevices.FirstOrDefault(device => device.DeviceSn == deviceSn)?.PowerValueRequested ?? 0;
    }

    public int GetSoc(string deviceSn)
    {
        return _onlineNoahDevices.FirstOrDefault(device => device.DeviceSn == deviceSn)?.Soc ?? 0;
    }

    public bool IsBatteryEmpty(string deviceSn)
    {
        return _onlineNoahDevices.FirstOrDefault(device => device.DeviceSn == deviceSn)?.IsBatteryEmpty ?? true;
    }

    public bool IsBatteryFull(string deviceSn)
    {
        return _onlineNoahDevices.FirstOrDefault(device => device.DeviceSn == deviceSn)?.IsBatteryFull ?? false;
    }

    public async Task RequestPowerAsync(string deviceSn, int powerValue)
    {
        if (string.IsNullOrWhiteSpace(deviceSn))
        {
            return;
        }

        await _queueWatchdog.EnqueueAsync(new DeviceNoahSetPowerQuery
        {
            DeviceType = "noah",
            DeviceSn = deviceSn,
            Value = powerValue,
            Force = true,
            TS = Event.Timestamp
        });
    }

    public async Task RequestPowerDistributionAsync(IReadOnlyDictionary<string, int> devicePowerValues)
    {
        foreach (var item in devicePowerValues)
        {
            await RequestPowerAsync(item.Key, item.Value);
        }
    }
}
