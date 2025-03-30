# 0.9.7.0 (Pre-release)

## Features:
- Upgrade to .NET 9
- UDMF implementation
- Improvements to initial map load times
- Line contrast mode (off, vanilla, smooth)
- Calculate locked key door color by using key icon image
- Added pixel gap correction to redering that is on by default. Prevents most pixel gap problems caused by floating point precision.

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