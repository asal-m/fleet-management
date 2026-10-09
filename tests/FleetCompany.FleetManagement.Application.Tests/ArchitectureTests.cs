using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace FleetCompany.FleetManagement.Application.Tests;

public sealed class ArchitectureTests
{
    private static readonly string Root = FindRoot();
    private static readonly string[] ForbiddenNamespaces = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Grpc", "Wolverine", "MPCore.Messaging", "RabbitMQ", "Confluent.Kafka", "MassTransit"];
    [Fact]
    public void Modules_reference_other_modules_only_through_contract_projects()
    {
        var projects = Directory.GetFiles(Path.Combine(Root, "src", "Modules"), "*.csproj", SearchOption.AllDirectories);
        Assert.NotEmpty(projects);
        foreach (var project in projects)
            foreach (var reference in XDocument.Load(project).Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, include));
                Assert.True(File.Exists(target), $"Missing project reference: {project} -> {target}");
                if (!projects.Contains(target, StringComparer.OrdinalIgnoreCase))
                    continue;
                var owner = Directory.GetParent(Path.GetDirectoryName(project)!)!.FullName;
                var targetOwner = Directory.GetParent(Path.GetDirectoryName(target)!)!.FullName;
                Assert.True(owner.Equals(targetOwner, StringComparison.OrdinalIgnoreCase) || Path.GetFileNameWithoutExtension(target).EndsWith(".Contracts", StringComparison.Ordinal), $"Module boundary violation: {project} -> {target}");
            }
    }

    [Fact]
    public void Domain_and_application_do_not_use_persistence_transport_or_broker_types()
    {
        foreach (var module in Modules())
        {
            var compilation = Compile(module);
            foreach (var tree in compilation.SyntaxTrees.Where(t => IsBusinessSource(t.FilePath)))
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var node in tree.GetRoot().DescendantNodes().OfType<NameSyntax>())
                {
                    var symbol = model.GetSymbolInfo(node).Symbol;
                    var ns = symbol is INamespaceSymbol namespaceSymbol ? namespaceSymbol.ToDisplayString() : symbol?.ContainingNamespace?.ToDisplayString();
                    Assert.True(ns is null || !ForbiddenNamespaces.Any(prefix => ns == prefix || ns.StartsWith(prefix + ".", StringComparison.Ordinal)), $"Forbidden dependency {symbol} at {node.GetLocation().GetLineSpan()}");
                    if (tree.FilePath.Split(Path.DirectorySeparatorChar).Contains("Domain"))
                        Assert.True(ns is null || !(ns == "System.Diagnostics" || ns.StartsWith("System.Diagnostics.", StringComparison.Ordinal)), $"Domain instrumentation {symbol} at {node.GetLocation().GetLineSpan()}");
                }
            }
        }
    }

    [Fact]
    public void Every_business_message_has_nonempty_english_and_persian_text()
    {
        foreach (var module in Modules())
        {
            var compilation = Compile(module);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tree in compilation.SyntaxTrees.Where(t => IsBusinessSource(t.FilePath)))
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var node in tree.GetRoot().DescendantNodes())
                {
                    ExpressionSyntax? key = null;
                    if (node is PrimaryConstructorBaseTypeSyntax baseType && model.GetTypeInfo(baseType.Type).Type?.Name == "BusinessRule")
                    {
                        // This rule takes a suffix from the caller; all of its call sites are checked below.
                        if (baseType.Parent?.Parent is ClassDeclarationSyntax { Identifier.ValueText: "ResourceMustExistRule" })
                            continue;
                        key = baseType.ArgumentList.Arguments[2].Expression;
                    }
                    else if (node is ConstructorInitializerSyntax initializer && model.GetSymbolInfo(initializer).Symbol is IMethodSymbol { ContainingType.Name: "BusinessRule" })
                        key = initializer.ArgumentList.Arguments[2].Expression;
                    else if (node is InvocationExpressionSyntax invocation && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "WithMessage" })
                        key = invocation.ArgumentList.Arguments[0].Expression;
                    else if (node is BaseObjectCreationExpressionSyntax creation)
                    {
                        var type = model.GetTypeInfo(creation).Type?.Name;
                        if (type == "FailureMessageDescriptor")
                        {
                            var expression = creation.ArgumentList!.Arguments[0].Expression;
                            // Forwarding a validated BusinessRule key is covered at the rule's declaration.
                            if (expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "MessageKey" })
                                continue;
                            key = expression;
                        }
                        else if (type == "ResourceMustExistRule")
                        {
                            var suffix = model.GetConstantValue(creation.ArgumentList!.Arguments[2].Expression);
                            Assert.True(suffix.HasValue && suffix.Value is string, $"Nonconstant resource message key: {creation}");
                            keys.Add("operations." + (string)suffix.Value!);
                        }
                    }

                    if (key is null)
                        continue;
                    var value = model.GetConstantValue(key);
                    Assert.True(value.HasValue && value.Value is string, $"Message key must be statically verifiable: {key.GetLocation().GetLineSpan()}");
                    keys.Add((string)value.Value!);
                }
            }

            Assert.NotEmpty(keys);
            var resources = Directory.GetFiles(Path.Combine(module, "Resources"), "*Messages.resx");
            var neutral = Assert.Single(resources);
            foreach (var resource in new[]
            {
                neutral,
                Path.ChangeExtension(neutral, "fa.resx")
            }

            )
            {
                Assert.True(File.Exists(resource), $"Missing language resource: {resource}");
                var texts = XDocument.Load(resource).Descendants("data").ToDictionary(x => x.Attribute("name")!.Value, x => x.Element("value")?.Value, StringComparer.Ordinal);
                foreach (var key in keys)
                    Assert.True(texts.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text), $"Missing/empty message '{key}' in {resource}");
            }
        }
    }

    private static CSharpCompilation Compile(string module)
    {
        var trees = Directory.GetFiles(module, "*.cs", SearchOption.AllDirectories).Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")).Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)).ToList();
        trees.Add(CSharpSyntaxTree.ParseText("global using System; global using System.Collections.Generic; global using System.Linq; global using System.Threading; global using System.Threading.Tasks;"));
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Concat(Directory.GetFiles(AppContext.BaseDirectory, "*.dll")).Distinct(StringComparer.OrdinalIgnoreCase);
        var compilation = CSharpCompilation.Create("ArchitectureInspection", trees, paths.Select(path => MetadataReference.CreateFromFile(path)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(d => d.ToString())));
        return compilation;
    }

    private static IEnumerable<string> Modules() => Directory.GetFiles(Path.Combine(Root, "src", "Modules"), "*.csproj", SearchOption.AllDirectories).Where(path => !Path.GetFileNameWithoutExtension(path).EndsWith(".Contracts", StringComparison.Ordinal)).Select(path => Path.GetDirectoryName(path)!);
    private static bool IsBusinessSource(string path) => path.Split(Path.DirectorySeparatorChar).Any(part => part is "Domain" or "Application");
    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, ".mpcore", "template-manifest.json")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Architecture tests require the source checkout.");
    }
}
