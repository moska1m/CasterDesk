param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$folder=Join-Path (Split-Path $PSScriptRoot -Parent) 'assets/siege/maps'
foreach($file in Get-ChildItem -LiteralPath $folder -Filter '*.jpg') {
    $original=[System.Drawing.Bitmap]::new($file.FullName)
    $output=[System.Drawing.Bitmap]::new($original.Width,$original.Height)
    try {
        for($y=0;$y -lt $original.Height;$y++) { for($x=0;$x -lt $original.Width;$x++) {
            $c=$original.GetPixel($x,$y)
            $gray=[int]((0.299*$c.R+0.587*$c.G+0.114*$c.B)*0.60)
            $output.SetPixel($x,$y,[System.Drawing.Color]::FromArgb($gray,$gray,$gray))
        }}
        $output.Save((Join-Path $folder ($file.BaseName+'-unavailable.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $original.Dispose();$output.Dispose() }
}
