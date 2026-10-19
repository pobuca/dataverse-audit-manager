# Contributing

Thank you for helping improve Dataverse Audit Manager.

## Before you start

- Search existing issues before opening a new one.
- Use an issue to discuss significant behavior or UI changes before implementing them.
- Never include customer data, credentials, connection strings, environment URLs, or audit exports in issues, screenshots, commits, or test fixtures.
- Test destructive audit-cleanup behavior only in a disposable nonproduction environment.

## Development setup

Requirements:

- Windows
- .NET Framework 4.8 developer tools
- A current .NET SDK
- XrmToolBox
- Access to a nonproduction Dataverse environment for integration testing

Build the project:

```powershell
dotnet restore DataverseAuditManager.XrmToolBox.csproj --configfile NuGet.Config
dotnet build DataverseAuditManager.XrmToolBox.csproj --configuration Release --no-restore
```

Install the local build after closing XrmToolBox:

```powershell
.\Deploy.ps1 -XrmToolBoxPath "C:\Path\To\XrmToolBox"
```

## Pull requests

1. Create a focused branch from `main`.
2. Keep changes small and explain the user impact.
3. Preserve staged-change and confirmation safeguards.
4. Keep long-running Dataverse operations asynchronous.
5. Confirm the tool opens safely without an active connection.
6. Run a Release build and resolve all warnings or errors introduced by the change.
7. Include screenshots for visible UI changes, using synthetic data only.
8. Update `README.md` and `CHANGELOG.md` when behavior changes.

By contributing, you agree that your contribution is licensed under the repository's MIT License.
