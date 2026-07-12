using EnergyAutomate.Growatt;

namespace EnergyAutomate.Services.CodeFactory;

public interface IEnergyScriptFactory<out TEvent>
{
    EnergyScriptSchema? ScriptSchema { get; }
    EnergyScriptPeriod? ScriptPeriod { get; }
    TEvent Event { get; }
    CancellationToken CancellationToken { get; }

    object? GetGlobalParameter(string name);
    object? GetRuntimeState(string name);
    Task UpdateRuntimeStateAsync(string name, object value, bool saveChanges = false);

    void Trace(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}

public interface IEnergyCalculationScriptFactory : IEnergyScriptFactory<EnergyCalculationEvent>
{
    IReadOnlyList<TibberRealTimeMeasurement> Measurements { get; }

    int GetAverageConsumption(int seconds);
    int GetAverageProduction(int seconds);

    Task SetCalculatedValueAsync(string name, object value);
}

public interface IEnergyAdjustmentScriptFactory : IEnergyScriptFactory<EnergyAdjustmentEvent>
{
    IReadOnlyList<DeviceList> OnlineNoahDevices { get; }

    int AveragePower { get; }
    int AveragePowerOffset { get; }
    int AveragePowerHysteresis { get; }
    bool AutoMode { get; }
    bool RestrictionMode { get; }
    bool BatteryPriorityMode { get; }

    Task SetPowerAsync(string deviceSn, int powerValue);
    Task SetPowerForAllNoahsAsync(int totalPower);
    Task ClearPowerRequestsAsync();

    Task SetBatteryPriorityAsync();
    Task SetLoadPriorityAsync(int powerValue = 0);
    Task ClearTimeSegmentsAsync();

    bool CheckCondition(string conditionKey);
    void SetActiveCondition(string conditionKey);
}

public interface IEnergyDistributionScriptFactory : IEnergyScriptFactory<EnergyDistributionEvent>
{
    IReadOnlyList<DeviceList> OnlineNoahDevices { get; }

    int MaxPower { get; }

    int GetCurrentCommittedPower(string deviceSn);
    int GetCurrentRequestedPower(string deviceSn);
    int GetSoc(string deviceSn);

    bool IsBatteryEmpty(string deviceSn);
    bool IsBatteryFull(string deviceSn);

    Task RequestPowerAsync(string deviceSn, int powerValue);
    Task RequestPowerDistributionAsync(IReadOnlyDictionary<string, int> devicePowerValues);
}

public interface IEnergyDistributionManagerScriptFactory : IEnergyScriptFactory<EnergyDistributionManagerEvent>
{
    int MaxPower { get; }

    Task RequestPowerAsync(string deviceSn, int powerValue);
    Task RequestPowerDistributionAsync(IReadOnlyDictionary<string, int> devicePowerValues);
    Task RunDirectDistributionAsync();
    Task RunWeightedDistributionAsync();
    Task RunLoadBalancingAsync();
}
