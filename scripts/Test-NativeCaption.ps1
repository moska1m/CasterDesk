param([string]$Dotnet='dotnet',[string]$PublishFolder,[string]$DataRoot)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$previousData=$env:CASTERDESK_DATA_DIR
Push-Location $root
try {
    $base=if($DataRoot){[IO.Path]::GetFullPath($DataRoot)}else{Join-Path $env:TEMP 'CasterDesk-native-checks'}
    $env:CASTERDESK_DATA_DIR=Join-Path $base ('caption-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $env:CASTERDESK_DATA_DIR -Force | Out-Null
    $assembly=if($PublishFolder){Join-Path ([IO.Path]::GetFullPath($PublishFolder)) 'CasterDesk.dll'}else{Join-Path $root 'source-desktop/bin/Release/net10.0-windows/CasterDesk.dll'}
    # A real Win32 window, with isolated match/settings data and no desktop-wide input.
    & $Dotnet $assembly --casterdesk-native-caption-check
    $testExit=$LASTEXITCODE
    $report=Join-Path $env:CASTERDESK_DATA_DIR 'native-window-check.txt'
    if(Test-Path -LiteralPath $report){Get-Content -LiteralPath $report}
    if($testExit -ne 0 -or !(Test-Path -LiteralPath $report)){throw "Native caption check failed: $env:CASTERDESK_DATA_DIR"}
    Write-Output $env:CASTERDESK_DATA_DIR
} finally {$env:CASTERDESK_DATA_DIR=$previousData;Pop-Location}
