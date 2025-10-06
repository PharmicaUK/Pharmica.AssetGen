# Contributing to Pharmica.AssetGen

First off, thank you for considering contributing to Pharmica.AssetGen! It's people like you that make this tool better for everyone.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [How Can I Contribute?](#how-can-i-contribute)
- [Style Guidelines](#style-guidelines)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## Getting Started

### Important: Fork-Only Contribution Model

**⚠️ External contributors must work from forks.** Direct branch creation in the main repository is restricted to organization members only.

1. **Fork the repository** on GitHub (click the "Fork" button in the top right)
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/Pharmica.AssetGen.git
   cd Pharmica.AssetGen
   ```
3. **Add the upstream repository**:
   ```bash
   git remote add upstream https://github.com/PharmicaUK/Pharmica.AssetGen.git
   ```
4. **Keep your fork updated**:
   ```bash
   git fetch upstream
   git checkout main
   git merge upstream/main
   ```

## Development Setup

### Prerequisites

- .NET SDK 9.0 or later
- Git
- A code editor (Visual Studio 2022, Rider, or VS Code recommended)

### Initial Setup

**⚠️ IMPORTANT: You MUST install and use Husky for pre-commit hooks.** This ensures code quality and commit message standards.

1. **Restore tools** (includes CSharpier, Husky, etc.):
   ```bash
   dotnet tool restore
   ```

2. **Install Git hooks** (REQUIRED):
   ```bash
   dotnet husky install
   ```

   This installs hooks that will:
   - Validate commit messages against Conventional Commits specification
   - Check code formatting with CSharpier
   - Run build to catch compilation errors

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Build the solution**:
   ```bash
   dotnet build
   ```

5. **Run tests**:
   ```bash
   dotnet test
   ```

### Verifying Husky Installation

After installation, verify hooks are active:
```bash
ls -la .git/hooks/
```

You should see `pre-commit` and `commit-msg` hooks pointing to `.husky/`.

### Project Structure

```
Pharmica.AssetGen/
├── Pharmica.AssetGen/          # Source generator and analyzer
│   ├── AssetGenerator.cs       # Main source generator
│   └── HardcodedPathAnalyzer.cs # Diagnostic analyzer
├── Pharmica.AssetGen.Tests/    # Test project
├── .github/                     # GitHub workflows and templates
├── .husky/                      # Git hooks configuration
└── README.md
```

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When you create a bug report, include as many details as possible using our bug report template.

**Good bug reports** include:
- Clear, descriptive title
- Exact steps to reproduce
- Expected vs actual behavior
- Environment details (OS, .NET version, IDE)
- Code samples and project structure
- Build output or error messages

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:
- Use a clear, descriptive title
- Provide a detailed description of the proposed feature
- Explain why this enhancement would be useful
- Include code examples showing how it would work
- List any potential drawbacks or alternatives

### Your First Code Contribution

Unsure where to begin? Look for issues labeled:
- `good first issue` - Good for newcomers
- `help wanted` - Extra attention needed
- `documentation` - Improvements or additions to documentation

## Style Guidelines

### Code Style

This project uses **CSharpier** for code formatting. The pre-commit hook will automatically check formatting.

To format your code:
```bash
dotnet csharpier .
```

### General Guidelines

- Use meaningful variable and method names
- Keep methods focused and concise
- Add XML documentation comments for public APIs
- Follow C# naming conventions:
  - PascalCase for classes, methods, properties
  - camelCase for local variables and parameters
  - _camelCase for private fields

### Roslyn Analyzer Guidelines

When working on the analyzer:
- Follow [Roslyn analyzer best practices](https://github.com/dotnet/roslyn-analyzers/blob/main/GuidelinesForNewRules.md)
- Provide clear, actionable diagnostic messages
- Include code fix providers when possible
- Keep performance in mind (analyzers run frequently)

## Commit Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `build`: Build system changes
- `ci`: CI/CD changes
- `chore`: Maintenance tasks

### Examples

```
feat: add support for custom file extensions
fix(analyzer): resolve false positive in nested directories
docs: update README with .NET 9 requirements
test: add tests for edge cases in path normalization
```

### Commit Hook Validation

**All commits are validated automatically** by the `commit-msg` hook. Your commit will be rejected if it doesn't follow the Conventional Commits format.

If validation fails, you'll see an error like:
```
⚠️  Commit message does not follow Conventional Commits format
✗ Expected format: <type>(<scope>): <description>
✗ Valid types: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert
```

Fix your commit message and try again.

## Pull Request Process

### Creating a Pull Request from Your Fork

1. **Create a feature branch in your fork**:
   ```bash
   git checkout -b feat/your-feature-name
   ```

2. **Make your changes** and commit them following our commit guidelines
   - Husky will automatically validate your commits
   - Ensure code is formatted with CSharpier
   - All tests must pass

3. **Keep your branch updated with upstream**:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

4. **Push to your fork**:
   ```bash
   git push origin feat/your-feature-name
   ```

5. **Open a Pull Request** from your fork to `PharmicaUK/Pharmica.AssetGen:main`
   - Use our PR template
   - Reference any related issues
   - Provide a clear description of changes

### PR Checklist

Before submitting, ensure:
- [ ] Code follows the style guidelines (CSharpier will check this)
- [ ] All tests pass locally (`dotnet test`)
- [ ] New tests added for new features/bug fixes
- [ ] Documentation updated (README, XML comments, etc.)
- [ ] Commit messages follow Conventional Commits (Husky validates this)
- [ ] PR description clearly explains changes
- [ ] All commits have passed pre-commit hooks

**Note:** CHANGELOG.md is automatically generated from commit messages. Do NOT manually edit it.

### PR Review Process

1. Automated checks must pass (build, tests, formatting)
2. At least one maintainer review is required
3. Address review feedback by pushing new commits
4. Once approved, a maintainer will merge your PR

## Testing Guidelines

### Writing Tests

- Use TUnit testing framework
- Follow the Arrange-Act-Assert pattern
- Test one thing per test method
- Use descriptive test names that explain what is being tested

Example:
```csharp
[Test]
public async Task AssetGenerator_WithNestedDirectory_CreatesNestedClasses()
{
    // Arrange
    var generator = new AssetGenerator();
    var files = new[] { new TestAdditionalFile("wwwroot/images/icons/user.svg") };

    // Act
    var result = GenerateCode(generator, files);

    // Assert
    await Assert.That(result).Contains("public static class Icons");
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~AssetGenerator"
```

### Test Coverage

- Aim for >80% code coverage for new features
- Critical paths should have 100% coverage
- Coverage reports are generated in CI

## Getting Help

- **Documentation**: Check the [README](README.md) first
- **Discussions**: Ask questions in [GitHub Discussions](https://github.com/PharmicaUK/Pharmica.AssetGen/discussions)
- **Issues**: Search existing issues or create a new one
- **Contact**: For security issues, see [SECURITY.md](SECURITY.md)

## Recognition

Contributors will be recognized in:
- GitHub contributors list
- Release notes (for significant contributions)
- Our documentation

Thank you for contributing to Pharmica.AssetGen! 🎉
