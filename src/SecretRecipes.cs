using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace VikingFoodPlus
{
    /// <summary>Geheimrezepte (1.6.0): je Biom ab dem Schwarzwald ein besonderes Gericht. Das Rezept lernt man nur ueber eine
    /// Rezeptrolle, die in Truhen bestimmter Orte liegt oder bei Haendlern verkauft wird. Jedes Gericht hat einen eigenen Effekt.</summary>
    public partial class VikingFoodPlus
    {
        private sealed class SecretSpec
        {
            public string Id, De, En, Model, Ingredients, Loot, Trader, BossKey, BossDe, BossEn;
            public float Hp, Stamina, Eitr, Regen;
            public int Level;
            public string DescDe, DescEn, HintDe, HintEn, EffDe, EffEn, TipDe, TipEn, BiomeDe, BiomeEn;
            public string Effect;      // sneak, undead, fall, block, run, dodge, stagger
            public float EffectValue;
            public Color Seal;
        }

        private static readonly SecretSpec[] Secrets = new SecretSpec[] {
            new SecretSpec { Id = "GraveShroom", De = "Gefüllter Grabpilz", En = "Stuffed grave shroom", Model = "QueensJam",
                Hp = 44, Stamina = 16, Eitr = 0, Regen = 3, Level = 1, Ingredients = "MushroomYellow:4,Blueberries:5,DeerMeat:2,Dandelion:2",
                DescDe = "Randvoll mit Beeren und Fleisch. Leise genießen.", DescEn = "Stuffed to the brim with berries and meat. Enjoy quietly.",
                HintDe = "Rolle aus Hügelgräbern und Trollhöhlen, oder von Haldor nach dem Sieg über den Ältesten", HintEn = "Scroll from burial chambers and troll caves, or from Haldor after defeating The Elder",
                EffDe = "Leisetreter", EffEn = "Soft-footed", TipDe = "Schleichen kostet 25 % weniger Ausdauer.", TipEn = "Sneaking costs 25% less stamina.",
                BiomeDe = "Schwarzwald", BiomeEn = "Black Forest", Effect = "sneak", EffectValue = -0.25f,
                Loot = "forestcrypt,fcrypt,trollcave", Trader = "haldor", BossKey = "defeated_gdking", BossDe = "dem Ältesten", BossEn = "The Elder", Seal = new Color(0.25f, 0.50f, 0.25f) },
            new SecretSpec { Id = "BogBloodSausage", De = "Moorhexen-Blutwurst", En = "Bog witch blood sausage", Model = "QueensJam",
                Hp = 54, Stamina = 19, Eitr = 0, Regen = 3, Level = 2, Ingredients = "Bloodbag:3,Entrails:3,Turnip:2,Thistle:2",
                DescDe = "Die Moorhexe verrät das Rezept nicht. Du hast es trotzdem.", DescEn = "The bog witch won't share the recipe. You have it anyway.",
                HintDe = "Rolle aus versunkenen Krypten, oder von Haldor nach dem Sieg über Bonemass", HintEn = "Scroll from sunken crypts, or from Haldor after defeating Bonemass",
                EffDe = "Draugr-Schreck", EffEn = "Draugr's dread", TipDe = "+10 % Schaden gegen Untote.", TipEn = "+10% damage against undead.",
                BiomeDe = "Sumpf", BiomeEn = "Swamp", Effect = "undead", EffectValue = 0.10f,
                Loot = "sunkencrypt", Trader = "haldor", BossKey = "defeated_bonemass", BossDe = "Bonemass", BossEn = "Bonemass", Seal = new Color(0.36f, 0.42f, 0.20f) },
            new SecretSpec { Id = "FrostWolfRoulade", De = "Frostwolf-Roulade", En = "Frost wolf roulade", Model = "QueensJam",
                Hp = 22, Stamina = 64, Eitr = 0, Regen = 3, Level = 2, Ingredients = "WolfMeat:4,Onion:3,FreezeGland:1,WolfClaw:1",
                DescDe = "So kalt serviert, wie der Wolf war.", DescEn = "Served as cold as the wolf was.",
                HintDe = "Rolle aus Frosthöhlen, oder von Haldor nach dem Sieg über Moder", HintEn = "Scroll from frost caves, or from Haldor after defeating Moder",
                EffDe = "Trittsicher", EffEn = "Sure-footed", TipDe = "50 % weniger Sturzschaden.", TipEn = "50% less fall damage.",
                BiomeDe = "Berge", BiomeEn = "Mountains", Effect = "fall", EffectValue = -0.5f,
                Loot = "mountaincave", Trader = "haldor", BossKey = "defeated_dragon", BossDe = "Moder", BossEn = "Moder", Seal = new Color(0.42f, 0.55f, 0.68f) },
            new SecretSpec { Id = "FulingFriedEgg", De = "Fuling-Spiegelei", En = "Fuling fried egg", Model = "QueensJam",
                Hp = 50, Stamina = 50, Eitr = 0, Regen = 4, Level = 2, Ingredients = "ChickenEgg:3,LoxMeat:2,BarleyFlour:2,Cloudberry:2",
                DescDe = "Die Fulinge wissen gar nicht, was sie verpassen.", DescEn = "The fulings don't know what they're missing.",
                HintDe = "Rolle aus Fuling-Dörfern, oder von Haldor nach dem Sieg über Yagluth", HintEn = "Scroll from fuling villages, or from Haldor after defeating Yagluth",
                EffDe = "Schildwall", EffEn = "Shield wall", TipDe = "Blocken kostet 15 % weniger Ausdauer.", TipEn = "Blocking costs 15% less stamina.",
                BiomeDe = "Ebenen", BiomeEn = "Plains", Effect = "block", EffectValue = -0.15f,
                Loot = "heath", Trader = "haldor", BossKey = "defeated_goblinking", BossDe = "Yagluth", BossEn = "Yagluth", Seal = new Color(0.75f, 0.58f, 0.15f) },
            new SecretSpec { Id = "DvergrMushroomLoaf", De = "Dvergr-Pilzlaib", En = "Dvergr mushroom loaf", Model = "QueensJam",
                Hp = 30, Stamina = 22, Eitr = 75, Regen = 4, Level = 3, Ingredients = "BarleyFlour:4,MushroomMagecap:3,Sap:2",
                DescDe = "Die Dvergr backen im Dunkeln. Das Brot leuchtet von selbst.", DescEn = "The dvergr bake in the dark. The bread glows on its own.",
                HintDe = "Rolle aus Dvergr-Ruinen und befallenen Minen, oder von Haldor nach dem Sieg über die Königin", HintEn = "Scroll from dvergr ruins and infested mines, or from Haldor after defeating The Queen",
                EffDe = "Ausdauerläufer", EffEn = "Long runner", TipDe = "Sprinten kostet 15 % weniger Ausdauer.", TipEn = "Sprinting costs 15% less stamina.",
                BiomeDe = "Nebelland", BiomeEn = "Mistlands", Effect = "run", EffectValue = -0.15f,
                Loot = "dvergrtower,dvergrtown,dvergr_loose", Trader = "haldor", BossKey = "defeated_queen", BossDe = "der Königin", BossEn = "The Queen", Seal = new Color(0.45f, 0.25f, 0.65f) },
            new SecretSpec { Id = "SulphurSkewer", De = "Schwefelspieß", En = "Sulphur skewer", Model = "QueensJam",
                Hp = 104, Stamina = 36, Eitr = 0, Regen = 6, Level = 3, Ingredients = "AsksvinMeat:3,SulfurStone:2,Vineberry:3,Fiddleheadfern:2",
                DescDe = "Riecht nach Vulkan. Schmeckt nach mehr.", DescEn = "Smells like a volcano. Tastes like more.",
                HintDe = "Rolle aus den Festungen im Aschland, oder von Haldor nach dem Sieg über Fader", HintEn = "Scroll from the fortresses of the Ashlands, or from Haldor after defeating Fader",
                EffDe = "Flinker Krieger", EffEn = "Nimble warrior", TipDe = "Ausweichrollen kosten 20 % weniger Ausdauer.", TipEn = "Dodge rolls cost 20% less stamina.",
                BiomeDe = "Aschland", BiomeEn = "Ashlands", Effect = "dodge", EffectValue = -0.20f,
                Loot = "charredfortress,ashland_stone", Trader = "haldor", BossKey = "defeated_fader", BossDe = "Fader", BossEn = "Fader", Seal = new Color(0.70f, 0.22f, 0.15f) },
            new SecretSpec { Id = "YmirPancakeTower", De = "Ymirs Pfannkuchenturm", En = "Ymir's pancake tower", Model = "QueensJam",
                Hp = 40, Stamina = 88, Eitr = 32, Regen = 5, Level = 4, Ingredients = "Oat:5,OatMilk:2,ChickenEgg:2,Lingonberry:4",
                DescDe = "Ein Turm, so hoch wie Ymirs Hunger.", DescEn = "A tower as tall as Ymir's hunger.",
                HintDe = "Rolle aus Truhen im Hohen Norden, oder von Haldor nach dem Sieg über Kall Fimbulbringer", HintEn = "Scroll from chests in the Deep North, or from Haldor after defeating Kall Fimbulbringer",
                EffDe = "Fester Stand", EffEn = "Firm stance", TipDe = "Du taumelst 25 % seltener.", TipEn = "You stagger 25% less.",
                BiomeDe = "Hoher Norden", BiomeEn = "Deep North", Effect = "stagger", EffectValue = -0.25f,
                Loot = "deepnorth,morkhalla", Trader = "haldor", BossKey = "defeated_frozenking_p3", BossDe = "Kall Fimbulbringer", BossEn = "Kall Fimbulbringer", Seal = new Color(0.30f, 0.55f, 0.75f) },
        };

        private ConfigEntry<bool> secretsEnabled, secretsNeedScroll, secretsLogChests;
        private ConfigEntry<float> scrollChance;
        private ConfigEntry<int> scrollPrice;
        private readonly Dictionary<string, ConfigEntry<bool>> secEnabled = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<float>> secHp = new Dictionary<string, ConfigEntry<float>>(), secSt = new Dictionary<string, ConfigEntry<float>>(),
            secEitr = new Dictionary<string, ConfigEntry<float>>(), secDur = new Dictionary<string, ConfigEntry<float>>(), secRegen = new Dictionary<string, ConfigEntry<float>>();
        private readonly Dictionary<string, ConfigEntry<string>> secIng = new Dictionary<string, ConfigEntry<string>>(), secLoot = new Dictionary<string, ConfigEntry<string>>(),
            secTrader = new Dictionary<string, ConfigEntry<string>>(), secBoss = new Dictionary<string, ConfigEntry<string>>();
        private readonly Dictionary<string, ConfigEntry<int>> secPortions = new Dictionary<string, ConfigEntry<int>>();

        // Laufzeitdaten fuer die Harmony-Patches
        internal static readonly Dictionary<string, string> ScrollToSecret = new Dictionary<string, string>();   // $vfp_scroll_x -> VFP_Secret_X
        internal static readonly List<SecretLoot> LootTable = new List<SecretLoot>();
        internal static readonly List<SecretTrade> TradeTable = new List<SecretTrade>();
        internal static int UndeadEffectHash;
        internal static float UndeadBonus = 0.10f;
        internal static bool AllowSecretLearn, NeedScroll = true, LogChests;
        internal static float ScrollChanceValue = 0.12f;
        internal static VikingFoodPlus Self;

        internal sealed class SecretLoot { public string[] Patterns; public GameObject Scroll; public string SecretName; }
        internal sealed class SecretTrade { public string Trader; public ItemDrop Scroll; public int Price; public string SecretName; public string Key; }

        private void BindSecretConfig()
        {
            Self = this;
            secretsEnabled = Config.Bind(SecSecret, "Enabled", true,
                "Geheimrezepte (Grabpilz, Blutwurst, Roulade, Spiegelei, Pilzlaib, Schwefelspieß, Pfannkuchenturm). Neustart noetig.");
            secretsNeedScroll = Config.Bind(SecSecret, "NeedScroll", true,
                "true = Geheimrezepte muessen per Rezeptrolle gelernt werden. false = sie werden wie normale Rezepte freigeschaltet. Neustart noetig.");
            scrollChance = Config.Bind(SecSecret, "ScrollChance", 0.12f,
                new ConfigDescription("Chance, dass eine passende Truhe beim ersten Oeffnen eine Rezeptrolle enthaelt (0.12 = 12 %). Jede Truhe wuerfelt einmal, wenn sie zum ersten Mal geoeffnet wird. Kennt man das Rezept schon, kommt die Rolle nicht mehr. Sofort wirksam.", new AcceptableValueRange<float>(0f, 1f)));
            scrollPrice = Config.Bind(SecSecret, "ScrollPrice", 250,
                new ConfigDescription("Preis einer Rezeptrolle bei Haldor in Muenzen (verfuegbar nach dem Sieg ueber den Biom-Boss). Neustart noetig.", new AcceptableValueRange<int>(1, 5000)));
            secretsLogChests = Config.Bind(SecSecret, "LogChests", false,
                "true = schreibt beim ersten Oeffnen jeder Truhe ihren Namen und den Rollen-Wurf ins Log (passt, Wurf, Rezept schon bekannt). Hilft bei der Fehlersuche und beim Anpassen der Fundorte.");
            NeedScroll = secretsNeedScroll.Value;
            ScrollChanceValue = scrollChance.Value;
            scrollChance.SettingChanged += (s, e) => ScrollChanceValue = scrollChance.Value;
            LogChests = secretsLogChests.Value;
            secretsLogChests.SettingChanged += (s, e) => LogChests = secretsLogChests.Value;
            foreach (var sp in Secrets)
            {
                string sec = "Secret - " + sp.Id;
                secEnabled[sp.Id] = Config.Bind(sec, "Enabled", true, "Dieses Geheimrezept aktivieren; Neustart noetig.");
                secHp[sp.Id] = Config.Bind(sec, "Health", sp.Hp, "Gesundheit; Neustart noetig.");
                secSt[sp.Id] = Config.Bind(sec, "Stamina", sp.Stamina, "Ausdauer; Neustart noetig.");
                secEitr[sp.Id] = Config.Bind(sec, "Eitr", sp.Eitr, "Eitr; Neustart noetig.");
                secDur[sp.Id] = Config.Bind(sec, "Duration", sp.Level == 1 ? 1500f : 1800f, "Dauer in Sekunden; Neustart noetig.");
                secRegen[sp.Id] = Config.Bind(sec, "Regen", sp.Regen, "Heilung pro Tick; Neustart noetig.");
                secIng[sp.Id] = Config.Bind(sec, "Ingredients", sp.Ingredients, "Prefab:Anzahl, kommagetrennt. Unbekannte Prefabs werden uebersprungen (Warnung im Log); Neustart noetig.");
                secPortions[sp.Id] = Config.Bind(sec, "Portions", 2, new ConfigDescription("Portionen pro Herstellung; Neustart noetig.", new AcceptableValueRange<int>(1, 10)));
                secLoot[sp.Id] = Config.Bind(sec, "LootChests", sp.Loot, "Teile von Truhen-Namen (kommagetrennt), in denen die Rolle liegen kann. Leer = in keiner Truhe. Namen per LogChests pruefen.");
                secTrader[sp.Id] = Config.Bind(sec, "SoldBy", sp.Trader, "Haendler, der die Rolle verkauft (Teil des Namens, z. B. haldor, bogwitch, hildir). Leer = keiner; Neustart noetig.");
                secBoss[sp.Id] = Config.Bind(sec, "SoldAfterBoss", sp.BossKey, "Globaler Schluessel, ab dem der Haendler die Rolle anbietet (Sieg ueber den Biom-Boss, z. B. defeated_gdking). Leer = sofort; Neustart noetig.");
            }
        }

        private void RegisterSecrets()
        {
            if (!secretsEnabled.Value) return;
            RecipeGroups.SecretsNeedScroll = NeedScroll;
            foreach (var sp in Secrets)
            {
                if (!secEnabled[sp.Id].Value) continue;
                try { RegisterSecret(sp); }
                catch (Exception ex) { Logger.LogWarning("Geheimrezept " + sp.Id + " übersprungen: " + ex.Message); }
            }
            Logger.LogInfo("Geheimrezepte: " + RecipeGroups.SecretItems.Count + " registriert, Rollen " + (NeedScroll ? "noetig" : "nicht noetig"));

        }

        private void RegisterSecret(SecretSpec sp)
        {
            string id = "VFP_Secret_" + sp.Id, key = "vfp_secret_" + sp.Id.ToLowerInvariant();
            var go = PrefabManager.Instance.CreateClonedPrefab(id, sp.Model);
            if (go == null) throw new Exception("Modell fehlt: " + sp.Model);
            var item = go.GetComponent<ItemDrop>();
            item.m_itemData.m_dropPrefab = go;   // sonst Verweis auf das Vanilla-Vorbild (falscher Name z. B. in Easy Spawner)
            var sh = item.m_itemData.m_shared;
            sh.m_name = "$" + key;
            sh.m_description = "$" + key + "_desc";
            sh.m_food = secHp[sp.Id].Value; sh.m_foodStamina = secSt[sp.Id].Value; sh.m_foodEitr = secEitr[sp.Id].Value;
            sh.m_foodBurnTime = secDur[sp.Id].Value; sh.m_foodRegen = secRegen[sp.Id].Value;
            sh.m_consumeStatusEffect = null; sh.m_appendToolTip = null;
            try { ApplyEmbeddedIcon(item, "Secret_" + sp.Id); } catch (Exception ex) { Logger.LogWarning("Icon " + sp.Id + ": " + ex.Message); }
            Sprite icon = sh.m_icons != null && sh.m_icons.Length > 0 ? sh.m_icons[0] : null;
            var se = CreateSecretEffect(sp, key, icon);
            if (se != null) sh.m_consumeStatusEffect = se;
            try { ApplyDishModel(go, "VFP_Secret_" + sp.Id); }
            catch (Exception ex) { Logger.LogWarning("Modell " + sp.Id + ": " + ex.Message); }
            ItemManager.Instance.AddItem(new CustomItem(go, true));

            var reqs = new List<RequirementConfig>();
            foreach (var part in secIng[sp.Id].Value.Split(','))
            {
                var t = part.Trim().Split(':');
                int n;
                if (t.Length != 2 || !int.TryParse(t[1], out n)) { Logger.LogWarning("Geheimrezept " + sp.Id + ": Zutat ungueltig: " + part); continue; }
                if (PrefabManager.Instance.GetPrefab(t[0]) == null) { Logger.LogWarning("Geheimrezept " + sp.Id + ": Zutat nicht gefunden, uebersprungen: " + t[0]); continue; }
                reqs.Add(new RequirementConfig(t[0], n));
            }
            if (reqs.Count == 0) throw new Exception("keine gueltigen Zutaten");
            foreach (var st in CookingStations())
                ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig {
                    Name = "Recipe_" + id + (st == Station ? "" : "_Cauldron"), Item = id, CraftingStation = st,
                    MinStationLevel = sp.Level, Amount = secPortions[sp.Id].Value, Requirements = reqs.ToArray()
                }));
            RecipeGroups.SecretItems.Add(new[] { id, "$" + key, "$" + key + "_hint", "VFP_Scroll_" + sp.Id });

            if (!NeedScroll) return;
            // Rezeptrolle
            var scroll = PrefabManager.Instance.CreateClonedPrefab("VFP_Scroll_" + sp.Id, "LeatherScraps");
            if (scroll == null) throw new Exception("Vorlage fuer die Rolle fehlt");
            var sd = scroll.GetComponent<ItemDrop>();
            var ss = sd.m_itemData.m_shared;
            ss.m_name = "$vfp_scroll_" + sp.Id.ToLowerInvariant();
            ss.m_description = "$vfp_scroll_" + sp.Id.ToLowerInvariant() + "_desc";
            ss.m_maxStackSize = 10; ss.m_weight = 0.1f; ss.m_teleportable = true; ss.m_value = 0;
            sd.m_itemData.m_dropPrefab = scroll;
            try { ApplyEmbeddedIcon(sd, "Scroll_" + sp.Id); } catch (Exception) { }
            try { ApplyScrollModel(scroll, sp.Seal); } catch (Exception ex) { Logger.LogWarning("Rollen-Modell " + sp.Id + ": " + ex.Message); }
            ItemManager.Instance.AddItem(new CustomItem(scroll, true));
            ScrollToSecret[ss.m_name] = id;
            var patterns = new List<string>();
            foreach (var p in secLoot[sp.Id].Value.Split(',')) { var q = p.Trim().ToLowerInvariant(); if (q.Length > 0) patterns.Add(q); }
            if (patterns.Count > 0) LootTable.Add(new SecretLoot { Patterns = patterns.ToArray(), Scroll = scroll, SecretName = "$" + key });
            var trader = secTrader[sp.Id].Value.Trim().ToLowerInvariant();
            if (trader.Length > 0) TradeTable.Add(new SecretTrade { Trader = trader, Scroll = sd, Price = scrollPrice.Value, SecretName = "$" + key, Key = secBoss[sp.Id].Value.Trim() });
        }

        private StatusEffect CreateSecretEffect(SecretSpec sp, string key, Sprite icon)
        {
            var se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_VFP_Secret_" + sp.Id;
            se.m_name = "$" + key + "_eff";
            se.m_tooltip = "$" + key + "_tip";
            se.m_ttl = BuffDuration;
            if (icon != null) se.m_icon = icon;
            switch (sp.Effect)
            {
                case "sneak": se.m_sneakStaminaUseModifier = sp.EffectValue; break;
                case "fall": se.m_fallDamageModifier = sp.EffectValue; break;
                case "block": se.m_blockStaminaUseModifier = sp.EffectValue; break;
                case "run": se.m_runStaminaDrainModifier = sp.EffectValue; break;
                case "dodge": se.m_dodgeStaminaUseModifier = sp.EffectValue; break;
                case "stagger": se.m_staggerModifier = sp.EffectValue; break;
                case "undead": UndeadBonus = sp.EffectValue; break;
            }
            ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(se, false));
            if (sp.Effect == "undead") UndeadEffectHash = se.NameHash();
            return se;
        }

        /// <summary>Ersetzt das Weltmodell der Rolle (Vorlage: Lederreste) durch die eigene Rolle mit Siegel in Biom-Farbe.</summary>
        private void ApplyScrollModel(GameObject item, Color seal)
        {
            Material shader = null;
            foreach (var mr in item.GetComponentsInChildren<MeshRenderer>(true)) if (shader == null && mr.sharedMaterial != null) shader = mr.sharedMaterial;
            var piece = PieceBaseMaterial("piece_workbench_ext1");
            if (piece != null) shader = piece;
            if (shader == null) throw new Exception("kein Material");
            foreach (var r in item.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var geo = ModelLoader.Build(item, "VFP_Scroll", shader);
            geo.transform.localScale = Vector3.one * 1.2f;
            foreach (var mr in geo.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = mr.sharedMaterial; if (m == null) continue;
                if (mr.gameObject.name == "Seal" && m.HasProperty("_Color")) m.SetColor("_Color", seal);
                if (mr.gameObject.name == "SealMark" && m.HasProperty("_Color")) m.SetColor("_Color", Color.Lerp(seal, Color.white, 0.35f));
            }
        }

        private void AddSecretTranslations(Dictionary<string, string> de, Dictionary<string, string> en)
        {
            de["vfp_group_secret"] = "Geheimrezepte"; en["vfp_group_secret"] = "Secret recipes";
            de["vfp_scroll_learned"] = "Geheimrezept gelernt:"; en["vfp_scroll_learned"] = "Secret recipe learned:";
            de["vfp_secret_locked"] = "??? – Geheimrezept"; en["vfp_secret_locked"] = "??? – Secret recipe";
            de["vfp_secret_locked_desc"] = "Finde die passende Rezeptrolle, um dieses Geheimrezept zu lernen."; en["vfp_secret_locked_desc"] = "Find the matching recipe scroll to learn this secret recipe.";
            de["vfp_scroll_known"] = "Dieses Rezept kennst du schon."; en["vfp_scroll_known"] = "You already know this recipe.";
            foreach (var sp in Secrets)
            {
                string k = "vfp_secret_" + sp.Id.ToLowerInvariant(), s = "vfp_scroll_" + sp.Id.ToLowerInvariant();
                de[k] = sp.De; en[k] = sp.En;
                de[k + "_desc"] = sp.DescDe; en[k + "_desc"] = sp.DescEn;
                de[k + "_eff"] = sp.EffDe; en[k + "_eff"] = sp.EffEn;
                de[k + "_tip"] = sp.TipDe; en[k + "_tip"] = sp.TipEn;
                de[k + "_hint"] = sp.HintDe; en[k + "_hint"] = sp.HintEn;
                de[s] = "Rezeptrolle: " + sp.De; en[s] = "Recipe scroll: " + sp.En;
                de[s + "_desc"] = "Geheimrezept aus dem Gebiet " + sp.BiomeDe + ". Rechtsklick, um es zu lernen. Gekocht wird am Runenkessel (Stufe " + sp.Level + ").";
                en[s + "_desc"] = "Secret recipe from the " + sp.BiomeEn + ". Right-click to learn it. Cooked at the Runic Cauldron (level " + sp.Level + ").";
            }
        }

        private readonly HashSet<string> loggedChests = new HashSet<string>();
        internal void LogInfo(string text) { Logger.LogInfo(text); }

        internal void LogChest(string n)
        {
            if (loggedChests.Add(n)) Logger.LogInfo("Truhe (LogChests): " + n);
        }

        private void ApplySecretPatches()
        {
            try
            {
                if (harmony == null) harmony = new HarmonyLib.Harmony(Guid);
                foreach (var t in typeof(SecretPatches).GetNestedTypes(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    try { harmony.CreateClassProcessor(t).Patch(); }
                    catch (Exception ex) { Logger.LogWarning("Geheimrezept-Patch " + t.Name + " nicht aktiv: " + ex.Message); }
                }
            }
            catch (Exception ex) { Logger.LogError("Geheimrezept-Patches: " + ex); }
        }

        internal static string PrefabName(GameObject go)
        {
            if (go == null) return "";
            string n = go.name;
            int i = n.IndexOf("(Clone)", StringComparison.Ordinal); if (i >= 0) n = n.Substring(0, i);
            i = n.IndexOf(" (", StringComparison.Ordinal); if (i >= 0) n = n.Substring(0, i);
            return n.Trim();
        }
    }

    /// <summary>Harmony-Patches der Geheimrezepte.</summary>
    public static class SecretPatches
    {
        private static bool IsSecretRecipe(Recipe r)
        {
            return r != null && r.m_item != null && r.m_item.name.StartsWith("VFP_Secret_");
        }

        /// <summary>Valheim schaltet Rezepte automatisch frei, sobald man die Zutaten kennt. Geheimrezepte nur per Rolle.</summary>
        [HarmonyPatch(typeof(Player), "AddKnownRecipe")]
        public static class BlockAutoLearn
        {
            public static bool Prefix(Recipe __0)
            {
                if (!VikingFoodPlus.NeedScroll || VikingFoodPlus.AllowSecretLearn) return true;
                return !IsSecretRecipe(__0);
            }
        }

        private static readonly System.Reflection.MethodInfo AddKnown = AccessTools.Method(typeof(Player), "AddKnownRecipe", new[] { typeof(Recipe) });

        /// <summary>Rechtsklick auf eine Rezeptrolle: Rezept lernen und Rolle verbrauchen.</summary>
        [HarmonyPatch(typeof(Humanoid), "UseItem")]
        public static class UseScroll
        {
            public static bool Prefix(Humanoid __instance, Inventory __0, ItemDrop.ItemData __1)
            {
                if (__1 == null || __1.m_shared == null) return true;
                string secretId;
                if (!VikingFoodPlus.ScrollToSecret.TryGetValue(__1.m_shared.m_name, out secretId)) return true;
                var pl = __instance as Player;
                if (pl == null || ObjectDB.instance == null) return false;
                Recipe recipe = null;
                foreach (var r in ObjectDB.instance.m_recipes)
                    if (r != null && r.m_item != null && r.m_item.name == secretId) { recipe = r; break; }
                if (recipe == null) return false;
                string name = recipe.m_item.m_itemData.m_shared.m_name;
                if (pl.IsRecipeKnown(name))
                {
                    pl.Message(MessageHud.MessageType.Center, "$vfp_scroll_known", 0, null, false);
                    return false;
                }
                VikingFoodPlus.AllowSecretLearn = true;
                try { if (AddKnown != null) AddKnown.Invoke(pl, new object[] { recipe }); }
                finally { VikingFoodPlus.AllowSecretLearn = false; }
                if (__0 != null) __0.RemoveOneItem(__1);
                pl.Message(MessageHud.MessageType.Center, "$vfp_scroll_learned " + name, 0, recipe.m_item.m_itemData.GetIcon(), false);
                return false;
            }
        }

        /// <summary>
        /// Rezeptrollen in Truhen: gewuerfelt beim ersten Oeffnen durch einen Spieler (nicht beim Erzeugen der Truhe).
        /// So klappt es auch auf Servern, wenn ein anderer Spieler den Dungeon geladen hat, und bei Truhen,
        /// die vor dem Update entstanden sind. Jede Truhe wuerfelt nur einmal (Merker im ZDO der Truhe).
        /// </summary>
        [HarmonyPatch]
        public static class ChestLoot
        {
            private const string RolledKey = "vfp_scroll_rolled";

            /// <summary>Valheim hat die Methode umbenannt (RPC_OpenRespons -> RPC_OpenResponse); beide Namen werden gesucht.</summary>
            public static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(Container), "RPC_OpenResponse") ?? AccessTools.Method(typeof(Container), "RPC_OpenRespons");
            }

            public static void Postfix(Container __instance, object[] __args)
            {
                try
                {
                    bool granted = false;
                    if (__args != null) foreach (var a in __args) if (a is bool b) { granted = b; break; }
                    if (!granted || __instance == null) return;
                    var nview = __instance.GetComponent<ZNetView>();
                    if (nview == null || !nview.IsValid() || !nview.IsOwner()) return;
                    var zdo = nview.GetZDO();
                    string n = VikingFoodPlus.PrefabName(__instance.gameObject).ToLowerInvariant();
                    if (n.StartsWith("piece_") || n.StartsWith("player_tombstone")) return;   // eigene Kisten / Grabsteine nie
                    if (zdo.GetBool(RolledKey, false)) { Log(n + ": schon gewuerfelt"); return; }
                    zdo.Set(RolledKey, true);
                    var inv = __instance.GetInventory();
                    if (inv == null) return;
                    var pl = Player.m_localPlayer;
                    bool any = false;
                    foreach (var l in VikingFoodPlus.LootTable)
                    {
                        bool match = false;
                        foreach (var p in l.Patterns) if (n.Contains(p)) { match = true; break; }
                        if (!match) continue;
                        any = true;
                        if (pl != null && pl.IsRecipeKnown(l.SecretName)) { Log(n + ": " + l.SecretName + " schon bekannt -> keine Rolle"); continue; }
                        float roll = UnityEngine.Random.value;
                        bool hit = roll < VikingFoodPlus.ScrollChanceValue;
                        Log(n + ": " + l.SecretName + " Wurf " + roll.ToString("0.00") + " < " + VikingFoodPlus.ScrollChanceValue.ToString("0.00") + " -> " + (hit ? "ROLLE" : "nichts"));
                        if (!hit) continue;
                        if (!inv.HaveEmptySlot()) { Log(n + ": Truhe voll, Rolle nicht hinzugefuegt"); continue; }
                        inv.AddItem(l.Scroll, 1);
                    }
                    if (!any) Log(n + ": keine passende Rolle fuer diese Truhe");
                }
                catch (Exception ex) { Debug.LogWarning("[Viking's Feast] Rollen-Truhe: " + ex.Message); }
            }

            private static void Log(string msg)
            {
                if (VikingFoodPlus.LogChests && VikingFoodPlus.Self != null) VikingFoodPlus.Self.LogInfo("Truhe (LogChests): " + msg);
            }
        }

        /// <summary>Einmal pro Spielstart alle Truhen-Prefabs ins Log schreiben (zum Pruefen der LootSources).</summary>
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ListChests
        {
            private static bool done;
            public static void Postfix(ZNetScene __instance)
            {
                if (done || __instance == null || VikingFoodPlus.Self == null) return;
                done = true;
                try
                {
                    var names = new List<string>();
                    foreach (var go in __instance.m_prefabs)
                        if (go != null && go.GetComponent<Container>() != null) names.Add(go.name);
                    names.Sort(StringComparer.OrdinalIgnoreCase);
                    VikingFoodPlus.Self.LogInfo("Truhen-Prefabs (" + names.Count + "): " + string.Join(", ", names.ToArray()));
                    var bosses = new List<string>();
                    foreach (var go in __instance.m_prefabs)
                    {
                        var ch = go != null ? go.GetComponent<Character>() : null;
                        if (ch != null && ch.m_boss) bosses.Add(go.name + "=" + ch.m_defeatSetGlobalKey);
                    }
                    VikingFoodPlus.Self.LogInfo("Bosse (Prefab=Schluessel): " + string.Join(", ", bosses.ToArray()));
                }
                catch (Exception) { }
            }
        }

        /// <summary>Haendler (z. B. Haldor, Moorhexe) verkaufen die passenden Rollen.</summary>
        [HarmonyPatch(typeof(Trader), "Start")]
        public static class TraderScrolls
        {
            public static void Postfix(Trader __instance)
            {
                try
                {
                    string n = VikingFoodPlus.PrefabName(__instance.gameObject).ToLowerInvariant();
                    foreach (var t in VikingFoodPlus.TradeTable)
                    {
                        if (!n.Contains(t.Trader)) continue;
                        bool have = false;
                        foreach (var it in __instance.m_items) if (it != null && it.m_prefab == t.Scroll) { have = true; break; }
                        if (have) continue;
                        __instance.m_items.Add(new Trader.TradeItem { m_prefab = t.Scroll, m_stack = 1, m_price = t.Price, m_requiredGlobalKey = t.Key ?? "" });
                    }
                }
                catch (Exception) { }
            }
        }

        /// <summary>Haendler bieten eine Rolle nicht mehr an, wenn man das Rezept schon kennt.</summary>
        [HarmonyPatch(typeof(Trader), "GetAvailableItems")]
        public static class HideKnownScrolls
        {
            public static void Postfix(List<Trader.TradeItem> __result)
            {
                try
                {
                    var pl = Player.m_localPlayer;
                    if (__result == null || pl == null) return;
                    foreach (var t in VikingFoodPlus.TradeTable)
                        if (pl.IsRecipeKnown(t.SecretName)) __result.RemoveAll(it => it != null && it.m_prefab == t.Scroll);
                }
                catch (Exception) { }
            }
        }

        /// <summary>Draugr-Schreck (Moorhexen-Blutwurst): mehr Schaden gegen Untote.</summary>
        [HarmonyPatch(typeof(Character), "Damage")]
        public static class UndeadBonus
        {
            public static void Prefix(Character __instance, HitData __0)
            {
                try
                {
                    if (VikingFoodPlus.UndeadEffectHash == 0 || __0 == null || __instance == null) return;
                    if (__instance.m_faction != Character.Faction.Undead) return;
                    var att = __0.GetAttacker() as Player;
                    if (att == null || !att.GetSEMan().HaveStatusEffect(VikingFoodPlus.UndeadEffectHash)) return;
                    __0.m_damage.Modify(1f + VikingFoodPlus.UndeadBonus);
                }
                catch (Exception) { }
            }
        }
    }
}
