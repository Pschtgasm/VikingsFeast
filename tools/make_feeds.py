"""3D-Modelle fuer die 6 Tierfutter (ab 1.7.0): offener Futtersack aus Sackleinen mit Kordel, oben das Futter
in der Biomfarbe (leicht leuchtend) und eine leuchtende Rune vorne. Ursprung Mitte unten, Y oben.
Die Groesse passt ApplyDishModel im Spiel ohnehin auf 48 cm Breite an.
Aufruf: python make_feeds.py   -> Models/VFP_Feed_<Id>.obj/.mtl + preview_feeds.png
Farben wie bei den Icons (tools/make_feed_icons.ps1): Wiese hell gelbgruen, Sumpf dunkles Moorgruen."""
import math, os, random
import numpy as np
from PIL import Image
from meshlib import *

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, '..', 'Models')
R = random.Random(17)

BASE_MATS = {
    'sack': ((0.62, 0.48, 0.30), (0, 0, 0)), 'sack_dark': ((0.40, 0.29, 0.17), (0, 0, 0)),
    'sack_inner': ((0.24, 0.17, 0.10), (0, 0, 0)), 'cord': ((0.72, 0.56, 0.26), (0, 0, 0)),
    'leaf': ((0.26, 0.48, 0.16), (0, 0, 0)), 'dandelion': ((0.92, 0.78, 0.18), (0.10, 0.08, 0)),
    'rasp': ((0.70, 0.14, 0.20), (0, 0, 0)), 'blue': ((0.20, 0.25, 0.55), (0, 0, 0)),
    'carrot': ((0.85, 0.42, 0.10), (0, 0, 0)), 'mush_yellow': ((0.90, 0.70, 0.22), (0, 0, 0)),
    'turnip': ((0.45, 0.22, 0.46), (0, 0, 0)), 'thistle': ((0.42, 0.28, 0.60), (0.10, 0.05, 0.18)),
    'meat': ((0.55, 0.22, 0.18), (0, 0, 0)), 'claw': ((0.88, 0.86, 0.80), (0, 0, 0)),
    'barley': ((0.86, 0.70, 0.36), (0, 0, 0)), 'cloud': ((0.94, 0.62, 0.28), (0, 0, 0)),
    'vineberry': ((0.62, 0.12, 0.30), (0.08, 0.0, 0.04)), 'ember': ((1.00, 0.45, 0.10), (0.85, 0.30, 0.04)),
    'iron': ((0.30, 0.30, 0.32), (0, 0, 0)),
    'haft': ((0.17, 0.13, 0.11), (0, 0, 0)), 'edge': ((0.88, 0.96, 1.00), (0.45, 0.55, 0.65)),
}

# Id: (Futterfarbe Kd, Leuchten Ke)
FEEDS = {
    'TamingHerb':   ((0.62, 0.88, 0.25), (0.22, 0.42, 0.05)),   # Wiese: hell gelbgruen
    'FreyaLure':    ((0.88, 0.40, 0.62), (0.40, 0.12, 0.25)),   # Schwarzwald: rosa
    'NornBlessing': ((0.10, 0.38, 0.14), (0.03, 0.22, 0.06)),   # Sumpf: dunkles Moorgruen
    'FenrirBite':   ((0.50, 0.24, 0.20), (0.04, 0.07, 0.11)),   # Gebirge: Fleisch (eisblau leuchtet nur die Rune)
    'ArmorGrain':   ((0.90, 0.72, 0.30), (0.40, 0.28, 0.05)),   # Ebene: golden
    'EmberShell':   ((0.75, 0.28, 0.10), (0.60, 0.20, 0.03)),   # Aschland: orange
}
RUNE = {   # Leuchtfarbe der Rune (Kd, Ke) = Farbe des Leuchtens im Spiel
    'TamingHerb':   ((0.72, 1.00, 0.30), (0.45, 0.75, 0.12)),
    'FreyaLure':    ((1.00, 0.48, 0.72), (0.70, 0.22, 0.42)),
    'NornBlessing': ((0.08, 0.45, 0.14), (0.04, 0.35, 0.09)),
    'FenrirBite':   ((0.58, 0.82, 1.00), (0.32, 0.55, 0.80)),
    'ArmorGrain':   ((1.00, 0.82, 0.32), (0.70, 0.50, 0.10)),
    'EmberShell':   ((1.00, 0.40, 0.12), (0.85, 0.28, 0.05)),
}

