# CHANGELOG

## v1.0.4

- Updated changelog and readme  
- Fixed description newlines not displaying correctly in published package md files  
- Fixed "x Addition" config descriptions referring to a multiplier rather than an addition  
- Added config options for modifying a gift box's scrap value and rarity
- Added config options for anomalously spawning gift boxes within a level, separate from the level's natural scrap pool mechanics
  
## v1.0.3

- Changed libs.Probability GetRandomWeightedIndex() error message to say "list" rather than "array"
- Added giftboxBehavior check to Start() transpiled method to properly handle error case of no behavior selected
- Fixes Start() transpiled method applying RoundManager.Instance.scrapValueMultiplier too early  
- Fixed Start() transpiler process not using Plugin.spawnGiftBoxChance.Value
- Fixed Start() transpiler process using filtered list .Clear() method directly instead of using setter = null  
  
## v1.0.1

- Updated icon, description, and changelog  
- Added LICENSE.md file to libs.Probability  
- Changed all LICENSE files to LICENSE.md  
- Small code cleanup  
- Added mod category tags  
  
## v1.0.0

- Released  
