param(
    [Parameter(Mandatory=$true)][string]$Installer,
    [Parameter(Mandatory=$true)][string]$Version,
    [ValidateSet('normal','important')][string]$Priority='normal',
    [string]$Notes='Uma nova versão do CasterDesk está disponível.',
    [string]$KeyPath=(Join-Path $env:LOCALAPPDATA 'CasterDesk/ReleaseKeys/update-signing.bin'),
    [string]$Output
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if($Version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-beta(\.(0|[1-9]\d*))*)?$'){throw 'Use uma versão release ou beta, por exemplo 1.0.8 ou 1.0.9-beta.1.'}
$file=Get-Item -LiteralPath $Installer
if($file.Name -ne "CasterDesk-$Version-win-x64-Setup.exe"){throw 'Nome do instalador não corresponde à versão.'}
if($file.Length -le 0 -or $file.Length -gt 1GB -or $Notes.Length -gt 12000){throw 'Tamanho ou notas inválidos.'}
if(!$Output){$Output=Join-Path $file.DirectoryName "update-$Version.json"}
$rsa=[Security.Cryptography.RSA]::Create()
try {
    $bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($KeyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    $read=0;$rsa.ImportPkcs8PrivateKey($bytes,[ref]$read)
    if($rsa.ExportSubjectPublicKeyInfoPem().Replace("`r",'').Trim() -ne (Get-Content (Join-Path $root 'assets/update-public-key.pem') -Raw).Replace("`r",'').Trim()){throw 'Chave de assinatura incorreta.'}
    $kind=if($Version.Contains('-')){'beta'}else{'release'}
    $payload=[ordered]@{Schema=1;Type=$kind;Version=$Version;Priority=$Priority;Notes=$Notes;PublishedAt=[DateTimeOffset]::UtcNow.ToString('o');Size=$file.Length;Sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
    $data=[Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Compress))
    $signature=$rsa.SignData($data,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pss)
    $envelope=[ordered]@{Payload=[Convert]::ToBase64String($data);Signature=[Convert]::ToBase64String($signature)}
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($Output),($envelope | ConvertTo-Json -Compress),[Text.UTF8Encoding]::new($false))
    Write-Output ([IO.Path]::GetFullPath($Output))
} finally {if($bytes){[Array]::Clear($bytes,0,$bytes.Length)};$rsa.Dispose()}
