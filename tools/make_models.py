"""Erzeugt die 4 Modelle der Mod als OBJ/MTL (Ordner ../Models) und Vorschaubilder.
Aufruf:  python make_models.py          (benoetigt numpy + pillow)
Teilenamen: 'B_'  = immer sichtbar, 'S2_'/'S3_'/'S4_' = nur auf dieser Runenkessel-Stufe sichtbar,
            'S1_' = Basisstufe. Eigene Blender-Modelle koennen dieselben Objektnamen verwenden."""
import math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from meshlib import *

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', 'Models')

PALETTE = {
    'stone': ((.42, .42, .44), (0, 0, 0)), 'stone_dark': ((.26, .26, .29), (0, 0, 0)),
    'wood': ((.38, .23, .12), (0, 0, 0)), 'wood_dark': ((.22, .14, .09), (0, 0, 0)), 'wood_pale': ((.56, .43, .28), (0, 0, 0)),
    'iron': ((.17, .17, .19), (0, 0, 0)), 'copper': ((.66, .36, .15), (0, 0, 0)), 'rope': ((.55, .45, .27), (0, 0, 0)),
    'leaf': ((.2, .42, .14), (0, 0, 0)), 'moss': ((.27, .42, .14), (0, 0, 0)),
    'antler': ((.74, .66, .48), (0, 0, 0)), 'bone': ((.86, .81, .66), (0, 0, 0)), 'void': ((.04, .04, .05), (0, 0, 0)),
    'glow_gold': ((.9, .6, .15), (1.0, .62, .12)), 'glow_green': ((.45, .9, .15), (.35, .95, .1)),
    'glow_pink': ((.9, .3, .95), (.85, .2, .95)), 'glow_blue': ((.35, .75, 1), (.25, .65, 1.0)),
    'crystal_green': ((.4, .85, .2), (.2, .6, .08)), 'crystal_blue': ((.3, .5, .95), (.1, .25, .75)),
    'cap_purple': ((.55, .14, .66), (.18, .03, .25)), 'mush_stem': ((.82, .77, .72), (0, 0, 0)),
    'ice': ((.72, .88, 1.0), (.08, .2, .3)), 'ice_deep': ((.3, .55, .92), (.08, .28, .65)), 'frost': ((.92, .96, 1.0), (.05, .06, .07)),
    'glass': ((.55, .82, .95), (.12, .35, .48)), 'glass_green': ((.55, .9, .35), (.15, .38, .06)), 'glass_purple': ((.78, .45, .95), (.26, .08, .36)),
    'herb_red': ((.75, .15, .1), (0, 0, 0)), 'herb_orange': ((.85, .45, .1), (0, 0, 0)),
    'herb_yellow': ((.85, .72, .15), (0, 0, 0)), 'herb_green': ((.28, .5, .16), (0, 0, 0)),
    'fish': ((.55, .6, .65), (0, 0, 0)), 'meat': ((.4, .12, .1), (0, 0, 0)),
}

def newmodel(name, used):
    m = Model(name)
    for k in used: m.mat(k, *PALETTE[k])
    return m

def glyph(m, part, mat, parent, z=.0, s=1.0):
    """Stilisierte Lebensbaum-Rune aus Quadern."""
    k = s
    m.add(part, mat, box((.045, .44, .03)), pos=(0, 0, z), parent=parent + (k,))
    for sign in (-1, 1):
        m.add(part, mat, box((.04, .22, .03)), pos=(sign * .075, .1, z), rot=(0, 0, -sign * 42), parent=parent + (k,))
        m.add(part, mat, box((.04, .16, .03)), pos=(sign * .06, .2, z), rot=(0, 0, -sign * 42), parent=parent + (k,))
    m.add(part, mat, box((.34, .04, .03)), pos=(0, -.22, z), parent=parent + (k,))

def mushroom(m, capp, stemp, spotp, base, R, H, tilt=(0, 0, 0), seed=0, capmat='cap_purple', spotmat='glow_pink'):
    rnd = random.Random(seed)
    stem_h = H * 1.7
    rm = rotm(*tilt)
    top = np.array(base) + rm @ np.array([0, stem_h, 0])
    mid = np.array(base) + rm @ np.array([0, stem_h * .5, 0]) + np.array([.02, 0, 0])
    m.add(stemp, 'mush_stem', tube([base, mid, top], [R * .22, R * .16, R * .13], seg=7, smooth=3))
    m.add(capp, capmat, dome(R, H, 12, 5), pos=tuple(top), rot=tilt)
    for i in range(6):
        t = rnd.uniform(.35, 1.1); ph = rnd.uniform(0, 6.28)
        p = np.array([R * math.sin(t) * math.cos(ph) * 1.0, H * math.cos(t), R * math.sin(t) * math.sin(ph)]) * 1.02
        m.add(spotp, spotmat, sphere(R * .09, 6, 4), pos=tuple(top + rm @ p))

def crystal(m, part, mat, base, direction, r, h, seg=6):
    prof = [(0, 0), (r, 0), None, (r, 0), (r, h * .68), None, (r, h * .68), (0, h)]
    m.add(part, mat, lathe(prof, seg), pos=base, rot=look_rot(direction), flat=True)

def rocks(m, part, mat, n, radius, y, size, seed, arc=(0, 2 * math.pi)):
    R = random.Random(seed)
    for i in range(n):
        a = arc[0] + (arc[1] - arc[0]) * i / n + R.uniform(-.08, .08)
        r = radius + R.uniform(-.05, .05); s = R.uniform(.88, 1.15)
        m.add(part, mat, jitter(sphere(1, 7, 5), .16, i + seed), pos=(r * math.cos(a), y, r * math.sin(a)),
              scl=(size * 1.1 * s, size * .7 * s, size * .95 * s), rot=(0, -math.degrees(a), 0), flat=True)

