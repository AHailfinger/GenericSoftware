using EnergyAutomate.Logging;

public class CustomLogger : ILogger
{
    private readonly string _name;
    private readonly CustomLoggerProvider _provider;
    private Func<string, bool>? _categoryFilter;

    public CustomLogger(string name, CustomLoggerProvider provider, Func<string, bool>? categoryFilter)
    {
        _name = name;
        _provider = provider;
        _categoryFilter = categoryFilter;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _provider.LogLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string>? formatter)
    {
        if (_categoryFilter == null || _categoryFilter != null && _categoryFilter(_name))
        {
            _provider.LogMessagesAdd(new CustomTraceLog
            {
                Category = _name,
                LogLevel = logLevel,
                EventId = eventId,
                Message = state?.ToString(),
                Exception = exception?.ToString(),
                TS = DateTimeOffset.UtcNow
            });
        }
    }

}

