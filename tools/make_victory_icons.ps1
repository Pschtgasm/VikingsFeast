# Erzeugt Icons\Victory_<Boss>.png (512x512): Item-Icons der 8 Siegesmaehler (geplant 1.8.0, siehe KONZEPT_SIEGESMAHL.md).
# Holzplatte mit dem Festmahl und den Zutaten des Rezepts, dahinter (kleiner) das Wahrzeichen des Bosses,
# Schein in der Boss-Farbe, Siegesband vorne. Gezeichnete Icons. Aufruf: powershell -File tools\make_victory_icons.ps1
Add-Type -AssemblyName System.Drawing
$out = Join-Path $PSScriptRoot '..\Icons'
$rnd = New-Object Random 7

function Col($a, $r, $g, $b) { [Drawing.Color]::FromArgb([int]$a, [int][Math]::Min(255, [Math]::Max(0, $r)), [int][Math]::Min(255, [Math]::Max(0, $g)), [int][Math]::Min(255, [Math]::Max(0, $b))) }
function Br($c) { New-Object Drawing.SolidBrush $c }
function Pn($c, $w) { $p = New-Object Drawing.Pen $c, ([float]$w); $p.StartCap = 'Round'; $p.EndCap = 'Round'; $p.LineJoin = 'Round'; $p }
function Pt($x, $y) { New-Object Drawing.PointF ([float]$x), ([float]$y) }
function Pts($list) { [Drawing.PointF[]]@($list | ForEach-Object { Pt $_[0] $_[1] }) }
function Ell($g, $b, $x, $y, $w, $h) { $g.FillEllipse($b, [float]$x, [float]$y, [float]$w, [float]$h) }
function EllO($g, $p, $x, $y, $w, $h) { $g.DrawEllipse($p, [float]$x, [float]$y, [float]$w, [float]$h) }
function Grad($x, $y, $w, $h, $c1, $c2, $ang) { New-Object Drawing.Drawing2D.LinearGradientBrush (New-Object Drawing.RectangleF ([float]$x), ([float]$y), ([float]$w), ([float]$h)), $c1, $c2, ([float]$ang) }
function Shine($g, $x, $y, $w, $h) { Ell $g (Br (Col 130 255 255 255)) $x $y $w $h }

function Draw-Halo($g, $rgb) {
    $path = New-Object Drawing.Drawing2D.GraphicsPath; $path.AddEllipse(26, 20, 460, 460)
    $b = New-Object Drawing.Drawing2D.PathGradientBrush $path
    $b.CenterColor = Col 170 $rgb[0] $rgb[1] $rgb[2]; $b.SurroundColors = @((Col 0 $rgb[0] $rgb[1] $rgb[2]))
    $g.FillPath($b, $path)
}
function Draw-Platter($g) {
    Ell $g (Br (Col 90 0 0 0)) 50 306 416 150
    Ell $g (Br (Col 255 74 46 24)) 40 256 432 190
    Ell $g (Grad 54 260 404 168 (Col 255 172 120 68) (Col 255 112 72 38) 90) 54 260 404 168
    $grain = Pn (Col 70 60 36 18) 3
    foreach ($k in 0..4) { $g.DrawArc($grain, [float](90 + $k * 12), [float](280 + $k * 6), [float](332 - $k * 24), [float](130 - $k * 12), 200, 140) }
    EllO $g (Pn (Col 255 58 36 18) 5) 54 260 404 168
}
function Draw-Ribbon($g, $rgb) {
    $c = Col 255 $rgb[0] $rgb[1] $rgb[2]; $dark = Col 255 ($rgb[0] * .55) ($rgb[1] * .55) ($rgb[2] * .55)
    $g.FillPolygon((Br $dark), (Pts @(@(112, 424), @(146, 410), @(146, 458), @(112, 472), @(122, 448))))
    $g.FillPolygon((Br $dark), (Pts @(@(400, 424), @(366, 410), @(366, 458), @(400, 472), @(390, 448))))
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $path.AddBezier((Pt 142 410), (Pt 218 434), (Pt 294 434), (Pt 370 410)); $path.AddLine((Pt 370 410), (Pt 370 452))
    $path.AddBezier((Pt 370 452), (Pt 294 476), (Pt 218 476), (Pt 142 452)); $path.CloseFigure()
    $g.FillPath((Br $c), $path); $g.DrawPath((Pn (Col 255 230 196 110) 3), $path)
    $s = for ($i = 0; $i -lt 10; $i++) { $a = -[Math]::PI / 2 + $i * [Math]::PI / 5; $r = if ($i % 2 -eq 0) { 15 } else { 6.5 }; , @((256 + $r * [Math]::Cos($a)), (443 + $r * [Math]::Sin($a))) }
    $g.FillPolygon((Br (Col 255 250 222 140)), (Pts $s))
}

