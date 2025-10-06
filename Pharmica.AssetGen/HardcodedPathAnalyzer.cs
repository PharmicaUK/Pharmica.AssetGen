using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Pharmica.AssetGen;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HardcodedPathAnalyzer : DiagnosticAnalyzer
{
    const string DiagnosticId = "ASSET002";
    const string Title = "Hardcoded wwwroot path detected";

    const string MessageFormat =
        "Hardcoded path '{0}' should use StaticAssets class for compile-time safety";

    const string Description =
        "Use the generated StaticAssets class instead of hardcoded paths to wwwroot files.";

    const string Category = "Usage";

    static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literalExpression = (LiteralExpressionSyntax)context.Node;
        var value = literalExpression.Token.ValueText;

        if (string.IsNullOrEmpty(value) || !value.StartsWith("/"))
        {
            return;
        }

        string[] commonDirs = ["/images/", "/css/", "/js/", "/fonts/", "/lib/", "/assets/"];

        var looksLikeStaticAsset =
            commonDirs.Any(dir => value.StartsWith(dir, System.StringComparison.OrdinalIgnoreCase))
            || value.EndsWith(".css", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".js", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".gif", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".svg", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".webp", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".ico", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".woff", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".woff2", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".ttf", System.StringComparison.OrdinalIgnoreCase)
            || value.EndsWith(".eot", System.StringComparison.OrdinalIgnoreCase);

        if (!looksLikeStaticAsset)
        {
            return;
        }

        if (value.Contains("{") || value.Contains("$"))
        {
            return;
        }

        if (IsInStaticAssetsContext(literalExpression))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(s_rule, literalExpression.GetLocation(), value);
        context.ReportDiagnostic(diagnostic);
    }

    static bool IsInStaticAssetsContext(SyntaxNode node)
    {
        var current = node.Parent;

        while (current != null)
        {
            switch (current)
            {
                case MemberAccessExpressionSyntax memberAccess:
                {
                    var expressionText = memberAccess.Expression.ToString();
                    if (
                        expressionText.Contains("StaticAssets")
                        || expressionText.Contains("Assets")
                        || expressionText.Contains("WebAssets")
                    )
                    {
                        return true;
                    }

                    break;
                }
                case FieldDeclarationSyntax:
                case PropertyDeclarationSyntax:
                {
                    var currentText = current.ToString();
                    if (currentText.Contains("const string") && currentText.Contains("="))
                    {
                        return true;
                    }

                    break;
                }
                case InvocationExpressionSyntax invocation:
                {
                    var invocationText = invocation.ToString();
                    if (
                        invocationText.Contains("Assert.")
                        || invocationText.Contains(".IsEqualTo")
                        || invocationText.Contains(".Equals")
                    )
                    {
                        return true;
                    }

                    break;
                }
                case CompilationUnitSyntax compilationUnit:
                {
                    var firstTrivia = compilationUnit.GetLeadingTrivia().FirstOrDefault();
                    if (firstTrivia.ToString().Contains("auto-generated"))
                    {
                        return true;
                    }

                    break;
                }
            }

            current = current.Parent;
        }

        return false;
    }
}
