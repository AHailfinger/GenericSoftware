using EnergyAutomate.Definitions;
using EnergyAutomate.Extentions;

namespace EnergyAutomate.Services.CodeFactory;

public sealed class EnergyAdjustmentScriptFactory : EnergyScriptFactoryBase<EnergyAdjustmentEvent>, IEnergyAdjustmentScriptFactory
{
    private readonly EnergyAutomateService _apiService;
    private readonly ApiQueueWatchdog<IDeviceQuery> _queueWatchdog;
    private readonly IReadOnlyList<DeviceList> _onlineNoahDevices;

    public EnergyAdjustmentScriptFactory(
        EnergyAdjustmentEvent eventData,
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

    public int AveragePower => _apiService.ApiSettingAvgPower;

    public int AveragePowerOffset => _apiService.ApiSettingAvgPowerOffset;

    public int AveragePowerHysteresis => _apiService.ApiSettingAvgPowerHysteresis;

    public bool AutoMode => _apiService.ApiSettingAutoMode;

    public bool RestrictionMode => _apiService.ApiSettingRestrictionMode;

    public bool BatteryPriorityMode => _apiService.ApiSettingBatteryPriorityMode;

    public async Task SetPowerAsync(string deviceSn, int powerValue)
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

    public async Task SetPowerForAllNoahsAsync(int totalPower)
    {
        if (_onlineNoahDevices.Count == 0)
        {
            Trace("No online Noah devices available for SetPowerForAllNoahsAsync.");
            return;
        }

        var powerPerDevice = totalPower / _onlineNoahDevices.Count;
        foreach (var device in _onlineNoahDevices)
        {
            await SetPowerAsync(device.DeviceSn ?? string.Empty, powerPerDevice);
        }
    }

    public Task ClearPowerRequestsAsync()
    {
        return _queueWatchdog.ClearAsync();
    }

    public async Task SetBatteryPriorityAsync()
    {
        foreach (var device in _onlineNoahDevices)
        {
            await _queueWatchdog.EnqueueAsync(new DeviceNoahSetTimeSegmentQuery
            {
                Force = true,
                DeviceSn = device.DeviceSn,
                DeviceType = "noah",
                Type = "1",
                StartTime = "00:00",
                EndTime = "23:59",
                Mode = "1",
                Power = "0",
                Enable = "1"
            });
        }
    }

    public async Task SetLoadPriorityAsync(int powerValue = 0)
    {
        foreach (var device in _onlineNoahDevices)
        {
            await _queueWatchdog.EnqueueAsync(new DeviceNoahSetTimeSegmentQuery
            {
                Force = true,
                DeviceSn = device.DeviceSn,
                DeviceType = "noah",
                Type = "1",
                StartTime = "08:00",
                EndTime = "23:59",
                Mode = "0",
                Power = powerValue.ToString(),
                Enable = "1"
            });
        }
    }

    public async Task ClearTimeSegmentsAsync()
    {
        foreach (var device in _onlineNoahDevices)
        {
            await _queueWatchdog.EnqueueAsync(new DeviceNoahSetTimeSegmentQuery
            {
                Force = true,
                DeviceSn = device.DeviceSn,
                DeviceType = "noah",
                Type = "1",
                StartTime = "00:00",
                EndTime = "23:59",
                Mode = "0",
                Power = "0",
                Enable = "0"
            });
        }
    }

    public bool CheckCondition(string conditionKey)
    {
        return string.Equals(_apiService.CurrentState.ActiveRTMCondition, conditionKey, StringComparison.OrdinalIgnoreCase);
    }

    public void SetActiveCondition(string conditionKey)
    {
        _apiService.CurrentState.ActiveRTMCondition = conditionKey;
        _apiService.ApiSettingAvgPowerAdjustmentTraceValues.AddOrUpdate(new APiTraceValue
        {
            Index = 51,
            Key = "ActiveRTMCondition",
            Value = conditionKey
        });
    }
}
