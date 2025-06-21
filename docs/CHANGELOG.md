# CHANGELOG  
  
## v1.3.0  
  
- Added per-item settings, such as blacklist, selection weight multiplier and addition, and chance to spawn extra instances  
- Multipliers are now applied before Additions rather than after. This makes it easier to override settings to a certain value by setting the multiplier to a guaranteed 0 and the addition to a guaranteed desired value  
- Added chances for a gift box to spawn extra instances of its contained item per giftbox behavior  
- Fixed bug where Price Influence (%) was not being used (wow)  
- Improved logic for determining available store items and scraps to spawn, and their selection weights  
  
## v1.2.0  
  
- Moved internal libraries into external utility packages  
- Added "Spawn Weight Can Be Zero" config value that must be explicitly set to true for scrap items with 0 rarity to be selectable by the gift box  
- Added "Must Be Buyable" config value that determines if store items must be accessible through the terminal to be selectable by the gift box
- Added "Position-Based Randomness Influence (%)" config value that modifies the amount of influence the gift box's spawn position has on its randomness  
- Improved gift box selection behavior to be more robust against incompatibilities / situations where scrap / item selection fails  
- Fixed RoundManager's scrap value multiplier not being applied to scrap (lmao)  
- Improved value/rarity multipliers logic to use rounding instead of truncating, and fixed maximum multiplier value being impossible
  
## v1.1.1  
  
- Improved mod compatibility by using branch instructions instead of return instructions, as to no longer prevent postfixes from running on modified methods  
- Modified RoundManager patch behavior to result in a warning, rather than an ArgumentOutOfRangeException and a softlock, when another mod causes the RoundManager's spawnableScrap list to be mismatched with the scrap spawn weights array  
- Minor code improvements  
  
## v1.1.0  
  
- Added more detailed reasoning to credits section of readme  
- Updated Eggsplosion logic to use RPC so it will not desync  
- Improved compatibility with other mods (namely Scarlet Devil Mansion)  
- Fixed gift boxes not correctly retaining modded behavior when loading one from a save file  
  
## v1.0.6  
  
- Added missing v1.0.5 changes to the changelog  
- Improved readme formatting for Gale mod manager  
  
## v1.0.5  
  
- Fixed Eggsplosion chance not using the Eggsplosion Chance config  
- Fixed potential desync of Eggsplosion Chance  
  
## v1.0.4  
  
- HUGELY IMPROVED config value names (old config values will be automatically migrated)  
- Fixed description newlines not displaying correctly in published package md files  
- Fixed "x Addition" config descriptions referring to a multiplier rather than an addition  
- Added config options for modifying a gift box's scrap value and rarity  
- Added config options for anomalously spawning gift boxes within a level, separate from the level's natural scrap pool mechanics  
- Added bugfix to prevent the giftbox from playing duplicate sounds when opened + added config specifically targeting this bugfix  
- Fixed `libs.Probability` `GetRandomWeightedIndex()` using `>=` instead of `>` which could result in probabilities with 0 weight being selected  
- Updated changelog and readme  
  
## v1.0.3  
  
- Changed `libs.Probability` `GetRandomWeightedIndex()` error message to say "list" rather than "array"  
- Added giftboxBehavior check to `Start()` transpiled method to properly handle error case of no behavior selected  
- Fixed `Start()` transpiled method applying `RoundManager.Instance.scrapValueMultiplier` too early  
- Fixed `Start()` transpiler process not using `Plugin.spawnGiftBoxChance.Value`  
- Fixed `Start()` transpiler process using filtered list `.Clear()` method directly instead of setting to `null`  
  
## v1.0.1  
  
- Updated icon, description, and changelog  
- Added `LICENSE.md` file to libs.Probability  
- Changed all `LICENSE` files to `LICENSE.md`  
- Small code cleanup  
- Added mod category tags  
  
## v1.0.0  
  
- Released  
  