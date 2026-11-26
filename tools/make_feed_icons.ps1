# Erzeugt Icons\Feed_<Id>.png (512x512): Futtersack aus Sackleinen mit Futter in der Biomfarbe und farbigem Leuchten.
# Platzhalter-Icons im Stil der Mod, bis gemalte Icons vorliegen. Aufruf: powershell -File tools\make_feed_icons.ps1
Add-Type -AssemblyName System.Drawing
$out = Join-Path $PSScriptRoot '..\Icons'
# Id, Futterfarbe (R,G,B), Leuchtfarbe (R,G,B) - Wiese hell gelbgruen, Sumpf dunkles Moorgruen
$feeds = @(
    @('TamingHerb',   @(150, 215, 60),  @(185, 255, 80)),
    @('FreyaLure',    @(225, 95, 150),  @(255, 120, 185)),
    @('NornBlessing', @(30, 95, 40),    @(20, 120, 40)),
    @('FenrirBite',   @(150, 70, 60),   @(140, 205, 255)),
    @('ArmorGrain',   @(225, 180, 70),  @(255, 205, 80)),
    @('EmberShell',   @(200, 70, 30),   @(255, 110, 40))
)
function New-Color($a, $rgb) { [Drawing.Color]::FromArgb($a, $rgb[0], $rgb[1], $rgb[2]) }

# Symbolfeld (60 x 56) -> Pixel auf der Sackfront
$SymS = 3.0; $SymX = 256 - 30 * $SymS; $SymY = 292
function Get-P([double]$x, [double]$y) { New-Object Drawing.PointF ([float]($SymX + $x * $SymS)), ([float]($SymY + $y * $SymS)) }
function Get-Pts($list) { [Drawing.PointF[]]@($list | ForEach-Object { Get-P $_[0] $_[1] }) }
function Get-LeafPts($base, $tip, $width, $n = 10) {
    $dx = $tip[0] - $base[0]; $dy = $tip[1] - $base[1]; $ln = [Math]::Sqrt($dx * $dx + $dy * $dy); $nx = -$dy / $ln; $ny = $dx / $ln
    $a = for ($i = 0; $i -lt $n; $i++) { $t = $i / ($n - 1); $w = $width * [Math]::Sin([Math]::PI * $t); , @(($base[0] + $dx * $t + $nx * $w), ($base[1] + $dy * $t + $ny * $w)) }
    $b = for ($i = $n - 2; $i -ge 1; $i--) { $t = $i / ($n - 1); $w = $width * [Math]::Sin([Math]::PI * $t); , @(($base[0] + $dx * $t - $nx * $w), ($base[1] + $dy * $t - $ny * $w)) }
    @($a) + @($b)
}
# Bartaxt wie in tools/make_feeds.py (AXE_HEAD, AXE_EDGE, AXE_CURL, axe_geom)
$AxeHead = @(@(2.5,0),@(3.4,4),@(4.6,9),@(5.6,13),@(3,14.6),@(-1,15.3),@(-5,14.8),@(-9,13.6),@(-12.5,11.5),@(-10.5,9.2),@(-7.6,6.8),@(-5.4,4.4),@(-3.8,2.2),@(-3.2,0),@(-3.2,-2.6),@(2.5,-2.6))
$AxeEdge = @(@(5.2,13.3),@(3,14.3),@(-1,15.0),@(-5,14.5),@(-9,13.3),@(-12.1,11.4))
$AxeCurl = for ($i = 0; $i -lt 22; $i++) { $a = 3.2 * [Math]::PI * $i / 21; $r = 2.6 - 2.0 * $i / 21; , @((-2.0 + $r * [Math]::Cos($a)), (8.8 + $r * [Math]::Sin($a))) }
function Get-AxeGeom($A, $B, $side, $S = 1.4) {
    $dx = $B[0] - $A[0]; $dy = $B[1] - $A[1]; $L = [Math]::Sqrt($dx * $dx + $dy * $dy); $ux = $dx / $L; $uy = $dy / $L
    $nx = -$uy; $ny = $ux; if ($nx * $side[0] + $ny * $side[1] -lt 0) { $nx = -$nx; $ny = -$ny }
    $loc = { param($p) , @(($B[0] + ($ux * $p[0] + $nx * $p[1]) * $S), ($B[1] + ($uy * $p[0] + $ny * $p[1]) * $S)) }
    $bend = 2.4
    $haft = @($A, @(($A[0] + $dx * .25 - $nx * $bend * .7), ($A[1] + $dy * .25 - $ny * $bend * .7)), @(($A[0] + $dx * .55 - $nx * $bend), ($A[1] + $dy * .55 - $ny * $bend)),
              @(($A[0] + $dx * .82 - $nx * $bend * .4), ($A[1] + $dy * .82 - $ny * $bend * .4)), $B, (& $loc @(4.2, 0)))
    @{ Haft = $haft; Head = @($AxeHead | ForEach-Object { , (& $loc $_) }); Edge = @($AxeEdge | ForEach-Object { , (& $loc $_) }); Curl = @($AxeCurl | ForEach-Object { , (& $loc $_) }) }
}

