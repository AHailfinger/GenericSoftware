namespace EnergyAutomate.Services.CodeFactory;

public sealed class EnergyCalculationScriptFactory : EnergyScriptFactoryBase<EnergyCalculationEvent>, IEnergyCalculationScriptFactory
{
    private readonly TibberRealTimeMeasurement _measurement;
    private readonly IReadOnlyList<TibberRealTimeMeasurement> _measurements;

    public EnergyCalculationScriptFactory(
        EnergyCalculationEvent eventData,
        TibberRealTimeMeasurement measurement,
        IReadOnlyList<TibberRealTimeMeasurement> measurements,
        ILogger logger,
        CancellationToken cancellationToken = default)
        : base(eventData, logger, cancellationToken)
    {
        _measurement = measurement;
        _measurements = measurements;
    }

    public IReadOnlyList<TibberRealTimeMeasurement> Measurements => _measurements;

    public int GetAverageConsumption(int seconds)
    {
        if (seconds <= 0)
        {
            return Event.Power;
        }

        var from = Event.Timestamp.UtcDateTime.AddSeconds(-seconds);
        var values = _measurements
            .Where(measurement => measurement.TS.UtcDateTime >= from && measurement.Power > 0)
            .Select(measurement => measurement.Power)
            .Append(Event.Power > 0 ? Event.Power : 0)
            .Where(value => value > 0)
            .ToList();

        return values.Count == 0 ? 0 : (int)values.Average();
    }

    public int GetAverageProduction(int seconds)
    {
        if (seconds <= 0)
        {
            return Event.PowerProduction ?? 0;
        }

        var from = Event.Timestamp.UtcDateTime.AddSeconds(-seconds);
        var values = _measurements
            .Where(measurement => measurement.TS.UtcDateTime >= from && (measurement.PowerProduction ?? 0) > 0)
            .Select(measurement => measurement.PowerProduction ?? 0)
            .Append((Event.PowerProduction ?? 0) > 0 ? Event.PowerProduction!.Value : 0)
            .Where(value => value > 0)
            .ToList();

        return values.Count == 0 ? 0 : (int)values.Average();
    }

    public Task SetCalculatedValueAsync(string name, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        switch (name)
        {
            case "PowerAvgConsumption":
                _measurement.PowerAvgConsumption = Convert.ToInt32(value);
                break;
            case "PowerAvgProduction":
                _measurement.PowerAvgProduction = Convert.ToInt32(value);
                break;
            case "NetPower":
            case "PowerProduction":
            case "PowerConsumption":
                return UpdateRuntimeStateAsync(name, value);
            default:
                return UpdateRuntimeStateAsync(name, value);
        }

        return Task.CompletedTask;
    }
}
