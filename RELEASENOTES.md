# 1.0.0.0 (Pre-release)

## Features:
- Added "Detailed" HUD
- Ability to swap between multiple HUD layouts defined in SBARDEF.lmp (Options, or +/- keys)
- Added NoiseAlert, Thing_Activate, Thing_Deactivate, HealThing, Thing_Hate, Thing_Raise, Thing_Stop, Thing_Damage, Thing_Move, ThrustThingZ, Thing_ChangeTID, and Thing_SetSpecial
- Added Light_RaiseByValue and Light_LowerByValue

## Bug Fixes:
- Fix nextmap/previousmap breaking on WADs with maps that exit to the same map.
- Fix thing specials activated on death to correctly target the killer thing.

## Misc:
- Refactor of old Status Bar renderer to data-driven SBARDEF format.
- Add suicide message.
- Add average scrolling for things in multiple scroll sectors to match UZDoom for appropriate UDMF namespaces.