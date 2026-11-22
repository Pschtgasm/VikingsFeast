"""3D-Modelle fuer die 24 normalen Gerichte (Objektstaender/Boden), nach den Icons. Ursprung Mitte unten, Y oben.
Aufruf: python make_foods.py   -> Models/VFP_Food_<Id>.obj + preview_foods.png"""
import math, os, random, sys
import numpy as np
from PIL import Image
from meshlib import *
import make_dishes as D
HERE = D.HERE; OUT = D.OUT
EXTRA = {
    'soup_yellow': ((0.86, 0.70, 0.30), (0, 0, 0)), 'soup_brown': ((0.30, 0.18, 0.10), (0, 0, 0)), 'soup_red': ((0.48, 0.08, 0.08), (0, 0, 0)),
    'soup_magenta': ((0.62, 0.15, 0.42), (0.10, 0.02, 0.06)), 'soup_purple': ((0.45, 0.22, 0.62), (0.12, 0.05, 0.18)), 'soup_orange': ((0.72, 0.30, 0.10), (0.06, 0.02, 0)),
    'soup_green': ((0.26, 0.32, 0.12), (0, 0, 0)), 'soup_cream': ((0.88, 0.82, 0.68), (0, 0, 0)), 'soup_gold': ((0.80, 0.55, 0.22), (0.05, 0.03, 0)),
    'mash_red': ((0.55, 0.06, 0.16), (0, 0, 0)), 'black_berry': ((0.10, 0.06, 0.12), (0, 0, 0)), 'cloud': ((0.92, 0.60, 0.28), (0, 0, 0)),
    'mush_yellow': ((0.90, 0.68, 0.22), (0, 0, 0)), 'mush_purple': ((0.48, 0.24, 0.70), (0.14, 0.06, 0.22)), 'mush_blue': ((0.40, 0.70, 0.95), (0.15, 0.30, 0.45)),
    'stem': ((0.86, 0.82, 0.70), (0, 0, 0)), 'seaweed': ((0.20, 0.30, 0.10), (0, 0, 0)), 'grain': ((0.82, 0.66, 0.36), (0, 0, 0)),
    'crystal_blue': ((0.45, 0.80, 0.95), (0.15, 0.30, 0.40)), 'crystal_red': ((0.75, 0.12, 0.25), (0.20, 0.03, 0.06)), 'ice': ((0.82, 0.90, 0.96), (0.06, 0.10, 0.14)),
    'boar': ((0.36, 0.22, 0.14), (0, 0, 0)), 'tusk': ((0.90, 0.86, 0.74), (0, 0, 0)), 'wolf': ((0.55, 0.55, 0.58), (0, 0, 0)), 'wolf_dark': ((0.25, 0.25, 0.28), (0, 0, 0)),
    'onion': ((0.80, 0.62, 0.40), (0, 0, 0)), 'onion_red': ((0.62, 0.30, 0.38), (0, 0, 0)), 'fish': ((0.70, 0.48, 0.24), (0, 0, 0)), 'fish_blue': ((0.38, 0.55, 0.78), (0, 0, 0)),
    'fish_belly': ((0.88, 0.80, 0.62), (0, 0, 0)), 'iron': ((0.16, 0.16, 0.17), (0, 0, 0)), 'puffer': ((0.78, 0.55, 0.30), (0, 0, 0)), 'angler': ((0.16, 0.14, 0.12), (0, 0, 0)),
    'lure': ((1.0, 0.85, 0.40), (0.80, 0.60, 0.20)), 'teeth': ((0.95, 0.92, 0.85), (0, 0, 0)), 'asksvin_roast': ((0.42, 0.14, 0.06), (0.04, 0.0, 0)),
    'frog': ((0.22, 0.30, 0.18), (0, 0, 0)), 'feather': ((0.45, 0.62, 0.90), (0.05, 0.08, 0.15)), 'potion': ((0.40, 0.70, 0.95), (0.20, 0.35, 0.50)), 'cork': ((0.55, 0.38, 0.20), (0, 0, 0)),
    'kale': ((0.25, 0.42, 0.22), (0, 0, 0)), 'tick': ((0.38, 0.06, 0.06), (0, 0, 0)), 'flatbread': ((0.82, 0.60, 0.30), (0, 0, 0)), 'bun': ((0.72, 0.45, 0.18), (0, 0, 0)),
    'patty': ((0.28, 0.14, 0.08), (0, 0, 0)), 'seed': ((0.92, 0.86, 0.66), (0, 0, 0)), 'jar': ((0.85, 0.62, 0.25), (0.10, 0.06, 0)), 'oat': ((0.86, 0.80, 0.66), (0, 0, 0)),
}
D.MATS.update(EXTRA)
R = random.Random(11)

