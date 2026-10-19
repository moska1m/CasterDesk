param([string]$Dotnet='dotnet',[string]$PublishFolder)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$previousData=$env:CASTERDESK_DATA_DIR
Push-Location $root
try {
    $env:CASTERDESK_DATA_DIR=Join-Path $root ('test-results/casterdesk-ui-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $env:CASTERDESK_DATA_DIR | Out-Null
    # This switch uses only Avalonia.Headless. It never starts the desktop backend.
    if($PublishFolder){
        $publication=[IO.Path]::GetFullPath((Join-Path $root $PublishFolder))
        # Execute only the self-contained publication's explicit headless test mode.
        $testProcess=Start-Process -FilePath (Join-Path $publication 'CasterDesk.exe') -ArgumentList '--casterdesk-check' -WorkingDirectory $publication -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput ($env:CASTERDESK_DATA_DIR+".stdout.txt") -RedirectStandardError ($env:CASTERDESK_DATA_DIR+".stderr.txt")
        $testExit=$testProcess.ExitCode
    } else {
        & $Dotnet source-desktop/bin/Release/net10.0-windows/CasterDesk.dll --casterdesk-check
        $testExit=$LASTEXITCODE
    }
    if($PublishFolder -and $testExit -ne 0){Get-Content ($env:CASTERDESK_DATA_DIR+".stderr.txt") -Tail 30}
    if($testExit -ne 0 -or !(Test-Path (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-check.txt'))){throw "Validação do CasterDesk falhou (saída $testExit): $env:CASTERDESK_DATA_DIR"}
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-updates-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-championship-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-texts-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-backup-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-localization-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-brand-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-corrections-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-obs-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-spec-check.txt')
    Get-Content (Join-Path $env:CASTERDESK_DATA_DIR 'casterdesk-audit-fix-check.txt')
} finally {$env:CASTERDESK_DATA_DIR=$previousData;Pop-Location}
