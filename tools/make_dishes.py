"""Eigene 3D-Modelle fuer Saisongerichte (Objektstaender/Boden): VFP_MidsummerCake, VFP_TurnipGhostSoup, VFP_YuleRoast.
Ursprung = Mitte unten, Y nach oben, Groesse ca. 0.3 m. Aufruf: python make_dishes.py"""
import math, os, random, sys
import numpy as np
from PIL import Image, ImageDraw
from meshlib import *
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, '..', 'Models')
MATS = {
    'wood': ((0.34, 0.22, 0.13), (0, 0, 0)), 'wood_dark': ((0.20, 0.13, 0.08), (0, 0, 0)),
    'sponge': ((0.72, 0.52, 0.28), (0, 0, 0)), 'cream': ((0.88, 0.84, 0.74), (0, 0, 0)),
    'rasp': ((0.62, 0.12, 0.16), (0, 0, 0)), 'blue': ((0.18, 0.22, 0.48), (0, 0, 0)),
    'honey': ((0.85, 0.55, 0.12), (0.20, 0.10, 0.0)), 'dandelion': ((0.90, 0.75, 0.15), (0.10, 0.08, 0)),
    'leaf': ((0.22, 0.38, 0.14), (0, 0, 0)), 'broth': ((0.35, 0.65, 0.18), (0.10, 0.25, 0.03)),
    'turnip': ((0.40, 0.20, 0.42), (0, 0, 0)), 'turnip_top': ((0.30, 0.45, 0.18), (0, 0, 0)),
    'carve': ((0.95, 0.75, 0.25), (0.60, 0.40, 0.08)), 'slime': ((0.30, 0.60, 0.20), (0.06, 0.14, 0.02)),
    'eye': ((0.05, 0.05, 0.05), (0, 0, 0)), 'meat': ((0.42, 0.20, 0.10), (0, 0, 0)), 'meat_glaze': ((0.62, 0.34, 0.12), (0.06, 0.03, 0)),
    'bone': ((0.78, 0.72, 0.58), (0, 0, 0)), 'carrot': ((0.82, 0.40, 0.10), (0, 0, 0)), 'mistle_berry': ((0.92, 0.92, 0.86), (0, 0, 0)),
    'bowl_white': ((0.70, 0.68, 0.62), (0, 0, 0)),
    'shroom': ((0.42, 0.24, 0.12), (0, 0, 0)), 'shroom_under': ((0.22, 0.12, 0.06), (0, 0, 0)),
    'sausage': ((0.16, 0.07, 0.06), (0, 0, 0)), 'sausage_cut': ((0.34, 0.10, 0.10), (0, 0, 0)), 'fat': ((0.90, 0.86, 0.78), (0, 0, 0)),
    'string_red': ((0.70, 0.12, 0.12), (0, 0, 0)), 'rune_green': ((0.50, 0.85, 0.30), (0.25, 0.55, 0.10)),
    'roulade': ((0.30, 0.13, 0.07), (0, 0, 0)), 'roulade_cut': ((0.72, 0.42, 0.38), (0, 0, 0)), 'string': ((0.88, 0.84, 0.72), (0, 0, 0)),
    'frost': ((0.86, 0.92, 0.98), (0.10, 0.16, 0.22)), 'sauce': ((0.30, 0.12, 0.05), (0, 0, 0)), 'pickle': ((0.40, 0.55, 0.25), (0, 0, 0)),
    'egg_white': ((0.94, 0.92, 0.86), (0, 0, 0)), 'yolk': ((0.95, 0.62, 0.10), (0.15, 0.08, 0)), 'bacon': ((0.62, 0.22, 0.18), (0, 0, 0)), 'bacon_fat': ((0.92, 0.78, 0.68), (0, 0, 0)),
    'crust': ((0.55, 0.32, 0.14), (0, 0, 0)), 'crumb': ((0.82, 0.70, 0.52), (0, 0, 0)), 'magecap': ((0.55, 0.30, 0.80), (0.20, 0.10, 0.35)),
    'stick': ((0.50, 0.38, 0.24), (0, 0, 0)), 'asksvin': ((0.42, 0.16, 0.08), (0, 0, 0)), 'sulfur': ((0.90, 0.78, 0.15), (0.20, 0.16, 0.02)),
    'vineberry': ((0.62, 0.12, 0.30), (0, 0, 0)), 'fern': ((0.30, 0.50, 0.18), (0, 0, 0)),
    'pancake': ((0.80, 0.56, 0.28), (0, 0, 0)), 'pancake_side': ((0.55, 0.32, 0.12), (0, 0, 0)), 'lingon': ((0.80, 0.10, 0.15), (0, 0, 0)),
    'plate': ((0.62, 0.66, 0.70), (0, 0, 0)),
}
def M(name):
    m = Model(name)
    for k, v in MATS.items(): m.mat(k, *v)
    return m
