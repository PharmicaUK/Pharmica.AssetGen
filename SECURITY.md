# Security Policy

## Reporting Security Issues

The Pharmica team takes security bugs in Pharmica.AssetGen seriously. We appreciate your efforts to responsibly disclose your findings, and will make every effort to acknowledge your contributions.

To report a security issue, please use the GitHub Security Advisory ["Report a Vulnerability"](https://github.com/PharmicaUK/Pharmica.AssetGen/security/advisories/new) tab.

The Pharmica team will send a response indicating the next steps in handling your report. After the initial reply to your report, the security team will keep you informed of the progress towards a fix and full announcement, and may ask for additional information or guidance.

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | :white_check_mark: |

## Security Considerations

Pharmica.AssetGen is a Roslyn source generator that runs at compile time. It:

- **Reads file paths only** - Does not read file contents, only paths from AdditionalFiles
- **Generates static code** - All output is deterministic based on file paths
- **No runtime overhead** - Generated code consists only of compile-time constants
- **Minimal dependencies** - Only depends on Microsoft.CodeAnalysis packages

Users should ensure they only use this package with trusted codebases, as it generates code during the build process.
