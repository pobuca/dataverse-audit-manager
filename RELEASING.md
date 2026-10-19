# Release process

The public package ID is `Pobuca.DataverseAuditManager.XrmToolBox`.

## One-time repository setup

Publishing uses [NuGet Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) and GitHub OIDC. Do not create or store a long-lived NuGet API key.

1. Sign in to NuGet.org and confirm the account or organization that should own the package.
2. Open the NuGet.org **Trusted Publishing** page from the account menu and add a GitHub policy with:
   - **Repository owner:** `pobuca`
   - **Repository:** `dataverse-audit-manager`
   - **Workflow file:** `release.yml` (file name only)
   - **Environment:** `release`
   - **Package scope/glob:** `Pobuca.DataverseAuditManager.XrmToolBox`
   - Allow publishing new packages and new package versions.
3. In GitHub, open **Settings → Environments**, create the `release` environment, and optionally require a reviewer before deployments.
4. Add an environment secret named `NUGET_USER` containing the NuGet.org profile username associated with the policy. Use the profile name, not an email address.
5. Enable private vulnerability reporting under **Settings → Security → Code security and analysis**.
6. After the Build workflow has succeeded, protect `main` with a ruleset requiring pull requests and the `build` status check.

The Release workflow requests a one-time GitHub OIDC token. `NuGet/login@v1` exchanges it for a short-lived NuGet credential immediately before publication. No NuGet publishing credential is stored in GitHub.

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

The Release workflow validates that the tag and project version match, builds and packages the plugin, authenticates to NuGet.org through Trusted Publishing, publishes the package, and creates a GitHub release containing the `.nupkg` and plugin DLL.

NuGet package versions are immutable. Never reuse or overwrite a published version.

## First XrmToolBox Tool Library submission

After the NuGet package is published and indexed:

1. Verify its public NuGet page, project URL, icon, description, dependency, and package contents.
2. Register the package ID at <https://www.xrmtoolbox.com/plugins/new/>.
3. Wait for XrmToolBox administrator review.
4. Address validation feedback without replacing an existing NuGet version; publish a new patch version instead.

The package declares `XrmToolBox` version `1.2025.10.74` as its minimum Tool Library compatibility dependency. This is intentionally different from the development package reference `XrmToolBoxPackage`.