def dragon(m, ice, eye, pos, yaw, k=1.0):
    p = (pos, (0, yaw, 0), k)
    m.add(ice, 'ice', box((.36, .2, .24)), pos=(.18, 0, 0), parent=p)
    m.add(ice, 'ice', box((.3, .12, .16)), pos=(.46, -.03, 0), rot=(0, 0, -6), parent=p)
    m.add(ice, 'ice', box((.34, .05, .17)), pos=(.4, -.14, 0), rot=(0, 0, -9), parent=p)
    for s in (-1, 1):
        m.add(ice, 'ice', tube([(.05, .1, s * .08), (-.12, .22, s * .13), (-.3, .26, s * .18)], [.04, .03, .008], seg=5, smooth=3), parent=p)
        m.add(ice, 'frost', cone(.025, .07, 5), pos=(.58, -.1, s * .05), rot=(180, 0, 0), parent=p)
        m.add(eye, 'glow_blue', sphere(.035, 6, 4), pos=(.24, .07, s * .125), parent=p)
        m.add(ice, 'ice', cone(.03, .09, 5), pos=(.5, .06, s * .06), parent=p)
    for i in range(3):
        m.add(ice, 'ice_deep', cone(.04, .12, 5), pos=(-.02 - i * .1, .1, 0), rot=(0, 0, 70), parent=p)