# ---------------------------------------------------------------- Bausteine
def bowl(m, r=.16, h=.11, band=True):
    prof = [(0, 0), (r * .55, 0), (r * .85, h * .3), (r, h * .8), (r * 1.02, h), (r * .93, h), (r * .82, h * .55), (r * .5, h * .2), (0, h * .2)]
    m.add('Bowl', 'wood', lathe(prof, 22))
    if band: m.add('Bowl', 'wood_dark', torus(r * .93, .006, 22, 4), pos=(0, h * .55, 0))
    return h * .86          # Hoehe der Fuellung
def surface(m, mat, y, r=.145):
    m.add('Fill', mat, cyl(r, .006, 22), pos=(0, y - .006, 0))
def board_round(m, r=.18): m.add('Board', 'wood', cyl(r, .022, 22)); return .022
def board_rect(m, w=.34, d=.24):
    m.add('Board', 'wood', box((w, .022, d)), pos=(0, .011, 0)); return .022
def pan(m, r=.15):
    m.add('Pan', 'iron', lathe([(0, 0), (r * .9, 0), (r, .04), (r * .96, .04), (r * .86, .008), (0, .008)], 22))
    m.add('Pan', 'wood_dark', cyl(.012, .14, 8), pos=(r, .035, 0), rot=(0, 0, -80))
    return .01
def scatter(m, part, mat, prim_fn, n, y, rad, ymax=0.0):
    for i in range(n):
        a = R.random() * 2 * math.pi; r = math.sqrt(R.random()) * rad
        m.add(part, mat, prim_fn(), pos=(r * math.cos(a), y + R.random() * ymax, r * math.sin(a)), rot=(R.uniform(0, 60), R.uniform(0, 360), 0))
def berries(m, y, rad, n, mats=('rasp', 'blue', 'black_berry'), s=.014):
    for i in range(n):
        a = R.random() * 2 * math.pi; r = math.sqrt(R.random()) * rad
        m.add('Berries', mats[i % len(mats)], sphere(s, 8, 5), pos=(r * math.cos(a), y + s * .6, r * math.sin(a)))
def mushrooms(m, y, rad, n, cap='mush_yellow', s=1.0):
    for i in range(n):
        a = R.random() * 2 * math.pi; r = math.sqrt(R.random()) * rad; x, z = r * math.cos(a), r * math.sin(a)
        m.add('Mush', 'stem', cyl(.008 * s, .025 * s, 6), pos=(x, y, z))
        m.add('Mush', cap, sphere(.022 * s, 10, 5), pos=(x, y + .025 * s, z), scl=(1, .55, 1))
def herbs(m, y, rad, n):
    for i in range(n):
        a = R.random() * 2 * math.pi; r = math.sqrt(R.random()) * rad
        D.leaf(m, (r * math.cos(a), y + .004, r * math.sin(a)), R.uniform(0, 360), .7)
def chunks(m, y, rad, n, mat='meat', s=.03):
    scatter(m, 'Chunks', mat, lambda: jitter(box((s, s * .8, s * .9)), s * .15, R.randint(0, 99)), n, y, rad, .01)
def fish(m, pos, yaw, mat='fish', k=1.0):
    x, y, z = pos
    m.add('Fish', mat, sphere(.06 * k, 14, 8), pos=(x, y, z), rot=(0, yaw, 0), scl=(2.0, .55, .7))
    ca, sa = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    tx, tz = x - .13 * k * ca, z + .13 * k * sa
    m.add('Fish', mat, cone(.045 * k, .07 * k, 4), pos=(tx, y, tz), rot=(0, yaw, 90), scl=(1, 1, .25))
    m.add('Fish', 'fish_belly', sphere(.05 * k, 10, 6), pos=(x, y - .012 * k, z), rot=(0, yaw, 0), scl=(1.8, .3, .6))
    m.add('Fish', 'eye', sphere(.006 * k, 6, 4), pos=(x + .09 * k * ca, y + .012 * k, z - .09 * k * sa + .018 * k))
