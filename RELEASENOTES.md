# 0.9.7.0 (Pre-release)

## Features:
- Upgrade to .NET 9
- UDMF implementation
- User data folder (config, save games, etc.) now defaults to `(user)/Saved Games/Helion` on Windows. The previous "portable mode" behavior of storing user data in the Helion folder is used if a `config.ini` file exists in the Helion folder, or if `-portable` is passed as a launch parameter.
- Improvements to initial map load times
- Line contrast mode (off, vanilla, smooth)
- Calculate locked key door color by using key icon image
- Added pixel gap correction to rendering that is on by default. Prevents most pixel gap problems caused by floating point precision
- Added MAPINFO and md5 to have correct level names and progression for nerve.wad (No Rest for the Living)
- Added fix for sprites clipping through lower floors with emulate vanilla rendering (issue #907)
- Replaced HUD horizontal margin with HUD width option
- Added optional health bars to render above shootable things
- Upgraded vanilla rendering emulation to draw sprites over walls similar to vanilla

## Bug fixes:
- Correct missile blocking checks to match original behavior (fixes radsuits blocking rockets etc)
- Fix line of sight array capacity check that could cause crash
- Organize key binding menu for clarity
- Fix apostrophe key not registering
- Fix monster kill sectors (type 8192) to not kill things without shootable flag
- Fix teleport destinations not being mapped when created through A_SpawnObject
- Fix A_SpawnObject x/y offset and x/y velocity calculations
- Fix line intersection check to be inclusive (fixes Eviternity II boss activating on map start)
- Fix dehacked check for applying translucent flag (fixes Dominus Diabolicus chairs being translucent)
- Fix partial invisibility cheat when toggled off to clear shadow flag from player
- Fix issue with emulate vanilla rendering that would cause sprites to show through walls after they move
- Fix potential issue where AudioStream might play garbage data
- Fix FATT_ATK1 to map to A_FatRaise for dehacked
- Fix id24 colormap walk specials to set colormap to lower texture when crossing from the backside of the line
- Fix monster movement to account for sector friction with Boom's 223 line type
- Fix transfer heights movement issue when a sector completes movement before it's control sector completes
- Fix changing skill during gameplay to correctly set on next level load
- Fix setting skill from command line changing the skill level from a save when a new map is loaded
- Fix issue where players view can be unintentionally changed from mouse input during melt transition
- Fix UMAPINFO default mapping for secret exit text levels
- Fix Doom1 y offset patch fixes for BIGDOOR7 and SKY1 to apply to wads with custom textures
- Fix crash when setting enum config value from console as a number and it's out of range 
