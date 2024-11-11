# Circlet Extended
![logo](https://staticdelivery.nexusmods.com/mods/3667/images/2617/2617-1701650283-1050290249.png)

Customize circlet light. Every circlet preserve its own state. Upgrade circlet to have new features: putting on top, overload and demister.

If you want another light sources you can also check [Firefly](https://thunderstore.io/c/valheim/p/shudnal/Firefly/) and [HipLantern](https://thunderstore.io/c/valheim/p/shudnal/HipLantern/) mods.

To make nights darker you can use [GammaOfNightLights](https://thunderstore.io/c/valheim/p/shudnal/GammaOfNightLights/) mod.

## Features
* change the light color
* change beam width
* change beam intensity (50 - 150 %)
* toggle the backlight (radiance around the circlet)
* disable the light when the bearer is in bed
* upgrade circlet to get new features (putting on top of the helmet, overload button, and demister)
* or disable the upgrade system to have all the features from the start
* toggle if the circlet should emit shadows
* or disable shadows at your side entirely if it impacts the performance while having another people playing with it
* configure light properties for every quality level
* circlet will consume fuel when it's equipped by player and its main light is on
* AzuEPI custom slot support
* ExtraSlots custom slot support

Each circlet can be configured individually. Circlet state is synced between clients.

## Extra features
Circlet could be crafted in forge
* Create - HelmetBronze x1, Ruby x1, Silver Necklace x1, Surtling Core x10

Circlet can not be crafted if you disabled getting features by quality upgrade as it disables the recipe completely.

Upgrade the circlet at the forge to get new features bound to quality level.
* Level 2 - Resin x20, Leather scraps x10, Iron nails x10, Chain x1
* Level 3 - Thunder stone x5, Silver x1, Red jute x2
* Level 4 - Wisplight, Black core

Recipes are balanced to get different features depending on overall progress. 

Default circlet have only configurable light. It easily can be used as configurable light source when it placed on a stand.

* To get a chance of possessing circlet if you are still yet to find Haldor but you're lucky to find Silver Necklace in meadows or mountain graves.
* To get a possibility to equip circlet while equipping a helmet you should discover iron and spend a valuable chain to get an upgrade.
* To get the overload ability which will be extra handy at the plains you should spend some money, discover silver and clear a couple of caves.
* To get an ability to spawn wisplight without need of equipping wisplight in utility slot and much more valuable ability to despawn wisplight when you are not in need you should craft a wisplight, clear some mines and spend valuable resource - black core.

### Put circlet on top

You can equip circlet without it taking an equipment helmet slot. It works with EAQS. The original helmet will stay and circlet will not provide armor and stats. You can disable that feature and circlet will always be visible instead of helmet.

You can configure with what helmet circet will be visible. Currently only troll leather helmet looks fine and it's enabled by default.

Circlet uses its own slot and custom item type. If you want Circlet to take utility slot you can set 18 in `Slot type` config.

### Overload
Blind your opponents with a bright flash at the cost of some circlet durability. 

The flash only affects enemies if their head is in the light cone. Also all the enemies next to you will be affected.

Blinded enemy will be staggered so you will have a chance to get a critical blow.

After each overload the circlet durability will be damaged a little. Overloaded circlet will take some time to restore its power.

Overload pushes away the Mistlands mists gradually decreasing for set amount of time.

### Demister

Spawn/despawn the wisplight at will. It's basically controllable wisplight with the utility slot freed for megingjord.

## Compatibility
The mod most likely will not work with others circlet altering mods
* The mod is incompatible with [ImprovedDvergerCirclet](https://valheim.thunderstore.io/package/RandyKnapp/ImprovedDvergerCirclet/) due to the same nature of the light controller
* The mod is incompatible with [Circlet Demister](https://valheim.thunderstore.io/package/Azumatt/Circlet_Demister/) due to demister cross functioning
* The circlets from [CustomDverger](https://valheim.thunderstore.io/package/OdinPlus/CustomDverger/) will not be controlled
* The light from [HeadLamp](https://valheim.thunderstore.io/package/Alpus/HeadLamp/) will not be controlled
* The compatibility with [CosmeticSlots](https://valheim.thunderstore.io/package/Frogger/CosmeticSlots/) needs further testing but it may work
* To provide compatibility with [Jewelcrafting](https://thunderstore.io/c/valheim/p/Smoothbrain/Jewelcrafting/) disable armor stand visual state
* If you experience incompatibility with any mod feel free to post on Nexus

## Installation (manual)
extract CircletExtended.dll file to your BepInEx\Plugins\ folder

## Configurating
The best way to handle configs is [Configuration Manager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/).

Or [Official BepInEx Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/).

## Mirrors
[Nexus](https://www.nexusmods.com/valheim/mods/2617)