using EnergyAutomate.Data;
using EnergyAutomate.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EnergyAutomate.Services.CodeFactory;

/// <summary>
/// Manages code template persistence and versioning in the database
/// </summary>
public class DatabaseCodeTemplateStore
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseCodeTemplateStore> _logger;

    public DatabaseCodeTemplateStore(ApplicationDbContext context, ILogger<DatabaseCodeTemplateStore> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Initialize templates from embedded defaults if they don't exist in database
    /// </summary>
    public async Task InitializeTemplatesAsync(IEnumerable<CodeTemplateDefinition> defaults, CancellationToken cancellationToken = default)
    {
        foreach (var defaultTemplate in defaults)
        {
            var existing = await _context.CodeTemplates
                .FirstOrDefaultAsync(t => t.Key == defaultTemplate.Key, cancellationToken);

            if (existing != null)
            {
                continue; // Template already initialized
            }

            var template = new CodeTemplate
            {
                Key = defaultTemplate.Key,
                Code = defaultTemplate.DefaultCode,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.CodeTemplates.Add(template);

            var history = new CodeTemplateHistory
            {
                CodeTemplate = template,
                Version = 1,
                Code = defaultTemplate.DefaultCode,
                CreatedAt = DateTime.UtcNow,
                ChangeNotes = "Initial version from embedded template",
                CodeHash = ComputeCodeHash(defaultTemplate.DefaultCode)
            };

            _context.CodeTemplateHistories.Add(history);

            _logger.LogInformation("Initialized template {TemplateKey}", defaultTemplate.Key);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Get all active templates from database
    /// </summary>
    public async Task<IReadOnlyList<CodeTemplate>> GetAllTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CodeTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Key)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get a specific template by key
    /// </summary>
    public async Task<CodeTemplate?> GetTemplateAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await _context.CodeTemplates
            .FirstOrDefaultAsync(t => t.Key == key && t.IsActive, cancellationToken);
    }

    /// <summary>
    /// Save a new version of the template
    /// </summary>
    public async Task<CodeTemplate> SaveTemplateAsync(string key, string code, string? changeNotes = null, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(code);

        var template = await _context.CodeTemplates
            .FirstOrDefaultAsync(t => t.Key == key, cancellationToken)
            ?? throw new InvalidOperationException($"Template '{key}' not found");

        var newHash = ComputeCodeHash(code);
        var oldHash = ComputeCodeHash(template.Code);

        // Don't save if code hasn't changed
        if (newHash == oldHash)
        {
            _logger.LogTrace("Code template {TemplateKey} has no changes", key);
            return template;
        }

        template.Code = code;
        template.Version++;
        template.LastModifiedAt = DateTime.UtcNow;
        template.LastModifiedBy = modifiedBy;

        var history = new CodeTemplateHistory
        {
            CodeTemplate = template,
            Version = template.Version,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = modifiedBy,
            ChangeNotes = changeNotes,
            CodeHash = newHash
        };

        _context.CodeTemplateHistories.Add(history);
        _context.CodeTemplates.Update(template);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Template {TemplateKey} saved as version {Version} by {User}", key, template.Version, modifiedBy ?? "system");

        return template;
    }

    /// <summary>
    /// Get the full history for a template
    /// </summary>
    public async Task<IReadOnlyList<CodeTemplateHistory>> GetHistoryAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await _context.CodeTemplateHistories
            .Include(h => h.CodeTemplate)
            .Where(h => h.CodeTemplate.Key == key)
            .OrderByDescending(h => h.Version)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get a specific version from history
    /// </summary>
    public async Task<CodeTemplateHistory?> GetVersionAsync(string key, int version, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return await _context.CodeTemplateHistories
            .Include(h => h.CodeTemplate)
            .FirstOrDefaultAsync(h => h.CodeTemplate.Key == key && h.Version == version, cancellationToken);
    }

    /// <summary>
    /// Rollback to a specific version
    /// </summary>
    public async Task<CodeTemplate> RollbackAsync(string key, int targetVersion, string? modifiedBy = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var template = await _context.CodeTemplates
            .FirstOrDefaultAsync(t => t.Key == key, cancellationToken)
            ?? throw new InvalidOperationException($"Template '{key}' not found");

        var targetHistory = await _context.CodeTemplateHistories
            .FirstOrDefaultAsync(h => h.CodeTemplate.Id == template.Id && h.Version == targetVersion, cancellationToken)
            ?? throw new InvalidOperationException($"Version {targetVersion} not found for template '{key}'");

        return await SaveTemplateAsync(key, targetHistory.Code, $"Rollback to version {targetVersion}", modifiedBy, cancellationToken);
    }

    /// <summary>
    /// Compute hash of code for quick comparison
    /// </summary>
    private static string ComputeCodeHash(string code)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(hash);
    }
}
