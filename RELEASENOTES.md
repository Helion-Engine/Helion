# 0.9.9.2 (Pre-release)

## Features:
- Added "Detailed" HUD
- Ability to swap between multiple HUD layouts defined in SBARDEF.lmp (Options, or +/- keys)

## Bug Fixes:
- Fix issue where monsters would not move when they have velocity applied (e.g. from bullet hit or explosion damage)
- Fix berserk intensity not working when set to zero with true color overlays disabled
- Fix message color for SBARDEF
- Fix letterbox areas not clearing and pain/pickup overlays drawing over letterbox areas when using virtual resolution
- Fix vertical alignment for fullscreen CWILV## graphics in Intermissions (like in Eviternity.WAD)
- Fix ZDoom-style message centering when using SBARDEF
- Fix intermission exitpic from MAPINFO not being set on transition. Fixes Eviternity II.
- Fix software emulation discarding extra sprite pixels on the backside of upper textures.
- Fix line of sight edge cases where monsters can't see the player.

## Misc:
- Added option to disable stats showing in automap
- Use more pixelated-looking TTF when generating ENDOOM
- Refactor of old Status Bar renderer to data-driven SBARDEF format.
- Correct brightmap option description that incorrectly described it didn't function with palette video mode.