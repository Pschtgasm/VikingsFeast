using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace VikingFoodPlus
{
    /// <summary>
    /// Siegesmaehler (Entwicklung ab 1.7.1, Veroeffentlichung als 1.8.0, siehe KONZEPT_SIEGESMAHL.md):
    /// pro Boss ein Festmahl (Servierbrett, 10 Portionen) aus seiner Trophaee und Zutaten seines Bioms. Eingefaerbtes
    /// Vanilla-Festmahl des Bioms mit dem Trophaeen-Modell als Tafelschmuck. Freischalten nach dem Bosssieg,
    /// Siegesmeldung beim ersten Essen, eigene Gruppe am Runenkessel mit ???-Zeilen.
    /// </summary>
    public partial class VikingFoodPlus
    {
        internal const string SecVictory = "7 - Victory Feasts";
        private ConfigEntry<bool> victoryEnabled, victoryNeedKill, victoryMessage, victoryTrophy;
        private readonly Dictionary<string, ConfigEntry<bool>> vicOn = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<string>> vicIng = new Dictionary<string, ConfigEntry<string>>();
        private readonly Dictionary<string, ConfigEntry<float>> vicYaw = new Dictionary<string, ConfigEntry<float>>(), vicPitch = new Dictionary<string, ConfigEntry<float>>(),
            vicScale = new Dictionary<string, ConfigEntry<float>>(), vicHeight = new Dictionary<string, ConfigEntry<float>>(), vicBack = new Dictionary<string, ConfigEntry<float>>(),
            vicSide = new Dictionary<string, ConfigEntry<float>>();
        private const float VictoryBuffDuration = 1800f;

        /// <summary>Startwerte fuer den Tafelschmuck je Boss: Drehung zur Seite (Grad), Kippen (Grad), Groesse (Faktor),
        /// Hoehe (Anteil der Trophaeengroesse), Abstand nach hinten (-1 vorne .. 1 hinten). Die Trophaeen sind fuer
        /// Wand-Objektstaender gebaut und schauen je nach Modell in eine andere Richtung (Screenshots 1.7.1-Test).</summary>
        private static readonly Dictionary<string, float[]> TrophyDefaults = new Dictionary<string, float[]> {
            { "Eikthyr",  new[] { 0f,   0f, 1.00f, 0.00f, 0.45f } },
            { "Elder",    new[] { -90f, 0f, 0.70f, 0.00f, 0.55f } },   // schaute nach links, zu gross, verdeckte das Essen
            { "Bonemass", new[] { 0f,   0f, 1.00f, 0.00f, 0.45f } },
            { "Moder",    new[] { -90f, 0f, 0.85f, 0.05f, 0.10f, 0.00f } },   // mittig auf dem Topf (Topf-Modell ist um 90 Grad gedreht)
            { "Yagluth",  new[] { 0f,   0f, 1.00f, 0.00f, 0.45f } },
            { "Queen",    new[] { 0f, -70f, 1.00f, 0.00f, 0.30f } },   // lag flach auf dem Ruecken -> nach vorn aufrichten
            { "Fader",    new[] { 0f,   0f, 1.00f, -0.35f, 0.30f } },  // schwebte ueber der Schuessel
            { "Fimbul",   new[] { 0f,   0f, 1.00f, 0.00f, 0.45f } },
        };

        internal sealed class VictorySpec
        {
            public string Boss, De, En, DescDe, DescEn, VanillaBase, Ingredients, Trophy, BossKey, BossDe, BossEn, Effect, EffDe, EffEn, TipDe, TipEn;
            public float Hp, Stamina, Eitr, Regen;
            public int Level;
            public Color Tint;
            public string Id { get { return "Victory_" + Boss; } }          // Icon Icons/Victory_<Boss>.png
            public string Key { get { return "vfp_victory_" + Boss.ToLowerInvariant(); } }
        }

        internal static readonly VictorySpec[] Victories = {
            new VictorySpec { Boss = "Eikthyr", De = "Hirschkönigs-Braten", En = "Stag king's roast",
                DescDe = "Ein Festmahl zu Ehren des Sieges über Eikthyr. Es knistert noch ein wenig.", DescEn = "A feast to honour the victory over Eikthyr. It still crackles a little.",
                VanillaBase = "FeastMeadows", Ingredients = "TrophyEikthyr:1,DeerMeat:8,Honey:4,Raspberry:8",
                Trophy = "TrophyEikthyr", BossKey = "defeated_eikthyr", BossDe = "Eikthyr", BossEn = "Eikthyr",
                Hp = 38, Stamina = 38, Eitr = 0, Regen = 2, Level = 1, Tint = new Color(0.80f, 0.92f, 1.00f),
                Effect = "speed", EffDe = "Hirschkönigs Schritt", EffEn = "Stag king's stride", TipDe = "Du läufst 10 % schneller.", TipEn = "You move 10% faster." },
            new VictorySpec { Boss = "Elder", De = "Wurzelschmaus des Ältesten", En = "Elder's root feast",
                DescDe = "Wurzeln, Pilze und Beeren aus dem Wald des Ältesten. Riecht nach Moos und Sieg.", DescEn = "Roots, mushrooms and berries from the Elder's forest. Smells of moss and victory.",
                VanillaBase = "FeastBlackforest", Ingredients = "TrophyTheElder:1,Carrot:10,MushroomYellow:8,Blueberries:8",
                Trophy = "TrophyTheElder", BossKey = "defeated_gdking", BossDe = "den Ältesten", BossEn = "the Elder",
                Hp = 48, Stamina = 48, Eitr = 0, Regen = 3, Level = 1, Tint = new Color(0.82f, 1.00f, 0.75f),
                Effect = "attackstamina", EffDe = "Kraft des Ältesten", EffEn = "Elder's strength", TipDe = "Angriffe und Holzfällen kosten 15 % weniger Ausdauer.", TipEn = "Attacks and chopping wood cost 15% less stamina." },
            new VictorySpec { Boss = "Bonemass", De = "Moorgelage", En = "Bog banquet",
                DescDe = "Ein Gelage auf dem Sieg über Knochenmasse. Nicht fragen, was im Eintopf schwimmt.", DescEn = "A banquet for the victory over Bonemass. Don't ask what floats in the stew.",
                VanillaBase = "FeastSwamps", Ingredients = "TrophyBonemass:1,Bloodbag:6,Entrails:8,Turnip:10",
                Trophy = "TrophyBonemass", BossKey = "defeated_bonemass", BossDe = "Knochenmasse", BossEn = "Bonemass",
                Hp = 58, Stamina = 58, Eitr = 0, Regen = 3, Level = 2, Tint = new Color(0.85f, 1.00f, 0.60f),
                Effect = "poison", EffDe = "Moorfestigkeit", EffEn = "Bog resilience", TipDe = "Weniger Giftschaden, Gesundheit regeneriert 25 % schneller.", TipEn = "Less poison damage, health regenerates 25% faster." },
            new VictorySpec { Boss = "Moder", De = "Drachenfest", En = "Dragon feast",
                DescDe = "Wolfsbraten, Zwiebeln und Frost. Ein Fest für alle, die Moder bezwungen haben.", DescEn = "Wolf roast, onions and frost. A feast for everyone who defeated Moder.",
                VanillaBase = "FeastMountains", Ingredients = "TrophyDragonQueen:1,WolfMeat:8,Onion:8,FreezeGland:4",
                Trophy = "TrophyDragonQueen", BossKey = "defeated_dragon", BossDe = "Moder", BossEn = "Moder",
                Hp = 70, Stamina = 70, Eitr = 0, Regen = 4, Level = 2, Tint = new Color(0.88f, 0.84f, 1.00f),
                Effect = "frostfall", EffDe = "Drachenhaut", EffEn = "Dragon hide", TipDe = "Weniger Frostschaden, 50 % weniger Fallschaden.", TipEn = "Less frost damage, 50% less fall damage." },
            new VictorySpec { Boss = "Yagluth", De = "Feuerkönigs-Tafel", En = "Fire king's table",
                DescDe = "Glühender Braten und frisches Brot. Yagluths Feuer brennt jetzt in deinem Bauch.", DescEn = "Glowing roast and fresh bread. Yagluth's fire now burns in your belly.",
                VanillaBase = "FeastPlains", Ingredients = "TrophyGoblinKing:1,LoxMeat:8,BarleyFlour:10,Cloudberry:8",
                Trophy = "TrophyGoblinKing", BossKey = "defeated_goblinking", BossDe = "Yagluth", BossEn = "Yagluth",
                Hp = 80, Stamina = 80, Eitr = 0, Regen = 4, Level = 2, Tint = new Color(1.00f, 0.78f, 0.62f),
                Effect = "firedamage", EffDe = "Feuer des Königs", EffEn = "Fire of the king", TipDe = "Deine Treffer verursachen zusätzlich 10 % Feuerschaden.", TipEn = "Your hits deal an extra 10% fire damage." },
            new VictorySpec { Boss = "Queen", De = "Bankett der Königin", En = "Queen's banquet",
                DescDe = "Ein Bankett aus dem Reich der Königin. Die Pilze leuchten noch. Ein bisschen.", DescEn = "A banquet from the Queen's realm. The mushrooms still glow. A little.",
                VanillaBase = "FeastMistlands", Ingredients = "TrophySeekerQueen:1,HareMeat:6,MushroomMagecap:8,RoyalJelly:6",
                Trophy = "TrophySeekerQueen", BossKey = "defeated_queen", BossDe = "die Königin", BossEn = "the Queen",
                Hp = 90, Stamina = 90, Eitr = 45, Regen = 5, Level = 3, Tint = new Color(0.98f, 0.80f, 1.00f),
                Effect = "carry", EffDe = "Panzer der Königin", EffEn = "Queen's carapace", TipDe = "+100 Tragkraft, 15 % weniger Schaden von Suchern, Gjall und Zecken.", TipEn = "+100 carry weight, 15% less damage from seekers, gjall and ticks." },
            new VictorySpec { Boss = "Fader", De = "Glutsieg-Mahl", En = "Ember victory meal",
                DescDe = "Aus der Glut des Aschlands, zu Ehren des Sieges über Fader. Vorsicht, heiß.", DescEn = "From the embers of the Ashlands, in honour of the victory over Fader. Careful, hot.",
                VanillaBase = "FeastAshlands", Ingredients = "TrophyFader:1,AsksvinMeat:8,VoltureMeat:4,Vineberry:10",
                Trophy = "TrophyFader", BossKey = "defeated_fader", BossDe = "Fader", BossEn = "Fader",
                Hp = 110, Stamina = 110, Eitr = 55, Regen = 6, Level = 3, Tint = new Color(1.00f, 0.86f, 0.62f),
                Effect = "fire", EffDe = "Glutsieg", EffEn = "Ember victory", TipDe = "Feuerschaden halbiert.", TipEn = "Fire damage halved." },
            new VictorySpec { Boss = "Fimbul", De = "Fimbul-Festtafel", En = "Fimbul banquet",
                DescDe = "Elchbraten, Haferbrei und Beeren aus dem ewigen Eis. Der Fimbulwinter ist vorbei. Fürs Erste.", DescEn = "Moose roast, oat porridge and berries from the eternal ice. Fimbulwinter is over. For now.",
                // Kall hat keine Boss-Trophaee (in den Spieldateien nur FrozenKingDrop, sein Beutestueck)
                VanillaBase = "FeastDeepNorth", Ingredients = "FrozenKingDrop:1,MooseMeat:8,Lingonberry:10,Oat:8",
                Trophy = "FrozenKingDrop", BossKey = "defeated_frozenking", BossDe = "Kall Fimbulbringer", BossEn = "Kall Fimbulbringer",
                Hp = 120, Stamina = 120, Eitr = 60, Regen = 7, Level = 4, Tint = new Color(0.82f, 0.95f, 1.00f),
                Effect = "cold", EffDe = "Fimbulwärme", EffEn = "Fimbul warmth", TipDe = "Weniger Frostschaden, Ausdauer regeneriert 15 % schneller.", TipEn = "Less frost damage, stamina regenerates 15% faster." },
        };

        // Laufzeitdaten fuer die Patches
        internal static readonly Dictionary<string, VictorySpec> VictoryByEatItem = new Dictionary<string, VictorySpec>();   // VFP_Victory_X -> Spec
        internal static int VictoryFireHash, VictoryQueenHash;
        internal static bool VictoryShowMessage = true, VictoryNeedKill = true;

        private void BindVictoryConfig()
        {
            victoryEnabled = Config.Bind(SecVictory, "Enabled", true,
                "Siegesmaehler: pro Boss ein Festmahl aus seiner Trophaee (Runenkessel, Gruppe Siegesmaehler, mit dem Servierbrett aufstellen). Neustart noetig.");
            victoryNeedKill = Config.Bind(SecVictory, "NeedBossKill", true,
                "true = ein Siegesmahl ist erst kochbar, wenn sein Boss in der Welt besiegt ist (vorher ??? mit Hinweis). false = immer kochbar. Neustart noetig.");
            victoryMessage = Config.Bind(SecVictory, "ShowMessage", true,
                "Meldung beim ersten Essen eines Siegesmahls (z. B. \"Ihr feiert den Sieg ueber Moder!\"). Sofort wirksam.");
            victoryTrophy = Config.Bind(SecVictory, "TrophyDecoration", true,
                "Das Modell der Boss-Trophaee steht als Tafelschmuck auf der Platte. Neustart noetig.");
            VictoryShowMessage = victoryMessage.Value; victoryMessage.SettingChanged += (s, e) => VictoryShowMessage = victoryMessage.Value;
            VictoryNeedKill = victoryNeedKill.Value;
            foreach (var v in Victories)
            {
                string sec = "Victory - " + v.Boss;
                vicOn[v.Boss] = Config.Bind(sec, "Enabled", true, "Dieses Siegesmahl an/aus. Neustart noetig.");
                vicIng[v.Boss] = Config.Bind(sec, "Ingredients", v.Ingredients, "Prefab:Anzahl kommagetrennt. Neustart noetig.");
                float[] d; if (!TrophyDefaults.TryGetValue(v.Boss, out d)) d = new[] { 0f, 0f, 1f, 0f, 0.45f };
                vicYaw[v.Boss] = Config.Bind(sec, "TrophyTurn", d[0], new ConfigDescription("Tafelschmuck: Drehung der Trophaee zur Seite in Grad (z. B. 90 oder -90, damit sie nach vorn schaut). Neustart noetig.", new AcceptableValueRange<float>(-180f, 180f)));
                vicPitch[v.Boss] = Config.Bind(sec, "TrophyTilt", d[1], new ConfigDescription("Tafelschmuck: Kippen nach vorn/hinten in Grad. Neustart noetig.", new AcceptableValueRange<float>(-90f, 90f)));
                vicScale[v.Boss] = Config.Bind(sec, "TrophySize", d[2], new ConfigDescription("Tafelschmuck: Groesse (1 = etwa 38 % der Plattenbreite). Neustart noetig.", new AcceptableValueRange<float>(0.2f, 3f)));
                vicHeight[v.Boss] = Config.Bind(sec, "TrophyHeight", d[3], new ConfigDescription("Tafelschmuck: hoeher (+) oder tiefer (-), in Anteilen der Trophaeengroesse. Neustart noetig.", new AcceptableValueRange<float>(-1f, 2f)));
                vicBack[v.Boss] = Config.Bind(sec, "TrophyBack", d[4], new ConfigDescription("Tafelschmuck: Position auf der Platte, -1 = ganz vorn, 0 = Mitte, 1 = ganz hinten. Neustart noetig.", new AcceptableValueRange<float>(-1f, 1.5f)));
                vicSide[v.Boss] = Config.Bind(sec, "TrophySide", d.Length > 5 ? d[5] : 0f, new ConfigDescription("Tafelschmuck: nach links (-) oder rechts (+) verschieben, -1 .. 1 = Plattenrand. Neustart noetig.", new AcceptableValueRange<float>(-1.5f, 1.5f)));
            }
        }

        private void ApplyVictoryPatches()
        {
            try
            {
                if (harmony == null) harmony = new Harmony(Guid);
                foreach (var t in typeof(VictoryPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    try { harmony.CreateClassProcessor(t).Patch(); }
                    catch (Exception ex) { Logger.LogWarning("Siegesmahl-Patch " + t.Name + " nicht aktiv: " + ex.Message); }
                }
            }
            catch (Exception ex) { Logger.LogError("Siegesmahl-Patches: " + ex); }
        }

        private void RegisterVictoryFeasts()
        {
            var feaster = PrefabManager.Instance.GetPrefab("Feaster");
            var tray = feaster != null ? feaster.GetComponent<ItemDrop>() : null;
            var table = tray != null ? tray.m_itemData.m_shared.m_buildPieces : null;
            if (table == null) throw new Exception("Servierbrett (Feaster) oder sein Bau-Menue nicht gefunden");
            RecipeGroups.VictoryNeedKill = VictoryNeedKill;
            int ok = 0;
            foreach (var v in Victories)
            {
                if (!vicOn[v.Boss].Value) continue;
                try { RegisterVictory(v, table); ok++; }
                catch (Exception ex) { Logger.LogWarning("Siegesmahl " + v.Boss + " übersprungen: " + ex.Message); }
            }
            Logger.LogInfo("Siegesmaehler: " + ok + " registriert" + (VictoryNeedKill ? ", Freischalten nach dem Bosssieg" : ""));
            if (ok < Victories.Length) LogTrophyNames();
        }

        private void RegisterVictory(VictorySpec v, PieceTable table)
        {
            string baseFeast = v.VanillaBase;
            if (PrefabManager.Instance.GetPrefab(baseFeast) == null || PrefabManager.Instance.GetPrefab(baseFeast + "_Material") == null)
            {
                Logger.LogWarning("Siegesmahl " + v.Boss + ": Vanilla-Festmahl " + baseFeast + " fehlt, nutze FeastPlains");
                baseFeast = "FeastPlains";
            }
            var spec = new FeastSpec(v.Id, v.De, v.En, v.DescDe, v.DescEn, baseFeast, vicIng[v.Boss].Value, v.Hp, v.Stamina, v.Eitr, v.Regen, v.Level, v.Tint);
            RegisterFeast(spec, table, table.gameObject.name, Station);   // Klonen, Einfaerben, Servierbrett-Platte, Rezept am Runenkessel

            string eatId = "VFP_" + v.Id, matId = eatId + "_Material", pieceId = eatId + "_Piece";
            var eat = PrefabManager.Instance.GetPrefab(eatId);
            var mat = PrefabManager.Instance.GetPrefab(matId);
            var piece = PrefabManager.Instance.GetPrefab(pieceId);
            if (eat == null || mat == null || piece == null) throw new Exception("Festmahl-Prefabs nicht erstellt");
            // Namen und Beschreibung ueber die Victory-Schluessel (RegisterFeast setzt vfp_<id>)
            foreach (var go in new[] { eat, mat })
            {
                var sh = go.GetComponent<ItemDrop>().m_itemData.m_shared;
                sh.m_name = "$" + v.Key; sh.m_description = "$" + v.Key + "_desc";
            }
            var pc = piece.GetComponent<Piece>(); pc.m_name = "$" + v.Key; pc.m_description = "$" + v.Key + "_desc";

            // Bonus beim Essen
            var eatShared = eat.GetComponent<ItemDrop>().m_itemData.m_shared;
            Sprite icon = eatShared.m_icons != null && eatShared.m_icons.Length > 0 ? eatShared.m_icons[0] : null;
            var se = CreateVictoryEffect(v, icon);
            if (se != null) eatShared.m_consumeStatusEffect = se;
            VictoryByEatItem[eatId] = v;

            // Tafelschmuck: Trophaeen-Modell auf der Platte (nicht eingefaerbt)
            if (victoryTrophy.Value)
            {
                try { AddTrophyDecoration(piece, v); }
                catch (Exception ex) { Logger.LogWarning("Siegesmahl " + v.Boss + ": Tafelschmuck nicht moeglich: " + ex.Message); }
            }
            RecipeGroups.VictoryItems.Add(new[] { matId, "$" + v.Key, "$" + v.Key + "_hint", v.Trophy, "$vfp_victory_locked", "$vfp_victory_locked_desc", v.BossKey });
        }

        private StatusEffect CreateVictoryEffect(VictorySpec v, Sprite icon)
        {
            var se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_VFP_" + v.Id;
            se.m_name = "$" + v.Key + "_eff";
            se.m_tooltip = "$" + v.Key + "_tip";
            se.m_ttl = VictoryBuffDuration;
            if (icon != null) se.m_icon = icon;
            switch (v.Effect)
            {
                case "speed": se.m_speedModifier = 0.10f; break;
                case "attackstamina": se.m_attackStaminaUseModifier = -0.15f; break;
                case "poison": se.m_mods = DamageMods("Poison", "Resistant"); se.m_healthRegenMultiplier = 1.25f; break;
                case "frostfall": se.m_mods = DamageMods("Frost", "Resistant"); se.m_fallDamageModifier = -0.5f; break;
                case "firedamage": break;   // per Patch (VictoryPatches.VictoryDamage)
                case "carry": se.m_addMaxCarryWeight = 100f; break;
                case "fire": se.m_mods = DamageMods("Fire", "Resistant"); break;
                case "cold": se.m_mods = DamageMods("Frost", "Resistant"); se.m_staminaRegenMultiplier = 1.15f; break;
            }
            ItemManager.Instance.AddStatusEffect(new Jotunn.Entities.CustomStatusEffect(se, false));
            if (v.Effect == "firedamage") VictoryFireHash = se.NameHash();
            if (v.Effect == "carry") VictoryQueenHash = se.NameHash();
            return se;
        }

        private List<HitData.DamageModPair> DamageMods(string type, string modifier)
        {
            var list = new List<HitData.DamageModPair>();
            object t = TryParseEnum(typeof(HitData.DamageType), type), m = TryParseEnum(typeof(HitData.DamageModifier), modifier);
            if (t != null && m != null) list.Add(new HitData.DamageModPair { m_type = (HitData.DamageType)t, m_modifier = (HitData.DamageModifier)m });
            return list;
        }

        /// <summary>Kopiert die sichtbaren Teile der Boss-Trophaee auf die Platte: hinten mittig, so gross, dass das Festmahl erkennbar bleibt.</summary>
        private void AddTrophyDecoration(GameObject piece, VictorySpec v)
        {
            var trophy = PrefabManager.Instance.GetPrefab(v.Trophy);
            if (trophy == null) throw new Exception("Trophaee " + v.Trophy + " nicht gefunden");
            var source = trophy.transform.Find("attach") ?? trophy.transform;
            // Groesse der Platte (lokale Bounds aller sichtbaren Teile)
            Bounds pb = LocalBounds(piece.transform, piece);
            var deco = new GameObject("VFP_TrophyDecoration");
            deco.transform.SetParent(piece.transform, false);
            var copy = UnityEngine.Object.Instantiate(source.gameObject, deco.transform, false);
            copy.name = "Trophy";
            copy.transform.localPosition = Vector3.zero;
            // Drehung je Boss (Config), damit die Trophaee nach vorn schaut statt zur Seite
            copy.transform.localRotation = Quaternion.Euler(vicPitch[v.Boss].Value, vicYaw[v.Boss].Value, 0f);
            // alles ausser Darstellung entfernen (Kollision, Physik, Netzwerk, Item-Logik)
            var remove = new List<Component>();
            foreach (var c in copy.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer) && !(c is SkinnedMeshRenderer)) remove.Add(c);
            foreach (var c in remove) UnityEngine.Object.DestroyImmediate(c);
            Bounds tb = LocalBounds(deco.transform, copy);
            float target = Mathf.Max(pb.size.x, pb.size.z) * 0.38f * vicScale[v.Boss].Value;   // Trophaee etwa 38 % der Plattenbreite
            float size = Mathf.Max(tb.size.x, Mathf.Max(tb.size.y, tb.size.z));
            float k = size > 0.001f ? target / size : 1f;
            deco.transform.localScale = Vector3.one * k;
            // mittig, nach hinten versetzt (Config), Unterkante auf Hoehe der Plattenoberseite (etwa 55 % der Plattenhoehe) plus Hoehe aus der Config
            float topY = pb.min.y + pb.size.y * 0.55f + vicHeight[v.Boss].Value * target;
            deco.transform.localPosition = new Vector3(pb.center.x + pb.extents.x * vicSide[v.Boss].Value - tb.center.x * k, topY - tb.min.y * k, pb.center.z + pb.extents.z * vicBack[v.Boss].Value - tb.center.z * k);
        }

        private static Bounds LocalBounds(Transform space, GameObject root)
        {
            Bounds b = new Bounds(); bool have = false;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null || mf.gameObject.name.StartsWith("VFP_TrophyDecoration")) continue;
                var mb = mf.sharedMesh.bounds;
                foreach (var c in new[] { mb.min, mb.max, new Vector3(mb.min.x, mb.max.y, mb.min.z), new Vector3(mb.max.x, mb.min.y, mb.max.z),
                                          new Vector3(mb.min.x, mb.min.y, mb.max.z), new Vector3(mb.max.x, mb.max.y, mb.min.z), new Vector3(mb.min.x, mb.max.y, mb.max.z), new Vector3(mb.max.x, mb.min.y, mb.min.z) })
                {
                    var p = space.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!have) { b = new Bounds(p, Vector3.zero); have = true; } else b.Encapsulate(p);
                }
            }
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                var mb = smr.sharedMesh.bounds;
                // alle 8 Ecken (vorher nur 2: nach einer Drehung lag die Mitte daneben, Moders Kopf rutschte zur Seite)
                foreach (var c in new[] { mb.min, mb.max, new Vector3(mb.min.x, mb.max.y, mb.min.z), new Vector3(mb.max.x, mb.min.y, mb.max.z),
                                          new Vector3(mb.min.x, mb.min.y, mb.max.z), new Vector3(mb.max.x, mb.max.y, mb.min.z), new Vector3(mb.min.x, mb.max.y, mb.max.z), new Vector3(mb.max.x, mb.min.y, mb.min.z) })
                {
                    var p = space.InverseTransformPoint(smr.transform.TransformPoint(c));
                    if (!have) { b = new Bounds(p, Vector3.zero); have = true; } else b.Encapsulate(p);
                }
            }
            if (!have) b = new Bounds(Vector3.zero, Vector3.one * 0.5f);
            return b;
        }

        /// <summary>Hilfe beim Pruefen der Prefab-Namen: alle Trophaeen ins Log.</summary>
        private void LogTrophyNames()
        {
            try
            {
                var names = new List<string>();
                var scene = ZNetScene.instance;
                if (scene != null) foreach (var p in scene.m_prefabs) if (p != null && p.name.StartsWith("Trophy")) names.Add(p.name);
                if (names.Count == 0) return;   // ZNetScene gibt es beim Registrieren meist noch nicht
                names.Sort(StringComparer.OrdinalIgnoreCase);
                Logger.LogInfo("Trophaeen-Prefabs (" + names.Count + "): " + string.Join(", ", names.ToArray()));
            }
            catch (Exception) { }
        }

        private void AddVictoryTranslations(Dictionary<string, string> de, Dictionary<string, string> en)
        {
            de["vfp_group_victory"] = "Siegesmähler"; en["vfp_group_victory"] = "Victory feasts";
            de["vfp_victory_locked"] = "??? – Siegesmahl"; en["vfp_victory_locked"] = "??? – Victory feast";
            de["vfp_victory_locked_desc"] = "Besiege den Boss, um dieses Siegesmahl freizuschalten."; en["vfp_victory_locked_desc"] = "Defeat the boss to unlock this victory feast.";
            de["vfp_victory_msg"] = "Ihr feiert den Sieg über"; en["vfp_victory_msg"] = "You celebrate the victory over";
            foreach (var v in Victories)
            {
                string k = v.Key;
                string stats = (v.Eitr > 0 ? v.Hp + " / " + v.Stamina + " / " + v.Eitr : v.Hp + " / " + v.Stamina);
                de[k] = v.De; en[k] = v.En;
                de[k + "_desc"] = v.DescDe + "\n\n<color=#ffd27a>Siegesmahl für die ganze Gruppe: " + v.TipDe + " (30 Minuten)</color>\nMit dem Servierbrett aufstellen.";
                en[k + "_desc"] = v.DescEn + "\n\n<color=#ffd27a>Victory feast for the whole group: " + v.TipEn + " (30 minutes)</color>\nPlace it with the Serving Tray.";
                de[k + "_eff"] = v.EffDe; en[k + "_eff"] = v.EffEn;
                de[k + "_tip"] = v.TipDe; en[k + "_tip"] = v.TipEn;
                de[k + "_hint"] = "Besiege " + v.BossDe + "."; en[k + "_hint"] = "Defeat " + v.BossEn + ".";
                de[k + "_boss"] = v.BossDe; en[k + "_boss"] = v.BossEn;
            }
        }

        /// <summary>Ist der Boss in dieser Welt besiegt? (globaler Schluessel; bei Kall auch die Phase-3-Variante)</summary>
        internal static bool BossDefeated(string key)
        {
            try
            {
                var zs = ZoneSystem.instance;
                if (zs == null || string.IsNullOrEmpty(key)) return false;
                return zs.GetGlobalKey(key) || (key == "defeated_frozenking" && zs.GetGlobalKey("defeated_frozenking_p3"));
            }
            catch (Exception) { return false; }
        }
    }

    /// <summary>Harmony-Patches der Siegesmaehler.</summary>
    public static class VictoryPatches
    {
        private static readonly MethodInfo AddKnown = AccessTools.Method(typeof(Player), "AddKnownRecipe", new[] { typeof(Recipe) });

        /// <summary>Freigeschaltete Siegesmaehler (Boss besiegt bzw. NeedBossKill aus) werden automatisch gelernt,
        /// auch wenn man die Trophaee noch nie selbst hatte.</summary>
        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList")]
        public static class LearnUnlocked
        {
            public static void Prefix()
            {
                try
                {
                    var pl = Player.m_localPlayer;
                    if (pl == null || ObjectDB.instance == null || AddKnown == null) return;
                    foreach (var vi in RecipeGroups.VictoryItems)
                    {
                        if (pl.IsRecipeKnown(vi[1])) continue;
                        if (VikingFoodPlus.VictoryNeedKill && !VikingFoodPlus.BossDefeated(vi[6])) continue;
                        foreach (var r in ObjectDB.instance.m_recipes)
                            if (r != null && r.m_item != null && r.m_item.name == vi[0]) { AddKnown.Invoke(pl, new object[] { r }); break; }
                    }
                }
                catch (Exception) { }
            }
        }

        /// <summary>Siegesmeldung beim ersten Essen eines Siegesmahls (pro Spieler und Mahl einmal, gemerkt im Spielerprofil).</summary>
        [HarmonyPatch(typeof(Player), "EatFood")]
        public static class VictoryMessage
        {
            public static void Postfix(Player __instance, ItemDrop.ItemData __0, bool __result)
            {
                try
                {
                    if (!__result || !VikingFoodPlus.VictoryShowMessage || __instance != Player.m_localPlayer || __0 == null || __0.m_dropPrefab == null) return;
                    VikingFoodPlus.VictorySpec v;
                    if (!VikingFoodPlus.VictoryByEatItem.TryGetValue(VikingFoodPlus.PrefabName(__0.m_dropPrefab), out v)) return;
                    string flag = "vfp_victory_eaten_" + v.Boss.ToLowerInvariant();
                    var data = __instance.m_customData;
                    if (data == null || data.ContainsKey(flag)) return;
                    data[flag] = "1";
                    string boss = CauldronFire.Loc("$" + v.Key + "_boss");
                    __instance.Message(MessageHud.MessageType.Center, CauldronFire.Loc("$vfp_victory_msg") + " " + boss + "!", 0, __0.GetIcon(), false);
                }
                catch (Exception) { }
            }
        }

        /// <summary>Feuerkoenigs-Tafel: +10 % Feuerschaden. Bankett der Koenigin: -15 % Schaden von Suchern, Gjall, Zecken.</summary>
        [HarmonyPatch(typeof(Character), "Damage")]
        public static class VictoryDamage
        {
            public static void Prefix(Character __instance, HitData __0)
            {
                try
                {
                    if (__0 == null) return;
                    var attacker = __0.GetAttacker();
                    if (VikingFoodPlus.VictoryFireHash != 0 && attacker is Player ap && ap.GetSEMan() != null && ap.GetSEMan().HaveStatusEffect(VikingFoodPlus.VictoryFireHash))
                        __0.m_damage.m_fire += __0.GetTotalDamage() * 0.10f;
                    if (VikingFoodPlus.VictoryQueenHash != 0 && __instance is Player vp && attacker != null && vp.GetSEMan() != null && vp.GetSEMan().HaveStatusEffect(VikingFoodPlus.VictoryQueenHash))
                    {
                        string n = VikingFoodPlus.PrefabName(attacker.gameObject).ToLowerInvariant();
                        if (n.Contains("seeker") || n.Contains("gjall") || n.Contains("tick")) __0.m_damage.Modify(0.85f);
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
