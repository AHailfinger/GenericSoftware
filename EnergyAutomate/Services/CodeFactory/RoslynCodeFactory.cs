using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace EnergyAutomate.Services.CodeFactory;

public sealed class RoslynCodeFactory
{
    private readonly ILogger<RoslynCodeFactory> _logger;

    public RoslynCodeFactory(ILogger<RoslynCodeFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public CodeTemplateValidationResult Validate(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions);

        var references = GetMetadataReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName: $"EnergyAutomate.RuntimeTemplate.{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        var diagnostics = emitResult.Diagnostics
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Select(ToDiagnostic)
            .ToList();

        _logger.LogTrace("Runtime code template validation finished. Success: {Success}; Diagnostics: {DiagnosticCount}", emitResult.Success, diagnostics.Count);

        return new CodeTemplateValidationResult(emitResult.Success, diagnostics);
    }

    private static CodeTemplateDiagnostic ToDiagnostic(Diagnostic diagnostic)
    {
        var lineSpan = diagnostic.Location.GetLineSpan();
        var start = lineSpan.StartLinePosition;

        return new CodeTemplateDiagnostic(
            diagnostic.Severity.ToString(),
            diagnostic.Id,
            diagnostic.GetMessage(),
            start.Line + 1,
            start.Character + 1);
    }

    private static IReadOnlyList<MetadataReference> GetMetadataReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Console).Assembly,
            typeof(Enumerable).Assembly,
            typeof(System.Runtime.GCSettings).Assembly,
            Assembly.Load("System.Runtime")
        };

        return assemblies
            .Where(assembly => !string.IsNullOrWhiteSpace(assembly.Location))
            .DistinctBy(assembly => assembly.Location)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .ToList();
    }
}
