param([string]$Go='go')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$destination=Join-Path $root 'build-config/casterdesk-replay.exe'
New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent) | Out-Null
Push-Location (Join-Path $root 'tools/replay-reader')
try {
    & $Go build -trimpath -o $destination .
    if($LASTEXITCODE -ne 0){throw 'Falha ao compilar o leitor local de replays.'}
    $licenses=Join-Path $root 'build-config/replay-licenses.txt'
    $text=[Text.StringBuilder]::new()
    [void]$text.AppendLine('CasterDesk replay adapter — dependency license notices')
    $modules=& $Go list -deps -f '{{with .Module}}{{.Path}}|{{.Dir}}{{end}}' .
    if($LASTEXITCODE-ne 0){throw 'Falha ao obter licenças das dependências.'}
    foreach($module in ($modules | Where-Object {$_ -match '\|'} | Sort-Object -Unique)){
        $parts=$module.Split('|',2)
        foreach($license in (Get-ChildItem -LiteralPath $parts[1] -File | Where-Object Name -Match '^(LICENSE|LICENCE|COPYING|NOTICE)')){
            [void]$text.AppendLine("`n===== $($parts[0]) / $($license.Name) =====")
            [void]$text.AppendLine([IO.File]::ReadAllText($license.FullName))
        }
    }
    [IO.File]::WriteAllText($licenses,$text.ToString(),[Text.UTF8Encoding]::new($false))
    $sourceArchive=Join-Path $root 'build-config/replay-reader-source.zip'
    Compress-Archive -Path (Join-Path $root 'tools/replay-reader/*') -DestinationPath $sourceArchive -Force
} finally {Pop-Location}
