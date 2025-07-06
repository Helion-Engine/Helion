# 0.9.8.0 (Pre-release)

## Features:
- Update OpenTK to 4.9.4
- Dehacked blood color support for both palette and true color images.
- Blood color is passed to children through A_SpawnObject so Smooth Doom MBF21 blood spawners with blood color match parent blood.
- UDMF midtex3d and midtex3dimpassible line flags support. Includes physics for monsters to walk on without dropping off.
- HUD message text will wrap to HUD width.

## Bug Fixes:
- Fix spawn ceiling sprite offsets for vanilla sprite rendering.
- Ignore GL nodes in archive and always build nodes internally to fix maps with bad GL nodes.
- Fix id24 pickups to skip using the sprite name for lookup.
- Fix incorrect warnings for sounds and invalid bex string memonic with custom sounds prefixed with USER_.
- Fix id24 skies to have defaults set for when not defined outside of flatmapping and correct lookup for animations.
- Fix UMAPINFO/MAPINFO EnterText/ExitText/SecretExitText/ExitText with escaped double quotes.