# 0.9.8.0 (Pre-release)

## Features:
- Update OpenTK to 4.9.4
- Dehacked blood color support for both palette and true color images.
- Blood color is passed to children through A_SpawnObject so Smooth Doom MBF21 blood spawners with blood color match parent blood.
- UDMF midtex3d and midtex3dimpassible line flags support. Includes physics for monsters to walk on without dropping off.
- Solo-net command that allows for cooperative gameplay rules in a single player game.
- HUD message text will wrap to HUD width.
- IWAD detection will fallback to reading lumps when MD5/filename checks fail for freedoom.
- Show IWAD selection screen IWAD fails to load with the -iwad parameter.

## Bug Fixes:
- Fix spawn ceiling sprite offsets for vanilla sprite rendering.
- Ignore GL nodes in archive and always build nodes internally to fix maps with bad GL nodes.
- Fix id24 pickups to skip using the sprite name for lookup.
- Fix id24 skies to have defaults set for when not defined outside of flatmapping and correct lookup for animations.
- Fix id24 sector offset/rotations not restoring after loading a save.
- Fix incorrect warnings for sounds and invalid bex string memonic with custom sounds prefixed with USER_.
- Fix UMAPINFO/MAPINFO EnterText/ExitText/SecretExitText/ExitText with escaped double quotes.
- Fix setting vanilla sky render mode with skydefs when only flatmapping is defined with no skies.
- Fix alignment with id24 skies.
- Fix sky fire foregrounds not rendering when used on two different sky backings.
- Fix A_AddFlags/A_RemoveFlags not updating sprite transparency when changing TRANSLUCENT flag.
- Fix A_AddFlags/A_RemoveFlags MaxTargetRange, MinMissileChance, and MeleeThreshold, Translucent editing globabl properties.
- Fix reading $ifgame in language file.
- Fix intermission patch graphics not using offset. Fixes Hell To Pay intermission screen.
- Fix middle textures not correctly blocking sprites from bleeding over lower textures with emulate vanilla rendering.
- Fix middle textures not rendering when moving sector is paused with emulate vanilla rendering.