namespace EnergyAutomate.Services.CodeFactory;

/// <summary>
/// View model for code template history entry
/// </summary>
public record CodeTemplateHistoryViewModel
{
    public required int Version { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public string? ChangeNotes { get; init; }
}
