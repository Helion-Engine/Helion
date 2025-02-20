# 0.9.6.1

## Features:

## Bug fixes:
  - Fix bug where VSync and rate limiter changes were only applied after a restart
  - Fix bug that would cause a crash on map load with large amounts of voodoo dolls
  - Fix dehacked per ammo for weapon ammo give amounts
  - Fix A_FireCGun to use vanilla frame index calculation for dehacked
  - Fix A_WeaponJump and A_CheckAmmo to correctly update flash state frame when called from weapon flash