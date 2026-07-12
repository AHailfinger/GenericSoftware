using EnergyAutomate.Logging;
using System.Collections.Concurrent;

public class CustomLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, ILogger> _loggers = new ConcurrentDictionary<string, ILogger>();
    private Func<string, bool>? _categoryFilter;
    private readonly object _logLock = new();

    public CustomLoggerProvider(IServiceProvider serviceProvider, LogLevel logLevel, Func<string, bool>? categoryFilter)
    {
        ServiceProvider = serviceProvider;
        _categoryFilter = categoryFilter;
        LogLevel = logLevel;
    }

    private List<CustomTraceLog> LogMessages { get; set; } = [];

    public bool LogMessagesAny => LogMessages.Any();

    public List<string> GetLogCategories()
    {
        lock (_logLock)
        {
            // Return a copy of the log messages to avoid concurrent modification issues
            return LogMessages.Where(x => x.Category != null).Select(s => s.Category!.Contains(".") ? s.Category.Split(".").Last() : s.Category).GroupBy(x => x).Select(s => s.Key).OrderBy(o => o).ToList();
        }
    }

    public List<CustomTraceLog> GetLogMessages(string? category = null)
    {
        lock (_logLock)
        {
            if (string.IsNullOrEmpty(category))
            {
                // Return all log messages if no category is specified
                return LogMessages.OrderByDescending(x => x.TS).ToList();
            }

            // Return a copy of the log messages to avoid concurrent modification issues
            return LogMessages.Where(x => x.Category == category || x.Category!.Split(".").Last() == category).OrderByDescending(x => x.TS).Take(50).ToList();
        }
    }

    public void LogMessagesAdd(CustomTraceLog item)
    {
        lock (_logLock)
        {
            LogMessages.Add(item);

            if (LogMessages.Where(x => x.Category == item.Category).ToList().Count > 50 )
            {
                var result = LogMessages.Where(x => x.Category == item.Category).OrderByDescending(o => o.TS).Skip(50).ToList();
                result.ForEach(x => LogMessages.Remove(x));
            }
        }

        LogMessagesChanged?.Invoke();
    }

    public event Action? LogMessagesChanged;

    public LogLevel LogLevel { get; set; } = LogLevel.Trace;

    public IServiceProvider? ServiceProvider { get; set; }

    public ILoggerFactory? LoggerFactory => ServiceProvider?.GetRequiredService<ILoggerFactory>();

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new CustomLogger(name, this, _categoryFilter));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
