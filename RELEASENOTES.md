# 0.9.8.0 (Pre-release)

## Features:
- Simplify bit packing strategy on vertex data using floatBitsToInt. Decreases entity sprite vertex size and increase range on line ids for vanilla sprite clipping emulation from 65k to over 8 million and sector light index from ~500k to over 1 million.

## Bug Fixes:
- Fix sprite frames to correctly calculate when lowercase.
- Fix dehacked frames loading incorrectly when a wad contains a dehacked patch and a dehacked patch is applied with the -deh parameter.
- Fix NoBlockmap not working with scrolling floors and correctly ignore things with NoSector.
- Fix MBF21 monster kill sector to kill monsters that are below the highest floor.
- Fix A_CheckAmmo to be inclusive to fix check failing when player has exactly the correct amount of ammo.
- Fix A_RefireTo to correctly check and set the flash frame state.
- Fix touchy to not work with ripper projectiles.
- Fix A_SpawnObject to clear velocity if object is spawned below floor or above ceiling.