# ---------------- Speisen und Zutaten
function Draw-Roast($g, $cx, $cy, $k, $crust, $glow) {
    Ell $g (Br (Col 120 40 20 10)) ($cx - 112 * $k) ($cy + 28 * $k) (224 * $k) (60 * $k)
    Ell $g (Grad ($cx - 110 * $k) ($cy - 70 * $k) (220 * $k) (130 * $k) (Col 255 $crust[0] $crust[1] $crust[2]) (Col 255 ($crust[0] * .55) ($crust[1] * .5) ($crust[2] * .45)) 70) ($cx - 110 * $k) ($cy - 70 * $k) (220 * $k) (130 * $k)
    $g.DrawArc((Pn (Col 110 255 230 180) (6 * $k)), [float]($cx - 80 * $k), [float]($cy - 58 * $k), [float](140 * $k), [float](70 * $k), 200, 110)
    foreach ($i in 0..3) { $g.DrawLine((Pn (Col 120 60 28 10) (4 * $k)), [float]($cx - 60 * $k + $i * 38 * $k), [float]($cy - 50 * $k), [float]($cx - 76 * $k + $i * 38 * $k), [float]($cy + 40 * $k)) }
    $bone = Col 255 236 226 200
    $g.DrawLine((Pn $bone (16 * $k)), [float]($cx + 86 * $k), [float]($cy - 30 * $k), [float]($cx + 134 * $k), [float]($cy - 62 * $k))
    Ell $g (Br $bone) ($cx + 124 * $k) ($cy - 82 * $k) (22 * $k) (22 * $k); Ell $g (Br $bone) ($cx + 136 * $k) ($cy - 68 * $k) (22 * $k) (22 * $k)
    if ($glow) { foreach ($i in 0..9) { $x = $cx + ($rnd.NextDouble() - .5) * 170 * $k; $y = $cy + ($rnd.NextDouble() - .6) * 90 * $k; Ell $g (Br (Col 230 $glow[0] $glow[1] $glow[2])) ($x - 5 * $k) ($y - 5 * $k) (10 * $k) (10 * $k) } }
}
function Draw-Bowl($g, $cx, $cy, $k, $soup) {
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $path.AddArc([float]($cx - 104 * $k), [float]($cy - 70 * $k), [float](208 * $k), [float](150 * $k), 0, 180); $path.CloseFigure()
    $g.FillPath((Grad ($cx - 104 * $k) ($cy - 10 * $k) (208 * $k) (90 * $k) (Col 255 120 78 42) (Col 255 64 40 20) 90), $path)
    Ell $g (Br (Col 255 90 58 30)) ($cx - 104 * $k) ($cy - 22 * $k) (208 * $k) (44 * $k)
    Ell $g (Br (Col 255 $soup[0] $soup[1] $soup[2])) ($cx - 92 * $k) ($cy - 16 * $k) (184 * $k) (34 * $k)
}
function Draw-Carrot($g, $x, $y, $ang, $k = 1.0) {
    $s = [Math]::Sin($ang); $c = [Math]::Cos($ang)
    $g.FillPolygon((Br (Col 255 232 120 30)), (Pts @(@(($x - 9 * $k * $s), ($y + 9 * $k * $c)), @(($x + 9 * $k * $s), ($y - 9 * $k * $c)), @(($x + 52 * $k * $c), ($y + 52 * $k * $s)))))
    if ($k -gt 1.2) { foreach ($t in .3, .55) { $g.DrawLine((Pn (Col 150 170 80 20) 2.5), [float]($x + 52 * $k * $c * $t - 6 * $k * $s * (1 - $t)), [float]($y + 52 * $k * $s * $t + 6 * $k * $c * (1 - $t)), [float]($x + 52 * $k * $c * $t), [float]($y + 52 * $k * $s * $t)) } }
    $g.DrawLine((Pn (Col 255 70 140 40) (5 * $k)), [float]$x, [float]$y, [float]($x - 16 * $k * $c - 6 * $k * $s), [float]($y - 16 * $k * $s + 6 * $k * $c))
    $g.DrawLine((Pn (Col 255 70 140 40) (5 * $k)), [float]$x, [float]$y, [float]($x - 18 * $k * $c + 6 * $k * $s), [float]($y - 18 * $k * $s - 8 * $k * $c))
}
function Draw-Turnip($g, $x, $y, $r) {
    $g.DrawLine((Pn (Col 255 70 140 40) 6), [float]$x, [float]($y - $r), [float]($x - 10), [float]($y - $r - 22)); $g.DrawLine((Pn (Col 255 70 140 40) 6), [float]$x, [float]($y - $r), [float]($x + 12), [float]($y - $r - 20))
    Ell $g (Grad ($x - $r) ($y - $r) (2 * $r) (2 * $r) (Col 255 170 80 175) (Col 255 240 230 220) 90) ($x - $r) ($y - $r) (2 * $r) (2 * $r)
}
function Draw-Mushroom($g, $x, $y, $r, $cap, $spots) {
    $g.FillRectangle((Br (Col 255 230 222 200)), [float]($x - $r * .3), [float]$y, [float]($r * .6), [float]($r * .9))
    $g.FillPie((Br (Col 255 $cap[0] $cap[1] $cap[2])), [float]($x - $r), [float]($y - $r * .8), [float](2 * $r), [float](1.6 * $r), 180, 180)
    if ($spots) { foreach ($i in 0..2) { Ell $g (Br (Col 220 255 255 240)) ($x - $r * .6 + $i * $r * .45) ($y - $r * .55 + ($i % 2) * 5) ($r * .28) ($r * .22) } }
}
function Draw-Berry($g, $x, $y, $r, $rgb) { Ell $g (Br (Col 255 $rgb[0] $rgb[1] $rgb[2])) ($x - $r) ($y - $r) (2 * $r) (2 * $r); Shine $g ($x - $r * .5) ($y - $r * .6) ($r * .6) ($r * .45) }
function Draw-Berries($g, $x, $y, $rgb, $n, $r) { for ($i = 0; $i -lt $n; $i++) { $a = $i * 2.4; $d = 6 + $i * 2.2; Draw-Berry $g ($x + $d * [Math]::Cos($a)) ($y + $d * [Math]::Sin($a) * .6) $r $rgb } }
function Draw-Raspberry($g, $x, $y) { foreach ($p in @(@(0, 0), @(8, 2), @(-8, 2), @(0, 8), @(5, -6), @(-5, -6), @(8, 10), @(-8, 10))) { Draw-Berry $g ($x + $p[0]) ($y + $p[1]) 6 @(205, 40, 70) } }
function Draw-Dandelion($g, $x, $y) { foreach ($i in 0..11) { $a = $i * [Math]::PI / 6; $g.DrawLine((Pn (Col 255 245 205 40) 5), [float]$x, [float]$y, [float]($x + 16 * [Math]::Cos($a)), [float]($y + 10 * [Math]::Sin($a))) }; Ell $g (Br (Col 255 230 160 20)) ($x - 7) ($y - 5) 14 10 }
function Draw-HoneyPot($g, $x, $y) {
    Ell $g (Grad ($x - 28) ($y - 30) 56 58 (Col 255 200 140 70) (Col 255 120 74 30) 0) ($x - 28) ($y - 30) 56 58
    Ell $g (Br (Col 255 245 180 40)) ($x - 20) ($y - 34) 40 14
    $g.FillPie((Br (Col 255 245 180 40)), [float]($x + 6), [float]($y - 30), 14, 26, 0, 180)
}
function Draw-Seed($g, $x, $y) { Ell $g (Br (Col 120 140 255 140)) ($x - 14) ($y - 14) 28 28; Ell $g (Br (Col 255 110 190 70)) ($x - 9) ($y - 9) 18 18; Shine $g ($x - 5) ($y - 6) 7 5 }
function Draw-Sausage($g, $x, $y, $len, $ang) {
    $p = Pn (Col 255 150 60 60) 20; $g.DrawLine($p, [float]$x, [float]$y, [float]($x + $len * [Math]::Cos($ang)), [float]($y + $len * [Math]::Sin($ang)))
    $g.DrawLine((Pn (Col 110 255 200 200) 5), [float]($x + 4), [float]($y - 5), [float]($x + $len * .7 * [Math]::Cos($ang)), [float]($y - 5 + $len * .7 * [Math]::Sin($ang)))
}
function Draw-Bloodbag($g, $x, $y) {
    Ell $g (Grad ($x - 24) ($y - 22) 48 46 (Col 255 200 30 40) (Col 255 110 10 20) 60) ($x - 24) ($y - 22) 48 46
    $g.FillPolygon((Br (Col 255 140 20 30)), (Pts @(@(($x - 8), ($y - 20)), @(($x + 8), ($y - 20)), @($x, ($y - 34))))); Shine $g ($x - 14) ($y - 14) 14 9
}
function Draw-Thistle($g, $x, $y) {
    $g.DrawLine((Pn (Col 255 80 130 60) 5), [float]$x, [float]($y + 22), [float]$x, [float]$y)
    Ell $g (Br (Col 255 90 140 70)) ($x - 11) ($y - 6) 22 16
    foreach ($i in -3..3) { $g.DrawLine((Pn (Col 255 170 90 220) 4), [float]($x + $i * 3), [float]($y - 2), [float]($x + $i * 5), [float]($y - 20)) }
}
function Draw-Onion($g, $x, $y, $r) {
    Ell $g (Grad ($x - $r) ($y - $r) (2 * $r) (2 * $r) (Col 255 225 180 120) (Col 255 160 100 60) 70) ($x - $r) ($y - $r) (2 * $r) (2 * $r)
    $g.FillPolygon((Br (Col 255 200 150 90)), (Pts @(@(($x - 5), ($y - $r + 2)), @(($x + 5), ($y - $r + 2)), @($x, ($y - $r - 12)))))
    $g.DrawArc((Pn (Col 120 120 70 30) 2.5), [float]($x - $r * .6), [float]($y - $r), [float]($r * 1.2), [float](2 * $r), 100, 160); Shine $g ($x - $r * .5) ($y - $r * .6) ($r * .5) ($r * .4)
}
function Draw-Gland($g, $x, $y) { Ell $g (Grad ($x - 18) ($y - 14) 36 28 (Col 255 150 210 255) (Col 255 60 120 200) 60) ($x - 18) ($y - 14) 36 28; Shine $g ($x - 10) ($y - 9) 10 6 }
function Draw-Tear($g, $x, $y, $s) {
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $path.AddBezier((Pt $x ($y - 18 * $s)), (Pt ($x + 14 * $s) ($y - 2 * $s)), (Pt ($x + 12 * $s) ($y + 14 * $s)), (Pt $x ($y + 14 * $s)))
    $path.AddBezier((Pt $x ($y + 14 * $s)), (Pt ($x - 12 * $s) ($y + 14 * $s)), (Pt ($x - 14 * $s) ($y - 2 * $s)), (Pt $x ($y - 18 * $s)))
    $g.FillPath((Br (Col 240 150 130 240)), $path); Shine $g ($x - 5 * $s) ($y - 2 * $s) (5 * $s) (7 * $s)
}
function Draw-Bread($g, $x, $y) {
    Ell $g (Grad ($x - 46) ($y - 26) 92 52 (Col 255 226 168 92) (Col 255 160 100 44) 80) ($x - 46) ($y - 26) 92 52
    foreach ($i in -1..1) { $g.DrawLine((Pn (Col 200 120 70 30) 4), [float]($x + $i * 22 - 8), [float]($y - 14), [float]($x + $i * 22 + 8), [float]($y + 6)) }
    Shine $g ($x - 30) ($y - 20) 26 8
}
function Draw-Leg($g, $x, $y, $ang, $meat) {
    $c = [Math]::Cos($ang); $s = [Math]::Sin($ang); $bone = Col 255 236 226 200
    $g.DrawLine((Pn $bone 10), [float]$x, [float]$y, [float]($x + 50 * $c), [float]($y + 50 * $s))
    Ell $g (Br $bone) ($x + 50 * $c - 9) ($y + 50 * $s - 12) 16 16; Ell $g (Br $bone) ($x + 50 * $c - 3) ($y + 50 * $s - 4) 16 16
    Ell $g (Grad ($x - 30) ($y - 22) 56 44 (Col 255 $meat[0] $meat[1] $meat[2]) (Col 255 ($meat[0] * .55) ($meat[1] * .5) ($meat[2] * .45)) 70) ($x - 30) ($y - 22) 56 44
    Shine $g ($x - 18) ($y - 16) 18 8
}
function Draw-Jelly($g, $x, $y) {
    $g.FillPie((Br (Col 235 250 215 80)), [float]($x - 26), [float]($y - 22), 52, 44, 180, 180)
    Ell $g (Br (Col 235 250 215 80)) ($x - 26) ($y - 6) 52 14; Shine $g ($x - 14) ($y - 16) 14 8
}
function Draw-BugMeat($g, $x, $y) {
    Ell $g (Grad ($x - 30) ($y - 18) 60 36 (Col 255 110 90 150) (Col 255 60 40 90) 70) ($x - 30) ($y - 18) 60 36
    foreach ($i in -1..1) { $g.DrawArc((Pn (Col 200 40 25 60) 3), [float]($x + $i * 16 - 8), [float]($y - 18), 16, 36, 280, 160) }
    Shine $g ($x - 20) ($y - 12) 14 6
}
function Draw-SmokePuff($g, $x, $y, $r) { Ell $g (Br (Col 255 200 190 170)) ($x - $r) ($y - $r) (2 * $r) (2 * $r); foreach ($i in 0..2) { Ell $g (Br (Col 255 230 120 50)) ($x - $r * .6 + $i * $r * .45) ($y - $r * .3) ($r * .3) ($r * .3) } }
function Draw-Kale($g, $x, $y, $ang) {
    $path = New-Object Drawing.Drawing2D.GraphicsPath; $c = [Math]::Cos($ang); $s = [Math]::Sin($ang)
    $path.AddBezier((Pt $x $y), (Pt ($x + 30 * $c - 18 * $s) ($y + 30 * $s + 18 * $c)), (Pt ($x + 56 * $c - 10 * $s) ($y + 56 * $s + 10 * $c)), (Pt ($x + 64 * $c) ($y + 64 * $s)))
    $path.AddBezier((Pt ($x + 64 * $c) ($y + 64 * $s)), (Pt ($x + 56 * $c + 10 * $s) ($y + 56 * $s - 10 * $c)), (Pt ($x + 30 * $c + 18 * $s) ($y + 30 * $s - 18 * $c)), (Pt $x $y))
    $g.FillPath((Br (Col 255 60 120 70)), $path); $g.DrawLine((Pn (Col 200 150 200 140) 2.5), [float]$x, [float]$y, [float]($x + 60 * $c), [float]($y + 60 * $s))
}
function Draw-Porridge($g, $x, $y) {
    Draw-Bowl $g $x $y 0.42 @(225, 210, 175)
    foreach ($i in 0..4) { Ell $g (Br (Col 255 245 235 210)) ($x - 30 + $i * 13) ($y - 6 + ($i % 2) * 3) 8 5 }
    Draw-Berry $g ($x - 6) ($y - 4) 5 @(210, 30, 40); Draw-Berry $g ($x + 8) ($y - 2) 5 @(210, 30, 40)
}