SACK_R, SACK_H = .13, .26          # Radius und Hoehe bis zum Rand
RIM_Y = SACK_H
MOUND_Y, MOUND_H, MOUND_R = RIM_Y - .012, .075, SACK_R * .84   # Futterhaufen ragt deutlich ueber den Rand

def new(fid):
    m = Model('VFP_Feed_' + fid)
    for k, v in BASE_MATS.items(): m.mat(k, *v)
    m.mat('feed', *FEEDS[fid]); m.mat('rune', *RUNE[fid])
    return m

def sack(m):
    """Bauchiger Sack, oben eingeschnuert, Rand nach aussen umgeschlagen."""
    r = SACK_R
    prof = [(0, 0), (r * .70, 0), (r * .92, .02), (r, .07), (r * 1.02, .13), (r * .96, .18), (r * .80, .215),
            (r * .72, .23), (r * .76, .245), (r * .86, RIM_Y), (r * .92, RIM_Y + .008)]
    m.add('Sack', 'sack', jitter(lathe(prof, 20), .025, 3))
    # Innenseite (dunkel), damit man nicht durch den offenen Rand schaut
    m.add('SackInner', 'sack_inner', lathe([(r * .88, RIM_Y + .004), (r * .80, RIM_Y - .03), (0, RIM_Y - .03)], 20))
    # Naehte / Falten als dunkle Streifen
    for i in range(6):
        a = 2 * math.pi * i / 6 + .3
        pts = [((r * k) * math.cos(a), y, (r * k) * math.sin(a)) for (k, y) in ((.93, .03), (1.03, .10), (1.03, .15), (.82, .21))]
        m.add('Folds', 'sack_dark', tube(pts, .0035, 4, smooth=4))

def cord(m):
    m.add('Cord', 'cord', torus(SACK_R * .74, .008, 22, 6), pos=(0, .228, 0))
    knot = (SACK_R * .74, .228, 0)
    m.add('Cord', 'cord', sphere(.014, 8, 5), pos=knot)
    for dz, ln in ((-.01, .07), (.012, .055)):
        pts = [knot, (knot[0] + .02, knot[1] - ln * .5, dz), (knot[0] + .025, knot[1] - ln, dz * 1.5)]
        m.add('Cord', 'cord', tube(pts, .005, 5, smooth=4))

# ---------------------------------------------------------------- Symbole vorne auf dem Sack
# 2D-Koordinaten wie im Icon/Entwurf: Feld 60 x 56, x nach rechts, y nach unten; werden auf die gewoelbte Sackfront gelegt.
SACK_PROF = [(0, .70), (.02, .92), (.07, 1.0), (.13, 1.02), (.18, .96), (.215, .80)]
SYM_K = .003

def sack_r(y):
    for (y0, k0), (y1, k1) in zip(SACK_PROF, SACK_PROF[1:]):
        if y0 <= y <= y1: return SACK_R * (k0 + (k1 - k0) * (y - y0) / (y1 - y0))
    return SACK_R * SACK_PROF[-1][1]

def sp(px, py, lift=.005):
    x = (px - 30) * SYM_K; y = .186 - py * SYM_K
    r = sack_r(y) * 1.025
    return (x, y, math.sqrt(max(r * r - x * x, 1e-6)) + lift)

