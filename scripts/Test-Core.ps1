param([string]$Dotnet='dotnet',[string]$RestoreConfig)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$previousData=$env:CASTERDESK_DATA_DIR
Push-Location $root
try {
    $restore=@('restore','source-core-checks/CasterDesk.CoreChecks.csproj')
    if($RestoreConfig){$restore+=@('--configfile',$RestoreConfig)}
    & $Dotnet @restore
    if($LASTEXITCODE -ne 0){throw 'Falha ao preparar o núcleo.'}
    & $Dotnet build source-core-checks/CasterDesk.CoreChecks.csproj -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'Falha ao compilar o núcleo.'}
    $env:CASTERDESK_DATA_DIR=Join-Path $root ('test-results/avalonia-core-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $env:CASTERDESK_DATA_DIR | Out-Null
    & $Dotnet source-core-checks/bin/Release/net10.0-windows/CasterDesk.Engine.dll
    if($LASTEXITCODE -ne 0){throw "Testes do núcleo falharam: $env:CASTERDESK_DATA_DIR"}
    Write-Output $env:CASTERDESK_DATA_DIR
} finally {$env:CASTERDESK_DATA_DIR=$previousData;Pop-Location}
