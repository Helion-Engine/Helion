# 0.9.5.0 (Pre-release)

## Bug fixes:
  - Fix various issues with intermission screen and end-game screen formatting, including use of widescreen assets
  - Interpolate weapon raise/lower animation
  - Prevent mouse vertical movement from causing the player to move when mouselook is enabled
  - Fix issues related to building on case-sensitive file systems (Linux)
  - Fix rendering order for two-sided middle walls that could cause flats/walls behind not to render
  - Fix boom WR lock door lines from constantly triggering key messages when contacting
  - Fixed two-sided middle scrolling to offset entire texture by the Y value when rendering to match original doom behavior
  - Fixed instant sector floors not raising when two monsters are overlapping

## Features:
  - Initial id24 specification support
  - SoundFont picker dialog
  