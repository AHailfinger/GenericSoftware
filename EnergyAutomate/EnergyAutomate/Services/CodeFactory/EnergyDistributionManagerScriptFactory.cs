namespace EnergyAutomate.Services.CodeFactory;

public sealed class EnergyDistributionManagerScriptFactory : EnergyScriptFactoryBase<EnergyDistributionManagerEvent>, IEnergyDistributionManagerScriptFactory
{
    private readonly EnergyAutomateService _apiService;
    private readonly ApiQueueWatchdog<IDeviceQuery> _queueWatchdog;
    private readonly RuntimeCodeTemplateExecutor _codeTemplateExecutor;
    private readonly ILogger _logger;

    public EnergyDistributionManagerScriptFactory(
        EnergyDistributionManagerEvent eventData,
        EnergyAutomateService apiService,
        ApiQueueWatchdog<IDeviceQuery> queueWatchdog,
        RuntimeCodeTemplateExecutor codeTemplateExecutor,
        ILogger logger,
        CancellationToken cancellationToken = default)
        : base(eventData, logger, cancellationToken)
    {
        _apiService = apiService;
        _queueWatchdog = queueWatchdog;
        _codeTemplateExecutor = codeTemplateExecutor;
        _logger = logger;
    }

    public int MaxPower => _apiService.ApiSettingMaxPower;

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
        var totalPower = devicePowerValues.Values.Sum();
        var eventData = new EnergyDistributionEvent(Event.Timestamp, totalPower, "DistributionManager.RequestPowerDistribution");
        var factory = new EnergyDistributionScriptFactory(eventData, _apiService, _queueWatchdog, Event.Devices, _logger);
        await _codeTemplateExecutor.ExecuteAsync(_apiService.ActiveDistributionTemplateKey, factory);
    }

    public async Task RunDirectDistributionAsync()
    {
        var device = Event.Devices.FirstOrDefault();
        if (device?.DeviceSn is null)
        {
            Trace("No device available for direct distribution.");
            return;
        }

        await RequestPowerAsync(device.DeviceSn, Event.TotalPower);
    }

    public async Task RunWeightedDistributionAsync()
    {
        var devices = Event.Devices.Where(device => device.DeviceSn is not null).ToList();
        if (devices.Count == 0)
        {
            Trace("No devices available for weighted distribution.");
            return;
        }

        var maxPowerPerDevice = MaxPower / devices.Count;
        var weights = devices.ToDictionary(
            device => device.DeviceSn!,
            device => Math.Max(10, 100 + (int)(device.PowerValueSolar / 10) + (device.PowerValueBatteryPower > 0 ? (int)(device.PowerValueBatteryPower / 20) : 0)));
        var totalWeight = weights.Values.Sum();
        var remainingPower = Event.TotalPower;
        var distribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var device in devices)
        {
            var value = totalWeight == 0
                ? Event.TotalPower / devices.Count
                : Event.TotalPower * weights[device.DeviceSn!] / totalWeight;
            value = Math.Min(value, maxPowerPerDevice);
            value = Math.Min(value, remainingPower);
            distribution[device.DeviceSn!] = Math.Max(0, value);
            remainingPower -= value;
        }

        await RequestPowerDistributionAsync(distribution);
    }

    public async Task RunLoadBalancingAsync()
    {
        if (Event.Devices.Count <= 1)
        {
            Trace("Load balancing not possible with 1 or 0 devices.");
            return;
        }

        var maxSocDevice = Event.Devices.OrderByDescending(device => device.Soc).First();
        var minSocDevice = Event.Devices.OrderBy(device => device.Soc).First();
        if (maxSocDevice.DeviceSn is null || minSocDevice.DeviceSn is null)
        {
            return;
        }

        if (maxSocDevice.Soc - minSocDevice.Soc < 5)
        {
            Trace("Load balancing skipped because SoC difference is below threshold.");
            return;
        }

        var maxPowerPerDevice = MaxPower / Event.Devices.Count;
        var actualShift = Math.Min(10, Math.Min(maxPowerPerDevice - maxSocDevice.PowerValueCommited, minSocDevice.PowerValueCommited));
        if (actualShift <= 0)
        {
            Trace("Load balancing skipped because no power shift is possible.");
            return;
        }

        await RequestPowerAsync(minSocDevice.DeviceSn, minSocDevice.PowerValueCommited - actualShift);
        await RequestPowerAsync(maxSocDevice.DeviceSn, maxSocDevice.PowerValueCommited + actualShift);
    }
}
