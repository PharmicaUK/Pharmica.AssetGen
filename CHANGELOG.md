# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-02-21

### Added
- `AssetGen_PathBase` configuration property for prefixing generated asset paths (e.g., `/admin/v3`)
- Setting `AssetGen_PathBase` to `"."` generates relative paths without a leading slash
- `AssetGen_FlattenExtensions` and `AssetGen_PathBase` exposed as `CompilerVisibleProperty` in build props
- Unit tests for `AssetGen_PathBase` (prefix paths, relative paths, default behavior, trailing slash normalization)

### Changed
- Upgraded .NET SDK from 9.0.306 to 10.0.103
- Updated target framework from `net9.0` to `net10.0` for test projects and fixture apps
- Updated `Microsoft.CodeAnalysis.CSharp` from 4.14.0 to 5.0.0
- Updated `Microsoft.AspNetCore.Mvc.Testing` from 9.0.10 to 10.0.3
- Updated `Microsoft.SourceLink.GitHub` from 8.0.0 to 10.0.103
- Updated `Microsoft.Testing.Extensions.CodeCoverage` from 18.1.0 to 18.4.1
- Updated `Microsoft.Testing.Extensions.TrxReport` from 2.0.1 to 2.1.0
- Updated `TUnit` from 0.77.3 to 1.16.4
- Updated `coverlet.collector` from 6.0.4 to 8.0.0
- Updated CSharpier from 1.1.2 to 1.2.6
- Updated Husky from 0.7.2 to 0.8.0
- Updated ReportGenerator from 5.4.16 to 5.5.1
- Updated CI workflows to use .NET 10.x
- Switched from `/p:` to `-p:` syntax for MSBuild properties in CI and tests
- Added test runner configuration to `global.json` (`Microsoft.Testing.Platform`)

### Fixed
- Integration tests: only clear NuGet HTTP cache instead of all caches (avoids locked DLL failures on Windows)
- Integration tests: downgrade NuGet cache clear failure from exception to warning
- CI workflows: fixed test command syntax (removed extra `--` separator)

## [1.0.2] - 2025-10-27

### Changed
- Updated .NET SDK from 9.0.305 to 9.0.306
- Updated `Microsoft.AspNetCore.Mvc.Testing` from 9.0.9 to 9.0.10
- Updated `Microsoft.Testing.Extensions.CodeCoverage` from 18.0.4 to 18.1.0
- Updated `Microsoft.Testing.Extensions.TrxReport` from 1.9.0 to 2.0.1
- Updated `TUnit` from 0.61.39 to 0.77.3
- Updated `actions/checkout` from v4 to v5
- Updated `actions/setup-dotnet` from v4 to v5
- Updated `actions/upload-artifact` from v4 to v5
- Updated `github/codeql-action` from v3 to v4

## [1.0.1] - 2025-10-07

### Added
- `Pharmica.AssetGen.props` build file exposing `AssetGen_ClassName` as a compiler-visible property
- Integration test fixtures (`TestWebApp`, `CustomClassNameApp`)
- Integration tests for generated code, custom class names, and build verification

### Changed
- Updated `Microsoft.Testing.Extensions.TrxReport` from 1.8.4 to 1.9.0

### Fixed
- CI: detect package version from filename during release workflow

### Documentation
- Updated README.md banner URL to work on NuGet gallery

### Removed
- Removed `banner.png` from repository

## [1.0.0] - 2025-10-06

### Added
- Initial release of Pharmica.AssetGen
- Incremental source generator for wwwroot static assets
- Compile-time type safety for static file references
- Automatic generation of nested static classes matching directory structure
- PascalCase conversion for file and directory names
- Support for custom class names via `AssetGen_ClassName` property
- Configurable extension flattening via `AssetGen_FlattenExtensions` property
- Hardcoded path analyzer (ASSET002) to detect string literals
- Diagnostic for duplicate asset keys (ASSET001)
- Zero runtime overhead - all code generated at compile time
- Full IntelliSense support for generated assets
- Monorepo-friendly with Directory.Build.props support
- Support for all common web asset types (images, CSS, JS, fonts, etc.)

### Documentation
- Comprehensive README with usage examples
- Installation and configuration guide
- Monorepo setup instructions
- Troubleshooting section
- FAQ section
- Best practices guide
- Performance characteristics documentation

[Unreleased]: https://github.com/PharmicaUK/Pharmica.AssetGen/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/PharmicaUK/Pharmica.AssetGen/compare/v1.0.2...v1.1.0
[1.0.2]: https://github.com/PharmicaUK/Pharmica.AssetGen/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/PharmicaUK/Pharmica.AssetGen/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/PharmicaUK/Pharmica.AssetGen/releases/tag/v1.0.0
