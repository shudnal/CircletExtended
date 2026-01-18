# 1.1.8
* fixed the issue where circlet can be repaired at literally any crafting station
* more detailed configs for circlet crafting and repairing

# 1.1.7
* custom slots for AzuEPI and ExtraSlots can now be dynamically enabled and disabled (this also fixes the issue when slot is disabled on a client and enabled on a server)

# 1.1.6
* fixed circlet not appearing in upgrade and craft menu if mod is installed AFTER obtaining circlet
* fixed light factor of stealth system not respecting light being disabled

# 1.1.5
* fixed circlet making other tools not draining durability

# 1.1.4
* Call To Arms patch
* default craft recipe requirement changed to use raw Bronze ingots instead of Bronze Helmet
* new restriction options which helmet can be used along with Circlet
* you can set emote to play on overload activation
* minor overload fixes
* fix for Circlet not being repairable
* fix for rare incompatibility with InfinityHammer

# 1.1.3
* ServerSync updated

# 1.1.2
* patch 0.220.3

# 1.1.1
* Circlet slot will be available on acquiring when feature upgrade is disabled
* more detailed hint text when shadows toggling
* new config for toggling spot light with main light (disabled by default)

# 1.1.0
* default values for config changed a bit (reduced overload charges amount to 20 (40 at lvl4) but increased temporary demister time on overload; basic fuel capacity reduced to 120 reaching 480 at lvl4)
* many aspects of durability using were refined (fuel will only drain when light is on, circlet will be properly unequipped when broken)
* fixed in multiplayer overload effect spawn several times
* custom slot for ExtraSlots will only be active if you upgrade Circlet to lvl2 to make it more clear Circlet can go into custom slot only after upgrade
* spot light made disableable with server sync. In case you play with HipLantern mod to not duplicate its functions.
* spot light range and intensity made configurable with quality level
* default light toggle hotkey changed to G

# 1.0.19
* mod description updated to make more clear lvl1 Circlet works as helmet

# 1.0.18
* Circlet no longer drain fuel if crafting station is opened

# 1.0.17
* ExtraSlotsCustomSlots support

# 1.0.16
* fix for MMHOOK dependent mods generating warnings on ExtraSlots API initialization

# 1.0.15
* ExtraSlots API rework

# 1.0.14
* ExtraSlots API removed

# 1.0.13
* ExtraSlots custom slot compatibility
* minor refinements

# 1.0.12
* bog witch patch

# 1.0.11
* stands and equipment control refinements

# 1.0.10
* AzuEPI custom slot support

# 1.0.9
* fix for really rare exception on crafting

# 1.0.8
* circlet will be last in list to repair to prevent repair stuck

# 1.0.7
* upgrade recipes made configurable 
* added balanced recipe to craft Circlet

# 1.0.6
* circlet disabling on sleep fixed

# 1.0.5
* circlet doesn't remove wisp spawned from Wisplight utility item
* circlet state finally shared in multiplayer

# 1.0.4
* circlet state shared in multiplayer

# 1.0.3
* Ashlands
* Circlet gem will take color of circlet light color
* Circlet will not be unequipped on helmet unequipping
* Circlet will not hide currently equipped helmet instead circlet will be hidden but still provide light
* helmet and circlet could be visible at the same time (but by default only troll leather helmet looks fine)
* spotlight(radiance) will emit correct shadows and will have color of the main light
* Circlet will consume fuel over time when its main light is on. Default value is 6 hrs for basic circlet adding 2 hrs for every quality lvl resulting in 12 hrs at max quality.

# 1.0.2
* warmer default config color
* shadows enabled by default
* shadows visual improvements
* spot light disabled by default
* spot light is unavailable with circlet quality 1
* demister could be enabled with lights off
* overload pushes away the mists
* new config options to disable visual circlet state for item on the ground, item stands and armor stands (Jewelcrafting compatibility)

# 1.0.1
* fixed nasty bug when equipped in EAQS slots item dissapear

# 1.0.0
* Initial release