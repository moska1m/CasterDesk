param([string]$ImagePath,[switch]$Diagnostic)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null=[Windows.Storage.StorageFile,Windows.Storage,ContentType=WindowsRuntime]
$null=[Windows.Storage.Streams.IRandomAccessStream,Windows.Storage.Streams,ContentType=WindowsRuntime]
$null=[Windows.Graphics.Imaging.BitmapDecoder,Windows.Graphics.Imaging,ContentType=WindowsRuntime]
$null=[Windows.Graphics.Imaging.SoftwareBitmap,Windows.Graphics.Imaging,ContentType=WindowsRuntime]
$null=[Windows.Media.Ocr.OcrEngine,Windows.Foundation,ContentType=WindowsRuntime]
$null=[Windows.Media.Ocr.OcrResult,Windows.Foundation,ContentType=WindowsRuntime]
if($Diagnostic){[pscustomobject]@{Available=($null -ne [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages());Languages=@([Windows.Media.Ocr.OcrEngine]::AvailableRecognizerLanguages | ForEach-Object LanguageTag)} | ConvertTo-Json -Compress;exit}
if([string]::IsNullOrWhiteSpace($ImagePath)){throw 'ImagePath is required'}
$asTask=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {$_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetGenericArguments().Count -eq 1 -and $_.GetParameters().Count -eq 1} | Select-Object -First 1
function Await($operation,$type) { $task=$asTask.MakeGenericMethod($type).Invoke($null,@($operation)); try{$task.Wait()}catch{throw $task.Exception.InnerException}; $task.Result }
$file=Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($ImagePath)) ([Windows.Storage.StorageFile])
$stream=Await ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
try {
    $decoder=Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
    $bitmap=Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
    try {
        $engine=[Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
        if($null -eq $engine){throw 'OCR language pack unavailable'}
        $result=Await ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
        $words=@(foreach($line in $result.Lines){foreach($word in $line.Words){[pscustomobject]@{Text=$word.Text;X=$word.BoundingRect.X;Y=$word.BoundingRect.Y;Width=$word.BoundingRect.Width;Height=$word.BoundingRect.Height}}})
        [pscustomobject]@{Width=$bitmap.PixelWidth;Height=$bitmap.PixelHeight;Words=$words} | ConvertTo-Json -Depth 4 -Compress
    } finally { $bitmap.Dispose() }
} finally {$stream.Dispose()}
