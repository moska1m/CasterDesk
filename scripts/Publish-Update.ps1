param(
    [Parameter(Mandatory=$true)][string]$Installer,
    [Parameter(Mandatory=$true)][string]$Target,
    [Parameter(Mandatory=$true)][string]$NotesFile,
    [string]$Gh='gh',
    [switch]$InitializeFeed,
    [switch]$Publish
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$repo='moska1m/CasterDesk'
Push-Location $root
try {
    if($Target -notmatch '^[a-fA-F0-9]{40}$'){throw 'Use o SHA completo do commit de publicação.'}
    $installerFile=Get-Item -LiteralPath $Installer
    if($installerFile.Name -notmatch '^CasterDesk-(\d+\.\d+\.\d+(?:-beta(?:\.\d+)*)?)-win-x64-Setup\.exe$'){throw 'Instalador inválido.'}
    $version=$Matches[1];$tag="v$version"
    $manifest=Join-Path $installerFile.DirectoryName "update-$version.json"
    if(!(Test-Path -LiteralPath $manifest)){throw 'Gere primeiro o instalador e seu manifesto assinado.'}
    $envelope=Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
    $data=[Convert]::FromBase64String($envelope.Payload)
    $rsa=[Security.Cryptography.RSA]::Create()
    try {
        $rsa.ImportFromPem((Get-Content assets/update-public-key.pem -Raw))
        if(!$rsa.VerifyData($data,[Convert]::FromBase64String($envelope.Signature),[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pss)){throw 'Manifesto sem assinatura reconhecida.'}
    } finally {$rsa.Dispose()}
    $entry=[Text.Encoding]::UTF8.GetString($data) | ConvertFrom-Json
    if($entry.Version -ne $version -or $entry.Size -ne $installerFile.Length -or $entry.Sha256 -ne (Get-FileHash -LiteralPath $installerFile.FullName).Hash.ToLowerInvariant()){throw 'O instalador mudou depois da assinatura.'}
    if(!(Test-Path -LiteralPath $NotesFile)){throw 'Notas de publicação ausentes.'}
    if(!$Publish){Write-Output "Preparado: $tag, commit $Target, tipo $($entry.Type), prioridade $($entry.Priority). Use -Publish para publicar release e catálogo assinado.";return}
    if(@(git status --porcelain).Count -ne 0){throw 'Conclua o commit antes de publicar.'}
    if((git rev-parse HEAD) -ne $Target){throw 'O commit não corresponde ao código empacotado.'}
    & $Gh api "repos/$repo/commits/$Target" --jq .sha | Out-Null
    if($LASTEXITCODE -ne 0){throw 'Envie o commit ao GitHub antes de publicar.'}
    $stage=Join-Path $root ('build-config/release-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    $existing=$null
    if(!$InitializeFeed){
        & $Gh release download update-feed --repo $repo --pattern versions.json --dir $stage
        if($LASTEXITCODE -ne 0){throw 'Não foi possível obter a lista anterior. Não publique uma lista vazia no lugar.'}
        $existing=Join-Path $stage 'versions.json'
    }
    $catalog=Join-Path $stage 'next-versions.json'
    & "$PSScriptRoot/Write-UpdateCatalog.ps1" -Manifest $manifest -ExistingCatalog $existing -Output $catalog
    # Prepare the signed catalog before changing the public release. Publish it last.
    $raw=& $Gh release view $tag --repo $repo --json isDraft,assets 2>$null
    if($LASTEXITCODE -eq 0){
        $release=$raw | ConvertFrom-Json
        if(!$release.isDraft){throw 'Essa versão já foi publicada. Não substitua instaladores de uma versão existente.'}
    } else {
        & $Gh release create $tag --repo $repo --target $Target --title "$version" --notes-file $NotesFile --draft
        if($LASTEXITCODE -ne 0){throw 'Falha ao preparar a release.'}
    }
    $files=@($installerFile.FullName,($installerFile.FullName+'.sha256'),($installerFile.FullName+'.manifest.json'),$manifest)
    & $Gh release upload $tag @files --repo $repo --clobber
    if($LASTEXITCODE -ne 0){throw 'Falha ao enviar os arquivos. A release permanece rascunho.'}
    $beta=$entry.Type -eq 'beta'
    & $Gh release edit $tag --repo $repo --draft=false "--prerelease=$($beta.ToString().ToLowerInvariant())" "--latest=$((!$beta).ToString().ToLowerInvariant())"
    if($LASTEXITCODE -ne 0){throw 'Não foi possível publicar a release.'}
    if($InitializeFeed){
        & $Gh release create update-feed --repo $repo --target $Target --title 'update-feed' --notes 'Catálogo assinado consumido pelo verificador de atualizações. Para instalar, use a versão mais recente do CasterDesk.' --prerelease --latest=false
        if($LASTEXITCODE -ne 0){throw 'Falha ao preparar a hospedagem da lista. O instalador já está disponível; publique o catálogo separadamente.'}
    }
    $feed=Join-Path $stage 'versions.json';Copy-Item -LiteralPath $catalog -Destination $feed -Force
    & $Gh release upload update-feed $feed --repo $repo --clobber
    if($LASTEXITCODE -ne 0){throw 'Release publicada; atualização da lista falhou. Reenvie o catálogo assinado antes de anunciar o verificador.'}
    Write-Output "Publicado: https://github.com/$repo/releases/tag/$tag"
} finally {Pop-Location}