# =================================================================== RUNENKESSEL
def build_cauldron():
    m = newmodel('VFP_RuneCauldron', ['stone', 'stone_dark', 'wood', 'wood_dark', 'iron', 'rope', 'leaf', 'moss', 'antler', 'bone', 'void',
        'glow_gold', 'glow_green', 'glow_pink', 'glow_blue', 'crystal_green', 'crystal_blue', 'cap_purple', 'mush_stem', 'ice', 'ice_deep', 'frost'])
    R = random.Random(7)
    PX, PZ, BY = 1.08, -.1, 1.82          # Pfostenabstand, Pfosten-Z, Balkenhoehe
    # ---- Basis (immer sichtbar)
    rocks(m, 'B_Stones', 'stone', 12, 1.0, .13, .3, 11)
    m.add('B_Slab', 'stone_dark', box((.95, .55, .14)), pos=(0, .26, 1.2), rot=(-14, 0, 0))
    m.add('B_Slab', 'stone', box((.3, .3, .2)), pos=(-.62, .15, 1.15), rot=(0, 15, 0))
    m.add('B_Slab', 'stone', box((.3, .22, .2)), pos=(.62, .11, 1.18), rot=(0, -12, 0))
    for s in (-1, 1):
        m.add('B_Posts', 'wood', tube([(s * PX, 0, PZ), (s * (PX + .08), .55, PZ), (s * (PX + .06), 1.15, PZ), (s * PX, BY - .02, PZ)], [.19, .15, .13, .105], seg=8, smooth=4))
        m.add('B_Posts', 'wood_dark', cone(.3, .45, 8), pos=(s * PX, 0, PZ), scl=(1, 1, 1))
        for a in (-35, 40):
            m.add('B_Roots', 'wood_dark', tube([(s * PX, .25, PZ), (s * PX + math.cos(math.radians(a)) * .3, .1, PZ + math.sin(math.radians(a)) * .3),
                                                (s * PX + math.cos(math.radians(a)) * .55, .02, PZ + math.sin(math.radians(a)) * .55)], [.07, .05, .03], seg=5, smooth=3))
        m.add('B_Rope', 'rope', torus(.1, .022, 14, 6), pos=(s * (PX - .02), BY - .08, PZ), rot=(0, 0, 90))
        m.add('B_Beam', 'wood_dark', sphere(.11, 8, 6), pos=(s * (PX + .1), BY, PZ))
    m.add('B_Beam', 'wood_dark', tube([(-PX - .1, BY, PZ), (0, BY - .02, PZ), (PX + .1, BY, PZ)], .085, seg=8, smooth=4))
    for s in (-1, 1):   # Laternen-Aufhaengung
        x = s * .62
        m.add('B_Lantern', 'iron', tube([(x, BY - .05, PZ), (x, BY - .28, PZ)], .012, seg=4))
        m.add('B_Lantern', 'iron', cone(.13, .09, 4), pos=(x, BY - .3, PZ), rot=(0, 45, 0))
        m.add('B_Lantern', 'iron', box((.17, .03, .17)), pos=(x, BY - .56, PZ))
        for cx, cz in ((-.075, -.075), (.075, -.075), (-.075, .075), (.075, .075)):
            m.add('B_Lantern', 'iron', box((.015, .24, .015)), pos=(x + cx, BY - .43, PZ + cz))

    def stage_common(n, rune, glowmat):
        P = f'S{n}_'
        for s in (-1, 1):
            m.add(P + 'LanternGlow', glowmat, box((.13, .2, .13)), pos=(s * .62, BY - .43, PZ))
        glyph(m, P + 'Rune', rune, ((0, .27, 1.2), (-14, 0, 0)), z=.085, s=1.0)
        for i in range(10):
            a = 2 * math.pi * i / 10 + .15
            m.add(P + 'RingRunes', rune, box((.05, .012, .12)), pos=(.76 * math.cos(a), .03, .76 * math.sin(a)), rot=(0, -math.degrees(a), 0))

    # ---- Stufe 1: Standard (Geweih, Efeu, warme Laternen)
    stage_common(1, 'glow_gold', 'glow_gold')
    for s in (-1, 1):
        base = (s * PX, BY - .02, PZ)
        main = [(0, 0, 0), (s * .1, .3, 0), (s * .26, .6, .04), (s * .3, .98, .08)]
        m.add('S1_Antlers', 'antler', tube([np.add(base, p) for p in main], [.05, .042, .032, .016], seg=6, smooth=3))
        for (i, dx, dy) in ((1, s * .2, .22), (2, s * .22, .26), (2, -s * .02, .2)):
            p0 = np.add(base, main[i]); p1 = p0 + np.array([dx, dy, .02])
            m.add('S1_Antlers', 'antler', tube([p0, (p0 + p1) / 2 + np.array([0, .02, 0]), p1], [.026, .02, .008], seg=5, smooth=2))
    for i in range(18):
        s = R.choice((-1, 1)); t = R.uniform(.15, .95)
        p = (s * (PX + .06 * math.sin(t * 3)), t * 1.75, PZ + R.uniform(-.14, .14))
        m.add('S1_Ivy', 'leaf', sphere(1, 6, 4), pos=p, scl=(R.uniform(.09, .16), R.uniform(.05, .08), R.uniform(.08, .13)), rot=(R.uniform(0, 90), R.uniform(0, 180), R.uniform(0, 90)), flat=True)
    for i in range(7):
        x = -.9 + i * .3; L = R.uniform(.22, .5)
        m.add('S1_Ivy', 'leaf', tube([(x, BY - .06, PZ + .08), (x + R.uniform(-.05, .05), BY - .06 - L * .5, PZ + .09), (x + R.uniform(-.08, .08), BY - .06 - L, PZ + .08)], [.018, .015, .006], seg=4, smooth=2))
        m.add('S1_Ivy', 'leaf', sphere(1, 6, 4), pos=(x, BY - .05, PZ + .08), scl=(.12, .06, .1), flat=True)

    # ---- Stufe 2: Sumpf (Schaedel, Knochen, Moos, gruene Kristalle)
    stage_common(2, 'glow_green', 'glow_green')
    cx = (0, BY + .12, PZ)
    m.add('S2_Skull', 'bone', sphere(.21, 9, 7), pos=cx, scl=(1.0, .9, 1.15))
    m.add('S2_Skull', 'bone', box((.2, .12, .22)), pos=(0, BY - .02, PZ + .2))
    m.add('S2_Skull', 'bone', box((.17, .05, .18)), pos=(0, BY - .1, PZ + .17))
    for s in (-1, 1):
        m.add('S2_SkullEyes', 'glow_green', sphere(.045, 6, 4), pos=(s * .08, BY + .13, PZ + .22))
        m.add('S2_Skull', 'antler', tube([(s * .1, BY + .22, PZ), (s * .3, BY + .35, PZ), (s * .52, BY + .62, PZ), (s * .5, BY + .85, PZ)], [.055, .045, .03, .008], seg=6, smooth=4))
        for k in range(3):   # Rippen
            x0 = s * (PX - .02 - k * .02); z0 = PZ + (k - 1) * .13
            path = [(x0, BY - .05, z0), (s * (PX - .35), BY + .3, z0), (s * (PX - .75), BY + .28, z0), (s * (PX - .95), BY - .1, z0)]
            m.add('S2_Bones', 'bone', tube(path, [.05, .04, .035, .02], seg=6, smooth=5))
    m.add('S2_Bones', 'bone', tube([(-.55, .09, .75), (.0, .1, .8), (.55, .12, .78)], .05, seg=6))
    for s in (-1, 1):
        m.add('S2_Bones', 'bone', sphere(.075, 6, 4), pos=(s * .55, .1, .75 + s * .03))
        m.add('S2_Bones', 'bone', sphere(.075, 6, 4), pos=(s * .55, .1, .81 + s * .03))
    m.add('S2_Bones', 'bone', sphere(.13, 8, 6), pos=(-.95, .14, .55), scl=(1, .9, 1.1))
    m.add('S2_Bones', 'bone', box((.14, .06, .12)), pos=(-.95, .06, .66))
    for i in range(14):
        x = R.uniform(-.95, .95); L = R.uniform(.1, .32)
        m.add('S2_Moss', 'moss', sphere(1, 6, 4), pos=(x, BY - .04 - L * .5, PZ + R.uniform(-.05, .08)), scl=(.045, L * .55, .045), flat=True)
    for i in range(10):
        s = R.choice((-1, 1)); t = R.uniform(.1, .9)
        m.add('S2_Moss', 'moss', sphere(1, 6, 4), pos=(s * (PX + .03), t * 1.7, PZ + R.uniform(-.12, .12)), scl=(.12, .08, .1), rot=(0, R.uniform(0, 180), 0), flat=True)
    for (x, z, n) in ((-.95, .1, 3), (.95, .3, 4), (.45, -.85, 3)):
        for i in range(n):
            crystal(m, 'S2_Crystals', 'crystal_green', (x + R.uniform(-.12, .12), 0, z + R.uniform(-.12, .12)),
                    (R.uniform(-.35, .35), 1, R.uniform(-.35, .35)), R.uniform(.035, .06), R.uniform(.18, .38), 5)

    # ---- Stufe 3: Nebelland (Pilze, lila Glow, blaue Kristalle)
    stage_common(3, 'glow_pink', 'glow_pink')
    big = [((-PX - .02, BY - .1, PZ), .42, .26, (0, 0, 12)), ((PX + .02, 1.1, PZ), .3, .2, (0, 0, -18)), ((-.2, BY - .02, PZ), .17, .11, (0, 0, 8)),
           ((.5, BY - .02, PZ), .13, .09, (0, 0, -6)), ((PX - .2, BY - .02, PZ + .1), .2, .14, (0, 0, -10))]
    ground = [((-.95, 0, .5), .22, .14, (0, 0, 8)), ((-.7, 0, .75), .14, .1, (0, 0, -10)), ((.95, 0, .55), .26, .16, (0, 0, -6)),
              ((.75, 0, .85), .15, .1, (0, 0, 12)), ((.1, 0, -.85), .2, .13, (0, 0, 4)), ((-.4, 0, -.8), .13, .09, (0, 0, -8))]
    for i, (b, r, h, tl) in enumerate(big + ground):
        mushroom(m, 'S3_Caps', 'S3_Stems', 'S3_Spots', b, r, h, tl, seed=i)
    for i, (x, z) in enumerate(((.7, .45), (-.65, .6), (1.15, -.5), (-1.2, -.35))):
        for j in range(3):
            crystal(m, 'S3_Crystals', 'crystal_blue', (x + R.uniform(-.1, .1), 0, z + R.uniform(-.1, .1)), (R.uniform(-.4, .4), 1, R.uniform(-.4, .4)),
                    R.uniform(.03, .05), R.uniform(.15, .34), 5)
    for i in range(8):
        s = R.choice((-1, 1)); x = s * R.uniform(PX - .3, PX + .1) if i < 6 else R.uniform(-.3, .3)
        crystal(m, 'S3_Drips', 'crystal_blue', (x, BY - .12 - R.uniform(0, .05), PZ + R.uniform(-.1, .1)), (0, -1, 0), .03, R.uniform(.12, .24), 5)

    # ---- Stufe 4: Hoher Norden (Eis, Drachenkoepfe, Kristalle)
    stage_common(4, 'glow_blue', 'glow_blue')
    for s in (-1, 1):
        m.add('S4_IcePosts', 'ice', tube([(s * (PX + .02), .35, PZ), (s * (PX + .08), .75, PZ), (s * (PX + .08), 1.2, PZ)], [.2, .22, .17], seg=8, smooth=3), flat=True)
        m.add('S4_Frost', 'frost', sphere(1, 8, 5), pos=(s * PX, BY + .1, PZ), scl=(.2, .07, .16), flat=True)
        neck = [(s * (PX + .08), BY, PZ), (s * (PX + .2), BY + .3, PZ), (s * (PX + .05), BY + .62, PZ), (s * (PX - .25), BY + .74, PZ)]
        m.add('S4_Dragons', 'ice', tube(neck, [.13, .11, .095, .085], seg=7, smooth=4))
        for i in range(4):
            t = (i + 1) / 5
            p = np.array(neck[0]) * (1 - t) + np.array(neck[2]) * t + np.array([s * .08, .02, 0])
            m.add('S4_Dragons', 'ice_deep', cone(.035, .11, 5), pos=tuple(p), rot=(0, 0, -s * 40))
        dragon(m, 'S4_Dragons', 'S4_DragonEyes', (s * (PX - .3), BY + .74, PZ), 0 if s < 0 else 180, k=1.0)
    for i in range(9):
        x = -1.0 + i * .25 + R.uniform(-.04, .04)
        crystal(m, 'S4_Icicles', 'ice', (x, BY - .1, PZ + R.uniform(-.05, .05)), (0, -1, 0), .028, R.uniform(.1, .3), 5)
    for (x, z, n) in ((-1.1, .35, 5), (1.1, .4, 5), (.1, -.95, 4)):
        for i in range(n):
            crystal(m, 'S4_Crystals', 'ice_deep' if i % 2 else 'ice', (x + R.uniform(-.18, .18), 0, z + R.uniform(-.18, .18)),
                    (R.uniform(-.45, .45), 1, R.uniform(-.45, .45)), R.uniform(.05, .09), R.uniform(.3, .65))
    for i in range(8):
        a = 2 * math.pi * i / 8
        m.add('S4_Frost', 'frost', sphere(1, 7, 4), pos=(1.0 * math.cos(a), .24, 1.0 * math.sin(a)), scl=(.22, .06, .2), flat=True)
    return m

