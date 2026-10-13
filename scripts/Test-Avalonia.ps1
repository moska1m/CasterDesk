param([string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$previousData=$env:CASTERDESK_DATA_DIR
Push-Location $root
try {
    $env:CASTERDESK_DATA_DIR=Join-Path $root ('test-results/avalonia-ui-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $env:CASTERDESK_DATA_DIR | Out-Null
    # This switch uses only Avalonia.Headless. It never starts the desktop backend.
    & $Dotnet source-avalonia/bin/Release/net10.0-windows/CasterDesk.Avalonia.dll --avalonia-check
    if($LASTEXITCODE -ne 0 -or !(Test-Path (Join-Path $env:CASTERDESK_DATA_DIR 'avalonia-check.txt'))){throw "Validação Avalonia falhou: $env:CASTERDESK_DATA_DIR"}
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'avalonia-check.txt')
} finally {$env:CASTERDESK_DATA_DIR=$previousData;Pop-Location}
