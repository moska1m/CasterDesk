param([Parameter(Mandatory=$true)][string]$Iscc,[string]$SignTool,[string]$CertificateThumbprint,[uri]$TimestampUrl)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (($SignTool -or $CertificateThumbprint -or $TimestampUrl) -and !($SignTool -and $CertificateThumbprint -and $TimestampUrl)) { throw 'Informe SignTool, CertificateThumbprint e TimestampUrl juntos.' }
Push-Location $root
try {
    dotnet publish source/CasterDesk.Legacy.csproj -c Release -r win-x64 --self-contained true -o publish
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o aplicativo.' }
    # Removed duplicate assets must not remain in a reused publication directory.
    $studioPublish = (Resolve-Path 'publish/assets/studio').Path
    $publishRoot = (Resolve-Path 'publish').Path + [IO.Path]::DirectorySeparatorChar
    if (!$studioPublish.StartsWith($publishRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Pasta de publicação inválida.' }
    $expected = Get-ChildItem 'assets/studio' -File | Select-Object -ExpandProperty Name
    Get-ChildItem -LiteralPath $studioPublish -File | Where-Object { $_.Name -notin $expected } | ForEach-Object { Remove-Item -LiteralPath $_.FullName }

    if ($SignTool) {
        foreach ($file in @('publish/CasterDesk.exe','publish/CasterDesk.dll')) {
            & "$PSScriptRoot/Sign-File.ps1" -SignTool $SignTool -CertificateThumbprint $CertificateThumbprint -TimestampUrl $TimestampUrl -File $file
        }
    } else { Write-Warning 'Beta SEM assinatura digital. O SmartScreen ainda pode mostrar avisos.' }
    & $Iscc installer/CasterDesk-Legacy.iss
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o instalador.' }
    $version=([xml](Get-Content source/CasterDesk.Legacy.csproj -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    Get-Item "dist/CasterDesk-$version-win-x64-Setup.exe" | ForEach-Object {
        if ($SignTool) { & "$PSScriptRoot/Sign-File.ps1" -SignTool $SignTool -CertificateThumbprint $CertificateThumbprint -TimestampUrl $TimestampUrl -File $_.FullName }
        $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($_.Name)" | Set-Content ($_.FullName + '.sha256') -Encoding ascii
    }
} finally { Pop-Location }
