# Changelog

## 1.6.14
- Fixed: in 1.6.13 the scroll roll was not active because Valheim renamed the method that opens chests. Both names are now supported.

## 1.6.13
- Fixed: recipe scrolls did not appear in chests on servers. The scroll is now rolled when a player opens a matching chest for the first time, no longer when the chest is created. This works no matter which player loaded the dungeon first, and also for chests that existed before the update. Each chest rolls only once.
- `LogChests` now also writes the scroll roll for each chest to the log (matching chest, roll, recipe already known).

## 1.6.12
- Changed: the built-in fire under the Runic Cauldron now always stays full. The wood never runs out and you no longer need to add any.
- New config option `General > CauldronFireInfinite` (default `true`). Set it to `false` to have the fire burn down like a campfire again. Takes effect immediately.

## 1.6.11
- Includes everything from 1.5.3 (3D models for all 24 regular dishes and the 3 seasonal dishes).
- Changelog now also lists 1.5.3.

## 1.6.10
- The secret dishes now also have the same size of about 48 cm on item stands and on the ground as all other dishes, and stand upright.

## 1.6.9
- Sizes of the secret dishes on item stands evened out: long, narrow dishes (sulphur skewer, blood sausage, roulade) are larger now, so all of them look about the same size.
- Removed the harmless "Icon fehlt: Mead_…" warnings for the four group kegs in the log.

## 1.6.8
- Fixed: in 1.6.7 the secret dishes were invisible on item stands and on the ground (the fixed size was not converted to the scale of the model, so they became tiny).

## 1.6.7
- Fixed: secret dishes could lie tilted on item stands because they inherited the orientation of different vanilla foods. All secret dishes now use the same upright base (like Ymir's pancake tower) and a fixed size of about 40 cm.

## 1.6.6
- Fixed: the Fuling fried egg still disappeared partly into neighbouring table planks next to an item stand. It is now a bit smaller (80%) and lies 7 cm higher.

## 1.6.5
- Fixed: the Fuling fried egg lay tilted on item stands (one side and the back bacon slice sank into the stand). It is now levelled and sits on top.
- Build script copies the test DLL directly into `BepInEx\plugins` of the test profile.

## 1.6.4
- Fixed: Ymir's pancake tower was too small on item stands (now 1.8 times larger).
- Fixed: the Fuling fried egg sank halfway into item stands; it now lies on top.
- Fixed: the Deep North scroll (Ymir's pancake tower) is now sold after Kall Fimbulbringer's final phase (`defeated_frozenking_p3`).

## 1.6.3
- Changed: Haldor now sells every recipe scroll once the boss of its biome has been defeated in your world (The Elder, Bonemass, Moder, Yagluth, The Queen, Fader, Kall Fimbulbringer). Before, only Haldor (grave shroom) and the Bog Witch (blood sausage) sold scrolls, without any condition. Scrolls you already know are not offered.
- New config per dish: `SoldBy` (trader, default `haldor`) and `SoldAfterBoss` (global key of the boss, empty = right away).
- New: own 3D models for the 7 secret dishes on the ground and on item stands.
- The game log now lists all bosses with their keys once per start, to check `SoldAfterBoss`.

## 1.6.2
- New: own 3D models for the seasonal dishes on the ground and on item stands (Midsummer berry cake on a board, grinning turnip broth with slime blobs, Yule roast with carrots and mistletoe). Before, they showed the vanilla jam jar, turnip stew and deer meat.

## 1.6.1
- Config reorganised: `1 - General`, `2 - Food`, `3 - Drinks`, `4 - Secret Recipes`, `5 - Seasonal` at the top, single dishes and drinks at the end. General settings start with their default values once; settings for single dishes and drinks are kept.
- Seasonal dishes can now be cooked all year by default (`5 - Seasonal > AlwaysAvailable = true`).
- Fermentation time of the barrel now shows its real default (`FermentTime = 2400` s, like the vanilla Fermenter).
- Removed from the config: drinking sounds (fixed: burp for single meads, drinking + burp for kegs), size and range of the upgrades (fixed: 0.75 and 10 m) and the internal `BalanceVersion`.

## 1.6.0
- New: 7 secret recipes, one per biome from the Black Forest to the Deep North. Each has its own icon and a 10-minute special effect:

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
- New: recipe scrolls. Secret recipes are learned with a scroll (right-click). Scrolls are found in chests of matching places (burial chambers, troll caves, sunken crypts, frost caves, fuling villages, dvergr ruins and infested mines, Ashlands fortresses, Deep North) and sold by Haldor and the Bog Witch. Each scroll has its own icon and a wax seal in the colour of its biome. Once you know a recipe, its scroll no longer appears in chests or at traders.
- New: the group "Secret recipes (x/7)" at the Runic Cauldron. Learned recipes are listed normally; recipes not learned yet appear as "???"; clicking one shows on the right where its scroll can be found, without revealing the dish.
- New config section `Secret Recipes`: `Enabled`, `NeedScroll` (false = secret recipes unlock like normal recipes), `ScrollChance` (default 12%), `ScrollPrice` (default 250 coins), `LogChests` (writes chest names to the log to adjust the loot places). Per dish `Secret - <ID>`: on/off, values, ingredients, portions, `LootChests` and `Trader`.

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
