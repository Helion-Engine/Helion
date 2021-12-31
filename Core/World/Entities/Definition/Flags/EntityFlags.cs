using Helion.Models;
using System;
using System.Runtime.CompilerServices;

namespace Helion.World.Entities.Definition.Flags;

public struct EntityFlags
{
    // Flags6 & FlagValue.Flag6 is available
    // Flags7 & FlagValue.Flag32 is available

    public static readonly int NumFlags = Enum.GetValues(typeof(EntityFlag)).Length;

    private int Flags1;
    private int Flags2;
    private int Flags3;
    private int Flags4;
    private int Flags5;
    private int Flags6;
    private int Flags7;
    private int Flags8;
    private int Flags9;
    private int Flags10;

    public bool AbsMaskAngle { get => (Flags1 & FlagValue.Flag1) != 0; set { if (value) Flags1 |= FlagValue.Flag1; else Flags1 &= FlagValue.InvFlag1; } }
    public bool AbsMaskPitch { get => (Flags1 & FlagValue.Flag2) != 0; set { if (value) Flags1 |= FlagValue.Flag2; else Flags1 &= FlagValue.InvFlag2; } }
    public bool ActivateImpact { get => (Flags1 & FlagValue.Flag3) != 0; set { if (value) Flags1 |= FlagValue.Flag3; else Flags1 &= FlagValue.InvFlag3; } }
    public bool ActivateMCross { get => (Flags1 & FlagValue.Flag4) != 0; set { if (value) Flags1 |= FlagValue.Flag4; else Flags1 &= FlagValue.InvFlag4; } }
    public bool ActivatePCross { get => (Flags1 & FlagValue.Flag5) != 0; set { if (value) Flags1 |= FlagValue.Flag5; else Flags1 &= FlagValue.InvFlag5; } }
    public bool ActLikeBridge { get => (Flags1 & FlagValue.Flag6) != 0; set { if (value) Flags1 |= FlagValue.Flag6; else Flags1 &= FlagValue.InvFlag6; } }
    public bool AdditivePoisonDamage { get => (Flags1 & FlagValue.Flag7) != 0; set { if (value) Flags1 |= FlagValue.Flag7; else Flags1 &= FlagValue.InvFlag7; } }
    public bool AdditivePoisonDuration { get => (Flags1 & FlagValue.Flag8) != 0; set { if (value) Flags1 |= FlagValue.Flag8; else Flags1 &= FlagValue.InvFlag8; } }
    public bool AimReflect { get => (Flags1 & FlagValue.Flag9) != 0; set { if (value) Flags1 |= FlagValue.Flag9; else Flags1 &= FlagValue.InvFlag9; } }
    public bool AllowBounceOnActors { get => (Flags1 & FlagValue.Flag10) != 0; set { if (value) Flags1 |= FlagValue.Flag10; else Flags1 &= FlagValue.InvFlag10; } }
    public bool AllowPain { get => (Flags1 & FlagValue.Flag11) != 0; set { if (value) Flags1 |= FlagValue.Flag11; else Flags1 &= FlagValue.InvFlag11; } }
    public bool AllowParticles { get => (Flags1 & FlagValue.Flag12) != 0; set { if (value) Flags1 |= FlagValue.Flag12; else Flags1 &= FlagValue.InvFlag12; } }
    public bool AllowThruFlags { get => (Flags1 & FlagValue.Flag13) != 0; set { if (value) Flags1 |= FlagValue.Flag13; else Flags1 &= FlagValue.InvFlag13; } }
    public bool AlwaysFast { get => (Flags1 & FlagValue.Flag14) != 0; set { if (value) Flags1 |= FlagValue.Flag14; else Flags1 &= FlagValue.InvFlag14; } }
    public bool AlwaysPuff { get => (Flags1 & FlagValue.Flag15) != 0; set { if (value) Flags1 |= FlagValue.Flag15; else Flags1 &= FlagValue.InvFlag15; } }
    public bool AlwaysRespawn { get => (Flags1 & FlagValue.Flag16) != 0; set { if (value) Flags1 |= FlagValue.Flag16; else Flags1 &= FlagValue.InvFlag16; } }
    public bool AlwaysTelefrag { get => (Flags1 & FlagValue.Flag17) != 0; set { if (value) Flags1 |= FlagValue.Flag17; else Flags1 &= FlagValue.InvFlag17; } }
    public bool Ambush { get => (Flags1 & FlagValue.Flag18) != 0; set { if (value) Flags1 |= FlagValue.Flag18; else Flags1 &= FlagValue.InvFlag18; } }
    public bool AvoidMelee { get => (Flags1 & FlagValue.Flag19) != 0; set { if (value) Flags1 |= FlagValue.Flag19; else Flags1 &= FlagValue.InvFlag19; } }
    public bool Blasted { get => (Flags1 & FlagValue.Flag20) != 0; set { if (value) Flags1 |= FlagValue.Flag20; else Flags1 &= FlagValue.InvFlag20; } }
    public bool BlockAsPlayer { get => (Flags1 & FlagValue.Flag21) != 0; set { if (value) Flags1 |= FlagValue.Flag21; else Flags1 &= FlagValue.InvFlag21; } }
    public bool BlockedBySolidActors { get => (Flags1 & FlagValue.Flag22) != 0; set { if (value) Flags1 |= FlagValue.Flag22; else Flags1 &= FlagValue.InvFlag22; } }
    public bool BloodlessImpact { get => (Flags1 & FlagValue.Flag23) != 0; set { if (value) Flags1 |= FlagValue.Flag23; else Flags1 &= FlagValue.InvFlag23; } }
    public bool BloodSplatter { get => (Flags1 & FlagValue.Flag24) != 0; set { if (value) Flags1 |= FlagValue.Flag24; else Flags1 &= FlagValue.InvFlag24; } }
    public bool Boss { get => (Flags1 & FlagValue.Flag25) != 0; set { if (value) Flags1 |= FlagValue.Flag25; else Flags1 &= FlagValue.InvFlag25; } }
    public bool BossDeath { get => (Flags1 & FlagValue.Flag26) != 0; set { if (value) Flags1 |= FlagValue.Flag26; else Flags1 &= FlagValue.InvFlag26; } }
    public bool BounceAutoOff { get => (Flags1 & FlagValue.Flag27) != 0; set { if (value) Flags1 |= FlagValue.Flag27; else Flags1 &= FlagValue.InvFlag27; } }
    public bool BounceAutoOffFloorOnly { get => (Flags1 & FlagValue.Flag28) != 0; set { if (value) Flags1 |= FlagValue.Flag28; else Flags1 &= FlagValue.InvFlag28; } }
    public bool BounceLikeHeretic { get => (Flags1 & FlagValue.Flag29) != 0; set { if (value) Flags1 |= FlagValue.Flag29; else Flags1 &= FlagValue.InvFlag29; } }
    public bool BounceOnActors { get => (Flags1 & FlagValue.Flag30) != 0; set { if (value) Flags1 |= FlagValue.Flag30; else Flags1 &= FlagValue.InvFlag30; } }
    public bool BounceOnCeilings { get => (Flags1 & FlagValue.Flag31) != 0; set { if (value) Flags1 |= FlagValue.Flag31; else Flags1 &= FlagValue.InvFlag31; } }
    public bool BounceOnFloors { get => (Flags1 & FlagValue.Flag32) != 0; set { if (value) Flags1 |= FlagValue.Flag32; else Flags1 &= FlagValue.InvFlag32; } }
    public bool BounceOnUnrippables { get => (Flags2 & FlagValue.Flag1) != 0; set { if (value) Flags2 |= FlagValue.Flag1; else Flags2 &= FlagValue.InvFlag1; } }
    public bool BounceOnWalls { get => (Flags2 & FlagValue.Flag2) != 0; set { if (value) Flags2 |= FlagValue.Flag2; else Flags2 &= FlagValue.InvFlag2; } }
    public bool Bright { get => (Flags2 & FlagValue.Flag3) != 0; set { if (value) Flags2 |= FlagValue.Flag3; else Flags2 &= FlagValue.InvFlag3; } }
    public bool Buddha { get => (Flags2 & FlagValue.Flag4) != 0; set { if (value) Flags2 |= FlagValue.Flag4; else Flags2 &= FlagValue.InvFlag4; } }
    public bool BumpSpecial { get => (Flags2 & FlagValue.Flag5) != 0; set { if (value) Flags2 |= FlagValue.Flag5; else Flags2 &= FlagValue.InvFlag5; } }
    public bool CanBlast { get => (Flags2 & FlagValue.Flag6) != 0; set { if (value) Flags2 |= FlagValue.Flag6; else Flags2 &= FlagValue.InvFlag6; } }
    public bool CanBounceWater { get => (Flags2 & FlagValue.Flag7) != 0; set { if (value) Flags2 |= FlagValue.Flag7; else Flags2 &= FlagValue.InvFlag7; } }
    public bool CannotPush { get => (Flags2 & FlagValue.Flag8) != 0; set { if (value) Flags2 |= FlagValue.Flag8; else Flags2 &= FlagValue.InvFlag8; } }
    public bool CanPass { get => (Flags2 & FlagValue.Flag9) != 0; set { if (value) Flags2 |= FlagValue.Flag9; else Flags2 &= FlagValue.InvFlag9; } }
    public bool CanPushWalls { get => (Flags2 & FlagValue.Flag10) != 0; set { if (value) Flags2 |= FlagValue.Flag10; else Flags2 &= FlagValue.InvFlag10; } }
    public bool CantLeaveFloorPic { get => (Flags2 & FlagValue.Flag11) != 0; set { if (value) Flags2 |= FlagValue.Flag11; else Flags2 &= FlagValue.InvFlag11; } }
    public bool CantSeek { get => (Flags2 & FlagValue.Flag12) != 0; set { if (value) Flags2 |= FlagValue.Flag12; else Flags2 &= FlagValue.InvFlag12; } }
    public bool CanUseWalls { get => (Flags2 & FlagValue.Flag13) != 0; set { if (value) Flags2 |= FlagValue.Flag13; else Flags2 &= FlagValue.InvFlag13; } }
    public bool CausePain { get => (Flags2 & FlagValue.Flag14) != 0; set { if (value) Flags2 |= FlagValue.Flag14; else Flags2 &= FlagValue.InvFlag14; } }
    public bool CeilingHugger { get => (Flags2 & FlagValue.Flag15) != 0; set { if (value) Flags2 |= FlagValue.Flag15; else Flags2 &= FlagValue.InvFlag15; } }
    public bool Corpse { get => (Flags2 & FlagValue.Flag16) != 0; set { if (value) Flags2 |= FlagValue.Flag16; else Flags2 &= FlagValue.InvFlag16; } }
    public bool CountItem { get => (Flags2 & FlagValue.Flag17) != 0; set { if (value) Flags2 |= FlagValue.Flag17; else Flags2 &= FlagValue.InvFlag17; } }
    public bool CountKill { get => (Flags2 & FlagValue.Flag18) != 0; set { if (value) Flags2 |= FlagValue.Flag18; else Flags2 &= FlagValue.InvFlag18; } }
    public bool CountSecret { get => (Flags2 & FlagValue.Flag19) != 0; set { if (value) Flags2 |= FlagValue.Flag19; else Flags2 &= FlagValue.InvFlag19; } }
    public bool Deflect { get => (Flags2 & FlagValue.Flag20) != 0; set { if (value) Flags2 |= FlagValue.Flag20; else Flags2 &= FlagValue.InvFlag20; } }
    public bool DehExplosion { get => (Flags2 & FlagValue.Flag21) != 0; set { if (value) Flags2 |= FlagValue.Flag21; else Flags2 &= FlagValue.InvFlag21; } }
    public bool DoHarmSpecies { get => (Flags2 & FlagValue.Flag22) != 0; set { if (value) Flags2 |= FlagValue.Flag22; else Flags2 &= FlagValue.InvFlag22; } }
    public bool DontBlast { get => (Flags2 & FlagValue.Flag23) != 0; set { if (value) Flags2 |= FlagValue.Flag23; else Flags2 &= FlagValue.InvFlag23; } }
    public bool DontBounceOnShootables { get => (Flags2 & FlagValue.Flag24) != 0; set { if (value) Flags2 |= FlagValue.Flag24; else Flags2 &= FlagValue.InvFlag24; } }
    public bool DontBounceOnSky { get => (Flags2 & FlagValue.Flag25) != 0; set { if (value) Flags2 |= FlagValue.Flag25; else Flags2 &= FlagValue.InvFlag25; } }
    public bool DontCorpse { get => (Flags2 & FlagValue.Flag26) != 0; set { if (value) Flags2 |= FlagValue.Flag26; else Flags2 &= FlagValue.InvFlag26; } }
    public bool DontDrain { get => (Flags2 & FlagValue.Flag27) != 0; set { if (value) Flags2 |= FlagValue.Flag27; else Flags2 &= FlagValue.InvFlag27; } }
    public bool DontFaceTalker { get => (Flags2 & FlagValue.Flag28) != 0; set { if (value) Flags2 |= FlagValue.Flag28; else Flags2 &= FlagValue.InvFlag28; } }
    public bool DontFall { get => (Flags2 & FlagValue.Flag29) != 0; set { if (value) Flags2 |= FlagValue.Flag29; else Flags2 &= FlagValue.InvFlag29; } }
    public bool DontGib { get => (Flags2 & FlagValue.Flag30) != 0; set { if (value) Flags2 |= FlagValue.Flag30; else Flags2 &= FlagValue.InvFlag30; } }
    public bool DontHarmClass { get => (Flags2 & FlagValue.Flag31) != 0; set { if (value) Flags2 |= FlagValue.Flag31; else Flags2 &= FlagValue.InvFlag31; } }
    public bool DontHarmSpecies { get => (Flags2 & FlagValue.Flag32) != 0; set { if (value) Flags2 |= FlagValue.Flag32; else Flags2 &= FlagValue.InvFlag32; } }
    public bool DontHurtSpecies { get => (Flags3 & FlagValue.Flag1) != 0; set { if (value) Flags3 |= FlagValue.Flag1; else Flags3 &= FlagValue.InvFlag1; } }
    public bool DontInterpolate { get => (Flags3 & FlagValue.Flag2) != 0; set { if (value) Flags3 |= FlagValue.Flag2; else Flags3 &= FlagValue.InvFlag2; } }
    public bool DontMorph { get => (Flags3 & FlagValue.Flag3) != 0; set { if (value) Flags3 |= FlagValue.Flag3; else Flags3 &= FlagValue.InvFlag3; } }
    public bool DontOverlap { get => (Flags3 & FlagValue.Flag4) != 0; set { if (value) Flags3 |= FlagValue.Flag4; else Flags3 &= FlagValue.InvFlag4; } }
    public bool DontReflect { get => (Flags3 & FlagValue.Flag5) != 0; set { if (value) Flags3 |= FlagValue.Flag5; else Flags3 &= FlagValue.InvFlag5; } }
    public bool DontRip { get => (Flags3 & FlagValue.Flag6) != 0; set { if (value) Flags3 |= FlagValue.Flag6; else Flags3 &= FlagValue.InvFlag6; } }
    public bool DontSeekInvisible { get => (Flags3 & FlagValue.Flag7) != 0; set { if (value) Flags3 |= FlagValue.Flag7; else Flags3 &= FlagValue.InvFlag7; } }
    public bool DontSplash { get => (Flags3 & FlagValue.Flag8) != 0; set { if (value) Flags3 |= FlagValue.Flag8; else Flags3 &= FlagValue.InvFlag8; } }
    public bool DontSquash { get => (Flags3 & FlagValue.Flag9) != 0; set { if (value) Flags3 |= FlagValue.Flag9; else Flags3 &= FlagValue.InvFlag9; } }
    public bool DontThrust { get => (Flags3 & FlagValue.Flag10) != 0; set { if (value) Flags3 |= FlagValue.Flag10; else Flags3 &= FlagValue.InvFlag10; } }
    public bool DontTranslate { get => (Flags3 & FlagValue.Flag11) != 0; set { if (value) Flags3 |= FlagValue.Flag11; else Flags3 &= FlagValue.InvFlag11; } }
    public bool DoomBounce { get => (Flags3 & FlagValue.Flag12) != 0; set { if (value) Flags3 |= FlagValue.Flag12; else Flags3 &= FlagValue.InvFlag12; } }
    public bool Dormant { get => (Flags3 & FlagValue.Flag13) != 0; set { if (value) Flags3 |= FlagValue.Flag13; else Flags3 &= FlagValue.InvFlag13; } }
    public bool Dropoff { get => (Flags3 & FlagValue.Flag14) != 0; set { if (value) Flags3 |= FlagValue.Flag14; else Flags3 &= FlagValue.InvFlag14; } }
    public bool Dropped { get => (Flags3 & FlagValue.Flag15) != 0; set { if (value) Flags3 |= FlagValue.Flag15; else Flags3 &= FlagValue.InvFlag15; } }
    public bool ExploCount { get => (Flags3 & FlagValue.Flag16) != 0; set { if (value) Flags3 |= FlagValue.Flag16; else Flags3 &= FlagValue.InvFlag16; } }
    public bool ExplodeOnWater { get => (Flags3 & FlagValue.Flag17) != 0; set { if (value) Flags3 |= FlagValue.Flag17; else Flags3 &= FlagValue.InvFlag17; } }
    public bool ExtremeDeath { get => (Flags3 & FlagValue.Flag18) != 0; set { if (value) Flags3 |= FlagValue.Flag18; else Flags3 &= FlagValue.InvFlag18; } }
    public bool Faster { get => (Flags3 & FlagValue.Flag19) != 0; set { if (value) Flags3 |= FlagValue.Flag19; else Flags3 &= FlagValue.InvFlag19; } }
    public bool FastMelee { get => (Flags3 & FlagValue.Flag20) != 0; set { if (value) Flags3 |= FlagValue.Flag20; else Flags3 &= FlagValue.InvFlag20; } }
    public bool FireDamage { get => (Flags3 & FlagValue.Flag21) != 0; set { if (value) Flags3 |= FlagValue.Flag21; else Flags3 &= FlagValue.InvFlag21; } }
    public bool FireResist { get => (Flags3 & FlagValue.Flag22) != 0; set { if (value) Flags3 |= FlagValue.Flag22; else Flags3 &= FlagValue.InvFlag22; } }
    public bool FixMapThingPos { get => (Flags3 & FlagValue.Flag23) != 0; set { if (value) Flags3 |= FlagValue.Flag23; else Flags3 &= FlagValue.InvFlag23; } }
    public bool FlatSprite { get => (Flags3 & FlagValue.Flag24) != 0; set { if (value) Flags3 |= FlagValue.Flag24; else Flags3 &= FlagValue.InvFlag24; } }
    public bool Float { get => (Flags3 & FlagValue.Flag25) != 0; set { if (value) Flags3 |= FlagValue.Flag25; else Flags3 &= FlagValue.InvFlag25; } }
    public bool FloatBob { get => (Flags3 & FlagValue.Flag26) != 0; set { if (value) Flags3 |= FlagValue.Flag26; else Flags3 &= FlagValue.InvFlag26; } }
    public bool FloorClip { get => (Flags3 & FlagValue.Flag27) != 0; set { if (value) Flags3 |= FlagValue.Flag27; else Flags3 &= FlagValue.InvFlag27; } }
    public bool FloorHugger { get => (Flags3 & FlagValue.Flag28) != 0; set { if (value) Flags3 |= FlagValue.Flag28; else Flags3 &= FlagValue.InvFlag28; } }
    public bool FoilBuddha { get => (Flags3 & FlagValue.Flag29) != 0; set { if (value) Flags3 |= FlagValue.Flag29; else Flags3 &= FlagValue.InvFlag29; } }
    public bool FoilInvul { get => (Flags3 & FlagValue.Flag30) != 0; set { if (value) Flags3 |= FlagValue.Flag30; else Flags3 &= FlagValue.InvFlag30; } }
    public bool ForceDecal { get => (Flags3 & FlagValue.Flag31) != 0; set { if (value) Flags3 |= FlagValue.Flag31; else Flags3 &= FlagValue.InvFlag31; } }
    public bool ForceInFighting { get => (Flags3 & FlagValue.Flag32) != 0; set { if (value) Flags3 |= FlagValue.Flag32; else Flags3 &= FlagValue.InvFlag32; } }
    public bool ForcePain { get => (Flags4 & FlagValue.Flag1) != 0; set { if (value) Flags4 |= FlagValue.Flag1; else Flags4 &= FlagValue.InvFlag1; } }
    public bool ForceRadiusDmg { get => (Flags4 & FlagValue.Flag2) != 0; set { if (value) Flags4 |= FlagValue.Flag2; else Flags4 &= FlagValue.InvFlag2; } }
    public bool ForceXYBillboard { get => (Flags4 & FlagValue.Flag3) != 0; set { if (value) Flags4 |= FlagValue.Flag3; else Flags4 &= FlagValue.InvFlag3; } }
    public bool ForceYBillboard { get => (Flags4 & FlagValue.Flag4) != 0; set { if (value) Flags4 |= FlagValue.Flag4; else Flags4 &= FlagValue.InvFlag4; } }
    public bool ForceZeroRadiusDmg { get => (Flags4 & FlagValue.Flag5) != 0; set { if (value) Flags4 |= FlagValue.Flag5; else Flags4 &= FlagValue.InvFlag5; } }
    public bool Friendly { get => (Flags4 & FlagValue.Flag6) != 0; set { if (value) Flags4 |= FlagValue.Flag6; else Flags4 &= FlagValue.InvFlag6; } }
    public bool Frightened { get => (Flags4 & FlagValue.Flag7) != 0; set { if (value) Flags4 |= FlagValue.Flag7; else Flags4 &= FlagValue.InvFlag7; } }
    public bool Frightening { get => (Flags4 & FlagValue.Flag8) != 0; set { if (value) Flags4 |= FlagValue.Flag8; else Flags4 &= FlagValue.InvFlag8; } }
    public bool FullVolActive { get => (Flags4 & FlagValue.Flag9) != 0; set { if (value) Flags4 |= FlagValue.Flag9; else Flags4 &= FlagValue.InvFlag9; } }
    public bool FullVolDeath { get => (Flags4 & FlagValue.Flag10) != 0; set { if (value) Flags4 |= FlagValue.Flag10; else Flags4 &= FlagValue.InvFlag10; } }
    public bool GetOwner { get => (Flags4 & FlagValue.Flag11) != 0; set { if (value) Flags4 |= FlagValue.Flag11; else Flags4 &= FlagValue.InvFlag11; } }
    public bool Ghost { get => (Flags4 & FlagValue.Flag12) != 0; set { if (value) Flags4 |= FlagValue.Flag12; else Flags4 &= FlagValue.InvFlag12; } }
    public bool GrenadeTrail { get => (Flags4 & FlagValue.Flag13) != 0; set { if (value) Flags4 |= FlagValue.Flag13; else Flags4 &= FlagValue.InvFlag13; } }
    public bool HarmFriends { get => (Flags4 & FlagValue.Flag14) != 0; set { if (value) Flags4 |= FlagValue.Flag14; else Flags4 &= FlagValue.InvFlag14; } }
    public bool HereticBounce { get => (Flags4 & FlagValue.Flag15) != 0; set { if (value) Flags4 |= FlagValue.Flag15; else Flags4 &= FlagValue.InvFlag15; } }
    public bool HexenBounce { get => (Flags4 & FlagValue.Flag16) != 0; set { if (value) Flags4 |= FlagValue.Flag16; else Flags4 &= FlagValue.InvFlag16; } }
    public bool HitMaster { get => (Flags4 & FlagValue.Flag17) != 0; set { if (value) Flags4 |= FlagValue.Flag17; else Flags4 &= FlagValue.InvFlag17; } }
    public bool HitOwner { get => (Flags4 & FlagValue.Flag18) != 0; set { if (value) Flags4 |= FlagValue.Flag18; else Flags4 &= FlagValue.InvFlag18; } }
    public bool HitTarget { get => (Flags4 & FlagValue.Flag19) != 0; set { if (value) Flags4 |= FlagValue.Flag19; else Flags4 &= FlagValue.InvFlag19; } }
    public bool HitTracer { get => (Flags4 & FlagValue.Flag20) != 0; set { if (value) Flags4 |= FlagValue.Flag20; else Flags4 &= FlagValue.InvFlag20; } }
    public bool IceCorpse { get => (Flags4 & FlagValue.Flag21) != 0; set { if (value) Flags4 |= FlagValue.Flag21; else Flags4 &= FlagValue.InvFlag21; } }
    public bool IceDamage { get => (Flags4 & FlagValue.Flag22) != 0; set { if (value) Flags4 |= FlagValue.Flag22; else Flags4 &= FlagValue.InvFlag22; } }
    public bool IceShatter { get => (Flags4 & FlagValue.Flag23) != 0; set { if (value) Flags4 |= FlagValue.Flag23; else Flags4 &= FlagValue.InvFlag23; } }
    public bool InCombat { get => (Flags4 & FlagValue.Flag24) != 0; set { if (value) Flags4 |= FlagValue.Flag24; else Flags4 &= FlagValue.InvFlag24; } }
    public bool InterpolateAngles { get => (Flags4 & FlagValue.Flag25) != 0; set { if (value) Flags4 |= FlagValue.Flag25; else Flags4 &= FlagValue.InvFlag25; } }
    public bool InventoryAdditiveTime { get => (Flags4 & FlagValue.Flag26) != 0; set { if (value) Flags4 |= FlagValue.Flag26; else Flags4 &= FlagValue.InvFlag26; } }
    public bool InventoryAlwaysPickup { get => (Flags4 & FlagValue.Flag27) != 0; set { if (value) Flags4 |= FlagValue.Flag27; else Flags4 &= FlagValue.InvFlag27; } }
    public bool InventoryAlwaysRespawn { get => (Flags4 & FlagValue.Flag28) != 0; set { if (value) Flags4 |= FlagValue.Flag28; else Flags4 &= FlagValue.InvFlag28; } }
    public bool InventoryAutoActivate { get => (Flags4 & FlagValue.Flag29) != 0; set { if (value) Flags4 |= FlagValue.Flag29; else Flags4 &= FlagValue.InvFlag29; } }
    public bool InventoryBigPowerup { get => (Flags4 & FlagValue.Flag30) != 0; set { if (value) Flags4 |= FlagValue.Flag30; else Flags4 &= FlagValue.InvFlag30; } }
    public bool InventoryFancyPickupSound { get => (Flags4 & FlagValue.Flag31) != 0; set { if (value) Flags4 |= FlagValue.Flag31; else Flags4 &= FlagValue.InvFlag31; } }
    public bool InventoryHubPower { get => (Flags4 & FlagValue.Flag32) != 0; set { if (value) Flags4 |= FlagValue.Flag32; else Flags4 &= FlagValue.InvFlag32; } }
    public bool InventoryIgnoreSkill { get => (Flags5 & FlagValue.Flag1) != 0; set { if (value) Flags5 |= FlagValue.Flag1; else Flags5 &= FlagValue.InvFlag1; } }
    public bool InventoryInterHubStrip { get => (Flags5 & FlagValue.Flag2) != 0; set { if (value) Flags5 |= FlagValue.Flag2; else Flags5 &= FlagValue.InvFlag2; } }
    public bool InventoryInvbar { get => (Flags5 & FlagValue.Flag3) != 0; set { if (value) Flags5 |= FlagValue.Flag3; else Flags5 &= FlagValue.InvFlag3; } }
    public bool InventoryIsArmor { get => (Flags5 & FlagValue.Flag4) != 0; set { if (value) Flags5 |= FlagValue.Flag4; else Flags5 &= FlagValue.InvFlag4; } }
    public bool InventoryIsHealth { get => (Flags5 & FlagValue.Flag5) != 0; set { if (value) Flags5 |= FlagValue.Flag5; else Flags5 &= FlagValue.InvFlag5; } }
    public bool InventoryKeepDepleted { get => (Flags5 & FlagValue.Flag6) != 0; set { if (value) Flags5 |= FlagValue.Flag6; else Flags5 &= FlagValue.InvFlag6; } }
    public bool InventoryNeverRespawn { get => (Flags5 & FlagValue.Flag7) != 0; set { if (value) Flags5 |= FlagValue.Flag7; else Flags5 &= FlagValue.InvFlag7; } }
    public bool InventoryNoAttenPickupSound { get => (Flags5 & FlagValue.Flag8) != 0; set { if (value) Flags5 |= FlagValue.Flag8; else Flags5 &= FlagValue.InvFlag8; } }
    public bool InventoryNoScreenBlink { get => (Flags5 & FlagValue.Flag9) != 0; set { if (value) Flags5 |= FlagValue.Flag9; else Flags5 &= FlagValue.InvFlag9; } }
    public bool InventoryNoScreenFlash { get => (Flags5 & FlagValue.Flag10) != 0; set { if (value) Flags5 |= FlagValue.Flag10; else Flags5 &= FlagValue.InvFlag10; } }
    public bool InventoryNoTeleportFreeze { get => (Flags5 & FlagValue.Flag11) != 0; set { if (value) Flags5 |= FlagValue.Flag11; else Flags5 &= FlagValue.InvFlag11; } }
    public bool InventoryPersistentPower { get => (Flags5 & FlagValue.Flag12) != 0; set { if (value) Flags5 |= FlagValue.Flag12; else Flags5 &= FlagValue.InvFlag12; } }
    public bool InventoryPickupFlash { get => (Flags5 & FlagValue.Flag13) != 0; set { if (value) Flags5 |= FlagValue.Flag13; else Flags5 &= FlagValue.InvFlag13; } }
    public bool InventoryQuiet { get => (Flags5 & FlagValue.Flag14) != 0; set { if (value) Flags5 |= FlagValue.Flag14; else Flags5 &= FlagValue.InvFlag14; } }
    public bool InventoryRestrictAbsolutely { get => (Flags5 & FlagValue.Flag15) != 0; set { if (value) Flags5 |= FlagValue.Flag15; else Flags5 &= FlagValue.InvFlag15; } }
    public bool InventoryTossed { get => (Flags5 & FlagValue.Flag16) != 0; set { if (value) Flags5 |= FlagValue.Flag16; else Flags5 &= FlagValue.InvFlag16; } }
    public bool InventoryTransfer { get => (Flags5 & FlagValue.Flag17) != 0; set { if (value) Flags5 |= FlagValue.Flag17; else Flags5 &= FlagValue.InvFlag17; } }
    public bool InventoryUnclearable { get => (Flags5 & FlagValue.Flag18) != 0; set { if (value) Flags5 |= FlagValue.Flag18; else Flags5 &= FlagValue.InvFlag18; } }
    public bool InventoryUndroppable { get => (Flags5 & FlagValue.Flag19) != 0; set { if (value) Flags5 |= FlagValue.Flag19; else Flags5 &= FlagValue.InvFlag19; } }
    public bool InventoryUntossable { get => (Flags5 & FlagValue.Flag20) != 0; set { if (value) Flags5 |= FlagValue.Flag20; else Flags5 &= FlagValue.InvFlag20; } }
    public bool Invisible { get => (Flags5 & FlagValue.Flag21) != 0; set { if (value) Flags5 |= FlagValue.Flag21; else Flags5 &= FlagValue.InvFlag21; } }
    public bool Invulnerable { get => (Flags5 & FlagValue.Flag22) != 0; set { if (value) Flags5 |= FlagValue.Flag22; else Flags5 &= FlagValue.InvFlag22; } }
    // Note: This is ZDoom only. Vanilla would use CountKill or !IsPlayer or PlayerObj == null.
    public bool IsMonster { get => (Flags5 & FlagValue.Flag23) != 0; set { if (value) Flags5 |= FlagValue.Flag23; else Flags5 &= FlagValue.InvFlag23; } }
    public bool IsTeleportSpot { get => (Flags5 & FlagValue.Flag24) != 0; set { if (value) Flags5 |= FlagValue.Flag24; else Flags5 &= FlagValue.InvFlag24; } }
    public bool JumpDown { get => (Flags5 & FlagValue.Flag25) != 0; set { if (value) Flags5 |= FlagValue.Flag25; else Flags5 &= FlagValue.InvFlag25; } }
    public bool JustAttacked { get => (Flags5 & FlagValue.Flag26) != 0; set { if (value) Flags5 |= FlagValue.Flag26; else Flags5 &= FlagValue.InvFlag26; } }
    public bool JustHit { get => (Flags5 & FlagValue.Flag27) != 0; set { if (value) Flags5 |= FlagValue.Flag27; else Flags5 &= FlagValue.InvFlag27; } }
    public bool LaxTeleFragDmg { get => (Flags5 & FlagValue.Flag28) != 0; set { if (value) Flags5 |= FlagValue.Flag28; else Flags5 &= FlagValue.InvFlag28; } }
    public bool LongMeleeRange { get => (Flags5 & FlagValue.Flag29) != 0; set { if (value) Flags5 |= FlagValue.Flag29; else Flags5 &= FlagValue.InvFlag29; } }
    public bool LookAllAround { get => (Flags5 & FlagValue.Flag30) != 0; set { if (value) Flags5 |= FlagValue.Flag30; else Flags5 &= FlagValue.InvFlag30; } }
    public bool LowGravity { get => (Flags5 & FlagValue.Flag31) != 0; set { if (value) Flags5 |= FlagValue.Flag31; else Flags5 &= FlagValue.InvFlag31; } }
    public bool MaskRotation { get => (Flags5 & FlagValue.Flag32) != 0; set { if (value) Flags5 |= FlagValue.Flag32; else Flags5 &= FlagValue.InvFlag32; } }
    public bool MbfBouncer { get => (Flags6 & FlagValue.Flag1) != 0; set { if (value) Flags6 |= FlagValue.Flag1; else Flags6 &= FlagValue.InvFlag1; } }
    public bool MirrorReflect { get => (Flags6 & FlagValue.Flag2) != 0; set { if (value) Flags6 |= FlagValue.Flag2; else Flags6 &= FlagValue.InvFlag2; } }
    public bool Missile { get => (Flags6 & FlagValue.Flag3) != 0; set { if (value) Flags6 |= FlagValue.Flag3; else Flags6 &= FlagValue.InvFlag3; } }
    public bool MissileEvenMore { get => (Flags6 & FlagValue.Flag4) != 0; set { if (value) Flags6 |= FlagValue.Flag4; else Flags6 &= FlagValue.InvFlag4; } }
    public bool MissileMore { get => (Flags6 & FlagValue.Flag5) != 0; set { if (value) Flags6 |= FlagValue.Flag5; else Flags6 &= FlagValue.InvFlag5; } }
    public bool MoveWithSector { get => (Flags6 & FlagValue.Flag7) != 0; set { if (value) Flags6 |= FlagValue.Flag7; else Flags6 &= FlagValue.InvFlag7; } }
    public bool MThruSpecies { get => (Flags6 & FlagValue.Flag8) != 0; set { if (value) Flags6 |= FlagValue.Flag8; else Flags6 &= FlagValue.InvFlag8; } }
    public bool NeverFast { get => (Flags6 & FlagValue.Flag9) != 0; set { if (value) Flags6 |= FlagValue.Flag9; else Flags6 &= FlagValue.InvFlag9; } }
    public bool NeverRespawn { get => (Flags6 & FlagValue.Flag10) != 0; set { if (value) Flags6 |= FlagValue.Flag10; else Flags6 &= FlagValue.InvFlag10; } }
    public bool NeverTarget { get => (Flags6 & FlagValue.Flag11) != 0; set { if (value) Flags6 |= FlagValue.Flag11; else Flags6 &= FlagValue.InvFlag11; } }
    public bool NoBlockmap { get => (Flags6 & FlagValue.Flag12) != 0; set { if (value) Flags6 |= FlagValue.Flag12; else Flags6 &= FlagValue.InvFlag12; } }
    public bool NoBlockMonst { get => (Flags6 & FlagValue.Flag13) != 0; set { if (value) Flags6 |= FlagValue.Flag13; else Flags6 &= FlagValue.InvFlag13; } }
    public bool NoBlood { get => (Flags6 & FlagValue.Flag14) != 0; set { if (value) Flags6 |= FlagValue.Flag14; else Flags6 &= FlagValue.InvFlag14; } }
    public bool NoBloodDecals { get => (Flags6 & FlagValue.Flag15) != 0; set { if (value) Flags6 |= FlagValue.Flag15; else Flags6 &= FlagValue.InvFlag15; } }
    public bool NoBossRip { get => (Flags6 & FlagValue.Flag16) != 0; set { if (value) Flags6 |= FlagValue.Flag16; else Flags6 &= FlagValue.InvFlag16; } }
    public bool NoBounceSound { get => (Flags6 & FlagValue.Flag17) != 0; set { if (value) Flags6 |= FlagValue.Flag17; else Flags6 &= FlagValue.InvFlag17; } }
    public bool NoClip { get => (Flags6 & FlagValue.Flag18) != 0; set { if (value) Flags6 |= FlagValue.Flag18; else Flags6 &= FlagValue.InvFlag18; } }
    public bool NoDamage { get => (Flags6 & FlagValue.Flag19) != 0; set { if (value) Flags6 |= FlagValue.Flag19; else Flags6 &= FlagValue.InvFlag19; } }
    public bool NoDamageThrust { get => (Flags6 & FlagValue.Flag20) != 0; set { if (value) Flags6 |= FlagValue.Flag20; else Flags6 &= FlagValue.InvFlag20; } }
    public bool NoDecal { get => (Flags6 & FlagValue.Flag21) != 0; set { if (value) Flags6 |= FlagValue.Flag21; else Flags6 &= FlagValue.InvFlag21; } }
    public bool NoDropoff { get => (Flags6 & FlagValue.Flag22) != 0; set { if (value) Flags6 |= FlagValue.Flag22; else Flags6 &= FlagValue.InvFlag22; } }
    public bool NoExplodeFloor { get => (Flags6 & FlagValue.Flag23) != 0; set { if (value) Flags6 |= FlagValue.Flag23; else Flags6 &= FlagValue.InvFlag23; } }
    public bool NoExtremeDeath { get => (Flags6 & FlagValue.Flag24) != 0; set { if (value) Flags6 |= FlagValue.Flag24; else Flags6 &= FlagValue.InvFlag24; } }
    public bool NoFear { get => (Flags6 & FlagValue.Flag25) != 0; set { if (value) Flags6 |= FlagValue.Flag25; else Flags6 &= FlagValue.InvFlag25; } }
    public bool NoFriction { get => (Flags6 & FlagValue.Flag26) != 0; set { if (value) Flags6 |= FlagValue.Flag26; else Flags6 &= FlagValue.InvFlag26; } }
    public bool NoFrictionBounce { get => (Flags6 & FlagValue.Flag27) != 0; set { if (value) Flags6 |= FlagValue.Flag27; else Flags6 &= FlagValue.InvFlag27; } }
    public bool NoForwardFall { get => (Flags6 & FlagValue.Flag28) != 0; set { if (value) Flags6 |= FlagValue.Flag28; else Flags6 &= FlagValue.InvFlag28; } }
    public bool NoGravity { get => (Flags6 & FlagValue.Flag29) != 0; set { if (value) Flags6 |= FlagValue.Flag29; else Flags6 &= FlagValue.InvFlag29; } }
    public bool NoIceDeath { get => (Flags6 & FlagValue.Flag30) != 0; set { if (value) Flags6 |= FlagValue.Flag30; else Flags6 &= FlagValue.InvFlag30; } }
    public bool NoInfighting { get => (Flags6 & FlagValue.Flag31) != 0; set { if (value) Flags6 |= FlagValue.Flag31; else Flags6 &= FlagValue.InvFlag31; } }
    public bool NoInfightSpecies { get => (Flags6 & FlagValue.Flag32) != 0; set { if (value) Flags6 |= FlagValue.Flag32; else Flags6 &= FlagValue.InvFlag32; } }
    public bool NoInteraction { get => (Flags7 & FlagValue.Flag1) != 0; set { if (value) Flags7 |= FlagValue.Flag1; else Flags7 &= FlagValue.InvFlag1; } }
    public bool NoKillScripts { get => (Flags7 & FlagValue.Flag2) != 0; set { if (value) Flags7 |= FlagValue.Flag2; else Flags7 &= FlagValue.InvFlag2; } }
    public bool NoLiftDrop { get => (Flags7 & FlagValue.Flag3) != 0; set { if (value) Flags7 |= FlagValue.Flag3; else Flags7 &= FlagValue.InvFlag3; } }
    public bool NoMenu { get => (Flags7 & FlagValue.Flag4) != 0; set { if (value) Flags7 |= FlagValue.Flag4; else Flags7 &= FlagValue.InvFlag4; } }
    public bool NonShootable { get => (Flags7 & FlagValue.Flag5) != 0; set { if (value) Flags7 |= FlagValue.Flag5; else Flags7 &= FlagValue.InvFlag5; } }
    public bool NoPain { get => (Flags7 & FlagValue.Flag6) != 0; set { if (value) Flags7 |= FlagValue.Flag6; else Flags7 &= FlagValue.InvFlag6; } }
    public bool NoRadiusDmg { get => (Flags7 & FlagValue.Flag7) != 0; set { if (value) Flags7 |= FlagValue.Flag7; else Flags7 &= FlagValue.InvFlag7; } }
    public bool NoSector { get => (Flags7 & FlagValue.Flag8) != 0; set { if (value) Flags7 |= FlagValue.Flag8; else Flags7 &= FlagValue.InvFlag8; } }
    public bool NoSkin { get => (Flags7 & FlagValue.Flag9) != 0; set { if (value) Flags7 |= FlagValue.Flag9; else Flags7 &= FlagValue.InvFlag9; } }
    public bool NoSplashAlert { get => (Flags7 & FlagValue.Flag10) != 0; set { if (value) Flags7 |= FlagValue.Flag10; else Flags7 &= FlagValue.InvFlag10; } }
    public bool NoTarget { get => (Flags7 & FlagValue.Flag11) != 0; set { if (value) Flags7 |= FlagValue.Flag11; else Flags7 &= FlagValue.InvFlag11; } }
    public bool NoTargetSwitch { get => (Flags7 & FlagValue.Flag12) != 0; set { if (value) Flags7 |= FlagValue.Flag12; else Flags7 &= FlagValue.InvFlag12; } }
    public bool NotAutoaimed { get => (Flags7 & FlagValue.Flag13) != 0; set { if (value) Flags7 |= FlagValue.Flag13; else Flags7 &= FlagValue.InvFlag13; } }
    public bool NotDMatch { get => (Flags7 & FlagValue.Flag14) != 0; set { if (value) Flags7 |= FlagValue.Flag14; else Flags7 &= FlagValue.InvFlag14; } }
    public bool NoTelefrag { get => (Flags7 & FlagValue.Flag15) != 0; set { if (value) Flags7 |= FlagValue.Flag15; else Flags7 &= FlagValue.InvFlag15; } }
    public bool NoTeleOther { get => (Flags7 & FlagValue.Flag16) != 0; set { if (value) Flags7 |= FlagValue.Flag16; else Flags7 &= FlagValue.InvFlag16; } }
    public bool NoTeleport { get => (Flags7 & FlagValue.Flag17) != 0; set { if (value) Flags7 |= FlagValue.Flag17; else Flags7 &= FlagValue.InvFlag17; } }
    public bool NoTelestomp { get => (Flags7 & FlagValue.Flag18) != 0; set { if (value) Flags7 |= FlagValue.Flag18; else Flags7 &= FlagValue.InvFlag18; } }
    public bool NoTimeFreeze { get => (Flags7 & FlagValue.Flag19) != 0; set { if (value) Flags7 |= FlagValue.Flag19; else Flags7 &= FlagValue.InvFlag19; } }
    public bool NotOnAutomap { get => (Flags7 & FlagValue.Flag20) != 0; set { if (value) Flags7 |= FlagValue.Flag20; else Flags7 &= FlagValue.InvFlag20; } }
    public bool NoTrigger { get => (Flags7 & FlagValue.Flag21) != 0; set { if (value) Flags7 |= FlagValue.Flag21; else Flags7 &= FlagValue.InvFlag21; } }
    public bool NoVerticalMeleeRange { get => (Flags7 & FlagValue.Flag22) != 0; set { if (value) Flags7 |= FlagValue.Flag22; else Flags7 &= FlagValue.InvFlag22; } }
    public bool NoWallBounceSnd { get => (Flags7 & FlagValue.Flag23) != 0; set { if (value) Flags7 |= FlagValue.Flag23; else Flags7 &= FlagValue.InvFlag23; } }
    public bool OldRadiusDmg { get => (Flags7 & FlagValue.Flag24) != 0; set { if (value) Flags7 |= FlagValue.Flag24; else Flags7 &= FlagValue.InvFlag24; } }
    public bool Painless { get => (Flags7 & FlagValue.Flag25) != 0; set { if (value) Flags7 |= FlagValue.Flag25; else Flags7 &= FlagValue.InvFlag25; } }
    public bool Pickup { get => (Flags7 & FlagValue.Flag26) != 0; set { if (value) Flags7 |= FlagValue.Flag26; else Flags7 &= FlagValue.InvFlag26; } }
    public bool PierceArmor { get => (Flags7 & FlagValue.Flag27) != 0; set { if (value) Flags7 |= FlagValue.Flag27; else Flags7 &= FlagValue.InvFlag27; } }
    public bool PlayerPawnCanSuperMorph { get => (Flags7 & FlagValue.Flag28) != 0; set { if (value) Flags7 |= FlagValue.Flag28; else Flags7 &= FlagValue.InvFlag28; } }
    public bool PlayerPawnCrouchableMorph { get => (Flags7 & FlagValue.Flag29) != 0; set { if (value) Flags7 |= FlagValue.Flag29; else Flags7 &= FlagValue.InvFlag29; } }
    public bool PlayerPawnNoThrustWhenInvul { get => (Flags7 & FlagValue.Flag30) != 0; set { if (value) Flags7 |= FlagValue.Flag30; else Flags7 &= FlagValue.InvFlag30; } }
    public bool PoisonAlways { get => (Flags7 & FlagValue.Flag31) != 0; set { if (value) Flags7 |= FlagValue.Flag31; else Flags7 &= FlagValue.InvFlag31; } }
    public bool PuffGetsOwner { get => (Flags8 & FlagValue.Flag1) != 0; set { if (value) Flags8 |= FlagValue.Flag1; else Flags8 &= FlagValue.InvFlag1; } }
    public bool PuffOnActors { get => (Flags8 & FlagValue.Flag2) != 0; set { if (value) Flags8 |= FlagValue.Flag2; else Flags8 &= FlagValue.InvFlag2; } }
    public bool Pushable { get => (Flags8 & FlagValue.Flag3) != 0; set { if (value) Flags8 |= FlagValue.Flag3; else Flags8 &= FlagValue.InvFlag3; } }
    public bool QuarterGravity { get => (Flags8 & FlagValue.Flag4) != 0; set { if (value) Flags8 |= FlagValue.Flag4; else Flags8 &= FlagValue.InvFlag4; } }
    public bool QuickToRetaliate { get => (Flags8 & FlagValue.Flag5) != 0; set { if (value) Flags8 |= FlagValue.Flag5; else Flags8 &= FlagValue.InvFlag5; } }
    public bool Randomize { get => (Flags8 & FlagValue.Flag6) != 0; set { if (value) Flags8 |= FlagValue.Flag6; else Flags8 &= FlagValue.InvFlag6; } }
    public bool Reflective { get => (Flags8 & FlagValue.Flag7) != 0; set { if (value) Flags8 |= FlagValue.Flag7; else Flags8 &= FlagValue.InvFlag7; } }
    public bool RelativeToFloor { get => (Flags8 & FlagValue.Flag8) != 0; set { if (value) Flags8 |= FlagValue.Flag8; else Flags8 &= FlagValue.InvFlag8; } }
    public bool Ripper { get => (Flags8 & FlagValue.Flag9) != 0; set { if (value) Flags8 |= FlagValue.Flag9; else Flags8 &= FlagValue.InvFlag9; } }
    public bool RocketTrail { get => (Flags8 & FlagValue.Flag10) != 0; set { if (value) Flags8 |= FlagValue.Flag10; else Flags8 &= FlagValue.InvFlag10; } }
    public bool RollCenter { get => (Flags8 & FlagValue.Flag11) != 0; set { if (value) Flags8 |= FlagValue.Flag11; else Flags8 &= FlagValue.InvFlag11; } }
    public bool RollSprite { get => (Flags8 & FlagValue.Flag12) != 0; set { if (value) Flags8 |= FlagValue.Flag12; else Flags8 &= FlagValue.InvFlag12; } }
    public bool ScreenSeeker { get => (Flags8 & FlagValue.Flag13) != 0; set { if (value) Flags8 |= FlagValue.Flag13; else Flags8 &= FlagValue.InvFlag13; } }
    public bool SeeInvisible { get => (Flags8 & FlagValue.Flag14) != 0; set { if (value) Flags8 |= FlagValue.Flag14; else Flags8 &= FlagValue.InvFlag14; } }
    public bool SeekerMissile { get => (Flags8 & FlagValue.Flag15) != 0; set { if (value) Flags8 |= FlagValue.Flag15; else Flags8 &= FlagValue.InvFlag15; } }
    public bool SeesDaggers { get => (Flags8 & FlagValue.Flag16) != 0; set { if (value) Flags8 |= FlagValue.Flag16; else Flags8 &= FlagValue.InvFlag16; } }
    public bool Shadow { get => (Flags8 & FlagValue.Flag17) != 0; set { if (value) Flags8 |= FlagValue.Flag17; else Flags8 &= FlagValue.InvFlag17; } }
    public bool ShieldReflect { get => (Flags8 & FlagValue.Flag18) != 0; set { if (value) Flags8 |= FlagValue.Flag18; else Flags8 &= FlagValue.InvFlag18; } }
    public bool Shootable { get => (Flags8 & FlagValue.Flag19) != 0; set { if (value) Flags8 |= FlagValue.Flag19; else Flags8 &= FlagValue.InvFlag19; } }
    public bool ShortMissileRange { get => (Flags8 & FlagValue.Flag20) != 0; set { if (value) Flags8 |= FlagValue.Flag20; else Flags8 &= FlagValue.InvFlag20; } }
    public bool Skullfly { get => (Flags8 & FlagValue.Flag21) != 0; set { if (value) Flags8 |= FlagValue.Flag21; else Flags8 &= FlagValue.InvFlag21; } }
    public bool SkyExplode { get => (Flags8 & FlagValue.Flag22) != 0; set { if (value) Flags8 |= FlagValue.Flag22; else Flags8 &= FlagValue.InvFlag22; } }
    public bool SlidesOnWalls { get => (Flags8 & FlagValue.Flag23) != 0; set { if (value) Flags8 |= FlagValue.Flag23; else Flags8 &= FlagValue.InvFlag23; } }
    public bool Solid { get => (Flags8 & FlagValue.Flag24) != 0; set { if (value) Flags8 |= FlagValue.Flag24; else Flags8 &= FlagValue.InvFlag24; } }
    public bool SpawnCeiling { get => (Flags8 & FlagValue.Flag25) != 0; set { if (value) Flags8 |= FlagValue.Flag25; else Flags8 &= FlagValue.InvFlag25; } }
    public bool SpawnFloat { get => (Flags8 & FlagValue.Flag26) != 0; set { if (value) Flags8 |= FlagValue.Flag26; else Flags8 &= FlagValue.InvFlag26; } }
    public bool SpawnSoundSource { get => (Flags8 & FlagValue.Flag27) != 0; set { if (value) Flags8 |= FlagValue.Flag27; else Flags8 &= FlagValue.InvFlag27; } }
    public bool Special { get => (Flags8 & FlagValue.Flag28) != 0; set { if (value) Flags8 |= FlagValue.Flag28; else Flags8 &= FlagValue.InvFlag28; } }
    public bool SpecialFireDamage { get => (Flags8 & FlagValue.Flag29) != 0; set { if (value) Flags8 |= FlagValue.Flag29; else Flags8 &= FlagValue.InvFlag29; } }
    public bool SpecialFloorClip { get => (Flags8 & FlagValue.Flag30) != 0; set { if (value) Flags8 |= FlagValue.Flag30; else Flags8 &= FlagValue.InvFlag30; } }
    public bool Spectral { get => (Flags8 & FlagValue.Flag31) != 0; set { if (value) Flags8 |= FlagValue.Flag31; else Flags8 &= FlagValue.InvFlag31; } }
    public bool SpriteAngle { get => (Flags8 & FlagValue.Flag32) != 0; set { if (value) Flags8 |= FlagValue.Flag32; else Flags8 &= FlagValue.InvFlag32; } }
    public bool SpriteFlip { get => (Flags9 & FlagValue.Flag1) != 0; set { if (value) Flags9 |= FlagValue.Flag1; else Flags9 &= FlagValue.InvFlag1; } }
    public bool StandStill { get => (Flags9 & FlagValue.Flag2) != 0; set { if (value) Flags9 |= FlagValue.Flag2; else Flags9 &= FlagValue.InvFlag2; } }
    public bool StayMorphed { get => (Flags9 & FlagValue.Flag3) != 0; set { if (value) Flags9 |= FlagValue.Flag3; else Flags9 &= FlagValue.InvFlag3; } }
    public bool Stealth { get => (Flags9 & FlagValue.Flag4) != 0; set { if (value) Flags9 |= FlagValue.Flag4; else Flags9 &= FlagValue.InvFlag4; } }
    public bool StepMissile { get => (Flags9 & FlagValue.Flag5) != 0; set { if (value) Flags9 |= FlagValue.Flag5; else Flags9 &= FlagValue.InvFlag5; } }
    public bool StrifeDamage { get => (Flags9 & FlagValue.Flag6) != 0; set { if (value) Flags9 |= FlagValue.Flag6; else Flags9 &= FlagValue.InvFlag6; } }
    public bool SummonedMonster { get => (Flags9 & FlagValue.Flag7) != 0; set { if (value) Flags9 |= FlagValue.Flag7; else Flags9 &= FlagValue.InvFlag7; } }
    public bool Synchronized { get => (Flags9 & FlagValue.Flag8) != 0; set { if (value) Flags9 |= FlagValue.Flag8; else Flags9 &= FlagValue.InvFlag8; } }
    public bool Teleport { get => (Flags9 & FlagValue.Flag9) != 0; set { if (value) Flags9 |= FlagValue.Flag9; else Flags9 &= FlagValue.InvFlag9; } }
    public bool Telestomp { get => (Flags9 & FlagValue.Flag10) != 0; set { if (value) Flags9 |= FlagValue.Flag10; else Flags9 &= FlagValue.InvFlag10; } }
    public bool ThruActors { get => (Flags9 & FlagValue.Flag11) != 0; set { if (value) Flags9 |= FlagValue.Flag11; else Flags9 &= FlagValue.InvFlag11; } }
    public bool ThruGhost { get => (Flags9 & FlagValue.Flag12) != 0; set { if (value) Flags9 |= FlagValue.Flag12; else Flags9 &= FlagValue.InvFlag12; } }
    public bool ThruReflect { get => (Flags9 & FlagValue.Flag13) != 0; set { if (value) Flags9 |= FlagValue.Flag13; else Flags9 &= FlagValue.InvFlag13; } }
    public bool ThruSpecies { get => (Flags9 & FlagValue.Flag14) != 0; set { if (value) Flags9 |= FlagValue.Flag14; else Flags9 &= FlagValue.InvFlag14; } }
    public bool Touchy { get => (Flags9 & FlagValue.Flag15) != 0; set { if (value) Flags9 |= FlagValue.Flag15; else Flags9 &= FlagValue.InvFlag15; } }
    public bool UseBounceState { get => (Flags9 & FlagValue.Flag16) != 0; set { if (value) Flags9 |= FlagValue.Flag16; else Flags9 &= FlagValue.InvFlag16; } }
    public bool UseKillScripts { get => (Flags9 & FlagValue.Flag17) != 0; set { if (value) Flags9 |= FlagValue.Flag17; else Flags9 &= FlagValue.InvFlag17; } }
    public bool UseSpecial { get => (Flags9 & FlagValue.Flag18) != 0; set { if (value) Flags9 |= FlagValue.Flag18; else Flags9 &= FlagValue.InvFlag18; } }
    public bool VisibilityPulse { get => (Flags9 & FlagValue.Flag19) != 0; set { if (value) Flags9 |= FlagValue.Flag19; else Flags9 &= FlagValue.InvFlag19; } }
    public bool Vulnerable { get => (Flags9 & FlagValue.Flag20) != 0; set { if (value) Flags9 |= FlagValue.Flag20; else Flags9 &= FlagValue.InvFlag20; } }
    public bool WallSprite { get => (Flags9 & FlagValue.Flag21) != 0; set { if (value) Flags9 |= FlagValue.Flag21; else Flags9 &= FlagValue.InvFlag21; } }
    public bool WeaponAltAmmoOptional { get => (Flags9 & FlagValue.Flag22) != 0; set { if (value) Flags9 |= FlagValue.Flag22; else Flags9 &= FlagValue.InvFlag22; } }
    public bool WeaponAltUsesBoth { get => (Flags9 & FlagValue.Flag23) != 0; set { if (value) Flags9 |= FlagValue.Flag23; else Flags9 &= FlagValue.InvFlag23; } }
    public bool WeaponAmmoCheckBoth { get => (Flags9 & FlagValue.Flag24) != 0; set { if (value) Flags9 |= FlagValue.Flag24; else Flags9 &= FlagValue.InvFlag24; } }
    public bool WeaponAmmoOptional { get => (Flags9 & FlagValue.Flag25) != 0; set { if (value) Flags9 |= FlagValue.Flag25; else Flags9 &= FlagValue.InvFlag25; } }
    public bool WeaponAxeBlood { get => (Flags9 & FlagValue.Flag26) != 0; set { if (value) Flags9 |= FlagValue.Flag26; else Flags9 &= FlagValue.InvFlag26; } }
    public bool WeaponBfg { get => (Flags9 & FlagValue.Flag27) != 0; set { if (value) Flags9 |= FlagValue.Flag27; else Flags9 &= FlagValue.InvFlag27; } }
    public bool WeaponCheatNotWeapon { get => (Flags9 & FlagValue.Flag28) != 0; set { if (value) Flags9 |= FlagValue.Flag28; else Flags9 &= FlagValue.InvFlag28; } }
    public bool WeaponDontBob { get => (Flags9 & FlagValue.Flag29) != 0; set { if (value) Flags9 |= FlagValue.Flag29; else Flags9 &= FlagValue.InvFlag29; } }
    public bool WeaponExplosive { get => (Flags9 & FlagValue.Flag30) != 0; set { if (value) Flags9 |= FlagValue.Flag30; else Flags9 &= FlagValue.InvFlag30; } }
    public bool WeaponMeleeWeapon { get => (Flags9 & FlagValue.Flag31) != 0; set { if (value) Flags9 |= FlagValue.Flag31; else Flags9 &= FlagValue.InvFlag31; } }
    public bool WeaponNoAlert { get => (Flags9 & FlagValue.Flag32) != 0; set { if (value) Flags9 |= FlagValue.Flag32; else Flags9 &= FlagValue.InvFlag32; } }
    public bool WeaponNoAutoaim { get => (Flags10 & FlagValue.Flag1) != 0; set { if (value) Flags10 |= FlagValue.Flag1; else Flags10 &= FlagValue.InvFlag1; } }
    public bool WeaponNoAutofire { get => (Flags10 & FlagValue.Flag2) != 0; set { if (value) Flags10 |= FlagValue.Flag2; else Flags10 &= FlagValue.InvFlag2; } }
    public bool WeaponNoDeathDeselect { get => (Flags10 & FlagValue.Flag3) != 0; set { if (value) Flags10 |= FlagValue.Flag3; else Flags10 &= FlagValue.InvFlag3; } }
    public bool WeaponNoDeathInput { get => (Flags10 & FlagValue.Flag4) != 0; set { if (value) Flags10 |= FlagValue.Flag4; else Flags10 &= FlagValue.InvFlag4; } }
    public bool WeaponNoAutoSwitch { get => (Flags10 & FlagValue.Flag5) != 0; set { if (value) Flags10 |= FlagValue.Flag5; else Flags10 &= FlagValue.InvFlag5; } }
    public bool WeaponPoweredUp { get => (Flags10 & FlagValue.Flag6) != 0; set { if (value) Flags10 |= FlagValue.Flag6; else Flags10 &= FlagValue.InvFlag6; } }
    public bool WeaponPrimaryUsesBoth { get => (Flags10 & FlagValue.Flag7) != 0; set { if (value) Flags10 |= FlagValue.Flag7; else Flags10 &= FlagValue.InvFlag7; } }
    public bool WeaponReadySndHalf { get => (Flags10 & FlagValue.Flag8) != 0; set { if (value) Flags10 |= FlagValue.Flag8; else Flags10 &= FlagValue.InvFlag8; } }
    public bool WeaponStaff2Kickback { get => (Flags10 & FlagValue.Flag9) != 0; set { if (value) Flags10 |= FlagValue.Flag9; else Flags10 &= FlagValue.InvFlag9; } }
    public bool WeaponWimpyWeapon { get => (Flags10 & FlagValue.Flag10) != 0; set { if (value) Flags10 |= FlagValue.Flag10; else Flags10 &= FlagValue.InvFlag10; } }
    public bool WeaponSpawn { get => (Flags10 & FlagValue.Flag11) != 0; set { if (value) Flags10 |= FlagValue.Flag11; else Flags10 &= FlagValue.InvFlag11; } }
    public bool WindThrust { get => (Flags10 & FlagValue.Flag12) != 0; set { if (value) Flags10 |= FlagValue.Flag12; else Flags10 &= FlagValue.InvFlag12; } }
    public bool ZdoomTrans { get => (Flags10 & FlagValue.Flag13) != 0; set { if (value) Flags10 |= FlagValue.Flag13; else Flags10 &= FlagValue.InvFlag13; } }
    public bool BossSpawnShot { get => (Flags10 & FlagValue.Flag14) != 0; set { if (value) Flags10 |= FlagValue.Flag14; else Flags10 &= FlagValue.InvFlag14; } }
    public bool Map07Boss1 { get => (Flags10 & FlagValue.Flag15) != 0; set { if (value) Flags10 |= FlagValue.Flag15; else Flags10 &= FlagValue.InvFlag15; } }
    public bool Map07Boss2 { get => (Flags10 & FlagValue.Flag16) != 0; set { if (value) Flags10 |= FlagValue.Flag16; else Flags10 &= FlagValue.InvFlag16; } }
    public bool E1M8Boss { get => (Flags10 & FlagValue.Flag17) != 0; set { if (value) Flags10 |= FlagValue.Flag17; else Flags10 &= FlagValue.InvFlag17; } }
    public bool E2M8Boss { get => (Flags10 & FlagValue.Flag18) != 0; set { if (value) Flags10 |= FlagValue.Flag18; else Flags10 &= FlagValue.InvFlag18; } }
    public bool E3M8Boss { get => (Flags10 & FlagValue.Flag19) != 0; set { if (value) Flags10 |= FlagValue.Flag19; else Flags10 &= FlagValue.InvFlag19; } }
    public bool E4M6Boss { get => (Flags10 & FlagValue.Flag20) != 0; set { if (value) Flags10 |= FlagValue.Flag20; else Flags10 &= FlagValue.InvFlag20; } }
    public bool E4M8Boss { get => (Flags10 & FlagValue.Flag21) != 0; set { if (value) Flags10 |= FlagValue.Flag21; else Flags10 &= FlagValue.InvFlag21; } }
    public bool FullVolSee { get => (Flags10 & FlagValue.Flag22) != 0; set { if (value) Flags10 |= FlagValue.Flag22; else Flags10 &= FlagValue.InvFlag22; } }

