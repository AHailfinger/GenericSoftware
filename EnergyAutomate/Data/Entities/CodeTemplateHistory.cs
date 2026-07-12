namespace EnergyAutomate.Data.Entities;

/// <summary>
/// Represents a historical version of a code template for audit trail and rollback capability
/// </summary>
public class CodeTemplateHistory
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to CodeTemplate
    /// </summary>
    public int CodeTemplateId { get; set; }

    /// <summary>
    /// Reference to the code template
    /// </summary>
    public required CodeTemplate CodeTemplate { get; set; }

    /// <summary>
    /// Version number
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The C# code content at this version
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created this version
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Optional changelog entry describing what changed
    /// </summary>
    public string? ChangeNotes { get; set; }

    /// <summary>
    /// Hash of the code for quick comparison
    /// </summary>
    public string? CodeHash { get; set; }
}
