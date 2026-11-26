# Changelog

## 1.8.0
- New: victory feasts. One feast per boss, made from the boss trophy and ingredients of its biome at the Runic Cauldron (new group "Victory feasts (x/8)"), placed with the Serving Tray. 10 servings for the whole group, each a bit stronger than the best food of its biome, plus a 30-minute bonus:

  | Boss | Victory feast | Health / Stamina / Eitr | Bonus |
  |---|---|---|---|
  | Eikthyr | Stag king's roast | 38 / 38 / – | Move 10% faster |
  | The Elder | Elder's root feast | 48 / 48 / – | Attacks and chopping wood cost 15% less stamina |
  | Bonemass | Bog banquet | 58 / 58 / – | Less poison damage, health regenerates 25% faster |
  | Moder | Dragon feast | 70 / 70 / – | Less frost damage, 50% less fall damage |
  | Yagluth | Fire king's table | 80 / 80 / – | Your hits deal an extra 10% fire damage |
  | The Queen | Queen's banquet | 90 / 90 / 45 | +100 carry weight, 15% less damage from seekers, gjall and ticks |
  | Fader | Ember victory meal | 110 / 110 / 55 | Fire damage halved |
  | Kall Fimbulbringer | Fimbul banquet | 120 / 120 / 60 | Less frost damage, stamina regenerates 15% faster |

  - A victory feast unlocks once its boss has been defeated in your world. Until then it shows up as "???" with a hint which boss to defeat.
  - The first time you eat from a victory feast you get a message, e.g. "You celebrate the victory over Moder!".
  - Each platter looks like the vanilla feast of the boss's biome, tinted in the boss's colour, with the boss trophy on the platter as decoration.
- New config section `7 - Victory Feasts`: `Enabled`, `NeedBossKill`, `ShowMessage`, `TrophyDecoration`. Per feast `Victory - <Boss>`: on/off and ingredients, plus the position of the trophy on the platter (`TrophyTurn`, `TrophyTilt`, `TrophySize`, `TrophyHeight`, `TrophyBack`, `TrophySide`).
- Changed: animal feed icons reworked in Valheim style (light, texture, outline and shadow instead of a glow).
- Fixed: dishes, secret recipes and the flatbread dough still pointed to the vanilla item they were copied from. Spawn tools like Easy Spawner therefore listed many of them as "Queen's jam".

## 1.7.1
- Changed: the mod description on Thunderstore is now much shorter. All recipes with their values, ingredients and effects are in a sortable online recipe sheet instead (link at the top of the description): https://claude.ai/artifact/HViqJptevjZptkcLeBPn3C
- New in the recipe sheet: a list of every ingredient and every recipe that uses it, to sort and filter by ingredient.
- No changes to the mod itself.

## 1.7.0
- New: animal feed. 6 feeds at the Runic Cauldron (new group "Animal feed") for boar, wolf, lox, asksvin and hen. Each feed lasts 30 minutes and makes the animal glow in its colour:

  | Biome | Feed | Station level | Ingredients | Effect | Glow |
  |---|---|---|---|---|---|
  | Meadows | Taming herb | 1 | 4 Dandelion, 4 Raspberries, 1 Honey | Wild animals tame twice as fast and stay calm: they are not frightened (taming keeps going) and do not attack players. Hitting the animal cancels this for 10 seconds | light yellow-green |
  | Black Forest | Freya's lure | 1 | 4 Blueberries, 3 Carrots, 2 Yellow mushrooms | Faster breeding (pregnancy chance x2, pregnancy time /2) | pink |
  | Swamp | Norns' blessing | 2 | 3 Turnips, 3 Thistle, 1 Guck | 25% chance that an offspring is born with one more star (up to 2 stars) | dark green |
  | Mountains | Fenrir's bite | 2 | 2 Wolf meat, 1 Onion, 1 Fenris claw | +25% damage | icy blue |
  | Plains | Armor grain | 2 | 6 Barley, 3 Flax, 3 Cloudberries | 25% less damage taken | golden |
  | Ashlands | Ember shell | 3 | 4 Vineberries, 3 Smoke puffs, 1 Proustite powder | Fire damage halved, heals 2% health every 5 seconds | orange |

  - Feeding: look at the animal and press the hotbar key (or right-click the feed in the inventory next to the animal), or drop the feed on the ground. Hungry animals walk to it, fed animals eat it when they stand right next to it.
  - At most 2 feeds per animal at the same time. A third feed replaces the oldest one, the same feed again restarts its time.
  - Feed also counts as a meal for the animal (taming and breeding).
  - The hover text of the animal shows each active feed with its remaining time. With 2 feeds the glow alternates between both colours.
  - Works in multiplayer: active feeds are stored on the animal and apply for all players.
