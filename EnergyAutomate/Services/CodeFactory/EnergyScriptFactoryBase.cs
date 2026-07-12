namespace EnergyAutomate.Services.CodeFactory;

public abstract class EnergyScriptFactoryBase<TEvent> : IEnergyScriptFactory<TEvent>
{
    private readonly Dictionary<string, object?> _runtimeState = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger _logger;

    protected EnergyScriptFactoryBase(TEvent eventData, ILogger logger, CancellationToken cancellationToken = default)
    {
        Event = eventData;
        _logger = logger;
        CancellationToken = cancellationToken;
    }

    public EnergyScriptSchema? ScriptSchema { get; init; } = new(Guid.Parse("5E31B420-2C4D-46F4-9B7C-BF58F3BC78EE"), "EnergyAutomate", "Default EnergyAutomate runtime schema");

    public EnergyScriptPeriod? ScriptPeriod { get; init; } = new(Guid.Parse("E949CA9A-5BA4-4959-A5D7-EE73344915BB"), "Default", null, null);

    public TEvent Event { get; }

    public CancellationToken CancellationToken { get; }

    public virtual object? GetGlobalParameter(string name) => null;

    public object? GetRuntimeState(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _runtimeState.TryGetValue(name, out var value) ? value : null;
    }

    public Task UpdateRuntimeStateAsync(string name, object value, bool saveChanges = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _runtimeState[name] = value;
        return Task.CompletedTask;
    }

    public void Trace(string message) => _logger.LogTrace("{Message}", message);

    public void Info(string message) => _logger.LogInformation("{Message}", message);

    public void Warning(string message) => _logger.LogWarning("{Message}", message);

    public void Error(string message) => _logger.LogError("{Message}", message);
}
