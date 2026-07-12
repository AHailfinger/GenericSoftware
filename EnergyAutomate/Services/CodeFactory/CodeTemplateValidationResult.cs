namespace EnergyAutomate.Services.CodeFactory;

public sealed record CodeTemplateDiagnostic(
    string Severity,
    string Id,
    string Message,
    int Line,
    int Column);

public sealed record CodeTemplateValidationResult(
    bool Success,
    IReadOnlyList<CodeTemplateDiagnostic> Diagnostics)
{
    public static CodeTemplateValidationResult Valid { get; } = new(true, []);
}
