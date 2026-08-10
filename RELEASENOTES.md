# 1.1.0.0 (Pre-release)

## Features:
- Add Radsuit intensity.
- Automatic blood color and fuzz blood options.

## Bug Fixes:
- Do not clear player velocity when slide movement fails. Matches vanilla doom behavior where players can move out of lines with enough momentum to pass clip checks. (Fixes Hellevator MAP06 start)
- Fix paths where checkered null texture would be used for brightmaps on sprites with null texture option.
- Fix scrolling floors/ceilings not rendering movement after loading a game in non-UDMF maps.
- Fix sounds without attenuation playing at full volume when sound volume is zero.
- Fix sounds randomly having old gain values when sound volume is changed.
- Fix repeat switch behavior when it starts with the off texture (fixes E3M1 repeat door close switch).

## Misc:
- Use DrawArraysInstanced instead of geometry shader for sprite rendering (allows for MacOS support).
- Use FramebufferSize in MacOS to fix windowed mode not rendering to the entire window for MacOS.
- Add compat option for MbfPlayerMovement