def ice_rim(m, r, y, n=10):
    for i in range(n):
        a = 2 * math.pi * i / n
        m.add('Ice', 'ice', cone(.01, .04, 5), pos=(r * math.cos(a), y - .035, r * math.sin(a)), rot=(180, 0, 0))
def crystals(m, y, rad, n, mat='crystal_blue', s=1.0):
    for i in range(n):
        a = R.random() * 2 * math.pi; r = math.sqrt(R.random()) * rad
        m.add('Crystals', mat, cone(.02 * s, .06 * s, 5), pos=(r * math.cos(a), y, r * math.sin(a)), rot=(R.uniform(-25, 25), 0, R.uniform(-25, 25)))

def new(name):
    m = D.M('VFP_Food_' + name)
    return m

# ---------------------------------------------------------------- Gerichte
def BoarHerbRoast():
    m = new('BoarHerbRoast'); y = bowl(m, .17, .07); surface(m, 'soup_brown', y)
    m.add('Head', 'boar', sphere(.08, 14, 9), pos=(-.01, y + .045, 0), scl=(1.2, .8, .9))
    m.add('Head', 'boar', cyl(.035, .06, 10), pos=(.08, y + .035, 0), rot=(0, 0, -90))
    m.add('Head', 'soup_brown', cyl(.03, .004, 10), pos=(.14, y + .035, 0), rot=(0, 0, -90))
    for z in (-.035, .035):
        m.add('Head', 'tusk', cone(.009, .05, 6), pos=(.11, y + .02, z), rot=(0, 0, -50))
        m.add('Head', 'boar', cone(.025, .045, 5), pos=(-.04, y + .1, z * 1.3), rot=(z * 500, 0, 15))
        m.add('Head', 'eye', sphere(.007, 6, 4), pos=(.06, y + .065, z * .7))
    for a in (2.2, 3.6, 4.4): m.add('Flowers', 'dandelion', sphere(.022, 8, 4), pos=(.12 * math.cos(a), y + .01, .12 * math.sin(a)), scl=(1, .35, 1))
    return m
def BerryMash():
    m = new('BerryMash'); y = bowl(m); surface(m, 'mash_red', y); berries(m, y, .12, 26, s=.016); return m
def YellowMushroomSoup():
    m = new('YellowMushroomSoup'); y = bowl(m); surface(m, 'soup_yellow', y); mushrooms(m, y - .012, .11, 8); herbs(m, y, .12, 6); return m
def DraugrStew():
    m = new('DraugrStew'); y = bowl(m); surface(m, 'soup_brown', y); chunks(m, y - .01, .11, 12)
    for i in range(6):
        a = R.random() * 6.28; r = R.uniform(.03, .12)
        m.add('Seaweed', 'seaweed', tube([(r * math.cos(a), y, r * math.sin(a)), (r * math.cos(a) + .02, y + .006, r * math.sin(a) + .02), (r * math.cos(a) + .04, y, r * math.sin(a))], [.005] * 3, seg=4, smooth=2))
    return m
def BloodThistleStew():
    m = new('BloodThistleStew'); y = bowl(m); surface(m, 'soup_red', y); chunks(m, y - .008, .11, 10, 'meat_glaze')
    scatter(m, 'Strips', 'mash_red', lambda: box((.05, .006, .015)), 14, y, .11, .006); return m
