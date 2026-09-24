param(
    [Parameter(Mandatory=$true)][string]$SignTool,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-fA-F0-9]{40}$')][string]$CertificateThumbprint,
    [Parameter(Mandatory=$true)][uri]$TimestampUrl,
    [Parameter(Mandatory=$true)][string]$File
)
$ErrorActionPreference='Stop'
if ($TimestampUrl.Scheme -notin @('https','http')) { throw 'Use o servidor de timestamp indicado pela certificadora.' }
$target=(Resolve-Path -LiteralPath $File).Path
& $SignTool sign /sha1 $CertificateThumbprint /s My /fd SHA256 /tr $TimestampUrl.AbsoluteUri /td SHA256 /d CasterDesk $target
if ($LASTEXITCODE -ne 0) { throw 'A assinatura falhou. O arquivo não deve ser publicado como assinado.' }
& $SignTool verify /pa /all /tw $target
if ($LASTEXITCODE -ne 0) { throw 'A validação da assinatura ou do timestamp falhou.' }
