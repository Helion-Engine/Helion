# 1.1.0.0 (Pre-release)

## Features:
- Add Radsuit intensity.
- Automatic blood color and fuzz blood options.
- Initial support for MacOS ARM64 builds.

## Bug Fixes:
- Do not clear player velocity when slide movement fails. Matches vanilla doom behavior where players can move out of lines with enough momentum to pass clip checks. (Fixes Hellevator MAP06 start)
- Fix paths where checkered null texture would be used for brightmaps on sprites with null texture option.
- Fix scrolling floors/ceilings not rendering movement after loading a game in non-UDMF maps.
- Fix sounds without attenuation playing at full volume when sound volume is zero.
- Fix sounds randomly having old gain values when sound volume is changed.
- Fix repeat switch behavior when it starts with the on texture (fixes E3M1 repeat door close switch).
- Fix case where skies were excluded from dynamic rendering when sector moves.
- Fix incorrect dehacked state lookup for arachnotron RUN7 and RUN8 states. Fixes KDIKDIZD suicide bomber scream.
- Fix flats in dynamic render path that were flagged not to render.
- Fix crash when trying to copy text to the clipboard in linux.
- Fix cases where automap would mark lines that couldn't be seen.
- Fix map load crash when trying to load a map with a zero length behavior.
- Fix crash when using nextmap command and the next map has a ACS behavior module to load.
- Fix A_JumpIfFlagsSet for MBF21 flags that modify entity properties. Fixes Abyssal Apocrypha MAP08 Totem of Resurrection.
- Fix incosistensies between static/dynamic rendering paths.
- Add better checks for sprite clipping when not using software sprite emulation. (Fixes Eye Juice Arachnotrons/Medkits floating)
- Match frame ticking behavior differences between player and non-player states.
- Fix dehacked not setting randomize flag. Fixes Legacy of Rust Calamity Blade firing.
- Fix status bars showing zero for chainsaw/fist.
- Fix status bar weapon slot condition to correctly check against switched weapon instead of the weapon that's actively being switched to.
- Fix status bar uses ammo condition.

## Misc:
- Use DrawArraysInstanced instead of geometry shader for sprite rendering (allows for MacOS support).
- Use FramebufferSize in MacOS to fix windowed mode not rendering to the entire window for MacOS.
- Add compat option for MbfPlayerMovement.
- Improved rendering performance for upper/lower transfer heights views.
- Minor improvements to CPU side sprite rendering.
- Make RNG method persist in config.