# ---------------- Wahrzeichen der Bosse (hinter der Platte, Bereich y 30-250)
function Draw-Antlers($g, $rgb) {
    $bone = Col 255 232 220 190; $pen = Pn $bone 15; $thin = Pn $bone 10
    foreach ($m in -1, 1) {
        $x0 = 256 + $m * 40
        $g.DrawCurve($pen, (Pts @(@($x0, 250), @(($x0 + $m * 30), 190), @(($x0 + $m * 66), 146), @(($x0 + $m * 110), 118), @(($x0 + $m * 136), 74))))
        $g.DrawCurve($thin, (Pts @(@(($x0 + $m * 50), 164), @(($x0 + $m * 40), 120), @(($x0 + $m * 48), 86))))
        $g.DrawCurve($thin, (Pts @(@(($x0 + $m * 92), 128), @(($x0 + $m * 94), 92), @(($x0 + $m * 110), 62))))
        $g.DrawCurve($thin, (Pts @(@(($x0 + $m * 118), 106), @(($x0 + $m * 156), 104), @(($x0 + $m * 178), 92))))
    }
    foreach ($i in 0..6) { $x = 130 + $rnd.Next(252); $y = 50 + $rnd.Next(150); $p = Pn (Col 220 $rgb[0] $rgb[1] $rgb[2]) 3; $g.DrawLine($p, [float]$x, [float]$y, [float]($x + 10), [float]($y + 16)); $g.DrawLine($p, [float]($x + 10), [float]($y + 16), [float]($x + 2), [float]($y + 26)) }
}
function Draw-Roots($g) {
    $wood = Col 255 92 62 36
    foreach ($m in -1, 1) {
        $g.DrawCurve((Pn $wood 16), (Pts @(@((256 + $m * 20), 262), @((256 + $m * 66), 206), @((256 + $m * 58), 150), @((256 + $m * 112), 96), @((256 + $m * 156), 86))))
        $g.DrawCurve((Pn $wood 9), (Pts @(@((256 + $m * 62), 176), @((256 + $m * 122), 166), @((256 + $m * 166), 140))))
        $g.DrawCurve((Pn $wood 7), (Pts @(@((256 + $m * 86), 120), @((256 + $m * 80), 78), @((256 + $m * 98), 50))))
        foreach ($p in @(@(162, 86), @(170, 138), @(100, 50), @(60, 160))) { Ell $g (Br (Col 255 96 168 70)) (256 + $m * $p[0] - 12) ($p[1] - 7) 24 14 }
    }
}
function Draw-Bones($g) {
    $bone = Col 255 226 218 190
    foreach ($m in -1, 1) {
        $x1 = 256 - $m * 110; $x2 = 256 + $m * 100
        $g.DrawLine((Pn $bone 20), [float]$x1, 80, [float]$x2, 232)
        foreach ($e in @(@($x1, 80), @($x2, 232))) { Ell $g (Br $bone) ($e[0] - 21) ($e[1] - 17) 22 22; Ell $g (Br $bone) ($e[0] - 2) ($e[1] - 6) 22 22 }
    }
    foreach ($i in 0..5) { Ell $g (Br (Col 170 150 210 60)) (160 + $rnd.Next(190)) (70 + $rnd.Next(130)) 16 16 }
}
function Draw-Crystals($g, $base, $hi) {
    foreach ($c in @(@(256, 70, 42, 176), @(198, 116, 30, 130), @(314, 110, 32, 140), @(150, 164, 22, 92), @(362, 162, 24, 96))) {
        $x = $c[0]; $y = $c[1]; $w = $c[2]; $h = $c[3]
        $poly = Pts @(@($x, $y), @(($x + $w / 2), ($y + $h * .25)), @(($x + $w / 2), ($y + $h * .8)), @($x, ($y + $h)), @(($x - $w / 2), ($y + $h * .8)), @(($x - $w / 2), ($y + $h * .25)))
        $g.FillPolygon((Br (Col 235 $base[0] $base[1] $base[2])), $poly)
        $g.FillPolygon((Br (Col 150 $hi[0] $hi[1] $hi[2])), (Pts @(@($x, $y), @(($x + $w / 2), ($y + $h * .25)), @($x, ($y + $h * .4)))))
        $g.DrawPolygon((Pn (Col 255 ($base[0] * .6) ($base[1] * .6) ($base[2] * .7)) 2.5), $poly)
    }
}
function Draw-Flames($g) {
    foreach ($f in @(@(256, 50, 80), @(190, 106, 58), @(322, 102, 60), @(138, 164, 42), @(374, 160, 44))) {
        $x = $f[0]; $y = $f[1]; $w = $f[2]
        $o = New-Object Drawing.Drawing2D.GraphicsPath
        $o.AddBezier((Pt $x $y), (Pt ($x + $w * .7) ($y + $w * .9)), (Pt ($x + $w * .6) ($y + $w * 2.1)), (Pt $x ($y + $w * 2.2)))
        $o.AddBezier((Pt $x ($y + $w * 2.2)), (Pt ($x - $w * .6) ($y + $w * 2.1)), (Pt ($x - $w * .7) ($y + $w * .9)), (Pt $x $y))
        $g.FillPath((Br (Col 235 240 90 30)), $o)
        $i2 = New-Object Drawing.Drawing2D.GraphicsPath; $y2 = $y + $w * .8
        $i2.AddBezier((Pt $x $y2), (Pt ($x + $w * .4) ($y2 + $w * .6)), (Pt ($x + $w * .35) ($y2 + $w * 1.3)), (Pt $x ($y2 + $w * 1.4)))
        $i2.AddBezier((Pt $x ($y2 + $w * 1.4)), (Pt ($x - $w * .35) ($y2 + $w * 1.3)), (Pt ($x - $w * .4) ($y2 + $w * .6)), (Pt $x $y2))
        $g.FillPath((Br (Col 240 255 210 80)), $i2)
    }
}
function Draw-Carapace($g) {
    $g.FillPie((Grad 126 56 260 220 (Col 255 70 150 150) (Col 255 40 40 90) 60), 126, 56, 260, 400, 180, 180)
    foreach ($i in 1..4) { $g.DrawArc((Pn (Col 200 20 30 60) 5), [float](126 + $i * 20), [float](56 + $i * 32), [float](260 - $i * 40), [float](400 - $i * 64), 180, 180) }
    $g.DrawLine((Pn (Col 200 20 30 60) 5), 256, 56, 256, 256); $g.DrawArc((Pn (Col 150 200 255 250) 6), 160, 76, 130, 190, 200, 50)
}
function Draw-Bell($g) {
    $path = New-Object Drawing.Drawing2D.GraphicsPath
    $path.AddBezier((Pt 256 60), (Pt 324 60), (Pt 316 154), (Pt 352 232)); $path.AddLine((Pt 352 232), (Pt 160 232))
    $path.AddBezier((Pt 160 232), (Pt 196 154), (Pt 188 60), (Pt 256 60)); $path.CloseFigure()
    $g.FillPath((Grad 158 60 196 174 (Col 255 214 150 70) (Col 255 120 70 30) 0), $path); $g.DrawPath((Pn (Col 255 90 50 20) 4), $path)
    Ell $g (Br (Col 255 110 64 26)) 238 42 36 26
    $g.DrawLines((Pn (Col 255 255 170 40) 5), (Pts @(@(250, 80), @(266, 116), @(246, 154), @(268, 198), @(258, 232))))
}
function Draw-Ice($g) {
    $edge = Pn (Col 255 120 190 230) 3
    foreach ($c in @(@(256, 50, 38, 190), @(194, 98, 28, 142), @(318, 94, 28, 148), @(146, 156, 20, 94), @(366, 152, 20, 98))) {
        $poly = Pts @(@($c[0], $c[1]), @(($c[0] + $c[2] / 2), ($c[1] + $c[3])), @(($c[0] - $c[2] / 2), ($c[1] + $c[3])))
        $g.FillPolygon((Br (Col 230 200 240 255)), $poly); $g.DrawPolygon($edge, $poly)
    }
    $p = Pn (Col 255 240 250 255) 5; foreach ($a in 0..5) { $an = $a * [Math]::PI / 3; $g.DrawLine($p, 256, 150, [float](256 + 42 * [Math]::Cos($an)), [float](150 + 42 * [Math]::Sin($an))) }
}

