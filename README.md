  
# LC_GiftBox_Config  
  
Configure gift boxes / presents, such as having store items / scrap / another present / nothing, weighting of item selection, spawn rates, etc.  
  
## Config Options  
  
### Contained Item Type  
  
* Store Item Chance (Selection Weight)  
* Scrap Item Chance (Selection Weight)  
* Gift Box Chance (Selection Weight)  
* Empty Chance (Selection Weight)  
* Unmodified Chance (Selection Weight)  
  
### Contained Scrap Item  
  
* Scrap Value Minimum  
* Scrap Value Maximum  
* Scrap Value Influence (%)  
* Spawn Weight Minimum  
* Spawn Weight Maximum  
* Spawn Weight Influence (%)  
* Spawn Weight Can Be Zero  
  
### Contained Store Item  
  
* Price Minimum  
* Price Maximum  
* Price Influence (%)  
  
### Contained Scrap Value  
  
* Inherit Gift Box Value Chance (%)  
* Addition Chance (%)  
* Addition Minimum  
* Addition Maximum  
* Multiplier Chance (%)  
* Multiplier Minimum  
* Multiplier Maximum  
  
### Gift Box Spawn Weight  
  
* Addition Chance (%)  
* Addition Minimum  
* Addition Maximum  
* Multiplier Chance (%)  
* Multiplier Minimum  
* Multiplier Maximum  
  
### Gift Box Spawn Anomaly  
  
*(An anomaly that causes additional gift boxes to spawn into a level, separately from the standard scrap pool mechanics)*  
  
* Anomalous Spawning Chance (%)  
* Minimum Gift Boxes  
* Maximum Gift Boxes  
  
### Gift Box Behaviors  
  
* Empty Gift Box Eggsplosion Chance (%)  
* Position-Based Randomness Influence (%)  
  
### Compatibility / Debugging  
  
* Disable All Mod Mechanics  
* Disable Gift Box Duplicate Sounds Bugfix  
* Disable Gift Box Setting Tool Scrap Value Bugfix  
  
## Credits  
  
* [Mom_Llama (@mamallama on LC Modding Discord)](https://thunderstore.io/c/lethal-company/p/Mom_Llama/) for creating this [LC Modding Template & Tutorial](https://lethalcompanymodding.github.io/Thunderstore/www/Guides/Your-First-Mod.html)  
* [malco (@Malcolm-Q on GitHub)](https://thunderstore.io/c/lethal-company/p/malco/) for creating this [IL Transpiler Helper](https://github.com/Malcolm-Q/LC-LateGameUpgrades/blob/main/MoreShipUpgrades/Misc/Util/Tools.cs), which inspired the design for the ILStepper class I created for this mod + future mods  
* [RedCrowbar (@landonk89 on GitHub)](https://thunderstore.io/c/lethal-company/p/RedCrowbar/) for creating this [Reflection Access Helper](https://github.com/landonk89/Buffed-Presents/blob/main/Source/AccessExtensions.cs), from which I learned how to use reflections and implement it within the ILStepper class  
  