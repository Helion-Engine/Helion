# 0.9.5.0 (Pre-release)

## Features:
  - Initial id24 specification support
  - SoundFont picker dialog
  - Minor usability improvement to options menus (can see and restore defaults)
  - Status bar texture picker dialog
  - Added automap marker functionality to key binding options
  - "Melt" screen wipe added

## Bug fixes:
  - Fix various issues with intermission screen and end-game screen formatting, including use of widescreen assets, text flow, alignment, pillarbox masking on scrolling finales
  - Interpolate weapon raise/lower animations and chainsaw bobbing
  - Prevent mouse vertical movement from causing the player to move when mouselook is enabled
  - Fix issues related to building on case-sensitive file systems (Linux)
  - Fix rendering order for two-sided middle walls that could cause flats/walls behind not to render
  - Fix boom WR lock door lines from constantly triggering key messages when contacting
  - Fix two-sided middle scrolling to offset entire texture by the Y value when rendering to match original doom behavior
  - Fix visual scroll offsets not being restored from a save for walls and flats
  - Fix instant sector floors not raising when two monsters are overlapping
  - Fix A_BfgSpray to be created after damage so they are rendered at corpse height if a monster is killed.
  - Fix issue with screenshot command not being processed.
  - Various fixes around key bindings saved to config file
  - Fix an issue that caused the process' working set to grow rapidly on level changes
  - Fix max ammo display on status bar in PWADs that modify this property
  - Fix rendering of taller fonts
  - Fix issue with clearing multiple automap markers
  - Fix self-referencing sectors to not block hitscan attacks and line of sight checks to match original doom behavior
  - Fix boom silent teleport specials to keep height from floor
  - Add missing PLS1EXP5 dehacked lookup (fixes decoration in Frozen Heart)
  - Map dehacked TRANSLATION bit memnomic to TRANSLATION1
  - Fix dehacked frame misc1/2 to correctly set weapon sprite offsets through the weapon frame state only
  - Fix rendering that would stop sprites from rendering behind two-sided transparent walls when not using emulate vanilla rendering
  - Fix monsters infighting too easily
  - Improved detection of WADs installed from Steam 
  - Separate HUD and menu scaling into two different options