# =================================================================== gemeinsame Deko fuer die Erweiterungen
THEME = {2: dict(rune='glow_green', crystal='crystal_green', glass='glass_green', core='crystal_green'),
         3: dict(rune='glow_pink', crystal='crystal_blue', glass='glass_purple', core='cap_purple'),
         4: dict(rune='glow_blue', crystal='ice_deep', glass='glass', core='ice')}

def allmats(name): return newmodel(name, list(PALETTE.keys()))

def skull(m, P, pos, yaw=0, k=1.0):
    p = (pos, (0, yaw, 0), k)
    m.add(P + 'Skull', 'bone', sphere(.21, 9, 7), scl=(1.0, .9, 1.15), parent=p)
    m.add(P + 'Skull', 'bone', box((.2, .12, .22)), pos=(0, -.14, .2), parent=p)
    m.add(P + 'Skull', 'bone', box((.17, .05, .18)), pos=(0, -.22, .17), parent=p)
    for s in (-1, 1):
        m.add(P + 'SkullEyes', 'glow_green', sphere(.045, 6, 4), pos=(s * .08, .01, .22), parent=p)
        m.add(P + 'Skull', 'antler', tube([(s * .1, .1, 0), (s * .3, .23, 0), (s * .5, .5, 0), (s * .48, .73, 0)], [.055, .045, .03, .008], seg=6, smooth=4), parent=p)

def ribs(m, P, a, b, apex, zs):
    for z in zs:
        dx = b[0] - a[0]
        path = [(a[0], a[1], z), (a[0] + dx * .15, apex * .85, z), (a[0] + dx * .5, apex, z), (b[0] - dx * .15, apex * .85, z), (b[0], b[1], z)]
        m.add(P + 'Bones', 'bone', tube(path, [.05, .045, .04, .04, .02], seg=6, smooth=5))

def femur(m, P, a, b):
    m.add(P + 'Bones', 'bone', tube([a, b], .045, seg=6))
    for e, o in ((a, .035), (b, .035)):
        for z in (-1, 1):
            m.add(P + 'Bones', 'bone', sphere(.07, 6, 4), pos=(e[0], e[1], e[2] + z * o))