def WolfHerbRoast():
    m = new('WolfHerbRoast'); y = bowl(m, .17, .08); surface(m, 'soup_brown', y)
    m.add('Head', 'wolf', sphere(.075, 14, 9), pos=(-.02, y + .05, 0), scl=(1.1, .85, .9))
    m.add('Head', 'wolf', cone(.045, .1, 10), pos=(.03, y + .04, 0), rot=(0, 0, -90))
    m.add('Head', 'wolf_dark', sphere(.012, 6, 4), pos=(.13, y + .04, 0))
    for z in (-.04, .04):
        m.add('Head', 'wolf', cone(.025, .06, 4), pos=(-.05, y + .1, z), rot=(z * 400, 0, 10))
        m.add('Head', 'lure', sphere(.007, 6, 4), pos=(.05, y + .07, z * .6))
        m.add('Head', 'teeth', cone(.004, .015, 4), pos=(.09, y + .02, z * .4), rot=(180, 0, 0))
    for a in (1.0, 5.2): m.add('Onions', 'onion', sphere(.03, 10, 6), pos=(.1 * math.cos(a), y + .02, .1 * math.sin(a)))
    return m
def WolfThistleStew():
    m = new('WolfThistleStew'); y = bowl(m); surface(m, 'soup_magenta', y); crystals(m, y - .005, .1, 5, s=1.3)
    scatter(m, 'Chunks', 'onion_red', lambda: torus(.02, .005, 10, 4), 6, y, .11); ice_rim(m, .16, .11); return m
def GoldenBarleyPorridge():
    m = new('GoldenBarleyPorridge'); y = bowl(m); surface(m, 'soup_gold', y)
    scatter(m, 'Grain', 'grain', lambda: sphere(.008, 6, 4), 50, y, .13, .004)
    for (x, z) in ((-.03, .01), (.0, -.02), (.02, .02), (-.01, -.04), (.035, -.01)): m.add('Berries', 'cloud', sphere(.017, 8, 5), pos=(x - .03, y + .012, z))
    m.add('Dipper', 'wood_dark', cyl(.007, .2, 6), pos=(.03, y, .0), rot=(0, 0, -45))
    m.add('Dipper', 'honey', sphere(.025, 10, 6), pos=(.05, y + .01, 0), scl=(1, .8, 1)); herbs(m, y + .02, .03, 3); return m
def MagicMushroomCream():
    m = new('MagicMushroomCream'); y = bowl(m); surface(m, 'soup_purple', y); mushrooms(m, y - .01, .1, 6, 'mush_purple', 1.4); return m
