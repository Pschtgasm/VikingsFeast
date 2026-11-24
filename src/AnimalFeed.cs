using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace VikingFoodPlus
{
    /// <summary>
    /// Tierfutter (ab 1.7.0): 6 Futter am Runenkessel (Gruppe "Tierfutter") fuer Eber, Wolf, Lox, Asksvin und Huhn.
    /// Fuettern per Hotbar-Taste (Tier ansehen) oder durch Ablegen auf dem Boden. Hoechstens 2 Futter pro Tier,
    /// das aelteste wird ersetzt. Die aktiven Futter stehen im ZDO des Tiers ("vfp_feeds") und gelten so fuer alle Spieler.
    /// </summary>
    public partial class VikingFoodPlus
    {
        internal const string SecFeed = "6 - Animal Feed";
        private ConfigEntry<bool> feedEnabled, feedGlow, feedTameCalm;
        private ConfigEntry<float> feedDuration, feedTameSpeed, feedBreedSpeed, feedStarChance, feedDamageBonus, feedArmor, feedFireReduction, feedHeal;
        private ConfigEntry<int> feedMaxStars;
        private readonly Dictionary<string, ConfigEntry<bool>> feedOn = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<string>> feedIng = new Dictionary<string, ConfigEntry<string>>();
        private readonly Dictionary<string, ConfigEntry<int>> feedPortions = new Dictionary<string, ConfigEntry<int>>();

        /// <summary>Tiere, die Futter annehmen (Prefabs der erwachsenen Tiere).</summary>
        internal static readonly string[] FeedAnimals = { "Boar", "Wolf", "Lox", "Asksvin", "Hen" };

        // Leuchtfarben: Wiese hell gelbgruen, Sumpf dunkles Moorgruen (gut unterscheidbar), Text etwas heller fuer die Lesbarkeit
        internal static readonly FeedSpec[] Feeds = {
            new FeedSpec(FeedIds.TamingHerb,   "Zähmkraut",         "Taming herb",      1, "Dandelion:4,Raspberry:4,Honey:1",
                new Color(0.70f, 1.00f, 0.25f), 1.4f, "#B8FF4D",
                "Die Wiese duftet. Das Tier wird weich.", "Smells like the Meadows. The beast softens."),
            new FeedSpec(FeedIds.FreyaLure,    "Freyas Lockfutter", "Freya's lure",     1, "Blueberries:4,Carrot:3,MushroomYellow:2",
                new Color(1.00f, 0.45f, 0.70f), 1.5f, "#FF7AB8",
                "Freya schaut weg. Die Tiere nicht.", "Freya looks away. The animals don't."),
            new FeedSpec(FeedIds.NornBlessing, "Nornensegen",       "Norns' blessing",  2, "Turnip:3,Thistle:3,Guck:1",
                new Color(0.05f, 0.40f, 0.12f), 2.4f, "#3FA34D",
                "Die Nornen weben mit. Manchmal einen Stern mehr.", "The Norns weave along. Sometimes one star more."),
            new FeedSpec(FeedIds.FenrirBite,   "Fenrirs Biss",      "Fenrir's bite",    2, "WolfMeat:2,Onion:1,WolfClaw:1",
                new Color(0.55f, 0.80f, 1.00f), 1.5f, "#8CCBFF",
                "Riecht nach Wolf. Beißt auch so.", "Smells like wolf. Bites like one too."),
            new FeedSpec(FeedIds.ArmorGrain,   "Panzerkorn",        "Armor grain",      2, "Barley:6,Flax:3,Cloudberry:3",
                new Color(1.00f, 0.80f, 0.30f), 1.4f, "#FFCC4D",
                "Hart wie die Ebene. Zäh wie ein Lox.", "Hard as the Plains. Tough as a lox."),
            new FeedSpec(FeedIds.EmberShell,   "Glutpanzer",        "Ember shell",      3, "Vineberry:4,MushroomSmokePuff:3,ProustitePowder:1",
                new Color(1.00f, 0.35f, 0.10f), 1.6f, "#FF7A33",
                "Warm von innen. Die Glut kann kommen.", "Warm inside. Let the embers come.")
        };

        private void BindFeedConfig()
        {
            feedEnabled = Config.Bind(SecFeed, "Enabled", true,
                "Tierfutter am Runenkessel (Gruppe Tierfutter) fuer Eber, Wolf, Lox, Asksvin und Huhn. Neustart noetig.");
            feedGlow = Config.Bind(SecFeed, "Glow", true,
                "Gefuetterte Tiere leuchten in der Farbe ihres Futters (bei 2 Futtern abwechselnd). false = kein Leuchten. Sofort wirksam.");
            feedDuration = Config.Bind(SecFeed, "Duration", 1800f,
                new ConfigDescription("Wirkungsdauer eines Futters in Sekunden (Spielzeit). Sofort wirksam fuer neues Futter.", new AcceptableValueRange<float>(60f, 14400f)));
            feedTameSpeed = Config.Bind(SecFeed, "TamingSpeed", 2f,
                new ConfigDescription("Zähmkraut: so viel schneller werden Wildtiere zahm (2 = doppelt so schnell). Sofort wirksam.", new AcceptableValueRange<float>(1f, 10f)));
            feedTameCalm = Config.Bind(SecFeed, "TamingCalm", true,
                "Zähmkraut: wilde Tiere werden nicht verängstigt (Zaehmen laeuft weiter) und greifen Spieler nicht an. Wer das Tier schlaegt, hebt das fuer 10 Sekunden auf. Sofort wirksam.");
            feedBreedSpeed = Config.Bind(SecFeed, "BreedingSpeed", 2f,
                new ConfigDescription("Freyas Lockfutter: Paarungschance mal diesen Wert, Tragzeit geteilt durch diesen Wert. Sofort wirksam.", new AcceptableValueRange<float>(1f, 10f)));
            feedStarChance = Config.Bind(SecFeed, "StarChance", 0.25f,
                new ConfigDescription("Nornensegen: Chance, dass ein Junges einen Stern mehr hat (0.25 = 25 %). Sofort wirksam.", new AcceptableValueRange<float>(0f, 1f)));
            feedMaxStars = Config.Bind(SecFeed, "MaxStars", 2,
                new ConfigDescription("Nornensegen: hoechstens so viele Sterne (Valheim: 2). Sofort wirksam.", new AcceptableValueRange<int>(1, 5)));
            feedDamageBonus = Config.Bind(SecFeed, "DamageBonus", 0.25f,
                new ConfigDescription("Fenrirs Biss: zusaetzlicher Schaden des Tiers (0.25 = +25 %). Sofort wirksam.", new AcceptableValueRange<float>(0f, 2f)));
            feedArmor = Config.Bind(SecFeed, "DamageReduction", 0.25f,
                new ConfigDescription("Panzerkorn: weniger erlittener Schaden (0.25 = -25 %). Sofort wirksam.", new AcceptableValueRange<float>(0f, 0.9f)));
            feedFireReduction = Config.Bind(SecFeed, "FireReduction", 0.5f,
                new ConfigDescription("Glutpanzer: weniger Feuerschaden (0.5 = halbiert). Sofort wirksam.", new AcceptableValueRange<float>(0f, 1f)));
            feedHeal = Config.Bind(SecFeed, "HealPercent", 0.02f,
                new ConfigDescription("Glutpanzer: Heilung alle 5 Sekunden in Anteilen der maximalen Gesundheit (0.02 = 2 %). Sofort wirksam.", new AcceptableValueRange<float>(0f, 0.25f)));
            foreach (var f in Feeds)
            {
                string sec = "Feed - " + f.Id;
                feedOn[f.Id] = Config.Bind(sec, "Enabled", true, "Dieses Futter an/aus. Neustart noetig.");
                feedIng[f.Id] = Config.Bind(sec, "Ingredients", f.Ingredients, "Prefab:Anzahl kommagetrennt. Neustart noetig.");
                feedPortions[f.Id] = Config.Bind(sec, "Portions", 2, new ConfigDescription("Hergestellte Menge. Neustart noetig.", new AcceptableValueRange<int>(1, 10)));
            }
            SyncFeedRules();
            Config.SettingChanged += (s, a) => SyncFeedRules();
        }

        private void SyncFeedRules()
        {
            FeedRules.Glow = feedGlow.Value;
            FeedRules.Duration = feedDuration.Value;
            FeedRules.TameSpeed = feedTameSpeed.Value;
            FeedRules.TamingCalm = feedTameCalm.Value;
            FeedRules.BreedSpeed = feedBreedSpeed.Value;
            FeedRules.StarChance = feedStarChance.Value;
            FeedRules.MaxLevel = feedMaxStars.Value + 1;
            FeedRules.DamageBonus = feedDamageBonus.Value;
            FeedRules.Armor = feedArmor.Value;
            FeedRules.FireReduction = feedFireReduction.Value;
            FeedRules.HealPercent = feedHeal.Value;
        }

        private void ApplyFeedPatches()
        {
            try
            {
                if (harmony == null) harmony = new Harmony(Guid);
                foreach (var t in typeof(FeedPatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    try { harmony.CreateClassProcessor(t).Patch(); }
                    catch (Exception ex) { Logger.LogWarning("Tierfutter-Patch " + t.Name + " nicht aktiv: " + ex.Message); }
                }
            }
            catch (Exception ex) { Logger.LogError("Tierfutter-Patches: " + ex); }
        }

        private void RegisterFeeds()
        {
            var drops = new List<ItemDrop>();
            foreach (var f in Feeds)
            {
                if (!feedOn[f.Id].Value) continue;
                try { var d = RegisterFeed(f); if (d != null) drops.Add(d); }
                catch (Exception ex) { Logger.LogWarning("Tierfutter " + f.Id + " übersprungen: " + ex.Message); }
            }
            if (drops.Count == 0) return;
            foreach (var name in FeedAnimals)
            {
                var prefab = PrefabManager.Instance.GetPrefab(name);
                if (prefab == null) { Logger.LogWarning("Tierfutter: Tier " + name + " nicht gefunden"); continue; }
                if (prefab.GetComponent<AnimalFeed>() == null) prefab.AddComponent<AnimalFeed>();
                // Hungrige Tiere laufen wie bei normalem Futter zum abgelegten Futter und fressen es
                var ai = prefab.GetComponent<MonsterAI>();
                if (ai != null && ai.m_consumeItems != null)
                    foreach (var d in drops) if (!ai.m_consumeItems.Contains(d)) ai.m_consumeItems.Add(d);
            }
            Logger.LogInfo("Tierfutter: " + drops.Count + " Futter für " + string.Join(", ", FeedAnimals) + " registriert.");
        }

        private ItemDrop RegisterFeed(FeedSpec f)
        {
            var reqs = new List<RequirementConfig>();
            foreach (var part in feedIng[f.Id].Value.Split(','))
            {
                var tokens = part.Trim().Split(':');
                int amount;
                if (tokens.Length != 2 || !int.TryParse(tokens[1], out amount)) throw new Exception("Ungueltige Zutat " + part);
                if (PrefabManager.Instance.GetPrefab(tokens[0]) == null) throw new Exception("Zutat fehlt: " + tokens[0]);
                reqs.Add(new RequirementConfig(tokens[0], amount));
            }
            string id = FeedIds.ItemPrefix + f.Id;
            // Vorlage wie bei den Gerichten: QueensJam. Hat den Befestigungspunkt "attach" und einen Item-Typ, den der
            // Objektstaender annimmt (Gerstenmehl ist "Material" und passt nicht auf den Staender).
            // Aussehen kommt vom eigenen Modell; Essen ist ausgeschlossen (Werte 0, UseItem-Patch faengt das Benutzen ab).
            string basePrefab = PrefabManager.Instance.GetPrefab("QueensJam") != null ? "QueensJam" : "BarleyFlour";
            var go = PrefabManager.Instance.CreateClonedPrefab(id, basePrefab);
            if (go == null) throw new Exception("Vorlage " + basePrefab + " fehlt");
            var drop = go.GetComponent<ItemDrop>();
            var sh = drop.m_itemData.m_shared;
            sh.m_name = "$" + f.Key;
            sh.m_description = "$" + f.Key + "_desc";
            sh.m_food = 0f; sh.m_foodStamina = 0f; sh.m_foodEitr = 0f; sh.m_foodRegen = 0f;
            sh.m_consumeStatusEffect = null;
            sh.m_maxStackSize = 20;
            sh.m_weight = 0.5f;
            var icon = LoadSprite("Feed_" + f.Id);
            if (icon != null) sh.m_icons = new[] { icon };
            drop.m_itemData.m_dropPrefab = go;
            // Eigenes Modell (tools/make_feeds.py -> Models/VFP_Feed_<Id>.obj), sonst eingefaerbter Gerstenmehl-Sack
            string model = "VFP_Feed_" + f.Id;
            bool haveModel = typeof(VikingFoodPlus).Assembly.GetManifestResourceStream("VikingFoodPlus.Models." + model + ".obj") != null;
            bool modelOk = false;
            if (haveModel)
            {
                try { ApplyDishModel(go, model); modelOk = true; }
                catch (Exception ex) { Logger.LogWarning("Modell " + model + ": " + ex.Message); }
            }
            if (!modelOk) TintModel(go, Color.Lerp(Color.white, f.Glow, 0.7f));
            ItemManager.Instance.AddItem(new CustomItem(go, true));
            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig {
                Name = "Recipe_" + id, Item = id, CraftingStation = Station, MinStationLevel = f.Level,
                Amount = feedPortions[f.Id].Value, Requirements = reqs.ToArray()
            }));
            FeedRules.ByName[sh.m_name] = f;
            return drop;
        }

        private void AddFeedTranslations(Dictionary<string, string> de, Dictionary<string, string> en)
        {
            de["vfp_group_feed"] = "Tierfutter"; en["vfp_group_feed"] = "Animal feed";
            de["vfp_feed_given"] = "Gefüttert:"; en["vfp_feed_given"] = "Fed:";
            de["vfp_feed_notarget"] = "Schau ein Tier an, um es zu füttern."; en["vfp_feed_notarget"] = "Look at an animal to feed it.";
            de["vfp_feed_wrong"] = "Dieses Tier frisst kein Tierfutter."; en["vfp_feed_wrong"] = "This animal does not eat animal feed.";
            de["vfp_feed_star"] = "Der Nornensegen wirkt: ein Junges mit einem Stern mehr!"; en["vfp_feed_star"] = "The Norns' blessing works: an offspring with one more star!";
            CultureInfo ci = CultureInfo.InvariantCulture;
            int minutes = Mathf.RoundToInt(FeedRules.Duration / 60f);
            string pctBonus = Mathf.RoundToInt(FeedRules.DamageBonus * 100f).ToString(ci), pctArmor = Mathf.RoundToInt(FeedRules.Armor * 100f).ToString(ci);
            string pctStar = Mathf.RoundToInt(FeedRules.StarChance * 100f).ToString(ci), pctFire = Mathf.RoundToInt(FeedRules.FireReduction * 100f).ToString(ci);
            string tame = FeedRules.TameSpeed.ToString("0.#", ci), breed = FeedRules.BreedSpeed.ToString("0.#", ci);
            var effDe = new Dictionary<string, string> {
                { FeedIds.TamingHerb,   "Wildtiere werden " + tame + "-mal so schnell zahm" + (FeedRules.TamingCalm ? ", bleiben ruhig und greifen nicht an." : ".") },
                { FeedIds.FreyaLure,    "Zahme Tiere paaren sich schneller (Chance x" + breed + ", Tragzeit /" + breed + ")." },
                { FeedIds.NornBlessing, pctStar + " % Chance, dass ein Junges einen Stern mehr hat." },
                { FeedIds.FenrirBite,   "+" + pctBonus + " % Schaden." },
                { FeedIds.ArmorGrain,   pctArmor + " % weniger Schaden." },
                { FeedIds.EmberShell,   pctFire + " % weniger Feuerschaden und langsame Heilung." }
            };
            var effEn = new Dictionary<string, string> {
                { FeedIds.TamingHerb,   "Wild animals tame " + tame + " times as fast" + (FeedRules.TamingCalm ? ", stay calm and do not attack." : ".") },
                { FeedIds.FreyaLure,    "Tamed animals breed faster (chance x" + breed + ", pregnancy /" + breed + ")." },
                { FeedIds.NornBlessing, pctStar + "% chance that an offspring has one more star." },
                { FeedIds.FenrirBite,   "+" + pctBonus + "% damage." },
                { FeedIds.ArmorGrain,   pctArmor + "% less damage taken." },
                { FeedIds.EmberShell,   pctFire + "% less fire damage and slow healing." }
            };
            string howDe = "\n\nFür Eber, Wolf, Lox, Asksvin und Huhn. Tier ansehen und Hotbar-Taste drücken oder auf den Boden legen. Wirkt " + minutes + " Minuten, höchstens 2 Futter pro Tier.";
            string howEn = "\n\nFor boar, wolf, lox, asksvin and hen. Look at the animal and press the hotbar key, or drop it on the ground. Lasts " + minutes + " minutes, at most 2 feeds per animal.";
            foreach (var f in Feeds)
            {
                de[f.Key] = f.German; en[f.Key] = f.English;
                de[f.Key + "_desc"] = f.DescDe + "\n\n<color=" + f.TextHex + ">" + effDe[f.Id] + "</color>" + howDe;
                en[f.Key + "_desc"] = f.DescEn + "\n\n<color=" + f.TextHex + ">" + effEn[f.Id] + "</color>" + howEn;
            }
        }

        internal sealed class FeedSpec
        {
            public readonly string Id, German, English, Ingredients, TextHex, DescDe, DescEn, Key;
            public readonly int Level;
            public readonly Color Glow;
            public readonly float Intensity;
            public FeedSpec(string id, string german, string english, int level, string ingredients, Color glow, float intensity, string textHex, string descDe, string descEn)
            { Id = id; German = german; English = english; Level = level; Ingredients = ingredients; Glow = glow; Intensity = intensity; TextHex = textHex; DescDe = descDe; DescEn = descEn; Key = "vfp_feed_" + id.ToLowerInvariant(); }
        }
    }

    internal static class FeedIds
    {
        public const string ItemPrefix = "VFP_Feed_";
        public const string TamingHerb = "TamingHerb", FreyaLure = "FreyaLure", NornBlessing = "NornBlessing",
                            FenrirBite = "FenrirBite", ArmorGrain = "ArmorGrain", EmberShell = "EmberShell";
    }

    /// <summary>Werte aus der Config (statisch, damit Patches und Komponenten sie ohne Plugin-Instanz lesen).</summary>
    internal static class FeedRules
    {
        public static bool Glow = true, TamingCalm = true;
        public static float Duration = 1800f, TameSpeed = 2f, BreedSpeed = 2f, StarChance = 0.25f, DamageBonus = 0.25f, Armor = 0.25f, FireReduction = 0.5f, HealPercent = 0.02f;
        public static int MaxLevel = 3;
        public const int MaxFeeds = 2;
        /// <summary>Item-Name ($vfp_feed_...) -> Futter.</summary>
        public static readonly Dictionary<string, VikingFoodPlus.FeedSpec> ByName = new Dictionary<string, VikingFoodPlus.FeedSpec>();
        public static readonly Dictionary<string, VikingFoodPlus.FeedSpec> ById = new Dictionary<string, VikingFoodPlus.FeedSpec>();
        static FeedRules() { foreach (var f in VikingFoodPlus.Feeds) ById[f.Id] = f; }

        public static VikingFoodPlus.FeedSpec FromItem(ItemDrop.ItemData item)
        {
            VikingFoodPlus.FeedSpec f;
            return item != null && item.m_shared != null && ByName.TryGetValue(item.m_shared.m_name, out f) ? f : null;
        }
    }

    /// <summary>
    /// Haengt an Eber, Wolf, Lox, Asksvin und Huhn. Speichert die aktiven Futter im ZDO, laesst das Tier leuchten,
    /// passt die Paarung an (Freyas Lockfutter), heilt (Glutpanzer) und frisst abgelegtes Futter auch ohne Hunger.
    /// </summary>
    public class AnimalFeed : MonoBehaviour
    {
        internal const string ZdoKey = "vfp_feeds";
        private const string RpcName = "VFP_AnimalFeed";
        private static readonly MethodInfo ResetFeeding = AccessTools.Method(typeof(Tameable), "ResetFeedingTimer");
        private static readonly FieldInfo SootheField = AccessTools.Field(typeof(Tameable), "m_sootheEffect");
        private static readonly MethodInfo SetAlertedMethod = AccessTools.Method(typeof(MonsterAI), "SetAlerted", new[] { typeof(bool) });
        private static readonly FieldInfo TargetField = AccessTools.Field(typeof(MonsterAI), "m_targetCreature");
        private static readonly FieldInfo HurtField = AccessTools.Field(typeof(BaseAI), "m_timeSinceHurt");
        /// <summary>true, waehrend AnimalFeed selbst SetAlerted(false) aufruft (Patch nicht blockieren).</summary>
        internal static bool Calming;
        private static int itemMask = -1;

        internal struct Entry { public string Id; public long Start, End; }

        private ZNetView nview;
        private Character character;
        private Tameable tameable;
        private Procreation procreation;
        private MonsterAI monsterAI;
        private float baseChance, baseDuration;
        private bool breedBoosted;
        private Light glow;
        private string cachedRaw;
        private readonly List<Entry> active = new List<Entry>();
        private float nextRead, nextHeal, nextScan;

        private void Awake()
        {
            nview = GetComponent<ZNetView>();
            character = GetComponent<Character>();
            tameable = GetComponent<Tameable>();
            procreation = GetComponent<Procreation>();
            monsterAI = GetComponent<MonsterAI>();
            if (procreation != null) { baseChance = procreation.m_pregnancyChance; baseDuration = procreation.m_pregnancyDuration; }
            if (nview != null && nview.GetZDO() != null) nview.Register<string>(RpcName, RPC_Feed);
        }

        internal static long Now()
        {
            return ZNet.instance != null ? (long)ZNet.instance.GetTimeSeconds() : 0L;
        }

        internal static List<Entry> Parse(string raw, long now)
        {
            var list = new List<Entry>();
            if (string.IsNullOrEmpty(raw)) return list;
            foreach (var part in raw.Split(';'))
            {
                var t = part.Split(':');
                long s, e;
                if (t.Length != 3 || !long.TryParse(t[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out s)
                    || !long.TryParse(t[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out e)) continue;
                if (e > now && FeedRules.ById.ContainsKey(t[0])) list.Add(new Entry { Id = t[0], Start = s, End = e });
            }
            return list;
        }

        private static string Serialize(List<Entry> list)
        {
            var parts = new List<string>();
            foreach (var e in list) parts.Add(e.Id + ":" + e.Start.ToString(CultureInfo.InvariantCulture) + ":" + e.End.ToString(CultureInfo.InvariantCulture));
            return string.Join(";", parts.ToArray());
        }

        /// <summary>Hat dieses Tier gerade das Futter id? (liest das ZDO, gilt auf allen Rechnern)</summary>
        internal static bool Has(Component c, string id)
        {
            if (c == null) return false;
            var feed = c.GetComponent<AnimalFeed>();
            if (feed == null || feed.nview == null || !feed.nview.IsValid()) return false;
            foreach (var e in Parse(feed.nview.GetZDO().GetString(ZdoKey, ""), Now())) if (e.Id == id) return true;
            return false;
        }

        /// <summary>Fuettern von irgendeinem Rechner aus: geht per RPC an den Besitzer des Tiers.</summary>
        internal void RequestFeed(string id)
        {
            if (nview == null || !nview.IsValid()) return;
            if (nview.IsOwner()) ApplyLocal(id);
            else nview.InvokeRPC(RpcName, id);
        }

        private void RPC_Feed(long sender, string id)
        {
            if (nview != null && nview.IsValid() && nview.IsOwner()) ApplyLocal(id);
        }

        /// <summary>Nur beim Besitzer: Futter eintragen. Gleiches Futter wird erneuert, bei mehr als 2 faellt das aelteste weg.</summary>
        internal void ApplyLocal(string id)
        {
            if (nview == null || !nview.IsValid() || !FeedRules.ById.ContainsKey(id)) return;
            if (character != null && character.IsDead()) return;
            long now = Now();
            var list = Parse(nview.GetZDO().GetString(ZdoKey, ""), now);
            list.RemoveAll(e => e.Id == id);
            while (list.Count >= FeedRules.MaxFeeds)
            {
                int oldest = 0;
                for (int i = 1; i < list.Count; i++) if (list[i].Start < list[oldest].Start) oldest = i;
                list.RemoveAt(oldest);
            }
            list.Add(new Entry { Id = id, Start = now, End = now + (long)FeedRules.Duration });
            nview.GetZDO().Set(ZdoKey, Serialize(list));
            nextRead = 0f;
            // Futter macht auch satt (zaehlt fuers Zaehmen und fuer die Paarung)
            if (tameable != null)
            {
                try { if (ResetFeeding != null) ResetFeeding.Invoke(tameable, null); } catch (Exception) { }
                try { var fx = SootheField != null ? SootheField.GetValue(tameable) as EffectList : null; if (fx != null) fx.Create(transform.position, transform.rotation); } catch (Exception) { }
            }
        }

        internal string HoverLines()
        {
            if (nview == null || !nview.IsValid()) return "";
            long now = Now();
            var list = Parse(nview.GetZDO().GetString(ZdoKey, ""), now);
            if (list.Count == 0) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var e in list)
            {
                var f = FeedRules.ById[e.Id];
                long left = Math.Max(0L, e.End - now);
                string time = left >= 3600 ? (left / 3600) + ":" + ((left / 60) % 60).ToString("00") + ":" + (left % 60).ToString("00")
                                           : (left / 60) + ":" + (left % 60).ToString("00");
                string name = CauldronFire.Loc("$" + f.Key);
                if (string.IsNullOrEmpty(name) || name.StartsWith("[") || name.StartsWith("$")) name = f.German;
                sb.Append("\n<color=").Append(f.TextHex).Append(">").Append(name).Append("</color> ").Append(time);
            }
            return sb.ToString();
        }

        private void Update()
        {
            if (nview == null || !nview.IsValid()) return;
            if (Time.time >= nextRead)
            {
                nextRead = Time.time + 0.5f;
                string raw = nview.GetZDO().GetString(ZdoKey, "");
                if (raw != cachedRaw || active.Count > 0)
                {
                    cachedRaw = raw;
                    active.Clear();
                    active.AddRange(Parse(raw, Now()));
                }
            }
            UpdateGlow();
            if (!nview.IsOwner()) return;
            UpdateBreeding();
            UpdateCalm();
            if (Time.time >= nextHeal)
            {
                nextHeal = Time.time + 5f;
                if (IsActive(FeedIds.EmberShell) && character != null && !character.IsDead() && FeedRules.HealPercent > 0f
                    && character.GetHealth() < character.GetMaxHealth())
                    character.Heal(character.GetMaxHealth() * FeedRules.HealPercent, true);
            }
            if (Time.time >= nextScan)
            {
                nextScan = Time.time + 1.5f;
                try { EatNearbyFeed(); } catch (Exception) { }
            }
        }

        private bool IsActive(string id)
        {
            foreach (var e in active) if (e.Id == id) return true;
            return false;
        }

        /// <summary>Zähmkraut haelt ein wildes Tier ruhig: nicht verängstigt (Zaehmen laeuft weiter), kein Angriff auf Spieler.
        /// Wer das Tier schlaegt, hebt die Wirkung fuer 10 Sekunden auf.</summary>
        internal bool IsCalm()
        {
            if (!FeedRules.TamingCalm || tameable == null || tameable.IsTamed() || !IsActive(FeedIds.TamingHerb)) return false;
            if (character != null && character.IsDead()) return false;
            if (monsterAI != null && HurtField != null)
            {
                try { if ((float)HurtField.GetValue(monsterAI) < 10f) return false; } catch (Exception) { }
            }
            return true;
        }

        private void UpdateCalm()
        {
            if (monsterAI == null || !IsCalm()) return;
            try
            {
                if (TargetField != null && TargetField.GetValue(monsterAI) is Player) TargetField.SetValue(monsterAI, null);
                if (monsterAI.IsAlerted() && SetAlertedMethod != null)
                {
                    Calming = true;
                    try { SetAlertedMethod.Invoke(monsterAI, new object[] { false }); }
                    finally { Calming = false; }
                }
            }
            catch (Exception) { }
        }

        /// <summary>Freyas Lockfutter: Paarungschance hoch, Tragzeit runter (Werte der Instanz, Vanilla-Prefab bleibt).</summary>
        private void UpdateBreeding()
        {
            if (procreation == null) return;
            bool boost = IsActive(FeedIds.FreyaLure) && FeedRules.BreedSpeed > 1f;
            if (boost)
            {
                procreation.m_pregnancyChance = Mathf.Min(1f, baseChance * FeedRules.BreedSpeed);
                procreation.m_pregnancyDuration = baseDuration / FeedRules.BreedSpeed;
                breedBoosted = true;
            }
            else if (breedBoosted)
            {
                procreation.m_pregnancyChance = baseChance;
                procreation.m_pregnancyDuration = baseDuration;
                breedBoosted = false;
            }
        }

        /// <summary>Abgelegtes Futter direkt neben dem Tier wird gefressen, auch wenn es satt ist.</summary>
        private void EatNearbyFeed()
        {
            if (character != null && character.IsDead()) return;
            if (itemMask < 0) itemMask = LayerMask.GetMask("item");
            if (itemMask == 0) return;
            foreach (var col in Physics.OverlapSphere(transform.position, 1.8f, itemMask))
            {
                var drop = col != null ? col.GetComponentInParent<ItemDrop>() : null;
                var f = drop != null ? FeedRules.FromItem(drop.m_itemData) : null;
                if (f == null) continue;
                if (!drop.RemoveOne()) continue;
                ApplyLocal(f.Id);
                return;   // ein Futter pro Durchgang
            }
        }

        private void UpdateGlow()
        {
            bool on = FeedRules.Glow && active.Count > 0 && (character == null || !character.IsDead());
            if (!on) { if (glow != null && glow.enabled) glow.enabled = false; return; }
            if (glow == null) CreateGlow();
            if (glow == null) return;
            // Zwei Futter: Farben wechseln alle 2 Sekunden, mit leichtem Pulsieren
            var f = FeedRules.ById[active[(int)(Time.time / 2f) % active.Count].Id];
            glow.color = f.Glow;
            glow.intensity = f.Intensity * (0.85f + 0.15f * Mathf.Sin(Time.time * 2.5f));
            glow.enabled = true;
        }

        private void CreateGlow()
        {
            var go = new GameObject("VFP_FeedGlow");
            go.transform.SetParent(transform, false);
            float height = 1f;
            var col = GetComponentInChildren<Collider>();
            if (col != null)
            {
                go.transform.position = col.bounds.center + Vector3.up * col.bounds.extents.y * 0.3f;
                height = col.bounds.size.y;
            }
            else go.transform.localPosition = Vector3.up;
            glow = go.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = Mathf.Clamp(2f + height * 1.5f, 2.5f, 7f);
            glow.shadows = LightShadows.None;
            glow.enabled = false;
        }
    }

    /// <summary>Harmony-Patches fuer das Tierfutter.</summary>
    public static class FeedPatches
    {
        /// <summary>Hotbar-Taste oder Rechtsklick im Inventar: angesehenes (oder naechstes) Tier fuettern.</summary>
        [HarmonyPatch(typeof(Humanoid), "UseItem")]
        public static class UseFeed
        {
            public static bool Prefix(Humanoid __instance, Inventory __0, ItemDrop.ItemData __1)
            {
                var f = FeedRules.FromItem(__1);
                if (f == null) return true;
                var pl = __instance as Player;
                if (pl == null) return false;
                try
                {
                    AnimalFeed target = null;
                    var hover = pl.GetHoverObject();
                    // Objektstaender (und andere Ablagen) angesehen: Valheim uebernimmt, das Futter wird aufgestellt
                    if (hover != null && hover.GetComponentInParent<ItemStand>() != null) return true;
                    if (hover != null)
                    {
                        target = hover.GetComponentInParent<AnimalFeed>();
                        if (target == null && hover.GetComponentInParent<Character>() != null)
                        {
                            pl.Message(MessageHud.MessageType.Center, "$vfp_feed_wrong", 0, null, false);
                            return false;
                        }
                    }
                    if (target == null) target = Nearest(pl.transform.position, 3f);
                    if (target == null)
                    {
                        pl.Message(MessageHud.MessageType.Center, "$vfp_feed_notarget", 0, null, false);
                        return false;
                    }
                    target.RequestFeed(f.Id);
                    var inv = __0 ?? pl.GetInventory();
                    if (inv != null) inv.RemoveOneItem(__1);
                    var ch = target.GetComponent<Character>();
                    string animal = ch != null ? ch.GetHoverName() : "";
                    pl.Message(MessageHud.MessageType.Center, "$vfp_feed_given " + animal, 0, __1.GetIcon(), false);
                }
                catch (Exception ex) { Debug.LogWarning("[Viking's Feast] Tierfutter: " + ex.Message); }
                return false;
            }

            private static AnimalFeed Nearest(Vector3 pos, float range)
            {
                AnimalFeed best = null; float bestD = range;
                foreach (var c in Character.GetAllCharacters())
                {
                    if (c == null || c.IsDead()) continue;
                    float d = Vector3.Distance(c.transform.position, pos);
                    if (d > bestD) continue;
                    var f = c.GetComponent<AnimalFeed>();
                    if (f != null) { best = f; bestD = d; }
                }
                return best;
            }
        }

        /// <summary>Hungrige Tiere fressen abgelegtes Futter auf normalem Weg (MonsterAI) -> Wirkung eintragen.</summary>
        [HarmonyPatch(typeof(Tameable), "OnConsumedItem")]
        public static class ConsumedFeed
        {
            public static void Postfix(Tameable __instance, ItemDrop __0)
            {
                try
                {
                    var f = __0 != null ? FeedRules.FromItem(__0.m_itemData) : null;
                    if (f == null) return;
                    var feed = __instance.GetComponent<AnimalFeed>();
                    if (feed != null) feed.ApplyLocal(f.Id);
                }
                catch (Exception) { }
            }
        }

        /// <summary>Zähmkraut: Zaehmfortschritt laeuft schneller.</summary>
        [HarmonyPatch(typeof(Tameable), "DecreaseRemainingTime")]
        public static class FasterTaming
        {
            public static void Prefix(Tameable __instance, ref float __0)
            {
                if (FeedRules.TameSpeed > 1f && AnimalFeed.Has(__instance, FeedIds.TamingHerb)) __0 *= FeedRules.TameSpeed;
            }
        }

        /// <summary>Zähmkraut: ruhiges Tier wird nicht alarmiert (sonst "verängstigt" und das Zaehmen stoppt).</summary>
        [HarmonyPatch(typeof(MonsterAI), "SetAlerted")]
        public static class CalmTaming
        {
            public static bool Prefix(MonsterAI __instance, bool __0)
            {
                if (!__0 || AnimalFeed.Calming) return true;
                try
                {
                    var feed = __instance.GetComponent<AnimalFeed>();
                    return feed == null || !feed.IsCalm();
                }
                catch (Exception) { return true; }
            }
        }

        /// <summary>Restzeit der Futter im Hover-Text des Tiers.</summary>
        [HarmonyPatch(typeof(Tameable), "GetHoverText")]
        public static class FeedHover
        {
            public static void Postfix(Tameable __instance, ref string __result)
            {
                try
                {
                    var feed = __instance.GetComponent<AnimalFeed>();
                    if (feed != null) __result += feed.HoverLines();
                }
                catch (Exception) { }
            }
        }

        /// <summary>Fenrirs Biss (+Schaden als Angreifer), Panzerkorn (-Schaden), Glutpanzer (-Feuerschaden).</summary>
        [HarmonyPatch(typeof(Character), "Damage")]
        public static class FeedDamage
        {
            public static void Prefix(Character __instance, HitData __0)
            {
                try
                {
                    if (__0 == null) return;
                    var attacker = __0.GetAttacker();
                    if (attacker != null && FeedRules.DamageBonus > 0f && AnimalFeed.Has(attacker, FeedIds.FenrirBite))
                        __0.m_damage.Modify(1f + FeedRules.DamageBonus);
                    if (__instance == null || __instance.GetComponent<AnimalFeed>() == null) return;
                    if (FeedRules.Armor > 0f && AnimalFeed.Has(__instance, FeedIds.ArmorGrain))
                        __0.m_damage.Modify(1f - FeedRules.Armor);
                    if (FeedRules.FireReduction > 0f && AnimalFeed.Has(__instance, FeedIds.EmberShell))
                        __0.m_damage.m_fire *= 1f - FeedRules.FireReduction;
                }
                catch (Exception) { }
            }
        }

        /// <summary>Nornensegen: bei der Geburt mit gewisser Chance ein Stern mehr fuer das Junge.</summary>
        [HarmonyPatch(typeof(Procreation), "Procreate")]
        public static class NornStar
        {
            private static readonly MethodInfo IsPregnant = AccessTools.Method(typeof(Procreation), "IsPregnant");

            private static bool Pregnant(Procreation p)
            {
                try { return IsPregnant != null && (bool)IsPregnant.Invoke(p, null); } catch (Exception) { return false; }
            }

            public static void Prefix(Procreation __instance, out HashSet<int> __state)
            {
                __state = null;
                if (FeedRules.StarChance <= 0f || !Pregnant(__instance) || !AnimalFeed.Has(__instance, FeedIds.NornBlessing)) return;
                __state = new HashSet<int>();
                foreach (var c in Character.GetAllCharacters()) if (c != null) __state.Add(c.GetInstanceID());
            }

            public static void Postfix(Procreation __instance, HashSet<int> __state)
            {
                if (__state == null || Pregnant(__instance)) return;
                try
                {
                    foreach (var c in Character.GetAllCharacters().ToArray())
                    {
                        if (c == null || __state.Contains(c.GetInstanceID())) continue;
                        if (Vector3.Distance(c.transform.position, __instance.transform.position) > 20f) continue;
                        if (c.GetLevel() >= FeedRules.MaxLevel || UnityEngine.Random.value >= FeedRules.StarChance) continue;
                        c.SetLevel(c.GetLevel() + 1);
                        var pl = Player.m_localPlayer;
                        if (pl != null && Vector3.Distance(pl.transform.position, c.transform.position) < 30f)
                            pl.Message(MessageHud.MessageType.TopLeft, "$vfp_feed_star", 0, null, false);
                    }
                }
                catch (Exception ex) { Debug.LogWarning("[Viking's Feast] Nornensegen: " + ex.Message); }
            }
        }
    }
}
