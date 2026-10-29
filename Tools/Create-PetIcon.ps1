# Generate a multi-resolution Windows application icon from Capies' first sprite frame.
# No extra Python modules or external tools required.
param(
    [string]$Source = (Join-Path $PSScriptRoot '..\Assets\Capies-sleep.png'),
    [string]$Output = (Join-Path $PSScriptRoot '..\Assets\Capies.ico'),
    [string]$Preview = (Join-Path $PSScriptRoot '..\Assets\Capies-icon-preview.png')
)

Add-Type -AssemblyName System.Drawing
$sheet = [System.Drawing.Bitmap]::FromFile((Resolve-Path $Source).Path)
try {
    if ($sheet.Width -ne 1280 -or $sheet.Height -ne 1280) {
        throw 'Expected 1280x1280 PNG sprite sheet with a 5x5 layout.'
    }

    # Only scan the first 256x256 frame to avoid including adjacent sprites.
    $left = 256; $top = 256; $right = -1; $bottom = -1
    for ($y = 0; $y -lt 256; $y++) {
        for ($x = 0; $x -lt 256; $x++) {
            if ($sheet.GetPixel($x,$y).A -gt 20) {
                if ($x -lt $left)   { $left = $x }
                if ($x -gt $right)  { $right = $x }
                if ($y -lt $top)    { $top = $y }
                if ($y -gt $bottom) { $bottom = $y }
            }
        }
    }
    if ($right -lt $left) { throw 'Could not find any non-transparent sprite pixels.' }

    $bounds = [System.Drawing.Rectangle]::new($left,$top,$right-$left+1,$bottom-$top+1)
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $streams = New-Object 'System.Collections.Generic.List[byte[]]'
    foreach ($size in $sizes) {
        $bitmap = [System.Drawing.Bitmap]::new($size,$size,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphic = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphic.Clear([System.Drawing.Color]::Transparent)
            $graphic.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphic.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphic.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphic.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

            $scale = ([double]$size * 0.89) / [double]([Math]::Max($bounds.Width,$bounds.Height))
            $w = [single]($bounds.Width * $scale)
            $h = [single]($bounds.Height * $scale)
            $destination = [System.Drawing.RectangleF]::new(
                [single](($size-$w)/2),[single](($size-$h)/2),$w,$h)
            $graphic.DrawImage($sheet,$destination,$bounds,[System.Drawing.GraphicsUnit]::Pixel)
        }
        finally { $graphic.Dispose() }

        if ($size -eq 256) {
            $bitmap.Save($Preview,[System.Drawing.Imaging.ImageFormat]::Png)
        }
        $stream = [System.IO.MemoryStream]::new()
        try {
            $bitmap.Save($stream,[System.Drawing.Imaging.ImageFormat]::Png)
            $streams.Add($stream.ToArray())
        }
        finally { $stream.Dispose(); $bitmap.Dispose() }
    }

    $file = [System.IO.File]::Create($Output)
    $writer = [System.IO.BinaryWriter]::new($file)
    try {
        $writer.Write([uint16]0) # reserved
        $writer.Write([uint16]1) # icon
        $writer.Write([uint16]$sizes.Count)
        $offset = 6 + $sizes.Count * 16
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $px = [int]$sizes[$i]
            $writer.Write([byte]($px % 256)) # 0 indicates 256px
            $writer.Write([byte]($px % 256))
            $writer.Write([byte]0) # colors
            $writer.Write([byte]0) # reserved
            $writer.Write([uint16]1) # planes
            $writer.Write([uint16]32) # bit depth
            $writer.Write([uint32]$streams[$i].Length)
            $writer.Write([uint32]$offset)
            $offset += $streams[$i].Length
        }
        foreach ($bytes in $streams) {
            $writer.Write([byte[]]$bytes)
        }
    }
    finally { $writer.Dispose() }

    Write-Output "Created $Output"
    Write-Output "Preview $Preview"
    Write-Output "Sprite bounds: $($bounds.X),$($bounds.Y) $($bounds.Width)x$($bounds.Height)"
    Write-Output "Icon sizes: $($sizes -join ', ')"
}
finally { $sheet.Dispose() }