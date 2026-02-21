using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Pharmica.AssetGen;

[Generator]
public sealed class AssetGenerator : IIncrementalGenerator
{
    const string DefaultRootDirectory = "wwwroot";

    private static readonly DiagnosticDescriptor s_duplicateAssetKeyRule = new(
        "ASSET001",
        "Duplicate asset key",
        "Multiple assets map to the same key '{0}'. Conflicting file: {1}. Consider using different file names or folder structure.",
        "Build",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        var assetFiles = ctx.AdditionalTextsProvider.Where(f =>
        {
            var normalized = f.Path.Replace('\\', '/');
            return normalized.Contains($"/{DefaultRootDirectory}/");
        });

        var configProvider = ctx.AnalyzerConfigOptionsProvider;

        var classNameProvider = configProvider.Select(
            (config, _) =>
            {
                config.GlobalOptions.TryGetValue(
                    "build_property.AssetGen_ClassName",
                    out var className
                );
                return string.IsNullOrWhiteSpace(className) ? "StaticAssets" : className!;
            }
        );

        var pathBaseProvider = configProvider.Select(
            (config, _) =>
            {
                if (
                    config.GlobalOptions.TryGetValue(
                        "build_property.AssetGen_PathBase",
                        out var pathBase
                    ) && !string.IsNullOrEmpty(pathBase)
                )
                {
                    return pathBase;
                }

                return "/";
            }
        );

        var assetData = assetFiles
            .Combine(configProvider)
            .Select(
                (tuple, _) =>
                {
                    var file = tuple.Left;
                    var config = tuple.Right;

                    var options = config.GetOptions(file);
                    var flattenExtensions = true;

                    if (
                        options.TryGetValue(
                            "build_property.AssetGen_FlattenExtensions",
                            out var val
                        )
                    )
                    {
                        flattenExtensions = !string.Equals(
                            val,
                            "false",
                            StringComparison.OrdinalIgnoreCase
                        );
                    }

                    return new AssetInfo(file.Path, flattenExtensions);
                }
            );

        var collected = assetData.Collect();

        var rootNsProvider = ctx.CompilationProvider.Select(
            (c, _) => c.AssemblyName ?? "Generated"
        );

        IncrementalValueProvider<(
            ImmutableArray<AssetInfo> Assets,
            string RootNamespace,
            string ClassName,
            string PathBase
        )> input = collected
            .Combine(rootNsProvider)
            .Combine(classNameProvider)
            .Combine(pathBaseProvider)
            .Select(
                (tuple, _) =>
                    (tuple.Left.Left.Left, tuple.Left.Left.Right, tuple.Left.Right, tuple.Right)
            );

        ctx.RegisterSourceOutput(
            input,
            (spc, data) =>
            {
                var assets = data.Assets;
                var rootNs = data.RootNamespace;
                var className = data.ClassName;
                var pathBase = data.PathBase;

                if (assets.IsEmpty)
                {
                    return;
                }

                AssetNode root = new(className, null, false);

                foreach (var asset in assets)
                {
                    var normalized = asset.Path.Replace('\\', '/');
                    var wwwrootIdx = normalized.LastIndexOf(
                        $"/{DefaultRootDirectory}/",
                        StringComparison.OrdinalIgnoreCase
                    );

                    if (wwwrootIdx < 0)
                    {
                        continue;
                    }

                    var relativePath = normalized.Substring(
                        wwwrootIdx + DefaultRootDirectory.Length + 2
                    );
                    var parts = relativePath.Split('/');

                    AddToTree(root, parts, 0, relativePath, asset.FlattenExtensions);
                }

                var keyMap = new Dictionary<string, List<string>>();
                CollectKeys(root, "", keyMap, className);

                foreach (var kvp in keyMap.Where(k => k.Value.Count > 1))
                {
                    foreach (var path in kvp.Value)
                    {
                        spc.ReportDiagnostic(
                            Diagnostic.Create(s_duplicateAssetKeyRule, Location.None, kvp.Key, path)
                        );
                    }

                    return;
                }

                StringBuilder sb = new();
                sb.AppendLine("// <auto-generated/>");
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine($"namespace {rootNs};");
                sb.AppendLine();

                GenerateClass(sb, root, 0, pathBase);

                spc.AddSource($"{className}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
        );
    }

    static void AddToTree(
        AssetNode node,
        string[] parts,
        int index,
        string fullPath,
        bool flattenExtensions
    )
    {
        while (true)
        {
            if (index >= parts.Length)
            {
                return;
            }

            var part = parts[index];
            var isFile = index == parts.Length - 1;

            var identifier = isFile ? MakeIdentifier(part, flattenExtensions) : ToPascalCase(part);

            var child = node.Children.FirstOrDefault(c => c.Name == identifier);

            if (child == null)
            {
                child = new AssetNode(identifier, isFile ? fullPath : null, isFile);
                node.Children.Add(child);
            }
            else if (isFile && fullPath != null)
            {
                child.AllPaths.Add(fullPath);
            }

            if (!isFile)
            {
                node = child;
                index += 1;
                continue;
            }

            break;
        }
    }

    static void CollectKeys(
        AssetNode node,
        string prefix,
        Dictionary<string, List<string>> keyMap,
        string rootClassName
    )
    {
        var currentKey = string.IsNullOrEmpty(prefix) ? node.Name : prefix + "." + node.Name;

        if (node.IsFile && node.AllPaths.Count > 0)
        {
            if (!keyMap.ContainsKey(currentKey))
            {
                keyMap[currentKey] = [];
            }

            keyMap[currentKey].AddRange(node.AllPaths);
        }

        foreach (var child in node.Children)
        {
            CollectKeys(child, node.Name == rootClassName ? "" : currentKey, keyMap, rootClassName);
        }
    }

    static void GenerateClass(StringBuilder sb, AssetNode node, int indent, string pathBase)
    {
        var indentStr = new string(' ', indent * 4);

        if (indent == 0)
        {
            sb.AppendLine($"{indentStr}/// <summary>");
            sb.AppendLine(
                $"{indentStr}/// Provides strongly-typed access to wwwroot assets with compile-time path validation."
            );
            sb.AppendLine($"{indentStr}/// </summary>");
        }

        sb.AppendLine($"{indentStr}public static class {node.Name}");
        sb.AppendLine($"{indentStr}{{");

        foreach (var child in node.Children.Where(c => c.IsFile).OrderBy(c => c.Name))
        {
            var webPath = "/" + child.Path!.Replace('\\', '/');
            var wwwrootIdx = webPath.LastIndexOf(
                $"/{DefaultRootDirectory}/",
                StringComparison.Ordinal
            );
            if (wwwrootIdx >= 0)
            {
                webPath = webPath.Substring(wwwrootIdx + DefaultRootDirectory.Length + 1);
            }

            var relativePath = webPath.TrimStart('/');
            if (pathBase == ".")
            {
                webPath = relativePath;
            }
            else
            {
                webPath = pathBase.TrimEnd('/') + "/" + relativePath;
            }

            sb.AppendLine($"{indentStr}    /// <summary>");
            sb.AppendLine($"{indentStr}    /// Path: {webPath}");
            sb.AppendLine($"{indentStr}    /// </summary>");
            sb.AppendLine($"{indentStr}    public const string {child.Name} = \"{webPath}\";");
            sb.AppendLine();
        }

        foreach (var child in node.Children.Where(c => !c.IsFile).OrderBy(c => c.Name))
        {
            GenerateClass(sb, child, indent + 1, pathBase);
        }

        sb.AppendLine($"{indentStr}}}");

        if (indent > 0)
        {
            sb.AppendLine();
        }
    }

    static string MakeIdentifier(string fileName, bool flattenExtensions)
    {
        if (!flattenExtensions)
        {
            // When not flattening, remove only the LAST extension before converting to PascalCase
            // For "style.min.css" we want "style.min" -> "StyleMin"
            var nameWithoutExt = fileName;
            var lastDotIndex = fileName.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                nameWithoutExt = fileName.Substring(0, lastDotIndex);
            }
            return ToPascalCase(nameWithoutExt);
        }

        StringBuilder result = new(fileName.Length);

        var capitalizeNext = true;

        foreach (var c in fileName)
        {
            if (c is '.' or '-' or '_' or ' ')
            {
                capitalizeNext = true;
            }
            else if (char.IsLetterOrDigit(c))
            {
                result.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                capitalizeNext = false;
            }
        }

        var final = result.ToString();

        // Prepend underscore if starts with digit or is empty
        if (string.IsNullOrEmpty(final) || char.IsDigit(final[0]))
        {
            final = "_" + final;
        }

        return final;
    }

    static string ToPascalCase(string input)
    {
        StringBuilder result = new(input.Length);
        var capitalizeNext = true;

        foreach (var c in input)
        {
            if (c is '.' or '-' or '_' or ' ')
            {
                capitalizeNext = true;
            }
            else if (char.IsLetterOrDigit(c))
            {
                result.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
                capitalizeNext = false;
            }
        }

        var final = result.ToString();

        if (string.IsNullOrEmpty(final) || char.IsDigit(final[0]))
        {
            final = "_" + final;
        }

        return final;
    }

    sealed class AssetInfo(string path, bool flattenExtensions)
    {
        public string Path { get; } = path;
        public bool FlattenExtensions { get; } = flattenExtensions;
    }

    sealed class AssetNode(string name, string? path, bool isFile)
    {
        public string Name { get; } = name;
        public string? Path { get; } = path;
        public bool IsFile { get; } = isFile;
        public List<AssetNode> Children { get; } = [];
        public List<string> AllPaths { get; } = path != null ? [path] : [];
    }
}
