param([string]$PublishFolder='publish-wpf-0.3.0')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$testDirectory=Join-Path $root ('test-results/wpf-release-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory | Out-Null
$previousData=$env:CASTERDESK_DATA_DIR
try {
    $env:CASTERDESK_DATA_DIR=$testDirectory
    $process=Start-Process -FilePath (Join-Path $root "$PublishFolder/CasterDesk.exe") -ArgumentList '--wpf-check' -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'Tempo excedido na validação WPF.' }
    $pass=Join-Path $testDirectory 'wpf-check.txt'
    if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $pass)) {
        throw "Validação WPF falhou. Consulte $testDirectory"
    }
    Get-Content -LiteralPath $pass
    Get-Content -LiteralPath (Join-Path $testDirectory 'match-interchange-check.txt')
    Get-Content -LiteralPath (Join-Path $testDirectory 'stability-check.txt')
    Get-Content -LiteralPath (Join-Path $testDirectory 'backup-test.txt')
    $pngDirectory=Join-Path $testDirectory 'png'
    New-Item -ItemType Directory -Path $pngDirectory | Out-Null
    $env:CASTERDESK_DATA_DIR=$pngDirectory
    $png=Start-Process -FilePath (Join-Path $root "$PublishFolder/CasterDesk.exe") -ArgumentList '--png-check' -WindowStyle Hidden -PassThru
    if (-not $png.WaitForExit(60000)) { $png.Kill(); throw 'Tempo excedido na validação PNG.' }
    $pngPass=Join-Path $pngDirectory 'image-optimization-test.txt'
    if ($png.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $pngPass)) { throw "Validação PNG falhou. Consulte $pngDirectory" }
    Get-Content -LiteralPath $pngPass
    Write-Output $testDirectory
} finally { $env:CASTERDESK_DATA_DIR=$previousData }