- New config section `6 - Animal Feed`: `Enabled`, `Glow` (glow on/off, takes effect immediately), `Duration` (default 1800 s), `TamingSpeed`, `TamingCalm`, `BreedingSpeed`, `StarChance`, `MaxStars`, `DamageBonus`, `DamageReduction`, `FireReduction`, `HealPercent`. Per feed `Feed - <ID>`: on/off, ingredients and portions (default 2).
- New: every feed has its own icon and its own 3D model: an open burlap sack with a cord, filled with feed in the colour of its biome and topped with matching ingredients. Each sack carries its own glowing symbol: taming herb a twig, Freya's lure a heart, Norns' blessing a star, Fenrir's bite two crossed bearded axes, armor grain a Viking round shield, ember shell a flame in a shield.
- Feed can be placed on item stands like the dishes and has the same size there.

## 1.6.1
- Fixed: some players saw the mod texts in German although their game was set to another language (reported on a dedicated server). The texts are now registered for every Valheim language: German for German, English for all others.
- New: `1 - General > Language` (`Auto`, `English`, `German`, default `Auto`). Choose the language of the mod texts yourself, independent of the game language. Each player sets it in their own config.

## 1.6.0
- New: 7 secret recipes, one per biome from the Black Forest to the Deep North. Each has its own icon, its own 3D model and a 10-minute special effect:

  | Biome | Dish | Health | Stamina | Eitr | Effect |
  |---|---|---|---|---|---|
  | Black Forest | Stuffed grave shroom | 44 | 16 | – | Sneaking costs 25% less stamina |
  | Swamp | Bog witch blood sausage | 54 | 19 | – | +10% damage against undead |
  | Mountains | Frost wolf roulade | 22 | 64 | – | 50% less fall damage |
  | Plains | Fuling fried egg | 50 | 50 | – | Blocking costs 15% less stamina |
  | Mistlands | Dvergr mushroom loaf | 30 | 22 | 75 | Sprinting costs 15% less stamina |
  | Ashlands | Sulphur skewer | 104 | 36 | – | Dodge rolls cost 20% less stamina |
  | Deep North | Ymir's pancake tower | 40 | 88 | 32 | You stagger 25% less |

  No secret dish is stronger in total than the best Valheim food of its biome.
- New: recipe scrolls. Secret recipes are learned with a scroll (right-click). Each scroll has its own icon and a wax seal in the colour of its biome.
  - Scrolls are found in chests of matching places: burial chambers and troll caves, sunken crypts, frost caves, fuling villages, dvergr ruins and infested mines, Ashlands fortresses and the Deep North.
  - Each chest rolls once, when it is opened for the first time (default chance 12%). This also works on servers and for chests that existed before the update.
  - Haldor sells every scroll once the boss of its biome has been defeated in your world (The Elder, Bonemass, Moder, Yagluth, The Queen, Fader, Kall Fimbulbringer).
  - Once you know a recipe, its scroll no longer appears in chests or at traders.
- New: the group "Secret recipes (x/7)" at the Runic Cauldron. Learned recipes are listed normally, recipes not learned yet appear as "???". Clicking one shows on the right where its scroll can be found, without revealing the dish.
- Changed: the built-in fire under the Runic Cauldron now always stays full. The wood never runs out and you no longer need to add any. Set `General > CauldronFireInfinite = false` to have it burn down like a campfire again.
- Changed: seasonal dishes can now be cooked all year by default (`5 - Seasonal > AlwaysAvailable = true`).
- Config reorganised: `1 - General`, `2 - Food`, `3 - Drinks`, `4 - Secret Recipes`, `5 - Seasonal` at the top, single dishes and drinks at the end. General settings start with their default values once; settings for single dishes and drinks are kept.
  - New in `4 - Secret Recipes`: `Enabled`, `NeedScroll` (false = secret recipes unlock like normal recipes), `ScrollChance` (default 0.12), `ScrollPrice` (default 250 coins), `LogChests` (writes each chest and its scroll roll to the log).
  - New per dish `Secret - <ID>`: on/off, values, ingredients, portions, `LootChests`, `SoldBy` and `SoldAfterBoss`.
  - New: `General > CauldronFireInfinite` (default `true`).
  - Fermentation time of the barrel now shows its real default (`FermentTime = 2400` s, like the vanilla Fermenter).
  - Removed: drinking sounds (fixed: burp for single meads, drinking + burp for kegs), size and range of the upgrades (fixed: 0.75 and 10 m) and the internal `BalanceVersion`.
- The game log lists all chests and all bosses with their keys once per start, to adjust `LootChests` and `SoldAfterBoss`.
- Removed the harmless "Icon fehlt: Mead_…" warnings for the four group kegs in the log.

