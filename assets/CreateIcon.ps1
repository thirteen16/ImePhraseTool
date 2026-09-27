Add-Type -AssemblyName System.Drawing
$frames = @()
foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
    $bitmap = [Drawing.Bitmap]::new($size, $size)
    $g = [Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.ScaleTransform($size / 256.0, $size / 256.0)
    $path = [Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc(8,8,64,64,180,90)
    $path.AddArc(184,8,64,64,270,90)
    $path.AddArc(184,184,64,64,0,90)
    $path.AddArc(8,184,64,64,90,90)
    $path.CloseFigure()
    $brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(37,99,235))
    $g.FillPath($brush, $path)
    $font = [Drawing.Font]::new('Microsoft YaHei UI',112,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
    $format = [Drawing.StringFormat]::new()
    $format.Alignment = [Drawing.StringAlignment]::Center
    $g.DrawString('文',$font,[Drawing.Brushes]::White,[Drawing.RectangleF]::new(20,16,216,148),$format)
    $pen = [Drawing.Pen]::new([Drawing.Color]::FromArgb(191,219,254),12)
    $pen.StartCap = [Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($pen,58,178,194,178)
    $g.DrawLines($pen,[Drawing.PointF[]]@([Drawing.PointF]::new(176,161),[Drawing.PointF]::new(195,178),[Drawing.PointF]::new(176,195)))
    $g.DrawLine($pen,62,214,198,214)
    $g.DrawLines($pen,[Drawing.PointF[]]@([Drawing.PointF]::new(80,197),[Drawing.PointF]::new(61,214),[Drawing.PointF]::new(80,231)))
    $stream = [IO.MemoryStream]::new()
    $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
    $frames += ,$stream.ToArray()
    $stream.Dispose(); $pen.Dispose(); $format.Dispose(); $font.Dispose(); $brush.Dispose(); $path.Dispose(); $g.Dispose(); $bitmap.Dispose()
}
$output = [IO.File]::Create((Join-Path $PSScriptRoot 'app.ico'))
$writer = [IO.BinaryWriter]::new($output)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
$offset = 6 + 16 * $frames.Count
$sizes = @(16,24,32,48,64,128,256)
for ($i=0; $i -lt $frames.Count; $i++) {
    $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension)
    $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
    $offset += $frames[$i].Length
}
foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
$writer.Dispose()
