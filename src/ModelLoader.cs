using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace VikingFoodPlus
{
    /// <summary>Laedt die in die DLL eingebetteten OBJ/MTL-Modelle (Ordner Models) und baut daraus Kind-Objekte.</summary>
    public static class ModelLoader
    {
        private struct MatDef { public Color Kd; public Color Ke; }

        private static string ReadResource(string name)
        {
            var asm = typeof(ModelLoader).Assembly;
            using (var s = asm.GetManifestResourceStream("VikingFoodPlus.Models." + name))
            {
                if (s == null) throw new Exception("Modell-Ressource fehlt: " + name);
                using (var r = new StreamReader(s)) return r.ReadToEnd();
            }
        }

        private static float F(string s) { return float.Parse(s, CultureInfo.InvariantCulture); }

        private static Dictionary<string, MatDef> ParseMtl(string text)
        {
            var d = new Dictionary<string, MatDef>(); string cur = null; MatDef m = new MatDef();
            foreach (var raw in text.Split('\n'))
            {
                var t = raw.Trim().Split(' ');
                if (t[0] == "newmtl") { if (cur != null) d[cur] = m; cur = t[1]; m = new MatDef(); }
                else if (t[0] == "Kd") m.Kd = new Color(F(t[1]), F(t[2]), F(t[3]));
                else if (t[0] == "Ke") m.Ke = new Color(F(t[1]), F(t[2]), F(t[3]));
            }
            if (cur != null) d[cur] = m;
            return d;
        }

        /// <summary>Erzeugt unter parent ein Wurzelobjekt "VFP_CustomGeometry" mit einem Kind je OBJ-Objekt.</summary>
        public static GameObject Build(GameObject parent, string model, Material baseMat)
        {
            var mats = ParseMtl(ReadResource(model + ".mtl"));
            var root = new GameObject("VFP_CustomGeometry");
            root.transform.SetParent(parent.transform, false);

            var verts = new List<Vector3>();
            string name = null, mat = null;
            var tris = new List<int>(); var map = new Dictionary<int, int>(); var pv = new List<Vector3>();

            Action flush = () =>
            {
                if (name == null || tris.Count == 0) return;
                var mesh = new Mesh { name = model + "_" + name };
                mesh.SetVertices(pv);
                mesh.SetTriangles(tris, 0);
                mesh.SetUVs(0, new List<Vector2>(new Vector2[pv.Count]));
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var go = new GameObject(name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = MakeMaterial(baseMat, mats.ContainsKey(mat) ? mats[mat] : new MatDef { Kd = Color.gray });
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;
            };

            foreach (var raw in ReadResource(model + ".obj").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length < 2) continue;
                var t = line.Split(' ');
                switch (t[0])
                {
                    case "v": // OBJ ist rechtshaendig, Unity linkshaendig -> X spiegeln und Dreieckswicklung umdrehen
                        verts.Add(new Vector3(-F(t[1]), F(t[2]), F(t[3]))); break;
                    case "o":
                        flush(); name = t[1]; tris.Clear(); map.Clear(); pv.Clear(); break;
                    case "usemtl": mat = t[1]; break;
                    case "f":
                        int[] idx = new int[3];
                        for (int i = 0; i < 3; i++)
                        {
                            int g = int.Parse(t[i + 1].Split('/')[0], CultureInfo.InvariantCulture) - 1;
                            int l;
                            if (!map.TryGetValue(g, out l)) { l = pv.Count; map[g] = l; pv.Add(verts[g]); }
                            idx[i] = l;
                        }
                        tris.Add(idx[0]); tris.Add(idx[2]); tris.Add(idx[1]);
                        break;
                }
            }
            flush();
            return root;
        }

        private static Material MakeMaterial(Material baseMat, MatDef def)
        {
            var m = new Material(baseMat);
            // Vanilla-Texturen entfernen, damit die Farbe sauber durchkommt
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", Texture2D.whiteTexture);
            foreach (var p in new[] { "_BumpMap", "_MetallicGlossMap", "_EmissionMap", "_ChitinTex", "_TerrainColorTex" })
                if (m.HasProperty(p)) m.SetTexture(p, null);
            if (m.HasProperty("_Color")) m.SetColor("_Color", def.Kd);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", def.Kd);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.12f);
            bool glow = def.Ke.r + def.Ke.g + def.Ke.b > 0.35f;
            if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", glow ? def.Ke * 1.6f : Color.black);
                if (glow) m.EnableKeyword("_EMISSION");
            }
            return m;
        }
    }

    /// <summary>Basis: schaltet Kind-Objekte "S1_".."S4_" je nach Stufe um. "B_"/"D_"-Teile bleiben immer sichtbar.</summary>
    public abstract class StageSwitcher : MonoBehaviour
    {
        private readonly List<KeyValuePair<int, GameObject>> stageParts = new List<KeyValuePair<int, GameObject>>();
        private int shown = -1;
        private float timer;

        protected abstract int DefaultStage { get; }
        protected abstract int CurrentStage();

        protected virtual void Awake()
        {
            var root = transform.Find("VFP_CustomGeometry");
            if (root == null) return;
            foreach (Transform c in root)
            {
                // Namen wie "S3_Caps" -> Stufe 3
                if (c.name.Length > 3 && c.name[0] == 'S' && char.IsDigit(c.name[1]) && c.name[2] == '_')
                    stageParts.Add(new KeyValuePair<int, GameObject>(c.name[1] - '0', c.gameObject));
            }
            Apply(DefaultStage);
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = 0.5f;
            int stage;
            try { stage = CurrentStage(); } catch (Exception) { stage = DefaultStage; }
            Apply(stage);
        }

        private void Apply(int stage)
        {
            if (stage == shown) return;
            shown = stage;
            foreach (var kv in stageParts) kv.Value.SetActive(kv.Key == stage);
        }
    }

    /// <summary>Runenkessel: Stufe 1 Standard, 2 Sumpf, 3 Nebelland, 4 Hoher Norden (= Stationslevel).</summary>
    public class CauldronStageVisuals : StageSwitcher
    {
        public static readonly List<CauldronStageVisuals> All = new List<CauldronStageVisuals>();
        public int Level = 1;
        public bool Valid;   // false bei Platzierungs-Vorschau (kein ZNetView)
        private CraftingStation station;
        private MethodInfo getLevel;

        protected override int DefaultStage { get { return 1; } }

        protected override void Awake()
        {
            station = GetComponent<CraftingStation>();
            try { if (station != null) getLevel = typeof(CraftingStation).GetMethod("GetLevel"); } catch (Exception) { getLevel = null; }
            base.Awake();
        }

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        protected override int CurrentStage()
        {
            Valid = false;
            if (getLevel == null || station == null) return 1;
            var args = getLevel.GetParameters().Length == 0 ? new object[0] : new object[] { true };
            int level = Mathf.Clamp((int)getLevel.Invoke(station, args), 1, 4);
            Level = level; Valid = true;
            return level;
        }
    }

    /// <summary>Erweiterung: uebernimmt die Optik der Stufe des naechsten Runenkessels (2..4).
    /// Ohne Kessel in der Naehe (z.B. Platzierungs-Vorschau) gilt die eigene Stufe OwnTier.</summary>
    public class ExtensionStageVisuals : StageSwitcher
    {
        public int OwnTier = 2;
        private const float SearchRadius = 40f;

        protected override int DefaultStage { get { return OwnTier; } }

        protected override int CurrentStage()
        {
            CauldronStageVisuals best = null; float bestDist = SearchRadius;
            foreach (var c in CauldronStageVisuals.All)
            {
                if (c == null || !c.Valid) continue;
                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < bestDist) { bestDist = d; best = c; }
            }
            return best == null ? OwnTier : Mathf.Clamp(best.Level, 2, 4);
        }
    }
}
