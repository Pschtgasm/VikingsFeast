using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VikingFoodPlus
{
    /// <summary>
    /// Kochen am Runenkessel: Waehrend Valheims Herstell-Fortschrittsbalken laeuft, steigt zusaetzlicher Dampf
    /// in der Farbe der Kesselstufe auf. Optional (CookTime > 0) wird der Balken am Runenkessel verlaengert.
    /// Das Kochgeraeusch kommt vom Vanilla-Kessel selbst.
    /// </summary>
    public static class CauldronCooking
    {
        public const string StationName = "$vfp_station";
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static FieldInfo craftDurationField, craftTimerField;
        private static bool fieldsSearched;
        private static float vanillaDuration = -1f;
        private static CauldronCookSteam activeSteam;

        /// <summary>Wird jeden Frame vom Plugin aufgerufen.</summary>
        public static void Tick(float cookTime)
        {
            var gui = InventoryGui.instance;
            var player = Player.m_localPlayer;
            if (gui == null || player == null) { SetSteam(null); return; }
            if (!fieldsSearched)
            {
                fieldsSearched = true;
                craftDurationField = typeof(InventoryGui).GetField("m_craftDuration", Flags);
                craftTimerField = typeof(InventoryGui).GetField("m_craftTimer", Flags);
                if (craftDurationField == null || craftDurationField.FieldType != typeof(float))
                {
                    craftDurationField = null;
                    Debug.LogWarning("[Viking's Feast] Kochzeit: InventoryGui.m_craftDuration nicht gefunden, Kochzeit bleibt Vanilla");
                }
            }
            if (craftDurationField == null) return;

            var station = player.GetCurrentCraftingStation();
            bool ours = station != null && station.m_name == StationName;
            float current = (float)craftDurationField.GetValue(gui);
            if (vanillaDuration < 0f) vanillaDuration = current;
            float wanted = ours && cookTime > 0f ? Mathf.Max(0.5f, cookTime) : vanillaDuration;
            if (Mathf.Abs(current - wanted) > 0.001f) craftDurationField.SetValue(gui, wanted);

            bool cooking = false;
            if (ours && craftTimerField != null)
            {
                try { cooking = Convert.ToSingle(craftTimerField.GetValue(gui)) >= 0f; }
                catch (Exception) { cooking = false; }
            }
            SetSteam(cooking ? station.GetComponent<CauldronCookSteam>() : null);
        }

        private static void SetSteam(CauldronCookSteam steam)
        {
            if (activeSteam == steam) return;
            if (activeSteam != null) activeSteam.SetCooking(false);
            activeSteam = steam;
            if (activeSteam != null) activeSteam.SetCooking(true);
        }

        /// <summary>Baut einen eigenen Dampf (ParticleSystem) ueber dem Kessel. Material vom Vanilla-Kessel bzw. Lagerfeuer,
        /// Farbe wird beim Kochen je Kesselstufe gesetzt. Standardmaessig aus.</summary>
        public static bool AddSteam(GameObject stationGo)
        {
            var mat = FindParticleMaterial(stationGo, "steam") ?? FindParticleMaterial(PrefabManagerGet("fire_pit"), "smoke")
                      ?? FindParticleMaterial(stationGo, "") ?? FindParticleMaterial(PrefabManagerGet("fire_pit"), "");
            if (mat == null) return false;

            var root = new GameObject("VFP_CookSteam");
            root.transform.SetParent(stationGo.transform, false);
            root.transform.localPosition = new Vector3(0f, PotTop(stationGo), 0f);
            var ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(1f, 1f, 1f, 0.7f);
            main.gravityModifier = -0.04f;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 14f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.2f), new GradientAlphaKey(0f, 1f) });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2f));
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = mat;
            root.SetActive(false);
            stationGo.AddComponent<CauldronCookSteam>().SteamRoot = root;
            return true;
        }

        private static GameObject PrefabManagerGet(string name)
        {
            try { return Jotunn.Managers.PrefabManager.Instance.GetPrefab(name); }
            catch (Exception) { return null; }
        }

        private static Material FindParticleMaterial(GameObject go, string nameHint)
        {
            if (go == null) return null;
            foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (r == null || r.sharedMaterial == null || r.name.StartsWith("VFP_")) continue;
                if (nameHint.Length == 0) return r.sharedMaterial;
                string n = (r.name + " " + r.sharedMaterial.name).ToLowerInvariant();
                if (n.Contains(nameHint)) return r.sharedMaterial;
            }
            return null;
        }

        /// <summary>Oberkante des Vanilla-Topfs (hoechster Punkt der Vanilla-Meshes), Fallback 0.9 m.</summary>
        private static float PotTop(GameObject stationGo)
        {
            float top = -1f;
            foreach (var mf in stationGo.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf == null || mf.sharedMesh == null) continue;
                bool custom = false;
                for (var t = mf.transform; t != null && t != stationGo.transform; t = t.parent)
                    if (t.name.StartsWith("VFP_")) { custom = true; break; }
                if (custom) continue;
                var b = mf.sharedMesh.bounds;
                var p = stationGo.transform.InverseTransformPoint(mf.transform.TransformPoint(b.center + new Vector3(0f, b.extents.y, 0f)));
                if (p.y > top && p.y < 2.5f) top = p.y;
            }
            return top > 0.1f ? top : 0.9f;
        }
    }

    /// <summary>Haengt am Runenkessel und schaltet den Koch-Dampf an und aus.
    /// Farbe je Kesselstufe: 1 Standard (weiss), 2 Sumpf gruen, 3 Nebelland lila, 4 Hoher Norden weissblau.</summary>
    public class CauldronCookSteam : MonoBehaviour
    {
        public GameObject SteamRoot;
        private ParticleSystem steam;
        private float offTimer;
        private int tintedStage = -1;
        private CauldronStageVisuals stageVisuals;

        private static Color StageTint(int stage)
        {
            switch (stage)
            {
                case 2: return new Color(0.45f, 1f, 0.30f, 0.8f);   // Sumpf: gruen
                case 3: return new Color(0.75f, 0.35f, 1f, 0.8f);   // Nebelland: lila
                case 4: return new Color(0.70f, 0.88f, 1f, 0.8f);   // Hoher Norden: weissblau
                default: return new Color(1f, 1f, 1f, 0.7f);        // Standard: weiss
            }
        }

        private void Awake()
        {
            var t = transform.Find("VFP_CookSteam");
            if (t != null) SteamRoot = t.gameObject;
            if (SteamRoot != null) { steam = SteamRoot.GetComponent<ParticleSystem>(); SteamRoot.SetActive(false); }
            stageVisuals = GetComponent<CauldronStageVisuals>();
        }

        public void SetCooking(bool cooking)
        {
            if (SteamRoot == null) return;
            if (cooking) { offTimer = 0f; ApplyTint(); if (!SteamRoot.activeSelf) SteamRoot.SetActive(true); }
            else if (SteamRoot.activeSelf) offTimer = 2f;   // Dampf kurz nachziehen lassen
        }

        private void Update()
        {
            if (SteamRoot == null || !SteamRoot.activeSelf) return;
            ApplyTint();
            if (offTimer <= 0f) return;
            offTimer -= Time.deltaTime;
            if (offTimer <= 0f) SteamRoot.SetActive(false);
        }

        private void ApplyTint()
        {
            if (steam == null) return;
            int stage = stageVisuals != null && stageVisuals.Valid ? stageVisuals.Level : 1;
            if (stage == tintedStage) return;
            tintedStage = stage;
            var main = steam.main;
            main.startColor = StageTint(stage);
        }
    }
}
