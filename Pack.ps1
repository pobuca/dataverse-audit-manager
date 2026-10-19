[CmdletBinding()]
param(
    [string]$Version,
    [string]$OutputDirectory,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $PSScriptRoot "artifacts"
}
$projectPath = Join-Path $PSScriptRoot "DataverseAuditManager.XrmToolBox.csproj"
$nuspecPath = Join-Path $PSScriptRoot "DataverseAuditManager.XrmToolBox.nuspec"
$dllPath = Join-Path $PSScriptRoot "bin\Release\net48\DataverseAuditManager.XrmToolBox.dll"
$packageId = "Pobuca.DataverseAuditManager.XrmToolBox"

[xml]$project = Get-Content -LiteralPath $projectPath
$projectVersion = [string]$project.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $projectVersion
}
$Version = $Version.TrimStart("v")

if ($Version -ne $projectVersion) {
    throw "Package version '$Version' must match project version '$projectVersion'. Update the project before packing."
}

if (-not $NoBuild) {
    & dotnet restore $projectPath --configfile (Join-Path $PSScriptRoot "NuGet.Config")
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

    & dotnet build $projectPath --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
}

if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Release output was not found: $dllPath"
}

$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($dllPath).Version.ToString()
if ($assemblyVersion -ne $Version -and $assemblyVersion -ne "$Version.0") {
    throw "Assembly version '$assemblyVersion' does not match package version '$Version'."
}

$nuget = Get-Command nuget.exe -ErrorAction SilentlyContinue
if (-not $nuget) {
    $nuget = Get-Command nuget -ErrorAction SilentlyContinue
}
if (-not $nuget) {
    throw "NuGet CLI was not found. Install nuget.exe or run NuGet/setup-nuget in CI."
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
Get-ChildItem -LiteralPath $OutputDirectory -Filter "$packageId.*.nupkg" -ErrorAction SilentlyContinue |
    Remove-Item -Force

& $nuget.Source pack $nuspecPath `
    -Version $Version `
    -OutputDirectory $OutputDirectory `
    -BasePath $PSScriptRoot `
    -NonInteractive
if ($LASTEXITCODE -ne 0) { throw "NuGet pack failed." }

$packagePath = Join-Path $OutputDirectory "$packageId.$Version.nupkg"
if (-not (Test-Path -LiteralPath $packagePath)) {
    throw "Expected package was not created: $packagePath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $requiredEntries = @(
        "lib/net48/Plugins/DataverseAuditManager.XrmToolBox.dll",
        "icon.png",
        "README.md"
    )
    $entryNames = $archive.Entries.FullName -replace "\\", "/"
    foreach ($entry in $requiredEntries) {
        if ($entryNames -notcontains $entry) {
            throw "Package validation failed: '$entry' is missing."
        }
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Created and validated: $packagePath" -ForegroundColor Green
Write-Output $packagePath