def moss_drips(m, P, pts, R, Lmin=.1, Lmax=.3):
    for (x, y, z) in pts:
        L = R.uniform(Lmin, Lmax)
        m.add(P + 'Moss', 'moss', sphere(1, 6, 4), pos=(x, y - L * .5, z), scl=(.045, L * .55, .045), flat=True)

def moss_patches(m, P, pts, R):
    for (x, y, z) in pts:
        m.add(P + 'Moss', 'moss', sphere(1, 6, 4), pos=(x, y, z), scl=(R.uniform(.09, .15), .05, R.uniform(.08, .12)), rot=(0, R.uniform(0, 180), 0), flat=True)

def crystals_at(m, P, mat, spots, R, n=3, hmin=.15, hmax=.32, rmin=.03, rmax=.05):
    for (x, y, z) in spots:
        for i in range(n):
            crystal(m, P + 'Crystals', mat, (x + R.uniform(-.1, .1), y, z + R.uniform(-.1, .1)), (R.uniform(-.4, .4), 1, R.uniform(-.4, .4)),
                    R.uniform(rmin, rmax), R.uniform(hmin, hmax), 5)

def mush_at(m, P, items, seed0=0):
    for i, (b, r, h, tl) in enumerate(items):
        mushroom(m, P + 'Caps', P + 'Stems', P + 'Spots', b, r, h, tl, seed=seed0 + i)

def icicles(m, P, pts, R, Lmin=.1, Lmax=.28):
    for (x, y, z) in pts:
        crystal(m, P + 'Icicles', 'ice', (x, y, z), (0, -1, 0), .028, R.uniform(Lmin, Lmax), 5)

def frost_caps(m, P, items):
    for (pos, sc) in items:
        m.add(P + 'Frost', 'frost', sphere(1, 8, 5), pos=pos, scl=sc, flat=True)

def dragon_neck(m, P, base, s, k=1.0):
    rel = np.array([(0, 0, 0), (s * .12, .3, 0), (-s * .03, .62, 0), (-s * .33, .74, 0)]) * k
    pts = np.array(base) + rel
    m.add(P + 'Dragons', 'ice', tube(pts, np.array([.13, .11, .095, .085]) * k, seg=7, smooth=4))
    for i in range(3):
        t = (i + 1) / 4
        p = pts[0] * (1 - t) + pts[2] * t + np.array([s * .08 * k, .02, 0])
        m.add(P + 'Dragons', 'ice_deep', cone(.035 * k, .11 * k, 5), pos=tuple(p), rot=(0, 0, -s * 40))
    dragon(m, P + 'Dragons', P + 'DragonEyes', tuple(pts[-1]), 0 if s < 0 else 180, k=k)

def lamp(m, P, pos, glow):
    x, y, z = pos
    m.add('B_Lamp', 'iron', box((.13, .03, .13)), pos=(x, y + .015, z))
    m.add('B_Lamp', 'iron', cone(.1, .06, 4), pos=(x, y + .27, z), rot=(0, 45, 0))
    for cx, cz in ((-.055, -.055), (.055, -.055), (-.055, .055), (.055, .055)):
        m.add('B_Lamp', 'iron', box((.012, .22, .012)), pos=(x + cx, y + .14, z + cz))
    m.add(P + 'LampGlow', glow, box((.09, .16, .09)), pos=(x, y + .13, z))

