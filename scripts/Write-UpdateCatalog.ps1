param(
    [Parameter(Mandatory=$true)][string[]]$Manifest,
    [string]$ExistingCatalog,
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$KeyPath=(Join-Path $env:LOCALAPPDATA 'CasterDesk/ReleaseKeys/update-signing.bin')
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$rsa=[Security.Cryptography.RSA]::Create()
function Read-Signed([string]$path){
    if((Get-Item -LiteralPath $path).Length -gt 512000){throw 'Arquivo de versões muito grande.'}
    $envelope=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $payload=[Convert]::FromBase64String($envelope.Payload)
    if(!$rsa.VerifyData($payload,[Convert]::FromBase64String($envelope.Signature),[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pss)){throw 'Assinatura inválida na lista ou manifesto anterior.'}
    return [Text.Encoding]::UTF8.GetString($payload) | ConvertFrom-Json
}
function Assert-Version($entry){
    if($entry.Schema -ne 1 -or $entry.Version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-beta(\.(0|[1-9]\d*))*)?$' -or $entry.Type -notin @('release','beta') -or (($entry.Type -eq 'beta') -ne $entry.Version.Contains('-')) -or $entry.Priority -notin @('normal','important') -or $null -eq $entry.Notes -or $entry.Notes.Length -gt 12000 -or $entry.Size -le 0 -or $entry.Size -gt 1GB -or $entry.Sha256 -notmatch '^[a-fA-F0-9]{64}$' -or [DateTimeOffset]$entry.PublishedAt -gt [DateTimeOffset]::UtcNow.AddDays(1)){throw 'Informações inválidas no catálogo de versões.'}
}
try {
    $bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($KeyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    $read=0;$rsa.ImportPkcs8PrivateKey($bytes,[ref]$read)
    if($rsa.ExportSubjectPublicKeyInfoPem().Replace("`r",'').Trim() -ne (Get-Content (Join-Path $root 'assets/update-public-key.pem') -Raw).Replace("`r",'').Trim()){throw 'Chave de assinatura incorreta.'}
    $versions=[ordered]@{}
    if($ExistingCatalog){
        $old=Read-Signed $ExistingCatalog
        if($old.Schema -ne 1 -or $null -eq $old.Versions){throw 'Catálogo anterior inválido.'}
        foreach($entry in $old.Versions){Assert-Version $entry;if($versions.Contains($entry.Version)){throw 'Versão duplicada no catálogo anterior.'};$versions[$entry.Version]=$entry}
    }
    foreach($path in $Manifest){$entry=Read-Signed $path;Assert-Version $entry;$versions[$entry.Version]=$entry}
    if($versions.Count -gt 200){throw 'Revise o catálogo: limite de 200 versões atingido.'}
    $catalog=[ordered]@{Schema=1;Versions=@($versions.Values | Sort-Object PublishedAt -Descending)}
    $payload=[Text.Encoding]::UTF8.GetBytes(($catalog | ConvertTo-Json -Depth 6 -Compress))
    $envelope=@{Payload=[Convert]::ToBase64String($payload);Signature=[Convert]::ToBase64String($rsa.SignData($payload,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pss))} | ConvertTo-Json -Compress
    if([Text.Encoding]::UTF8.GetByteCount($envelope) -gt 512000){throw 'Catálogo excede o limite do aplicativo.'}
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($Output),$envelope,[Text.UTF8Encoding]::new($false))
    Write-Output ([IO.Path]::GetFullPath($Output))
} finally {if($bytes){[Array]::Clear($bytes,0,$bytes.Length)};$rsa.Dispose()}
