param(
    [string]$ManifestPath = "$env:APPDATA\MscrmTools\XrmToolBox\Plugins\manifest.json",
    [string[]]$Categories = @("Configuration", "Data", "Security")
)

$ErrorActionPreference = "Stop"

if (Get-Process -Name "XrmToolBox" -ErrorAction SilentlyContinue) {
    throw "Close XrmToolBox before updating its plugin manifest."
}

if (-not (Test-Path $ManifestPath)) {
    throw "XrmToolBox plugin manifest was not found at '$ManifestPath'. Start XrmToolBox once, close it, and run this script again."
}

$manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
$plugin = $manifest.PluginMetadata | Where-Object { $_.Name -eq "Dataverse Audit Manager" } | Select-Object -First 1

if ($null -eq $plugin) {
    throw "Dataverse Audit Manager was not found in '$ManifestPath'. Start XrmToolBox once so it can scan the plugin, then run this script again."
}

Copy-Item $ManifestPath "$ManifestPath.bak" -Force
$plugin.Categories = @($Categories)
$manifest | ConvertTo-Json -Depth 20 | Set-Content $ManifestPath -Encoding UTF8

Write-Host "Categories updated: $($Categories -join ', ')" -ForegroundColor Green
Write-Host "Manifest: $ManifestPath"