# =================================================================== GAERFASS
def build_barrel():
    m = allmats('VFP_FermentBarrel')
    ox, y0, H = -.28, .16, 1.05
    def rad(y): return .4 + .1 * math.sin(math.pi * y / H)
    prof = [(0, 0), (rad(0), 0), None] + [(rad(H * i / 12), H * i / 12) for i in range(13)] + [None, (rad(H), H), (0, H)]
    m.add('B_Barrel', 'wood', lathe(prof, 14), pos=(ox, y0, 0))
    for y in (.12, .5, .93):
        m.add('B_Hoops', 'iron', torus(rad(y) + .005, .03, 24, 6), pos=(ox, y0 + y, 0))
    m.add('B_Lid', 'wood_dark', cyl(.4, .06, 14), pos=(ox, y0 + H, 0))
    for z in (-.3, .3):
        m.add('B_Cradle', 'wood_dark', box((1.05, .12, .14)), pos=(ox, .06, z))
        for x in (-.52, .52):
            m.add('B_Cradle', 'wood', box((.1, .16, .18)), pos=(ox + x, .08, z))
    m.add('B_Tap', 'copper', cyl(.035, .24, 8), pos=(ox, y0 + .38, .43), rot=(90, 0, 0))
    m.add('B_Tap', 'copper', box((.15, .03, .03)), pos=(ox, y0 + .38, .67))
    m.add('B_Tap', 'copper', sphere(.05, 7, 5), pos=(ox, y0 + .38, .46))
    top = y0 + H
    pipe = [(ox + .15, top, 0), (ox + .15, top + .45, 0), (ox + .25, top + .6, 0), (ox + .6, top + .62, 0), (ox + 1.0, top + .6, 0), (ox + 1.12, top + .45, 0), (ox + 1.12, .55, 0)]
    m.add('B_Pipes', 'copper', tube(pipe, .05, seg=8, smooth=4))
    m.add('B_Pipes', 'iron', torus(.055, .018, 12, 5), pos=(ox + .15, top + .25, 0))
    m.add('B_Pipes', 'iron', torus(.055, .018, 12, 5), pos=(ox + .6, top + .62, 0), rot=(0, 0, 90))
    m.add('B_Pipes', 'iron', torus(.055, .018, 12, 5), pos=(ox + 1.12, top + .0 + .85, 0))
    m.add('B_Pipes', 'copper', tube([(ox + .15, top - .02, 0), (ox + .15, top + .2, 0)], .08, seg=8))
    m.add('B_Pipes', 'copper', sphere(.075, 8, 6), pos=(ox + .15, top, 0))
    m.add('B_Pipes', 'copper', tube([(ox + .5, top + .12, .12), (ox + .5, top + .6, .12), (ox + .62, top + .65, .12)], .03, seg=6, smooth=2))
    m.add('B_Tank', 'copper', cyl(.19, .5, 12), pos=(ox + 1.12, 0, 0))
    for y in (.12, .4):
        m.add('B_Tank', 'iron', torus(.19, .022, 16, 5), pos=(ox + 1.12, y, 0))
    m.add('B_Tank', 'iron', cone(.21, .12, 12), pos=(ox + 1.12, .5, 0))
    for n in (2, 3, 4):
        P = 'S%d_' % n; T = THEME[n]; R = random.Random(100 + n)
        glyph(m, P + 'Rune', T['rune'], ((ox, y0 + .55, rad(.55) + .02), (0, 0, 0)), s=.8)
        m.add(P + 'Gauge', T['rune'], sphere(.06, 8, 6), pos=(ox + 1.12, .27, .19))
        lamp(m, P, (ox - .8, 0, .42), T['rune'])
        lid = [(ox + R.uniform(-.3, .3), top + .05, R.uniform(-.3, .3)) for _ in range(12)]
        if n == 2:
            skull(m, P, (ox - .12, top + .12, .15), k=.8)
            ribs(m, P, (ox - .7, 0), (ox + .55, 0), top + .3, [-.36, -.2, .3, .4])
            moss_patches(m, P, lid, R)
            moss_drips(m, P, [(ox + .43 * math.cos(a), top - .02, .43 * math.sin(a)) for a in np.linspace(0, 6.28, 14)], R)
            crystals_at(m, P, T['crystal'], [(ox - .65, 0, .1), (ox + .55, 0, .6)], R)
            femur(m, P, (ox - .95, .08, .65), (ox - .45, .08, .78))
        elif n == 3:
            mush_at(m, P, [((ox - .15, top, -.12), .24, .15, (0, 0, 8)), ((ox - .3, top, .12), .13, .09, (0, 0, -6)), ((ox + .05, top, .25), .1, .07, (0, 0, 4)),
                           ((ox - .72, 0, .42), .22, .14, (0, 0, 8)), ((ox - .52, 0, .78), .13, .09, (0, 0, -10)), ((ox + .55, 0, .6), .17, .11, (0, 0, 6)),
                           ((ox + 1.0, 0, -.5), .16, .1, (0, 0, -8)), ((ox - .75, 0, -.5), .14, .1, (0, 0, 8))], seed0=n * 10)
            crystals_at(m, P, T['crystal'], [(ox - .9, 0, -.1), (ox + .7, 0, .7), (ox + .2, 0, -.7)], R)
        else:
            dragon_neck(m, P, (ox - .68, .45, -.3), -1, k=.85)
            dragon_neck(m, P, (ox + .68, .45, -.3), 1, k=.85)
            frost_caps(m, P, [((ox, top + .07, 0), (.4, .06, .4)), ((ox + 1.12, .6, 0), (.2, .07, .2)), ((ox - .3, .15, .3), (.5, .05, .12)), ((ox + .3, .15, -.3), (.5, .05, .12))])
            icicles(m, P, [(ox + .41 * math.cos(a), top - .02, .41 * math.sin(a)) for a in np.linspace(0, 6.28, 12)], R)
            crystals_at(m, P, T['crystal'], [(ox - .85, 0, .1), (ox + .75, 0, .65), (ox - .3, 0, -.75)], R, n=3, hmin=.3, hmax=.6, rmin=.05, rmax=.09)
    return m

