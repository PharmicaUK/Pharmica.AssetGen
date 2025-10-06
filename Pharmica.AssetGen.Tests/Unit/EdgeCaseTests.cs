using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Pharmica.AssetGen.Tests.Helpers;
using TUnit.Core;

namespace Pharmica.AssetGen.Tests.Unit;

public class EdgeCaseTests
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
    public async Task VeryDeepNesting_CreatesCorrectHierarchy()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/a/b/c/d/e/f/file.txt"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("public static class A");
        await Assert.That(generatedCode).Contains("public static class B");
        await Assert.That(generatedCode).Contains("public static class C");
        await Assert.That(generatedCode).Contains("public static class D");
        await Assert.That(generatedCode).Contains("public static class E");
        await Assert.That(generatedCode).Contains("public static class F");
        await Assert.That(generatedCode).Contains("FileTxt = \"/a/b/c/d/e/f/file.txt\"");
    }

    [Test]
    public async Task FilenameWithOnlySpecialCharacters_DoesNotCrash()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[] { new TestAdditionalFile("/project/wwwroot/images/---.png") };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert - Main goal is to ensure it doesn't crash
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        // The exact identifier generated isn't as important as ensuring it works
        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("public static class Images");
        await Assert.That(generatedCode).Contains("/images/---.png");
    }

    [Test]
    public async Task UnicodeCharactersInFilename_HandledCorrectly()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/images/café.png"),
            new TestAdditionalFile("/project/wwwroot/images/日本語.jpg"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        // Unicode letters should be preserved
        await Assert.That(generatedCode).Contains("CaféPng");
        await Assert.That(generatedCode).Contains("日本語Jpg");
    }

    [Test]
    public async Task MixedPathSeparators_WindowsAndUnix()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("C:\\project\\wwwroot\\images\\logo.png"),
            new TestAdditionalFile("/project/wwwroot/css/style.css"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("LogoPng");
        await Assert.That(generatedCode).Contains("StyleCss");
    }

    [Test]
    public async Task LargeNumberOfFiles_GeneratesSuccessfully()
    {
        // Arrange
        var generator = new AssetGenerator();
        var files = new List<TestAdditionalFile>();
        for (int i = 0; i < 100; i++)
        {
            files.Add(new TestAdditionalFile($"/project/wwwroot/images/file{i}.png"));
        }

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, files.ToArray());

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("File0Png");
        await Assert.That(generatedCode).Contains("File50Png");
        await Assert.That(generatedCode).Contains("File99Png");
    }

    [Test]
    public async Task FilesWithSimilarNames_AllGenerated()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/images/logo.png"),
            new TestAdditionalFile("/project/wwwroot/images/logo.svg"),
            new TestAdditionalFile("/project/wwwroot/images/logo.webp"),
            new TestAdditionalFile("/project/wwwroot/images/logo-small.png"),
            new TestAdditionalFile("/project/wwwroot/images/logo-large.png"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("LogoPng");
        await Assert.That(generatedCode).Contains("LogoSvg");
        await Assert.That(generatedCode).Contains("LogoWebp");
        await Assert.That(generatedCode).Contains("LogoSmallPng");
        await Assert.That(generatedCode).Contains("LogoLargePng");
    }

    [Test]
    public async Task DirectoryNameConflictWithFilename_BothGenerated()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/test.txt"),
            new TestAdditionalFile("/project/wwwroot/test/file.txt"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        // Root level file
        await Assert.That(generatedCode).Contains("TestTxt = \"/test.txt\"");
        // Nested directory and file
        await Assert.That(generatedCode).Contains("public static class Test");
        await Assert.That(generatedCode).Contains("FileTxt = \"/test/file.txt\"");
    }

    [Test]
    public async Task MultipleExtensions_FlattenedCorrectly()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/file.tar.gz"),
            new TestAdditionalFile("/project/wwwroot/script.min.js"),
            new TestAdditionalFile("/project/wwwroot/style.min.css.map"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("FileTarGz");
        await Assert.That(generatedCode).Contains("ScriptMinJs");
        await Assert.That(generatedCode).Contains("StyleMinCssMap");
    }

    [Test]
    public async Task EmptyDirectoryName_HandledGracefully()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot//file.txt"), // Double slash
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert - Should handle gracefully
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);
    }

    [Test]
    public async Task CaseSensitivity_DifferentCasesGenerateSameIdentifier()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/images/Logo.png"),
            new TestAdditionalFile("/project/wwwroot/images/LOGO.png"),
            new TestAdditionalFile("/project/wwwroot/images/logo.png"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert - Should report duplicate key error
        var runResult = driver.GetRunResult();
        var generatorDiagnostics = runResult.Results[0].Diagnostics;

        var duplicateKeyDiagnostic = generatorDiagnostics.FirstOrDefault(d => d.Id == "ASSET001");
        await Assert.That(duplicateKeyDiagnostic).IsNotNull();
    }

    [Test]
    public async Task HiddenFiles_GeneratedWithIdentifiers()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/.htaccess"),
            new TestAdditionalFile("/project/wwwroot/.well-known/security.txt"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        // Hidden files starting with . should still be processed
        await Assert.That(generatedCode).Contains("Htaccess");
        await Assert.That(generatedCode).Contains("WellKnown");
        await Assert.That(generatedCode).Contains("SecurityTxt");
    }

    [Test]
    public async Task CustomClassName_AppliedToAllLevels()
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
                        ["build_property.AssetGen_ClassName"] = "MyAssets",
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

        await Assert.That(generatedCode).Contains("public static class MyAssets");
        await Assert.That(generatedCode).DoesNotContain("public static class StaticAssets");
    }

    [Test]
    public async Task FilenameWithSpaces_ConvertedToPascalCase()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/wwwroot/my file name.txt"),
            new TestAdditionalFile("/project/wwwroot/another   file.txt"), // Multiple spaces
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(1);

        var generatedCode = runResult.GeneratedTrees[0].ToString();
        await Assert.That(generatedCode).Contains("MyFileNameTxt");
        await Assert.That(generatedCode).Contains("AnotherFileTxt");
    }

    [Test]
    public async Task NoWwwrootInPath_NoCodeGenerated()
    {
        // Arrange
        var generator = new AssetGenerator();
        var additionalFiles = new[]
        {
            new TestAdditionalFile("/project/public/images/logo.png"),
            new TestAdditionalFile("/project/static/style.css"),
        };

        var compilation = CreateCompilation();
        var driver = CreateDriver(generator, additionalFiles);

        // Act
        driver = (GeneratorDriver)
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Assert
        await Assert.That(diagnostics).IsEmpty();
        var runResult = driver.GetRunResult();
        // No wwwroot in path, so no code should be generated
        await Assert.That(runResult.GeneratedTrees.Length).IsEqualTo(0);
    }
}