R = random.Random(4)
def leaf(m, pos, yaw, k=1.0):
    m.add('Leaves', 'leaf', sphere(.03 * k, 6, 4), pos=pos, rot=(0, yaw, 0), scl=(1.6, .25, .7))

def cake():
    m = M('VFP_MidsummerCake')
    m.add('Board', 'wood', cyl(.17, .02, 20))
    m.add('Cake', 'sponge', cyl(.13, .085, 20), pos=(0, .02, 0))
    m.add('Cream', 'cream', cyl(.133, .016, 20), pos=(0, .102, 0))
    m.add('Cream', 'cream', cyl(.132, .01, 20), pos=(0, .055, 0))            # Cremeschicht in der Mitte
    for i in range(12):                                                     # Cremetupfer am Rand
        a = 2 * math.pi * i / 12
        m.add('Cream', 'cream', sphere(.018, 8, 5), pos=(.11 * math.cos(a), .12, .11 * math.sin(a)))
    for i in range(14):                                                     # Beeren oben
        a = R.random() * 2 * math.pi; r = math.sqrt(R.random()) * .085
        mat = 'rasp' if i % 2 else 'blue'
        m.add('Berries', mat, sphere(.016 if mat == 'rasp' else .013, 8, 5), pos=(r * math.cos(a), .128, r * math.sin(a)), scl=(1, 1.15 if mat == 'rasp' else 1, 1))
    m.add('Honey', 'honey', cyl(.045, .006, 14), pos=(0, .118, 0))
    for i in range(5):                                                      # Honig laeuft am Rand herunter
        a = 2 * math.pi * i / 5 + .3
        m.add('Honey', 'honey', cyl(.009, .04, 6), pos=(.132 * math.cos(a), .075, .132 * math.sin(a)))
    for a in (0.6, 2.4, 4.3):                                               # Loewenzahn
        x, z = .095 * math.cos(a), .095 * math.sin(a)
        m.add('Flowers', 'dandelion', sphere(.02, 8, 4), pos=(x, .135, z), scl=(1, .4, 1))
        leaf(m, (x + .02, .14, z), a * 57)
    return m

def soup():
    m = M('VFP_TurnipGhostSoup')
    prof = [(0, 0), (.08, 0), (.12, .02), (.15, .07), (.155, .1), (.14, .1), (.13, .07), (.1, .03), (0, .03)]
    m.add('Bowl', 'wood', lathe(prof, 20))
    m.add('Broth', 'broth', cyl(.135, .008, 20), pos=(0, .078, 0))
    # grinsende Ruebe (lila, wie im Spiel), etwas eingesunken
    tc = (0, .13, 0)
    m.add('Turnip', 'turnip', sphere(.075, 14, 9), pos=tc, scl=(1, .9, 1))
    m.add('Turnip', 'turnip_top', cone(.02, .07, 6), pos=(0, .2, 0))
    for a in (-30, 40, 160):
        m.add('Turnip', 'turnip_top', sphere(.02, 6, 4), pos=(.03 * math.cos(math.radians(a)), .24, .03 * math.sin(math.radians(a))), scl=(1.8, .3, .8), rot=(0, a, 0))
    # Gesicht vorne (+z): leuchtende Augen und breites Grinsen
    for x in (-.028, .028):
        m.add('Face', 'carve', cone(.016, .012, 3), pos=(x, .145, .068), rot=(90, 0, 0))
    for i in range(7):
        t = (i - 3) / 3.0
        m.add('Face', 'carve', box((.012, .012, .01)), pos=(t * .04, .112 - .012 * (1 - t * t), .068))
    # kleine Schleimklumpen mit Augen in der Bruehe
    for (x, z, s) in ((-.085, .04, 1.0), (.08, -.05, .8), (.05, .085, .7)):
        m.add('Slimes', 'slime', sphere(.022 * s, 10, 6), pos=(x, .095, z), scl=(1, .8, 1))
        for ex in (-.008, .008):
            m.add('Slimes', 'eye', sphere(.004 * s, 6, 4), pos=(x + ex * s, .105, z + .018 * s))
    for i in range(6): leaf(m, (R.uniform(-.1, .1), .092, R.uniform(-.1, .1)), R.uniform(0, 360), .6)
    return m