function Draw-Symbol($g, $id, $glow) {
    $col = New-Color 240 $glow
    $brush = New-Object Drawing.SolidBrush $col
    $pen = New-Object Drawing.Pen $col, 11; $pen.StartCap = 'Round'; $pen.EndCap = 'Round'; $pen.LineJoin = 'Round'
    $dark = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 70, 48, 26)), 4
    $iron = [Drawing.Color]::FromArgb(255, 77, 77, 82)
    switch ($id) {
        'TamingHerb' {
            $g.DrawCurve($pen, (Get-Pts @(@(30, 47), @(30, 36), @(30.5, 24), @(30, 13))))
            foreach ($l in @(@(@(30, 42), @(13, 22), 6.5), @(@(30, 33), @(46, 15), 6), @(@(30, 22), @(22, 7), 4))) {
                $g.FillPolygon($brush, (Get-Pts (Get-LeafPts $l[0] $l[1] $l[2])))
            }
        }
        'FreyaLure' {
            $pts = for ($i = 0; $i -lt 60; $i++) { $t = 2 * [Math]::PI * $i / 60
                $x = 16 * [Math]::Pow([Math]::Sin($t), 3); $y = 13 * [Math]::Cos($t) - 5 * [Math]::Cos(2 * $t) - 2 * [Math]::Cos(3 * $t) - [Math]::Cos(4 * $t)
                , @((30 + $x * 1.05), (25 - $y * 1.05)) }
            $g.FillPolygon($brush, (Get-Pts $pts))
        }
        'NornBlessing' {
            $pts = for ($i = 0; $i -lt 10; $i++) { $a = -[Math]::PI / 2 + $i * [Math]::PI / 5; $r = if ($i % 2 -eq 0) { 18 } else { 7.5 }
                , @((30 + $r * [Math]::Cos($a)), (27 + $r * [Math]::Sin($a))) }
            $g.FillPolygon($brush, (Get-Pts $pts))
        }
        'ArmorGrain' {
            $c = Get-P 12 10; $d = 36 * $SymS
            $g.FillEllipse($brush, $c.X, $c.Y, $d, $d)
            foreach ($x in 21, 27, 33, 39) { $h = [Math]::Sqrt([Math]::Max(18 * 18 - ($x - 30) * ($x - 30), 0)) - 1
                $g.DrawLine($dark, (Get-P $x (28 - $h)), (Get-P $x (28 + $h))) }
            $g.DrawEllipse((New-Object Drawing.Pen $iron, 9), $c.X, $c.Y, $d, $d)
            $b = Get-P 30 28; $br = 6.5 * $SymS
            $g.FillEllipse((New-Object Drawing.SolidBrush $iron), $b.X - $br, $b.Y - $br, 2 * $br, 2 * $br)
            $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(110, 255, 255, 255))), $b.X - $br * .5, $b.Y - $br * .6, $br * .7, $br * .5)
        }
        'EmberShell' {
            $pen.Width = 9
            $g.DrawPolygon($pen, (Get-Pts @(@(16, 10), @(44, 10), @(44, 26), @(42, 34), @(37, 41), @(30, 47), @(23, 41), @(18, 34), @(16, 26))))
            $g.FillClosedCurve($brush, (Get-Pts @(@(30, 42), @(24, 37), @(23, 30), @(26, 24), @(28.5, 29), @(29, 22), @(33, 14), @(34, 22), @(37.5, 28), @(37, 36))), 'Alternate', 0.3)
        }
        'FenrirBite' {
            # zwei gekreuzte Bartaexte: dunkle Stiele mit Knauf, eisblaue Klingen mit heller Schneide und Gravur
            $axes = @((Get-AxeGeom @(14, 55) @(39, 17) @(1, .2)), (Get-AxeGeom @(46, 55) @(21, 17) @(-1, .2)))
            $wood = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 44, 33, 28)), 10; $wood.StartCap = 'Round'; $wood.EndCap = 'Round'
            $woodHi = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(150, 110, 85, 64)), 2.5
            foreach ($ax in $axes) {
                $pts = Get-Pts $ax.Haft
                $g.DrawCurve($wood, $pts); $g.DrawCurve($woodHi, $pts)
                $k = $pts[0]; $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 44, 33, 28))), $k.X - 9, $k.Y - 9, 18, 18)
            }
            $outline = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, [int]($glow[0] * .45), [int]($glow[1] * .5), [int]($glow[2] * .6))), 3; $outline.LineJoin = 'Round'
            $edge = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 235, 248, 255)), 4.5; $edge.StartCap = 'Round'; $edge.EndCap = 'Round'
            $engr = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(220, 40, 60, 80)), 2.4; $engr.StartCap = 'Round'; $engr.EndCap = 'Round'
            foreach ($ax in $axes) {
                $hp = Get-Pts $ax.Head
                $g.FillPolygon($brush, $hp); $g.DrawPolygon($outline, $hp)
                $g.DrawCurve($edge, (Get-Pts $ax.Edge))
                $g.DrawCurve($engr, (Get-Pts $ax.Curl))
            }
        }
        default { }
    }
}

