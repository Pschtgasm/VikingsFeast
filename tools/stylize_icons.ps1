# Nachbearbeitung gezeichneter Icons im Valheim-Stil: Licht von links oben, leichte Maltextur und Weichzeichnung,
# dunkler Umriss und weicher Schlagschatten. Aufruf: powershell -File tools\stylize_icons.ps1 Icons\Victory_*.png Icons\Feed_*.png
param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Paths)
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class VfpStylize
{
    static float Hash(int x, int y) { unchecked { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xFFFF) / 32767.5f - 1f; } }

    public static void Run(string path)
    {
        Bitmap src;
        using (var tmp = new Bitmap(path)) src = new Bitmap(tmp);
        int w = src.Width, h = src.Height;
        var data = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var px = new byte[w * h * 4]; Marshal.Copy(data.Scan0, px, 0, px.Length); src.UnlockBits(data); src.Dispose();
        var outPx = new byte[px.Length];
        var a = new float[w * h];
        for (int i = 0; i < w * h; i++) a[i] = px[i * 4 + 3] / 255f;

        // 1) Farbe: leichte Weichzeichnung (Maltextur), Licht von links oben, feine Pinsel-Koernung
        var col = new float[w * h * 3];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x; if (a[i] <= 0f) continue;
                float r = 0, g = 0, b = 0, ws = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                        int j = yy * w + xx; float wt = a[j] * (dx == 0 && dy == 0 ? 2f : 1f);
                        r += px[j * 4 + 2] * wt; g += px[j * 4 + 1] * wt; b += px[j * 4] * wt; ws += wt;
                    }
                if (ws <= 0) continue;
                r /= ws; g /= ws; b /= ws;
                float light = 1.14f - 0.30f * ((x + y) / (float)(w + h));                          // links oben hell, rechts unten dunkel
                float grain = Hash(x / 2, y / 2) * 9f + Hash(x / 5 + 911, y / 5 + 37) * 7f;        // Pinsel-Koernung
                col[i * 3] = r * light + grain; col[i * 3 + 1] = g * light + grain; col[i * 3 + 2] = b * light + grain * 0.8f;
            }

        // 2) Umriss: Alpha um 3 px erweitern
        var dil = new float[w * h];
        const int R = 3;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float m = 0;
                for (int dy = -R; dy <= R && m < 1f; dy++)
                    for (int dx = -R; dx <= R; dx++)
                    {
                        if (dx * dx + dy * dy > R * R) continue;
                        int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                        float v = a[yy * w + xx]; if (v > m) m = v;
                    }
                dil[y * w + x] = m;
            }

        // 3) Schlagschatten: Alpha versetzt (6, 9) und weichgezeichnet
        var sh = new float[w * h]; var tmpA = new float[w * h];
        const int S = 7, OX = 6, OY = 9;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float s = 0; int n = 0;
                for (int dx = -S; dx <= S; dx++) { int xx = x + dx - OX; if (xx >= 0 && xx < w && y - OY >= 0 && y - OY < h) s += dil[(y - OY) * w + xx]; n++; }
                tmpA[y * w + x] = s / n;
            }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float s = 0; int n = 0;
                for (int dy = -S; dy <= S; dy++) { int yy = y + dy; if (yy >= 0 && yy < h) s += tmpA[yy * w + x]; n++; }
                sh[y * w + x] = s / n * 0.5f;
            }

        // Zusammensetzen: Schatten, dunkler Umriss, Bild
        for (int i = 0; i < w * h; i++)
        {
            float sa = sh[i];
            float oa = dil[i];
            float ia = a[i];
            // Schatten (schwarz) unter dem Umriss
            float rr = 0, gg = 0, bb = 0, aa = sa;
            // Umriss dunkelbraun ueber den Schatten
            float or_ = 38, og = 24, ob = 14;
            rr = rr * (1 - oa) + or_ * oa; gg = gg * (1 - oa) + og * oa; bb = bb * (1 - oa) + ob * oa; aa = aa * (1 - oa) + oa;
            // Bild ueber den Umriss
            float cr = Math.Max(0, Math.Min(255, col[i * 3])), cg = Math.Max(0, Math.Min(255, col[i * 3 + 1])), cb = Math.Max(0, Math.Min(255, col[i * 3 + 2]));
            rr = rr * (1 - ia) + cr * ia; gg = gg * (1 - ia) + cg * ia; bb = bb * (1 - ia) + cb * ia; aa = aa * (1 - ia) + ia;
            if (aa > 0) { rr = Math.Min(255, rr / Math.Max(aa, 1e-4f) * (aa)); }
            outPx[i * 4 + 2] = (byte)Math.Max(0, Math.Min(255, rr / Math.Max(aa, 1e-4f)));
            outPx[i * 4 + 1] = (byte)Math.Max(0, Math.Min(255, gg / Math.Max(aa, 1e-4f)));
            outPx[i * 4] = (byte)Math.Max(0, Math.Min(255, bb / Math.Max(aa, 1e-4f)));
            outPx[i * 4 + 3] = (byte)Math.Max(0, Math.Min(255, aa * 255f));
        }
        var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        var dd = dst.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(outPx, 0, dd.Scan0, outPx.Length); dst.UnlockBits(dd);
        dst.Save(path, ImageFormat.Png); dst.Dispose();
    }
}
'@
foreach ($p in $Paths) { foreach ($f in (Get-ChildItem $p)) { [VfpStylize]::Run($f.FullName); "stilisiert: " + $f.Name } }