def AsksvinFireRoast():
    m = new('AsksvinFireRoast'); y = board_rect(m)
    m.add('Roast', 'asksvin_roast', sphere(.09, 14, 9), pos=(0, y + .045, 0), scl=(1.5, .65, .8))
    for i in range(7): m.add('Roast', 'meat_glaze', cone(.02, .06, 4), pos=(-.09 + i * .03, y + .08, 0), rot=(0, 0, -20 + i * 6))
    for (x, z) in ((-.12, .08), (.12, -.08)):
        for k in range(5): m.add('Berries', 'vineberry', sphere(.014, 8, 5), pos=(x + (k % 3) * .015, y + .014 + (k // 3) * .014, z + (k % 2) * .012))
    herbs(m, y, .15, 5); return m
def EmberHerbSoup():
    m = new('EmberHerbSoup'); y = bowl(m); surface(m, 'soup_orange', y); mushrooms(m, y - .01, .1, 4, 'mush_purple', 1.4)
    berries(m, y, .12, 10, mats=('rasp',)); return m
def HerbFish():
    m = new('HerbFish'); y = board_round(m); fish(m, (0, y + .03, 0), 10); herbs(m, y + .05, .08, 6); berries(m, y, .16, 8, mats=('rasp',)); return m
def ForestFishPan():
    m = new('ForestFishPan'); y = pan(m); fish(m, (-.01, y + .03, 0), 0, k=.9); mushrooms(m, y, .12, 7); berries(m, y, .12, 8, mats=('blue',)); return m
def SwampFishStew():
    m = new('SwampFishStew'); y = bowl(m); surface(m, 'soup_green', y); chunks(m, y - .008, .1, 7, 'fish_belly', .035)
    scatter(m, 'Seaweed', 'seaweed', lambda: box((.06, .004, .012)), 10, y, .11, .004); return m
def FrostFishRoast():
    m = new('FrostFishRoast'); y = bowl(m, .17, .07); surface(m, 'soup_cream', y); fish(m, (0, y + .03, 0), 15, 'fish_blue')
    for a in (2.0, 3.3, 4.5): m.add('Onions', 'onion', sphere(.026, 10, 6), pos=(.11 * math.cos(a), y + .015, .11 * math.sin(a)))
    scatter(m, 'Frost', 'frost', lambda: sphere(.005, 4, 3), 30, y + .04, .12, .02); return m
def PufferBarleyPan():
    m = new('PufferBarleyPan'); y = pan(m); scatter(m, 'Grain', 'grain', lambda: sphere(.008, 6, 4), 60, y + .01, .13, .006)
    m.add('Puffer', 'puffer', sphere(.06, 14, 9), pos=(0, y + .06, 0))
    for i in range(26):
        a = R.random() * 6.28; b = R.uniform(-.2, 1.2)
        d = (math.cos(b) * math.cos(a), math.sin(b), math.cos(b) * math.sin(a))
        m.add('Puffer', 'tusk', cone(.005, .02, 4), pos=(d[0] * .058, y + .06 + d[1] * .058, d[2] * .058), rot=look_rot(d))
    m.add('Puffer', 'eye', sphere(.008, 6, 4), pos=(.05, y + .075, .025)); berries(m, y + .01, .13, 8, mats=('rasp',)); return m
def AnglerMistSoup():
    m = new('AnglerMistSoup'); y = bowl(m); surface(m, 'soup_purple', y)
    m.add('Angler', 'angler', sphere(.07, 14, 9), pos=(0, y + .03, 0), scl=(1.1, .8, 1))
    for i in range(7): m.add('Angler', 'teeth', cone(.006, .02, 4), pos=(.05, y + .02, -.04 + i * .013), rot=(0, 0, 180))
    m.add('Angler', 'angler', tube([(-.01, y + .08, 0), (.02, y + .13, 0), (.06, y + .12, 0)], [.005, .004, .003], seg=4, smooth=2))
    m.add('Angler', 'lure', sphere(.014, 8, 5), pos=(.065, y + .115, 0))
    for z in (-.03, .03): m.add('Angler', 'lure', sphere(.008, 6, 4), pos=(.045, y + .06, z))
    crystals(m, y, .13, 4, 'crystal_blue', .9); return m
def RoyalMistRagout():
    m = new('RoyalMistRagout'); y = bowl(m); surface(m, 'soup_gold', y); chunks(m, y - .006, .1, 7, 'meat_glaze', .04)
    mushrooms(m, y - .006, .1, 3, 'mush_blue', 1.2)
    m.add('Jar', 'jar', cyl(.035, .08, 12), pos=(.15, 0, -.12)); m.add('Jar', 'cork', cyl(.025, .015, 10), pos=(.15, .08, -.12)); return m
def AsksvinEmberFeast():
    m = new('AsksvinEmberFeast'); y = bowl(m, .17, .08); surface(m, 'soup_brown', y)
    m.add('Creature', 'frog', sphere(.07, 14, 9), pos=(-.01, y + .035, 0), scl=(1.2, .75, 1))
    for z in (-.03, .03): m.add('Creature', 'lure', sphere(.012, 6, 4), pos=(.06, y + .07, z))
    crystals(m, y, .13, 6, 'crystal_red', 1.0)
    m.add('Feather', 'feather', sphere(.05, 10, 6), pos=(.05, y + .02, .1), rot=(0, 30, 10), scl=(2.2, .15, .5)); return m
def EternalFrostEssence():
    m = new('EternalFrostEssence'); y = board_round(m)
    m.add('Flask', 'potion', sphere(.07, 14, 9), pos=(-.02, y + .07, 0))
    m.add('Flask', 'potion', cyl(.02, .05, 10), pos=(-.02, y + .13, 0)); m.add('Flask', 'cork', cyl(.023, .02, 10), pos=(-.02, y + .175, 0))
    crystals(m, y, .16, 6, 'ice', 1.1)
    for k in range(4): D.leaf(m, (.09 + .02 * k, y + .02, .06 - .03 * k), 40 * k, 1.6)
    berries(m, y, .16, 9, mats=('rasp',)); return m
def TickSud():
    m = new('TickSud'); y = board_rect(m)
    m.add('Sauce', 'mash_red', cyl(.12, .004, 18), pos=(0, y, 0), scl=(1.2, 1, .9))
    m.add('Tick', 'tick', sphere(.06, 14, 9), pos=(0, y + .04, 0), scl=(1.2, .7, 1))
    m.add('Tick', 'angler', sphere(.025, 10, 6), pos=(.07, y + .03, 0))
    for side in (-1, 1):
        for i in range(4):
            x = -.04 + i * .03
            m.add('Tick', 'angler', tube([(x, y + .03, side * .045), (x + .01, y + .05, side * .08), (x + .02, y + .005, side * .11)], [.004] * 3, seg=4, smooth=2))
    berries(m, y, .14, 8, mats=('rasp',)); return m
def HeartyFlatbread():
    m = new('HeartyFlatbread'); y = board_round(m)
    m.add('Bread', 'flatbread', sphere(.13, 16, 6), pos=(0, y + .01, 0), scl=(1.1, .14, .9))
    for i in range(5): m.add('Bread', 'patty', box((.04, .004, .008)), pos=(-.06 + i * .03, y + .029, -.02 + (i % 2) * .03), rot=(0, 30, 0))
    chunks(m, y + .025, .08, 6, 'meat', .02)
    m.add('Onion', 'onion_red', sphere(.03, 10, 6), pos=(.13, y + .03, .08)); return m
def MooseBurger():
    m = new('MooseBurger'); y = board_round(m)
    m.add('Burger', 'bun', cyl(.09, .025, 18), pos=(0, y, 0)); m.add('Burger', 'patty', cyl(.095, .03, 18), pos=(0, y + .025, 0))
    m.add('Burger', 'lingon', cyl(.092, .008, 18), pos=(0, y + .055, 0)); m.add('Burger', 'onion_red', torus(.06, .006, 14, 4), pos=(0, y + .066, 0))
    m.add('Burger', 'bun', dome(.09, .06, 18, 6, .0), pos=(0, y + .066, 0))
    for i in range(12):
        a = R.random() * 6.28; r = R.uniform(.01, .07)
        m.add('Burger', 'seed', sphere(.005, 4, 3), pos=(r * math.cos(a), y + .066 + .06 * math.sqrt(max(0, 1 - (r / .09) ** 2)), r * math.sin(a)), scl=(1.5, .6, 1))
    berries(m, y, .16, 6, mats=('lingon',)); return m
def NorthernOatPorridge():
    m = new('NorthernOatPorridge'); y = bowl(m); surface(m, 'oat', y)
    scatter(m, 'Grain', 'seed', lambda: sphere(.007, 5, 3), 40, y, .13, .003)
    berries(m, y, .1, 12, mats=('lingon', 'blue')); ice_rim(m, .16, .11, 8)
    m.add('Spoon', 'wood_dark', cyl(.008, .2, 6), pos=(.06, y, .02), rot=(0, 20, -50)); return m

ALL = [BoarHerbRoast, BerryMash, YellowMushroomSoup, DraugrStew, BloodThistleStew, WolfHerbRoast, WolfThistleStew, GoldenBarleyPorridge,
       MagicMushroomCream, AsksvinFireRoast, EmberHerbSoup, HerbFish, ForestFishPan, SwampFishStew, FrostFishRoast, PufferBarleyPan,
       AnglerMistSoup, RoyalMistRagout, AsksvinEmberFeast, EternalFrostEssence, TickSud, HeartyFlatbread, MooseBurger, NorthernOatPorridge]
if __name__ == '__main__':
    T = 220; tiles = []
    for f in ALL:
        mdl = f(); n = mdl.export(OUT); print('%-34s %5d' % (mdl.name, n))
        tiles.append(render(mdl, 25, -32, size=T, center=(0, .08, 0), dist=5.2, zoom=9.0))
    im = Image.new('RGB', (T * 6, T * 4))
    for i, t in enumerate(tiles): im.paste(t, ((i % 6) * T, (i // 6) * T))
    im.save(os.path.join(HERE, 'preview_foods.png'))
