"""Erzeugt Models/VFP_MeadFlask.obj/.mtl: runde Met-Flasche (Valheim-Stil) mit Korken, Schnur und Etikett.
Teile: Liquid, LiquidTop, Glass, Cork, Rope, Label, LabelRing. Liquid/Glass werden im Spiel je Met eingefaerbt."""
import math, os
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, '..', 'Models')
V = []; parts = {}
def v(x, y, z): V.append((x, y, z)); return len(V)
def tri(p, a, b, c): parts.setdefault(p, []).append((a, b, c))
def quad(p, a, b, c, d): tri(p, a, b, c); tri(p, a, c, d)
def lathe(p, prof, n=28):
    rows = [[v(r * math.cos(2 * math.pi * i / n), y, r * math.sin(2 * math.pi * i / n)) for i in range(n)] for r, y in prof]
    for k in range(len(rows) - 1):
        for i in range(n): quad(p, rows[k][i], rows[k][(i + 1) % n], rows[k + 1][(i + 1) % n], rows[k + 1][i])
R, CY, FILL = .46, .46, .62
belly = []
for k in range(17):
    th = math.pi * (1 - k / 16); belly.append((R * math.sin(th), CY - R * math.cos(th)))
liq = [p for p in belly if p[1] <= FILL] ; rl = math.sqrt(R * R - (FILL - CY) ** 2)
liq.append((rl, FILL))
lathe('Liquid', [(r * .985, y) for r, y in liq])
lathe('LiquidTop', [(0, FILL), (rl * .985, FILL)])
glass = [(rl, FILL)] + [p for p in belly if p[1] > FILL] + [(.16, .96), (.14, 1.04), (.15, 1.18), (.18, 1.22)]
lathe('Glass', glass)
lathe('Cork', [(.13, 1.16), (.14, 1.32), (.12, 1.38), (0, 1.39)], 16)
lathe('Rope', [(.175, 1.06), (.19, 1.08), (.19, 1.13), (.175, 1.15)], 16)
# Etikett vorne und hinten: Kugelkappe knapp ueber dem Bauch
def label(sign, part, r0, r1, off):
    n, m = 24, 4
    for j in range(m):
        a0, a1 = r0 + (r1 - r0) * j / m, r0 + (r1 - r0) * (j + 1) / m
        ring0 = []; ring1 = []
        for i in range(n):
            t = 2 * math.pi * i / n
            for a, ring in ((a0, ring0), (a1, ring1)):
                dx, dy = math.sin(a) * math.cos(t), math.sin(a) * math.sin(t); dz = math.cos(a)
                rr = R + off
                ring.append(v(dx * rr, CY - .04 + dy * rr, sign * dz * rr))
        for i in range(n):
            if sign > 0: quad(part, ring0[i], ring1[i], ring1[(i + 1) % n], ring0[(i + 1) % n])
            else: quad(part, ring0[i], ring0[(i + 1) % n], ring1[(i + 1) % n], ring1[i])
for s in (1, -1):
    label(s, 'Label', 0.0, .30, .006)
    label(s, 'LabelRing', .30, .36, .006)
mats = {'Liquid': ((.9, .6, .2), (.2, .1, .0)), 'LiquidTop': ((1, .75, .35), (.3, .15, .0)), 'Glass': ((.75, .82, .85), (0, 0, 0)),
        'Cork': ((.62, .45, .27), (0, 0, 0)), 'Rope': ((.78, .66, .44), (0, 0, 0)), 'Label': ((.93, .86, .68), (0, 0, 0)), 'LabelRing': ((.28, .16, .08), (0, 0, 0))}
with open(os.path.join(OUT, 'VFP_MeadFlask.mtl'), 'w') as f:
    for k, (kd, ke) in mats.items():
        f.write('newmtl %s\nKd %.3f %.3f %.3f\nKe %.3f %.3f %.3f\n\n' % ((k,) + kd + ke))
with open(os.path.join(OUT, 'VFP_MeadFlask.obj'), 'w') as f:
    f.write('mtllib VFP_MeadFlask.mtl\n')
    for x, y, z in V: f.write('v %.4f %.4f %.4f\n' % (x, y, z))
    for p, tris in parts.items():
        f.write('o %s\nusemtl %s\n' % (p, p))
        for a, b, c in tris: f.write('f %d %d %d\n' % (a, b, c))
print('VFP_MeadFlask', len(V), 'Punkte', sum(len(t) for t in parts.values()), 'Dreiecke')
