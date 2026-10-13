param([string]$Dotnet='dotnet',[string]$RestoreConfig)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    & "$PSScriptRoot/Test-Core.ps1" -Dotnet $Dotnet -RestoreConfig $RestoreConfig
    $restore=@('restore','source-avalonia/CasterDesk.Avalonia.csproj')
    if($RestoreConfig){$restore+=@('--configfile',$RestoreConfig)}
    & $Dotnet @restore
    if($LASTEXITCODE -ne 0){throw 'Dependências Avalonia indisponíveis. Não existe entrega compilada.'}
    & $Dotnet build source-avalonia/CasterDesk.Avalonia.csproj -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'Falha ao compilar Avalonia.'}
    & "$PSScriptRoot/Test-Avalonia.ps1" -Dotnet $Dotnet
} finally {Pop-Location}
