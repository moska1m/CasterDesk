param([string]$Dotnet='dotnet',[string]$RestoreConfig)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    & "$PSScriptRoot/Test-Core.ps1" -Dotnet $Dotnet -RestoreConfig $RestoreConfig
    $restore=@('restore','source-desktop/CasterDesk.Desktop.csproj')
    if($RestoreConfig){$restore+=@('--configfile',$RestoreConfig)}
    & $Dotnet @restore
    if($LASTEXITCODE -ne 0){throw 'Dependências do CasterDesk indisponíveis. Não existe entrega compilada.'}
    & $Dotnet build source-desktop/CasterDesk.Desktop.csproj -c Release --no-restore
    if($LASTEXITCODE -ne 0){throw 'Falha ao compilar CasterDesk.'}
    & "$PSScriptRoot/Test-Desktop.ps1" -Dotnet $Dotnet
} finally {Pop-Location}