foreach ($f in $feeds) {
    $id = $f[0]; $food = $f[1]; $glow = $f[2]
    $bmp = New-Object Drawing.Bitmap 512, 512, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.InterpolationMode = 'HighQualityBicubic'; $g.Clear([Drawing.Color]::Transparent)

    # kein Schein mehr (Valheim-Stil)

    # Sackkoerper
    $sack = New-Object Drawing.Drawing2D.GraphicsPath
    $sack.AddBezier(150, 200, 70, 290, 80, 450, 175, 470)
    $sack.AddLine(175, 470, 337, 470)
    $sack.AddBezier(337, 470, 432, 450, 442, 290, 362, 200)
    $sack.CloseFigure()
    $sb = New-Object Drawing.Drawing2D.LinearGradientBrush (New-Object Drawing.Rectangle 80, 190, 360, 290), ([Drawing.Color]::FromArgb(255, 196, 160, 112)), ([Drawing.Color]::FromArgb(255, 112, 82, 50)), 35
    $g.FillPath($sb, $sack)
    # Sackleinen-Struktur
    $weave = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(45, 60, 40, 20)), 3
    $g.SetClip($sack)
    for ($x = 60; $x -lt 460; $x += 14) { $g.DrawLine($weave, $x, 190, $x - 30, 480) }
    for ($y = 200; $y -lt 480; $y += 14) { $g.DrawLine($weave, 60, $y, 460, $y + 10) }
    $g.ResetClip()
    $g.DrawPath((New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 70, 48, 26)), 7), $sack)

    # Offener Rand mit Futter
    $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 150, 116, 74))), 128, 150, 256, 100)
    $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 84, 60, 34))), 146, 162, 220, 76)
    $rnd = New-Object Random ($id.GetHashCode())
    for ($i = 0; $i -lt 70; $i++) {
        $a = $rnd.NextDouble() * [Math]::PI * 2; $r = [Math]::Sqrt($rnd.NextDouble())
        $px = 256 + [Math]::Cos($a) * $r * 100; $py = 196 + [Math]::Sin($a) * $r * 30 - (1 - $r) * 34
        $k = 0.75 + $rnd.NextDouble() * 0.45
        $c = [Drawing.Color]::FromArgb(255, [Math]::Min(255, [int]($food[0] * $k)), [Math]::Min(255, [int]($food[1] * $k)), [Math]::Min(255, [int]($food[2] * $k)))
        $s = 14 + $rnd.Next(12)
        $g.FillEllipse((New-Object Drawing.SolidBrush $c), [float]($px - $s / 2), [float]($py - $s / 2), $s, [float]($s * 0.8))
        $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(90, 255, 255, 255))), [float]($px - $s / 4), [float]($py - $s / 3), [float]($s / 3), [float]($s / 4))
    }
    $g.DrawEllipse((New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 70, 48, 26)), 7), 128, 150, 256, 100)

    # Kordel
    $cord = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 190, 150, 70)), 12
    $g.DrawBezier($cord, 120, 262, 200, 290, 312, 290, 392, 262)
    $g.DrawBezier($cord, 330, 284, 360, 320, 350, 360, 372, 392)
    $g.FillEllipse((New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 160, 120, 50))), 314, 270, 30, 30)

    # Symbol in Leuchtfarbe auf dem Sack (gleiche Formen wie tools/make_feeds.py, Feld 60 x 56)
    Draw-Symbol $g $id $glow

    $g.Dispose()
    $path = Join-Path $out ("Feed_" + $id + ".png")
    $bmp.Save($path, [Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    "erstellt: $path"
}

# Valheim-Stil: Licht, Maltextur, Umriss, Schatten
& (Join-Path $PSScriptRoot 'stylize_icons.ps1') (Join-Path $out 'Feed_*.png')