# =================================================================== TROCKENGESTELL
def build_rack():
    m = allmats('VFP_RuneDryingRack')
    R = random.Random(5)
    W, H = .98, 2.05
    for s in (-1, 1):
        for z in (-.33, .33):
            m.add('B_Frame', 'wood', tube([(s * W + s * .04, 0, z), (s * W, H, 0)], [.075, .06], seg=7))
        m.add('B_Frame', 'wood_dark', tube([(s * W + s * .02, .65, -.2), (s * W + s * .02, .65, .2)], .045, seg=6))
        m.add('B_Frame', 'stone', jitter(sphere(1, 7, 5), .15, 3 + int(s)), pos=(s * (W + .1), .09, 0), scl=(.3, .12, .5), flat=True)
        m.add('B_Frame', 'wood_dark', sphere(.085, 7, 5), pos=(s * W, H, 0))
        m.add('B_Rope', 'rope', torus(.07, .016, 12, 5), pos=(s * W, H - .1, 0), rot=(0, 0, 90))
        m.add('B_Plaque', 'wood_pale', box((.05, .3, .28)), pos=(s * (W + .02), 1.25, 0))
    m.add('B_Beam', 'wood_dark', tube([(-W - .15, H, 0), (W + .15, H, 0)], .065, seg=7))
    m.add('B_Beam', 'wood', tube([(-W + .05, 1.28, 0), (W - .05, 1.28, 0)], .045, seg=6))
    cols = ['herb_red', 'herb_orange', 'herb_yellow', 'herb_green', 'herb_red', 'herb_green', 'herb_orange', 'herb_yellow']
    for i in range(8):
        x = -.82 + i * .235; L = R.uniform(.38, .55); c = cols[i]
        m.add('D_String', 'rope', tube([(x, H - .03, 0), (x, H - .16, 0)], .008, seg=4))
        prof = [(0, 0), (.02, .02), (.07, .12), (.095, .28), (.06, .46), (.03, .54), (0, .58)]
        m.add('D_' + c, c, lathe(prof, 8), pos=(x, H - .16 - L, 0), scl=(1, L / .58, 1), rot=(180, 0, 0))
        m.add('D_String', 'rope', torus(.04, .009, 10, 4), pos=(x, H - .2, 0))
    for x in (-.62, -.2, .62):
        m.add('D_String', 'rope', tube([(x, 1.26, 0), (x, 1.1, 0)], .008, seg=4))
        m.add('D_Fish', 'fish', sphere(1, 8, 6), pos=(x, .82, 0), scl=(.045, .26, .11))
        m.add('D_Fish', 'fish', cone(.1, .12, 4), pos=(x, .52, 0), rot=(180, 0, 0), scl=(.35, 1, 1))
    for i, x in enumerate((.12, .27, .42)):
        m.add('D_String', 'rope', tube([(x, 1.26, 0), (x, 1.12, 0)], .008, seg=4))
        m.add('D_Meat', 'meat', box((.07, .34, .05)), pos=(x, .93 - i * .02, 0), rot=(0, 0, R.uniform(-5, 5)))
    for n in (2, 3, 4):
        P = 'S%d_' % n; T = THEME[n]; R = random.Random(200 + n)
        for s in (-1, 1):
            glyph(m, P + 'Runes', T['rune'], ((s * (W + .02), 1.25, 0), (0, s * 90, 0)), z=.04, s=.55)
        lamp(m, P, (W + .4, 0, .35), T['rune'])
        if n == 2:
            skull(m, P, (-W, H + .12, 0), yaw=0, k=.75)
            femur(m, P, (-.4, .08, .7), (.35, .08, .8))
            femur(m, P, (.6, .08, -.6), (1.0, .08, -.45))
            m.add(P + 'Bones', 'bone', sphere(.13, 8, 6), pos=(.7, .14, .55), scl=(1, .9, 1.1))
            moss_drips(m, P, [(-W + .1 + i * .2, H - .06, .09) for i in range(10)], R, .08, .22)
            moss_patches(m, P, [(s * (W + .02), R.uniform(.2, 1.7), R.uniform(-.1, .1)) for s in (-1, 1) for _ in range(4)], R)
            crystals_at(m, P, T['crystal'], [(-.8, 0, .55), (.2, 0, -.75), (1.2, 0, .1)], R)
        elif n == 3:
            mush_at(m, P, [((-W, H, 0), .3, .18, (0, 0, 10)), ((W, H, 0), .22, .14, (0, 0, -10)),
                           ((-.62, 0, .5), .2, .13, (0, 0, 8)), ((-.4, 0, .62), .12, .08, (0, 0, -10)), ((.65, 0, -.55), .22, .14, (0, 0, -6)),
                           ((.85, 0, .45), .13, .09, (0, 0, 10)), ((-.9, 0, -.55), .15, .1, (0, 0, 5))], seed0=40)
            crystals_at(m, P, T['crystal'], [(.2, 0, .6), (-.9, 0, .2), (1.1, 0, -.2)], R)
        else:
            dragon_neck(m, P, (-W - .12, H - .05, 0), -1, k=.9)
            dragon_neck(m, P, (W + .12, H - .05, 0), 1, k=.9)
            frost_caps(m, P, [((x, H + .06, 0), (.22, .06, .1)) for x in (-.6, -.2, .2, .6)] + [((s * (W + .1), .2, 0), (.3, .07, .4)) for s in (-1, 1)])
            icicles(m, P, [(-.9 + i * .2, H - .05, .09) for i in range(10)], R, .08, .2)
            crystals_at(m, P, T['crystal'], [(-1.2, 0, .5), (1.25, 0, .5), (.1, 0, -.8)], R, n=3, hmin=.3, hmax=.6, rmin=.05, rmax=.09)
    return m

