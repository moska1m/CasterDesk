param([Parameter(Mandatory=$true)][string]$Executable,[ValidateRange(1024,65535)][int]$Port=43828,[switch]$Remove)
$ErrorActionPreference='Stop'
$program=[IO.Path]::GetFullPath($Executable)
if([IO.Path]::GetFileName($program) -cne 'CasterDesk.exe'){throw 'Executável inválido.'}
if(!$Remove -and !(Test-Path -LiteralPath $program -PathType Leaf)){throw 'CasterDesk não encontrado.'}
$sha=[Security.Cryptography.SHA256]::Create()
try{$hash=([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($program.ToUpperInvariant())))).Replace('-','')}finally{$sha.Dispose()}
$name='CasterDesk-Local-'+$hash.Substring(0,16)
Get-NetFirewallRule -Name $name -ErrorAction SilentlyContinue | Remove-NetFirewallRule
if(!$Remove){New-NetFirewallRule -Name $name -DisplayName 'CasterDesk - controle na rede local' -Direction Inbound -Action Allow -Enabled True -Profile Private -Program $program -Protocol TCP -LocalPort $Port -RemoteAddress LocalSubnet -EdgeTraversalPolicy Block | Out-Null}
