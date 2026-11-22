using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace VikingFoodPlus
{
    [BepInPlugin(Guid, "Viking's Feast", "1.5.3")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public partial class VikingFoodPlus : BaseUnityPlugin
    {
        public const string Guid = "de.vikingfoodplus.mod";
        private const string Station = "VFP_RuneCauldron";
        private ConfigEntry<bool> enabledMod;
        private ConfigEntry<string> distilleryCost;
        private ConfigEntry<bool> integratedFire;
        private ConfigEntry<float> fireHeightOffset;
        private ConfigEntry<float> extensionScale;
        private ConfigEntry<float> extensionDistance;
        private ConfigEntry<bool> foodBuffs;
        private ConfigEntry<float> cookTime;
        private ConfigEntry<bool> fireSmoke;
        private ConfigEntry<bool> feastsEnabled;
        private ConfigEntry<string> feastStation;
        private ConfigEntry<string> cookingStation;
        private ConfigEntry<bool> seasonalEnabled;
        private ConfigEntry<bool> seasonalAlways;
        private readonly Dictionary<string, ConfigEntry<float>> health = new Dictionary<string, ConfigEntry<float>>();
        private readonly Dictionary<string, ConfigEntry<float>> eitr = new Dictionary<string, ConfigEntry<float>>();
        private readonly Dictionary<string, ConfigEntry<float>> stamina = new Dictionary<string, ConfigEntry<float>>();
        private readonly Dictionary<string, ConfigEntry<float>> duration = new Dictionary<string, ConfigEntry<float>>();
        private readonly Dictionary<string, ConfigEntry<float>> regen = new Dictionary<string, ConfigEntry<float>>();
        private readonly Dictionary<string, ConfigEntry<string>> ingredients = new Dictionary<string, ConfigEntry<string>>();
        private readonly Dictionary<string, ConfigEntry<bool>> foodEnabled = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<int>> portions = new Dictionary<string, ConfigEntry<int>>();
        private static readonly FoodSpec[] Foods = new FoodSpec[] {
            new FoodSpec("BoarHerbRoast","Wildschwein-Kräuterbraten","Boar herb roast","CookedMeat",36,13,1,"TrophyBoar:1,Dandelion:5,Honey:3"),
            new FoodSpec("BerryMash","Waldbeerenmus","Forest berry mash","QueensJam",12,40,1,"Raspberry:8,Dandelion:4,Honey:3"),
            new FoodSpec("YellowMushroomSoup","Gelbe Pilzsuppe","Yellow mushroom soup","CarrotSoup",15,48,1,"MushroomYellow:8,Carrot:4,Blueberries:4"),
            new FoodSpec("DraugrStew","Draugr-Fleischtopf","Draugr meat stew","Sausages",66,20,2,"Entrails:5,Turnip:4,Thistle:5"),
            new FoodSpec("BloodThistleStew","Blut-Seetang-Eintopf","Blood seaweed stew","BlackSoup",64,22,2,"Bloodbag:4,Entrails:4,Thistle:5"),
            new FoodSpec("WolfHerbRoast","Wolfsbraten mit Kräutern","Wolf herb roast","CookedWolfMeat",68,22,2,"TrophyWolf:1,WolfMeat:4,Onion:4"),
            new FoodSpec("WolfThistleStew","Drachenbrühe","Dragon broth","WolfMeatSkewer",24,70,2,"PowderedDragonEgg:2,Onion:4,FreezeGland:2"),
            new FoodSpec("GoldenBarleyPorridge","Goldener Gerstenbrei","Golden barley porridge","Bread",25,78,2,"BarleyFlour:8,Cloudberry:6,Honey:4"),
            new FoodSpec("MagicMushroomCream","Magische Pilzcreme","Magic mushroom cream","Salad",28,88,3,"MushroomMagecap:6,MushroomJotunPuffs:6,RoyalJelly:4"),
            new FoodSpec("AsksvinFireRoast","Vulkangeierbraten","Volcanic vulture roast","CookedAsksvinMeat",100,32,3,"VoltureMeat:4,Vineberry:6,MushroomSmokePuff:4"),
            new FoodSpec("EmberHerbSoup","Glutkräuter-Suppe","Ember herb soup","MashedMeat",32,96,3,"Vineberry:7,MushroomSmokePuff:6,MushroomMagecap:4"),
            // FishRaw = Roher Fisch. Fish5 = Trollfisch, Fish9 = Anglerfisch, Fish10 = Nordlachs, Fish12 = Kugelfisch.
            // Vorlage FishCooked (gekochter Fisch), weil FishRaw kein Essen ist und im Spiel keine Werte anzeigt.
            new FoodSpec("HerbFish","Gebratener Kräuterfisch","Grilled herb fish","FishCooked",38,15,1,"FishRaw:2,Dandelion:3,Honey:2",0,1200,2),
            new FoodSpec("ForestFishPan","Waldpilz-Fischpfanne","Forest mushroom fish pan","FishWraps",42,20,1,"FishRaw:2,MushroomYellow:4,Thistle:3"),
            new FoodSpec("SwampFishStew","Sumpffisch-Eintopf","Swamp fish stew","SerpentStew",22,60,2,"Fish5:2,Thistle:4,Turnip:3"),
            new FoodSpec("FrostFishRoast","Frostfischbraten","Frost fish roast","FishCooked",71,24,2,"Fish10:2,Onion:4,FreezeGland:2",0,1500,3),
            new FoodSpec("PufferBarleyPan","Kugelfisch-Gerstenpfanne","Pufferfish barley pan","FishWraps",82,28,2,"Fish12:2,BarleyFlour:5,Cloudberry:4"),
            new FoodSpec("AnglerMistSoup","Anglerfisch-Nebelsuppe","Anglerfish mist soup","FishAndBread",32,99,3,"Fish9:2,MushroomMagecap:5,RoyalJelly:3"),
            // Three new Eitr foods; modded ingredient prefabs may need adjustment in the config.
            new FoodSpec("RoyalMistRagout","Königliches Nebelragout","Royal mist ragout","MisthareSupreme",30,15,2,"RoyalJelly:4,MushroomMagecap:5,HareMeat:3",85,1800,5),
            new FoodSpec("AsksvinEmberFeast","Asksvin-Glutmahl","Asksvin ember feast","CookedAsksvinMeat",38,20,3,"CelestialFeather:3,ProustitePowder:5,AsksvinMeat:4",100,1800,6),
            new FoodSpec("EternalFrostEssence","Essenz des Ewigen Frosts","Essence of eternal frost","SerpentStew",38,20,4,"FrozenFuel:6,Kale:2,Lingonberry:5",110,1800,7),
            new FoodSpec("TickSud","Zecken-Sud","Tick broth","BlackSoup",70,24,3,"Bloodbag:4,SpiceForests:2,Sap:2",0,1800,3),
            new FoodSpec("HeartyFlatbread","Herzhaftes Fladenbrot","Hearty flatbread","Bread",35,70,2,"BarleyFlour:10,Onion:3,LoxMeat:2",0,1800,3),
            new FoodSpec("MooseBurger","Elchburger","Moose burger","CookedLoxMeat",110,35,4,"MooseMeat:2,BarleyFlour:4,Onion:2,Lingonberry:3",0,1800,5),
            // Hoher-Norden-IDs gegen Valheim 1.0 geprueft (FrozenFuel = Fluessiger Frost, Oat = Hafer).
            new FoodSpec("NorthernOatPorridge","Nordlicht-Haferbrei","Northern lights oat porridge","QueensJam",35,112,4,"Oat:8,Lingonberry:5,OatMilk:4,Poteitr:3"),
            // Saisongerichte (95-105 % der Valheim-Bestwerte): nur waehrend Valheims Mittsommer-, Halloween- und Julfest-Zeit (siehe Seasons)
            new FoodSpec("MidsummerCake","Mittsommer-Beerenkuchen","Midsummer berry cake","QueensJam",14,45,1,"Raspberry:6,Blueberries:6,Honey:4,Dandelion:3"),
            new FoodSpec("TurnipGhostSoup","Grinsende Rübenbrühe","Grinning turnip broth","TurnipStew",18,54,2,"Turnip:4,Ooze:2,MushroomYellow:2,SpiceForests:1"),
            new FoodSpec("YuleRoast","Julfest-Braten","Yule roast","CookedDeerMeat",45,14,1,"DeerMeat:3,Raspberry:4,Honey:2,Carrot:2")
        };

        // Saison je Gericht: Schluesselwoerter der passenden Vanilla-SeasonalItemGroup
        private static readonly Dictionary<string, string[]> Seasons = new Dictionary<string, string[]> {
            {"MidsummerCake",   new[] { "maypole", "midsummer" }},
            {"TurnipGhostSoup", new[] { "pointy", "jack", "halloween", "pumpkin" }},
            {"YuleRoast",       new[] { "xmas", "yule", "christmas", "mistletoe", "garland", "gift" }}
        };

        // Eigene Beschreibung je Gericht im Valheim-Stil (kurz): Id -> { Deutsch, Englisch }
        private static readonly Dictionary<string, string[]> Descriptions = new Dictionary<string, string[]> {
            {"BoarHerbRoast", new[] { "Ein ganzes Wildschwein. Na ja, fast.", "A whole boar. Well, almost." }},
            {"BerryMash", new[] { "Süß, klebrig und schnell weg.", "Sweet, sticky and gone in no time." }},
            {"YellowMushroomSoup", new[] { "Gelb. Warm. Hoffentlich die richtigen Pilze.", "Yellow. Warm. Hopefully the right mushrooms." }},
            {"DraugrStew", new[] { "Kein Draugr wurde verletzt. Glaubt man.", "No draugr were harmed. Probably." }},
            {"BloodThistleStew", new[] { "Dunkel, dick und ein bisschen lebendig.", "Dark, thick and slightly alive." }},
            {"WolfHerbRoast", new[] { "Der Wolf hätte dich auch gegessen.", "The wolf would have eaten you too." }},
            {"WolfThistleStew", new[] { "Kribbelt. Das soll so sein.", "It tingles. That's normal." }},
            {"GoldenBarleyPorridge", new[] { "Wie ein Sonnenaufgang auf den Ebenen. Nur zum Löffeln.", "Like sunrise on the Plains. With a spoon." }},
            {"MagicMushroomCream", new[] { "Schimmert leicht. Nicht zu lange hinsehen.", "Shimmers faintly. Don't stare too long." }},
            {"AsksvinFireRoast", new[] { "Scharf. Sehr scharf.", "Spicy. Very spicy." }},
            {"EmberHerbSoup", new[] { "Erst pusten. Dann nochmal pusten.", "Blow on it first. Then blow again." }},
            {"HerbFish", new[] { "Einfach, ehrlich, fischig.", "Simple, honest, fishy." }},
            {"ForestFishPan", new[] { "Duftet nach Wald und Lagerfeuer.", "Smells of forest and campfire." }},
            {"SwampFishStew", new[] { "Riecht nach Moor. Schmeckt auch so.", "Smells of the bog. Tastes like it too." }},
            {"FrostFishRoast", new[] { "Außen knusprig, innen eisig.", "Crispy outside, icy inside." }},
            {"PufferBarleyPan", new[] { "Richtig zubereitet. Hoffentlich.", "Prepared correctly. Hopefully." }},
            {"AnglerMistSoup", new[] { "Leuchtet in der Schale. Und danach im Bauch.", "Glows in the bowl. And later in your belly." }},
            {"RoyalMistRagout", new[] { "Eines Jarls würdig. Oder eines hungrigen Wikingers.", "Fit for a jarl. Or a hungry viking." }},
            {"AsksvinEmberFeast", new[] { "Ein Festmahl aus Asche und Feuer.", "A feast of ash and fire." }},
            {"EternalFrostEssence", new[] { "So kalt, dass die Zähne klappern.", "So cold your teeth chatter." }},
            {"TickSud", new[] { "Besser nicht fragen, was drin ist.", "Better not ask what's in it." }},
            {"HeartyFlatbread", new[] { "Knusprig, herzhaft und gut für unterwegs.", "Crispy, hearty and good for the road." }},
            {"MooseBurger", new[] { "Ein Elch zwischen zwei Broten.", "A moose between two breads." }},
            {"MidsummerCake", new[] { "Schmeckt nach langen Tagen und kurzen Nächten.", "Tastes of long days and short nights." }},
            {"TurnipGhostSoup", new[] { "Die Rübe grinst zurück.", "The turnip grins back." }},
            {"YuleRoast", new[] { "Ein Festbraten für die längste Nacht des Jahres.", "A feast roast for the longest night of the year." }},
            {"NorthernOatPorridge", new[] { "Wärmt von innen, wenn draußen alles friert.", "Warms you inside when everything outside freezes." }}
        };

        // Kleine Zusatz-Effekte fuer einige Gerichte (10 Minuten). Schutz = "SlightlyResistant" (-25 % Schaden),
        // also schwaecher als Valheims Schutz-Met (-50 %). Verhindert kein Frieren.
        private static readonly Dictionary<string, BuffSpec> Buffs = new Dictionary<string, BuffSpec> {
            {"BloodThistleStew",    new BuffSpec("poison", "Poison", 0f, 0f)},
            {"FrostFishRoast",      new BuffSpec("frost",  "Frost",  0f, 0f)},
            {"AnglerMistSoup",      new BuffSpec("mind",   null,     0f, 1.1f)},
            {"EmberHerbSoup",       new BuffSpec("fire",   "Fire",   0f, 0f)},
            {"NorthernOatPorridge", new BuffSpec("north",  null,     1.1f, 0f)},
            {"MidsummerCake",       new BuffSpec("summer", null,     1.1f, 0f)},
            {"TurnipGhostSoup",     new BuffSpec("ghost",  null,     0f,   0f, 1.1f)},
            {"YuleRoast",           new BuffSpec("yule",   "Frost",  0f,   0f)}
        };
        private const float BuffDuration = 600f;

        // Festmahl-Platten: werden wie Valheims Festmahle mit dem Servierbrett aufgestellt, 10 Portionen, 50 Minuten.
        private static readonly FeastSpec[] Feasts = new FeastSpec[] {
            new FeastSpec("FeastBog", "Moorschmaus", "Bog feast",
                "Riecht nach Moor. Alle essen trotzdem mit.", "Smells of the bog. Everyone eats anyway.",
                "FeastSwamps", "VFP_DraugrStew:4,VFP_SwampFishStew:2,Bloodbag:4,SpiceForests:1", 40, 30, 0, 3, 2, new Color(0.70f, 1.00f, 0.60f)),   // gruenlich
            new FeastSpec("FeastHarvest", "Goldene Erntetafel", "Golden harvest table",
                "Genug für ein ganzes Dorf. Oder drei hungrige Wikinger.", "Enough for a whole village. Or three hungry vikings.",
                "FeastPlains", "VFP_HeartyFlatbread:3,VFP_PufferBarleyPan:2,Cloudberry:5,SpicePlains:1", 50, 60, 0, 4, 3, new Color(1.00f, 0.85f, 0.50f)),   // golden
            new FeastSpec("FeastNorthernLights", "Nordlicht-Festtafel", "Northern lights banquet",
                "Wärmt Bauch und Seele, wenn draußen alles gefriert.", "Warms belly and soul when everything outside freezes.",
                "FeastDeepNorth", "VFP_MooseBurger:3,VFP_NorthernOatPorridge:2,Lingonberry:5,SpiceDeepNorth:1", 70, 60, 80, 7, 4, new Color(0.75f, 0.90f, 1.00f))   // blaeulich-frostig
        };

        private void Awake()
        {
            enabledMod = Config.Bind("General", "Enabled", true, "Enable mod content; restart required.");
            foreach (var food in Foods)
            {
                // Seit 1.4.2 "Dish - " statt "Food - ": alte Config-Werte (z. B. alte Zutaten) werden so automatisch ignoriert.
                foodEnabled[food.Id] = Config.Bind("Dish - " + food.Id, "Enabled", true, "Enable this dish; restart required.");
                health[food.Id] = Config.Bind("Dish - " + food.Id, "Health", food.Hp, "Health per portion; restart required.");
                stamina[food.Id] = Config.Bind("Dish - " + food.Id, "Stamina", food.Stamina, "Stamina per portion; restart required.");
                eitr[food.Id] = Config.Bind("Dish - " + food.Id, "Eitr", food.Eitr, "Eitr per portion; restart required.");
                duration[food.Id] = Config.Bind("Dish - " + food.Id, "Duration", food.Duration, "Food duration in seconds; restart required.");
                regen[food.Id] = Config.Bind("Dish - " + food.Id, "Regen", food.Regen, "Food healing per tick; restart required.");
                ingredients[food.Id] = Config.Bind("Dish - " + food.Id, "Ingredients", food.Ingredients, "Prefab:count comma-separated; restart required.");
                portions[food.Id] = Config.Bind("Dish - " + food.Id, "Portions", 2, new ConfigDescription("Output portions; restart required.", new AcceptableValueRange<int>(1, 10)));
            }
            // Neue Balance (1.4.5, 80-110 % der Valheim-Bestwerte): Leben/Ausdauer/Eitr einmalig auf die neuen Standardwerte setzen,
            // sonst behalten bestehende Configs die alten, zu starken Werte.
            var balance = Config.Bind("General", "BalanceVersion", 0, "Intern: Stand der Gericht-Werte. Nicht aendern.");
            if (balance.Value < 4)
            {
                foreach (var food in Foods)
                {
                    health[food.Id].Value = (float)health[food.Id].DefaultValue;
                    stamina[food.Id].Value = (float)stamina[food.Id].DefaultValue;
                    eitr[food.Id].Value = (float)eitr[food.Id].DefaultValue;
                }
                balance.Value = 4;
            }
            distilleryCost = Config.Bind("Piece - IceDistillery", "Requirements", "Crystal:20,DragonTear:5,BlackMetalScrap:10,YggdrasilWood:2",
                "Baukosten der Eisdestillerie als Prefab:Anzahl, kommagetrennt. Unbekannte Prefabs werden uebersprungen (Warnung im Log). Namen per Konsole 'spawn' + Autovervollstaendigung pruefen; Neustart noetig.");
            integratedFire = Config.Bind("Piece - RuneCauldron", "IntegratedFire", true,
                "Eingebautes Feuer unter dem Runenkessel. Brennt wie ein Lagerfeuer: Holz nachlegen, brennt herunter, geht aus. false = der Kessel braucht wie in Vanilla ein separates Feuer darunter. Neustart noetig.");
            fireHeightOffset = Config.Bind("Piece - RuneCauldron", "FireHeightOffset", 0f,
                "Hoehe des eingebauten Feuers relativ zum Kessel in Metern (z. B. -0.2 oder 0.1). Neustart noetig.");
            extensionScale = Config.Bind("Piece - Extensions", "Scale", 0.75f,
                new ConfigDescription("Groesse der Kessel-Erweiterungen (Gaerfass, Trockengestell, Eisdestillerie). 1 = Originalgroesse. Neustart noetig.", new AcceptableValueRange<float>(0.3f, 1.5f)));
            extensionDistance = Config.Bind("Piece - Extensions", "MaxDistance", 10f,
                new ConfigDescription("Maximaler Abstand in Metern, in dem eine Erweiterung noch zum Runenkessel zaehlt (Vanilla-Werkbank: 5). Naeher geht immer. Neustart noetig.", new AcceptableValueRange<float>(2f, 30f)));
            foodBuffs = Config.Bind("General", "FoodBuffs", true,
                "Einige Gerichte geben zusaetzlich einen kleinen Effekt fuer 10 Minuten (z. B. etwas Frostschutz). Neustart noetig.");
            cookTime = Config.Bind("Piece - RuneCauldron", "CookTime", 0f,
                new ConfigDescription("Kochzeit am Runenkessel in Sekunden. 0 = wie in Valheim (Standard). Groessere Werte verlaengern den Fortschrittsbalken beim Herstellen. Sofort wirksam.", new AcceptableValueRange<float>(0f, 15f)));
            fireSmoke = Config.Bind("Piece - RuneCauldron", "FireSmoke", true,
                "Rauch des eingebauten Kesselfeuers (wie beim Lagerfeuer). false = Feuer ohne Rauch. Neustart noetig.");
            cookingStation = Config.Bind("General", "CookingStation", "RuneCauldron",
                new ConfigDescription("Wo die Gerichte gekocht werden: RuneCauldron = nur am Runenkessel (Standard), Cauldron = nur am normalen Valheim-Kessel, Both = an beiden. Die benoetigte Kochstufe gilt auch am Valheim-Kessel (dort ueber dessen eigene Erweiterungen). Neustart noetig.",
                new AcceptableValueList<string>("RuneCauldron", "Cauldron", "Both")));
            feastsEnabled = Config.Bind("Feasts", "Enabled", true,
                "Festmahl-Platten (Moorschmaus, Goldene Erntetafel, Nordlicht-Festtafel). Werden mit dem Servierbrett aufgestellt. Neustart noetig.");
            feastStation = Config.Bind("Feasts", "Station", "PrepTable",
                new ConfigDescription("Wo die Festmahl-Platten hergestellt werden: PrepTable = Zubereitungstisch (wie in Valheim), RuneCauldron = Runenkessel (Stufe 2/3/4). Neustart noetig.",
                new AcceptableValueList<string>("PrepTable", "RuneCauldron")));
            seasonalEnabled = Config.Bind("Seasonal", "Enabled", true,
                "Saisongerichte (Mittsommer-Beerenkuchen, Grinsende Rübenbrühe, Julfest-Braten). Neustart noetig.");
            seasonalAlways = Config.Bind("Seasonal", "AlwaysAvailable", false,
                "true = Saisongerichte das ganze Jahr kochbar. false = nur waehrend Valheims Mittsommer-, Halloween- und Julfest-Zeit. Neustart noetig.");
            BindDrinkConfig();
            if (!enabledMod.Value) return;
            if (drinksEnabled.Value) ApplyDrinkStatics();
            RecipeGroups.GroupsEnabled = drinksEnabled.Value && recipeGroups.Value;
            ApplyRecipeGroups();   // Gruppen und/oder Sortier-Knoepfe (beides per Config abschaltbar)
            ItemManager.OnItemsRegistered += AssignSeasonalRecipes;
            PrefabManager.OnVanillaPrefabsAvailable += Register;
        }

        private void Update()
        {
            if (!enabledCooking || enabledMod == null || !enabledMod.Value || cookTime == null) return;
            try { CauldronCooking.Tick(cookTime.Value); }
            catch (Exception ex) { Logger.LogWarning("Kochzeit: " + ex.Message); enabledCooking = false; }
        }
        private bool enabledCooking = true;

        private void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
            try { RegisterStation(); } catch (Exception ex) { Logger.LogError("Runenkessel: " + ex); }
            // Gaerfass: mit Getraenken ein Klon des Valheim-Fermenters (kann gaeren), sonst wie bisher
            try { RegisterExtension("VFP_FermentBarrel", drinksEnabled.Value && PrefabManager.Instance.GetPrefab("fermenter") != null ? "fermenter" : "piece_workbench_ext1", "Gärfass", "Fermentation barrel", 2,
                new[] { new RequirementConfig("FineWood", 15), new RequirementConfig("Iron", 8), new RequirementConfig("ElderBark", 5) }); }
            catch (Exception ex) { Logger.LogError("Gärfass: " + ex); }
            try { RegisterExtension("VFP_RuneDryingRack", "piece_workbench_ext1", "Runen-Trockengestell", "Runic drying rack", 3,
                new[] { new RequirementConfig("YggdrasilWood", 15), new RequirementConfig("BlackMetal", 10), new RequirementConfig("BlackMarble", 5) }); }
            catch (Exception ex) { Logger.LogError("Trockengestell: " + ex); }
            try { RegisterExtension("VFP_IceDistillery", "piece_workbench_ext1", "Eisdestillerie", "Ice distillery", 4, ParseRequirements(distilleryCost.Value)); }
            catch (Exception ex) { Logger.LogError("Eisdestillerie: " + ex); }
            foreach (var food in Foods)
            {
                if (Seasons.ContainsKey(food.Id) && !seasonalEnabled.Value) continue;
                try { if (foodEnabled[food.Id].Value) RegisterFood(food); }
                catch (Exception ex) { Logger.LogWarning("Rezept " + food.Id + " übersprungen: " + ex.Message); }
            }
            if (feastsEnabled.Value)
            {
                try { RegisterFeasts(); }
                catch (Exception ex) { Logger.LogError("Festmahle: " + ex); }
            }
            if (drinksEnabled.Value)
            {
                try { RegisterDrinks(); }
                catch (Exception ex) { Logger.LogError("Getraenke: " + ex); }
            }
            try { if (foodEnabled["HeartyFlatbread"].Value) RegisterFlatbreadBaking(); }
            catch (Exception ex) { Logger.LogError("Fladenbrot-Kochofen: " + ex); }
            try { RegisterTranslations(); } catch (Exception ex) { Logger.LogError("Localization: " + ex); }
            Logger.LogInfo("Viking's Feast 1.5.3 registration complete.");
        }

        private void RegisterStation()
        {
            var go = PrefabManager.Instance.CreateClonedPrefab(Station, "piece_cauldron");
            if (go == null) throw new Exception("piece_cauldron missing");
            var station = go.GetComponent<CraftingStation>();
            if (station == null) throw new Exception("CraftingStation missing");
            station.m_name = "$vfp_station";
            var piece = go.GetComponent<Piece>();
            piece.m_name = "$vfp_station";
            piece.m_description = "$vfp_station_desc";
            Decorate(go, 1);
            // Eigenes Baumenue-Icon statt des Vanilla-Kessels.
            try { ApplyPieceIcon(piece, "BuildRuneCauldron"); }
            catch (Exception ex) { Logger.LogWarning("Baumenü-Icon Runenkessel: " + ex.Message); }
            // Zusaetzlicher Dampf beim Kochen (Kopie der Vanilla-Kessel-Partikel), vor dem Feuer anlegen
            try { if (!CauldronCooking.AddSteam(go)) Logger.LogWarning("Kochdampf: kein Partikel-Material gefunden, Kochen ohne Dampf"); }
            catch (Exception ex) { Logger.LogWarning("Kochdampf: " + ex.Message); }
            if (integratedFire.Value)
            {
                try { AddIntegratedFire(go); }
                catch (Exception ex) { Logger.LogError("Kesselfeuer: " + ex); }
            }
            PieceManager.Instance.AddPiece(new CustomPiece(go, true, new PieceConfig {
                PieceTable = "_HammerPieceTable", Category = "Crafting",
                Requirements = new[] { new RequirementConfig("Wood", 15), new RequirementConfig("Stone", 10), new RequirementConfig("Bronze", 5) }
            }));
        }

        /// <summary>Baut das Feuer unter den Kessel: Holzscheite (eigenes Modell), Flammen/Licht vom Vanilla-Lagerfeuer
        /// und eine Feuer-Effektzone an den Feuer-Pruefpunkten des Kessels. Alles ausser den Scheiten ist nur aktiv, solange es brennt.</summary>
        private void AddIntegratedFire(GameObject stationGo)
        {
            var root = new GameObject("VFP_CauldronFire");
            root.transform.SetParent(stationGo.transform, false);
            root.transform.localPosition = new Vector3(0f, fireHeightOffset.Value, 0f);
            root.layer = stationGo.layer;
            root.AddComponent<Switch>();   // Hover-Text + E-Taste (wie beim Vanilla-Lagerfeuer)
            var fire = root.AddComponent<CauldronFire>();

            // Holzscheite + Glut (Glut leuchtet nur, solange das Feuer brennt)
            Material baseMat = null;
            foreach (var r in stationGo.GetComponentsInChildren<MeshRenderer>(true))
                if (r.sharedMaterial != null) { baseMat = r.sharedMaterial; break; }
            if (baseMat != null)
            {
                var logs = ModelLoader.Build(root, "VFP_FireLogs", baseMat);
                foreach (Transform child in logs.transform)
                    if (child.name == "F_Embers") fire.BurningObjects.Add(child.gameObject);
            }
            else Logger.LogWarning("Kesselfeuer: kein Material fuer die Holzscheite gefunden");

            // Werte und Flammen vom Vanilla-Lagerfeuer uebernehmen
            var firePit = PrefabManager.Instance.GetPrefab("fire_pit");
            var fireplace = firePit != null ? firePit.GetComponent<Fireplace>() : null;
            if (fireplace != null)
            {
                fire.MaxFuel = ReadField(fireplace, "m_maxFuel", fire.MaxFuel);
                fire.SecPerFuel = ReadField(fireplace, "m_secPerFuel", fire.SecPerFuel);
                fire.StartFuel = ReadField(fireplace, "m_startFuel", fire.StartFuel);
                fire.FuelAddedEffects = ReadField<EffectList>(fireplace, "m_fuelAddedEffects", null);
                var fuelItem = ReadField<ItemDrop>(fireplace, "m_fuelItem", null);
                if (fuelItem != null) fire.FuelItemName = fuelItem.m_itemData.m_shared.m_name;
                var visuals = new List<GameObject>();
                var main = ReadField<GameObject>(fireplace, "m_enabledObject", null);
                var high = ReadField<GameObject>(fireplace, "m_enabledObjectHigh", null);
                var low = ReadField<GameObject>(fireplace, "m_enabledObjectLow", null);
                if (main != null) visuals.Add(main);
                if (high != null) visuals.Add(high); else if (low != null) visuals.Add(low);
                foreach (var src in visuals)
                {
                    var copy = UnityEngine.Object.Instantiate(src, root.transform, false);
                    copy.name = "VFP_" + src.name;
                    if (!fireSmoke.Value) RemoveSmoke(copy);
                    copy.SetActive(false);
                    fire.BurningObjects.Add(copy);
                }
            }
            else Logger.LogWarning("Kesselfeuer: Vanilla fire_pit nicht gefunden, Feuer ohne Flammen");

            // Klickbereich fuer "Holz nachlegen"
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.15f, 0f);
            col.size = new Vector3(0.8f, 0.3f, 0.8f);

            AddFireAreas(stationGo, root, fire);
        }

        /// <summary>Entfernt Rauch (SmokeSpawner und Rauch-Partikel) aus den kopierten Lagerfeuer-Effekten.</summary>
        private void RemoveSmoke(GameObject fireCopy)
        {
            var remove = new List<UnityEngine.Object>();
            foreach (var comp in fireCopy.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                if (comp.GetType().Name == "SmokeSpawner") remove.Add(comp);
                else if (comp is ParticleSystem && comp.gameObject != fireCopy && comp.gameObject.name.ToLowerInvariant().Contains("smoke")) remove.Add(comp.gameObject);
            }
            foreach (var o in remove) if (o != null) UnityEngine.Object.DestroyImmediate(o);
        }

        /// <summary>Feuer-Effektzone an den Feuer-Pruefpunkten des Kessels, damit er das eigene Feuer erkennt.</summary>
        private void AddFireAreas(GameObject stationGo, GameObject fireRoot, CauldronFire fire)
        {
            // Ebene von einer vorhandenen Feuer-Zone uebernehmen (kopierte Lagerfeuer-Flammen), sonst per Name suchen
            int layer = -1;
            foreach (var go in fire.BurningObjects)
            {
                if (go == null) continue;
                var existing = go.GetComponentInChildren<EffectArea>(true);
                if (existing != null) { layer = existing.gameObject.layer; break; }
            }
            if (layer < 0) layer = LayerMask.NameToLayer("effect_area");
            if (layer < 0) layer = LayerMask.NameToLayer("effect");
            if (layer < 0) { Logger.LogInfo("Kesselfeuer: keine eigene Feuer-Zone noetig, Lagerfeuer-Flammen erkennen das Feuer"); return; }
            var points = new List<Transform>();
            var station = stationGo.GetComponent<CraftingStation>();
            var field = typeof(CraftingStation).GetField("m_fireCheckPoints", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (station != null && field != null && field.GetValue(station) is Transform[] checkPoints)
                foreach (var t in checkPoints) if (t != null) points.Add(t);
            if (points.Count == 0) points.Add(fireRoot.transform);
            var typeField = typeof(EffectArea).GetField("m_type", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var point in points)
            {
                var area = new GameObject("VFP_FireArea");
                area.layer = layer;
                area.transform.SetParent(point, false);
                var sphere = area.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 0.5f;
                var effect = area.AddComponent<EffectArea>();
                if (typeField != null) typeField.SetValue(effect, ParseFlags(typeField.FieldType, "Heat", "Fire", "Burning"));
                area.SetActive(false);
                fire.BurningObjects.Add(area);
            }
        }

        private object ParseFlags(Type enumType, params string[] names)
        {
            int value = 0;
            foreach (var name in names)
            {
                try { value |= Convert.ToInt32(Enum.Parse(enumType, name)); }
                catch (Exception) { }
            }
            return Enum.ToObject(enumType, value);
        }

        /// <summary>Erzeugt den Zusatz-Effekt eines Gerichts als SE_Stats und registriert ihn bei Jotunn.</summary>
        private StatusEffect CreateBuff(string foodId, BuffSpec buff, Sprite icon)
        {
            var se = ScriptableObject.CreateInstance<SE_Stats>();
            se.name = "SE_VFP_" + foodId;
            se.m_name = "$vfp_buff_" + buff.Key;
            se.m_tooltip = "$vfp_buff_" + buff.Key + "_tip";
            se.m_ttl = BuffDuration;
            if (icon != null) se.m_icon = icon;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            if (buff.DamageType != null)
            {
                object modifier = TryParseEnum(typeof(HitData.DamageModifier), "SlightlyResistant");
                object damageType = TryParseEnum(typeof(HitData.DamageType), buff.DamageType);
                if (modifier == null || damageType == null)
                {
                    Logger.LogWarning("Effekt " + foodId + ": Schutzstufe nicht verfuegbar, Effekt uebersprungen");
                    return null;
                }
                var pair = new HitData.DamageModPair();
                pair.m_type = (HitData.DamageType)damageType;
                pair.m_modifier = (HitData.DamageModifier)modifier;
                se.m_mods = new List<HitData.DamageModPair> { pair };
            }
            if (buff.StaminaRegen > 0f)
            {
                var f = typeof(SE_Stats).GetField("m_staminaRegenMultiplier", flags);
                if (f != null) f.SetValue(se, buff.StaminaRegen);
            }
            if (buff.HealthRegen > 0f)
            {
                var f = typeof(SE_Stats).GetField("m_healthRegenMultiplier", flags);
                if (f != null) f.SetValue(se, buff.HealthRegen);
            }
            if (buff.EitrRegen > 0f)
            {
                var f = typeof(SE_Stats).GetField("m_eitrRegenMultiplier", flags);
                if (f != null) f.SetValue(se, buff.EitrRegen);
            }
            ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(se, false));
            return se;
        }

        private object TryParseEnum(Type enumType, string name)
        {
            try { return Enum.Parse(enumType, name); }
            catch (Exception) { return null; }
        }

        private sealed class BuffSpec
        {
            public readonly string Key, DamageType;
            public readonly float StaminaRegen, EitrRegen, HealthRegen;
            public BuffSpec(string key, string damageType, float staminaRegen, float eitrRegen, float healthRegen = 0f)
            { Key = key; DamageType = damageType; StaminaRegen = staminaRegen; EitrRegen = eitrRegen; HealthRegen = healthRegen; }
        }

        // ---------------- Saisongerichte ----------------

        /// <summary>Haengt die Saisonrezepte in Valheims SeasonalItemGroups (am Player-Prefab) ein.
        /// Valheim zeigt deaktivierte Rezepte, die in der Gruppe der aktuellen Saison stehen.</summary>
        private void AssignSeasonalRecipes()
        {
            if (!seasonalEnabled.Value || seasonalAlways.Value || ObjectDB.instance == null) return;
            try
            {
                var player = PrefabManager.Instance.GetPrefab("Player");
                var groupsField = typeof(Player).GetField("m_seasonalItemGroups", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var comp = player != null ? player.GetComponent<Player>() : null;
                var groups = comp != null && groupsField != null ? groupsField.GetValue(comp) as IEnumerable : null;
                if (groups == null) { Logger.LogWarning("Saisongerichte: Valheims Saison-Gruppen nicht gefunden, Gerichte ganzjaehrig"); EnableSeasonalRecipes(); return; }
                foreach (var kv in Seasons)
                foreach (var recipe in FindRecipes("VFP_" + kv.Key))
                {
                    SeasonalItemGroup target = null;
                    foreach (var g in groups)
                    {
                        var group = g as SeasonalItemGroup;
                        if (group != null && GroupMatches(group, kv.Value)) { target = group; break; }
                    }
                    if (target == null)
                    {
                        Logger.LogWarning("Saisongericht " + kv.Key + ": passende Saison nicht gefunden, ganzjaehrig verfuegbar");
                        recipe.m_enabled = true;
                        continue;
                    }
                    target.Recipes.RemoveAll(r => r == null || r == recipe || r.name == recipe.name);
                    target.Recipes.Add(recipe);
                }
            }
            catch (Exception ex) { Logger.LogWarning("Saisongerichte: " + ex.Message); EnableSeasonalRecipes(); }
        }

        private void EnableSeasonalRecipes()
        {
            foreach (var id in Seasons.Keys) foreach (var r in FindRecipes("VFP_" + id)) r.m_enabled = true;
        }

        private List<Recipe> FindRecipes(string itemName)
        {
            var list = new List<Recipe>();
            foreach (var r in ObjectDB.instance.m_recipes)
                if (r != null && r.m_item != null && r.m_item.name == itemName) list.Add(r);
            return list;
        }

        private bool GroupMatches(SeasonalItemGroup group, string[] keywords)
        {
            var names = new List<string>();
            if (group.Recipes != null)
                foreach (var r in group.Recipes)
                    if (r != null) { names.Add(r.name); if (r.m_item != null) names.Add(r.m_item.name); }
            if (group.Pieces is IEnumerable pieces)
                foreach (var p in pieces)
                    if (p is UnityEngine.Object o && o != null) names.Add(o.name);
            names.Add(group.name);
            foreach (var n in names)
            {
                var lower = (n ?? "").ToLowerInvariant();
                if (lower.StartsWith("vfp_")) continue;
                foreach (var k in keywords) if (lower.Contains(k)) return true;
            }
            return false;
        }

        // ---------------- Festmahl-Platten ----------------

        private void RegisterFeasts()
        {
            var feaster = PrefabManager.Instance.GetPrefab("Feaster");
            var tray = feaster != null ? feaster.GetComponent<ItemDrop>() : null;
            var table = tray != null ? tray.m_itemData.m_shared.m_buildPieces : null;
            if (table == null) throw new Exception("Servierbrett (Feaster) oder sein Bau-Menue nicht gefunden");
            string tableName = table.gameObject.name;
            string stationPrefab = ResolveFeastStation();
            foreach (var feast in Feasts)
            {
                try { RegisterFeast(feast, table, tableName, stationPrefab); }
                catch (Exception ex) { Logger.LogWarning("Festmahl " + feast.Id + " übersprungen: " + ex.Message); }
            }
        }

        private string ResolveFeastStation()
        {
            if (feastStation.Value == "RuneCauldron") return Station;
            if (PrefabManager.Instance.GetPrefab("piece_preptable") != null) return "piece_preptable";
            var scene = ZNetScene.instance;
            if (scene != null)
                foreach (var prefab in scene.m_prefabs)
                {
                    var cs = prefab != null ? prefab.GetComponent<CraftingStation>() : null;
                    if (cs != null && cs.m_name != null && cs.m_name.ToLowerInvariant().Contains("prep")) return prefab.name;
                }
            Logger.LogWarning("Festmahle: Zubereitungstisch nicht gefunden, Herstellung am Runenkessel");
            return Station;
        }

        private void RegisterFeast(FeastSpec feast, PieceTable table, string tableName, string stationPrefab)
        {
            string key = "vfp_" + feast.Id.ToLowerInvariant();
            string eatId = "VFP_" + feast.Id, matId = "VFP_" + feast.Id + "_Material", pieceId = "VFP_" + feast.Id + "_Piece";
            var baseEat = PrefabManager.Instance.GetPrefab(feast.VanillaBase);
            var baseMat = PrefabManager.Instance.GetPrefab(feast.VanillaBase + "_Material");
            if (baseEat == null || baseMat == null) throw new Exception("Vanilla-Festmahl " + feast.VanillaBase + " nicht gefunden");

            // Zutaten vorab pruefen (fehlende Gerichte -> Festmahl ueberspringen)
            var reqs = new List<RequirementConfig>();
            foreach (var part in feast.Ingredients.Split(','))
            {
                var tokens = part.Split(':');
                if (PrefabManager.Instance.GetPrefab(tokens[0]) == null) throw new Exception("Zutat fehlt: " + tokens[0]);
                reqs.Add(new RequirementConfig(tokens[0], int.Parse(tokens[1])));
            }

            Sprite icon = LoadSprite(feast.Id);

            // Portion (das, was man von der Platte isst)
            var eat = PrefabManager.Instance.CreateClonedPrefab(eatId, baseEat);
            SetupFeastItem(eat.GetComponent<ItemDrop>(), feast, key, icon);
            // Wichtig: Player.EatFood liest item.m_dropPrefab.name. Die Platte (Feast.m_foodItem) nutzt das Prefab direkt,
            // daher muss m_dropPrefab hier gesetzt sein, sonst NullReferenceException und das Essen wirkt nicht.
            eat.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = eat;
            TintModel(eat, feast.Tint);
            ItemManager.Instance.AddItem(new CustomItem(eat, true));

            // Inventar-Item (wird hergestellt und mit dem Servierbrett aufgestellt)
            var mat = PrefabManager.Instance.CreateClonedPrefab(matId, baseMat);
            SetupFeastItem(mat.GetComponent<ItemDrop>(), feast, key, icon);
            mat.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = mat;
            TintModel(mat, feast.Tint);
            ItemManager.Instance.AddItem(new CustomItem(mat, true));

            // Platte im Bau-Menue des Servierbretts: Vanilla-Platte suchen und klonen
            GameObject basePiece = null;
            foreach (var p in table.m_pieces)
            {
                var pc = p != null ? p.GetComponent<Piece>() : null;
                if (pc == null || pc.m_resources == null) continue;
                foreach (var r in pc.m_resources)
                    if (r != null && r.m_resItem != null && r.m_resItem.gameObject.name == baseMat.name) { basePiece = p; break; }
                if (basePiece != null) break;
            }
            if (basePiece == null) throw new Exception("Vanilla-Platte fuer " + feast.VanillaBase + " im Servierbrett nicht gefunden");
            var piece = PrefabManager.Instance.CreateClonedPrefab(pieceId, basePiece);
            ReplaceItemRefs(piece, baseEat, eat);
            ReplaceItemRefs(piece, baseMat, mat);
            var feastComp = piece.GetComponentInChildren<Feast>(true);
            if (feastComp != null)
            {
                // Sicherstellen, dass die Platte unsere Portion mit unseren Werten serviert
                feastComp.m_foodItem = eat.GetComponent<ItemDrop>();
            }
            else Logger.LogWarning("Festmahl " + feast.Id + ": Feast-Komponente nicht gefunden");
            TintModel(piece, feast.Tint);   // Groesse und Form bleiben wie beim Vanilla-Festmahl
            var pieceComp = piece.GetComponent<Piece>();
            pieceComp.m_name = "$" + key;
            pieceComp.m_description = "$" + key + "_desc";
            if (icon != null) pieceComp.m_icon = icon;
            PieceManager.Instance.AddPiece(new CustomPiece(piece, true, new PieceConfig {
                PieceTable = tableName,
                Requirements = new[] { new RequirementConfig(matId, 1, 0, true) }
            }));

            // Rezept
            int level = stationPrefab == Station ? feast.CauldronLevel : 1;
            ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig {
                Item = matId, CraftingStation = stationPrefab, MinStationLevel = level, Amount = 1, Requirements = reqs.ToArray()
            }));
        }

        private void SetupFeastItem(ItemDrop drop, FeastSpec feast, string key, Sprite icon)
        {
            if (drop == null) throw new Exception("ItemDrop fehlt");
            var shared = drop.m_itemData.m_shared;
            shared.m_name = "$" + key;
            shared.m_description = "$" + key + "_desc";
            shared.m_food = feast.Hp;
            shared.m_foodStamina = feast.Stamina;
            shared.m_foodEitr = feast.Eitr;
            shared.m_foodRegen = feast.Regen;
            if (icon != null) shared.m_icons = new[] { icon };
        }

        /// <summary>Faerbt die Modelle eines Klons leicht ein (eigene Material-Kopien, Vanilla bleibt unveraendert).
        /// Partikel (Dampf, Rauch) werden nicht eingefaerbt.</summary>
        private void TintModel(GameObject root, Color tint)
        {
            const float strength = 1.0f;
            var factor = Color.Lerp(Color.white, tint, strength);
            var copies = new Dictionary<Material, Material>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.GetType().Name == "ParticleSystemRenderer") continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    Material copy;
                    if (!copies.TryGetValue(m, out copy))
                    {
                        copy = new Material(m) { name = m.name + "_VFP" };
                        foreach (var prop in new[] { "_Color", "_BaseColor", "_MainColor", "_TintColor" })
                            if (copy.HasProperty(prop))
                            {
                                var c = copy.GetColor(prop);
                                copy.SetColor(prop, new Color(c.r * factor.r, c.g * factor.g, c.b * factor.b, c.a));
                            }
                        copies[m] = copy;
                    }
                    mats[i] = copy;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>Ersetzt in allen Komponenten eines Klons Verweise auf ein Vanilla-Item durch unser Item.</summary>
        private void ReplaceItemRefs(GameObject root, GameObject oldItem, GameObject newItem)
        {
            var oldDrop = oldItem.GetComponent<ItemDrop>();
            var newDrop = newItem.GetComponent<ItemDrop>();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null || comp is Transform) continue;
                foreach (var f in comp.GetType().GetFields(flags))
                {
                    try
                    {
                        if (f.IsInitOnly || f.IsLiteral) continue;
                        var v = f.GetValue(comp);
                        if (v == null) continue;
                        if (f.FieldType == typeof(ItemDrop)) { if (ReferenceEquals(v, oldDrop)) f.SetValue(comp, newDrop); }
                        else if (f.FieldType == typeof(GameObject)) { if (ReferenceEquals(v, oldItem)) f.SetValue(comp, newItem); }
                        else if (f.FieldType == typeof(string)) { if ((string)v == oldItem.name) f.SetValue(comp, newItem.name); }
                        else if (v is IList list)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                if (ReferenceEquals(list[i], oldDrop)) list[i] = newDrop;
                                else if (ReferenceEquals(list[i], oldItem)) list[i] = newItem;
                            }
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private Sprite LoadSprite(string id)
        {
            var asm = typeof(VikingFoodPlus).Assembly;
            using (var stream = asm.GetManifestResourceStream("VikingFoodPlus.Icons." + id + ".png"))
            {
                if (stream == null) { Logger.LogWarning("Icon fehlt: " + id); return null; }
                var bytes = new byte[stream.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0) return null;
                    offset += read;
                }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, bytes)) return null;
                tex.name = "VFP_Icon_" + id;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
            }
        }

        private sealed class FeastSpec
        {
            public readonly string Id, German, English, DescDe, DescEn, VanillaBase, Ingredients;
            public readonly float Hp, Stamina, Eitr, Regen;
            public readonly int CauldronLevel;
            public readonly Color Tint;
            public FeastSpec(string id, string german, string english, string descDe, string descEn, string vanillaBase, string ingredients,
                float hp, float stamina, float eitr, float regen, int cauldronLevel, Color tint)
            { Id = id; German = german; English = english; DescDe = descDe; DescEn = descEn; VanillaBase = vanillaBase; Ingredients = ingredients;
              Hp = hp; Stamina = stamina; Eitr = eitr; Regen = regen; CauldronLevel = cauldronLevel; Tint = tint; }
        }

        private T ReadField<T>(object obj, string name, T fallback)
        {
            var f = obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) return fallback;
            var v = f.GetValue(obj);
            return v is T typed ? typed : fallback;
        }

        private RequirementConfig[] ParseRequirements(string spec)
        {
            var list = new List<RequirementConfig>();
            foreach (var part in spec.Split(','))
            {
                var tokens = part.Trim().Split(':');
                int amount;
                if (tokens.Length != 2 || !int.TryParse(tokens[1], out amount)) { Logger.LogWarning("Ungueltige Baukosten: " + part); continue; }
                if (PrefabManager.Instance.GetPrefab(tokens[0]) == null) { Logger.LogWarning("Baukosten: Prefab nicht gefunden, uebersprungen: " + tokens[0]); continue; }
                list.Add(new RequirementConfig(tokens[0], amount));
            }
            if (list.Count == 0) throw new Exception("Keine gueltigen Baukosten in der Config");
            return list.ToArray();
        }

        private void RegisterExtension(string id, string basePrefab, string de, string en, int style, RequirementConfig[] requirements)
        {
            var go = PrefabManager.Instance.CreateClonedPrefab(id, basePrefab);
            if (go == null) throw new Exception(basePrefab + " missing");
            var piece = go.GetComponent<Piece>();
            if (piece == null) throw new Exception("Piece missing");
            piece.m_name = "$vfp_" + id.Substring(4).ToLowerInvariant();
            piece.m_description = "$vfp_" + id.Substring(4).ToLowerInvariant() + "_desc";
            // Distinct construction-menu icon for each Runenkessel extension.
            try { ApplyPieceIcon(piece, style == 2 ? "BuildFermentBarrel" : style == 3 ? "BuildRuneDryingRack" : "BuildIceDistillery"); }
            catch (Exception ex) { Logger.LogWarning("Baumenü-Icon " + id + ": " + ex.Message); }
            // Workbench extension already contains Valheim's native StationExtension component.
            // Point it to our cauldron prefab. Reflection avoids relying on field visibility in game versions.
            if (go.GetComponent<Fermenter>() != null) PrepareBarrelFermenter(go);
            var ext = go.GetComponent<StationExtension>();
            if (ext == null) throw new Exception("StationExtension missing");
            // Groesserer Wirkungsabstand zum Kessel (naeher platzieren funktioniert weiterhin).
            var distField = typeof(StationExtension).GetField("m_maxStationDistance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (distField != null && distField.FieldType == typeof(float)) distField.SetValue(ext, extensionDistance.Value);
            else Logger.LogWarning("Erweiterung " + id + ": StationExtension.m_maxStationDistance nicht gefunden, Abstand bleibt Vanilla");
            var field = typeof(StationExtension).GetField("m_craftingStation");
            if (field == null) throw new Exception("StationExtension.m_craftingStation missing");
            var stationPrefab = PrefabManager.Instance.GetPrefab(Station);
            if (stationPrefab == null) throw new Exception("Runenkessel prefab missing");
            var target = stationPrefab.GetComponent<CraftingStation>();
            if (field.FieldType == typeof(CraftingStation)) field.SetValue(ext, target);
            else if (field.FieldType == typeof(string)) field.SetValue(ext, Station);
            else throw new Exception("Unsupported crafting station field: " + field.FieldType);
            // Disable cloned workbench visuals while retaining the extension mechanics.
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            Decorate(go, style);
            if (go.GetComponent<Fermenter>() != null)
            {
                try { SetupBarrelStates(go); } catch (Exception ex) { Logger.LogWarning("Gaerfass-Zustaende: " + ex.Message); }
            }
            // Erweiterung insgesamt kleiner (Modell und Kollision zusammen).
            go.transform.localScale = Vector3.one * extensionScale.Value;
            PieceManager.Instance.AddPiece(new CustomPiece(go, true, new PieceConfig {
                PieceTable = "_HammerPieceTable", Category = "Crafting", Requirements = requirements
            }));
        }

        private static Material PieceBaseMaterial(string prefabName)
        {
            var src = PrefabManager.Instance.GetPrefab(prefabName);
            if (src == null) return null;
            Material first = null;
            foreach (var r in src.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null || m.shader == null) continue;
                if (m.shader.name.Contains("Piece")) return m;   // Valheim "Custom/Piece"
                if (first == null) first = m;
            }
            return first;
        }

        /// <summary>Gerichte mit eigenem 3D-Modell (Boden und Objektstaender) statt des geklonten Valheim-Modells.</summary>
        private static readonly HashSet<string> DishModels = new HashSet<string> { "MidsummerCake", "TurnipGhostSoup", "YuleRoast" };
        /// <summary>Alle normalen Gerichte haben eigene Modelle (Models/VFP_Food_*.obj).</summary>
        private const bool FoodModels = true;
        /// <summary>Einheitliche Breite aller Gericht-Modelle auf Objektstaender und Boden (Meter).</summary>
        private const float DishWidth = 0.48f;

        /// <summary>Ersetzt das Weltmodell eines Gerichts durch ein eigenes OBJ-Modell. Liegt am Kind "attach" (das zeigt der
        /// Objektstaender), skaliert auf die breiteste Seite des Vanilla-Modells.</summary>
        /// <summary>Feinkorrektur einzelner Modelle: Groessenfaktor, Hoehe in Metern, 1 = gegen die Schraeglage von "attach" gerade ausrichten.</summary>
        private static readonly Dictionary<string, float[]> DishModelTweaks = new Dictionary<string, float[]> {
        };

        internal void ApplyDishModel(GameObject item, string model)
        {
            var attach = item.transform.Find("attach");
            var parent = attach != null ? attach : item.transform;
            Bounds vb = new Bounds(); bool have = false;
            foreach (var mf in parent.GetComponentsInChildren<MeshFilter>(true))
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                foreach (var c in new[] { b.min, b.max, new Vector3(b.min.x, b.max.y, b.min.z), new Vector3(b.max.x, b.min.y, b.max.z),
                                          new Vector3(b.min.x, b.min.y, b.max.z), new Vector3(b.max.x, b.max.y, b.min.z) })
                {
                    var pt = parent.InverseTransformPoint(mf.transform.TransformPoint(c));
                    if (!have) { vb = new Bounds(pt, Vector3.zero); have = true; } else vb.Encapsulate(pt);
                }
            }
            if (!have) vb = new Bounds(Vector3.zero, Vector3.one * 0.3f);
            var shader = PieceBaseMaterial("piece_workbench_ext1");
            if (shader == null) foreach (var mr in item.GetComponentsInChildren<MeshRenderer>(true)) if (shader == null && mr.sharedMaterial != null) shader = mr.sharedMaterial;
            if (shader == null) throw new Exception("kein Material");
            foreach (var r in item.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var geo = ModelLoader.Build(parent.gameObject, model, shader);
            // Der Objektstaender uebernimmt die Drehung von "attach" (bei Vanilla-Essen oft schraeg) -> Modell gerade ausrichten
            float[] tw0;
            if (attach != null && DishModelTweaks.TryGetValue(model, out tw0) && tw0.Length > 2 && tw0[2] > 0f)
                geo.transform.localRotation = Quaternion.Inverse(attach.localRotation);
            Bounds gb = new Bounds(); bool g = false;
            foreach (var mf in geo.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                foreach (var c in new[] { b.min, b.max, new Vector3(b.min.x, b.max.y, b.min.z), new Vector3(b.max.x, b.min.y, b.max.z),
                                          new Vector3(b.min.x, b.min.y, b.max.z), new Vector3(b.max.x, b.max.y, b.min.z) })
                {
                    // Lage relativ zum Modell-Ursprung, aber in den Achsen von parent (nach der Drehung, Massstab 1)
                    var pt = parent.InverseTransformPoint(mf.transform.TransformPoint(c)) - geo.transform.localPosition;
                    if (!g) { gb = new Bounds(pt, Vector3.zero); g = true; } else gb.Encapsulate(pt);
                }
            }
            float size = g ? Mathf.Max(gb.size.x, gb.size.z) : 1f;
            float unit0 = Mathf.Abs(parent.lossyScale.x) > 1e-5f ? Mathf.Abs(parent.lossyScale.x) : 1f;
            float k = size > 0.001f ? (DishWidth / unit0) / size : 1f;   // alle Gerichte gleich breit
            float[] tw; float yOff = 0f;
            // feste Werte sind in Metern; "attach" kann skaliert sein -> in dessen lokale Einheiten umrechnen
            float unit = Mathf.Abs(parent.lossyScale.x) > 1e-5f ? Mathf.Abs(parent.lossyScale.x) : 1f;
            if (DishModelTweaks.TryGetValue(model, out tw)) { if (tw[0] > 0f && size > 0.001f) k = (tw[0] / unit) / size; yOff = tw[1] / unit; }
            geo.transform.localScale = Vector3.one * k;
            geo.transform.localPosition = new Vector3(vb.center.x - (g ? gb.center.x * k : 0f), vb.min.y - (g ? gb.min.y * k : 0f) + yOff, vb.center.z - (g ? gb.center.z * k : 0f));
        }

        private void Decorate(GameObject parent, int style)
        {
            // Eigene 3D-Modelle (eingebettete OBJ-Dateien, erzeugt mit tools/make_models.py)
            string model = style == 1 ? "VFP_RuneCauldron" : style == 2 ? "VFP_FermentBarrel" : style == 3 ? "VFP_RuneDryingRack" : "VFP_IceDistillery";
            // Quelle fuer den Shader: ein MeshRenderer des geklonten Vanilla-Prefabs (keine Partikel)
            // Gaerfass ist (mit Getraenken) ein Fermenter-Klon: dessen erstes Material nutzt einen Shader, der unsere
            // Farben ignoriert (Modell wird komplett weiss). Daher immer das bewaehrte Material der Werkbank-Erweiterung.
            Material baseMat = parent.GetComponent<Fermenter>() != null ? PieceBaseMaterial("piece_workbench_ext1") : null;
            if (baseMat == null)
                foreach (var renderer in parent.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.sharedMaterial != null) { baseMat = renderer.sharedMaterial; break; }
            if (baseMat == null) { Logger.LogWarning("No source material for custom geometry"); return; }
            try
            {
                ModelLoader.Build(parent, model, baseMat);
                if (style == 1 && parent.GetComponent<CauldronStageVisuals>() == null) parent.AddComponent<CauldronStageVisuals>();
                if (style >= 2 && parent.GetComponent<ExtensionStageVisuals>() == null) parent.AddComponent<ExtensionStageVisuals>().OwnTier = style;
            }
            catch (Exception ex) { Logger.LogError("Modell " + model + ": " + ex); }
        }

        private void RegisterFood(FoodSpec food)
        {
            string id = "VFP_" + food.Id;
            var go = PrefabManager.Instance.CreateClonedPrefab(id, "QueensJam");   // Vorlage steht auf dem Objektstaender gerade; Aussehen kommt vom eigenen Modell
            if (go == null) throw new Exception("Missing item model: " + food.Model);
            var item = go.GetComponent<ItemDrop>();
            if (item == null) throw new Exception("Missing ItemDrop on " + food.Model);
            var shared = item.m_itemData.m_shared;
            shared.m_name = "$vfp_" + food.Id.ToLowerInvariant();
            shared.m_description = "$vfp_" + food.Id.ToLowerInvariant() + "_desc";
            shared.m_food = health[food.Id].Value;
            shared.m_foodStamina = stamina[food.Id].Value;
            shared.m_foodEitr = eitr[food.Id].Value;
            shared.m_foodBurnTime = duration[food.Id].Value;
            if (food.Regen > 0) shared.m_foodRegen = regen[food.Id].Value;
            // Each recipe has its own embedded PNG icon.
            try { ApplyEmbeddedIcon(item, food.Id); }
            catch (Exception ex) { Logger.LogWarning("Icon " + food.Id + ": " + ex.Message); }
            BuffSpec buff;
            if (foodBuffs.Value && Buffs.TryGetValue(food.Id, out buff))
            {
                try
                {
                    Sprite icon = shared.m_icons != null && shared.m_icons.Length > 0 ? shared.m_icons[0] : null;
                    var effect = CreateBuff(food.Id, buff, icon);
                    if (effect != null) shared.m_consumeStatusEffect = effect;
                }
                catch (Exception ex) { Logger.LogWarning("Effekt " + food.Id + ": " + ex.Message); }
            }
            if (DishModels.Contains(food.Id) || FoodModels)
            {
                try { ApplyDishModel(go, DishModels.Contains(food.Id) ? "VFP_" + food.Id : "VFP_Food_" + food.Id); }
                catch (Exception ex) { Logger.LogWarning("Modell " + food.Id + ": " + ex.Message); }
            }
            ItemManager.Instance.AddItem(new CustomItem(go, true));
            var reqs = new List<RequirementConfig>();
            foreach (var part in ingredients[food.Id].Value.Split(','))
            {
                var tokens = part.Split(':');
                if (tokens.Length != 2) throw new Exception("Invalid ingredient " + part);
                string prefab = tokens[0];
                if (PrefabManager.Instance.GetPrefab(prefab) == null) throw new Exception("Missing ingredient prefab: " + prefab);
                reqs.Add(new RequirementConfig(prefab, int.Parse(tokens[1])));
            }
            // Fladenbrot wird als Teig am Runenkessel hergestellt und im Ofen gebacken.
            if (food.Id == "HeartyFlatbread") RegisterFlatbreadDough();
            string recipeItem = food.Id == "HeartyFlatbread" ? "VFP_HeartyFlatbreadDough" : id;
            foreach (var st in CookingStations())
            {
                ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig {
                    Name = "Recipe_" + recipeItem + (st == Station ? "" : "_Cauldron"),
                    Item = recipeItem, CraftingStation = st, MinStationLevel = food.Level,
                    Amount = portions[food.Id].Value, Requirements = reqs.ToArray(),
                    // Saisongerichte: wie Valheims Saisonrezepte deaktiviert, die aktive Saison schaltet sie frei
                    Enabled = !Seasons.ContainsKey(food.Id) || seasonalAlways.Value
                }));
            }
        }

        // Kochstationen laut Config: Runenkessel (Standard), Valheim-Kessel oder beide.
        private string[] CookingStations()
        {
            switch (cookingStation.Value)
            {
                case "Cauldron": return new[] { "piece_cauldron" };
                case "Both": return new[] { Station, "piece_cauldron" };
                default: return new[] { Station };
            }
        }

        private void RegisterFlatbreadDough()
        {
            const string doughId = "VFP_HeartyFlatbreadDough";
            var dough = PrefabManager.Instance.CreateClonedPrefab(doughId, "BreadDough");
            if (dough == null) throw new Exception("Vanilla BreadDough prefab not found");
            var drop = dough.GetComponent<ItemDrop>();
            if (drop == null) throw new Exception("BreadDough has no ItemDrop");
            drop.m_itemData.m_shared.m_name = "$vfp_flatbreaddough";
            drop.m_itemData.m_shared.m_description = "$vfp_flatbreaddough_desc";
            // No food bonuses on uncooked dough.
            drop.m_itemData.m_shared.m_food = 0;
            drop.m_itemData.m_shared.m_foodStamina = 0;
            drop.m_itemData.m_shared.m_foodEitr = 0;
            ItemManager.Instance.AddItem(new CustomItem(dough, true));
        }

        private void RegisterFlatbreadBaking()
        {
            // Copy the vanilla BreadDough -> Bread conversion, retaining the
            // oven's original cooking time and burning behavior.
            var oven = PrefabManager.Instance.GetPrefab("piece_oven");
            if (oven == null) throw new Exception("Vanilla piece_oven not found");
            var station = oven.GetComponent<CookingStation>();
            if (station == null) throw new Exception("Oven CookingStation not found");
            var field = typeof(CookingStation).GetField("m_conversion", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new Exception("CookingStation.m_conversion not found");
            var conversions = field.GetValue(station) as IList;
            if (conversions == null) throw new Exception("Oven conversion list unavailable");
            var cooked = PrefabManager.Instance.GetPrefab("VFP_HeartyFlatbread");
            var dough = PrefabManager.Instance.GetPrefab("VFP_HeartyFlatbreadDough");
            if (cooked == null || dough == null) throw new Exception("Flatbread item or dough missing");
            object template = null;
            foreach (var entry in conversions)
            {
                var from = entry.GetType().GetField("m_from")?.GetValue(entry) as ItemDrop;
                var to = entry.GetType().GetField("m_to")?.GetValue(entry) as ItemDrop;
                if (from != null && to != null && from.name.Contains("BreadDough") && to.name.Contains("Bread"))
                { template = entry; break; }
            }
            if (template == null) throw new Exception("Vanilla bread oven conversion not found");
            var conversion = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(template, null);
            conversion.GetType().GetField("m_from").SetValue(conversion, dough.GetComponent<ItemDrop>());
            conversion.GetType().GetField("m_to").SetValue(conversion, cooked.GetComponent<ItemDrop>());
            conversions.Add(conversion);
            Logger.LogInfo("Fladenbrot: Teig am Runenkessel, Backen im Kochofen registriert.");
        }

        private void ApplyPieceIcon(Piece piece, string id)
        {
            var asm = typeof(VikingFoodPlus).Assembly;
            string resource = "VikingFoodPlus.Icons." + id + ".png";
            using (var stream = asm.GetManifestResourceStream(resource))
            {
                if (stream == null) throw new Exception("Icon resource missing: " + resource);
                byte[] bytes = new byte[stream.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0) throw new Exception("Unable to read icon: " + resource);
                    offset += read;
                }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, bytes)) throw new Exception("Invalid PNG: " + resource);
                tex.name = "VFP_PieceIcon_" + id;
                piece.m_icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
            }
        }

        private void ApplyEmbeddedIcon(ItemDrop item, string id)
        {
            var asm = typeof(VikingFoodPlus).Assembly;
            string resource = "VikingFoodPlus.Icons." + id + ".png";
            using (var stream = asm.GetManifestResourceStream(resource))
            {
                if (stream == null) { Logger.LogWarning("Eigenes Icon fehlt: " + resource); return; }
                byte[] bytes = new byte[stream.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0) throw new Exception("Could not read embedded icon");
                    offset += read;
                }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, bytes)) throw new Exception("PNG decode failed");
                tex.name = "VFP_Icon_" + id;
                item.m_itemData.m_shared.m_icons = new[] {
                    Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f))
                };
            }
        }

        private void RegisterTranslations()
        {
            var loc = new CustomLocalization(Info.Metadata);
            var de = new Dictionary<string, string> {
                {"vfp_flatbreaddough", "Herzhafter Fladenbrot-Teig"},
                {"vfp_flatbreaddough_desc", "Im Kochofen zu herzhaftem Fladenbrot backen."},
                {"vfp_station", "Runenkessel"},
                {"vfp_station_desc", "Runenkessel für besondere Gerichte. Mit eingebautem Feuer: Holz nachlegen wie beim Lagerfeuer."},
                {"vfp_fire", "Kesselfeuer"},
                {"vfp_fire_fuel", "Brennstoff"},
                {"vfp_fire_add", "Holz nachlegen"},
                {"vfp_buff_poison", "Moorfest"},
                {"vfp_buff_poison_tip", "Etwas weniger Giftschaden."},
                {"vfp_buff_frost", "Frostschutz"},
                {"vfp_buff_frost_tip", "Etwas weniger Frostschaden."},
                {"vfp_buff_mind", "Klarer Geist"},
                {"vfp_buff_mind_tip", "Eitr erholt sich etwas schneller."},
                {"vfp_buff_fire", "Glutschutz"},
                {"vfp_buff_fire_tip", "Etwas weniger Feuerschaden."},
                {"vfp_buff_north", "Nordlichtkraft"},
                {"vfp_buff_north_tip", "Ausdauer erholt sich etwas schneller."},
                {"vfp_buff_summer", "Mittsommerfreude"},
                {"vfp_buff_summer_tip", "Ausdauer erholt sich etwas schneller."},
                {"vfp_buff_ghost", "Geisterkraft"},
                {"vfp_buff_ghost_tip", "Leben erholt sich etwas schneller."},
                {"vfp_buff_yule", "Julwärme"},
                {"vfp_buff_yule_tip", "Etwas weniger Frostschaden."},
                {"vfp_fermentbarrel", "Gärfass"},
                {"vfp_fermentbarrel_desc", "Erweiterung für den Runenkessel. Erhöht die Kochstufe auf 2. In der Nähe des Runenkessels aufstellen."},
                {"vfp_runedryingrack", "Runen-Trockengestell"},
                {"vfp_runedryingrack_desc", "Erweiterung für den Runenkessel. Erhöht die Kochstufe auf 3. In der Nähe des Runenkessels aufstellen."},
                {"vfp_icedistillery", "Eisdestillerie"},
                {"vfp_icedistillery_desc", "Erweiterung für den Runenkessel. Erhöht die Kochstufe auf 4 und schaltet die Gerichte des Nordens frei. In der Nähe des Runenkessels aufstellen."}
            };
            var en = new Dictionary<string, string> {
                {"vfp_flatbreaddough", "Hearty flatbread dough"},
                {"vfp_flatbreaddough_desc", "Bake in an oven to make hearty flatbread."},
                {"vfp_station", "Runic Cauldron"},
                {"vfp_station_desc", "A runic cauldron for preparing special dishes. Has a built-in fire: add wood like at a campfire."},
                {"vfp_fire", "Cauldron fire"},
                {"vfp_fire_fuel", "Fuel"},
                {"vfp_fire_add", "Add wood"},
                {"vfp_buff_poison", "Bog-hardened"},
                {"vfp_buff_poison_tip", "Slightly less poison damage."},
                {"vfp_buff_frost", "Frost ward"},
                {"vfp_buff_frost_tip", "Slightly less frost damage."},
                {"vfp_buff_mind", "Clear mind"},
                {"vfp_buff_mind_tip", "Eitr regenerates a little faster."},
                {"vfp_buff_fire", "Ember ward"},
                {"vfp_buff_fire_tip", "Slightly less fire damage."},
                {"vfp_buff_north", "Northern strength"},
                {"vfp_buff_north_tip", "Stamina regenerates a little faster."},
                {"vfp_buff_summer", "Midsummer joy"},
                {"vfp_buff_summer_tip", "Stamina regenerates a little faster."},
                {"vfp_buff_ghost", "Ghostly vigor"},
                {"vfp_buff_ghost_tip", "Health regenerates a little faster."},
                {"vfp_buff_yule", "Yule warmth"},
                {"vfp_buff_yule_tip", "Slightly less frost damage."},
                {"vfp_fermentbarrel", "Fermentation Barrel"},
                {"vfp_fermentbarrel_desc", "Runic Cauldron upgrade. Raises the cooking station level to 2. Place near the Runic Cauldron."},
                {"vfp_runedryingrack", "Runic Drying Rack"},
                {"vfp_runedryingrack_desc", "Runic Cauldron upgrade. Raises the cooking station level to 3. Place near the Runic Cauldron."},
                {"vfp_icedistillery", "Ice Distillery"},
                {"vfp_icedistillery_desc", "Runic Cauldron upgrade. Raises the cooking station level to 4 and unlocks the dishes of the North. Place near the Runic Cauldron."}
            };
            foreach (var food in Foods)
            {
                var key = "vfp_" + food.Id.ToLowerInvariant();
                de[key] = food.German;
                en[key] = food.English;
                string[] text;
                if (Descriptions.TryGetValue(food.Id, out text)) { de[key + "_desc"] = text[0]; en[key + "_desc"] = text[1]; }
                else { de[key + "_desc"] = "Kräftiges Wikingergericht."; en[key + "_desc"] = "A hearty Viking dish."; }
            }
            foreach (var feast in Feasts)
            {
                var key = "vfp_" + feast.Id.ToLowerInvariant();
                de[key] = feast.German; en[key] = feast.English;
                de[key + "_desc"] = feast.DescDe; en[key + "_desc"] = feast.DescEn;
            }
            if (drinksEnabled.Value) AddDrinkTranslations(de, en);
            de["vfp_sort_health"] = "Gesundheit"; en["vfp_sort_health"] = "Health";
            de["vfp_sort_stamina"] = "Ausdauer"; en["vfp_sort_stamina"] = "Stamina";
            de["vfp_sort_eitr"] = "Eitr"; en["vfp_sort_eitr"] = "Eitr";
            loc.AddTranslation("German", de);
            loc.AddTranslation("English", en);
            LocalizationManager.Instance.AddLocalization(loc);
        }

        private sealed class FoodSpec
        {
            public readonly string Id, German, English, Model, Ingredients;
            public readonly float Hp, Stamina, Eitr, Duration, Regen;
            public readonly int Level;
            public FoodSpec(string id, string german, string english, string model, float hp, float stamina, int level, string ingredients, float eitr=0, float duration=0, float regen=0)
            { Id=id; German=german; English=english; Model=model; Hp=hp; Stamina=stamina; Eitr=eitr; Level=level; Ingredients=ingredients; Duration=duration>0?duration:(level==1?1200:(level==2?1500:1800)); Regen=regen; }
        }
    }
}