def roast():
    m = M('VFP_YuleRoast')
    m.add('Board', 'wood', cyl(.19, .025, 22))
    # Keule: Fleischkoerper + Knochen nach vorne rechts
    m.add('Roast', 'meat', sphere(.09, 14, 9), pos=(-.03, .085, -.01), scl=(1.35, .75, .95), rot=(0, 20, 0))
    m.add('Roast', 'meat_glaze', sphere(.07, 12, 7), pos=(-.04, .118, -.015), scl=(1.3, .45, .85), rot=(0, 20, 0))
    m.add('Bone', 'bone', cyl(.015, .075, 8), pos=(.135, .085, .035), rot=(0, 20, 90))
    m.add('Bone', 'bone', sphere(.022, 8, 5), pos=(.135, .085, .05))
    m.add('Bone', 'bone', sphere(.019, 8, 5), pos=(.135, .085, .025))
    for i in range(4):                                                      # Honig laeuft herunter
        a = -0.8 + i * .5
        m.add('Honey', 'honey', cyl(.007, .05, 6), pos=(-.03 + .1 * math.cos(a), .06, -.01 + .075 * math.sin(a)))
    m.add('Honey', 'honey', sphere(.035, 10, 6), pos=(-.05, .135, -.02), scl=(1.2, .25, .9))
    # drei Karotten vorne, aufeinander
    for (x, y, z, ay) in ((-.06, .045, .12, 80), (.0, .045, .13, 100), (-.03, .07, .125, 92)):
        m.add('Carrots', 'carrot', cone(.018, .11, 8), pos=(x, y, z), rot=(0, ay, 90))
        m.add('Carrots', 'leaf', cone(.01, .04, 5), pos=(x - .06 * math.cos(math.radians(ay - 90)), y, z), rot=(0, ay, -90))
    # Schale mit Karottenscheiben
    prof = [(0, 0), (.035, 0), (.05, .03), (.045, .03), (.03, .01), (0, .01)]
    m.add('Bowl', 'bowl_white', lathe(prof, 14), pos=(.11, .025, -.1))
    for i in range(6):
        a = 2 * math.pi * i / 6
        m.add('Bowl', 'carrot', cyl(.012, .006, 8), pos=(.11 + .02 * math.cos(a), .05, -.1 + .02 * math.sin(a)), rot=(R.uniform(-20, 20), 0, R.uniform(-20, 20)))
    # Mistelzweige
    for (x, z, a) in ((-.15, -.06, 30), (-.13, .05, -20), (.0, -.15, 80)):
        for k in range(3): leaf(m, (x + .015 * k * math.cos(math.radians(a)), .03, z + .015 * k * math.sin(math.radians(a))), a + k * 25, 1.1)
        m.add('Mistletoe', 'mistle_berry', sphere(.008, 6, 4), pos=(x, .04, z))
        m.add('Mistletoe', 'mistle_berry', sphere(.007, 6, 4), pos=(x + .01, .04, z + .006))
    return m

