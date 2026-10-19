# Release process

The public package ID is `Pobuca.DataverseAuditManager.XrmToolBox`.

## One-time repository setup

1. Create a NuGet.org account and confirm ownership details.
2. Create a NuGet API key scoped to `Pobuca.DataverseAuditManager.XrmToolBox` (or initially to new packages).
3. In GitHub, open **Settings → Secrets and variables → Actions**.
4. Add the API key as the repository secret `NUGET_API_KEY`.
5. Enable private vulnerability reporting under **Settings → Security → Code security and analysis**.
6. After the Build workflow has succeeded, protect `main` with a ruleset requiring pull requests and the `build` status check.

## Validate a package locally

```powershell
.\Pack.ps1
```

The script restores and builds Release, checks that the assembly and package versions match, creates the package under `artifacts`, and verifies required package entries.

Inspect the resulting `.nupkg` before the first publication. The plugin DLL must be located at:

```text
lib/net48/Plugins/DataverseAuditManager.XrmToolBox.dll
```

## Publish a release

1. Update `<Version>` in `DataverseAuditManager.XrmToolBox.csproj`.
2. Update the version and release notes in `DataverseAuditManager.XrmToolBox.nuspec`.
3. Add the release to `CHANGELOG.md`.
4. Build and test against a nonproduction Dataverse environment.
5. Run `./Pack.ps1` and inspect the package.
6. Commit and merge the release changes into `main`.
7. Tag the exact project version and push the tag:

```powershell
git tag -a v1.4.3 -m "Dataverse Audit Manager 1.4.3"
git push origin v1.4.3
```

The Release workflow validates that the tag and project version match, builds and packages the plugin, publishes to NuGet.org when `NUGET_API_KEY` is configured, and creates a GitHub release containing the `.nupkg` and plugin DLL.

NuGet package versions are immutable. Never reuse or overwrite a published version.

## First XrmToolBox Tool Library submission

After the NuGet package is published and indexed:

1. Verify its public NuGet page, project URL, icon, description, dependency, and package contents.
2. Register the package ID at <https://www.xrmtoolbox.com/plugins/new/>.
3. Wait for XrmToolBox administrator review.
4. Address validation feedback without replacing an existing NuGet version; publish a new patch version instead.

The package declares `XrmToolBox` version `1.2025.10.74` as its minimum Tool Library compatibility dependency. This is intentionally different from the development package reference `XrmToolBoxPackage`.
