namespace EnergyAutomate.Services.CodeFactory;

public sealed record EnergyCalculationEvent(
    DateTimeOffset Timestamp,
    int Power,
    int? PowerProduction,
    int TotalPower);

public sealed record EnergyAdjustmentEvent(
    DateTimeOffset Timestamp,
    int TotalPower,
    int PowerAvgConsumption,
    int PowerAvgProduction,
    bool IsGrowattOnline,
    bool IsExpensiveRestrictionMode);

public sealed record EnergyDistributionEvent(
    DateTimeOffset Timestamp,
    int RequestedTotalPower,
    string Reason);

public sealed record EnergyDistributionManagerEvent(
    DateTimeOffset Timestamp,
    IReadOnlyList<DeviceList> Devices,
    int TotalPower,
    bool PrioritizeHighSoc);
