# 1.0.0.0 (Pre-release)

## Features:
- Added "Detailed" HUD
- Ability to swap between multiple HUD layouts defined in SBARDEF.lmp (Options, or +/- keys)
- Added Sector_Set3dFloor and ExtraFloor_LightOnly
- Added NoiseAlert, Thing_Activate, Thing_Deactivate, HealThing, Thing_Hate, Thing_Raise, Thing_Stop, Thing_Damage, Thing_Move, ThrustThingZ, Thing_ChangeTID, and Thing_SetSpecial
- Added Light_RaiseByValue, Light_LowerByValue, Light_Glow, Light_Flicker, and Light_Strobe, and Light_Fade
- Added Sector_SetRotation, Sector_SetCeilingPanning, Sector_SetFloorPanning, Sector_SetCeilingScale, Sector_SetFloorScale, Sector_SetDamage, and Sector_SetGravity
- Added Line_SetBlocking
- Added TeleportGroup and TeleportInSector
- Added Floor_LowerInstant, Floor_RaiseInstant, Ceiling_LowerInstant, Ceiling_RaiseInstant

## Bug Fixes:
- Fix nextmap/previousmap breaking on WADs with maps that exit to the same map.
- Fix thing specials activated on death to correctly target the killer thing.
- Fix friendly monsters not targeting enemies.
- Fix line pass through flag to not check line blocking to match boom behavior (fixes Tele-Direct MAP11 elevator).
- Fix intermission font character width setting. (fixes Tele-Direct intermission font numbers).
- Fix parsing both UMAPINFO and ZMAPINFO when present. ZMAPINFO takes priority. (fixes Crematomania MAP30 endgame).
- Fix chainsaw/punch always using zero pitch when autoaim is on and there nothing to aim at.
- Fix issue with software emulation that would cause issues with sprites rendering over lowers when a two-sided middle wall was set on the opposite side.
- Fix lite amp goggles/render.fullbright not increasing the light level when using palette color with true color overlays disabled.
- Fix sprites being generated and duplicated at runtime in true color.

## Misc:
- Refactor of old Status Bar renderer to data-driven SBARDEF format.
- Add suicide message.
- Add average scrolling for things in multiple scroll sectors to match UZDoom for appropriate UDMF namespaces.
- Log config file path to console.
- Add color to mark player special trigger lines in automap.
- Verify file order in saves and include more detail on why a save is incompatible with the loaded files.
- Update TBOs that use RGB32F to use RGBA32F for 3.3 cards that were never updated to support ARB_texture_buffer_object_rgb32.
- Use colormap index one for lite amp goggles instead of zero to match original doom behavior.