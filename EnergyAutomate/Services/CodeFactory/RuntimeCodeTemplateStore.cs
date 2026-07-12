using System.Collections.Concurrent;

namespace EnergyAutomate.Services.CodeFactory;

public sealed class RuntimeCodeTemplateStore
{
    private readonly IReadOnlyList<CodeTemplateDefinition> _defaults;
    private readonly ConcurrentDictionary<string, string> _runtimeCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<RuntimeCodeTemplateStore> _logger;
    private readonly DatabaseCodeTemplateStore _databaseStore;

    public RuntimeCodeTemplateStore(ICodeTemplateProvider templateProvider, DatabaseCodeTemplateStore databaseStore, ILogger<RuntimeCodeTemplateStore> logger)
    {
        ArgumentNullException.ThrowIfNull(templateProvider);
        ArgumentNullException.ThrowIfNull(databaseStore);
        ArgumentNullException.ThrowIfNull(logger);

        _defaults = templateProvider.GetTemplates();
        _databaseStore = databaseStore;
        _logger = logger;
    }

    public IReadOnlyList<CodeTemplateViewModel> GetTemplates()
    {
        return _defaults
            .Select(definition => CodeTemplateViewModel.FromDefinition(
                definition,
                _runtimeCode.TryGetValue(definition.Key, out var currentCode) ? currentCode : null))
            .OrderBy(template => template.Topic)
            .ThenBy(template => template.DisplayName)
            .ToList();
    }

    public CodeTemplateViewModel? GetTemplate(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var definition = _defaults.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
        if (definition is null)
        {
            return null;
        }

        return CodeTemplateViewModel.FromDefinition(
            definition,
            _runtimeCode.TryGetValue(definition.Key, out var currentCode) ? currentCode : null);
    }

    public CodeTemplateViewModel SaveTemplate(string key, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(code);

        var definition = _defaults.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Template '{key}' was not found.");

        _runtimeCode[definition.Key] = code;
        _logger.LogTrace("Runtime code template {TemplateKey} saved in memory", definition.Key);

        // Save to database asynchronously in the background
        _ = Task.Run(async () =>
        {
            try
            {
                await _databaseStore.SaveTemplateAsync(key, code, changeNotes: null, modifiedBy: "system");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save template {TemplateKey} to database", key);
            }
        });

        return CodeTemplateViewModel.FromDefinition(definition, code);
    }

    public CodeTemplateViewModel ResetTemplate(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var definition = _defaults.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Template '{key}' was not found.");

        _runtimeCode.TryRemove(definition.Key, out _);
        _logger.LogTrace("Runtime code template {TemplateKey} reset to default", definition.Key);

        return CodeTemplateViewModel.FromDefinition(definition);
    }

    /// <summary>
    /// Initialize runtime templates from database. Call this during application startup.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Initialize database with defaults if needed
            await _databaseStore.InitializeTemplatesAsync(_defaults, cancellationToken);

            // Load all templates from database into runtime memory
            var templates = await _databaseStore.GetAllTemplatesAsync(cancellationToken);
            foreach (var template in templates)
            {
                _runtimeCode[template.Key] = template.Code;
            }

            _logger.LogInformation("Runtime template store initialized with {Count} templates from database", templates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize runtime template store");
            throw;
        }
    }

    /// <summary>
    /// Save template asynchronously with change tracking
    /// </summary>
    public async Task<CodeTemplateViewModel> SaveTemplateAsync(string key, string code, string? changeNotes = null, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(code);

        var definition = _defaults.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Template '{key}' was not found.");

        // Save to memory immediately
        _runtimeCode[definition.Key] = code;

        // Save to database
        await _databaseStore.SaveTemplateAsync(key, code, changeNotes, modifiedBy, cancellationToken);

        _logger.LogInformation("Template {TemplateKey} saved by {User}", key, modifiedBy ?? "system");

        return CodeTemplateViewModel.FromDefinition(definition, code);
    }

    /// <summary>
    /// Get history for a template
    /// </summary>
    public async Task<IReadOnlyList<CodeTemplateHistoryViewModel>> GetHistoryAsync(string key, CancellationToken cancellationToken = default)
    {
        var history = await _databaseStore.GetHistoryAsync(key, cancellationToken);
        return history
            .Select(h => new CodeTemplateHistoryViewModel
            {
                Version = h.Version,
                CreatedAt = h.CreatedAt,
                CreatedBy = h.CreatedBy,
                ChangeNotes = h.ChangeNotes
            })
            .ToList();
    }

    /// <summary>
    /// Rollback to a specific version
    /// </summary>
    public async Task<CodeTemplateViewModel> RollbackAsync(string key, int targetVersion, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var template = await _databaseStore.RollbackAsync(key, targetVersion, modifiedBy, cancellationToken);

        // Update runtime memory
        _runtimeCode[key] = template.Code;

        var definition = _defaults.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
        _logger.LogInformation("Template {TemplateKey} rolled back to version {Version}", key, targetVersion);

        return CodeTemplateViewModel.FromDefinition(definition, template.Code);
    }
}
