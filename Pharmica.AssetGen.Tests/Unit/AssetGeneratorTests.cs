using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Pharmica.AssetGen.Tests.Helpers;
using TUnit.Core;

namespace Pharmica.AssetGen.Tests.Unit;

public class AssetGeneratorTests
{
    private static GeneratorDriver CreateDriver(
        AssetGenerator generator,
        params AdditionalText[] additionalTexts
    )
    {
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(
            new Dictionary<string, string>()
        );
        return CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create(additionalTexts))
            .WithUpdatedAnalyzerConfigOptions(optionsProvider);
    }

    private static Compilation CreateCompilation()
    {
        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }
        );
    }

    [Test]
    public async Task BasicAssetGeneration_CreatesStaticAssetsClass()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/images/logo.png"),
            new TestAdditionalFile("/project/wwwroot/css/style.css"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics
            );

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("public static class StaticAssets");
        await Assert.That(generatedCode).Contains("public static class Images");
        await Assert.That(generatedCode).Contains("public static class Css");
        await Assert.That(generatedCode).Contains("LogoPng = \"/images/logo.png\"");
        await Assert.That(generatedCode).Contains("StyleCss = \"/css/style.css\"");
    }

    [Test]
    public async Task CustomClassName_UsesConfiguredName()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[] { new TestAdditionalFile("/project/wwwroot/test.js") };

        var options = new CSharpParseOptions().WithPreprocessorSymbols(
            "ASSETGEN_CLASSNAME_WebAssets"
        );

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: Array.Empty<SyntaxTree>(),
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            },
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary
            ).WithSpecificDiagnosticOptions(
                new Dictionary<string, ReportDiagnostic>
                {
                    ["AssetGen_ClassName"] = ReportDiagnostic.Default,
                }
            )
        );

        var driver = CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalFiles))
            .WithUpdatedAnalyzerConfigOptions(
                new TestAnalyzerConfigOptionsProvider(
                    new Dictionary<string, string>
                    {
                        ["build_property.AssetGen_ClassName"] = "WebAssets",
                    }
                )
            );

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("public static class WebAssets");
        await Assert.That(generatedCode).DoesNotContain("public static class StaticAssets");
    }

    [Test]
    public async Task DuplicateAssetKeys_ReportsDiagnostic()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/images/logo.png"),
            new TestAdditionalFile("/project/wwwroot/images/logo_png"), // Would generate same key
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results[0].Diagnostics;

        var duplicateKeyDiagnostic = generatorDiagnostics.FirstOrDefault(d => d.Id == "ASSET001");
        await Assert.That(duplicateKeyDiagnostic).IsNotNull();
        await Assert.That(duplicateKeyDiagnostic!.Severity).IsEqualTo(DiagnosticSeverity.Error);
    }

    [Test]
    public async Task NestedDirectories_CreatesNestedClasses()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/assets/images/icons/user.svg"),
            new TestAdditionalFile("/project/wwwroot/assets/images/backgrounds/hero.jpg"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();

        await Assert.That(generatedCode).Contains("public static class Assets");
        await Assert.That(generatedCode).Contains("public static class Images");
        await Assert.That(generatedCode).Contains("public static class Icons");
        await Assert.That(generatedCode).Contains("public static class Backgrounds");
        await Assert.That(generatedCode).Contains("UserSvg = \"/assets/images/icons/user.svg\"");
        await Assert
            .That(generatedCode)
            .Contains("HeroJpg = \"/assets/images/backgrounds/hero.jpg\"");
    }

    [Test]
    public async Task SpecialCharactersInFilename_ConvertsToPascalCase()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/images/my-logo.png"),
            new TestAdditionalFile("/project/wwwroot/images/icon_small.svg"),
            new TestAdditionalFile("/project/wwwroot/images/hero image.jpg"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();

        await Assert.That(generatedCode).Contains("MyLogoPng");
        await Assert.That(generatedCode).Contains("IconSmallSvg");
        await Assert.That(generatedCode).Contains("HeroImageJpg");
    }

    [Test]
    public async Task FilenameStartingWithNumber_PrependsUnderscore()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/images/3d-model.obj"),
            new TestAdditionalFile("/project/wwwroot/images/404.png"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();

        await Assert.That(generatedCode).Contains("_3dModelObj");
        await Assert.That(generatedCode).Contains("_404Png");
    }

    [Test]
    public async Task FlattenExtensionsFalse_KeepsExtensionsSeparate()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/css/style.min.css"),
        };

        var compilation = CreateCompilation();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalFiles))
            .WithUpdatedAnalyzerConfigOptions(
                new TestAnalyzerConfigOptionsProvider(
                    new Dictionary<string, string>
                    {
                        ["build_property.AssetGen_FlattenExtensions"] = "false",
                    }
                )
            );

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();

        await Assert.That(generatedCode).Contains("StyleMin");
        await Assert.That(generatedCode).DoesNotContain("StyleMinCss");
    }

    [Test]
    public async Task EmptyWwwroot_DoesNotGenerateCode()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = Array.Empty<TestAdditionalFile>();

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        // When there are no assets, the generator returns early and produces no output
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(0);
    }

    [Test]
    public async Task IncrementalGeneration_OnlyRegeneratesOnFileChanges()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[] { new TestAdditionalFile("/project/wwwroot/test.js") };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act - First generation
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation1,
                out _
            );

        var firstResult = driver.GetRunResult();

        // Act - Second generation with same files
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(
                outputCompilation1,
                out var outputCompilation2,
                out _
            );

        var secondResult = driver.GetRunResult();

        // Assert - Should produce identical output
        await Assert
            .That(firstResult.GeneratedTrees[0].ToString())
            .IsEqualTo(secondResult.GeneratedTrees[0].ToString());
    }

    [Test]
    public async Task PathBase_PrefixesGeneratedPaths()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/images/logo.png"),
            new TestAdditionalFile("/project/wwwroot/css/style.css"),
        };

        var compilation = CreateCompilation();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalFiles))
            .WithUpdatedAnalyzerConfigOptions(
                new TestAnalyzerConfigOptionsProvider(
                    new Dictionary<string, string>
                    {
                        ["build_property.AssetGen_PathBase"] = "/admin/v3",
                    }
                )
            );

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("LogoPng = \"/admin/v3/images/logo.png\"");
        await Assert.That(generatedCode).Contains("StyleCss = \"/admin/v3/css/style.css\"");
    }

    [Test]
    public async Task PathBase_Dot_GeneratesRelativePaths()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[] { new TestAdditionalFile("/project/wwwroot/images/logo.png") };

        var compilation = CreateCompilation();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalFiles))
            .WithUpdatedAnalyzerConfigOptions(
                new TestAnalyzerConfigOptionsProvider(
                    new Dictionary<string, string> { ["build_property.AssetGen_PathBase"] = "." }
                )
            );

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("LogoPng = \"images/logo.png\"");
        await Assert.That(generatedCode).DoesNotContain("LogoPng = \"/images/logo.png\"");
    }

    [Test]
    public async Task PathBase_DefaultBehavior_PreservesLeadingSlash()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[] { new TestAdditionalFile("/project/wwwroot/images/logo.png") };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("LogoPng = \"/images/logo.png\"");
    }

    [Test]
    public async Task PathBase_TrailingSlashNormalized()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[] { new TestAdditionalFile("/project/wwwroot/images/logo.png") };

        var compilation = CreateCompilation();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(additionalFiles))
            .WithUpdatedAnalyzerConfigOptions(
                new TestAnalyzerConfigOptionsProvider(
                    new Dictionary<string, string>
                    {
                        ["build_property.AssetGen_PathBase"] = "/admin/v3/",
                    }
                )
            );

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("LogoPng = \"/admin/v3/images/logo.png\"");
        await Assert.That(generatedCode).DoesNotContain("//images");
    }

    [Test]
    public async Task CommonAssetExtensions_AllGenerated()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/images/photo.jpg"),
            new TestAdditionalFile("/project/wwwroot/images/logo.png"),
            new TestAdditionalFile("/project/wwwroot/images/icon.svg"),
            new TestAdditionalFile("/project/wwwroot/images/hero.webp"),
            new TestAdditionalFile("/project/wwwroot/fonts/font.woff2"),
            new TestAdditionalFile("/project/wwwroot/videos/demo.mp4"),
            new TestAdditionalFile("/project/wwwroot/docs/manual.pdf"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedCode = runResult.GeneratedTrees[0].ToString();

        await Assert.That(generatedCode).Contains("PhotoJpg");
        await Assert.That(generatedCode).Contains("LogoPng");
        await Assert.That(generatedCode).Contains("IconSvg");
        await Assert.That(generatedCode).Contains("HeroWebp");
        await Assert.That(generatedCode).Contains("FontWoff2");
        await Assert.That(generatedCode).Contains("DemoMp4");
        await Assert.That(generatedCode).Contains("ManualPdf");
    }
}
