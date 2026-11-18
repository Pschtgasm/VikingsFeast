# Changelog

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