# ---------------- die 8 Item-Icons (Zutaten wie im Rezept, KONZEPT_SIEGESMAHL.md)
$bosses = @(
    @{ Id = 'Eikthyr';  Glow = @(110, 190, 255) }, @{ Id = 'Elder';  Glow = @(110, 200, 80) },
    @{ Id = 'Bonemass'; Glow = @(170, 220, 60) },  @{ Id = 'Moder';  Glow = @(170, 140, 255) },
    @{ Id = 'Yagluth';  Glow = @(255, 110, 40) },  @{ Id = 'Queen';  Glow = @(220, 90, 230) },
    @{ Id = 'Fader';    Glow = @(255, 160, 40) },  @{ Id = 'Fimbul'; Glow = @(160, 225, 255) }
)
foreach ($b in $bosses) {
    $bmp = New-Object Drawing.Bitmap 512, 512, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode = 'AntiAlias'; $g.Clear([Drawing.Color]::Transparent)
    # kein Schein mehr (Valheim-Stil); Draw-Halo bleibt fuer Varianten erhalten
    switch ($b.Id) {
        'Eikthyr' {   # Hirschfleisch, Honig, Himbeeren
            Draw-Antlers $g $b.Glow; Draw-Platter $g
            Draw-Roast $g 244 330 1.05 @(190, 108, 56) $null
            Draw-HoneyPot $g 108 342; Draw-Raspberry $g 150 384; Draw-Raspberry $g 372 384; Draw-Raspberry $g 404 340; Draw-Raspberry $g 326 400 }
        'Elder' {     # Karotte, Gelber Pilz, Blaubeeren
            Draw-Roots $g; Draw-Platter $g
            Draw-Carrot $g 192 300 0.55 1.9; Draw-Carrot $g 214 292 0.25 2.0; Draw-Carrot $g 232 300 0.0 1.9; Draw-Carrot $g 206 318 0.4 1.8
            Draw-Mushroom $g 146 360 26 @(232, 180, 50) $false; Draw-Mushroom $g 366 346 28 @(232, 180, 50) $false; Draw-Mushroom $g 320 384 20 @(232, 180, 50) $false
            Draw-Berries $g 210 384 @(60, 70, 165) 7 9; Draw-Berries $g 392 388 @(60, 70, 165) 5 8; Draw-Berries $g 258 300 @(60, 70, 165) 4 7 }
        'Bonemass' {  # Blutbeutel, Eingeweide, Ruebe (als Eintopf)
            Draw-Bones $g; Draw-Platter $g; Draw-Bowl $g 250 316 1.0 @(120, 70, 50)
            Draw-Sausage $g 196 304 50 -0.3; Draw-Turnip $g 280 302 18; Draw-Sausage $g 250 322 40 0.2
            Draw-Bloodbag $g 112 360; Draw-Bloodbag $g 392 352; Draw-Turnip $g 352 392 20; Draw-Turnip $g 160 400 16 }
        'Moder' {     # Wolfsfleisch, Zwiebel, Frostdruese
            Draw-Crystals $g @(150, 120, 240) @(230, 220, 255); Draw-Platter $g
            Draw-Roast $g 246 332 1.0 @(150, 110, 100) $null; Ell $g (Br (Col 190 240 245 255)) 168 272 150 26
            Draw-Onion $g 116 352 22; Draw-Onion $g 150 390 18; Draw-Gland $g 376 352; Draw-Gland $g 344 392; Draw-Onion $g 398 392 16; Draw-Gland $g 196 402 }
        'Yagluth' {   # Lox-Fleisch, Gerstenmehl (Brot), Moltebeeren
            Draw-Flames $g; Draw-Platter $g
            Draw-Roast $g 262 330 1.0 @(130, 64, 36) @(255, 150, 40)
            Draw-Bread $g 128 350; Draw-Bread $g 176 392; Draw-Berries $g 376 386 @(240, 150, 50) 7 10 }
        'Queen' {     # Hasenfleisch, Magierkappe, Gelee royale
            Draw-Carapace $g; Draw-Platter $g
            Draw-Leg $g 236 322 -0.5 @(196, 120, 80); Draw-Leg $g 300 352 -2.6 @(196, 120, 80)
            Draw-Mushroom $g 140 350 28 @(150, 70, 210) $true; Draw-Mushroom $g 380 344 26 @(150, 70, 210) $true; Draw-Mushroom $g 172 396 20 @(150, 70, 210) $true
            Draw-Jelly $g 250 402; Draw-Jelly $g 354 398 }
        'Fader' {     # Asksvin-Fleisch, Geierfleisch, Rebenbeeren
            Draw-Bell $g; Draw-Platter $g
            Draw-Roast $g 236 332 1.0 @(150, 70, 40) @(255, 180, 60); Draw-Leg $g 356 340 -0.9 @(170, 90, 60)
            Draw-Berries $g 128 362 @(160, 30, 90) 7 9; Draw-Berries $g 386 396 @(160, 30, 90) 5 8; Draw-Leg $g 170 396 -2.4 @(170, 90, 60) }
        'Fimbul' {    # Elchfleisch, Preiselbeeren, Hafer
            Draw-Ice $g; Draw-Platter $g
            
            Draw-Roast $g 236 334 1.0 @(160, 84, 56) $null; Ell $g (Br (Col 170 245 250 255)) 160 276 130 20
            Draw-Porridge $g 376 372; Draw-Berries $g 124 360 @(210, 30, 40) 7 9; Draw-Berries $g 176 398 @(210, 30, 40) 5 8 }
    }
    # kein Siegesband mehr (Valheim-Stil)
    $g.Dispose()
    $path = Join-Path $out ("Victory_" + $b.Id + ".png"); $bmp.Save($path, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    "erstellt: " + (Split-Path $path -Leaf)
}

# Valheim-Stil: Licht, Maltextur, Umriss, Schatten
& (Join-Path $PSScriptRoot 'stylize_icons.ps1') (Join-Path $out 'Victory_*.png')
