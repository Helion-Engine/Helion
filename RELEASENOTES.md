# 0.9.9.1 (Pre-release)

## Features:
- Add SBARDEF.
- Add stacked message counts.
- Add ZDoom-styled centered HUD key notifications.
- Add SpawnMulti and SpawnMultiCoopOnly.
- Add support for new UMAPINFO keys 'jumping', 'crouching', and 'freeaim'.
- Add clear screen option in video options to emulate hall of mirrors effect where pixels are not draw.

## Bug Fixes:
- Fix not cooperative flag check for solo-net.
- Fix crash that can happen with hud string rendering.
- Fix boom teleport line specials to match boom behavior for non-players. Fixes Remanence MAP01 cyber platforms not lowering.
- Fix boom generalized sector damage not working.
- Fix A_JumpIfFlagsSet always evaluating to true if Args3 is set for MBF21 flags.

## Misc:
- Add compatibility for Eviternity II Annihilate Me skill level to swap incorrect usage of SpawnMulti to SpawnMultiCoopOnly.
- Implemented custom frame limiter. Reduces CPU/power usage and yields better results since the OpenTK 4.9.4 upgrade in Helion 0.9.8.0 that was causing FPS drops on certain machines.
- Removed intermediate buffer copy per frame. Increases performance with integrated GPUs at higher resolutions caused by bandwidth bottleneck.
- Removed unnecessary OpenGL clear calls. Small performance improvement with integrated GPUs.
- Added better OpenGL version testing that would previously cause hard crashes on some Linux setups.
- Added -fast parameter to toggle fast monsters in addition to sv_fastmonsters.