"""Kleine Mesh-Bibliothek: Primitive, OBJ/MTL-Export und Software-Renderer fuer Vorschaubilder."""
import math, os
import numpy as np
from PIL import Image

# ---------------------------------------------------------------- Transformationen
def rotm(rx=0, ry=0, rz=0):
    a, b, c = [math.radians(v) for v in (rx, ry, rz)]
    Rx = np.array([[1, 0, 0], [0, math.cos(a), -math.sin(a)], [0, math.sin(a), math.cos(a)]])
    Ry = np.array([[math.cos(b), 0, math.sin(b)], [0, 1, 0], [-math.sin(b), 0, math.cos(b)]])
    Rz = np.array([[math.cos(c), -math.sin(c), 0], [math.sin(c), math.cos(c), 0], [0, 0, 1]])
    return Rz @ Ry @ Rx

def look_rot(d):
    """Rotation, die +Y auf Richtung d dreht."""
    d = np.array(d, float); d /= np.linalg.norm(d)
    y = np.array([0, 1, 0.])
    v = np.cross(y, d); c = float(y @ d)
    if np.linalg.norm(v) < 1e-9:
        return np.eye(3) if c > 0 else rotm(180, 0, 0)
    vx = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]])
    return np.eye(3) + vx + vx @ vx * (1 / (1 + c))

# ---------------------------------------------------------------- Primitive (V, F)
def _clean(V, F):
    V = np.array(V, float); F = np.array(F, int)
    if len(F):
        a, b, c = V[F[:, 0]], V[F[:, 1]], V[F[:, 2]]
        area = np.linalg.norm(np.cross(b - a, c - a), axis=1)
        F = F[area > 1e-10]
    return V, F

def lathe(profile, seg=12):
    """profile: Liste (r,y); None = Bruch (harte Kante, neue Vertexringe)."""
    V, F, prev = [], [], None
    for p in profile:
        if p is None:
            prev = None; continue
        r, y = p; start = len(V)
        for j in range(seg):
            a = 2 * math.pi * j / seg
            V.append((r * math.cos(a), y, r * math.sin(a)))
        if prev is not None:
            for j in range(seg):
                j2 = (j + 1) % seg
                a, b, c, d = prev + j, prev + j2, start + j, start + j2
                F += [(a, b, d), (a, d, c)]
        prev = start
    return _clean(V, F)

def cyl(r, h, seg=10, r2=None):
    r2 = r if r2 is None else r2
    return lathe([(0, 0), (r, 0), None, (r, 0), (r2, h), None, (r2, h), (0, h)], seg)

def cone(r, h, seg=10):
    return cyl(r, h, seg, 0.0)

def sphere(r=1.0, seg=10, rings=7):
    prof = [(r * math.sin(math.pi * i / rings), -r * math.cos(math.pi * i / rings)) for i in range(rings + 1)]
    return lathe(prof, seg)

def dome(R, H, seg=12, rings=5, lip=0.05):
    top = [(R * math.sin(t), H * math.cos(t)) for t in np.linspace(0, math.pi / 2, rings + 1)]
    return lathe(top + [(R * .93, -lip), (R * .3, -.005), (0, -.005)], seg)

def box(size):
    sx, sy, sz = size; h = np.array([sx, sy, sz]) / 2
    V, F = [], []
    for ax in range(3):
        for s in (1, -1):
            n = np.zeros(3); n[ax] = s
            u = np.zeros(3); u[(ax + 1) % 3] = 1
            v = np.zeros(3); v[(ax + 2) % 3] = 1
            quad = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
            if s < 0: quad = quad[::-1]
            i0 = len(V)
            for (a, b) in quad:
                V.append((n * h[ax] + u * a * h[(ax + 1) % 3] + v * b * h[(ax + 2) % 3]))
            F += [(i0, i0 + 1, i0 + 2), (i0, i0 + 2, i0 + 3)]
    return _clean(V, F)

def torus(R, r, segU=20, segV=8):
    V, F = [], []
    for i in range(segU):
        a = 2 * math.pi * i / segU
        for j in range(segV):
            b = 2 * math.pi * j / segV
            V.append(((R + r * math.cos(b)) * math.cos(a), r * math.sin(b), (R + r * math.cos(b)) * math.sin(a)))
    for i in range(segU):
        for j in range(segV):
            a = i * segV + j; b = ((i + 1) % segU) * segV + j
            c = i * segV + (j + 1) % segV; d = ((i + 1) % segU) * segV + (j + 1) % segV
            F += [(a, b, d), (a, d, c)]
    return _clean(V, F)

