"""Erzeugt Models/VFP_Scroll.obj/.mtl: gerollte Rezeptrolle mit Band und Wachssiegel.
Teile: Paper, PaperEnd, Ribbon, Seal, SealMark. Seal/SealMark werden im Spiel je Biom eingefaerbt."""
import math, os
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, '..', 'Models')
V = []; parts = {}
def v(x, y, z): V.append((x, y, z)); return len(V)
def tri(p, a, b, c): parts.setdefault(p, []).append((a, b, c))
def quad(p, a, b, c, d): tri(p, a, b, c); tri(p, a, c, d)
R, L, N = .055, .44, 24
def ring(x, r, n=N, y0=R, ):
    return [v(x, y0 + r * math.cos(2 * math.pi * i / n), r * math.sin(2 * math.pi * i / n)) for i in range(n)]
# Papierrolle entlang X, liegt auf dem Boden (Mittelachse auf Hoehe R)
xs = [-L / 2 + L * k / 10 for k in range(11)]
rings = [ring(x, R) for x in xs]
for k in range(len(rings) - 1):
    for i in range(N): quad('Paper', rings[k][i], rings[k][(i + 1) % N], rings[k + 1][(i + 1) % N], rings[k + 1][i])
# Enden: eingerollte Spirale als konzentrische Ringe (leicht versetzt)
for sx in (-1, 1):
    x = sx * L / 2
    prev = ring(x, R)
    for k, rr in enumerate([R * .8, R * .6, R * .4, R * .2, 0.001]):
        cur = ring(x - sx * .004 * (k % 2), rr)
        part = 'PaperEnd' if k % 2 == 0 else 'Paper'
        for i in range(N):
            if sx > 0: quad(part, prev[i], prev[(i + 1) % N], cur[(i + 1) % N], cur[i])
            else: quad(part, prev[i], cur[i], cur[(i + 1) % N], prev[(i + 1) % N])
        prev = cur
# Band: zwei schmale Ringe links der Mitte
for bx in (-.07, -.035):
    a = ring(bx - .008, R * 1.06); b = ring(bx + .008, R * 1.06)
    for i in range(N): quad('Ribbon', a[i], a[(i + 1) % N], b[(i + 1) % N], b[i])
# Siegel: flacher Zylinder vorne (+z) in der Mitte, Rand gewellt
cx, cy, cz = .03, R, R * 1.02
n = 20; top = []; bot = []
for i in range(n):
    t = 2 * math.pi * i / n; rr = .045 * (1 + .08 * math.sin(6 * t))
    top.append(v(cx + rr * math.cos(t), cy + rr * math.sin(t), cz + .014))
    bot.append(v(cx + rr * math.cos(t), cy + rr * math.sin(t), cz - .004))
c = v(cx, cy, cz + .016)
for i in range(n):
    tri('Seal', c, top[i], top[(i + 1) % n]); quad('Seal', bot[i], bot[(i + 1) % n], top[(i + 1) % n], top[i])
# Praegung: kleiner erhabener Ring
mk0 = []; mk1 = []
for i in range(n):
    t = 2 * math.pi * i / n
    mk0.append(v(cx + .022 * math.cos(t), cy + .022 * math.sin(t), cz + .0165))
    mk1.append(v(cx + .030 * math.cos(t), cy + .030 * math.sin(t), cz + .0165))
for i in range(n): quad('SealMark', mk0[i], mk1[i], mk1[(i + 1) % n], mk0[(i + 1) % n])
mats = {'Paper': ((.86, .76, .56), (0, 0, 0)), 'PaperEnd': ((.66, .54, .36), (0, 0, 0)), 'Ribbon': ((.55, .13, .11), (0, 0, 0)),
        'Seal': ((.25, .5, .25), (0, 0, 0)), 'SealMark': ((.45, .75, .45), (0, 0, 0))}
with open(os.path.join(OUT, 'VFP_Scroll.mtl'), 'w') as f:
    for k, (kd, ke) in mats.items(): f.write('newmtl %s\nKd %.3f %.3f %.3f\nKe %.3f %.3f %.3f\n\n' % ((k,) + kd + ke))
with open(os.path.join(OUT, 'VFP_Scroll.obj'), 'w') as f:
    f.write('mtllib VFP_Scroll.mtl\n')
    for x, y, z in V: f.write('v %.4f %.4f %.4f\n' % (x, y, z))
    for p, tris in parts.items():
        f.write('o %s\nusemtl %s\n' % (p, p))
        for a, b, c2 in tris: f.write('f %d %d %d\n' % (a, b, c2))
print('VFP_Scroll', len(V), 'Punkte', sum(len(t) for t in parts.values()), 'Dreiecke')
