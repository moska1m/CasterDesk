param([Parameter(Mandatory=$true)][string]$Iscc,[string]$Dotnet='dotnet',[string]$RestoreConfig,[Parameter(Mandatory=$true)][string]$PluginPackage,[string]$GoogleDesktopClient,[ValidateSet('normal','important')][string]$UpdatePriority='normal',[string]$UpdateNotesFile,[switch]$UnsignedCandidate)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    [xml]$properties=Get-Content Directory.Build.props
    $version=[string]$properties.Project.PropertyGroup.CasterDeskVersion
    $fileVersion=[string]$properties.Project.PropertyGroup.FileVersion
    $updateNotes=if($UpdateNotesFile){Get-Content -LiteralPath $UpdateNotesFile -Raw}else{'Uma nova versão do CasterDesk está disponível.'}
    if(!$UnsignedCandidate -and !(Test-Path -LiteralPath (Join-Path $env:LOCALAPPDATA 'CasterDesk/ReleaseKeys/update-signing.bin'))){throw 'Chave de publicação ausente. Prepare-a com New-UpdateKey.ps1 antes de empacotar.'}
    $googleBuild=@()
    if($GoogleDesktopClient){
        $googleSource=Get-Content -LiteralPath $GoogleDesktopClient -Raw | ConvertFrom-Json
        if(!$googleSource.installed -or $googleSource.installed.client_id -notmatch '^[a-zA-Z0-9._-]+\.apps\.googleusercontent\.com$' -or !$googleSource.installed.client_secret){throw 'Use o JSON de um cliente OAuth do tipo App para computador.'}
        # Only the native application identity belongs in the package. Never copy
        # arbitrary JSON fields, account tokens, service accounts or browser sessions.
        $googleDirectory=Join-Path $root 'build-config'
        New-Item -ItemType Directory -Path $googleDirectory -Force | Out-Null
        $googlePackage=Join-Path $googleDirectory 'google-desktop-client.json'
        @{installed=@{client_id=[string]$googleSource.installed.client_id;client_secret=[string]$googleSource.installed.client_secret}} | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $googlePackage -Encoding utf8
        $googleBuild=@("-p:GoogleDesktopClient=$googlePackage")
    }
    if($version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9.]+)?$'){throw 'Versão inválida.'}
    $pluginFull=[IO.Path]::GetFullPath($PluginPackage)
    $rootFull=[IO.Path]::GetFullPath($root)
    if(!$pluginFull.StartsWith($rootFull+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'O pacote do plugin precisa estar dentro da pasta do projeto.'}
    if(!(Test-Path -LiteralPath (Join-Path $pluginFull 'bin/64bit/casterdesk-obs.dll'))){throw 'DLL do plugin compilado ausente.'}
    $pluginRelative=[IO.Path]::GetRelativePath($rootFull,$pluginFull)
    # Never archive dependency caches or build trees into the shipped source ZIP.
    $dirtyPlugin=@(git status --porcelain --untracked-files=no -- obs-plugin)
    if($LASTEXITCODE -ne 0 -or $dirtyPlugin.Count -gt 0){throw 'Faça commit dos fontes OBS antes de gerar seu pacote verificável.'}
    & git archive --format=zip --prefix=obs-plugin/ --output (Join-Path $pluginFull 'source.zip') HEAD:obs-plugin
    if($LASTEXITCODE -ne 0){throw 'Falha ao arquivar os fontes versionados do OBS.'}
    & $Iscc "/DPluginPackage=$pluginRelative" installer/CasterDesk-OBS.iss
    if($LASTEXITCODE -ne 0){throw 'Falha ao compilar instalador do plugin OBS.'}
    & "$PSScriptRoot/Test-Core.ps1" -Dotnet $Dotnet -RestoreConfig $RestoreConfig
    $publish="publish-casterdesk-$version-"+[Guid]::NewGuid().ToString('N').Substring(0,8)
    $restore=@('restore','source-desktop/CasterDesk.Desktop.csproj','-r','win-x64')
    if($RestoreConfig){$restore+=@('--configfile',$RestoreConfig)}
    & $Dotnet @restore
    if($LASTEXITCODE -ne 0){throw 'Falha ao restaurar os pacotes de publicação do CasterDesk.'}
    & $Dotnet publish source-desktop/CasterDesk.Desktop.csproj -c Release -r win-x64 --self-contained true --no-restore -o $publish -m:1 -p:UseSharedCompilation=false @googleBuild
    if($LASTEXITCODE -ne 0){throw 'Falha ao publicar CasterDesk.'}
    if($GoogleDesktopClient -and !(Test-Path -LiteralPath (Join-Path $publish 'assets/google-desktop-client.json'))){throw 'Configuração do login Google ausente na publicação.'}
    foreach($required in @('assets/match-table-template.xlsx','CasterDesk.exe','CasterDesk.dll','CasterDesk.Core.dll','coreclr.dll','hostfxr.dll','SkiaSharp.dll','libSkiaSharp.dll','OcrImage.ps1','assets/siege/catalog.json','assets/promo/background.png','assets/studio/Icon_Defender.png','assets/branding/CasterDesk.ico','assets/branding/casterdesk-horizontal-white.svg','assets/branding/casterdesk-horizontal-black.svg')) {
        if(!(Test-Path -LiteralPath (Join-Path $publish $required))){throw "Arquivo ausente na publicação: $required"}
    }
    foreach($required in @('assets/obs/dock.html','assets/obs/dock.js','Microsoft.AspNetCore.Server.Kestrel.Core.dll')) {
        if(!(Test-Path -LiteralPath (Join-Path $publish $required))){throw "Arquivo ausente na publicação: $required"}
    }
    if(!(Test-Path -LiteralPath (Join-Path $publish 'assets/update-public-key.pem'))){throw 'Chave pública do atualizador ausente.'}
    & "$PSScriptRoot/Test-Desktop.ps1" -Dotnet $Dotnet -PublishFolder $publish
    & $Iscc "/DAppVersion=$version" "/DAppFileVersion=$fileVersion" "/DPublishFolder=$publish" installer/CasterDesk.iss
    if($LASTEXITCODE -ne 0){throw 'Falha ao compilar instalador do CasterDesk.'}
    $installer=Get-Item "dist/CasterDesk-$version-win-x64-Setup.exe"
    $hash=(Get-FileHash $installer.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($installer.Name)" | Set-Content ($installer.FullName+'.sha256') -Encoding ascii
    $files=Get-ChildItem $publish -File -Recurse | Where-Object Extension -ne '.pdb' | ForEach-Object {[ordered]@{Path=[IO.Path]::GetRelativePath((Join-Path $root $publish),$_.FullName);Length=$_.Length;Sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}}
    $pluginSetup=Get-Item 'dist/CasterDesk-OBS-1.0.0-win-x64-Setup.exe'
    $plugin=[ordered]@{Version='1.0.0';DllSha256=(Get-FileHash (Join-Path $pluginFull 'bin/64bit/casterdesk-obs.dll')).Hash.ToLowerInvariant();Installer=$pluginSetup.Name;InstallerSha256=(Get-FileHash $pluginSetup.FullName).Hash.ToLowerInvariant();SourcesSha256=(Get-FileHash (Join-Path $pluginFull 'source.zip')).Hash.ToLowerInvariant();NativeOBSLoadingTested=$false}
    [ordered]@{Version=$version;Published=$publish;Built=Get-Date -Format o;Installer=$installer.Name;Sha256=$hash;Signed=$false;NativeInstallationTested=$false;ObsPlugin=$plugin;Files=@($files)} | ConvertTo-Json -Depth 5 | Set-Content ($installer.FullName+'.manifest.json') -Encoding utf8
    if($UnsignedCandidate){Write-Output 'CANDIDATO SEM ASSINATURA: não foi gerado anúncio de atualização nem publicada uma release.'}
    else {& "$PSScriptRoot/Write-UpdateManifest.ps1" -Installer $installer.FullName -Version $version -Priority $UpdatePriority -Notes $updateNotes}
    Write-Output $installer.FullName
} finally {Pop-Location}
