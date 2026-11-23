# Builds Regneræven as a Windows program in .\publish\Regneraeven
# Self-contained: the target PC does not need .NET installed (WebView2 ships with Windows 11).
$ErrorActionPreference = 'Stop'
$out = Join-Path $PSScriptRoot 'publish\Regneraeven'
dotnet publish (Join-Path $PSScriptRoot 'src\Regneraeven.Desktop') -c Release -r win-x64 --self-contained true -o $out
Write-Host ""
Write-Host "Færdig: $out\Regneraeven.exe" -ForegroundColor Green
Write-Host "Tip: højreklik på Regneraeven.exe > Fastgør til proceslinjen / Opret genvej."