    public EntityFlags(EntityFlagsModel model)
    {
        Flags1 = model.Bits[0];
        Flags2 = model.Bits[1];
        Flags3 = model.Bits[2];
        Flags4 = model.Bits[3];
        Flags5 = model.Bits[4];
        Flags6 = model.Bits[5];
        Flags7 = model.Bits[6];
        Flags8 = model.Bits[7];
        Flags9 = model.Bits[8];
        Flags10 = model.Bits[9];
    }

    public EntityFlagsModel ToEntityFlagsModel()
    {
        EntityFlagsModel entityFlagsModel = new EntityFlagsModel() { Bits = new int[10] };

        entityFlagsModel.Bits[0] = Flags1;
        entityFlagsModel.Bits[1] = Flags2;
        entityFlagsModel.Bits[2] = Flags3;
        entityFlagsModel.Bits[3] = Flags4;
        entityFlagsModel.Bits[4] = Flags5;
        entityFlagsModel.Bits[5] = Flags6;
        entityFlagsModel.Bits[6] = Flags7;
        entityFlagsModel.Bits[7] = Flags8;
        entityFlagsModel.Bits[8] = Flags9;
        entityFlagsModel.Bits[9] = Flags10;

        return entityFlagsModel;
    }

    public void ClearAll()
    {
        Flags1 = 0;
        Flags2 = 0;
        Flags3 = 0;
        Flags4 = 0;
        Flags5 = 0;
        Flags6 = 0;
        Flags7 = 0;
        Flags8 = 0;
        Flags9 = 0;
        Flags10 = 0;
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
            if (Flags4 != entityFlags.Flags4)
                return false;
            if (Flags5 != entityFlags.Flags5)
                return false;
            if (Flags6 != entityFlags.Flags6)
                return false;
            if (Flags7 != entityFlags.Flags7)
                return false;
            if (Flags8 != entityFlags.Flags8)
                return false;
            if (Flags9 != entityFlags.Flags9)
                return false;
            if (Flags10 != entityFlags.Flags10)
                return false;

            return true;
        }

        return false;
    }

    public override int GetHashCode() => Flags1 ^ Flags2 ^ Flags3 ^ Flags4 ^ Flags5 ^ Flags6 ^ Flags7 ^
        Flags8 ^ Flags9 ^ Flags10;
}
