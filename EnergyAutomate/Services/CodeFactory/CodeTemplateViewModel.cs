namespace EnergyAutomate.Services.CodeFactory;

public sealed record CodeTemplateViewModel(
    string Topic,
    string Key,
    string DisplayName,
    string Description,
    string Language,
    string DefaultCode,
    string CurrentCode,
    bool IsModified)
{
    public static CodeTemplateViewModel FromDefinition(CodeTemplateDefinition definition, string? currentCode = null)
    {
        var code = currentCode ?? definition.DefaultCode;
        return new CodeTemplateViewModel(
            definition.Topic,
            definition.Key,
            definition.DisplayName,
            definition.Description,
            definition.Language,
            definition.DefaultCode,
            code,
            !string.Equals(definition.DefaultCode, code, StringComparison.Ordinal));
    }
}
