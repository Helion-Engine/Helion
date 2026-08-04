# 1.1.0.0 (Pre-release)

## Features:
- Add Radsuit intensity.
- Automatic blood color and fuzz blood options.

## Bug Fixes:
- Do not clear player velocity when slide movement fails. Matches vanilla doom behavior where players can move out of lines with enough momentum to pass clip checks. (Fixes Hellevator MAP06 start)

## Misc:
- Use modern OpenGL functions for VAO attributes when supported.
- Use DrawArraysInstanced instead of geometry shader for sprite rendering (allows for MacOS support).
- Use FramebufferSize in MacOS to fix windowed mode not rendering to the entire window for MacOS.
- Add compat option for MbfPlayerMovement