# CHANGELOG  
  
## v1.1.0  
  
- Added more detailed reasoning to credits section of readme  
- Updated Eggsplosion logic to use RPC so it will not desync  
- Improved compatibility with other mods (namely Scarlet Devil Mansion)  
- Fixed gift boxes not correctly retained modded behavior when loading one from a save file  
  
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
  