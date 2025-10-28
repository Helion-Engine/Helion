# 0.9.8.0 (Pre-release)

## Features:
- Simplify bit packing strategy on vertex data using floatBitsToInt. Decreases entity sprite vertex size and increase range on line ids for vanilla sprite clipping emulation from 65k to over 8 million and sector light index from ~500k to over 1 million.
- Add support to initial loading to only load assets.pk3 once.
- Weapon switch pickup preferences and priority configuration. Includes options for always, always except when attacking, never, priority, and priority except when attacking.
- Add mouse support in main menu.
- Support numbers for map command line (e.g. map 1 for MAP01 or map 12 for E1M2).
- Small improvement on save performance in maps with a significant number of things.
- Save game serilization improvements on memory usage.
- Support doom UDMF namespace.
- Improved performance with emulate vanilla rendering.
- Vanilla movement physics compatibility option is now default.
- Add video option to simplify setting true color/palette with vanilla rendering options.
- Add randomly mirror corpses option similar to Retro/Crispy/Woof.
- Add option to select laptop GPU mode (high performance, vs power saving). Helion will automatically set high performance (discrete) GPU to prevent Windows from defaulting to the power saving (integrated) GPU.

## Bug Fixes:
- Fix sprite frames to correctly calculate when lowercase.
- Fix dehacked frames loading incorrectly when a wad contains a dehacked patch and a dehacked patch is applied with the -deh parameter.
- Fix NoBlockmap not working with scrolling floors and correctly ignore things with NoSector.
- Fix MBF21 monster kill sector to kill monsters that are below the highest floor.
- Fix A_CheckAmmo to be inclusive to fix check failing when player has exactly the correct amount of ammo.
- Fix A_RefireTo to correctly check and set the flash frame state.
- Fix touchy to not work with ripper projectiles.
- Fix A_SpawnObject to clear velocity if object is spawned below floor or above ceiling.
- Fix missiles not exploding on floors when floor hits the missile.
- Fix MF_BOUNCES flag to account for mass and implement velocity modifications from Boom.
- Fix dropoff check to work correctly MF_BOUNCES things and things on midtex3d lines crossing sectors.
- Fix line of sight checks to ignore lines where floor and ceiling heights are equal (fixes Eviternity MAP26 chain breaking sound).
- Fix reading pre-0.9.8.0 saves.
- Fix one-sided scrolling lines not interpolating.
- Fix borderless window option in Windows to not be promoted to exclusive fullscreen.
- Fix holes in floors that can be caused by self-referencing lines. (Fixes BTSX E1 MAP02 exit).
- Fix looping sounds with same sound limit feature not playing when coming back into player's sound range.
- Fix issue with lower wall gap correction and sprites.
- Fix UDMF upper scale scale calculation.
- Fix rendering of walls with negative scale values with pixel gap correction and correct offset handling.
- Fix A_BossDeath and A_KeenDie to exclude self. Fixes Tele-Direct Atlantic Doom blueprints not opening door.
- Fix logging missing sound errors when config option is off.
- Fix changing hud.scale from console to automatically disable hud.autoscale so it isn't modified on restart.
- Fix friendly monsters passing through two-sided impassible lines.