def grave_shroom():
    m = M('VFP_Secret_GraveShroom')
    m.add('Under', 'shroom_under', sphere(.14, 18, 10), pos=(0, .06, 0), scl=(1, .45, 1))
    m.add('Cap', 'shroom', torus(.11, .035, 22, 8), pos=(0, .085, 0))
    m.add('Cap', 'shroom_under', cyl(.11, .01, 20), pos=(0, .07, 0))
    for i in range(130):                                     # Beeren-Fleisch-Haufen, hoch ueber den Rand
        a = R.random() * 2 * math.pi; r = math.sqrt(R.random()) * .1
        y = .09 + .13 * (1 - (r / .1) ** 2) ** .7
        if i % 5 == 0: m.add('Meat', 'meat', box((.028, .02, .024)), pos=(r * math.cos(a), y, r * math.sin(a)), rot=(R.uniform(0, 40), R.uniform(0, 90), 0))
        else: m.add('Berries', 'blue', sphere(.014, 8, 5), pos=(r * math.cos(a), y, r * math.sin(a)))
    leaf(m, (.01, .235, 0), 30, 1.2); leaf(m, (-.02, .235, .01), 120, 1.1)
    return m

def blood_sausage():
    m = M('VFP_Secret_BogBloodSausage')
    pts = [(-.15, .05, -.02), (-.05, .06, .03), (.06, .06, .03), (.15, .05, -.03)]
    m.add('Sausage', 'sausage', tube(pts, [.045, .05, .05, .04], seg=12, smooth=4))
    m.add('Cut', 'sausage_cut', cyl(.044, .004, 14), pos=(-.152, .05, -.02), rot=(0, 0, 90))
    for i in range(9):
        a = R.random() * 2 * math.pi; r = R.random() * .03
        m.add('Cut', 'fat', box((.006, .008, .008)), pos=(-.156, .05 + r * math.sin(a), -.02 + r * math.cos(a)))
    m.add('Tie', 'string_red', torus(.02, .005, 10, 5), pos=(.17, .05, -.03), rot=(0, 0, 90))
    m.add('Tie', 'string_red', tube([(.18, .05, -.03), (.21, .02, -.01), (.2, .0, .02)], [.004, .004, .004], seg=5, smooth=2))
    for (a, b) in (((0, .095, 0), (0, .108, .0)),):
        pass
    # eingebrannte Sumpf-Rune oben (toter Baum)
    for (p0, p1) in (((0, -.025), (0, .02)), ((0, .005), (-.015, .02)), ((0, .0), (.014, .018)), ((0, -.025), (-.01, -.03)), ((0, -.025), (.01, -.03))):
        mid = ((p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2); L = math.hypot(p1[0] - p0[0], p1[1] - p0[1]); ang = math.degrees(math.atan2(p1[0] - p0[0], p1[1] - p0[1]))
        m.add('Rune', 'rune_green', box((.007, .003, L * 1.6)), pos=(mid[0] * 1.6, .108, mid[1] * 1.6 + .02), rot=(0, ang, 0))
    return m

def roulade():
    m = M('VFP_Secret_FrostWolfRoulade')
    m.add('Sauce', 'sauce', cyl(.15, .006, 20), scl=(1.2, 1, .8))
    m.add('Roll', 'roulade', cyl(.06, .2, 16), pos=(.1, .066, 0), rot=(0, 0, 90))
    for x in (-.04, .01, .06): m.add('String', 'string', torus(.061, .004, 16, 4), pos=(x, .066, 0), rot=(0, 0, 90))
    m.add('Face', 'roulade_cut', cyl(.058, .004, 16), pos=(-.101, .066, 0), rot=(0, 0, 90))
    for r in (.045, .03, .015): m.add('Spiral', 'roulade', torus(r, .003, 16, 4), pos=(-.104, .066, 0), rot=(0, 0, 90))
    m.add('Spiral', 'pickle', torus(.038, .002, 16, 4), pos=(-.105, .066, 0), rot=(0, 0, 90))
    m.add('Slice', 'roulade', cyl(.055, .025, 16), pos=(-.15, .006, .07))
    m.add('Slice', 'roulade_cut', cyl(.052, .003, 16), pos=(-.15, .031, .07))
    for r in (.04, .025, .012): m.add('Slice', 'roulade', torus(r, .003, 16, 4), pos=(-.15, .034, .07))
    for i in range(40):                                       # Raureif oben auf der Rolle
        x = R.uniform(-.09, .1); a = R.uniform(-.9, .9)
        m.add('Frost', 'frost', sphere(.006, 5, 3), pos=(x, .066 + .06 * math.cos(a), .06 * math.sin(a)))
    for (x, z) in ((.0, .05), (.08, -.06), (-.06, -.05)):
        m.add('Frost', 'frost', cone(.012, .03, 4), pos=(x, .13, z))
    return m

def fried_egg():
    m = M('VFP_Secret_FulingFriedEgg')
    prof = [(0, 0)] + [(.13 + .012 * math.sin(i * 1.7), 0) for i in range(1)] + [(.13, .008), (0, .012)]
    m.add('White', 'egg_white', jitter(cyl(.13, .012, 18), .008, 3), scl=(1.1, 1, .9))
    m.add('Yolk', 'yolk', sphere(.045, 14, 8), pos=(.01, .012, -.005), scl=(1, .55, 1))
    for z, ph in ((-.062, 0), (.068, 1.5)):
        pts = [(-.07 + .035 * k, .02 + .006 * math.sin(k * 1.6 + ph), z) for k in range(5)]
        m.add('Bacon', 'bacon', tube(pts, [.012] * 5, seg=4, smooth=3), scl=(1, .45, 2.2), pos=(0, .012, 0))
        m.add('Bacon', 'bacon_fat', tube([(x, y + .004, z) for x, y, z in pts], [.005] * 5, seg=4, smooth=3), scl=(1, .5, 2.2), pos=(0, .012, 0))
    return m

def pilzlaib():
    m = M('VFP_Secret_DvergrMushroomLoaf')
    cut = .07; ax, ay, az = .16, .11, .14
    V, F = dome(1.0, 1.0, 22, 9, .0)
    V = np.array(V, float) * np.array([ax, ay, az])
    V[:, 0] = np.minimum(V[:, 0], cut)                      # angeschnitten: flache Seite bei x = cut
    m.add('Loaf', 'crust', (V, F), pos=(-.04, 0, 0))
    # Krumen-Schnittflaeche (halbe Ellipse) knapp vor der Schnittkante
    k = math.sqrt(1 - (cut / ax) ** 2); hz, hy = az * k * .96, ay * k * .96
    fv = [(0, .004, 0)] + [(0, .004 + hy * math.sin(t), hz * math.cos(t)) for t in np.linspace(0, math.pi, 20)]
    ff = [(0, i, i + 1) for i in range(1, 20)]
    m.add('Face', 'crumb', (fv, ff), pos=(cut - .04 + .002, 0, 0))
    m.add('Face', 'crumb', (fv, [(a, c, b) for a, b, c in ff]), pos=(cut - .04 + .002, 0, 0))
    for i in range(10):
        t = R.uniform(.15, 2.99); r = R.uniform(.1, .8)
        m.add('Mage', 'magecap', sphere(.008, 6, 4), pos=(cut - .04 + .006, .004 + hy * math.sin(t) * r, hz * math.cos(t) * r))
    # zwei Scheiben, angelehnt
    for x, z, lean in ((.1, .06, 20), (.13, .0, 28)):
        sv = [(0, 0, 0)] + [(0, .075 * math.sin(t), .08 * math.cos(t)) for t in np.linspace(0, math.pi, 16)]
        sf = [(0, i, i + 1) for i in range(1, 16)]
        prim = (sv, sf + [(a, c, b) for a, b, c in sf])
        m.add('Slice', 'crumb', prim, pos=(x, .0, z), rot=(0, 0, -lean))
        m.add('Slice', 'crust', tube([(x, .0, z + .08), (x + .02 * math.sin(math.radians(lean)), .06, z + .045), (x + .03 * math.sin(math.radians(lean)), .075, z), (x + .02 * math.sin(math.radians(lean)), .06, z - .045), (x, .0, z - .08)], [.006] * 5, seg=4, smooth=3))
        m.add('Slice', 'magecap', sphere(.008, 6, 4), pos=(x + .004, .035, z + .01))
    for i in range(3): m.add('Cuts', 'crumb', box((.1, .005, .01)), pos=(-.1 + i * .035, .095 - abs(i - 1) * .01, -.03 + i * .03), rot=(0, 35, 0))
    return m

def sulphur_skewer():
    m = M('VFP_Secret_SulphurSkewer')
    m.add('Stick', 'stick', cyl(.006, .42, 6), pos=(-.21, .05, 0), rot=(0, 0, -90))
    for i in range(4):
        x = -.13 + i * .085
        m.add('Meat', 'asksvin', jitter(box((.06, .055, .055)), .006, i), pos=(x, .05, 0), rot=(R.uniform(-20, 20), R.uniform(0, 45), 0))
        for k in range(18):
            m.add('Sulfur', 'sulfur', box((.01, .01, .01)), pos=(x + R.uniform(-.03, .03), .075 + R.uniform(0, .01), R.uniform(-.03, .03)), rot=(R.uniform(0, 90), R.uniform(0, 90), 0))
        if i < 3:
            for (dy, dz) in ((0, .0), (.012, .01), (-.01, .012)): m.add('Berries', 'vineberry', sphere(.011, 6, 4), pos=(x + .042, .05 + dy, dz))
    for x in (-.09, .08): m.add('Fern', 'fern', torus(.014, .004, 10, 4), pos=(x, .095, .02), rot=(90, 0, 0))
    return m

def pancake_tower():
    m = M('VFP_Secret_YmirPancakeTower')
    m.add('Plate', 'plate', lathe([(0, 0), (.13, 0), (.17, .02), (.165, .025), (.12, .008), (0, .008)], 22))
    for k in range(5):
        y = .01 + k * .022
        m.add('Pancakes', 'pancake_side', jitter(cyl(.1, .016, 20), .005, k), pos=(0, y, 0))
        m.add('Pancakes', 'pancake', cyl(.097, .004, 20), pos=(0, y + .016, 0))
    top = .01 + 5 * .022
    m.add('Cream', 'cream', sphere(.04, 12, 7), pos=(0, top + .01, 0), scl=(1, .6, 1))
    m.add('Cream', 'cream', sphere(.025, 10, 6), pos=(0, top + .03, 0), scl=(1, .7, 1))
    for i in range(14):
        a = R.random() * 2 * math.pi; r = R.uniform(.045, .085)
        m.add('Berries', 'lingon', sphere(.009, 6, 4), pos=(r * math.cos(a), top + .006, r * math.sin(a)))
    for a in (.4, 1.9, 3.6, 5.0):
        m.add('Sauce', 'lingon', cyl(.007, .05, 6), pos=(.1 * math.cos(a), top - .05, .1 * math.sin(a)))
    leaf(m, (.01, top + .055, 0), 40, .9); leaf(m, (-.012, top + .055, .005), 140, .8)
    return m

SECRETS = [grave_shroom, blood_sausage, roulade, fried_egg, pilzlaib, sulphur_skewer, pancake_tower]

if __name__ == '__main__':
    if '--secrets' in sys.argv:
        tiles = []
        for f in SECRETS:
            mdl = f(); n = mdl.export(OUT); print('%-32s %5d Dreiecke' % (mdl.name, n))
            tiles.append(render(mdl, (-55 if 'Loaf' in mdl.name else 25), -30, size=300, center=(0, .08, 0), dist=5.2, zoom=9.5))
        im = Image.new('RGB', (300 * 4, 600), (14, 20, 22))
        for i, t in enumerate(tiles): im.paste(t, ((i % 4) * 300 + (150 if i >= 4 else 0), (i // 4) * 300))
        im.save(os.path.join(HERE, 'preview_secrets.png')); sys.exit(0)

    ms = [cake(), soup(), roast()]
    tiles = []
    for mdl in ms:
        n = mdl.export(OUT); print('%-20s %5d Dreiecke' % (mdl.name, n))
        tiles.append(render(mdl, 25, -30, size=360, center=(0, .1, 0), dist=5.2, zoom=9.0))
    im = Image.new('RGB', (360 * len(tiles), 360))
    for i, t in enumerate(tiles): im.paste(t, (i * 360, 0))
    im.save(os.path.join(HERE, 'preview_dishes.png'))
