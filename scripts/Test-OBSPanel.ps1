param([string]$PluginRoot=(Join-Path (Split-Path $PSScriptRoot -Parent) 'obs-plugin'))
$ErrorActionPreference='Stop'
$previousPath=$env:PATH;$previousPlugins=$env:QT_PLUGIN_PATH;$previousPlatform=$env:QT_QPA_PLATFORM
try {
    $qt=Get-ChildItem (Join-Path $PluginRoot '.deps') -Recurse -Filter Qt6Core.dll | Select-Object -First 1
    if(!$qt){throw 'Runtime Qt ausente.'}
    $depBin=Get-ChildItem (Join-Path $PluginRoot '.deps') -Directory | ForEach-Object {Join-Path $_.FullName 'bin'}
    $env:PATH=$qt.DirectoryName+';'+($depBin -join ';')+';'+$env:PATH
    $env:QT_PLUGIN_PATH=Join-Path $qt.Directory.Parent.FullName 'plugins';$env:QT_QPA_PLATFORM='minimal'
    if(-not ('NativeHeadlessMode' -as [type])){Add-Type -TypeDefinition 'using System.Runtime.InteropServices; public static class NativeHeadlessMode { [DllImport("kernel32.dll")] public static extern uint SetErrorMode(uint mode); }'}
    [NativeHeadlessMode]::SetErrorMode(3) | Out-Null
    $test=Start-Process -FilePath (Join-Path $PluginRoot 'build_x64/RelWithDebInfo/casterdesk-panel-checks.exe') -WorkingDirectory $PluginRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $PluginRoot 'panel-test.out') -RedirectStandardError (Join-Path $PluginRoot 'panel-test.err')
    if(!$test.WaitForExit(45000)){Stop-Process -Id $test.Id -Force;throw 'Tempo limite nos testes nativos.'}
    Get-Content (Join-Path $PluginRoot 'panel-test.out'),(Join-Path $PluginRoot 'panel-test.err')
    if($test.ExitCode -ne 0){throw "Falha nos testes nativos: $($test.ExitCode)"}
} finally {$env:PATH=$previousPath;$env:QT_PLUGIN_PATH=$previousPlugins;$env:QT_QPA_PLATFORM=$previousPlatform}
