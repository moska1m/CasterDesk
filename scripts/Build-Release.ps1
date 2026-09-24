param([Parameter(Mandatory=$true)][string]$Iscc)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    dotnet publish source/CronoLive.csproj -c Release -r win-x64 --self-contained true -o publish
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o aplicativo.' }
    & $Iscc installer/CasterDesk.iss
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o instalador.' }
    Get-ChildItem dist/*.exe | ForEach-Object {
        $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($_.Name)" | Set-Content ($_.FullName + '.sha256') -Encoding ascii
    }
} finally { Pop-Location }