def stroke(m, pts2d, mat='rune', r=.0045, closed=False, smooth=3, lift=.006):
    pts = list(pts2d) + ([pts2d[0]] if closed else [])
    m.add('Symbol', mat, tube([sp(x, y, lift) for x, y in pts], r, 6, smooth=smooth))

def _area2(a, b, c):
    return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])

def triangulate(pts):
    """Ear-Clipping fuer beliebige einfache Polygone (auch nicht konvex, z. B. Axtblatt mit Bart)."""
    n = len(pts); idx = list(range(n)); tris = []
    sign = 1 if sum(pts[i][0] * pts[(i + 1) % n][1] - pts[(i + 1) % n][0] * pts[i][1] for i in range(n)) > 0 else -1
    def inside(p, a, b, c):
        d1, d2, d3 = _area2(a, b, p) * sign, _area2(b, c, p) * sign, _area2(c, a, p) * sign
        return d1 >= 0 and d2 >= 0 and d3 >= 0
    while len(idx) > 3:
        for k in range(len(idx)):
            i0, i1, i2 = idx[k - 1], idx[k], idx[(k + 1) % len(idx)]
            a, b, c = pts[i0], pts[i1], pts[i2]
            if _area2(a, b, c) * sign <= 1e-9: continue
            if any(inside(pts[j], a, b, c) for j in idx if j not in (i0, i1, i2)): continue
            tris.append((i0, i1, i2)); idx.pop(k); break
        else:
            break   # entartet: Rest weglassen
    if len(idx) == 3: tris.append(tuple(idx))
    return tris

def blob(m, pts2d, mat='rune', lift=.007, center=None):
    """Gefuellte Flaeche (beliebiges einfaches Polygon) als flaches Prisma auf der Sackfront."""
    # im Bild zeigt y nach unten: Polygon so drehen, dass die Front im Modell (+Z) gegen den Uhrzeigersinn laeuft
    area = sum(pts2d[i][0] * pts2d[(i + 1) % len(pts2d)][1] - pts2d[(i + 1) % len(pts2d)][0] * pts2d[i][1] for i in range(len(pts2d)))
    pts = list(pts2d) if area < 0 else list(pts2d)[::-1]
    n = len(pts)
    V = [sp(x, y, lift) for x, y in pts] + [sp(x, y, 0) for x, y in pts]
    F = []
    for a, b, c in triangulate(pts):
        if _area2(pts[a], pts[b], pts[c]) > 0: b, c = c, b
        F += [(a, b, c), (n + a, n + c, n + b)]
    for i in range(n):
        j = (i + 1) % n
        F += [(i, n + i, n + j), (i, n + j, j)]
    m.add('Symbol', mat, (np.array(V, float), np.array(F, int)))

def leaf_pts(base, tip, width, n=10):
    bx, by = base; tx, ty = tip; dx, dy = tx - bx, ty - by; ln = math.hypot(dx, dy); nx, ny = -dy / ln, dx / ln
    side1 = [(bx + dx * t + nx * width * math.sin(math.pi * t), by + dy * t + ny * width * math.sin(math.pi * t)) for t in np.linspace(0, 1, n)]
    side2 = [(bx + dx * t - nx * width * math.sin(math.pi * t), by + dy * t - ny * width * math.sin(math.pi * t)) for t in np.linspace(1, 0, n)][1:-1]
    return side1 + side2

def sym_twig(m):
    stroke(m, [(30, 47), (30, 36), (30.5, 24), (30, 13)], r=.004)
    blob(m, leaf_pts((30, 42), (13, 22), 6.5))
    blob(m, leaf_pts((30, 33), (46, 15), 6))
    blob(m, leaf_pts((30, 22), (22, 7), 4))

def sym_heart(m):
    pts = []
    for t in np.linspace(0, 2 * math.pi, 40, endpoint=False):
        x = 16 * math.sin(t) ** 3; y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((30 + x * 1.05, 25 - y * 1.05))
    blob(m, pts, center=(30, 26))

