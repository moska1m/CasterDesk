param([Parameter(Mandatory=$true)][string]$Iscc,[string]$Dotnet='dotnet',[string]$RestoreConfig,[string]$SignTool,[string]$CertificateThumbprint,[uri]$TimestampUrl)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    [xml]$properties=Get-Content Directory.Build.props
    $version=[string]$properties.Project.PropertyGroup.CasterDeskVersion
    $fileVersion=[string]$properties.Project.PropertyGroup.FileVersion
    if($version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9.]+)?$'){throw 'Versão inválida.'}
    $publish="publish-wpf-$version-"+[Guid]::NewGuid().ToString('N').Substring(0,8)
    $restore=@('restore','source-wpf/CasterDesk.Wpf.csproj','-r','win-x64')
    if($RestoreConfig){$restore+=@('--configfile',$RestoreConfig)}
    & $Dotnet @restore
    if($LASTEXITCODE -ne 0){throw 'Falha ao preparar dependências WPF.'}
    & $Dotnet publish source-wpf/CasterDesk.Wpf.csproj -c Release -r win-x64 --self-contained true --no-restore -o $publish -m:1 -p:UseSharedCompilation=false
    if($LASTEXITCODE -ne 0){throw 'Falha ao compilar WPF.'}
    & "$PSScriptRoot/Test-Wpf.ps1" -PublishFolder $publish
    foreach($required in @('CasterDesk.exe','CasterDesk.Core.dll','assets/siege/catalog.json','OcrImage.ps1')){
        if(!(Test-Path -LiteralPath (Join-Path $publish $required))){throw "Arquivo ausente na publicação: $required"}
    }
    if(Get-ChildItem $publish -Filter 'CasterDesk.Engine*'){throw 'A publicação contém sobras do engine antigo.'}
    $signing=![string]::IsNullOrWhiteSpace($CertificateThumbprint)
    if($signing){if(!$SignTool -or !$TimestampUrl){throw 'Assinatura requer SignTool e servidor de timestamp.'}; & "$PSScriptRoot/Sign-File.ps1" -SignTool $SignTool -CertificateThumbprint $CertificateThumbprint -TimestampUrl $TimestampUrl -File "$publish/CasterDesk.exe"}
    & $Iscc "/DAppVersion=$version" "/DAppFileVersion=$fileVersion" "/DPublishFolder=$publish" installer/CasterDesk-Wpf.iss
    if($LASTEXITCODE -ne 0){throw 'Falha ao gerar instalador WPF.'}
    $installer=Get-Item "dist/CasterDesk-$version-win-x64-Setup.exe"
    if($signing){& "$PSScriptRoot/Sign-File.ps1" -SignTool $SignTool -CertificateThumbprint $CertificateThumbprint -TimestampUrl $TimestampUrl -File $installer.FullName}
    $hash=(Get-FileHash $installer.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($installer.Name)" | Set-Content ($installer.FullName+'.sha256') -Encoding ascii
    $files=Get-ChildItem $publish -File -Recurse | Where-Object Extension -ne '.pdb' | ForEach-Object {[ordered]@{Path=[IO.Path]::GetRelativePath((Join-Path $root $publish),$_.FullName);Length=$_.Length;Sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}}
    [ordered]@{Version=$version;Published=$publish;Built=Get-Date -Format o;Installer=$installer.Name;Sha256=$hash;Signed=$signing;Files=@($files)} | ConvertTo-Json -Depth 5 | Set-Content ($installer.FullName+'.manifest.json') -Encoding utf8
    Write-Output $installer.FullName
} finally {Pop-Location}
