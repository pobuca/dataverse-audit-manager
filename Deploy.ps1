param(
    [string]$XrmToolBoxPath = "$env:USERPROFILE\Documents\XrmToolbox v1.2026.8.75"
)

$ErrorActionPreference = "Stop"
$projectDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $projectDirectory "DataverseAuditManager.XrmToolBox.csproj"
$config = Join-Path $projectDirectory "NuGet.Config"
$output = Join-Path $projectDirectory "bin\Release\net48\DataverseAuditManager.XrmToolBox.dll"
$pluginsDirectory = Join-Path $XrmToolBoxPath "Plugins"
$target = Join-Path $pluginsDirectory "DataverseAuditManager.XrmToolBox.dll"

if (-not (Test-Path (Join-Path $XrmToolBoxPath "XrmToolBox.exe"))) {
    throw "XrmToolBox.exe was not found in '$XrmToolBoxPath'."
}

if (Get-Process -Name "XrmToolBox" -ErrorAction SilentlyContinue) {
    throw "Close XrmToolBox before deploying the plugin."
}

dotnet restore $project --configfile $config
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

dotnet build $project --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

New-Item -ItemType Directory -Path $pluginsDirectory -Force | Out-Null
Copy-Item $output $target -Force

Write-Host "Installed plugin: $target" -ForegroundColor Green