## 1.5.3
- New: own 3D models for all 24 dishes and the 3 seasonal dishes (Midsummer Cake, Turnip Ghost Soup, Yule Roast). They match their icons and are shown when dropped on the ground and on item stands.
- On item stands all dishes now stand upright and have a uniform size of about 48 cm. Before, they showed the vanilla base foods, some of them tilted.

## 1.5.2
- Fixed: a finished mead keg taken out of the Fermentation Barrel showed a swamp feast platter on the ground. It now shows the keg with its symbol and coloured bands.
- New: sort buttons in the crafting menu. Three Valheim potions at the top right (healing, stamina and eitr mead) sort the dishes by health, stamina or eitr, highest first. Clicking the active potion again restores the normal order. Hovering over a potion shows what it sorts by (Health, Stamina, Eitr).
- New config option `General > SortButtons`: `RuneCauldron` (default, only at the Runic Cauldron), `Cooking` (Runic Cauldron, Cauldron, Food Preparation Table and Oven), `All` (every crafting station) or `Off`. Takes effect immediately.

## 1.5.1
- Fixed: the Fermentation Barrel was completely white when drinks were enabled. Its model now uses the same material as the other Runic Cauldron upgrades again.
- Short mod description on Thunderstore now mentions the drinks.

## 1.5.0
- New: drinks. 7 single meads and 4 mead kegs for groups, each with a small effect that Valheim does not have.
- Single meads (6 bottles, 10 minutes): make the mead base at the vanilla Mead Ketill and ferment it in the vanilla Fermenter or in the Fermentation Barrel.
  - Fisher's brew: fish bite more readily
  - Master cook's brew: eaten food lasts 10% longer
  - Hunter's brew: bows and crossbows draw 10% faster
  - Bog bitter: no wet effect
  - Helmsman's grog: side wind drives the ship 20% better (when steering)
  - Gardener's mead: 20% chance of an extra yield when harvesting and picking
  - Galdr mead: spells cost 10% less eitr
- Mead kegs (10 servings, 20 minutes): make the keg base at the Runic Cauldron (station level 2), ferment it in the Fermentation Barrel and place the keg with the Serving Tray.
  - Shield brothers' mead: -5% damage taken while a fellow player is nearby
  - Battle cry mead: +5% damage while a fellow player is nearby
  - Skald's brew: skills rise 10% faster
  - Rower's keg: +3% sailing speed per player on board (max +12%)
  - Feast bonus: a bit stronger with 3 or more players nearby. Rest bonus: you get rested faster.
- Every single mead has its own bottle model in its colour (on the ground and on item stands).
- At most 2 drinks are active at the same time; drinking a third ends the oldest.
- Runic Cauldron: the recipe list is split into two collapsible groups, Food and Mead (click the heading to open or close; config `[Drinks] RecipeGroups`).
- The Fermentation Barrel can now ferment mead (also without a roof), shows its state (empty, fermenting with foam, ready with mead at the tap) and the remaining time when you look at it (config `[Drinks] ShowFermentTime`, default on).
- Placed mead kegs show their symbol and two coloured bands on the barrel; drinking no longer plays the eating sound. New drinking sounds: single meads end with a burp, kegs play drinking + burp (config `DrinkSound`, `KegSound`: 0 = off, 1 = burp, 2 = drinking + burp).
- New config sections `[Drinks]` and `[Drink - <ID>]`: drinks on/off, ingredients, effect strength, duration, fermentation time, Mead Ketill / Runic Cauldron / vanilla Fermenter options.
- The mead bases show the effect of their mead in the tooltip.
- Fixed a floating iron ring above the collecting tank of the Fermentation Barrel.

## 1.4.7
- New food icons for all 24 dishes in Valheim's style: muted colours, fewer details, no glow.
- New build menu icons for the Runic Cauldron and its three upgrades, rendered from the actual models.
- Runic Cauldron and upgrades now use Valheim-like colours: darker wood, iron and stone, aged bone, cloudy glass and crystals, paler ice. The Mistlands stage is blue-violet instead of bright pink.
- Lanterns, crystals and glass only glow softly now, the same in every stage.
- The rune on the cauldron and the upgrades is replaced by a glowing sign for each stage: boar head (Standard), dead tree (Swamp), Seeker (Mistlands), snowflake (Deep North). The signs glow a bit brighter than the lanterns.
- New config option `[General] CookingStation`: cook the dishes at the Runic Cauldron (`RuneCauldron`, default), at the vanilla Cauldron (`Cauldron`) or at both (`Both`). At the vanilla Cauldron the dishes need the same station level, reached with the vanilla cauldron upgrades.

## 1.4.6
- 3 seasonal dishes that appear together with Valheim's seasonal events and disappear afterwards:
  Midsummer Berry Cake (Midsummer), Grinning Turnip Broth (Halloween), Yule Roast (Yule). Cooked dishes stay edible after the event.
