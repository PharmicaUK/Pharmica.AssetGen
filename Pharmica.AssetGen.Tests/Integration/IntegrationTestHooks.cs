#pragma warning disable RS1035 // Do not use banned APIs - Integration tests need Process APIs

using System.Diagnostics;
using System.IO;
using System.Linq;
using TUnit.Core;

namespace Pharmica.AssetGen.Tests.Integration;

/// <summary>
/// Global hooks for integration tests that set up and tear down the test environment.
/// </summary>
public static class IntegrationTestHooks
{
    private static readonly string SolutionRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../..")
    );
    private static readonly string GeneratorProject = Path.Combine(
        SolutionRoot,
        "Pharmica.AssetGen/Pharmica.AssetGen.csproj"
    );
    private static readonly string LocalNuGetDir = Path.Combine(SolutionRoot, ".localnuget");

    public static string TestVersion { get; private set; } = string.Empty;

    [Before(TestSession)]
    public static async Task OneTimeSetup()
    {
        // In CI, use the pre-packed version from workflow
        var isCI =
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true"
            || Environment.GetEnvironmentVariable("CI") == "true";

        if (isCI && Directory.Exists(LocalNuGetDir))
        {
            // Detect version from the .nupkg file
            var existingPackages = Directory.GetFiles(LocalNuGetDir, "Pharmica.AssetGen.*.nupkg");
            if (existingPackages.Length > 0)
            {
                var packageFile = Path.GetFileName(existingPackages[0]);
                // Extract version from filename: Pharmica.AssetGen.1.0.1.nupkg -> 1.0.1
                var version = packageFile.Replace("Pharmica.AssetGen.", "").Replace(".nupkg", "");
                TestVersion = version;
                return;
            }

            // Fallback to 0.0.0-ci if no package found
            TestVersion = "0.0.0-ci";
            return;
        }

        // Local development: pack a unique test version
        Directory.CreateDirectory(LocalNuGetDir);

        TestVersion = $"1.0.0-test-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var packResult = await RunDotnetCommand(
            "pack",
            GeneratorProject,
            "Packing generator as NuGet",
            $"-o \"{LocalNuGetDir}\"",
            "--configuration Release",
            $"/p:Version={TestVersion}"
        );

        if (packResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to pack NuGet package:\n{packResult.Output}"
            );
        }

        var packages = Directory.GetFiles(LocalNuGetDir, $"Pharmica.AssetGen.{TestVersion}.nupkg");
        if (packages.Length == 0)
        {
            throw new InvalidOperationException(
                $"NuGet package with version {TestVersion} was not created"
            );
        }

        var clearResult = await RunDotnetCommand(
            "nuget",
            "locals",
            "Clearing NuGet cache",
            "all",
            "--clear"
        );

        if (clearResult.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to clear NuGet cache:\n{clearResult.Output}"
            );
        }
    }

    [After(TestSession)]
    public static void OneTimeTeardown()
    {
        if (Directory.Exists(LocalNuGetDir))
        {
            try
            {
                Directory.Delete(LocalNuGetDir, true);
            }
            catch
            {
                // Intentionally swallowing exceptions during cleanup
            }
        }
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
}
