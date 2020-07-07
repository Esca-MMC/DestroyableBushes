# Destroyable Bushes
A mod for the game Stardew Valley, allowing players to destroy every type of bush with an upgraded axe. Destroyed bushes drop small amounts of wood and respawn after 3 days. These features can be customized in the config.json file.

## Contents
* [Installation](#installation)
* [Options](#options)

## Installation
1. **Install the latest version of [SMAPI](https://smapi.io/).**
2. **Download Destroyable Bushes** from [the Releases page on GitHub](https://github.com/Esca-MMC/DestroyableBushes/releases), Nexus Mods, or ModDrop.
3. **Unzip Destroyable Bushes** into the `Stardew Valley\Mods` folder.

Multiplayer notes:
* This mod should affect each player separately. Players who want to destroy bushes should install Destroyable Bushes and customize their own options as needed.
* The host player's "WhenBushesRegrow" setting will be used.

## Options

Destroyable Bushes includes options to only affect certain in-game locations. The amount of wood dropped by each bush type can also be customized.

To edit these options:

1. **Run the game** using SMAPI. This will generate the mod's **config.json** file in the `Stardew Valley\Mods\DestroyableBushes` folder.
2. **Exit the game** and open the **config.json** file with any text editing program.

This mod also supports [spacechase0](https://github.com/spacechase0)'s [Generic Mod Config Menu](https://spacechase0.com/mods/stardew-valley/generic-mod-config-menu/) (GMCM). Players with that mod will be able to change config.json settings from Stardew's main menu.

The available settings are:

Name | Valid settings | Description
-----|----------------|------------
AllBushesAreDestroyable | **true** or false | If true, bushes at every in-game location will be destroyable. If false, only locations in the list "DestroyableBushLocations" list will be destroyable.
DestroyableBushLocations | A list of location names, e.g. `["farm", "forest", "woods"] | A list of locations where bushes will be destroyable (if AllBushesAreDestroyable is false). Names should be in quotation marks and separated by commas. To find a location's "proper" name, you may need to use another mod such as [Debug Mode](https://www.nexusmods.com/stardewvalley/mods/679/).
WhenBushesRegrow | A number and a unit of time, e.g. **"3 days"** (or *null* to never regrow bushes) | If the unit is "days", bushes will respawn after that number of days. "Seasons" (or "months") will respawn bushes after that many seasons (on the first day of the season). "Years" will respawn bushes after that many years (on the first day of Spring).
AmountOfWoodDropped | N/A | The settings below control how many pieces of wood are dropped by each bush type when destroyed. Players with the Forester profession will receive 25% more wood.
SmallBushes | A positive integer (default **2**) | The number of wood pieces dropped by small bushes when destroyed.
MediumBushes | A positive integer (default **4**) | The number of wood pieces dropped by medium bushes when destroyed.
LargeBushes | A positive integer (default **8**) | The number of wood pieces dropped by large bushes when destroyed.
GreenTeaBushes | A positive integer (default **0**) | The number of wood pieces dropped by green tea bushes when destroyed.