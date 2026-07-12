namespace EnergyAutomate.Data.Entities;

/// <summary>
/// Represents the current version of a code template
/// </summary>
public class CodeTemplate
{
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier for the template (e.g., "calculation.average-power")
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Current version number
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The C# code content
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// User who created/modified this template
    /// </summary>
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// Whether this template is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// History of all versions
    /// </summary>
    public ICollection<CodeTemplateHistory> History { get; set; } = new List<CodeTemplateHistory>();
}
