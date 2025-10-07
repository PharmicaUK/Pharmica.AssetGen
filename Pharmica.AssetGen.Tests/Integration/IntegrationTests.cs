#pragma warning disable RS1035 // Do not use banned APIs - Integration tests need Process APIs

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TUnit.Core;

namespace Pharmica.AssetGen.Tests.Integration;

/// <summary>
/// Integration tests that verify the source generator works end-to-end
/// by compiling a real ASP.NET Core project and inspecting generated files.
/// </summary>
[NotInParallel]
public class IntegrationTests
{
    private static readonly string SolutionRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../..")
    );
    private static readonly string TestWebAppProject = Path.Combine(
        SolutionRoot,
        "Pharmica.AssetGen.Tests/Integration/Fixtures/TestWebApp/TestWebApp.csproj"
    );
    private static readonly string TestWebAppObjDir = Path.Combine(
        SolutionRoot,
        "Pharmica.AssetGen.Tests/Integration/Fixtures/TestWebApp/obj"
    );

    [Before(Test)]
    public async Task Setup()
    {
        await RunDotnetCommand(
            "clean",
            TestWebAppProject,
            "Cleaning TestWebApp",
            $"/p:AssetGenTestVersion={IntegrationTestHooks.TestVersion}"
        );
    }

    [Test]
    public async Task GeneratedCodeExists_WhenProjectIsBuilt()
    {
        var buildResult = await RunDotnetCommand(
            "build",
            TestWebAppProject,
            "Building TestWebApp",
            $"/p:AssetGenTestVersion={IntegrationTestHooks.TestVersion}"
        );

        await Assert
            .That(buildResult.ExitCode)
            .IsEqualTo(0)
            .Because($"Build failed with errors:\n{buildResult.Output}");

        var generatedFiles = await FindGeneratedSourceFiles();
        await Assert
            .That(generatedFiles)
            .IsNotEmpty()
            .Because("No generated source files found in obj directory");

        var assetFile = generatedFiles.FirstOrDefault(f => f.Contains("StaticAssets"));
        await Assert.That(assetFile).IsNotNull().Because("StaticAssets generated file not found");
    }

    [Test]
    public async Task GeneratedCode_ContainsExpectedAssetProperties()
    {
        var generatedFiles = await FindGeneratedSourceFiles();
        var assetFile = generatedFiles.FirstOrDefault(f => f.Contains("StaticAssets"));
        await Assert.That(assetFile).IsNotNull();

        var generatedCode = await File.ReadAllTextAsync(assetFile!);

        await Assert.That(generatedCode).Contains("class StaticAssets");
        await Assert.That(generatedCode).Contains("class Images");
        await Assert.That(generatedCode).Contains("LogoPng");
        await Assert.That(generatedCode).Contains("IconSvg");
        await Assert.That(generatedCode).Contains("class Css");
        await Assert.That(generatedCode).Contains("StyleCss");
        await Assert.That(generatedCode).Contains("class Js");
        await Assert.That(generatedCode).Contains("AppJs");
    }

    [Test]
    public async Task GeneratedCode_ContainsNestedDirectories()
    {
        var generatedFiles = await FindGeneratedSourceFiles();
        var assetFile = generatedFiles.FirstOrDefault(f => f.Contains("StaticAssets"));
        var generatedCode = await File.ReadAllTextAsync(assetFile!);

        await Assert.That(generatedCode).Contains("class Lib");
        await Assert.That(generatedCode).Contains("class Bootstrap");
        await Assert.That(generatedCode).Contains("BootstrapMinCss");
    }

    [Test]
    public async Task GeneratedCode_ContainsCorrectPaths()
    {
        var generatedFiles = await FindGeneratedSourceFiles();
        var assetFile = generatedFiles.FirstOrDefault(f => f.Contains("StaticAssets"));
        var generatedCode = await File.ReadAllTextAsync(assetFile!);

        await Assert.That(generatedCode).Contains("\"/images/logo.png\"");
        await Assert.That(generatedCode).Contains("\"/images/icon.svg\"");
        await Assert.That(generatedCode).Contains("\"/css/style.css\"");
        await Assert.That(generatedCode).Contains("\"/js/app.js\"");
        await Assert.That(generatedCode).Contains("\"/lib/bootstrap/bootstrap.min.css\"");
    }

    [Test]
    public async Task TestWebApp_CompilesSuccessfully()
    {
        var buildResult = await RunDotnetCommand(
            "build",
            TestWebAppProject,
            "Building TestWebApp in Release",
            "--configuration Release",
            $"/p:AssetGenTestVersion={IntegrationTestHooks.TestVersion}"
        );

        await Assert
            .That(buildResult.ExitCode)
            .IsEqualTo(0)
            .Because($"Compilation failed:\n{buildResult.Output}");

        var cs0103Pattern = new Regex(@"CS0103", RegexOptions.IgnoreCase);
        await Assert
            .That(cs0103Pattern.IsMatch(buildResult.Output))
            .IsFalse()
            .Because("Generated code contains undefined type errors (CS0103)");
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetCommand(
        string command,
        string projectPath,
        string description,
        params string[] additionalArgs
    )
    {
        var args = $"{command} \"{projectPath}\" {string.Join(" ", additionalArgs)}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException(
                $"Failed to start dotnet process for: {description}"
            );
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var fullOutput = output + "\n" + error;
        return (process.ExitCode, fullOutput);
    }

    private static async Task<List<string>> FindGeneratedSourceFiles()
    {
        await RunDotnetCommand(
            "build",
            TestWebAppProject,
            "Building with emitted files",
            "--configuration Release",
            "-p:EmitCompilerGeneratedFiles=true",
            $"/p:AssetGenTestVersion={IntegrationTestHooks.TestVersion}"
        );

        if (!Directory.Exists(TestWebAppObjDir))
        {
            return [];
        }

        return Directory
            .GetFiles(TestWebAppObjDir, "*.g.cs", SearchOption.AllDirectories)
            .Concat(
                Directory.GetFiles(
                    TestWebAppObjDir,
                    "*StaticAssets*.cs",
                    SearchOption.AllDirectories
                )
            )
            .Distinct()
            .ToList();
    }
}
