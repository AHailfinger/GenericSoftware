namespace EnergyAutomate.Services.CodeFactory;

public sealed record EnergyScriptPeriod(
    Guid Id,
    string Name,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil);
