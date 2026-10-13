param([string]$Dotnet='dotnet',[string]$PublishFolder)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$previousData=$env:CASTERDESK_DATA_DIR
Push-Location $root
try {
    $env:CASTERDESK_DATA_DIR=Join-Path $root ('test-results/avalonia-ui-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $env:CASTERDESK_DATA_DIR | Out-Null
    # This switch uses only Avalonia.Headless. It never starts the desktop backend.
    if($PublishFolder){
        $publication=[IO.Path]::GetFullPath((Join-Path $root $PublishFolder))
        # Execute only the self-contained publication's explicit headless test mode.
        $testProcess=Start-Process -FilePath (Join-Path $publication 'CasterDesk.Avalonia.exe') -ArgumentList '--avalonia-check' -WorkingDirectory $publication -WindowStyle Hidden -Wait -PassThru
        $testExit=$testProcess.ExitCode
    } else {
        & $Dotnet source-avalonia/bin/Release/net10.0-windows/CasterDesk.Avalonia.dll --avalonia-check
        $testExit=$LASTEXITCODE
    }
    if($testExit -ne 0 -or !(Test-Path (Join-Path $env:CASTERDESK_DATA_DIR 'avalonia-check.txt'))){throw "Validação Avalonia falhou (saída $testExit): $env:CASTERDESK_DATA_DIR"}
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'avalonia-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'avalonia-championship-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'avalonia-texts-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'avalonia-backup-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'avalonia-localization-check.txt')
} finally {$env:CASTERDESK_DATA_DIR=$previousData;Pop-Location}
