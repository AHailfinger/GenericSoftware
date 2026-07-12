using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace EnergyAutomate.Services.CodeFactory;

public sealed class RuntimeCodeTemplateExecutor
{
    private readonly RuntimeCodeTemplateStore _templateStore;
    private readonly ILogger<RuntimeCodeTemplateExecutor> _logger;

    public RuntimeCodeTemplateExecutor(RuntimeCodeTemplateStore templateStore, ILogger<RuntimeCodeTemplateExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(templateStore);
        ArgumentNullException.ThrowIfNull(logger);

        _templateStore = templateStore;
        _logger = logger;
    }

    public async Task ExecuteAsync(string templateKey, object factory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentNullException.ThrowIfNull(factory);

        var template = _templateStore.GetTemplate(templateKey)
            ?? throw new InvalidOperationException($"Template '{templateKey}' was not found.");

        var assembly = Compile(template.Key, template.CurrentCode);
        var scriptType = assembly.GetTypes()
            .FirstOrDefault(type => type is { IsClass: true, IsAbstract: false }
                && type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Any(method => method.Name == "ExecuteAsync" && method.GetParameters().Length == 1));

        if (scriptType is null)
        {
            throw new InvalidOperationException($"Template '{templateKey}' does not contain a public ExecuteAsync(factory) method.");
        }

        var script = Activator.CreateInstance(scriptType)
            ?? throw new InvalidOperationException($"Template '{templateKey}' could not be instantiated.");

        var executeMethod = scriptType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(method => method.Name == "ExecuteAsync" && method.GetParameters().Length == 1);

        cancellationToken.ThrowIfCancellationRequested();
        var result = executeMethod.Invoke(script, [factory]);

        if (result is Task task)
        {
            await task;
        }

        _logger.LogTrace("Runtime template {TemplateKey} executed using {ScriptType}", templateKey, scriptType.FullName);
    }

    private static Assembly Compile(string templateKey, string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        var references = GetMetadataReferences();
        var compilation = CSharpCompilation.Create(
            assemblyName: $"EnergyAutomate.RuntimeTemplate.{templateKey}.{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        if (!emitResult.Success)
        {
            var diagnostics = string.Join(Environment.NewLine, emitResult.Diagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString()));
            throw new InvalidOperationException($"Template '{templateKey}' compilation failed:{Environment.NewLine}{diagnostics}");
        }

        stream.Position = 0;
        return Assembly.Load(stream.ToArray());
    }

    private static IReadOnlyList<MetadataReference> GetMetadataReferences()
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location));

        return loadedAssemblies
            .DistinctBy(assembly => assembly.Location)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .ToList();
    }
}
