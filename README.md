<img width="1280" height="446" alt="banner" src="https://github.com/user-attachments/assets/7f9a09cf-d75b-41bd-a715-7d93160792a4" />

[![NuGet](https://img.shields.io/nuget/v/Pharmica.AssetGen.svg)](https://www.nuget.org/packages/Pharmica.AssetGen/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Pharmica.AssetGen.svg)](https://www.nuget.org/packages/Pharmica.AssetGen/)
[![Build Status](https://github.com/PharmicaUK/Pharmica.AssetGen/actions/workflows/pr.yml/badge.svg)](https://github.com/PharmicaUK/Pharmica.AssetGen/actions/workflows/pr.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub Issues](https://img.shields.io/github/issues/PharmicaUK/Pharmica.AssetGen.svg)](https://github.com/PharmicaUK/Pharmica.AssetGen/issues)
[![GitHub Pull Requests](https://img.shields.io/github/issues-pr/PharmicaUK/Pharmica.AssetGen.svg)](https://github.com/PharmicaUK/Pharmica.AssetGen/pulls)
[![GitHub Stars](https://img.shields.io/github/stars/PharmicaUK/Pharmica.AssetGen.svg)](https://github.com/PharmicaUK/Pharmica.AssetGen/stargazers)
[![GitHub Commit Activity](https://img.shields.io/github/commit-activity/m/PharmicaUK/Pharmica.AssetGen.svg)](https://github.com/PharmicaUK/Pharmica.AssetGen/commits/main)

A Roslyn source generator that provides compile-time type safety for wwwroot assets in ASP.NET Core applications.

## Why Use This?

Instead of hardcoding paths like `"/images/logo.png"` in your code (which can break at runtime if the file is renamed or deleted), you can reference them as `StaticAssets.Images.LogoPng`. If the file doesn't exist or is renamed, you'll get a compile-time error instead of a runtime 404.

### Key Features

- ✅ **Compile-time safety** - Catch missing/renamed assets before deployment
- ✅ **IntelliSense support** - Browse assets with autocomplete
- ✅ **Zero runtime overhead** - All code generation happens at build time
- ✅ **Hardcoded path analyzer** - Get warnings for hardcoded paths in your code
- ✅ **Configurable** - Customize class name and extension handling
- ✅ **Monorepo friendly** - Works seamlessly in solutions with multiple projects

## Installation

```bash
dotnet add package Pharmica.AssetGen
```

## Quick Start

### 1. Install the Package

```xml
<ItemGroup>
  <PackageReference Include="Pharmica.AssetGen" />
</ItemGroup>
```

### 2. Add wwwroot Files as AdditionalFiles

```xml
<ItemGroup>
  <AdditionalFiles Include="wwwroot/**/*" />
</ItemGroup>
```

### 3. Use the Generated Class

Given this wwwroot structure:

```
wwwroot/
├── images/
│   ├── logo.png
│   └── icon-small.svg
├── css/
│   └── style.min.css
└── js/
    └── app.js
```

The generator creates:

```csharp
public static class StaticAssets
{
    public static class Images
    {
        public const string LogoPng = "/images/logo.png";
        public const string IconSmallSvg = "/images/icon-small.svg";
    }

    public static class Css
    {
        public const string StyleMinCss = "/css/style.min.css";
    }

    public static class Js
    {
        public const string AppJs = "/js/app.js";
    }
}
```

### 4. Reference Assets in Your Code

**Razor/Blazor:**

```razor
<img src="@StaticAssets.Images.LogoPng" alt="Logo" />
<link rel="stylesheet" href="@StaticAssets.Css.StyleMinCss" />
<script src="@StaticAssets.Js.AppJs"></script>
```

**Minimal API:**

```csharp
app.MapGet("/logo", () => Results.File(StaticAssets.Images.LogoPng));
```

**Controllers:**

```csharp
public IActionResult Index()
{
    ViewData["LogoPath"] = StaticAssets.Images.LogoPng;
    return View();
}
```

## Configuration

### Customize Class Name

By default, the generated class is named `StaticAssets`. You can customize this to avoid conflicts:

```xml
<PropertyGroup>
  <AssetGen_ClassName>WebAssets</AssetGen_ClassName>
</PropertyGroup>
```

Then use: `WebAssets.Images.LogoPng`

### Path Base

By default, generated paths start with `/` (e.g., `/images/logo.png`). If your application is hosted under a sub-path, you can configure a path base:

```xml
<PropertyGroup>
  <AssetGen_PathBase>/admin/v3</AssetGen_PathBase>
</PropertyGroup>
```

This generates paths like `/admin/v3/images/logo.png` instead of `/images/logo.png`.

To generate relative paths (no leading slash), set the path base to `.`:

```xml
<PropertyGroup>
  <AssetGen_PathBase>.</AssetGen_PathBase>
</PropertyGroup>
```

This generates `images/logo.png` — useful for Blazor WebAssembly or other scenarios where relative paths are needed.

### File Extension Handling

By default, file extensions are "flattened" into PascalCase:

- `logo.png` → `LogoPng`
- `style.min.css` → `StyleMinCss`

To disable this behavior and keep extensions separate:

```xml
<PropertyGroup>
  <AssetGen_FlattenExtensions>false</AssetGen_FlattenExtensions>
</PropertyGroup>
```

### Naming Rules

- **PascalCase:** All identifiers are converted to PascalCase
- **Special Characters:** Dots (`.`), hyphens (`-`), underscores (`_`), and spaces are removed, and the next character is capitalized
- **Invalid Identifiers:** Names starting with numbers are prefixed with an underscore

Examples:

- `my-logo.png` → `MyLogoPng`
- `icon small.svg` → `IconSmallSvg`
- `3d-model.obj` → `_3dModelObj`

## Hardcoded Path Analyzer

The package includes an analyzer (ASSET002) that warns when you use hardcoded paths to static assets:

```csharp
// ⚠️ Warning ASSET002: Hardcoded path '/images/logo.png' should use StaticAssets class
public string Logo => "/images/logo.png";

// ✅ No warning
public string Logo => StaticAssets.Images.LogoPng;
```

The analyzer detects common asset patterns:

- Paths starting with `/images/`, `/css/`, `/js/`, `/fonts/`, `/lib/`, `/assets/`
- Common file extensions: `.css`, `.js`, `.png`, `.jpg`, `.svg`, `.webp`, `.ico`, `.woff`, etc.

## Performance Characteristics

### Build Time Impact

- **Incremental Builds:** Minimal overhead (~50-200ms for typical projects)
- **Clean Builds:** Proportional to number of assets (1000 assets ~500ms)
- **Incremental Generator:** Only regenerates when wwwroot files change

The generator uses Roslyn's incremental generation pipeline, so it only runs when:

1. wwwroot files are added, removed, or renamed
2. Configuration properties change
3. Clean builds are performed

### Output Directory

- **wwwroot files are NOT duplicated** - They're only included as `AdditionalFiles` for analysis
- **No runtime overhead** - All generated code is compile-time constants
- **Generated file size:** Approximately 50 bytes per asset + hierarchy structure

### Memory Usage

Minimal - the generator processes files in a streaming fashion and doesn't load file contents into memory (only file paths).

## Error Handling

### Duplicate Keys (ASSET001)

If multiple files would generate the same identifier, the generator reports a compile error:

```
error ASSET001: Multiple assets map to the same key 'StaticAssets.Images.Logo'.
Conflicting file: /wwwroot/images/logo.png.
Consider using different file names or folder structure.
```

**Common Scenarios:**

- `images/logo.png` and `images/logo_png` → Both generate `LogoPng`
- Files with complex naming that collapse to the same identifier

**Solution:** Rename one of the files to be distinct.

## Requirements

- .NET Standard 2.0+ (works with both .NET Framework 4.7.2+ and .NET Core/.NET 5+)
- ASP.NET Core projects with wwwroot folder

## Best Practices

1. **Use Consistent Naming:** Prefer kebab-case or snake_case for file names
2. **Organize by Type:** Use subdirectories (`images/`, `css/`, `js/`) for better organization
3. **Avoid Special Characters:** While supported, avoid unusual characters in file names
4. **Enable Analyzer:** Keep ASSET002 warnings enabled to catch hardcoded paths
5. **Commit Generated Code:** For transparency, consider emitting generated files to source control:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

## Working with Bundled Assets

If you use a front-end bundler (esbuild, webpack, Vite, Parcel, etc.) that outputs files into `wwwroot`, those files must exist **before** `dotnet build` runs — otherwise the source generator won't see them.

### The Problem

Roslyn source generators run during compilation. If your bundler hasn't written its output to `wwwroot` yet, the generator has nothing to pick up and the corresponding constants won't be generated.

### The Solution

Run your front-end build as a separate step **before** `dotnet build`. Most task runners (Taskfile, Make, Just, etc.) make this straightforward:

```bash
# Build front-end assets first
npm run build

# Then build the .NET project
dotnet build
```

In CI/CD pipelines like GitHub Actions, split these into separate steps:

```yaml
- name: Install Node dependencies
  run: npm ci

- name: Build front-end assets
  run: npm run build

- name: Build .NET project
  run: dotnet build
```

> **Tip:** The same principle applies to any tool that generates files into `wwwroot` — Sass/LESS compilers, image optimizers, etc. Just make sure they run before `dotnet build`.

## Troubleshooting

### Generator Not Running

1. Verify `wwwroot/**/*` is included in `AdditionalFiles`
2. Check that the project references the generator correctly
3. Try a clean rebuild: `dotnet clean && dotnet build`

### Analyzer Not Showing Warnings

1. Verify the generator is referenced as an analyzer
2. Check your IDE analyzer settings
3. Restart your IDE/Roslyn language server

## FAQ

### Q: Does this affect my published wwwroot folder?

**A:** No. The generator only reads file paths - it doesn't modify or duplicate your wwwroot files. They're published normally.

### Q: What about dynamic asset paths?

**A:** For dynamic paths (e.g., user uploads, CDN URLs), continue using strings. This tool is for _static_ assets that ship with your application.

### Q: Can I use this with Blazor WebAssembly?

**A:** Yes! It works with any ASP.NET Core project type (MVC, Razor Pages, Blazor Server, Blazor WASM, Minimal APIs).

### Q: Does this work with asset fingerprinting/bundling?

**A:** The generator provides the logical path (`/images/logo.png`). If you're using asset fingerprinting (e.g., `logo.abc123.png`), you'll need to pipe the constant through your fingerprinting system. Future versions may include built-in support.

### Q: How do I disable the analyzer warnings?

**A:** Add this to your project file:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);ASSET002</NoWarn>
</PropertyGroup>
```

## Contributing

Contributions are welcome! This project is open source under the MIT license.

### Development Setup

1. Clone the repository
2. Run `dotnet build`
3. Run tests: `dotnet test`

### Reporting Issues

Please include:

- Your project structure (especially wwwroot layout)
- The generated code (if applicable)
- Steps to reproduce
- Expected vs actual behavior

## License

MIT License - see LICENSE file for details.

## Acknowledgments

Built with:

- Roslyn Source Generators
- Incremental Generator Pipeline
- C# Syntax Analysis

Inspired by the need for type-safe asset references in modern ASP.NET Core applications and the lessons learned from SQL file generators.
