# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Pharmica.AssetGen is a **Roslyn incremental source generator** and **diagnostic analyzer** for ASP.NET Core. It generates strongly-typed constants for wwwroot static assets (e.g., `StaticAssets.Images.LogoPng` instead of `"/images/logo.png"`), providing compile-time safety and IntelliSense.

## Build & Development Commands

```bash
dotnet tool restore          # Install CSharpier, Husky, ReportGenerator
dotnet husky install         # Install pre-commit/commit-msg git hooks
dotnet restore               # Restore NuGet packages
dotnet build                 # Build the solution
dotnet csharpier .           # Auto-format code
dotnet csharpier check .     # Check formatting (used by pre-commit hook)
```

## Testing

Uses **TUnit** (not xUnit/NUnit). Tests are async and use `Assert.That(value).IsEqualTo(expected)` style assertions.

```bash
dotnet test                                              # Run all tests
dotnet test --filter "FullyQualifiedName~AssetGenerator" # Run tests matching a pattern
dotnet test --collect:"XPlat Code Coverage"              # Run with coverage
```

- **Unit tests** (`Pharmica.AssetGen.Tests/Unit/`): Test generator and analyzer with mocked Roslyn APIs via `TestHelpers.cs`
- **Integration tests** (`Pharmica.AssetGen.Tests/Integration/`): Build real ASP.NET Core fixture projects in `Fixtures/` and verify generated output

## Architecture

### Source Generator (`AssetGenerator.cs`)
Implements `IIncrementalGenerator`. Pipeline:
1. Filters `AdditionalTexts` for files matching `*/wwwroot/*`
2. Reads build properties: `AssetGen_ClassName` (default: "StaticAssets"), `AssetGen_FlattenExtensions` (default: true)
3. Builds a hierarchical tree from file paths → nested static classes with `const string` fields
4. Reports `ASSET001` (error) on duplicate key collisions

### Diagnostic Analyzer (`HardcodedPathAnalyzer.cs`)
Implements `DiagnosticAnalyzer`. Reports `ASSET002` (warning) when string literals contain hardcoded wwwroot paths (e.g., `"/css/site.css"`). Context-aware: suppresses in StaticAssets references, test code, and generated code.

### Key Design Constraints
- Generator targets **netstandard2.0** (Roslyn requirement for source generators)
- Tests target **net9.0**
- NuGet package ships as analyzer: DLLs go into `analyzers/dotnet/cs` in the package
- Default build properties are set in `build/Pharmica.AssetGen.props`
- Central package versioning via `Directory.Packages.props`

## Code Style & Commit Conventions

- **Formatter:** CSharpier (enforced by pre-commit hook — code will be rejected if not formatted)
- **Commits:** Conventional Commits required (enforced by commit-msg hook). Format: `type(scope): description`. Valid types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
- **No co-authored-by lines** — never add `Co-Authored-By` trailers to commit messages
- **CHANGELOG.md** is auto-generated from commits — do not edit manually
- Naming: PascalCase for public APIs, `_camelCase` for private fields, `s_camelCase` for static fields