def sym_star(m):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5; r = 18 if i % 2 == 0 else 7.5
        pts.append((30 + r * math.cos(a), 27 + r * math.sin(a)))
    blob(m, pts, center=(30, 27))

def sym_viking_shield(m):
    """Wikinger-Rundschild: bemalte Scheibe, Planken, Eisenrand, Schildbuckel."""
    circle = [(30 + 18 * math.cos(a), 28 + 18 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 32, endpoint=False)]
    blob(m, circle, lift=.006)
    for x in (21, 27, 33, 39):   # Plankenfugen
        h = math.sqrt(max(18 ** 2 - (x - 30) ** 2, 0)) - 1
        stroke(m, [(x, 28 - h), (x, 28 + h)], mat='sack_dark', r=.0018, smooth=0, lift=.0085)
    stroke(m, circle, mat='iron', r=.0035, closed=True, smooth=0, lift=.0075)
    bx, by, bz = sp(30, 28, .009)
    m.add('Symbol', 'iron', sphere(.014, 12, 6), pos=(bx, by, bz), scl=(1, 1, .55))

def sym_flame_shield(m):
    shield = [(16, 10), (44, 10), (44, 26), (42, 34), (37, 41), (30, 47), (23, 41), (18, 34), (16, 26)]
    stroke(m, shield, r=.004, closed=True, smooth=0)
    flame = [(30, 42), (24, 37), (23, 30), (26, 24), (28.5, 29), (29, 22), (33, 14), (34, 22), (37.5, 28), (37, 36)]
    blob(m, flame, mat='ember', center=(30, 33))

# Bartaxt (nach dem Vorbild einer Wikinger-Bartaxt): lokale Koordinaten (along = Richtung Stielende oben, across = zur Schneide)
AXE_HEAD = [(2.5, 0), (3.4, 4), (4.6, 9), (5.6, 13),                                  # gerade Oberkante bis zur oberen Spitze
            (3, 14.6), (-1, 15.3), (-5, 14.8), (-9, 13.6), (-12.5, 11.5),            # gewoelbte Schneide bis zur Bartspitze
            (-10.5, 9.2), (-7.6, 6.8), (-5.4, 4.4), (-3.8, 2.2), (-3.2, 0),         # nach innen geschwungene Bartunterkante
            (-3.2, -2.6), (2.5, -2.6)]                                               # Tuelle hinter dem Stiel
AXE_EDGE = [(5.2, 13.3), (3, 14.3), (-1, 15.0), (-5, 14.5), (-9, 13.3), (-12.1, 11.4)]
AXE_CURL = [(-2.0 + r * math.cos(a), 8.8 + r * math.sin(a)) for a, r in zip(np.linspace(0, 3.2 * math.pi, 22), np.linspace(2.6, .6, 22))]

def axe_geom(A, B, side):
    """Stiel von A (Knauf) nach B (Klinge), Klinge zur Seite side. Liefert Stiel-, Klingen-, Schneiden- und Gravurpunkte im Symbolfeld."""
    dx, dy = B[0] - A[0], B[1] - A[1]; L = math.hypot(dx, dy); ux, uy = dx / L, dy / L
    nx, ny = -uy, ux
    if nx * side[0] + ny * side[1] < 0: nx, ny = -nx, -ny
    S = AXE_SCALE
    loc = lambda al, ac: (B[0] + (ux * al + nx * ac) * S, B[1] + (uy * al + ny * ac) * S)
    bend = 2.4   # Stiel leicht geschwungen, weg von der Klinge (wie bei der Vorlage)
    haft = [A, (A[0] + dx * .25 - nx * bend * .7, A[1] + dy * .25 - ny * bend * .7), (A[0] + dx * .55 - nx * bend, A[1] + dy * .55 - ny * bend),
            (A[0] + dx * .82 - nx * bend * .4, A[1] + dy * .82 - ny * bend * .4), B, loc(4.2, 0)]
    return haft, [loc(*p) for p in AXE_HEAD], [loc(*p) for p in AXE_EDGE], [loc(*p) for p in AXE_CURL]

