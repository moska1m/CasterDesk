param([string]$KeyPath=(Join-Path $env:LOCALAPPDATA 'CasterDesk/ReleaseKeys/update-signing.bin'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$rsa=[Security.Cryptography.RSA]::Create(4096)
try {
    if(Test-Path -LiteralPath $KeyPath){
        $protected=[IO.File]::ReadAllBytes($KeyPath)
        $bytes=[Security.Cryptography.ProtectedData]::Unprotect($protected,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
        $read=0;$rsa.ImportPkcs8PrivateKey($bytes,[ref]$read)
    } else {
        New-Item -ItemType Directory -Path (Split-Path $KeyPath -Parent) -Force | Out-Null
        $bytes=$rsa.ExportPkcs8PrivateKey()
        $protected=[Security.Cryptography.ProtectedData]::Protect($bytes,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
        [IO.File]::WriteAllBytes($KeyPath,$protected)
    }
    $public=Join-Path $root 'assets/update-public-key.pem'
    $pem=$rsa.ExportSubjectPublicKeyInfoPem()
    if((Test-Path -LiteralPath $public) -and ((Get-Content -LiteralPath $public -Raw).Replace("`r",'').Trim() -ne $pem.Replace("`r",'').Trim())){throw 'A chave existente não corresponde à chave pública do projeto. Não substitua a chave usada pelos aplicativos instalados.'}
    [IO.File]::WriteAllText($public,$pem)
    Write-Output "Chave de atualização protegida pelo Windows: $KeyPath"
    Write-Output "Chave pública do projeto: $public"
} finally {if($bytes){[Array]::Clear($bytes,0,$bytes.Length)};$rsa.Dispose()}
