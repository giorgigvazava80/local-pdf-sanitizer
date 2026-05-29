# Contributing

## Reporting Bugs
Open a GitHub issue with:
- what you expected to happen;
- what actually happened;
- browser version, Windows version, and .NET SDK version if relevant;
- steps to reproduce and any safe sample files or logs.

## Proposing Improvements
Issues and pull requests are welcome for documentation, tests, deployment, and maintainability improvements. Please keep changes focused and explain any behavior changes clearly.

## Build and Test Locally
Build the native host:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build_native_host.ps1
```

Run tests:

```powershell
dotnet test .\tests\PdfSanitizerHost.Tests\PdfSanitizerHost.Tests.csproj -c Release
```

## Style Expectations
- Keep changes small and easy to review.
- Follow the existing C#, JavaScript, and PowerShell style in the repository.
- Prefer safe defaults and avoid introducing network-dependent runtime behavior.
- Update documentation when installation, deployment, or security assumptions change.

## Security Issues
For security-sensitive findings, please follow [SECURITY.md](SECURITY.md) instead of opening a public issue first.
