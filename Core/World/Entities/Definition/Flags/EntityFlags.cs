using Helion.Models;
using System.Runtime.CompilerServices;

namespace Helion.World.Entities.Definition.Flags;

public struct EntityFlags
{
    public const int SpecialFlag = FlagValue.Flag1;
    public const int SolidFlag = FlagValue.Flag2;
    public const int ShootableFlag = FlagValue.Flag3;
    public const int TouchyFlag = FlagValue.Flag24;

    public const int Translation1Flag = FlagValue.Flag12;
    public const int Translation2Flag = FlagValue.Flag13;
    public const int TranslationFlag = Translation1Flag | Translation2Flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetTranslationColorMap()
    {
        return (Flags3 & TranslationFlag) >> 11;
    }

    public int Flags1;
    public int Flags2;
    public int Flags3;

    public bool Special { get => (Flags1 & FlagValue.Flag1) != 0; set { if (value) Flags1 |= FlagValue.Flag1; else Flags1 &= FlagValue.InvFlag1; } }
    public bool Solid { get => (Flags1 & FlagValue.Flag2) != 0; set { if (value) Flags1 |= FlagValue.Flag2; else Flags1 &= FlagValue.InvFlag2; } }
    public bool Shootable { get => (Flags1 & FlagValue.Flag3) != 0; set { if (value) Flags1 |= FlagValue.Flag3; else Flags1 &= FlagValue.InvFlag3; } }
    public bool NoSector { get => (Flags1 & FlagValue.Flag4) != 0; set { if (value) Flags1 |= FlagValue.Flag4; else Flags1 &= FlagValue.InvFlag4; } }
    public bool NoBlockmap { get => (Flags1 & FlagValue.Flag5) != 0; set { if (value) Flags1 |= FlagValue.Flag5; else Flags1 &= FlagValue.InvFlag5; } }
    public bool Ambush { get => (Flags1 & FlagValue.Flag6) != 0; set { if (value) Flags1 |= FlagValue.Flag6; else Flags1 &= FlagValue.InvFlag6; } }
    public bool NoGravity { get => (Flags1 & FlagValue.Flag7) != 0; set { if (value) Flags1 |= FlagValue.Flag7; else Flags1 &= FlagValue.InvFlag7; } }
    public bool Dropoff { get => (Flags1 & FlagValue.Flag8) != 0; set { if (value) Flags1 |= FlagValue.Flag8; else Flags1 &= FlagValue.InvFlag8; } }
    public bool Pickup { get => (Flags1 & FlagValue.Flag9) != 0; set { if (value) Flags1 |= FlagValue.Flag9; else Flags1 &= FlagValue.InvFlag9; } }
    public bool NoClip { get => (Flags1 & FlagValue.Flag10) != 0; set { if (value) Flags1 |= FlagValue.Flag10; else Flags1 &= FlagValue.InvFlag10; } }
    public bool SlidesOnWalls { get => (Flags1 & FlagValue.Flag11) != 0; set { if (value) Flags1 |= FlagValue.Flag11; else Flags1 &= FlagValue.InvFlag11; } }
    public bool Float { get => (Flags1 & FlagValue.Flag12) != 0; set { if (value) Flags1 |= FlagValue.Flag12; else Flags1 &= FlagValue.InvFlag12; } }
    public bool Teleport { get => (Flags1 & FlagValue.Flag13) != 0; set { if (value) Flags1 |= FlagValue.Flag13; else Flags1 &= FlagValue.InvFlag13; } }
    public bool Missile { get => (Flags1 & FlagValue.Flag14) != 0; set { if (value) Flags1 |= FlagValue.Flag14; else Flags1 &= FlagValue.InvFlag14; } }
    public bool Dropped { get => (Flags1 & FlagValue.Flag15) != 0; set { if (value) Flags1 |= FlagValue.Flag15; else Flags1 &= FlagValue.InvFlag15; } }
    public bool Shadow { get => (Flags1 & FlagValue.Flag16) != 0; set { if (value) Flags1 |= FlagValue.Flag16; else Flags1 &= FlagValue.InvFlag16; } }
    public bool NoBlood { get => (Flags1 & FlagValue.Flag17) != 0; set { if (value) Flags1 |= FlagValue.Flag17; else Flags1 &= FlagValue.InvFlag17; } }
    public bool Corpse { get => (Flags1 & FlagValue.Flag19) != 0; set { if (value) Flags1 |= FlagValue.Flag19; else Flags1 &= FlagValue.InvFlag19; } }
    public bool CountItem { get => (Flags1 & FlagValue.Flag20) != 0; set { if (value) Flags1 |= FlagValue.Flag20; else Flags1 &= FlagValue.InvFlag20; } }
    public bool CountKill { get => (Flags1 & FlagValue.Flag21) != 0; set { if (value) Flags1 |= FlagValue.Flag21; else Flags1 &= FlagValue.InvFlag21; } }
    public bool Skullfly { get => (Flags1 & FlagValue.Flag22) != 0; set { if (value) Flags1 |= FlagValue.Flag22; else Flags1 &= FlagValue.InvFlag22; } }
    public bool NotDMatch { get => (Flags1 & FlagValue.Flag23) != 0; set { if (value) Flags1 |= FlagValue.Flag23; else Flags1 &= FlagValue.InvFlag23; } }
    public bool ActLikeBridge { get => (Flags1 & FlagValue.Flag24) != 0; set { if (value) Flags1 |= FlagValue.Flag24; else Flags1 &= FlagValue.InvFlag24; } }
    public bool Boss { get => (Flags1 & FlagValue.Flag25) != 0; set { if (value) Flags1 |= FlagValue.Flag25; else Flags1 &= FlagValue.InvFlag25; } }
    public bool SpawnCeiling { get => (Flags1 & FlagValue.Flag26) != 0; set { if (value) Flags1 |= FlagValue.Flag26; else Flags1 &= FlagValue.InvFlag26; } }
    public bool CanPass { get => (Flags1 & FlagValue.Flag27) != 0; set { if (value) Flags1 |= FlagValue.Flag27; else Flags1 &= FlagValue.InvFlag27; } }
    public bool DontGib { get => (Flags1 & FlagValue.Flag28) != 0; set { if (value) Flags1 |= FlagValue.Flag28; else Flags1 &= FlagValue.InvFlag28; } }
    public bool JustHit { get => (Flags1 & FlagValue.Flag29) != 0; set { if (value) Flags1 |= FlagValue.Flag29; else Flags1 &= FlagValue.InvFlag29; } }
    public bool QuickToRetaliate { get => (Flags1 & FlagValue.Flag30) != 0; set { if (value) Flags1 |= FlagValue.Flag30; else Flags1 &= FlagValue.InvFlag30; } }
    public bool Randomize { get => (Flags1 & FlagValue.Flag31) != 0; set { if (value) Flags1 |= FlagValue.Flag31; else Flags1 &= FlagValue.InvFlag31; } }
    public bool NoTarget { get => (Flags1 & FlagValue.Flag32) != 0; set { if (value) Flags1 |= FlagValue.Flag32; else Flags1 &= FlagValue.InvFlag32; } }
    public bool BossSpawnShot { get => (Flags2 & FlagValue.Flag1) != 0; set { if (value) Flags2 |= FlagValue.Flag1; else Flags2 &= FlagValue.InvFlag1; } }
    public bool Map07Boss1 { get => (Flags2 & FlagValue.Flag2) != 0; set { if (value) Flags2 |= FlagValue.Flag2; else Flags2 &= FlagValue.InvFlag2; } }
    public bool Map07Boss2 { get => (Flags2 & FlagValue.Flag3) != 0; set { if (value) Flags2 |= FlagValue.Flag3; else Flags2 &= FlagValue.InvFlag3; } }
    public bool E1M8Boss { get => (Flags2 & FlagValue.Flag4) != 0; set { if (value) Flags2 |= FlagValue.Flag4; else Flags2 &= FlagValue.InvFlag4; } }
    public bool E2M8Boss { get => (Flags2 & FlagValue.Flag5) != 0; set { if (value) Flags2 |= FlagValue.Flag5; else Flags2 &= FlagValue.InvFlag5; } }
    public bool E3M8Boss { get => (Flags2 & FlagValue.Flag6) != 0; set { if (value) Flags2 |= FlagValue.Flag6; else Flags2 &= FlagValue.InvFlag6; } }
    public bool E4M6Boss { get => (Flags2 & FlagValue.Flag7) != 0; set { if (value) Flags2 |= FlagValue.Flag7; else Flags2 &= FlagValue.InvFlag7; } }
    public bool E4M8Boss { get => (Flags2 & FlagValue.Flag8) != 0; set { if (value) Flags2 |= FlagValue.Flag8; else Flags2 &= FlagValue.InvFlag8; } }
    public bool FullVolSee { get => (Flags2 & FlagValue.Flag9) != 0; set { if (value) Flags2 |= FlagValue.Flag9; else Flags2 &= FlagValue.InvFlag9; } }
    public bool FullVolDeath { get => (Flags2 & FlagValue.Flag10) != 0; set { if (value) Flags2 |= FlagValue.Flag10; else Flags2 &= FlagValue.InvFlag10; } }
    public bool DoHarmSpecies { get => (Flags2 & FlagValue.Flag11) != 0; set { if (value) Flags2 |= FlagValue.Flag11; else Flags2 &= FlagValue.InvFlag11; } }
    public bool Invulnerable { get => (Flags2 & FlagValue.Flag12) != 0; set { if (value) Flags2 |= FlagValue.Flag12; else Flags2 &= FlagValue.InvFlag12; } }
    public bool IsMonster { get => (Flags2 & FlagValue.Flag13) != 0; set { if (value) Flags2 |= FlagValue.Flag13; else Flags2 &= FlagValue.InvFlag13; } }
    public bool Friendly { get => (Flags2 & FlagValue.Flag14) != 0; set { if (value) Flags2 |= FlagValue.Flag14; else Flags2 &= FlagValue.InvFlag14; } }
    public bool StepMissile { get => (Flags2 & FlagValue.Flag15) != 0; set { if (value) Flags2 |= FlagValue.Flag15; else Flags2 &= FlagValue.InvFlag15; } }
    public bool NoFriction { get => (Flags2 & FlagValue.Flag16) != 0; set { if (value) Flags2 |= FlagValue.Flag16; else Flags2 &= FlagValue.InvFlag16; } }
    public bool DontFall { get => (Flags2 & FlagValue.Flag17) != 0; set { if (value) Flags2 |= FlagValue.Flag17; else Flags2 &= FlagValue.InvFlag17; } }
    public bool WeaponWimpyWeapon { get => (Flags2 & FlagValue.Flag18) != 0; set { if (value) Flags2 |= FlagValue.Flag18; else Flags2 &= FlagValue.InvFlag18; } }
    public bool WeaponNoAutoSwitch { get => (Flags2 & FlagValue.Flag19) != 0; set { if (value) Flags2 |= FlagValue.Flag19; else Flags2 &= FlagValue.InvFlag19; } }
    public bool InventoryAlwaysPickup { get => (Flags2 & FlagValue.Flag20) != 0; set { if (value) Flags2 |= FlagValue.Flag20; else Flags2 &= FlagValue.InvFlag20; } }
    public bool WeaponNoAlert { get => (Flags2 & FlagValue.Flag21) != 0; set { if (value) Flags2 |= FlagValue.Flag21; else Flags2 &= FlagValue.InvFlag21; } }
    public bool WeaponMeleeWeapon { get => (Flags2 & FlagValue.Flag22) != 0; set { if (value) Flags2 |= FlagValue.Flag22; else Flags2 &= FlagValue.InvFlag22; } }
    public bool WindThrust { get => (Flags2 & FlagValue.Flag23) != 0; set { if (value) Flags2 |= FlagValue.Flag23; else Flags2 &= FlagValue.InvFlag23; } }
    public bool Touchy { get => (Flags2 & FlagValue.Flag24) != 0; set { if (value) Flags2 |= FlagValue.Flag24; else Flags2 &= FlagValue.InvFlag24; } }
    public bool ForceRadiusDmg { get => (Flags2 & FlagValue.Flag25) != 0; set { if (value) Flags2 |= FlagValue.Flag25; else Flags2 &= FlagValue.InvFlag25; } }
    public bool NoRadiusDmg { get => (Flags2 & FlagValue.Flag26) != 0; set { if (value) Flags2 |= FlagValue.Flag26; else Flags2 &= FlagValue.InvFlag26; } }
    public bool NoVerticalMeleeRange { get => (Flags2 & FlagValue.Flag27) != 0; set { if (value) Flags2 |= FlagValue.Flag27; else Flags2 &= FlagValue.InvFlag27; } }
    public bool MissileMore { get => (Flags2 & FlagValue.Flag28) != 0; set { if (value) Flags2 |= FlagValue.Flag28; else Flags2 &= FlagValue.InvFlag28; } }
    public bool MissileEvenMore { get => (Flags2 & FlagValue.Flag29) != 0; set { if (value) Flags2 |= FlagValue.Flag29; else Flags2 &= FlagValue.InvFlag29; } }
    public bool OldRadiusDmg { get => (Flags2 & FlagValue.Flag30) != 0; set { if (value) Flags2 |= FlagValue.Flag30; else Flags2 &= FlagValue.InvFlag30; } }
    public bool MbfBouncer { get => (Flags2 & FlagValue.Flag31) != 0; set { if (value) Flags2 |= FlagValue.Flag31; else Flags2 &= FlagValue.InvFlag31; } }
    public bool Ripper { get => (Flags2 & FlagValue.Flag32) != 0; set { if (value) Flags2 |= FlagValue.Flag32; else Flags2 &= FlagValue.InvFlag32; } }
    public bool NoTeleport { get => (Flags3 & FlagValue.Flag1) != 0; set { if (value) Flags3 |= FlagValue.Flag1; else Flags3 &= FlagValue.InvFlag1; } }
    public bool Invisible { get => (Flags3 & FlagValue.Flag2) != 0; set { if (value) Flags3 |= FlagValue.Flag2; else Flags3 &= FlagValue.InvFlag2; } }
    public bool JustAttacked { get => (Flags3 & FlagValue.Flag3) != 0; set { if (value) Flags3 |= FlagValue.Flag3; else Flags3 &= FlagValue.InvFlag3; } }
    public bool Bright { get => (Flags3 & FlagValue.Flag4) != 0; set { if (value) Flags3 |= FlagValue.Flag4; else Flags3 &= FlagValue.InvFlag4; } }
    public bool IsTeleportSpot { get => (Flags3 & FlagValue.Flag5) != 0; set { if (value) Flags3 |= FlagValue.Flag5; else Flags3 &= FlagValue.InvFlag5; } }
    public bool WeaponNoAutofire { get => (Flags3 & FlagValue.Flag6) != 0; set { if (value) Flags3 |= FlagValue.Flag6; else Flags3 &= FlagValue.InvFlag6; } }
    public bool IgnoreDropOff { get => (Flags3 & FlagValue.Flag7) != 0; set { if (value) Flags3 |= FlagValue.Flag7; else Flags3 &= FlagValue.InvFlag7; } }
    public bool MonsterMove { get => (Flags3 & FlagValue.Flag8) != 0; set { if (value) Flags3 |= FlagValue.Flag8; else Flags3 &= FlagValue.InvFlag8; } }
    public bool Fly { get => (Flags3 & FlagValue.Flag9) != 0; set { if (value) Flags3 |= FlagValue.Flag9; else Flags3 &= FlagValue.InvFlag9; } }
    public bool Teleported { get => (Flags3 & FlagValue.Flag10) != 0; set { if (value) Flags3 |= FlagValue.Flag10; else Flags3 &= FlagValue.InvFlag10; } }
    public bool CrushGiblets { get => (Flags3 & FlagValue.Flag11) != 0; set { if (value) Flags3 |= FlagValue.Flag11; else Flags3 &= FlagValue.InvFlag11; } }
    public bool Translation1 { get => (Flags3 & FlagValue.Flag12) != 0; set { if (value) Flags3 |= FlagValue.Flag12; else Flags3 &= FlagValue.InvFlag12; } }
    public bool Translation2 { get => (Flags3 & FlagValue.Flag13) != 0; set { if (value) Flags3 |= FlagValue.Flag13; else Flags3 &= FlagValue.InvFlag13; } }

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
