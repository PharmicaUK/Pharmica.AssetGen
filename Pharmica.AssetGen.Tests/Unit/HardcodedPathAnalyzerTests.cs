using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Core;

namespace Pharmica.AssetGen.Tests.Unit;

public class HardcodedPathAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }
        );

        var analyzer = new HardcodedPathAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer)
        );

        var diagnostics = await compilationWithAnalyzers.GetAllDiagnosticsAsync();
        return diagnostics.Where(d => d.Id == "ASSET002").ToImmutableArray();
    }

    [Test]
    public async Task HardcodedImagePath_ReportsWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string Logo = ""/images/logo.png"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("ASSET002");
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Warning);
        await Assert.That(diagnostics[0].GetMessage()).Contains("/images/logo.png");
        await Assert.That(diagnostics[0].GetMessage()).Contains("StaticAssets");
    }

    [Test]
    public async Task HardcodedCssPath_ReportsWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string Stylesheet => ""/css/style.css"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].GetMessage()).Contains("/css/style.css");
    }

    [Test]
    public async Task HardcodedJsPath_ReportsWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public void LoadScript()
    {
        var script = ""/js/app.js"";
    }
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].GetMessage()).Contains("/js/app.js");
    }

    [Test]
    public async Task HardcodedFontPath_ReportsWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string Font = ""/fonts/custom.woff2"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].GetMessage()).Contains("/fonts/custom.woff2");
    }

    [Test]
    public async Task HardcodedLibPath_ReportsWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string Library = ""/lib/jquery/jquery.min.js"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].GetMessage()).Contains("/lib/jquery/jquery.min.js");
    }

    [Test]
    public async Task HardcodedAssetsPath_ReportsWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string Asset = ""/assets/icon.svg"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].GetMessage()).Contains("/assets/icon.svg");
    }

    [Test]
    public async Task NonStaticAssetPath_NoWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string ApiPath = ""/api/users"";
    public string ViewPath = ""/views/home"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task RelativePathWithoutLeadingSlash_NoWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string Path = ""images/logo.png"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task UrlWithDomain_NoWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string CdnUrl = ""https://cdn.example.com/images/logo.png"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task InterpolatedString_NoWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string GetPath(string filename)
    {
        return $""/images/{filename}"";
    }
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task MultipleHardcodedPaths_ReportsMultipleWarnings()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string Logo = ""/images/logo.png"";
    public string Style = ""/css/style.css"";
    public string Script = ""/js/app.js"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(3);
    }

    [Test]
    public async Task HardcodedPathInMethod_ReportsWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string GetLogoPath()
    {
        return ""/images/logo.png"";
    }
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(1);
    }

    [Test]
    public async Task HardcodedPathInAttribute_ReportsWarning()
    {
        // Arrange
        var source =
            @"
[System.ComponentModel.Description(""/images/icon.png"")]
public class TestClass
{
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(1);
    }

    [Test]
    public async Task CommonImageExtensions_AllDetected()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string Png = ""/images/logo.png"";
    public string Jpg = ""/images/photo.jpg"";
    public string Jpeg = ""/images/photo.jpeg"";
    public string Svg = ""/images/icon.svg"";
    public string Webp = ""/images/hero.webp"";
    public string Gif = ""/images/animation.gif"";
    public string Ico = ""/images/favicon.ico"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(7);
    }

    [Test]
    public async Task NestedPaths_ReportsWarning()
    {
        // Arrange
        var source =
            @"
public class TestClass
{
    public string Icon = ""/assets/images/icons/user.svg"";
}";

        // Act
        var diagnostics = await GetDiagnosticsAsync(source);

        // Assert
        await Assert.That(diagnostics.Length).IsEqualTo(1);
    }
}
