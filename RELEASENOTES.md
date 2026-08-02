# 1.1.0.0 (Pre-release)

## Features:
- Add Radsuit intensity.
- Automatic blood color and fuzz blood options.

## Bug Fixes:

## Misc:
- Use modern OpenGL functions for VAO attributes when supported.
- Use DrawArraysInstanced instead of geometry shader for sprite rendering (allows for MacOS support).
- Use FramebufferSize in MacOS to fix windowed mode not rendering to the entire window for MacOS.