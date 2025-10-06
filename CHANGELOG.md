# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- TUnit test project with comprehensive test coverage
- Pre-commit hooks using Husky.Net
- Conventional commits validation
- CSharpier code formatting checks
- GitHub Actions workflows for CI/CD
- Automated NuGet publishing on release
- Dependabot for dependency updates
- Issue templates for bug reports and feature requests
- Pull request template
- Contributing guidelines
- Security policy
- Code coverage reporting in CI

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

[Unreleased]: https://github.com/PharmicaUK/Pharmica.AssetGen/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/PharmicaUK/Pharmica.AssetGen/releases/tag/v1.0.0
