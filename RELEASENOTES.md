# 0.9.8.0 (Pre-release)

## Features:
- Simplify bit packing strategy on vertex data using floatBitsToInt. Decreases entity sprite vertex size and increase range on line ids for vanilla sprite clipping emulation from 65k to over 8 million and sector light index from ~500k to over 1 million.

## Bug Fixes:
- Fix sprite frames to correctly calculate when lowercase.
- Fix dehacked frames loading incorrectly when a wad contains a dehacked patch and a dehacked patch is applied with the -deh parameter.
- Fix NoBlockmap not working with scrolling floors and correctly ignore things with NoSector.