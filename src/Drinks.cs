using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace VikingFoodPlus
{
    // =====================================================================================================
    //  Getraenke (1.5.0)
    //  Einzelmets:    Met-Basis am Valheim-Metkessel (optional auch am Runenkessel), Gaeren im Valheim-Fermenter
    //                 oder im Gaerfass -> 6 Flaschen, Wirkung 10 Min.
    //  Gruppenmets:   Met-Basis am Runenkessel (Stufe 2), Gaeren nur im Gaerfass -> 1 Metfaesschen (Festmahl-System,
    //                 mit dem Servierbrett aufstellen, 10 Portionen), Wirkung 20 Min. Gelage- und Rast-Bonus.
    //  Hoechstens MaxActive Getraenke gleichzeitig (das aelteste endet).
    // =====================================================================================================
    public partial class VikingFoodPlus
    {
        private sealed class DrinkSpec
        {
            public readonly string Id, German, English, DescDe, DescEn, FxDe, FxEn, TipDe, TipEn, Ingredients;
            public readonly bool Group;
            public readonly float Strength;
            public DrinkSpec(string id, bool group, string german, string english, string descDe, string descEn,
                string fxDe, string fxEn, string tipDe, string tipEn, string ingredients, float strength)
            {
                Id = id; Group = group; German = german; English = english; DescDe = descDe; DescEn = descEn;
                FxDe = fxDe; FxEn = fxEn; TipDe = tipDe; TipEn = tipEn; Ingredients = ingredients; Strength = strength;
            }
        }

        private static readonly DrinkSpec[] Drinks = new DrinkSpec[] {
            // ---- Einzelmets
            new DrinkSpec("Fisher", false, "Fischerbräu", "Fisher's brew",
                "Riecht nach Köder. Die Fische mögen das.", "Smells of bait. The fish like that.",
                "Anglerglück", "Angler's luck", "Fische beißen schneller an.", "Fish bite more readily.",
                "Honey:10,FishRaw:3,FishingBait:10,Dandelion:5", 0.25f),
            new DrinkSpec("Cook", false, "Kochmeister-Bräu", "Master cook's brew",
                "Ein Schluck, und jedes Mahl sättigt länger.", "One sip, and every meal lasts longer.",
                "Feinschmecker", "Gourmet", "Gegessene Gerichte halten 10 % länger.", "Eaten food lasts 10% longer.",
                "Honey:10,MushroomYellow:6,Carrot:6,Thistle:4", 0.10f),
            new DrinkSpec("Hunter", false, "Jägerbräu", "Hunter's brew",
                "Ruhige Hand, schneller Pfeil.", "Steady hand, swift arrow.",
                "Ruhige Hand", "Steady hand", "Bogen und Armbrust spannen 10 % schneller.", "Bows and crossbows draw 10% faster.",
                "Honey:10,Blueberries:10,RawMeat:3,Feathers:5", 0.10f),
            new DrinkSpec("Bog", false, "Moor-Bitter", "Bog bitter",
                "Bitter wie der Sumpf, aber er hält dich trocken.", "Bitter as the swamp, but it keeps you dry.",
                "Trocken", "Dry", "Kein Nass-Effekt.", "No wet effect.",
                "Honey:10,Thistle:6,Turnip:6,Guck:4", 1f),
            new DrinkSpec("Helmsman", false, "Steuermanns-Grog", "Helmsman's grog",
                "Der Wind dreht sich, der Steuermann nicht.", "The wind turns, the helmsman does not.",
                "Windfänger", "Wind catcher", "Seitenwind treibt das Schiff 20 % besser an (als Steuermann).", "Side wind drives the ship 20% better (when steering).",
                "Honey:10,SerpentScale:3,Onion:8,Resin:6", 0.20f),
            new DrinkSpec("Gardener", false, "Gärtnermet", "Gardener's mead",
                "Aus der Ernte gebraut, für die nächste Ernte getrunken.", "Brewed from the harvest, drunk for the next one.",
                "Grüner Daumen", "Green thumb", "20 % Chance auf eine Extra-Portion beim Ernten und Pflücken.", "20% chance of an extra yield when harvesting and picking.",
                "Honey:10,Carrot:5,Turnip:5,Barley:8", 0.20f),
            new DrinkSpec("Galdr", false, "Galdr-Met", "Galdr mead",
                "Die Runen kosten weniger, wenn der Kopf leicht ist.", "Runes cost less when the head is light.",
                "Galdr", "Galdr", "Zauber kosten 10 % weniger Eitr.", "Spells cost 10% less eitr.",
                "Honey:10,Sap:6,MushroomMagecap:6,RoyalJelly:4", 0.10f),
            // ---- Gruppenmets (Metfaesschen)
            new DrinkSpec("Shieldbrothers", true, "Schildbrüder-Met", "Shield brothers' mead",
                "Wer zusammen trinkt, steht zusammen.", "Those who drink together stand together.",
                "Schildwall", "Shield wall", "-5 % erlittener Schaden, solange ein Mitspieler in der Nähe ist.", "-5% damage taken while a fellow player is nearby.",
                "Honey:20,Onion:10,WolfMeat:8,TrophyWolf:2", 0.05f),
            new DrinkSpec("Battlecry", true, "Schlachtruf-Met", "Battle cry mead",
                "Ein Horn für jeden, ein Schrei für alle.", "A horn for each, a roar for all.",
                "Schlachtruf", "Battle cry", "+5 % Schaden, solange ein Mitspieler in der Nähe ist.", "+5% damage while a fellow player is nearby.",
                "Honey:20,Bloodbag:8,Cloudberry:10,TrophyGoblin:1", 0.05f),
            new DrinkSpec("Skald", true, "Skaldenbräu", "Skald's brew",
                "Jede Geschichte wird besser mit jedem Becher.", "Every story gets better with every cup.",
                "Skaldenlied", "Skald's song", "Fertigkeiten steigen 10 % schneller.", "Skills rise 10% faster.",
                "Honey:20,Cloudberry:15,Barley:15,TrophyLox:1", 0.10f),
            new DrinkSpec("Rower", true, "Ruderer-Fass", "Rower's keg",
                "Je mehr Hände am Ruder, desto schneller das Boot.", "The more hands at the oars, the faster the boat.",
                "Rudertakt", "Oar rhythm", "+3 % Segelgeschwindigkeit pro Spieler an Bord (höchstens +12 %).", "+3% sailing speed per player on board (max +12%).",
                "Honey:20,SerpentMeat:4,Barley:12,Resin:8", 0.03f),
        };

        // Farbe je Getraenk (wie im Icon): fuer das 3D-Modell der Flasche
        private static readonly Dictionary<string, Color> DrinkColors = new Dictionary<string, Color> {
            {"Fisher", new Color(0.15f, 0.55f, 1.00f)}, {"Cook", new Color(1.00f, 0.55f, 0.05f)}, {"Hunter", new Color(0.25f, 0.25f, 1.00f)},
            {"Bog", new Color(0.45f, 0.75f, 0.05f)}, {"Helmsman", new Color(0.85f, 0.30f, 0.05f)}, {"Gardener", new Color(0.35f, 0.95f, 0.15f)},
            {"Galdr", new Color(0.70f, 0.25f, 1.00f)}
        };

        private static readonly Dictionary<string, Color> KegColors = new Dictionary<string, Color> {
            {"Shieldbrothers", new Color(0.62f, 0.66f, 0.78f)}, {"Battlecry", new Color(0.95f, 0.12f, 0.10f)},
            {"Skald", new Color(1.00f, 0.80f, 0.10f)}, {"Rower", new Color(0.10f, 0.50f, 1.00f)}
        };
        private EffectList drinkStartEffects;
        private ConfigEntry<bool> drinksEnabled;
        private ConfigEntry<bool> drinksAtMeadKetill;
        private ConfigEntry<bool> drinksAtRuneCauldron;
        private ConfigEntry<bool> drinksInVanillaFermenter;
        private ConfigEntry<float> drinkFermentTime;
        private ConfigEntry<int> drinkMaxActive;
        private ConfigEntry<float> drinkGroupBonus;
        private ConfigEntry<float> drinkRestBonus;
        private ConfigEntry<float> drinkRowerMax;
        private ConfigEntry<bool> recipeGroups;
        private ConfigEntry<string> sortButtons;
        private ConfigEntry<bool> showFermentTime;
        private ConfigEntry<int> drinkSound, kegSound;
        private readonly Dictionary<int, EffectList> drinkSounds = new Dictionary<int, EffectList>();
        internal static bool ShowFermentTime = true;
        private readonly Dictionary<string, ConfigEntry<bool>> drinkEnabled = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<string>> drinkIngredients = new Dictionary<string, ConfigEntry<string>>();
        private readonly Dictionary<string, ConfigEntry<float>> drinkStrength = new Dictionary<string, ConfigEntry<float>>();
        private readonly Dictionary<string, ConfigEntry<float>> drinkDuration = new Dictionary<string, ConfigEntry<float>>();

        internal static VikingFoodPlus Instance;
        internal static int MaxActiveDrinks = 2;
        internal static float GroupBonus = 0.05f, RestBonus = 0.5f, RowerMax = 0.12f;
        private Harmony harmony;
        private const string BarrelId = "VFP_FermentBarrel";

        private void BindDrinkConfig()
        {
            drinksEnabled = Config.Bind("Drinks", "Enabled", true, "Getraenke (Einzelmets und Metfaesschen). Neustart noetig.");
            drinksAtMeadKetill = Config.Bind("Drinks", "SinglesAtMeadKetill", true,
                "Met-Basis der Einzelmets am Valheim-Metkessel herstellen (Standard). Neustart noetig.");
            drinksAtRuneCauldron = Config.Bind("Drinks", "SinglesAtRuneCauldron", false,
                "Met-Basis der Einzelmets zusaetzlich am Runenkessel herstellen. Neustart noetig.");
            drinksInVanillaFermenter = Config.Bind("Drinks", "SinglesInVanillaFermenter", true,
                "Einzelmets im Valheim-Fermenter gaeren (Standard). Im Gaerfass gehen sie immer. Neustart noetig.");
            drinkFermentTime = Config.Bind("Drinks", "FermentTime", 0f,
                new ConfigDescription("Gaerzeit im Gaerfass in Sekunden. 0 = wie der Valheim-Fermenter. Neustart noetig.", new AcceptableValueRange<float>(0f, 20000f)));
            drinkMaxActive = Config.Bind("Drinks", "MaxActive", 2,
                new ConfigDescription("Hoechstens so viele Getraenke gleichzeitig. Beim naechsten endet das aelteste. Sofort wirksam.", new AcceptableValueRange<int>(1, 11)));
            drinkGroupBonus = Config.Bind("Drinks", "GroupBonus", 0.05f,
                new ConfigDescription("Gelage-Bonus: zusaetzliche Staerke der Metfaesschen, wenn beim Trinken mind. 3 Spieler in der Naehe sind (0.05 = +5 %). Neustart noetig.", new AcceptableValueRange<float>(0f, 0.5f)));
            drinkRestBonus = Config.Bind("Drinks", "RestBonus", 0.5f,
                new ConfigDescription("Rast-Bonus der Metfaesschen: so viel schneller wird man ausgeruht (0.5 = 50 % schneller). Neustart noetig.", new AcceptableValueRange<float>(0f, 2f)));
            drinkRowerMax = Config.Bind("Drinks", "RowerMaxBonus", 0.12f,
                new ConfigDescription("Ruderer-Fass: hoechstes Tempo-Plus (0.12 = +12 %). Neustart noetig.", new AcceptableValueRange<float>(0f, 1f)));
            drinkSound = Config.Bind("Drinks", "DrinkSound", 1,
                new ConfigDescription("Trinkgeraeusch der Einzelmets: 0 = aus, 1 = Ruelpser, 2 = Trinken + Ruelpser. Neustart noetig.", new AcceptableValueRange<int>(0, 2)));
            kegSound = Config.Bind("Drinks", "KegSound", 2,
                new ConfigDescription("Trinkgeraeusch der Metfaesschen: 0 = aus, 1 = Ruelpser, 2 = Trinken + Ruelpser. Neustart noetig.", new AcceptableValueRange<int>(0, 2)));
            showFermentTime = Config.Bind("Drinks", "ShowFermentTime", true,
                "Gaerfass: beim Drueberschauen die Restzeit bis fertig anzeigen. Sofort wirksam.");
            recipeGroups = Config.Bind("Drinks", "RecipeGroups", true,
                "Runenkessel: Rezepte in die aufklappbaren Gruppen Essen und Met teilen. Neustart noetig.");
            sortButtons = Config.Bind("General", "SortButtons", "RuneCauldron",
                new ConfigDescription("Sortier-Knoepfe (Gesundheits-, Ausdauer-, Eitr-Trank) oben rechts im Herstellmenue: Off = aus, RuneCauldron = nur am Runenkessel, " +
                    "Cooking = an allen Koch-Stationen (Runenkessel, Kessel, Zubereitungstisch, Ofen), All = an jeder Werkbank/Station. Sofort wirksam.",
                    new AcceptableValueList<string>("Off", "RuneCauldron", "Cooking", "All")));
            RecipeGroups.SortScope = sortButtons.Value;
            sortButtons.SettingChanged += (s, e) => RecipeGroups.SortScope = sortButtons.Value;
            foreach (var d in Drinks)
            {
                string sec = "Drink - " + d.Id;
                drinkEnabled[d.Id] = Config.Bind(sec, "Enabled", true, "Getraenk an/aus. Neustart noetig.");
                drinkIngredients[d.Id] = Config.Bind(sec, "Ingredients", d.Ingredients, "Zutaten der Met-Basis als Prefab:Anzahl, kommagetrennt. Neustart noetig.");
                drinkStrength[d.Id] = Config.Bind(sec, "Strength", d.Strength,
                    new ConfigDescription("Staerke des Effekts (0.1 = 10 %). Neustart noetig.", new AcceptableValueRange<float>(0f, 2f)));
                drinkDuration[d.Id] = Config.Bind(sec, "Duration", d.Group ? 1200f : 600f,
                    new ConfigDescription("Wirkungsdauer in Sekunden. Neustart noetig.", new AcceptableValueRange<float>(30f, 7200f)));
            }
        }

        private void ApplyDrinkStatics()
        {
            Instance = this;
            MaxActiveDrinks = drinkMaxActive.Value;
            GroupBonus = drinkGroupBonus.Value;
            RestBonus = drinkRestBonus.Value;
            RowerMax = drinkRowerMax.Value;
            ShowFermentTime = showFermentTime.Value;
            showFermentTime.SettingChanged += (s2, e2) => ShowFermentTime = showFermentTime.Value;
            drinkMaxActive.SettingChanged += (s, e) => MaxActiveDrinks = drinkMaxActive.Value;
            try
            {
                if (harmony == null) harmony = new Harmony(Guid);
                foreach (var t in typeof(DrinkPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    try { harmony.CreateClassProcessor(t).Patch(); }
                    catch (Exception ex) { Logger.LogWarning("Getraenke-Patch " + t.Name + " nicht aktiv: " + ex.Message); }
                }
            }
            catch (Exception ex) { Logger.LogError("Getraenke-Patches: " + ex); }
        }

        // -------------------------------------------------------------------------------------------------
        //  Registrierung
        // -------------------------------------------------------------------------------------------------
        private void RegisterDrinks()
        {
            var baseTemplate = FindVanillaItem("MeadBaseTasty", "MeadBase");
            var drinkTemplateSe = FindVanillaItem("MeadTasty", "Mead");
            drinkStartEffects = null;
            if (drinkTemplateSe != null)
            {
                var tse = drinkTemplateSe.GetComponent<ItemDrop>().m_itemData.m_shared.m_consumeStatusEffect;
                if (tse != null) drinkStartEffects = tse.m_startEffects;
                if (drinkStartEffects != null && drinkStartEffects.m_effectPrefabs != null)
                    foreach (var e in drinkStartEffects.m_effectPrefabs)
                        if (e != null && e.m_prefab != null) Logger.LogInfo("Getraenke: Trinkeffekt " + e.m_prefab.name);
            }
            var drinkTemplate = FindVanillaItem("MeadTasty", "Mead");
            if (baseTemplate == null || drinkTemplate == null) throw new Exception("Valheim-Met (MeadBaseTasty/MeadTasty) nicht gefunden");
            string ketill = drinksAtMeadKetill.Value ? ResolveMeadKetill() : null;
            var barrel = PrefabManager.Instance.GetPrefab(BarrelId);
            var barrelFermenter = barrel != null ? barrel.GetComponent<Fermenter>() : null;
            if (barrelFermenter == null) Logger.LogWarning("Getraenke: Gaerfass ohne Gaer-Funktion, Metfaesschen nicht brau-bar");
            var vanillaFermenterGo = drinksInVanillaFermenter.Value ? PrefabManager.Instance.GetPrefab("fermenter") : null;
            var vanillaFermenter = vanillaFermenterGo != null ? vanillaFermenterGo.GetComponent<Fermenter>() : null;

            PieceTable feastTable = null; string feastTableName = null;
            var feaster = PrefabManager.Instance.GetPrefab("Feaster");
            var tray = feaster != null ? feaster.GetComponent<ItemDrop>() : null;
            if (tray != null && tray.m_itemData.m_shared.m_buildPieces != null)
            { feastTable = tray.m_itemData.m_shared.m_buildPieces; feastTableName = feastTable.gameObject.name; }

            foreach (var d in Drinks)
            {
                if (!drinkEnabled[d.Id].Value) continue;
                try
                {
                    var reqs = ParseIngredients(drinkIngredients[d.Id].Value);
                    var se = CreateDrinkEffect(d);
                    if (!d.Group)
                    {
                        var mead = CloneItem("VFP_Mead_" + d.Id, drinkTemplate, "vfp_mead_" + d.Id.ToLowerInvariant(), "Mead_" + d.Id);
                        mead.GetComponent<ItemDrop>().m_itemData.m_shared.m_consumeStatusEffect = se;
                        try { ApplyFlaskModel(mead, DrinkColors.ContainsKey(d.Id) ? DrinkColors[d.Id] : Color.yellow); }
                        catch (Exception ex) { Logger.LogWarning("Flaschen-Modell " + d.Id + ": " + ex.Message); }
                        if (se.m_icon == null) se.m_icon = FirstIcon(mead);
                        ItemManager.Instance.AddItem(new CustomItem(mead, true));
                        var mbase = CloneItem("VFP_MeadBase_" + d.Id, baseTemplate, "vfp_meadbase_" + d.Id.ToLowerInvariant(), "MeadBase_" + d.Id);
                        mbase.GetComponent<ItemDrop>().m_itemData.m_shared.m_appendToolTip = mead.GetComponent<ItemDrop>();
                        ItemManager.Instance.AddItem(new CustomItem(mbase, true));
                        if (ketill != null) AddDrinkRecipe(mbase.name, ketill, 1, reqs, "");
                        if (drinksAtRuneCauldron.Value || ketill == null) AddDrinkRecipe(mbase.name, Station, 1, reqs, "_Rune");
                        AddConversion(barrelFermenter, mbase, mead, 6);
                        AddConversion(vanillaFermenter, mbase, mead, 6);
                    }
                    else
                    {
                        if (feastTable == null) throw new Exception("Servierbrett (Feaster) nicht gefunden");
                        var kbase = CloneItem("VFP_KegBase_" + d.Id, baseTemplate, "vfp_kegbase_" + d.Id.ToLowerInvariant(), "KegBase_" + d.Id);
                        ItemManager.Instance.AddItem(new CustomItem(kbase, true));
                        ItemDrop kegEat;
                        var kegMat = RegisterKeg(d, se, feastTable, feastTableName, out kegEat);
                        kbase.GetComponent<ItemDrop>().m_itemData.m_shared.m_appendToolTip = kegEat;
                        AddDrinkRecipe(kbase.name, Station, 2, reqs, "");
                        AddConversion(barrelFermenter, kbase, kegMat, 1);
                    }
                }
                catch (Exception ex) { Logger.LogWarning("Getraenk " + d.Id + " übersprungen: " + ex.Message); }
            }
        }

        private GameObject FindVanillaItem(string exact, string prefix)
        {
            var go = PrefabManager.Instance.GetPrefab(exact);
            if (go != null) return go;
            if (ObjectDB.instance != null)
                foreach (var it in ObjectDB.instance.m_items)
                    if (it != null && it.name.StartsWith(prefix) && it.GetComponent<ItemDrop>() != null) return it;
            return null;
        }

        private string ResolveMeadKetill()
        {
            if (PrefabManager.Instance.GetPrefab("piece_MeadCauldron") != null) return "piece_MeadCauldron";
            var scene = ZNetScene.instance;
            if (scene != null)
                foreach (var prefab in scene.m_prefabs)
                {
                    var cs = prefab != null ? prefab.GetComponent<CraftingStation>() : null;
                    if (cs == null) continue;
                    var n = (prefab.name + " " + cs.m_name).ToLowerInvariant();
                    if (n.Contains("meadcauldron") || n.Contains("meadketill") || n.Contains("ketill")) return prefab.name;
                }
            Logger.LogWarning("Getraenke: Metkessel nicht gefunden, Einzelmets werden am Runenkessel gebraut");
            return null;
        }

        private RequirementConfig[] ParseIngredients(string spec)
        {
            var list = new List<RequirementConfig>();
            foreach (var part in spec.Split(','))
            {
                var t = part.Trim().Split(':');
                int n;
                if (t.Length != 2 || !int.TryParse(t[1], out n)) throw new Exception("Ungueltige Zutat " + part);
                if (PrefabManager.Instance.GetPrefab(t[0]) == null) throw new Exception("Zutat fehlt: " + t[0]);
                list.Add(new RequirementConfig(t[0], n));
            }
            return list.ToArray();
        }

        private GameObject CloneItem(string id, GameObject template, string key, string iconId)
        {
            var go = PrefabManager.Instance.CreateClonedPrefab(id, template);
            var drop = go.GetComponent<ItemDrop>();
            if (drop == null) throw new Exception("ItemDrop fehlt an " + template.name);
            drop.m_itemData.m_shared.m_name = "$" + key;
            drop.m_itemData.m_shared.m_description = "$" + key + "_desc";
            drop.m_itemData.m_dropPrefab = go;
            // Vorlage (Tasty) bringt eigene Effekte mit -> entfernen, sonst zeigt der Tooltip "Koestlicher Met"
            drop.m_itemData.m_shared.m_consumeStatusEffect = null;
            drop.m_itemData.m_shared.m_attackStatusEffect = null;
            drop.m_itemData.m_shared.m_equipStatusEffect = null;
            drop.m_itemData.m_shared.m_appendToolTip = null;
            var icon = LoadSprite(iconId);
            if (icon != null) drop.m_itemData.m_shared.m_icons = new[] { icon };
            return go;
        }

        /// <summary>Ersetzt das Tasty-Met-Modell durch die eigene Flasche (im Kind "attach", damit sie auch am
        /// Gegenstandsstaender erscheint) und faerbt Fluessigkeit und Glas in der Farbe des Mets.</summary>
        private void ApplyFlaskModel(GameObject item, Color col)
        {
            var attach = item.transform.Find("attach") ?? item.transform;
            Material shader = null; Bounds bounds = new Bounds(); bool haveBounds = false;
            foreach (var mf in attach.GetComponentsInChildren<MeshFilter>(true))
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null) continue;
                if (shader == null && mr.sharedMaterial != null) shader = mr.sharedMaterial;
                // Groesse der Vanilla-Flasche im Raum von "attach"
                var b = mf.sharedMesh.bounds;
                foreach (var corner in new[] { b.min, b.max, new Vector3(b.min.x, b.max.y, b.min.z), new Vector3(b.max.x, b.min.y, b.max.z) })
                {
                    var pt = attach.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    if (!haveBounds) { bounds = new Bounds(pt, Vector3.zero); haveBounds = true; } else bounds.Encapsulate(pt);
                }
            }
            if (shader == null) throw new Exception("kein Material am Vanilla-Met");
            foreach (var r in attach.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var geo = ModelLoader.Build(attach.gameObject, "VFP_MeadFlask", shader);
            // Modell ist 1.39 Einheiten hoch, Boden bei y = 0: auf die Hoehe der Vanilla-Flasche skalieren
            float h = haveBounds && bounds.size.y > 0.01f ? bounds.size.y : 0.3f;
            float k = h / 1.39f;
            geo.transform.localScale = Vector3.one * k;
            geo.transform.localPosition = haveBounds ? new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) : Vector3.zero;
            // Etikett zum Betrachter drehen (Gegenstandsstaender: Pfeile zum Spieler -> Etikett vorne)
            geo.transform.localRotation = Quaternion.Euler(0f, 55f, 0f);
            foreach (var mr in geo.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = mr.sharedMaterial; if (m == null) continue;
                Color c; Color glow = Color.black;
                switch (mr.gameObject.name)
                {
                    case "Liquid": c = col; glow = col * 0.35f; break;
                    case "LiquidTop": c = Color.Lerp(col, Color.white, 0.2f); glow = col * 0.5f; break;
                    case "Glass": c = Color.Lerp(col, Color.white, 0.3f) * 0.85f; c.a = 1f; break;
                    default: continue;
                }
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                if (m.HasProperty("_EmissionColor")) { m.SetColor("_EmissionColor", glow); m.EnableKeyword("_EMISSION"); }
            }
        }

        private static Sprite FirstIcon(GameObject item)
        {
            var s = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            return s.m_icons != null && s.m_icons.Length > 0 ? s.m_icons[0] : null;
        }

        private void AddDrinkRecipe(string item, string station, int level, RequirementConfig[] reqs, string suffix)
        {
            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig {
                Name = "Recipe_" + item + suffix, Item = item, CraftingStation = station, MinStationLevel = level,
                Amount = 1, Requirements = reqs
            }));
        }

        private void AddConversion(Fermenter fermenter, GameObject from, GameObject to, int count)
        {
            if (fermenter == null) return;
            var field = typeof(Fermenter).GetField("m_conversion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var list = field != null ? field.GetValue(fermenter) as IList : null;
            if (list == null) throw new Exception("Fermenter.m_conversion fehlt");
            var type = list.GetType().GetGenericArguments()[0];
            var conv = Activator.CreateInstance(type);
            type.GetField("m_from").SetValue(conv, from.GetComponent<ItemDrop>());
            type.GetField("m_to").SetValue(conv, to.GetComponent<ItemDrop>());
            var produced = type.GetField("m_producedItems");
            if (produced != null) produced.SetValue(conv, count);
            list.Add(conv);
        }

        private StatusEffect CreateDrinkEffect(DrinkSpec d)
        {
            var se = ScriptableObject.CreateInstance<SE_VFPDrink>();
            se.name = "SE_VFP_Drink_" + d.Id;
            se.m_name = "$vfp_drinkfx_" + d.Id.ToLowerInvariant();
            se.m_tooltip = "$vfp_drinkfx_" + d.Id.ToLowerInvariant() + "_tip";
            se.m_category = "vfp_drink_" + d.Id.ToLowerInvariant();
            se.m_ttl = drinkDuration[d.Id].Value;
            var sound = GetDrinkSound(d.Group ? kegSound.Value : drinkSound.Value);
            if (sound != null) se.m_startEffects = sound;
            else if (drinkStartEffects != null) se.m_startEffects = drinkStartEffects;
            se.Kind = d.Id;
            se.Group = d.Group;
            se.BaseStrength = drinkStrength[d.Id].Value;
            se.Strength = se.BaseStrength;
            var icon = LoadSprite("Mead_" + d.Id);
            if (icon == null) icon = LoadSprite("Keg_" + d.Id);
            if (icon != null) se.m_icon = icon;
            if (d.Id == "Skald")
            {
                // Valheim kennt "alle Fertigkeiten" (SkillType.All = 999) direkt im SE_Stats
                se.m_raiseSkill = (Skills.SkillType)999;
                se.m_raiseSkillModifier = se.BaseStrength;
            }
            ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(se, false));
            return se;
        }

        // Metfaesschen = eigenes Festmahl (Servierbrett) mit Faesschen-Modell, ohne Essenswerte, gibt den Effekt
        private GameObject RegisterKeg(DrinkSpec d, StatusEffect se, PieceTable table, string tableName, out ItemDrop eatDrop)
        {
            const string vanillaBase = "FeastSwamps";
            string key = "vfp_keg_" + d.Id.ToLowerInvariant();
            string eatId = "VFP_Keg_" + d.Id, matId = "VFP_Keg_" + d.Id + "_Material", pieceId = "VFP_Keg_" + d.Id + "_Piece";
            var baseEat = PrefabManager.Instance.GetPrefab(vanillaBase);
            var baseMat = PrefabManager.Instance.GetPrefab(vanillaBase + "_Material");
            if (baseEat == null || baseMat == null) throw new Exception("Vanilla-Festmahl " + vanillaBase + " nicht gefunden");
            var icon = LoadSprite("Keg_" + d.Id);

            var eat = PrefabManager.Instance.CreateClonedPrefab(eatId, baseEat);
            SetupKegItem(eat.GetComponent<ItemDrop>(), key, icon, se);
            eat.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = eat;
            eat.GetComponent<ItemDrop>().m_itemData.m_shared.m_appendToolTip = null;
            eatDrop = eat.GetComponent<ItemDrop>();
            ItemManager.Instance.AddItem(new CustomItem(eat, true));

            var mat = PrefabManager.Instance.CreateClonedPrefab(matId, baseMat);
            SetupKegItem(mat.GetComponent<ItemDrop>(), key, icon, null);
            mat.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = mat;
            mat.GetComponent<ItemDrop>().m_itemData.m_shared.m_appendToolTip = eatDrop;
            ItemManager.Instance.AddItem(new CustomItem(mat, true));

            GameObject basePiece = null;
            foreach (var p in table.m_pieces)
            {
                var pc = p != null ? p.GetComponent<Piece>() : null;
                if (pc == null || pc.m_resources == null) continue;
                foreach (var r in pc.m_resources)
                    if (r != null && r.m_resItem != null && r.m_resItem.gameObject.name == baseMat.name) { basePiece = p; break; }
                if (basePiece != null) break;
            }
            if (basePiece == null) throw new Exception("Vanilla-Platte " + vanillaBase + " im Servierbrett nicht gefunden");
            var piece = PrefabManager.Instance.CreateClonedPrefab(pieceId, basePiece);
            ReplaceItemRefs(piece, baseEat, eat);
            ReplaceItemRefs(piece, baseMat, mat);
            var feastComp = piece.GetComponentInChildren<Feast>(true);
            if (feastComp == null) throw new Exception("Feast-Komponente fehlt");
            feastComp.m_foodItem = eat.GetComponent<ItemDrop>();
            feastComp.m_eatEffect = new EffectList();   // kein Essgeraeusch; der Trinkeffekt kommt vom Status-Effekt
            // Platte ausblenden, eigenes Faesschen-Modell zeigen
            Material shader = null;
            foreach (var r in piece.GetComponentsInChildren<MeshRenderer>(true))
                if (shader == null && r.sharedMaterial != null) shader = r.sharedMaterial;
            foreach (var r in piece.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            if (shader != null)
            {
                try
                {
                    var geo = ModelLoader.Build(piece, "VFP_Keg", shader);
                    DecorateKeg(geo, d.Id);
                }
                catch (Exception ex) { Logger.LogWarning("Faesschen-Modell: " + ex.Message); }
            }
            // Auch das Item am Boden (z. B. frisch aus dem Gaerfass) als Faesschen zeigen statt als Sumpf-Platte
            try { ApplyKegItemModel(mat, d.Id); } catch (Exception ex) { Logger.LogWarning("Faesschen-Item " + d.Id + ": " + ex.Message); }
            try { ApplyKegItemModel(eat, d.Id); } catch (Exception) { }
            var pieceComp = piece.GetComponent<Piece>();
            pieceComp.m_name = "$" + key;
            pieceComp.m_description = "$" + key + "_desc";
            if (icon != null) pieceComp.m_icon = icon;
            PieceManager.Instance.AddPiece(new CustomPiece(piece, true, new PieceConfig {
                PieceTable = tableName,
                Requirements = new[] { new RequirementConfig(matId, 1, 0, true) }
            }));
            return mat;
        }

        private void DecorateKeg(GameObject geo, string id)
        {
            AddKegSymbol(geo, id);
            Color band;
            if (KegColors.TryGetValue(id, out band))
                foreach (var mr in geo.GetComponentsInChildren<MeshRenderer>(true))
                    if (mr.gameObject.name == "K_Bands" && mr.sharedMaterial != null)
                    {
                        var m = mr.sharedMaterial;
                        if (m.HasProperty("_Color")) m.SetColor("_Color", band);
                        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", band * 0.15f);
                    }
        }

        /// <summary>Ersetzt das Weltmodell eines Faesschen-Items (Vanilla: Sumpf-Platte) durch das Faesschen,
        /// skaliert auf die Groesse der Vanilla-Platte.</summary>
        private void ApplyKegItemModel(GameObject item, string id)
        {
            Material shader = null; Bounds vb = new Bounds(); bool have = false;
            var root = item.transform;
            foreach (var mf in item.GetComponentsInChildren<MeshFilter>(true))
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null) continue;
                if (shader == null && mr.sharedMaterial != null) shader = mr.sharedMaterial;
                var b = mf.sharedMesh.bounds;
                foreach (var c in new[] { b.min, b.max, new Vector3(b.min.x, b.max.y, b.min.z), new Vector3(b.max.x, b.min.y, b.max.z),
                                          new Vector3(b.min.x, b.min.y, b.max.z), new Vector3(b.max.x, b.max.y, b.min.z) })
                {
                    var pt = root.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!have) { vb = new Bounds(pt, Vector3.zero); have = true; } else vb.Encapsulate(pt);
                }
            }
            if (shader == null || !have) throw new Exception("kein Modell am Vanilla-Item");
            foreach (var r in item.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var geo = ModelLoader.Build(item, "VFP_Keg", shader);
            DecorateKeg(geo, id);
            // Groesse des Faesschens messen und auf die breiteste Seite der Platte bringen
            Bounds gb = new Bounds(); bool g = false;
            foreach (var mf in geo.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                foreach (var c in new[] { b.min, b.max })
                {
                    var pt = geo.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!g) { gb = new Bounds(pt, Vector3.zero); g = true; } else gb.Encapsulate(pt);
                }
            }
            float target = Mathf.Max(vb.size.x, vb.size.z) * 0.85f, have_ = g ? Mathf.Max(gb.size.x, gb.size.z) : 1f;
            float k = have_ > 0.001f ? target / have_ : 1f;
            geo.transform.localScale = Vector3.one * k;
            geo.transform.localPosition = new Vector3(vb.center.x, vb.min.y - (g ? gb.min.y * k : 0f), vb.center.z);
        }

        // ---- Eigenes Trinkgeraeusch: WAV aus der DLL, abgespielt ueber einen geklonten Valheim-Sound (gleiche Lautstaerke-Regler)
        private EffectList GetDrinkSound(int variant)
        {
            if (variant <= 0) return null;
            EffectList cached;
            if (drinkSounds.TryGetValue(variant, out cached)) return cached;
            EffectList result = null;
            try
            {
                var clip = LoadWav("Drink" + variant);
                if (clip == null) return null;
                GameObject template = null;
                var player = PrefabManager.Instance.GetPrefab("Player");
                var hum = player != null ? player.GetComponent<Humanoid>() : null;
                if (hum != null && hum.m_consumeItemEffects != null && hum.m_consumeItemEffects.m_effectPrefabs != null)
                    foreach (var e in hum.m_consumeItemEffects.m_effectPrefabs)
                        if (e != null && e.m_prefab != null && e.m_prefab.GetComponentInChildren<ZSFX>(true) != null) { template = e.m_prefab; break; }
                GameObject fx;
                if (template != null)
                {
                    fx = PrefabManager.Instance.CreateClonedPrefab("sfx_VFP_Drink" + variant, template);
                    foreach (var z in fx.GetComponentsInChildren<ZSFX>(true)) { z.m_audioClips = new[] { clip }; z.m_minPitch = 0.95f; z.m_maxPitch = 1.05f; }
                    // nur der Ton: Partikel (z. B. Kruemel beim Essen) abschalten
                    foreach (var pr in fx.GetComponentsInChildren<ParticleSystemRenderer>(true)) pr.enabled = false;
                    foreach (var r in fx.GetComponentsInChildren<MeshRenderer>(true)) r.enabled = false;
                }
                else
                {
                    fx = PrefabManager.Instance.CreateEmptyPrefab("sfx_VFP_Drink" + variant, false);
                    var src = fx.AddComponent<AudioSource>();
                    src.clip = clip; src.playOnAwake = true; src.spatialBlend = 1f; src.maxDistance = 20f;
                    var td = fx.AddComponent<TimedDestruction>(); td.m_timeout = clip.length + 0.5f; td.m_triggerOnAwake = true;
                }
                PrefabManager.Instance.AddPrefab(new CustomPrefab(fx, false));
                result = new EffectList();
                result.m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = fx, m_enabled = true, m_variant = -1, m_attach = true } };
                Logger.LogInfo("Getraenke: eigenes Trinkgeraeusch " + variant + (template != null ? " (ueber " + template.name + ")" : " (AudioSource)"));
            }
            catch (Exception ex) { Logger.LogWarning("Trinkgeraeusch: " + ex.Message); }
            drinkSounds[variant] = result;
            return result;
        }

        private AudioClip LoadWav(string id)
        {
            using (var stream = typeof(VikingFoodPlus).Assembly.GetManifestResourceStream("VikingFoodPlus.Sounds." + id + ".wav"))
            {
                if (stream == null) { Logger.LogWarning("Sound fehlt: " + id); return null; }
                var bytes = new byte[stream.Length]; int off = 0;
                while (off < bytes.Length) { int r = stream.Read(bytes, off, bytes.Length - off); if (r <= 0) break; off += r; }
                int channels = BitConverter.ToInt16(bytes, 22), rate = BitConverter.ToInt32(bytes, 24), pos = 12, dataStart = -1, dataLen = 0;
                while (pos + 8 <= bytes.Length)
                {
                    string chunk = System.Text.Encoding.ASCII.GetString(bytes, pos, 4); int len = BitConverter.ToInt32(bytes, pos + 4);
                    if (chunk == "data") { dataStart = pos + 8; dataLen = Math.Min(len, bytes.Length - dataStart); break; }
                    pos += 8 + len;
                }
                if (dataStart < 0) return null;
                int samples = dataLen / 2;
                var data = new float[samples];
                for (int i = 0; i < samples; i++) data[i] = BitConverter.ToInt16(bytes, dataStart + i * 2) / 32768f;
                var clip = AudioClip.Create("VFP_" + id, samples / channels, channels, rate, false);
                clip.SetData(data, 0);
                return clip;
            }
        }

        /// <summary>Symbol des Faesschens beidseitig auf das Fass (flaches Bild, knapp ueber der Fassmitte).</summary>
        private void AddKegSymbol(GameObject geo, string id)
        {
            var sprite = LoadSprite("KegSym_" + id);
            if (sprite == null || geo == null) return;
            Shader lit = Shader.Find("Sprites/Diffuse");
            foreach (var side in new[] { 1f, -1f })
            {
                var go = new GameObject("VFP_KegSymbol");
                go.transform.SetParent(geo.transform, false);
                go.transform.localPosition = new Vector3(0f, 0.37f, 0.262f * side);
                // Sprite zeigt nach -Z: vorne (+Z) um 180 Grad drehen, damit es nicht gespiegelt ist
                go.transform.localRotation = Quaternion.Euler(0f, side > 0 ? 180f : 0f, 0f);
                float size = 0.22f / (sprite.rect.width / sprite.pixelsPerUnit);
                go.transform.localScale = new Vector3(size, size, size);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                if (lit != null) sr.material = new Material(lit);
            }
        }

        private void SetupKegItem(ItemDrop drop, string key, Sprite icon, StatusEffect se)
        {
            if (drop == null) throw new Exception("ItemDrop fehlt");
            var s = drop.m_itemData.m_shared;
            s.m_name = "$" + key;
            s.m_description = "$" + key + "_desc";
            s.m_food = 0f; s.m_foodStamina = 0f; s.m_foodEitr = 0f; s.m_foodRegen = 0f;
            if (se != null) s.m_consumeStatusEffect = se;
            if (icon != null) s.m_icons = new[] { icon };
        }

        // -------------------------------------------------------------------------------------------------
        //  Gaerfass: Klon des Valheim-Fermenters + StationExtension (Kochstufe 2), eigenes Modell mit Gaerzustaenden
        // -------------------------------------------------------------------------------------------------
        private void PrepareBarrelFermenter(GameObject go)
        {
            if (go.GetComponent<StationExtension>() == null)
            {
                var src = PrefabManager.Instance.GetPrefab("piece_workbench_ext1");
                var srcExt = src != null ? src.GetComponent<StationExtension>() : null;
                if (srcExt == null) throw new Exception("piece_workbench_ext1 / StationExtension fehlt");
                var ext = go.AddComponent<StationExtension>();
                foreach (var f in typeof(StationExtension).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    if (!f.IsStatic && !f.IsInitOnly) { try { f.SetValue(ext, f.GetValue(srcExt)); } catch (Exception) { } }
            }
        }

        private void SetupBarrelStates(GameObject go)
        {
            var fermenter = go.GetComponent<Fermenter>();
            if (fermenter == null) return;
            fermenter.m_name = "$vfp_fermentbarrel";
            // Vanilla-Zustandsobjekte des geklonten Fermenters abschalten (Ton/Partikel), eigene Modellteile nutzen
            foreach (var old in new[] { fermenter.m_topObject, fermenter.m_fermentingObject, fermenter.m_readyObject })
                if (old != null) old.SetActive(false);
            var geo = go.transform.Find("VFP_CustomGeometry");
            if (geo == null) { Logger.LogWarning("Gaerfass: Modell fehlt, Gaerzustaende nicht sichtbar"); return; }
            fermenter.m_topObject = GroupChildren(geo, "F_Top");
            fermenter.m_fermentingObject = GroupChildren(geo, "F_Fermenting");
            fermenter.m_readyObject = GroupChildren(geo, "F_Ready");
            // Ausgabe am Zapfhahn (Modell: x=-0.28 -> Unity +0.28, Hahn vorne bei z=0.7)
            var output = new GameObject("VFP_TapOutput");
            output.transform.SetParent(go.transform, false);
            output.transform.localPosition = new Vector3(0.28f, 0.5f, 0.85f);
            fermenter.m_outputPoint = output.transform;
            if (drinkFermentTime.Value > 0f) fermenter.m_fermentationDuration = drinkFermentTime.Value;
            if (go.GetComponent<VFPBarrelMarker>() == null) go.AddComponent<VFPBarrelMarker>();
        }

        private static GameObject GroupChildren(Transform geo, string name)
        {
            var parent = new GameObject(name);
            parent.transform.SetParent(geo, false);
            var move = new List<Transform>();
            foreach (Transform c in geo) if (c != parent.transform && c.name == name) move.Add(c);
            foreach (var c in move) c.SetParent(parent.transform, false);
            return parent;
        }

        private void ApplyRecipeGroups()
        {
            try
            {
                if (harmony == null) harmony = new Harmony(Guid);
                harmony.CreateClassProcessor(typeof(RecipeGroups.Patch)).Patch();
            }
            catch (Exception ex) { Logger.LogWarning("Rezept-Gruppen / Sortierung nicht aktiv: " + ex.Message); }
        }

        private void AddDrinkTranslations(Dictionary<string, string> de, Dictionary<string, string> en)
        {
            de["vfp_group_food"] = "Essen"; en["vfp_group_food"] = "Food";
            de["vfp_ferment_left"] = "Fertig in"; en["vfp_ferment_left"] = "Ready in";
            de["vfp_group_mead"] = "Met"; en["vfp_group_mead"] = "Mead";
            de["vfp_fermentbarrel_desc"] = "Erweiterung für den Runenkessel (Kochstufe 2). Lässt Met gären, auch ohne Dach. Metfässchen gibt es nur hier.";
            en["vfp_fermentbarrel_desc"] = "Runic Cauldron upgrade (station level 2). Ferments mead, even without a roof. Mead kegs can only be brewed here.";
            foreach (var d in Drinks)
            {
                string id = d.Id.ToLowerInvariant();
                if (!d.Group)
                {
                    de["vfp_mead_" + id] = d.German; en["vfp_mead_" + id] = d.English;
                    de["vfp_mead_" + id + "_desc"] = d.DescDe; en["vfp_mead_" + id + "_desc"] = d.DescEn;
                    de["vfp_meadbase_" + id] = "Met-Basis: " + d.German; en["vfp_meadbase_" + id] = "Mead base: " + d.English;
                    de["vfp_meadbase_" + id + "_desc"] = "Im Fermenter oder Gärfass zu " + d.German + " gären.";
                    en["vfp_meadbase_" + id + "_desc"] = "Ferment in a fermenter or fermentation barrel to make " + d.English + ".";
                }
                else
                {
                    de["vfp_keg_" + id] = d.German; en["vfp_keg_" + id] = d.English;
                    de["vfp_keg_" + id + "_desc"] = d.DescDe + " Mit dem Servierbrett aufstellen, 10 Portionen für alle.";
                    en["vfp_keg_" + id + "_desc"] = d.DescEn + " Place with the serving tray, 10 servings for everyone.";
                    de["vfp_kegbase_" + id] = "Fässchen-Basis: " + d.German; en["vfp_kegbase_" + id] = "Keg base: " + d.English;
                    de["vfp_kegbase_" + id + "_desc"] = "Im Gärfass zu einem Metfässchen gären.";
                    en["vfp_kegbase_" + id + "_desc"] = "Ferment in the fermentation barrel to make a mead keg.";
                }
                de["vfp_drinkfx_" + id] = d.FxDe; en["vfp_drinkfx_" + id] = d.FxEn;
                de["vfp_drinkfx_" + id + "_tip"] = d.TipDe + (d.Group ? " Gelage: stärker ab 3 Spielern. Rast: schneller ausgeruht." : "");
                en["vfp_drinkfx_" + id + "_tip"] = d.TipEn + (d.Group ? " Feast: stronger with 3+ players. Rest: rested faster." : "");
            }
        }
    }

    /// <summary>Markiert das Gaerfass (kein Dach noetig).</summary>
    public class VFPBarrelMarker : MonoBehaviour { }

    /// <summary>Getraenke-Effekt. Einfache Werte laufen ueber SE_Stats, Sonder-Effekte ueber DrinkPatches.</summary>
    public class SE_VFPDrink : SE_Stats
    {
        public string Kind = "";
        public bool Group;
        public float BaseStrength;
        public float Strength;
        private bool limitChecked;
        private static readonly FieldInfo TimeField = typeof(StatusEffect).GetField("m_time", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Setup(Character character)
        {
            base.Setup(character);
            Strength = BaseStrength;
            // Gelage-Bonus: mind. 3 Spieler (inkl. Trinker) in der Naehe
            if (Group && character != null && Kind != "Rower" && Kind != "Skald")
            {
                var list = new List<Player>();
                Player.GetPlayersInRange(character.transform.position, 10f, list);
                if (list.Count >= 3) Strength = BaseStrength + VikingFoodPlus.GroupBonus;
            }
            if (Group && Kind == "Skald" && character != null)
            {
                var list = new List<Player>();
                Player.GetPlayersInRange(character.transform.position, 10f, list);
                m_raiseSkillModifier = BaseStrength + (list.Count >= 3 ? VikingFoodPlus.GroupBonus : 0f);
            }
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            if (m_character == null) return;
            var seman = m_character.GetSEMan();
            if (seman == null) return;
            if (!limitChecked) { limitChecked = true; EnforceLimit(seman); }
            // Moor-Bitter: vorhandenen Nass-Effekt beim Trinken ablaufen lassen (neuer wird per Patch verhindert)
            if (Kind == "Bog" && TimeField != null)
            {
                var wet = seman.GetStatusEffect(SEMan.s_statusEffectWet);
                if (wet != null && wet.m_ttl > 0f) { try { TimeField.SetValue(wet, wet.m_ttl + 1f); } catch (Exception) { } }
            }
            // Rast-Bonus der Faesschen: "Ruhend" laeuft schneller ab -> schneller ausgeruht
            if (Group && VikingFoodPlus.RestBonus > 0f && TimeField != null)
            {
                var resting = seman.GetStatusEffect(SEMan.s_statusEffectResting);
                if (resting != null)
                {
                    try { TimeField.SetValue(resting, (float)TimeField.GetValue(resting) + dt * VikingFoodPlus.RestBonus); }
                    catch (Exception) { }
                }
            }
        }

        private void EnforceLimit(SEMan seman)
        {
            var mine = new List<SE_VFPDrink>();
            foreach (var se in seman.GetStatusEffects())
                if (se is SE_VFPDrink d) mine.Add(d);
            while (mine.Count > VikingFoodPlus.MaxActiveDrinks)
            {
                SE_VFPDrink oldest = null; float oldestTime = -1f;
                foreach (var d in mine)
                {
                    if (d == this) continue;
                    float t = TimeField != null ? (float)TimeField.GetValue(d) : 0f;
                    if (t > oldestTime) { oldestTime = t; oldest = d; }
                }
                if (oldest == null) break;
                // nicht direkt entfernen (laeuft gerade in SEMan.Update), sondern ablaufen lassen
                try { TimeField.SetValue(oldest, oldest.m_ttl + 1f); } catch (Exception) { }
                mine.Remove(oldest);
            }
        }

        // Schildbrueder-Met: weniger Schaden mit Mitspieler in der Naehe
        public override void OnDamaged(HitData hit, Character attacker)
        {
            base.OnDamaged(hit, attacker);
            if (Kind == "Shieldbrothers" && hit != null && DrinkPatches.AllyNearby(m_character))
                hit.ApplyModifier(1f - Strength);
        }

        // Schlachtruf-Met: mehr Schaden mit Mitspieler in der Naehe
        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            base.ModifyAttack(skill, ref hitData);
            if (Kind == "Battlecry" && hitData != null && DrinkPatches.AllyNearby(m_character))
                hitData.ApplyModifier(1f + Strength);
        }
    }

    /// <summary>Harmony-Patches fuer die Getraenke-Effekte, die Valheims Effektsystem nicht abdeckt.</summary>
    public static class DrinkPatches
    {
        internal static SE_VFPDrink Get(Character c, string kind)
        {
            if (c == null) return null;
            var seman = c.GetSEMan();
            if (seman == null) return null;
            foreach (var se in seman.GetStatusEffects())
                if (se is SE_VFPDrink d && d.Kind == kind) return d;
            return null;
        }

        internal static bool AllyNearby(Character c)
        {
            if (c == null || !c.IsPlayer()) return false;
            var list = new List<Player>();
            Player.GetPlayersInRange(c.transform.position, 10f, list);
            foreach (var p in list) if (p != null && p != (Player)c) return true;
            return false;
        }

        private static readonly MethodInfo GetOwner = AccessTools.Method(typeof(FishingFloat), "GetOwner");

        // Fischerbraeu: Fische nehmen den Koeder oefter an
        [HarmonyPatch(typeof(Fish), "TestBate")]
        public static class FishBite
        {
            public static void Postfix(FishingFloat __0, ref bool __result)
            {
                if (__result || __0 == null || GetOwner == null) return;
                Character owner = null;
                try { owner = GetOwner.Invoke(__0, null) as Character; } catch (Exception) { return; }
                var d = Get(owner, "Fisher");
                if (d != null && UnityEngine.Random.value < d.Strength) __result = true;
            }
        }

        // Kochmeister-Braeu (Feinschmecker): Essen haelt laenger
        [HarmonyPatch(typeof(Player), "UpdateFood")]
        public static class FoodDuration
        {
            public static void Prefix(Player __instance, ref float __0)
            {
                var d = Get(__instance, "Cook");
                if (d != null) __0 /= (1f + d.Strength);
            }
        }

        // Jaegerbraeu: schneller spannen
        [HarmonyPatch(typeof(Humanoid), "GetAttackDrawPercentage")]
        public static class DrawSpeed
        {
            public static void Postfix(Humanoid __instance, ref float __result)
            {
                if (__result <= 0f || __result >= 1f) return;
                var d = Get(__instance, "Hunter");
                if (d != null) __result = Mathf.Clamp01(__result * (1f + d.Strength));
            }
        }

        // Galdr-Met: weniger Eitr-Kosten
        [HarmonyPatch(typeof(Attack), "GetAttackEitr", new[] { typeof(Character), typeof(ItemDrop.ItemData) })]
        public static class EitrCost
        {
            public static void Postfix(Character __0, ref float __result)
            {
                if (__result <= 0f) return;
                var d = Get(__0, "Galdr");
                if (d != null) __result *= Mathf.Max(0f, 1f - d.Strength);
            }
        }

        // Gaertnermet: Extra-Portion beim Pflücken und Ernten
        [HarmonyPatch(typeof(Pickable), "Interact")]
        public static class ExtraHarvest
        {
            public static void Postfix(Pickable __instance, Humanoid __0, bool __result)
            {
                if (!__result || __instance == null || __instance.m_itemPrefab == null) return;
                if (__0 == null || __0 != Player.m_localPlayer) return;
                var d = Get(__0, "Gardener");
                if (d == null || UnityEngine.Random.value >= d.Strength) return;
                var pos = __instance.transform.position + Vector3.up * 0.5f;
                UnityEngine.Object.Instantiate(__instance.m_itemPrefab, pos, Quaternion.identity);
            }
        }

        // Steuermanns-Grog: Seitenwind wirkt besser, wenn der Steuermann ihn getrunken hat
        [HarmonyPatch(typeof(Ship), "GetWindAngleFactor")]
        public static class SideWind
        {
            public static void Postfix(Ship __instance, ref float __result)
            {
                var p = Player.m_localPlayer;
                if (p == null || __result >= 1f || p.GetControlledShip() != __instance) return;
                var d = Get(p, "Helmsman");
                if (d != null) __result = Mathf.Min(1f, __result * (1f + d.Strength));
            }
        }

        // Ruderer-Fass: +x % Segelkraft pro Spieler an Bord, der es getrunken hat
        private static readonly FieldInfo ShipPlayers = typeof(Ship).GetField("m_players", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPatch(typeof(Ship), "GetSailForce")]
        public static class RowerSpeed
        {
            public static void Postfix(Ship __instance, ref Vector3 __result)
            {
                var players = ShipPlayers != null ? ShipPlayers.GetValue(__instance) as IList : null;
                if (players == null || players.Count == 0) return;
                float bonus = 0f;
                foreach (var o in players)
                {
                    var d = Get(o as Character, "Rower");
                    if (d != null) bonus += d.Strength;
                }
                if (bonus > 0f) __result *= 1f + Mathf.Min(bonus, VikingFoodPlus.RowerMax);
            }
        }

        // Moor-Bitter: Nass-Effekt gar nicht erst anlegen
        private static readonly FieldInfo SemanCharacter = typeof(SEMan).GetField("m_character", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPatch]
        public static class NoWet
        {
            public static MethodBase TargetMethod()
            {
                foreach (var m in typeof(SEMan).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (m.Name != "AddStatusEffect") continue;
                    var ps = m.GetParameters();
                    if (ps.Length > 0 && ps[0].ParameterType == typeof(int)) return m;
                }
                return null;
            }

            public static bool Prefix(SEMan __instance, int __0, ref StatusEffect __result)
            {
                if (__0 != SEMan.s_statusEffectWet || SemanCharacter == null) return true;
                var c = SemanCharacter.GetValue(__instance) as Character;
                if (Get(c, "Bog") == null) return true;
                __result = null;
                return false;
            }
        }

        // Einzelmets: Valheim spielt beim Benutzen jedes Verbrauchsgegenstands das Ess-Geraeusch (m_consumeItemEffects).
        // Fuer unsere Mets kurz durch eine leere Liste ersetzen; der Trinkeffekt kommt vom Status-Effekt.
        private static readonly FieldInfo ConsumeFx = AccessTools.Field(typeof(Humanoid), "m_consumeItemEffects");
        private static readonly EffectList NoFx = new EffectList();

        [HarmonyPatch(typeof(Humanoid), "UseItem")]
        public static class NoEatSound
        {
            public static void Prefix(Humanoid __instance, ItemDrop.ItemData __1, out EffectList __state)
            {
                __state = null;
                if (__instance == null || __1 == null || ConsumeFx == null) return;
                var n = __1.m_shared.m_name;
                if (n == null || !n.StartsWith("$vfp_mead_")) return;
                __state = ConsumeFx.GetValue(__instance) as EffectList;
                ConsumeFx.SetValue(__instance, NoFx);
            }

            public static void Postfix(Humanoid __instance, EffectList __state)
            {
                if (__state != null && ConsumeFx != null) ConsumeFx.SetValue(__instance, __state);
            }
        }

        // Gaerfass: beim Hovern die Restzeit bis "fertig" anzeigen
        private static readonly MethodInfo FermStatus = AccessTools.Method(typeof(Fermenter), "GetStatus");
        private static readonly MethodInfo FermTime = AccessTools.Method(typeof(Fermenter), "GetFermentationTime");

        [HarmonyPatch(typeof(Fermenter), "GetHoverText")]
        public static class BarrelRemaining
        {
            public static void Postfix(Fermenter __instance, ref string __result)
            {
                if (!VikingFoodPlus.ShowFermentTime || __instance == null || __instance.GetComponent<VFPBarrelMarker>() == null || FermStatus == null || FermTime == null) return;
                try
                {
                    if (FermStatus.Invoke(__instance, null).ToString() != "Fermenting") return;
                    double left = __instance.m_fermentationDuration - (double)FermTime.Invoke(__instance, null);
                    if (left < 0) left = 0;
                    var t = TimeSpan.FromSeconds(left);
                    string time = t.TotalHours >= 1 ? string.Format("{0}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds) : string.Format("{0}:{1:00}", t.Minutes, t.Seconds);
                    string label = CauldronFire.Loc("$vfp_ferment_left");
                    if (string.IsNullOrEmpty(label) || label.StartsWith("[") || label.StartsWith("$")) label = "Fertig in";
                    __result += "\n" + label + ": <color=orange>" + time + "</color>";
                }
                catch (Exception) { }
            }
        }

        // Gaerfass: gaert auch ohne Dach
        private static readonly FieldInfo FermExposed = typeof(Fermenter).GetField("m_exposed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo FermRoof = typeof(Fermenter).GetField("m_hasRoof", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPatch(typeof(Fermenter), "UpdateCover")]
        public static class BarrelNoRoof
        {
            public static bool Prefix(Fermenter __instance)
            {
                if (__instance == null || __instance.GetComponent<VFPBarrelMarker>() == null) return true;
                if (FermExposed != null) FermExposed.SetValue(__instance, false);
                if (FermRoof != null) FermRoof.SetValue(__instance, true);
                return false;
            }
        }
    }
}