AXE_SCALE = 1.4
AXES = [((14, 55), (39, 17), (1, .2)), ((46, 55), (21, 17), (-1, .2))]

def sym_axes(m):
    geoms = [axe_geom(*a) for a in AXES]
    for k, (haft, _, _, _) in enumerate(geoms):
        stroke(m, haft, mat='haft', r=.0044, smooth=4, lift=.006 + k * .0016)
        kx, ky, kz = sp(haft[0][0], haft[0][1], .006 + k * .0016)
        m.add('Symbol', 'haft', sphere(.0068, 8, 5), pos=(kx, ky, kz))
    for head, edge, curl in ((g[1], g[2], g[3]) for g in geoms):
        blob(m, head, lift=.0092)
        stroke(m, edge, mat='edge', r=.0016, smooth=3, lift=.0102)
        stroke(m, curl, mat='sack_dark', r=.0010, smooth=2, lift=.0101)

SYMBOLS = {'TamingHerb': sym_twig, 'FreyaLure': sym_heart, 'NornBlessing': sym_star, 'FenrirBite': sym_axes,
           'ArmorGrain': sym_viking_shield, 'EmberShell': sym_flame_shield}

def rune(m):
    """Symbol vorne auf dem Sack. Ohne eigenes Symbol: alte Rune (Stab mit zwei Aesten)."""
    fid = m.name.replace('VFP_Feed_', '')
    if fid in SYMBOLS: SYMBOLS[fid](m); return
    z = SACK_R * 1.04
    def bar(x0, y0, x1, y1):
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2; ln = math.hypot(x1 - x0, y1 - y0)
        ang = math.degrees(math.atan2(y1 - y0, x1 - x0))
        m.add('Rune', 'rune', box((ln, .011, .006)), pos=(cx, cy, z), rot=(0, 0, ang))
    bar(0, .045, 0, .165)
    bar(0, .14, .035, .115)
    bar(0, .11, -.035, .085)

def feed_mound(m):
    """Futterhaufen im offenen Sack."""
    m.add('Feed', 'feed', jitter(dome(MOUND_R, MOUND_H, 18, 5, .0), .06, 7), pos=(0, MOUND_Y, 0))
    for i in range(30):
        a = R.random() * 2 * math.pi; rr = math.sqrt(R.random()) * MOUND_R * .85
        m.add('Feed', 'feed', sphere(.009, 5, 3), pos=(rr * math.cos(a), top_y(rr), rr * math.sin(a)), scl=(1.3, .7, 1))

def top_y(rr):
    return MOUND_Y + MOUND_H * math.sqrt(max(0, 1 - (rr / MOUND_R) ** 2))

def on_top(m, part, mat, prim_fn, n, rad=.08, rot=True):
    for i in range(n):
        a = R.random() * 2 * math.pi; rr = math.sqrt(R.random()) * rad
        m.add(part, mat, prim_fn(), pos=(rr * math.cos(a), top_y(rr) + .004, rr * math.sin(a)),
              rot=(R.uniform(0, 40), R.uniform(0, 360), 0) if rot else None)

# ---------------------------------------------------------------- die 6 Futter
def TamingHerb():
    m = new('TamingHerb'); sack(m); feed_mound(m)
    on_top(m, 'Herbs', 'leaf', lambda: sphere(.02, 6, 4), 9, rot=True)
    for i in range(4):   # Loewenzahnbluete
        a = 2 * math.pi * i / 4 + .4; rr = .05
        m.add('Flowers', 'dandelion', sphere(.014, 8, 4), pos=(rr * math.cos(a), top_y(rr) + .02, rr * math.sin(a)), scl=(1, .6, 1))
    on_top(m, 'Berries', 'rasp', lambda: sphere(.011, 7, 4), 5, rot=False)
    cord(m); rune(m); return m

