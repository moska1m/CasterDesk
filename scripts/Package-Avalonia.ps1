param([Parameter(Mandatory=$true)][string]$Iscc,[string]$Dotnet='dotnet',[string]$RestoreConfig)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    [xml]$properties=Get-Content Directory.Build.props
    $version=[string]$properties.Project.PropertyGroup.CasterDeskVersion
    $fileVersion=[string]$properties.Project.PropertyGroup.FileVersion
    if($version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9.]+)?$'){throw 'Versão inválida.'}
    & "$PSScriptRoot/Test-Core.ps1" -Dotnet $Dotnet -RestoreConfig $RestoreConfig
    $publish="publish-avalonia-$version-"+[Guid]::NewGuid().ToString('N').Substring(0,8)
    $restore=@('restore','source-avalonia/CasterDesk.Avalonia.csproj','-r','win-x64')
    if($RestoreConfig){$restore+=@('--configfile',$RestoreConfig)}
    & $Dotnet @restore
    if($LASTEXITCODE -ne 0){throw 'Falha ao restaurar os pacotes de publicação Avalonia.'}
    & $Dotnet publish source-avalonia/CasterDesk.Avalonia.csproj -c Release -r win-x64 --self-contained true --no-restore -o $publish -m:1 -p:UseSharedCompilation=false
    if($LASTEXITCODE -ne 0){throw 'Falha ao publicar Avalonia.'}
    foreach($required in @('CasterDesk.exe','CasterDesk.dll','CasterDesk.Core.dll','coreclr.dll','hostfxr.dll','SkiaSharp.dll','libSkiaSharp.dll','OcrImage.ps1','assets/siege/catalog.json','assets/promo/background.png','assets/studio/Icon_Defender.png','assets/branding/CasterDesk.ico','assets/branding/casterdesk-horizontal-white.svg','assets/branding/casterdesk-horizontal-black.svg')) {
        if(!(Test-Path -LiteralPath (Join-Path $publish $required))){throw "Arquivo ausente na publicação: $required"}
    }
    foreach($required in @('assets/obs/dock.html','assets/obs/dock.js','Microsoft.AspNetCore.Server.Kestrel.Core.dll')) {
        if(!(Test-Path -LiteralPath (Join-Path $publish $required))){throw "Arquivo ausente na publicação: $required"}
    }
    & "$PSScriptRoot/Test-Avalonia.ps1" -Dotnet $Dotnet -PublishFolder $publish
    & $Iscc "/DAppVersion=$version" "/DAppFileVersion=$fileVersion" "/DPublishFolder=$publish" installer/CasterDesk-Avalonia.iss
    if($LASTEXITCODE -ne 0){throw 'Falha ao compilar instalador Avalonia.'}
    $installer=Get-Item "dist/CasterDesk-$version-win-x64-Setup.exe"
    $hash=(Get-FileHash $installer.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($installer.Name)" | Set-Content ($installer.FullName+'.sha256') -Encoding ascii
    $files=Get-ChildItem $publish -File -Recurse | Where-Object Extension -ne '.pdb' | ForEach-Object {[ordered]@{Path=[IO.Path]::GetRelativePath((Join-Path $root $publish),$_.FullName);Length=$_.Length;Sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}}
    [ordered]@{Version=$version;Published=$publish;Built=Get-Date -Format o;Installer=$installer.Name;Sha256=$hash;Signed=$false;NativeInstallationTested=$false;Files=@($files)} | ConvertTo-Json -Depth 5 | Set-Content ($installer.FullName+'.manifest.json') -Encoding utf8
    Write-Output $installer.FullName
} finally {Pop-Location}
