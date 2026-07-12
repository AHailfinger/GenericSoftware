public static class LoggingExtensions
{
    public static ILoggingBuilder AddCustomLogger(this ILoggingBuilder builder, LogLevel logLevel = LogLevel.Information, Func<string, bool>? categoryFilter = null)
    {
        builder.Services.AddSingleton<CustomLoggerProvider>(sp => new CustomLoggerProvider(sp, logLevel, categoryFilter));
        builder.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<CustomLoggerProvider>());
        return builder;
    }
}