def FreyaLure():
    m = new('FreyaLure'); sack(m); feed_mound(m)
    on_top(m, 'Berries', 'blue', lambda: sphere(.011, 7, 4), 10, rot=False)
    for i in range(3):
        a = 2 * math.pi * i / 3; rr = .045
        m.add('Carrots', 'carrot', cone(.012, .07, 6), pos=(rr * math.cos(a), top_y(rr), rr * math.sin(a)), rot=(R.uniform(-35, -15), math.degrees(a), 70))
    on_top(m, 'Mush', 'mush_yellow', lambda: sphere(.016, 8, 4), 3, rot=False)
    cord(m); rune(m); return m

def NornBlessing():
    m = new('NornBlessing'); sack(m); feed_mound(m)
    for i in range(3):
        a = 2 * math.pi * i / 3 + .5; rr = .05; y = top_y(rr)
        m.add('Turnips', 'turnip', sphere(.02, 8, 5), pos=(rr * math.cos(a), y + .012, rr * math.sin(a)), scl=(1, .9, 1))
        m.add('Turnips', 'leaf', cone(.006, .035, 4), pos=(rr * math.cos(a), y + .028, rr * math.sin(a)))
    on_top(m, 'Thistle', 'thistle', lambda: cone(.01, .03, 5), 6, rot=True)
    cord(m); rune(m); return m

def FenrirBite():
    m = new('FenrirBite'); sack(m); feed_mound(m)
    on_top(m, 'Meat', 'meat', lambda: jitter(box((.03, .022, .026)), .02, R.randint(0, 99)), 6, rot=True)
    for i in range(3):   # Fenris-Klauen ragen heraus
        a = 2 * math.pi * i / 3 + .2; rr = .04
        m.add('Claws', 'claw', cone(.008, .05, 6), pos=(rr * math.cos(a), top_y(rr), rr * math.sin(a)), rot=(R.uniform(-25, 25), 0, R.uniform(-30, 30)))
    cord(m); rune(m); return m

def ArmorGrain():
    m = new('ArmorGrain'); sack(m); feed_mound(m)
    on_top(m, 'Grain', 'barley', lambda: sphere(.006, 5, 3), 40, rad=.09, rot=True)
    for i in range(4):   # Gerstenaehren
        a = 2 * math.pi * i / 4 + .7; rr = .03; y = top_y(rr)
        base = (rr * math.cos(a), y, rr * math.sin(a))
        tip = (base[0] * 2.2, y + .07, base[2] * 2.2)
        m.add('Ears', 'barley', tube([base, tip], .0025, 4))
        m.add('Ears', 'barley', sphere(.008, 6, 4), pos=tip, scl=(1, 2.2, 1))
    on_top(m, 'Berries', 'cloud', lambda: sphere(.011, 7, 4), 4, rot=False)
    cord(m); rune(m); return m

def EmberShell():
    m = new('EmberShell'); sack(m); feed_mound(m)
    on_top(m, 'Berries', 'vineberry', lambda: sphere(.012, 7, 4), 7, rot=False)
    on_top(m, 'Embers', 'ember', lambda: jitter(sphere(.009, 6, 4), .2, R.randint(0, 99)), 8, rot=False)
    cord(m); rune(m); return m

ALL = [TamingHerb, FreyaLure, NornBlessing, FenrirBite, ArmorGrain, EmberShell]
if __name__ == '__main__':
    T = 260; tiles = []
    for f in ALL:
        mdl = f(); n = mdl.export(OUT); print('%-24s %5d Dreiecke' % (mdl.name, n))
        tiles.append(render(mdl, 25, -28, size=T, center=(0, .16, 0), dist=5.2, zoom=5.5))
    im = Image.new('RGB', (T * 3, T * 2))
    for i, t in enumerate(tiles): im.paste(t, ((i % 3) * T, (i // 3) * T))
    im.save(os.path.join(HERE, 'preview_feeds.png'))
    print('Vorschau: tools/preview_feeds.png')
