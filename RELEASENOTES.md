# 1.2.0.0 (Pre-release)

## Features:
- Pack textures into 2D arrays for faster rendering speeds, especially on older iGPUs.

## Bug Fixes:
- Fix various float checks would prevent floating enemies from floating up and/or getting stuck in ceilings when bunched together.
- Clear ambush flag in A_FaceTarget to match original behavior.

## Misc:
- General blockmap traversal improvements.
- Significant performance improvement for line of sight.