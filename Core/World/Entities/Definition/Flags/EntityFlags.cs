using Helion.Models;
using System.Runtime.CompilerServices;

namespace Helion.World.Entities.Definition.Flags;

public struct EntityFlags
{
    public const int SpecialFlag = FlagValue.Flag1;
    public const int SolidFlag = FlagValue.Flag2;
    public const int ShootableFlag = FlagValue.Flag3;
    public const int DropOffFlag = FlagValue.Flag8;
    public const int TouchyFlag = FlagValue.Flag24;
    public const int FloatFlag = FlagValue.Flag12;
    public const int MissileFlag = FlagValue.Flag14;
    public const int SkullFlyFlag = FlagValue.Flag22;
    public const int ActsLikeBridgeFlag = FlagValue.Flag24;

    public const int Translation1Flag = FlagValue.Flag12;
    public const int Translation2Flag = FlagValue.Flag13;
    public const int TranslationFlag = Translation1Flag | Translation2Flag;

    public int Flags1;
    public int Flags2;
    public int Flags3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Special() => (Flags1 & FlagValue.Flag1) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpecial() => Flags1 |= FlagValue.Flag1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpecial(bool value) { if (value) SetSpecial(); else ClearSpecial(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearSpecial() => Flags1 &= FlagValue.InvFlag1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Solid() => (Flags1 & FlagValue.Flag2) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSolid(bool value) { if (value) SetSolid(); else ClearSolid(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSolid() => Flags1 |= FlagValue.Flag2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearSolid() => Flags1 &= FlagValue.InvFlag2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Shootable() => (Flags1 & FlagValue.Flag3) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetShootable() => Flags1 |= FlagValue.Flag3;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetShootable(bool value) { if (value) SetShootable(); else ClearShootable(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearShootable() => Flags1 &= FlagValue.InvFlag3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool NoSector() => (Flags1 & FlagValue.Flag4) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoSector() => Flags1 |= FlagValue.Flag4;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoSector(bool value) { if (value) SetNoSector(); else ClearNoSector(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoSector() => Flags1 &= FlagValue.InvFlag4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool NoBlockmap() => (Flags1 & FlagValue.Flag5) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoBlockmap() => Flags1 |= FlagValue.Flag5;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoBlockmap(bool value) { if (value) SetNoBlockmap(); else ClearNoBlockmap(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoBlockmap() => Flags1 &= FlagValue.InvFlag5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Ambush() => (Flags1 & FlagValue.Flag6) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetAmbush() => Flags1 |= FlagValue.Flag6;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetAmbush(bool value) { if (value) SetAmbush(); else ClearAmbush(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearAmbush() => Flags1 &= FlagValue.InvFlag6;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool NoGravity() => (Flags1 & FlagValue.Flag7) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoGravity() => Flags1 |= FlagValue.Flag7;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoGravity(bool value) { if (value) SetNoGravity(); else ClearNoGravity(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoGravity() => Flags1 &= FlagValue.InvFlag7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Dropoff() => (Flags1 & FlagValue.Flag8) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDropoff() => Flags1 |= FlagValue.Flag8;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDropoff(bool value) { if (value) SetDropoff(); else ClearDropoff(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearDropoff() => Flags1 &= FlagValue.InvFlag8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Pickup() => (Flags1 & FlagValue.Flag9) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPickup() => Flags1 |= FlagValue.Flag9;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPickup(bool value) { if (value) SetPickup(); else ClearPickup(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearPickup() => Flags1 &= FlagValue.InvFlag9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool NoClip() => (Flags1 & FlagValue.Flag10) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoClip() => Flags1 |= FlagValue.Flag10;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoClip(bool value) { if (value) SetNoClip(); else ClearNoClip(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoClip() => Flags1 &= FlagValue.InvFlag10;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool SlidesOnWalls() => (Flags1 & FlagValue.Flag11) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSlidesOnWalls() => Flags1 |= FlagValue.Flag11;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSlidesOnWalls(bool value) { if (value) SetSlidesOnWalls(); else ClearSlidesOnWalls(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearSlidesOnWalls() => Flags1 &= FlagValue.InvFlag11;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Float() => (Flags1 & FlagValue.Flag12) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFloat() => Flags1 |= FlagValue.Flag12;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFloat(bool value) { if (value) SetFloat(); else ClearFloat(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearFloat() => Flags1 &= FlagValue.InvFlag12;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Teleport() => (Flags1 & FlagValue.Flag13) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTeleport() => Flags1 |= FlagValue.Flag13;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTeleport(bool value) { if (value) SetTeleport(); else ClearTeleport(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearTeleport() => Flags1 &= FlagValue.InvFlag13;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Missile() => (Flags1 & FlagValue.Flag14) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMissile() => Flags1 |= FlagValue.Flag14;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMissile(bool value) { if (value) SetMissile(); else ClearMissile(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearMissile() => Flags1 &= FlagValue.InvFlag14;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void FlipMissile() => Flags1 ^= FlagValue.Flag14;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Dropped() => (Flags1 & FlagValue.Flag15) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDropped() => Flags1 |= FlagValue.Flag15;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDropped(bool value) { if (value) SetDropped(); else ClearDropped(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearDropped() => Flags1 &= FlagValue.InvFlag15;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Shadow() => (Flags1 & FlagValue.Flag16) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetShadow() => Flags1 |= FlagValue.Flag16;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetShadow(bool value) { if (value) SetShadow(); else ClearShadow(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearShadow() => Flags1 &= FlagValue.InvFlag16;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool NoBlood() => (Flags1 & FlagValue.Flag17) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoBlood() => Flags1 |= FlagValue.Flag17;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoBlood(bool value) { if (value) SetNoBlood(); else ClearNoBlood(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoBlood() => Flags1 &= FlagValue.InvFlag17;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Corpse() => (Flags1 & FlagValue.Flag19) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCorpse() => Flags1 |= FlagValue.Flag19;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCorpse(bool value) { if (value) SetCorpse(); else ClearCorpse(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearCorpse() => Flags1 &= FlagValue.InvFlag19;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool CountItem() => (Flags1 & FlagValue.Flag20) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCountItem() => Flags1 |= FlagValue.Flag20;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCountItem(bool value) { if (value) SetCountItem(); else ClearCountItem(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearCountItem() => Flags1 &= FlagValue.InvFlag20;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool CountKill() => (Flags1 & FlagValue.Flag21) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCountKill() => Flags1 |= FlagValue.Flag21;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCountKill(bool value) { if (value) SetCountKill(); else ClearCountKill(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearCountKill() => Flags1 &= FlagValue.InvFlag21;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Skullfly() => (Flags1 & FlagValue.Flag22) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSkullfly() => Flags1 |= FlagValue.Flag22;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSkullfly(bool value) { if (value) SetSkullfly(); else ClearSkullfly(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearSkullfly() => Flags1 &= FlagValue.InvFlag22;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool NotDMatch() => (Flags1 & FlagValue.Flag23) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNotDMatch() => Flags1 |= FlagValue.Flag23;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNotDMatch(bool value) { if (value) SetNotDMatch(); else ClearNotDMatch(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNotDMatch() => Flags1 &= FlagValue.InvFlag23;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool ActLikeBridge() => (Flags1 & FlagValue.Flag24) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetActLikeBridge() => Flags1 |= FlagValue.Flag24;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetActLikeBridge(bool value) { if (value) SetActLikeBridge(); else ClearActLikeBridge(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearActLikeBridge() => Flags1 &= FlagValue.InvFlag24;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Boss() => (Flags1 & FlagValue.Flag25) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetBoss() => Flags1 |= FlagValue.Flag25;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetBoss(bool value) { if (value) SetBoss(); else ClearBoss(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearBoss() => Flags1 &= FlagValue.InvFlag25;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool SpawnCeiling() => (Flags1 & FlagValue.Flag26) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpawnCeiling() => Flags1 |= FlagValue.Flag26;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpawnCeiling(bool value) { if (value) SetSpawnCeiling(); else ClearSpawnCeiling(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearSpawnCeiling() => Flags1 &= FlagValue.InvFlag26;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool CanPass() => (Flags1 & FlagValue.Flag27) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCanPass() => Flags1 |= FlagValue.Flag27;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCanPass(bool value) { if (value) SetCanPass(); else ClearCanPass(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearCanPass() => Flags1 &= FlagValue.InvFlag27;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool DontGib() => (Flags1 & FlagValue.Flag28) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDontGib() => Flags1 |= FlagValue.Flag28;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDontGib(bool value) { if (value) SetDontGib(); else ClearDontGib(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearDontGib() => Flags1 &= FlagValue.InvFlag28;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool JustHit() => (Flags1 & FlagValue.Flag29) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetJustHit() => Flags1 |= FlagValue.Flag29;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetJustHit(bool value) { if (value) SetJustHit(); else ClearJustHit(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearJustHit() => Flags1 &= FlagValue.InvFlag29;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool QuickToRetaliate() => (Flags1 & FlagValue.Flag30) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetQuickToRetaliate() => Flags1 |= FlagValue.Flag30;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetQuickToRetaliate(bool value) { if (value) SetQuickToRetaliate(); else ClearQuickToRetaliate(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearQuickToRetaliate() => Flags1 &= FlagValue.InvFlag30;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Randomize() => (Flags1 & FlagValue.Flag31) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetRandomize() => Flags1 |= FlagValue.Flag31;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetRandomize(bool value) { if (value) SetRandomize(); else ClearRandomize(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearRandomize() => Flags1 &= FlagValue.InvFlag31;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool NoTarget() => (Flags1 & FlagValue.Flag32) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoTarget() => Flags1 |= FlagValue.Flag32;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoTarget(bool value) { if (value) SetNoTarget(); else ClearNoTarget(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoTarget() => Flags1 &= FlagValue.InvFlag32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool BossSpawnShot() => (Flags2 & FlagValue.Flag1) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetBossSpawnShot() => Flags2 |= FlagValue.Flag1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetBossSpawnShot(bool value) { if (value) SetBossSpawnShot(); else ClearBossSpawnShot(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearBossSpawnShot() => Flags2 &= FlagValue.InvFlag1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Map07Boss1() => (Flags2 & FlagValue.Flag2) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMap07Boss1() => Flags2 |= FlagValue.Flag2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMap07Boss1(bool value) { if (value) SetMap07Boss1(); else ClearMap07Boss1(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearMap07Boss1() => Flags2 &= FlagValue.InvFlag2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Map07Boss2() => (Flags2 & FlagValue.Flag3) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMap07Boss2() => Flags2 |= FlagValue.Flag3;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMap07Boss2(bool value) { if (value) SetMap07Boss2(); else ClearMap07Boss2(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearMap07Boss2() => Flags2 &= FlagValue.InvFlag3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool E1M8Boss() => (Flags2 & FlagValue.Flag4) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE1M8Boss() => Flags2 |= FlagValue.Flag4;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE1M8Boss(bool value) { if (value) SetE1M8Boss(); else ClearE1M8Boss(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearE1M8Boss() => Flags2 &= FlagValue.InvFlag4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool E2M8Boss() => (Flags2 & FlagValue.Flag5) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE2M8Boss() => Flags2 |= FlagValue.Flag5;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE2M8Boss(bool value) { if (value) SetE2M8Boss(); else ClearE2M8Boss(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearE2M8Boss() => Flags2 &= FlagValue.InvFlag5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool E3M8Boss() => (Flags2 & FlagValue.Flag6) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE3M8Boss() => Flags2 |= FlagValue.Flag6;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE3M8Boss(bool value) { if (value) SetE3M8Boss(); else ClearE3M8Boss(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearE3M8Boss() => Flags2 &= FlagValue.InvFlag6;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool E4M6Boss() => (Flags2 & FlagValue.Flag7) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE4M6Boss() => Flags2 |= FlagValue.Flag7;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE4M6Boss(bool value) { if (value) SetE4M6Boss(); else ClearE4M6Boss(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearE4M6Boss() => Flags2 &= FlagValue.InvFlag7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool E4M8Boss() => (Flags2 & FlagValue.Flag8) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE4M8Boss() => Flags2 |= FlagValue.Flag8;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetE4M8Boss(bool value) { if (value) SetE4M8Boss(); else ClearE4M8Boss(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearE4M8Boss() => Flags2 &= FlagValue.InvFlag8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool FullVolSee() => (Flags2 & FlagValue.Flag9) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFullVolSee() => Flags2 |= FlagValue.Flag9;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFullVolSee(bool value) { if (value) SetFullVolSee(); else ClearFullVolSee(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearFullVolSee() => Flags2 &= FlagValue.InvFlag9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool FullVolDeath() => (Flags2 & FlagValue.Flag10) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFullVolDeath() => Flags2 |= FlagValue.Flag10;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFullVolDeath(bool value) { if (value) SetFullVolDeath(); else ClearFullVolDeath(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearFullVolDeath() => Flags2 &= FlagValue.InvFlag10;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool DoHarmSpecies() => (Flags2 & FlagValue.Flag11) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDoHarmSpecies() => Flags2 |= FlagValue.Flag11;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDoHarmSpecies(bool value) { if (value) SetDoHarmSpecies(); else ClearDoHarmSpecies(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearDoHarmSpecies() => Flags2 &= FlagValue.InvFlag11;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Invulnerable() => (Flags2 & FlagValue.Flag12) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetInvulnerable() => Flags2 |= FlagValue.Flag12;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetInvulnerable(bool value) { if (value) SetInvulnerable(); else ClearInvulnerable(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearInvulnerable() => Flags2 &= FlagValue.InvFlag12;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsMonster() => (Flags2 & FlagValue.Flag13) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetIsMonster() => Flags2 |= FlagValue.Flag13;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetIsMonster(bool value) { if (value) SetIsMonster(); else ClearIsMonster(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearIsMonster() => Flags2 &= FlagValue.InvFlag13;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Friendly() => (Flags2 & FlagValue.Flag14) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFriendly() => Flags2 |= FlagValue.Flag14;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFriendly(bool value) { if (value) SetFriendly(); else ClearFriendly(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearFriendly() => Flags2 &= FlagValue.InvFlag14;

    public bool StepMissile { readonly get => (Flags2 & FlagValue.Flag15) != 0; set { if (value) Flags2 |= FlagValue.Flag15; else Flags2 &= FlagValue.InvFlag15; } }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool NoFriction() => (Flags2 & FlagValue.Flag16) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoFriction() => Flags2 |= FlagValue.Flag16;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoFriction(bool value) { if (value) SetNoFriction(); else ClearNoFriction(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoFriction() => Flags2 &= FlagValue.InvFlag16;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool DontFall() => (Flags2 & FlagValue.Flag17) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDontFall() => Flags2 |= FlagValue.Flag17;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDontFall(bool value) { if (value) SetDontFall(); else ClearDontFall(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearDontFall() => Flags2 &= FlagValue.InvFlag17;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool WeaponWimpyWeapon() => (Flags2 & FlagValue.Flag18) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponWimpyWeapon() => Flags2 |= FlagValue.Flag18;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponWimpyWeapon(bool value) { if (value) SetWeaponWimpyWeapon(); else ClearWeaponWimpyWeapon(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearWeaponWimpyWeapon() => Flags2 &= FlagValue.InvFlag18;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool WeaponNoAutoSwitch() => (Flags2 & FlagValue.Flag19) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponNoAutoSwitch() => Flags2 |= FlagValue.Flag19;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponNoAutoSwitch(bool value) { if (value) SetWeaponNoAutoSwitch(); else ClearWeaponNoAutoSwitch(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearWeaponNoAutoSwitch() => Flags2 &= FlagValue.InvFlag19;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool InventoryAlwaysPickup() => (Flags2 & FlagValue.Flag20) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetInventoryAlwaysPickup() => Flags2 |= FlagValue.Flag20;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetInventoryAlwaysPickup(bool value) { if (value) SetInventoryAlwaysPickup(); else ClearInventoryAlwaysPickup(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearInventoryAlwaysPickup() => Flags2 &= FlagValue.InvFlag20;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool WeaponNoAlert() => (Flags2 & FlagValue.Flag21) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponNoAlert() => Flags2 |= FlagValue.Flag21;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponNoAlert(bool value) { if (value) SetWeaponNoAlert(); else ClearWeaponNoAlert(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearWeaponNoAlert() => Flags2 &= FlagValue.InvFlag21;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool WeaponMeleeWeapon() => (Flags2 & FlagValue.Flag22) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponMeleeWeapon() => Flags2 |= FlagValue.Flag22;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponMeleeWeapon(bool value) { if (value) SetWeaponMeleeWeapon(); else ClearWeaponMeleeWeapon(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearWeaponMeleeWeapon() => Flags2 &= FlagValue.InvFlag22;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool WindThrust() => (Flags2 & FlagValue.Flag23) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWindThrust() => Flags2 |= FlagValue.Flag23;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWindThrust(bool value) { if (value) SetWindThrust(); else ClearWindThrust(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearWindThrust() => Flags2 &= FlagValue.InvFlag23;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Touchy() => (Flags2 & FlagValue.Flag24) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTouchy() => Flags2 |= FlagValue.Flag24;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTouchy(bool value) { if (value) SetTouchy(); else ClearTouchy(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearTouchy() => Flags2 &= FlagValue.InvFlag24;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool ForceRadiusDmg() => (Flags2 & FlagValue.Flag25) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetForceRadiusDmg() => Flags2 |= FlagValue.Flag25;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetForceRadiusDmg(bool value) { if (value) SetForceRadiusDmg(); else ClearForceRadiusDmg(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearForceRadiusDmg() => Flags2 &= FlagValue.InvFlag25;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool NoRadiusDmg() => (Flags2 & FlagValue.Flag26) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoRadiusDmg() => Flags2 |= FlagValue.Flag26;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoRadiusDmg(bool value) { if (value) SetNoRadiusDmg(); else ClearNoRadiusDmg(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoRadiusDmg() => Flags2 &= FlagValue.InvFlag26;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool NoVerticalMeleeRange() => (Flags2 & FlagValue.Flag27) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoVerticalMeleeRange() => Flags2 |= FlagValue.Flag27;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoVerticalMeleeRange(bool value) { if (value) SetNoVerticalMeleeRange(); else ClearNoVerticalMeleeRange(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoVerticalMeleeRange() => Flags2 &= FlagValue.InvFlag27;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool MissileMore() => (Flags2 & FlagValue.Flag28) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMissileMore() => Flags2 |= FlagValue.Flag28;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMissileMore(bool value) { if (value) SetMissileMore(); else ClearMissileMore(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearMissileMore() => Flags2 &= FlagValue.InvFlag28;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool MissileEvenMore() => (Flags2 & FlagValue.Flag29) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMissileEvenMore() => Flags2 |= FlagValue.Flag29;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMissileEvenMore(bool value) { if (value) SetMissileEvenMore(); else ClearMissileEvenMore(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearMissileEvenMore() => Flags2 &= FlagValue.InvFlag29;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool OldRadiusDmg() => (Flags2 & FlagValue.Flag30) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetOldRadiusDmg() => Flags2 |= FlagValue.Flag30;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetOldRadiusDmg(bool value) { if (value) SetOldRadiusDmg(); else ClearOldRadiusDmg(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearOldRadiusDmg() => Flags2 &= FlagValue.InvFlag30;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool MbfBouncer() => (Flags2 & FlagValue.Flag31) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMbfBouncer() => Flags2 |= FlagValue.Flag31;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMbfBouncer(bool value) { if (value) SetMbfBouncer(); else ClearMbfBouncer(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearMbfBouncer() => Flags2 &= FlagValue.InvFlag31;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Ripper() => (Flags2 & FlagValue.Flag32) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetRipper() => Flags2 |= FlagValue.Flag32;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetRipper(bool value) { if (value) SetRipper(); else ClearRipper(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearRipper() => Flags2 &= FlagValue.InvFlag32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool NoTeleport() => (Flags3 & FlagValue.Flag1) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoTeleport() => Flags3 |= FlagValue.Flag1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoTeleport(bool value) { if (value) SetNoTeleport(); else ClearNoTeleport(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoTeleport() => Flags3 &= FlagValue.InvFlag1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Invisible() => (Flags3 & FlagValue.Flag2) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetInvisible() => Flags3 |= FlagValue.Flag2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetInvisible(bool value) { if (value) SetInvisible(); else ClearInvisible(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearInvisible() => Flags3 &= FlagValue.InvFlag2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool JustAttacked() => (Flags3 & FlagValue.Flag3) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetJustAttacked() => Flags3 |= FlagValue.Flag3;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetJustAttacked(bool value) { if (value) SetJustAttacked(); else ClearJustAttacked(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearJustAttacked() => Flags3 &= FlagValue.InvFlag3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Bright() => (Flags3 & FlagValue.Flag4) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetBright() => Flags3 |= FlagValue.Flag4;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetBright(bool value) { if (value) SetBright(); else ClearBright(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearBright() => Flags3 &= FlagValue.InvFlag4;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsTeleportSpot() => (Flags3 & FlagValue.Flag5) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetIsTeleportSpot() => Flags3 |= FlagValue.Flag5;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetIsTeleportSpot(bool value) { if (value) SetIsTeleportSpot(); else ClearIsTeleportSpot(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearIsTeleportSpot() => Flags3 &= FlagValue.InvFlag5;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool WeaponNoAutofire() => (Flags3 & FlagValue.Flag6) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponNoAutofire() => Flags3 |= FlagValue.Flag6;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetWeaponNoAutofire(bool value) { if (value) SetWeaponNoAutofire(); else ClearWeaponNoAutofire(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearWeaponNoAutofire() => Flags3 &= FlagValue.InvFlag6;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool IgnoreDropOff() => (Flags3 & FlagValue.Flag7) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetIgnoreDropOff() => Flags3 |= FlagValue.Flag7;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetIgnoreDropOff(bool value) { if (value) SetIgnoreDropOff(); else ClearIgnoreDropOff(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearIgnoreDropOff() => Flags3 &= FlagValue.InvFlag7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool MonsterMove() => (Flags3 & FlagValue.Flag8) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMonsterMove() => Flags3 |= FlagValue.Flag8;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMonsterMove(bool value) { if (value) SetMonsterMove(); else ClearMonsterMove(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearMonsterMove() => Flags3 &= FlagValue.InvFlag8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Fly() => (Flags3 & FlagValue.Flag9) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFly() => Flags3 |= FlagValue.Flag9;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFly(bool value) { if (value) SetFly(); else ClearFly(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearFly() => Flags3 &= FlagValue.InvFlag9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Teleported() => (Flags3 & FlagValue.Flag10) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTeleported() => Flags3 |= FlagValue.Flag10;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTeleported(bool value) { if (value) SetTeleported(); else ClearTeleported(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearTeleported() => Flags3 &= FlagValue.InvFlag10;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool CrushGiblets() => (Flags3 & FlagValue.Flag11) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCrushGiblets() => Flags3 |= FlagValue.Flag11;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCrushGiblets(bool value) { if (value) SetCrushGiblets(); else ClearCrushGiblets(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearCrushGiblets() => Flags3 &= FlagValue.InvFlag11;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Translation1() => (Flags3 & FlagValue.Flag12) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTranslation1() => Flags3 |= FlagValue.Flag12;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTranslation1(bool value) { if (value) SetTranslation1(); else ClearTranslation1(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearTranslation1() => Flags3 &= FlagValue.InvFlag12;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Translation2() => (Flags3 & FlagValue.Flag13) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTranslation2() => Flags3 |= FlagValue.Flag13;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTranslation2(bool value) { if (value) SetTranslation2(); else ClearTranslation2(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearTranslation2() => Flags3 &= FlagValue.InvFlag13;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool NoRespawn() => (Flags3 & FlagValue.Flag14) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoRespawn() => Flags3 |= FlagValue.Flag14;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNoRespawn(bool value) { if (value) SetNoRespawn(); else ClearNoRespawn(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearNoRespawn() => Flags3 &= FlagValue.InvFlag14;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool SpecialStaySingle() => (Flags3 & FlagValue.Flag15) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpecialStaySingle() => Flags3 |= FlagValue.Flag15;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpecialStaySingle(bool value) { if (value) SetSpecialStaySingle(); else ClearSpecialStaySingle(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearSpecialStaySingle() => Flags3 &= FlagValue.InvFlag15;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool SpecialStayCooperative() => (Flags3 & FlagValue.Flag16) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpecialStayCooperative() => Flags3 |= FlagValue.Flag16;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpecialStayCooperative(bool value) { if (value) SetSpecialStayCooperative(); else ClearSpecialStayCooperative(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearSpecialStayCooperative() => Flags3 &= FlagValue.InvFlag16;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool SpecialStayDeathmatch() => (Flags3 & FlagValue.Flag17) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpecialStayDeathmatch() => Flags3 |= FlagValue.Flag17;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSpecialStayDeathmatch(bool value) { if (value) SetSpecialStayDeathmatch(); else ClearSpecialStayDeathmatch(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearSpecialStayDeathmatch() => Flags3 &= FlagValue.InvFlag17;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Slide() => (Flags3 & FlagValue.Flag18) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSlide() => Flags3 |= FlagValue.Flag18;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetSlide(bool value) { if (value) SetSlide(); else ClearSlide(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearSlide() => Flags3 &= FlagValue.InvFlag18;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool InFloat() => (Flags3 & FlagValue.Flag19) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetInFloat() => Flags3 |= FlagValue.Flag19;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetInFloat(bool value) { if (value) SetInFloat(); else ClearInFloat(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearInFloat() => Flags3 &= FlagValue.InvFlag19;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Dormant() => (Flags3 & FlagValue.Flag20) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDormant() => Flags3 |= FlagValue.Flag20;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDormant(bool value) { if (value) SetDormant(); else ClearDormant(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearDormant() => Flags3 &= FlagValue.InvFlag20;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool CountSecret() => (Flags3 & FlagValue.Flag21) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCountSecret() => Flags3 |= FlagValue.Flag21;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetCountSecret(bool value) { if (value) SetCountSecret(); else ClearCountSecret(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearCountSecret() => Flags3 &= FlagValue.InvFlag21;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Attacking() => (Flags3 & FlagValue.Flag22) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetAttacking() => Flags3 |= FlagValue.Flag22;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetAttacking(bool value) { if (value) SetAttacking(); else ClearAttacking(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearAttacking() => Flags3 &= FlagValue.InvFlag22;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Stealth() => (Flags3 & FlagValue.Flag23) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetStealth() => Flags3 |= FlagValue.Flag23;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetStealth(bool value) { if (value) SetStealth(); else ClearStealth(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearStealth() => Flags3 &= FlagValue.InvFlag23;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool Mirror() => (Flags3 & FlagValue.Flag24) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMirror() => Flags3 |= FlagValue.Flag24;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetMirror(bool value) { if (value) SetMirror(); else ClearMirror(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearMirror() => Flags3 &= FlagValue.InvFlag24;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void FlipMirror() => Flags3 ^= FlagValue.Flag24;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public readonly bool DontMirrorCorpse() => (Flags3 & FlagValue.Flag25) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDontMirrorCorpse() => Flags3 |= FlagValue.Flag25;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetDontMirrorCorpse(bool value) { if (value) SetDontMirrorCorpse(); else ClearDontMirrorCorpse(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void ClearDontMirrorCorpse() => Flags3 &= FlagValue.InvFlag25;


    public EntityFlags(EntityFlagsModel model)
    {
        if (model.Bits != null)
        {
            Flags1 = model.Bits[0];
            Flags2 = model.Bits[1];
            Flags3 = model.Bits[2];
            return;
        }

        Flags1 = model.Flags1;
        Flags2 = model.Flags2;
        Flags3 = model.Flags3;
    }

    public EntityFlagsModel ToEntityFlagsModel()
    {
        return new()
        {
            Flags1 = Flags1,
            Flags2 = Flags2,
            Flags3 = Flags3,
        };
    }

    public void ClearAll()
    {
        Flags1 = 0;
        Flags2 = 0;
        Flags3 = 0;
    }

    public override bool Equals(object? obj)
    {
        if (obj is EntityFlags entityFlags)
        {
            if (Flags1 != entityFlags.Flags1)
                return false;
            if (Flags2 != entityFlags.Flags2)
                return false;
            if (Flags3 != entityFlags.Flags3)
                return false;
           
            return true;
        }

        return false;
    }

    public override int GetHashCode() => Flags1 ^ Flags2 ^ Flags3;
}
