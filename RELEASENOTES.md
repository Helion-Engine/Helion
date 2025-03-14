# 0.9.7.0 (Pre-release)

## Features:
- Upgrade to .NET 9
- UDMF implementation
- Improvements to initial map load times
- Line contrast mode (off, vanilla, smooth)

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