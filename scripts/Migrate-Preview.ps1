param(
    [Parameter(Mandatory=$true)][string]$PreviewFolder,
    [string]$DataFolder = (Join-Path $env:LOCALAPPDATA 'CasterDesk')
)
$ErrorActionPreference = 'Stop'
if (Get-Process CasterDesk -ErrorAction SilentlyContinue) { throw 'Feche o CasterDesk antes de migrar.' }
$source = (Resolve-Path -LiteralPath $PreviewFolder).Path
if (!(Test-Path -LiteralPath (Join-Path $source 'CasterDesk.exe'))) { throw 'Selecione a pasta app da prévia do CasterDesk.' }
if ((Test-Path -LiteralPath $DataFolder) -and (Get-ChildItem -LiteralPath $DataFolder -Force | Select-Object -First 1)) {
    throw 'A pasta de destino já contém dados. Use importação de partidas pelo aplicativo para evitar sobrescrever o histórico.'
}
New-Item -ItemType Directory -Force $DataFolder | Out-Null
foreach ($name in @('settings.json','casting-settings.json','export-settings.json','Partidas','Exportacao')) {
    $item = Join-Path $source $name
    if (Test-Path -LiteralPath $item) { Copy-Item -LiteralPath $item -Destination $DataFolder -Recurse }
}
# Preserve existing OBS output paths. Only copied logo references need relocation.
$brandPath = Join-Path $DataFolder 'export-settings.json'
if (Test-Path -LiteralPath $brandPath) {
    $brands = Get-Content -LiteralPath $brandPath -Raw | ConvertFrom-Json
    foreach ($property in $brands.PSObject.Properties) {
        $logo = $property.Value.LogoFile
        if ($logo -and $logo.StartsWith((Join-Path $source 'Exportacao'), [StringComparison]::OrdinalIgnoreCase)) {
            $property.Value.LogoFile = Join-Path $DataFolder $logo.Substring($source.Length).TrimStart('\','/')
        }
    }
    $brands | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $brandPath -Encoding utf8
}
Write-Output 'Dados copiados. A prévia original foi preservada. Confira os caminhos TXT do OBS antes da transmissão.'