# =================================================================== EISDESTILLERIE
def build_distillery():
    m = allmats('VFP_IceDistillery')
    m.add('B_Base', 'wood_dark', box((1.8, .22, 1.2)), pos=(0, .11, 0))
    m.add('B_Base', 'stone_dark', box((1.9, .08, 1.3)), pos=(0, .04, 0))
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.add('B_Base', 'wood', box((.14, .26, .14)), pos=(sx * .88, .13, sz * .58))
    fx, cx = -.5, .42
    m.add('B_Flask', 'iron', torus(.24, .035, 16, 5), pos=(fx, .27, 0))
    m.add('B_Flask', 'wood', cyl(.115, .09, 10), pos=(fx, 1.23, 0))
    m.add('B_Flask', 'iron', torus(.1, .02, 12, 5), pos=(fx, 1.05, 0))
    m.add('B_Coil', 'copper', tube(helix(cx, 0, .17, 1.35, .55, 4.5, 90), .03, seg=6))
    for a in (90, 270):
        pa = math.radians(a)
        m.add('B_Coil', 'wood', tube([(cx + .3 * math.cos(pa), .22, .3 * math.sin(pa)), (cx + .3 * math.cos(pa), 1.5, .3 * math.sin(pa))], .025, seg=5))
    for y in (1.48, .6):
        m.add('B_Coil', 'iron', torus(.3, .018, 18, 5), pos=(cx, y, 0))
    m.add('B_Pipes', 'copper', tube([(fx, 1.25, 0), (fx, 1.45, 0), (fx + .1, 1.62, 0), (cx, 1.66, 0), (cx + .17, 1.45, 0), (cx + .17, 1.35, 0)], .042, seg=7, smooth=4))
    m.add('B_Pipes', 'iron', torus(.05, .016, 10, 5), pos=(fx, 1.45, 0))
    m.add('B_Pipes', 'copper', tube([(cx - .17, .55, 0), (cx - .12, .42, .15), (cx + .1, .6, .5), (cx + .1, .72, .5)], .035, seg=6, smooth=4))
    m.add('B_Pipes', 'iron', torus(.045, .014, 10, 4), pos=(cx + .1, .6, .5))
    for n in (2, 3, 4):
        P = 'S%d_' % n; T = THEME[n]; R = random.Random(300 + n)
        liq = T['rune']
        m.add(P + 'Glass', T['glass'], sphere(.36, 14, 10), pos=(fx, .6, 0))
        m.add(P + 'Glass', T['glass'], cyl(.1, .38, 10, .085), pos=(fx, .87, 0))
        m.add(P + 'Liquid', liq, sphere(.22, 10, 7), pos=(fx, .5, 0))
        for i in range(5):
            m.add(P + 'Liquid', liq, sphere(.03 + .01 * (i % 2), 6, 4), pos=(fx + R.uniform(-.1, .1), .78 + i * .07, R.uniform(-.06, .06)))
        m.add(P + 'Liquid', liq, lathe([(0, 0), (.1, 0), (.1, .2), (.04, .28), (.04, .38), (0, .38)], 10), pos=(cx + .1, .22, .5))
        m.add(P + 'Core', T['core'], cyl(.1, 1.05, 10), pos=(cx, .22, 0), flat=True)
        glyph(m, P + 'Rune', liq, ((.0, .13, .62), (0, 0, 0)), z=.0, s=.3)
        lamp(m, P, (-.9, .22, .45), liq)
        if n == 2:
            skull(m, P, (.78, .42, .66), yaw=-30, k=.7)
            ribs(m, P, (-.85, .22), (.05, .22), 1.3, [-.5])
            ribs(m, P, (.15, .22), (.85, .22), 1.05, [-.5])
            femur(m, P, (-.3, .26, .5), (.2, .26, .58))
            moss_drips(m, P, [(-.85 + i * .17, .24, .6) for i in range(11)], R, .06, .2)
            moss_patches(m, P, [(R.uniform(-.8, .8), .24, R.uniform(-.5, .5)) for _ in range(9)], R)
            crystals_at(m, P, T['crystal'], [(-1.0, .02, -.45), (.95, .02, -.5), (-.95, .02, .5)], R)
        elif n == 3:
            mush_at(m, P, [((-.85, .22, -.4), .22, .14, (0, 0, 8)), ((.82, .22, -.35), .2, .13, (0, 0, -8)), ((.85, .22, .5), .13, .09, (0, 0, 8)),
                           ((-1.05, 0, .55), .24, .15, (0, 0, 6)), ((1.0, 0, .6), .17, .11, (0, 0, -8)), ((1.05, 0, -.5), .16, .1, (0, 0, 6)), ((-.4, 0, -.8), .14, .09, (0, 0, 0))], seed0=80)
            crystals_at(m, P, T['crystal'], [(-1.0, .02, -.45), (.9, .02, -.55)], R)
        else:
            frost_caps(m, P, [((-.8 + i * .145, .24, z + R.uniform(-.03, .03)), (.11, .05, .09)) for i in range(12) for z in (.55, -.55)])
            dragon(m, P + 'Dragon', P + 'DragonEyes', (.78, .5, .7), -40, k=.7)
            m.add(P + 'Dragon', 'ice', sphere(1, 8, 5), pos=(.75, .35, .62), scl=(.2, .16, .18), flat=True)
            crystals_at(m, P, 'ice_deep', [(-.95, .02, -.45), (.9, .02, -.5), (-.85, .02, .5)], R, n=4, hmin=.3, hmax=.75, rmin=.05, rmax=.1)
            icicles(m, P, [(-.85 + i * .34, .2, .58) for i in range(6)], R, .08, .2)
    return m

# =================================================================== Ausgabe
def sheet(path, tiles, size=520):
    n = len(tiles); font = ImageFont.load_default(size=26)
    im = Image.new('RGB', (size * n, size + 40), (10, 14, 16)); d = ImageDraw.Draw(im)
    for i, (title, tile) in enumerate(tiles):
        im.paste(tile, (i * size, 40)); d.text((i * size + 14, 6), title, fill=(230, 210, 150), font=font)
    im.save(path)

if __name__ == '__main__':
    previews = '--no-preview' not in sys.argv
    models = [build_cauldron(), build_barrel(), build_rack(), build_distillery()]
    for m in models:
        n = m.export(OUT); print('%-22s %3d Teile %6d Dreiecke' % (m.name, len(m.parts), n))
    if previews:
        c, b, r, d = models
        def stage(n): return lambda p: p.startswith('B_') or p.startswith('S%d_' % n)
        tiles = [('Stufe %d' % n, render(c, 28, 17, include=stage(n), center=(0, 1.2, 0), dist=5.6, zoom=.72)) for n in (1, 2, 3, 4)]
        sheet(os.path.join(HERE, 'preview_cauldron.png'), tiles)
        T = 380
        rows = []
        for mdl, ctr, zm, yaw in ((b, (.1, .95, 0), .95, 30), (r, (0, 1.0, 0), .8, 25), (d, (0, .85, 0), .9, 28)):
            rows.append([render(mdl, yaw, 16, size=T, include=(lambda n: (lambda p: p.startswith(('B_', 'D_', 'S%d_' % n))))(n), center=ctr, dist=5.4, zoom=zm) for n in (2, 3, 4)])
        im = Image.new('RGB', (T * 3, T * 3 + 40), (10, 14, 16)); dr = ImageDraw.Draw(im); f = ImageFont.load_default(size=24)
        for ci, t in enumerate(('Stufe 2 Sumpf', 'Stufe 3 Nebelland', 'Stufe 4 Hoher Norden')): dr.text((ci * T + 12, 8), t, fill=(230, 210, 150), font=f)
        for ri, row in enumerate(rows):
            for ci, t in enumerate(row): im.paste(t, (ci * T, 40 + ri * T))
        im.save(os.path.join(HERE, 'preview_extensions.png'))