- Seasonal dishes are 95-105 % of the best vanilla food of their biome (values can be changed in the config).
- Each seasonal dish has a small 10-minute effect: Midsummer joy (stamina regen +10 %), Ghostly vigor (health regen +10 %), Yule warmth (slightly less frost damage).
- New config options `[Seasonal] Enabled` and `AlwaysAvailable` (cook them all year).

## 1.4.5
- Rebalanced all dishes: their main stat is now 80-110 % of the best vanilla food of the same biome, based on how hard the ingredients are to get. Each dish has a clear focus (health, stamina or eitr).
- Dishes that need special fish (Trollfish, Northern Salmon, Pufferfish, Anglerfish) are at the upper end (about 110 %), because those fish are harder to get.
- All values can still be changed in the config (`[Dish - <ID>]` > `Health`, `Stamina`, `Eitr`).
- Health, stamina and eitr in existing configs are reset once to the new values. Other settings are kept.

## 1.4.4
- 3 new feast platters, placed with the vanilla Serving Tray (10 servings, 50 minutes, shared by everyone):
  Bog Feast (Swamp), Golden Harvest Table (Plains), Northern Lights Banquet (Deep North).
- Each platter has a focus instead of balanced stats: Bog Feast more health, Golden Harvest Table more stamina, Northern Lights Banquet more eitr.
- The platters keep the size and look of the vanilla feasts, slightly tinted (Bog Feast greenish, Golden Harvest Table golden, Northern Lights Banquet frosty blue), so they are easy to tell apart.
- Feasts are made at the Food Preparation Table by default, like in vanilla. Config `[Feasts] Station = RuneCauldron` moves them to the Runic Cauldron (station level 2/3/4). `[Feasts] Enabled` turns them off.
- Fixed: eating from the feast platters now gives the food stats.

## 1.4.3
- The Runic Cauldron now has a built-in fire. It works like a campfire: add wood, it burns down and goes out. The cauldron only cooks while the fire is burning.
- New config options under `[Piece - RuneCauldron]`: `IntegratedFire` (default `true`; set to `false` to use a separate fire like in vanilla) and `FireHeightOffset`.
- New build menu icon for the Runic Cauldron.
- Cauldron upgrades are smaller (75 % by default).
- Upgrades now count up to 10 m away from the Runic Cauldron (vanilla workbench: 5 m). Placing them close still works.
- New config options under `[Piece - Extensions]`: `Scale` (0.3 to 1.5) and `MaxDistance` (2 to 30 m).
- Every dish now has its own short description in Valheim style (German and English).
- 5 dishes now give a small extra effect for 10 minutes: Blood Seaweed Stew (slightly less poison damage), Frost Fish Roast (slightly less frost damage), Anglerfish Mist Soup (eitr regen +10 %), Ember Herb Soup (slightly less fire damage), Northern Lights Oat Porridge (stamina regen +10 %). Can be switched off with `[General] FoodBuffs = false`.
- While cooking at the Runic Cauldron, extra steam rises from the cauldron, coloured by its stage (standard, swamp green, mistlands purple, northern white-blue). Cooking time stays as in vanilla; it can be extended with `[Piece - RuneCauldron] CookTime`.
- New config option `[Piece - RuneCauldron] FireSmoke` (default `true`) to turn off the smoke of the built-in cauldron fire.

## 1.4.2
- New recipe values now apply automatically after updating: dish settings are stored under `[Dish - <ID>]` in the config. Old `[Food - …]` sections are ignored and can be deleted.
- Fish dishes need 2 fish (old configs could still show 3).

## 1.4.1
- Removed 15 dishes that were disabled by default. The mod now contains 24 dishes, all enabled.
  (If you had enabled one of the removed dishes, those items will disappear from your inventory.)
- Fixed Grilled Herb Fish and Frost Fish Roast showing no food stats in game (base item is now cooked fish).
- Fixed wrong item IDs; these dishes were skipped before and now work:
  Dragon Broth, Tick Broth, Volcanic Vulture Roast, Ember Herb Soup, Essence of Eternal Frost, Northern Lights Oat Porridge.
- Deep North ingredients verified against Valheim 1.0.
- Fish dishes now need 2 fish; Swamp Fish Stew uses Trollfish.
- Frost Fish Roast uses Northern Salmon, Pufferfish Barley Pan uses Pufferfish.
- Tick Broth uses Woodland Herb Blend, Dragon Broth uses Powdered Dragon Eggshells.
- German name fix: Vulkangeierbraten.
- Hearty Flatbread: dough at the Runic Cauldron, baked in the vanilla oven.
- New description with images and a full recipe table.

## 1.4.0
- Added three upgrades for the Runic Cauldron.
- The Runic Cauldron changes its appearance with each upgrade.
- Reworked food visuals and in-game icons.
- Improved transparency of selected food icons.
