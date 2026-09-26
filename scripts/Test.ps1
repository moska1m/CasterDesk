param([string]$Executable)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (!$Executable) {
    dotnet build (Join-Path $root 'source/CronoLive.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar os testes.' }
    $Executable = Join-Path $root 'source/bin/Release/net10.0-windows/CasterDesk.exe'
}
$Executable = (Resolve-Path $Executable).Path
$previous = $env:CASTERDESK_DATA_DIR
$results = Join-Path $root ('test-results/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $results | Out-Null
try {
    foreach ($test in @('self-test','championship-test','export-test','perf-test','theme-restart-test','timer-ui-test','theme-smoke','localization-test')) {
        $env:CASTERDESK_DATA_DIR = Join-Path $results $test
        $p = Start-Process $Executable -ArgumentList "--$test" -PassThru -WindowStyle Hidden
        if (!$p.WaitForExit(120000)) { throw "Teste demorou mais de 120 segundos: $test (PID $($p.Id))." }
        if ($p.ExitCode -ne 0) { throw "Falha no teste $test, saída $($p.ExitCode)." }
        Write-Output "$test OK"
    }
    Write-Output "Relatórios: $results"
} finally { $env:CASTERDESK_DATA_DIR = $previous }