def _catmull(pts, sub):
    P = [pts[0]] + list(pts) + [pts[-1]]; out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = [np.array(P[k], float) for k in (i - 1, i, i + 1, i + 2)]
        for s in range(sub):
            t = s / sub
            out.append(.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    out.append(np.array(pts[-1], float))
    return np.array(out)

def tube(path, radii, seg=6, smooth=0, caps=True):
    path = np.array(path, float)
    n0 = len(path)
    if np.isscalar(radii): radii = [radii] * n0
    if smooth:
        newp = _catmull(path, smooth)
        tt = np.linspace(0, 1, n0); t2 = np.linspace(0, 1, len(newp))
        radii = np.interp(t2, tt, radii); path = newp
    radii = np.array(radii, float); n = len(path)
    T = np.zeros_like(path)
    for i in range(n):
        a = path[max(i - 1, 0)]; b = path[min(i + 1, n - 1)]
        t = b - a; T[i] = t / np.linalg.norm(t)
    ref = np.array([0, 1, 0.]) if abs(T[0] @ np.array([0, 1, 0.])) < .9 else np.array([1, 0, 0.])
    N = np.cross(T[0], ref); N /= np.linalg.norm(N)
    V, F = [], []
    for i in range(n):
        if i > 0:
            N = N - (N @ T[i]) * T[i]; N /= np.linalg.norm(N)
        B = np.cross(T[i], N)
        for j in range(seg):
            a = 2 * math.pi * j / seg
            V.append(path[i] + radii[i] * (math.cos(a) * N + math.sin(a) * B))
    for i in range(n - 1):
        for j in range(seg):
            j2 = (j + 1) % seg
            a, b, c, d = i * seg + j, i * seg + j2, (i + 1) * seg + j, (i + 1) * seg + j2
            F += [(a, b, d), (a, d, c)]
    if caps:
        for end in (0, n - 1):
            c0 = len(V); V.append(path[end]); base = len(V)
            ring = [V[end * seg + j] for j in range(seg)]
            V.extend(ring)
            for j in range(seg):
                j2 = (j + 1) % seg
                F.append((c0, base + j, base + j2) if end == n - 1 else (c0, base + j2, base + j))
    return _clean(V, F)

def helix(cx, cz, R, y0, y1, turns, steps=72, a0=0.0):
    pts = []
    for i in range(steps + 1):
        t = i / steps; a = a0 + turns * 2 * math.pi * t
        pts.append((cx + R * math.cos(a), y0 + (y1 - y0) * t, cz + R * math.sin(a)))
    return pts

def jitter(prim, amp, seed=0):
    """Verschiebt Vertices radial; Position-abhaengig, damit gemeinsame Punkte zusammenbleiben."""
    V, F = prim; V = V.copy()
    for i, p in enumerate(V):
        h = math.sin(p[0] * 12.9898 + p[1] * 78.233 + p[2] * 37.719 + seed * 4.1) * 43758.5453
        h = h - math.floor(h)
        n = np.linalg.norm(p)
        if n > 1e-9: V[i] = p * (1 + amp * (h * 2 - 1))
    return V, F

# ---------------------------------------------------------------- Modell
class Model:
    def __init__(self, name):
        self.name = name; self.parts = {}; self.mats = {}

    def mat(self, name, kd, ke=(0, 0, 0)):
        self.mats[name] = (kd, ke)

    def add(self, part, mat, prim, pos=(0, 0, 0), rot=None, scl=(1, 1, 1), flat=False, parent=None):
        V, F = prim; V = np.array(V, float) * np.array(scl, float)
        if rot is not None:
            R = rot if isinstance(rot, np.ndarray) else rotm(*rot)
            V = V @ R.T
        V = V + np.array(pos, float)
        if parent is not None:
            ppos, prot = parent[0], parent[1]; k = parent[2] if len(parent) > 2 else 1.0
            V = V * k
            V = V @ (prot if isinstance(prot, np.ndarray) else rotm(*prot)).T + np.array(ppos, float)
        F = np.array(F, int)
        vol = np.einsum('ij,ij->i', V[F[:, 0]], np.cross(V[F[:, 1]], V[F[:, 2]])).sum() / 6
        if vol < 0: F = F[:, ::-1]
        if flat:
            V = V[F.reshape(-1)]; F = np.arange(len(V)).reshape(-1, 3)
        if part in self.parts and self.parts[part]['mat'] != mat:
            part = part + '_' + mat          # ein Objekt = ein Material
        p = self.parts.setdefault(part, {'mat': mat, 'V': [], 'F': [], 'n': 0})
        p['V'].append(V); p['F'].append(F + p['n']); p['n'] += len(V)

    def arrays(self, part):
        p = self.parts[part]
        return np.vstack(p['V']), np.vstack(p['F']), p['mat']

    def export(self, folder):
        os.makedirs(folder, exist_ok=True)
        with open(os.path.join(folder, self.name + '.mtl'), 'w') as f:
            for n, (kd, ke) in self.mats.items():
                f.write('newmtl %s\nKd %.3f %.3f %.3f\nKe %.3f %.3f %.3f\n\n' % ((n,) + tuple(kd) + tuple(ke)))
        off = 0; tris = 0
        with open(os.path.join(folder, self.name + '.obj'), 'w') as f:
            f.write('# %s - generated by tools/make_models.py\nmtllib %s.mtl\n' % (self.name, self.name))
            for part in self.parts:
                V, F, mat = self.arrays(part)
                f.write('o %s\nusemtl %s\n' % (part, mat))
                for v in V: f.write('v %.4f %.4f %.4f\n' % tuple(v))
                for t in F: f.write('f %d %d %d\n' % (t[0] + 1 + off, t[1] + 1 + off, t[2] + 1 + off))
                off += len(V); tris += len(F)
        return tris

# ---------------------------------------------------------------- Vorschau-Renderer
def render(model, yaw=30, pitch=20, size=520, ss=2, include=lambda n: True, dist=5.2, center=(0, .9, 0), zoom=1.0, bg=((14, 20, 22), (30, 42, 40))):
    W = size * ss
    img = np.zeros((W, W, 3)); zb = np.full((W, W), -1e9)
    yy = np.linspace(0, 1, W)[:, None]
    for c in range(3): img[:, :, c] = bg[0][c] * (1 - yy) + bg[1][c] * yy
    Ry, Rx = rotm(0, yaw, 0), rotm(-pitch, 0, 0)
    L = np.array([-.45, .75, .5]); L /= np.linalg.norm(L)
    cen = np.array(center)
    scale = W * .46 * zoom / 1.8
    for part in model.parts:
        if not include(part): continue
        V, F, mat = model.arrays(part)
        kd, ke = model.mats[mat]
        P = (V - cen) @ Ry.T @ Rx.T
        depth = dist - P[:, 2]
        sx = W / 2 + scale * P[:, 0] * dist / depth
        sy = W / 2 - scale * (P[:, 1]) * dist / depth
        for t in F:
            a, b, c = P[t[0]], P[t[1]], P[t[2]]
            n = np.cross(b - a, c - a); ln = np.linalg.norm(n)
            if ln < 1e-12: continue
            n /= ln
            if n[2] < 0: continue  # Backface-Culling = Winding-Test
            xs = sx[t]; ys = sy[t]
            x0, x1 = int(max(xs.min(), 0)), int(min(xs.max() + 1, W - 1))
            y0, y1 = int(max(ys.min(), 0)), int(min(ys.max() + 1, W - 1))
            if x1 <= x0 or y1 <= y0: continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
            d = (ys[1] - ys[2]) * (xs[0] - xs[2]) + (xs[2] - xs[1]) * (ys[0] - ys[2])
            if abs(d) < 1e-9: continue
            w0 = ((ys[1] - ys[2]) * (gx - xs[2]) + (xs[2] - xs[1]) * (gy - ys[2])) / d
            w1 = ((ys[2] - ys[0]) * (gx - xs[2]) + (xs[0] - xs[2]) * (gy - ys[2])) / d
            w2 = 1 - w0 - w1
            m = (w0 >= -1e-4) & (w1 >= -1e-4) & (w2 >= -1e-4)
            if not m.any(): continue
            z = w0 * P[t[0], 2] + w1 * P[t[1], 2] + w2 * P[t[2], 2]
            sub = zb[y0:y1 + 1, x0:x1 + 1]
            upd = m & (z > sub)
            if not upd.any(): continue
            lam = max(float(n @ (L @ Ry.T @ Rx.T)), 0)
            col = np.array(kd) * (.32 + .75 * lam) + np.array(ke) * .85
            sub[upd] = z[upd]
            img[y0:y1 + 1, x0:x1 + 1][upd] = np.clip(col, 0, 1) * 255
    im = Image.fromarray(img.astype(np.uint8))
    return im.resize((size, size), Image.LANCZOS) if ss > 1 else im
