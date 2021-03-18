using Helion.Models;
using System;

namespace Helion.World.Entities.Definition.Flags
{
    public struct EntityFlags
    {

        private const int Bits = 32;
        private const int ShiftBit = 1;

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
        private int Flags11;

        public bool AbsMaskAngle { get => this[EntityFlag.AbsMaskAngle]; set => this[EntityFlag.AbsMaskAngle] = value; }
        public bool AbsMaskPitch { get => this[EntityFlag.AbsMaskPitch]; set => this[EntityFlag.AbsMaskPitch] = value; }
        public bool ActivateImpact { get => this[EntityFlag.ActivateImpact]; set => this[EntityFlag.ActivateImpact] = value; }
        public bool ActivateMCross { get => this[EntityFlag.ActivateMCross]; set => this[EntityFlag.ActivateMCross] = value; }
        public bool ActivatePCross { get => this[EntityFlag.ActivatePCross]; set => this[EntityFlag.ActivatePCross] = value; }
        public bool ActLikeBridge { get => this[EntityFlag.ActLikeBridge]; set => this[EntityFlag.ActLikeBridge] = value; }
        public bool AdditivePoisonDamage { get => this[EntityFlag.AdditivePoisonDamage]; set => this[EntityFlag.AdditivePoisonDamage] = value; }
        public bool AdditivePoisonDuration { get => this[EntityFlag.AdditivePoisonDuration]; set => this[EntityFlag.AdditivePoisonDuration] = value; }
        public bool AimReflect { get => this[EntityFlag.AimReflect]; set => this[EntityFlag.AimReflect] = value; }
        public bool AllowBounceOnActors { get => this[EntityFlag.AllowBounceOnActors]; set => this[EntityFlag.AllowBounceOnActors] = value; }
        public bool AllowPain { get => this[EntityFlag.AllowPain]; set => this[EntityFlag.AllowPain] = value; }
        public bool AllowParticles { get => this[EntityFlag.AllowParticles]; set => this[EntityFlag.AllowParticles] = value; }
        public bool AllowThruFlags { get => this[EntityFlag.AllowThruFlags]; set => this[EntityFlag.AllowThruFlags] = value; }
        public bool AlwaysFast { get => this[EntityFlag.AlwaysFast]; set => this[EntityFlag.AlwaysFast] = value; }
        public bool AlwaysPuff { get => this[EntityFlag.AlwaysPuff]; set => this[EntityFlag.AlwaysPuff] = value; }
        public bool AlwaysRespawn { get => this[EntityFlag.AlwaysRespawn]; set => this[EntityFlag.AlwaysRespawn] = value; }
        public bool AlwaysTelefrag { get => this[EntityFlag.AlwaysTelefrag]; set => this[EntityFlag.AlwaysTelefrag] = value; }
        public bool Ambush { get => this[EntityFlag.Ambush]; set => this[EntityFlag.Ambush] = value; }
        public bool AvoidMelee { get => this[EntityFlag.AvoidMelee]; set => this[EntityFlag.AvoidMelee] = value; }
        public bool Blasted { get => this[EntityFlag.Blasted]; set => this[EntityFlag.Blasted] = value; }
        public bool BlockAsPlayer { get => this[EntityFlag.BlockAsPlayer]; set => this[EntityFlag.BlockAsPlayer] = value; }
        public bool BlockedBySolidActors { get => this[EntityFlag.BlockedBySolidActors]; set => this[EntityFlag.BlockedBySolidActors] = value; }
        public bool BloodlessImpact { get => this[EntityFlag.BloodlessImpact]; set => this[EntityFlag.BloodlessImpact] = value; }
        public bool BloodSplatter { get => this[EntityFlag.BloodSplatter]; set => this[EntityFlag.BloodSplatter] = value; }
        public bool Boss { get => this[EntityFlag.Boss]; set => this[EntityFlag.Boss] = value; }
        public bool BossDeath { get => this[EntityFlag.BossDeath]; set => this[EntityFlag.BossDeath] = value; }
        public bool BounceAutoOff { get => this[EntityFlag.BounceAutoOff]; set => this[EntityFlag.BounceAutoOff] = value; }
        public bool BounceAutoOffFloorOnly { get => this[EntityFlag.BounceAutoOffFloorOnly]; set => this[EntityFlag.BounceAutoOffFloorOnly] = value; }
        public bool BounceLikeHeretic { get => this[EntityFlag.BounceLikeHeretic]; set => this[EntityFlag.BounceLikeHeretic] = value; }
        public bool BounceOnActors { get => this[EntityFlag.BounceOnActors]; set => this[EntityFlag.BounceOnActors] = value; }
        public bool BounceOnCeilings { get => this[EntityFlag.BounceOnCeilings]; set => this[EntityFlag.BounceOnCeilings] = value; }
        public bool BounceOnFloors { get => this[EntityFlag.BounceOnFloors]; set => this[EntityFlag.BounceOnFloors] = value; }
        public bool BounceOnUnrippables { get => this[EntityFlag.BounceOnUnrippables]; set => this[EntityFlag.BounceOnUnrippables] = value; }
        public bool BounceOnWalls { get => this[EntityFlag.BounceOnWalls]; set => this[EntityFlag.BounceOnWalls] = value; }
        public bool Bright { get => this[EntityFlag.Bright]; set => this[EntityFlag.Bright] = value; }
        public bool Buddha { get => this[EntityFlag.Buddha]; set => this[EntityFlag.Buddha] = value; }
        public bool BumpSpecial { get => this[EntityFlag.BumpSpecial]; set => this[EntityFlag.BumpSpecial] = value; }
        public bool CanBlast { get => this[EntityFlag.CanBlast]; set => this[EntityFlag.CanBlast] = value; }
        public bool CanBounceWater { get => this[EntityFlag.CanBounceWater]; set => this[EntityFlag.CanBounceWater] = value; }
        public bool CannotPush { get => this[EntityFlag.CannotPush]; set => this[EntityFlag.CannotPush] = value; }
        public bool CanPass { get => this[EntityFlag.CanPass]; set => this[EntityFlag.CanPass] = value; }
        public bool CanPushWalls { get => this[EntityFlag.CanPushWalls]; set => this[EntityFlag.CanPushWalls] = value; }
        public bool CantLeaveFloorPic { get => this[EntityFlag.CantLeaveFloorPic]; set => this[EntityFlag.CantLeaveFloorPic] = value; }
        public bool CantSeek { get => this[EntityFlag.CantSeek]; set => this[EntityFlag.CantSeek] = value; }
        public bool CanUseWalls { get => this[EntityFlag.CanUseWalls]; set => this[EntityFlag.CanUseWalls] = value; }
        public bool CausePain { get => this[EntityFlag.CausePain]; set => this[EntityFlag.CausePain] = value; }
        public bool CeilingHugger { get => this[EntityFlag.CeilingHugger]; set => this[EntityFlag.CeilingHugger] = value; }
        public bool Corpse { get => this[EntityFlag.Corpse]; set => this[EntityFlag.Corpse] = value; }
        public bool CountItem { get => this[EntityFlag.CountItem]; set => this[EntityFlag.CountItem] = value; }
        public bool CountKill { get => this[EntityFlag.CountKill]; set => this[EntityFlag.CountKill] = value; }
        public bool CountSecret { get => this[EntityFlag.CountSecret]; set => this[EntityFlag.CountSecret] = value; }
        public bool Deflect { get => this[EntityFlag.Deflect]; set => this[EntityFlag.Deflect] = value; }
        public bool DehExplosion { get => this[EntityFlag.DehExplosion]; set => this[EntityFlag.DehExplosion] = value; }
        public bool DoHarmSpecies { get => this[EntityFlag.DoHarmSpecies]; set => this[EntityFlag.DoHarmSpecies] = value; }
        public bool DontBlast { get => this[EntityFlag.DontBlast]; set => this[EntityFlag.DontBlast] = value; }
        public bool DontBounceOnShootables { get => this[EntityFlag.DontBounceOnShootables]; set => this[EntityFlag.DontBounceOnShootables] = value; }
        public bool DontBounceOnSky { get => this[EntityFlag.DontBounceOnSky]; set => this[EntityFlag.DontBounceOnSky] = value; }
        public bool DontCorpse { get => this[EntityFlag.DontCorpse]; set => this[EntityFlag.DontCorpse] = value; }
        public bool DontDrain { get => this[EntityFlag.DontDrain]; set => this[EntityFlag.DontDrain] = value; }
        public bool DontFaceTalker { get => this[EntityFlag.DontFaceTalker]; set => this[EntityFlag.DontFaceTalker] = value; }
        public bool DontFall { get => this[EntityFlag.DontFall]; set => this[EntityFlag.DontFall] = value; }
        public bool DontGib { get => this[EntityFlag.DontGib]; set => this[EntityFlag.DontGib] = value; }
        public bool DontHarmClass { get => this[EntityFlag.DontHarmClass]; set => this[EntityFlag.DontHarmClass] = value; }
        public bool DontHarmSpecies { get => this[EntityFlag.DontHarmSpecies]; set => this[EntityFlag.DontHarmSpecies] = value; }
        public bool DontHurtSpecies { get => this[EntityFlag.DontHurtSpecies]; set => this[EntityFlag.DontHurtSpecies] = value; }
        public bool DontInterpolate { get => this[EntityFlag.DontInterpolate]; set => this[EntityFlag.DontInterpolate] = value; }
        public bool DontMorph { get => this[EntityFlag.DontMorph]; set => this[EntityFlag.DontMorph] = value; }
        public bool DontOverlap { get => this[EntityFlag.DontOverlap]; set => this[EntityFlag.DontOverlap] = value; }
        public bool DontReflect { get => this[EntityFlag.DontReflect]; set => this[EntityFlag.DontReflect] = value; }
        public bool DontRip { get => this[EntityFlag.DontRip]; set => this[EntityFlag.DontRip] = value; }
        public bool DontSeekInvisible { get => this[EntityFlag.DontSeekInvisible]; set => this[EntityFlag.DontSeekInvisible] = value; }
        public bool DontSplash { get => this[EntityFlag.DontSplash]; set => this[EntityFlag.DontSplash] = value; }
        public bool DontSquash { get => this[EntityFlag.DontSquash]; set => this[EntityFlag.DontSquash] = value; }
        public bool DontThrust { get => this[EntityFlag.DontThrust]; set => this[EntityFlag.DontThrust] = value; }
        public bool DontTranslate { get => this[EntityFlag.DontTranslate]; set => this[EntityFlag.DontTranslate] = value; }
        public bool DoomBounce { get => this[EntityFlag.DoomBounce]; set => this[EntityFlag.DoomBounce] = value; }
        public bool Dormant { get => this[EntityFlag.Dormant]; set => this[EntityFlag.Dormant] = value; }
        public bool Dropoff { get => this[EntityFlag.Dropoff]; set => this[EntityFlag.Dropoff] = value; }
        public bool Dropped { get => this[EntityFlag.Dropped]; set => this[EntityFlag.Dropped] = value; }
        public bool ExploCount { get => this[EntityFlag.ExploCount]; set => this[EntityFlag.ExploCount] = value; }
        public bool ExplodeOnWater { get => this[EntityFlag.ExplodeOnWater]; set => this[EntityFlag.ExplodeOnWater] = value; }
        public bool ExtremeDeath { get => this[EntityFlag.ExtremeDeath]; set => this[EntityFlag.ExtremeDeath] = value; }
        public bool Faster { get => this[EntityFlag.Faster]; set => this[EntityFlag.Faster] = value; }
        public bool FastMelee { get => this[EntityFlag.FastMelee]; set => this[EntityFlag.FastMelee] = value; }
        public bool FireDamage { get => this[EntityFlag.FireDamage]; set => this[EntityFlag.FireDamage] = value; }
        public bool FireResist { get => this[EntityFlag.FireResist]; set => this[EntityFlag.FireResist] = value; }
        public bool FixMapThingPos { get => this[EntityFlag.FixMapThingPos]; set => this[EntityFlag.FixMapThingPos] = value; }
        public bool FlatSprite { get => this[EntityFlag.FlatSprite]; set => this[EntityFlag.FlatSprite] = value; }
        public bool Float { get => this[EntityFlag.Float]; set => this[EntityFlag.Float] = value; }
        public bool FloatBob { get => this[EntityFlag.FloatBob]; set => this[EntityFlag.FloatBob] = value; }
        public bool FloorClip { get => this[EntityFlag.FloorClip]; set => this[EntityFlag.FloorClip] = value; }
        public bool FloorHugger { get => this[EntityFlag.FloorHugger]; set => this[EntityFlag.FloorHugger] = value; }
        public bool FoilBuddha { get => this[EntityFlag.FoilBuddha]; set => this[EntityFlag.FoilBuddha] = value; }
        public bool FoilInvul { get => this[EntityFlag.FoilInvul]; set => this[EntityFlag.FoilInvul] = value; }
        public bool ForceDecal { get => this[EntityFlag.ForceDecal]; set => this[EntityFlag.ForceDecal] = value; }
        public bool ForceInFighting { get => this[EntityFlag.ForceInFighting]; set => this[EntityFlag.ForceInFighting] = value; }
        public bool ForcePain { get => this[EntityFlag.ForcePain]; set => this[EntityFlag.ForcePain] = value; }
        public bool ForceRadiusDmg { get => this[EntityFlag.ForceRadiusDmg]; set => this[EntityFlag.ForceRadiusDmg] = value; }
        public bool ForceXYBillboard { get => this[EntityFlag.ForceXYBillboard]; set => this[EntityFlag.ForceXYBillboard] = value; }
        public bool ForceYBillboard { get => this[EntityFlag.ForceYBillboard]; set => this[EntityFlag.ForceYBillboard] = value; }
        public bool ForceZeroRadiusDmg { get => this[EntityFlag.ForceZeroRadiusDmg]; set => this[EntityFlag.ForceZeroRadiusDmg] = value; }
        public bool Friendly { get => this[EntityFlag.Friendly]; set => this[EntityFlag.Friendly] = value; }
        public bool Frightened { get => this[EntityFlag.Frightened]; set => this[EntityFlag.Frightened] = value; }
        public bool Frightening { get => this[EntityFlag.Frightening]; set => this[EntityFlag.Frightening] = value; }
        public bool FullVolActive { get => this[EntityFlag.FullVolActive]; set => this[EntityFlag.FullVolActive] = value; }
        public bool FullVolDeath { get => this[EntityFlag.FullVolDeath]; set => this[EntityFlag.FullVolDeath] = value; }
        public bool GetOwner { get => this[EntityFlag.GetOwner]; set => this[EntityFlag.GetOwner] = value; }
        public bool Ghost { get => this[EntityFlag.Ghost]; set => this[EntityFlag.Ghost] = value; }
        public bool GrenadeTrail { get => this[EntityFlag.GrenadeTrail]; set => this[EntityFlag.GrenadeTrail] = value; }
        public bool HarmFriends { get => this[EntityFlag.HarmFriends]; set => this[EntityFlag.HarmFriends] = value; }
        public bool HereticBounce { get => this[EntityFlag.HereticBounce]; set => this[EntityFlag.HereticBounce] = value; }
        public bool HexenBounce { get => this[EntityFlag.HexenBounce]; set => this[EntityFlag.HexenBounce] = value; }
        public bool HitMaster { get => this[EntityFlag.HitMaster]; set => this[EntityFlag.HitMaster] = value; }
        public bool HitOwner { get => this[EntityFlag.HitOwner]; set => this[EntityFlag.HitOwner] = value; }
        public bool HitTarget { get => this[EntityFlag.HitTarget]; set => this[EntityFlag.HitTarget] = value; }
        public bool HitTracer { get => this[EntityFlag.HitTracer]; set => this[EntityFlag.HitTracer] = value; }
        public bool IceCorpse { get => this[EntityFlag.IceCorpse]; set => this[EntityFlag.IceCorpse] = value; }
        public bool IceDamage { get => this[EntityFlag.IceDamage]; set => this[EntityFlag.IceDamage] = value; }
        public bool IceShatter { get => this[EntityFlag.IceShatter]; set => this[EntityFlag.IceShatter] = value; }
        public bool InCombat { get => this[EntityFlag.InCombat]; set => this[EntityFlag.InCombat] = value; }
        public bool InterpolateAngles { get => this[EntityFlag.InterpolateAngles]; set => this[EntityFlag.InterpolateAngles] = value; }
        public bool InventoryAdditiveTime { get => this[EntityFlag.InventoryAdditiveTime]; set => this[EntityFlag.InventoryAdditiveTime] = value; }
        public bool InventoryAlwaysPickup { get => this[EntityFlag.InventoryAlwaysPickup]; set => this[EntityFlag.InventoryAlwaysPickup] = value; }
        public bool InventoryAlwaysRespawn { get => this[EntityFlag.InventoryAlwaysRespawn]; set => this[EntityFlag.InventoryAlwaysRespawn] = value; }
        public bool InventoryAutoActivate { get => this[EntityFlag.InventoryAutoActivate]; set => this[EntityFlag.InventoryAutoActivate] = value; }
        public bool InventoryBigPowerup { get => this[EntityFlag.InventoryBigPowerup]; set => this[EntityFlag.InventoryBigPowerup] = value; }
        public bool InventoryFancyPickupSound { get => this[EntityFlag.InventoryFancyPickupSound]; set => this[EntityFlag.InventoryFancyPickupSound] = value; }
        public bool InventoryHubPower { get => this[EntityFlag.InventoryHubPower]; set => this[EntityFlag.InventoryHubPower] = value; }
        public bool InventoryIgnoreSkill { get => this[EntityFlag.InventoryIgnoreSkill]; set => this[EntityFlag.InventoryIgnoreSkill] = value; }
        public bool InventoryInterHubStrip { get => this[EntityFlag.InventoryInterHubStrip]; set => this[EntityFlag.InventoryInterHubStrip] = value; }
        public bool InventoryInvbar { get => this[EntityFlag.InventoryInvbar]; set => this[EntityFlag.InventoryInvbar] = value; }
        public bool InventoryIsArmor { get => this[EntityFlag.InventoryIsArmor]; set => this[EntityFlag.InventoryIsArmor] = value; }
        public bool InventoryIsHealth { get => this[EntityFlag.InventoryIsHealth]; set => this[EntityFlag.InventoryIsHealth] = value; }
        public bool InventoryKeepDepleted { get => this[EntityFlag.InventoryKeepDepleted]; set => this[EntityFlag.InventoryKeepDepleted] = value; }
        public bool InventoryNeverRespawn { get => this[EntityFlag.InventoryNeverRespawn]; set => this[EntityFlag.InventoryNeverRespawn] = value; }
        public bool InventoryNoAttenPickupSound { get => this[EntityFlag.InventoryNoAttenPickupSound]; set => this[EntityFlag.InventoryNoAttenPickupSound] = value; }
        public bool InventoryNoScreenBlink { get => this[EntityFlag.InventoryNoScreenBlink]; set => this[EntityFlag.InventoryNoScreenBlink] = value; }
        public bool InventoryNoScreenFlash { get => this[EntityFlag.InventoryNoScreenFlash]; set => this[EntityFlag.InventoryNoScreenFlash] = value; }
        public bool InventoryNoTeleportFreeze { get => this[EntityFlag.InventoryNoTeleportFreeze]; set => this[EntityFlag.InventoryNoTeleportFreeze] = value; }
        public bool InventoryPersistentPower { get => this[EntityFlag.InventoryPersistentPower]; set => this[EntityFlag.InventoryPersistentPower] = value; }
        public bool InventoryPickupFlash { get => this[EntityFlag.InventoryPickupFlash]; set => this[EntityFlag.InventoryPickupFlash] = value; }
        public bool InventoryQuiet { get => this[EntityFlag.InventoryQuiet]; set => this[EntityFlag.InventoryQuiet] = value; }
        public bool InventoryRestrictAbsolutely { get => this[EntityFlag.InventoryRestrictAbsolutely]; set => this[EntityFlag.InventoryRestrictAbsolutely] = value; }
        public bool InventoryTossed { get => this[EntityFlag.InventoryTossed]; set => this[EntityFlag.InventoryTossed] = value; }
        public bool InventoryTransfer { get => this[EntityFlag.InventoryTransfer]; set => this[EntityFlag.InventoryTransfer] = value; }
        public bool InventoryUnclearable { get => this[EntityFlag.InventoryUnclearable]; set => this[EntityFlag.InventoryUnclearable] = value; }
        public bool InventoryUndroppable { get => this[EntityFlag.InventoryUndroppable]; set => this[EntityFlag.InventoryUndroppable] = value; }
        public bool InventoryUntossable { get => this[EntityFlag.InventoryUntossable]; set => this[EntityFlag.InventoryUntossable] = value; }
        public bool Invisible { get => this[EntityFlag.Invisible]; set => this[EntityFlag.Invisible] = value; }
        public bool Invulnerable { get => this[EntityFlag.Invulnerable]; set => this[EntityFlag.Invulnerable] = value; }
        public bool IsMonster { get => this[EntityFlag.IsMonster]; set => this[EntityFlag.IsMonster] = value; }
        public bool IsTeleportSpot { get => this[EntityFlag.IsTeleportSpot]; set => this[EntityFlag.IsTeleportSpot] = value; }
        public bool JumpDown { get => this[EntityFlag.JumpDown]; set => this[EntityFlag.JumpDown] = value; }
        public bool JustAttacked { get => this[EntityFlag.JustAttacked]; set => this[EntityFlag.JustAttacked] = value; }
        public bool JustHit { get => this[EntityFlag.JustHit]; set => this[EntityFlag.JustHit] = value; }
        public bool LaxTeleFragDmg { get => this[EntityFlag.LaxTeleFragDmg]; set => this[EntityFlag.LaxTeleFragDmg] = value; }
        public bool LongMeleeRange { get => this[EntityFlag.LongMeleeRange]; set => this[EntityFlag.LongMeleeRange] = value; }
        public bool LookAllAround { get => this[EntityFlag.LookAllAround]; set => this[EntityFlag.LookAllAround] = value; }
        public bool LowGravity { get => this[EntityFlag.LowGravity]; set => this[EntityFlag.LowGravity] = value; }
        public bool MaskRotation { get => this[EntityFlag.MaskRotation]; set => this[EntityFlag.MaskRotation] = value; }
        public bool MbfBouncer { get => this[EntityFlag.MbfBouncer]; set => this[EntityFlag.MbfBouncer] = value; }
        public bool MirrorReflect { get => this[EntityFlag.MirrorReflect]; set => this[EntityFlag.MirrorReflect] = value; }
        public bool Missile { get => this[EntityFlag.Missile]; set => this[EntityFlag.Missile] = value; }
        public bool MissileEvenMore { get => this[EntityFlag.MissileEvenMore]; set => this[EntityFlag.MissileEvenMore] = value; }
        public bool MissileMore { get => this[EntityFlag.MissileMore]; set => this[EntityFlag.MissileMore] = value; }
        public bool Monster { get => this[EntityFlag.Monster]; set => this[EntityFlag.Monster] = value; }
        public bool MoveWithSector { get => this[EntityFlag.MoveWithSector]; set => this[EntityFlag.MoveWithSector] = value; }
        public bool MThruSpecies { get => this[EntityFlag.MThruSpecies]; set => this[EntityFlag.MThruSpecies] = value; }
        public bool NeverFast { get => this[EntityFlag.NeverFast]; set => this[EntityFlag.NeverFast] = value; }
        public bool NeverRespawn { get => this[EntityFlag.NeverRespawn]; set => this[EntityFlag.NeverRespawn] = value; }
        public bool NeverTarget { get => this[EntityFlag.NeverTarget]; set => this[EntityFlag.NeverTarget] = value; }
        public bool NoBlockmap { get => this[EntityFlag.NoBlockmap]; set => this[EntityFlag.NoBlockmap] = value; }
        public bool NoBlockMonst { get => this[EntityFlag.NoBlockMonst]; set => this[EntityFlag.NoBlockMonst] = value; }
        public bool NoBlood { get => this[EntityFlag.NoBlood]; set => this[EntityFlag.NoBlood] = value; }
        public bool NoBloodDecals { get => this[EntityFlag.NoBloodDecals]; set => this[EntityFlag.NoBloodDecals] = value; }
        public bool NoBossRip { get => this[EntityFlag.NoBossRip]; set => this[EntityFlag.NoBossRip] = value; }
        public bool NoBounceSound { get => this[EntityFlag.NoBounceSound]; set => this[EntityFlag.NoBounceSound] = value; }
        public bool NoClip { get => this[EntityFlag.NoClip]; set => this[EntityFlag.NoClip] = value; }
        public bool NoDamage { get => this[EntityFlag.NoDamage]; set => this[EntityFlag.NoDamage] = value; }
        public bool NoDamageThrust { get => this[EntityFlag.NoDamageThrust]; set => this[EntityFlag.NoDamageThrust] = value; }
        public bool NoDecal { get => this[EntityFlag.NoDecal]; set => this[EntityFlag.NoDecal] = value; }
        public bool NoDropoff { get => this[EntityFlag.NoDropoff]; set => this[EntityFlag.NoDropoff] = value; }
        public bool NoExplodeFloor { get => this[EntityFlag.NoExplodeFloor]; set => this[EntityFlag.NoExplodeFloor] = value; }
        public bool NoExtremeDeath { get => this[EntityFlag.NoExtremeDeath]; set => this[EntityFlag.NoExtremeDeath] = value; }
        public bool NoFear { get => this[EntityFlag.NoFear]; set => this[EntityFlag.NoFear] = value; }
        public bool NoFriction { get => this[EntityFlag.NoFriction]; set => this[EntityFlag.NoFriction] = value; }
        public bool NoFrictionBounce { get => this[EntityFlag.NoFrictionBounce]; set => this[EntityFlag.NoFrictionBounce] = value; }
        public bool NoForwardFall { get => this[EntityFlag.NoForwardFall]; set => this[EntityFlag.NoForwardFall] = value; }
        public bool NoGravity { get => this[EntityFlag.NoGravity]; set => this[EntityFlag.NoGravity] = value; }
        public bool NoIceDeath { get => this[EntityFlag.NoIceDeath]; set => this[EntityFlag.NoIceDeath] = value; }
        public bool NoInfighting { get => this[EntityFlag.NoInfighting]; set => this[EntityFlag.NoInfighting] = value; }
        public bool NoInfightSpecies { get => this[EntityFlag.NoInfightSpecies]; set => this[EntityFlag.NoInfightSpecies] = value; }
        public bool NoInteraction { get => this[EntityFlag.NoInteraction]; set => this[EntityFlag.NoInteraction] = value; }
        public bool NoKillScripts { get => this[EntityFlag.NoKillScripts]; set => this[EntityFlag.NoKillScripts] = value; }
        public bool NoLiftDrop { get => this[EntityFlag.NoLiftDrop]; set => this[EntityFlag.NoLiftDrop] = value; }
        public bool NoMenu { get => this[EntityFlag.NoMenu]; set => this[EntityFlag.NoMenu] = value; }
        public bool NonShootable { get => this[EntityFlag.NonShootable]; set => this[EntityFlag.NonShootable] = value; }
        public bool NoPain { get => this[EntityFlag.NoPain]; set => this[EntityFlag.NoPain] = value; }
        public bool NoRadiusDmg { get => this[EntityFlag.NoRadiusDmg]; set => this[EntityFlag.NoRadiusDmg] = value; }
        public bool NoSector { get => this[EntityFlag.NoSector]; set => this[EntityFlag.NoSector] = value; }
        public bool NoSkin { get => this[EntityFlag.NoSkin]; set => this[EntityFlag.NoSkin] = value; }
        public bool NoSplashAlert { get => this[EntityFlag.NoSplashAlert]; set => this[EntityFlag.NoSplashAlert] = value; }
        public bool NoTarget { get => this[EntityFlag.NoTarget]; set => this[EntityFlag.NoTarget] = value; }
        public bool NoTargetSwitch { get => this[EntityFlag.NoTargetSwitch]; set => this[EntityFlag.NoTargetSwitch] = value; }
        public bool NotAutoaimed { get => this[EntityFlag.NotAutoaimed]; set => this[EntityFlag.NotAutoaimed] = value; }
        public bool NotDMatch { get => this[EntityFlag.NotDMatch]; set => this[EntityFlag.NotDMatch] = value; }
        public bool NoTelefrag { get => this[EntityFlag.NoTelefrag]; set => this[EntityFlag.NoTelefrag] = value; }
        public bool NoTeleOther { get => this[EntityFlag.NoTeleOther]; set => this[EntityFlag.NoTeleOther] = value; }
        public bool NoTeleport { get => this[EntityFlag.NoTeleport]; set => this[EntityFlag.NoTeleport] = value; }
        public bool NoTelestomp { get => this[EntityFlag.NoTelestomp]; set => this[EntityFlag.NoTelestomp] = value; }
        public bool NoTimeFreeze { get => this[EntityFlag.NoTimeFreeze]; set => this[EntityFlag.NoTimeFreeze] = value; }
        public bool NotOnAutomap { get => this[EntityFlag.NotOnAutomap]; set => this[EntityFlag.NotOnAutomap] = value; }
        public bool NoTrigger { get => this[EntityFlag.NoTrigger]; set => this[EntityFlag.NoTrigger] = value; }
        public bool NoVerticalMeleeRange { get => this[EntityFlag.NoVerticalMeleeRange]; set => this[EntityFlag.NoVerticalMeleeRange] = value; }
        public bool NoWallBounceSnd { get => this[EntityFlag.NoWallBounceSnd]; set => this[EntityFlag.NoWallBounceSnd] = value; }
        public bool OldRadiusDmg { get => this[EntityFlag.OldRadiusDmg]; set => this[EntityFlag.OldRadiusDmg] = value; }
        public bool Painless { get => this[EntityFlag.Painless]; set => this[EntityFlag.Painless] = value; }
        public bool Pickup { get => this[EntityFlag.Pickup]; set => this[EntityFlag.Pickup] = value; }
        public bool PierceArmor { get => this[EntityFlag.PierceArmor]; set => this[EntityFlag.PierceArmor] = value; }
        public bool PlayerPawnCanSuperMorph { get => this[EntityFlag.PlayerPawnCanSuperMorph]; set => this[EntityFlag.PlayerPawnCanSuperMorph] = value; }
        public bool PlayerPawnCrouchableMorph { get => this[EntityFlag.PlayerPawnCrouchableMorph]; set => this[EntityFlag.PlayerPawnCrouchableMorph] = value; }
        public bool PlayerPawnNoThrustWhenInvul { get => this[EntityFlag.PlayerPawnNoThrustWhenInvul]; set => this[EntityFlag.PlayerPawnNoThrustWhenInvul] = value; }
        public bool PoisonAlways { get => this[EntityFlag.PoisonAlways]; set => this[EntityFlag.PoisonAlways] = value; }
        public bool Projectile { get => this[EntityFlag.Projectile]; set => this[EntityFlag.Projectile] = value; }
        public bool PuffGetsOwner { get => this[EntityFlag.PuffGetsOwner]; set => this[EntityFlag.PuffGetsOwner] = value; }
        public bool PuffOnActors { get => this[EntityFlag.PuffOnActors]; set => this[EntityFlag.PuffOnActors] = value; }
        public bool Pushable { get => this[EntityFlag.Pushable]; set => this[EntityFlag.Pushable] = value; }
        public bool QuarterGravity { get => this[EntityFlag.QuarterGravity]; set => this[EntityFlag.QuarterGravity] = value; }
        public bool QuickToRetaliate { get => this[EntityFlag.QuickToRetaliate]; set => this[EntityFlag.QuickToRetaliate] = value; }
        public bool Randomize { get => this[EntityFlag.Randomize]; set => this[EntityFlag.Randomize] = value; }
        public bool Reflective { get => this[EntityFlag.Reflective]; set => this[EntityFlag.Reflective] = value; }
        public bool RelativeToFloor { get => this[EntityFlag.RelativeToFloor]; set => this[EntityFlag.RelativeToFloor] = value; }
        public bool Ripper { get => this[EntityFlag.Ripper]; set => this[EntityFlag.Ripper] = value; }
        public bool RocketTrail { get => this[EntityFlag.RocketTrail]; set => this[EntityFlag.RocketTrail] = value; }
        public bool RollCenter { get => this[EntityFlag.RollCenter]; set => this[EntityFlag.RollCenter] = value; }
        public bool RollSprite { get => this[EntityFlag.RollSprite]; set => this[EntityFlag.RollSprite] = value; }
        public bool ScreenSeeker { get => this[EntityFlag.ScreenSeeker]; set => this[EntityFlag.ScreenSeeker] = value; }
        public bool SeeInvisible { get => this[EntityFlag.SeeInvisible]; set => this[EntityFlag.SeeInvisible] = value; }
        public bool SeekerMissile { get => this[EntityFlag.SeekerMissile]; set => this[EntityFlag.SeekerMissile] = value; }
        public bool SeesDaggers { get => this[EntityFlag.SeesDaggers]; set => this[EntityFlag.SeesDaggers] = value; }
        public bool Shadow { get => this[EntityFlag.Shadow]; set => this[EntityFlag.Shadow] = value; }
        public bool ShieldReflect { get => this[EntityFlag.ShieldReflect]; set => this[EntityFlag.ShieldReflect] = value; }
        public bool Shootable { get => this[EntityFlag.Shootable]; set => this[EntityFlag.Shootable] = value; }
        public bool ShortMissileRange { get => this[EntityFlag.ShortMissileRange]; set => this[EntityFlag.ShortMissileRange] = value; }
        public bool Skullfly { get => this[EntityFlag.Skullfly]; set => this[EntityFlag.Skullfly] = value; }
        public bool SkyExplode { get => this[EntityFlag.SkyExplode]; set => this[EntityFlag.SkyExplode] = value; }
        public bool SlidesOnWalls { get => this[EntityFlag.SlidesOnWalls]; set => this[EntityFlag.SlidesOnWalls] = value; }
        public bool Solid {  get => this[EntityFlag.Solid]; set => this[EntityFlag.Solid] = value; }
        public bool SpawnCeiling { get => this[EntityFlag.SpawnCeiling]; set => this[EntityFlag.SpawnCeiling] = value; }
        public bool SpawnFloat { get => this[EntityFlag.SpawnFloat]; set => this[EntityFlag.SpawnFloat] = value; }
        public bool SpawnSoundSource { get => this[EntityFlag.SpawnSoundSource]; set => this[EntityFlag.SpawnSoundSource] = value; }
        public bool Special { get => this[EntityFlag.Special]; set => this[EntityFlag.Special] = value; }
        public bool SpecialFireDamage { get => this[EntityFlag.SpecialFireDamage]; set => this[EntityFlag.SpecialFireDamage] = value; }
        public bool SpecialFloorClip { get => this[EntityFlag.SpecialFloorClip]; set => this[EntityFlag.SpecialFloorClip] = value; }
        public bool Spectral { get => this[EntityFlag.Spectral]; set => this[EntityFlag.Spectral] = value; }
        public bool SpriteAngle { get => this[EntityFlag.SpriteAngle]; set => this[EntityFlag.SpriteAngle] = value; }
        public bool SpriteFlip { get => this[EntityFlag.SpriteFlip]; set => this[EntityFlag.SpriteFlip] = value; }
        public bool StandStill { get => this[EntityFlag.StandStill]; set => this[EntityFlag.StandStill] = value; }
        public bool StayMorphed { get => this[EntityFlag.StayMorphed]; set => this[EntityFlag.StayMorphed] = value; }
        public bool Stealth { get => this[EntityFlag.Stealth]; set => this[EntityFlag.Stealth] = value; }
        public bool StepMissile { get => this[EntityFlag.StepMissile]; set => this[EntityFlag.StepMissile] = value; }
        public bool StrifeDamage { get => this[EntityFlag.StrifeDamage]; set => this[EntityFlag.StrifeDamage] = value; }
        public bool SummonedMonster { get => this[EntityFlag.SummonedMonster]; set => this[EntityFlag.SummonedMonster] = value; }
        public bool Synchronized { get => this[EntityFlag.Synchronized]; set => this[EntityFlag.Synchronized] = value; }
        public bool Teleport { get => this[EntityFlag.Teleport]; set => this[EntityFlag.Teleport] = value; }
        public bool Telestomp { get => this[EntityFlag.Telestomp]; set => this[EntityFlag.Telestomp] = value; }
        public bool ThruActors { get => this[EntityFlag.ThruActors]; set => this[EntityFlag.ThruActors] = value; }
        public bool ThruGhost { get => this[EntityFlag.ThruGhost]; set => this[EntityFlag.ThruGhost] = value; }
        public bool ThruReflect { get => this[EntityFlag.ThruReflect]; set => this[EntityFlag.ThruReflect] = value; }
        public bool ThruSpecies { get => this[EntityFlag.ThruSpecies]; set => this[EntityFlag.ThruSpecies] = value; }
        public bool Touchy { get => this[EntityFlag.Touchy]; set => this[EntityFlag.Touchy] = value; }
        public bool UseBounceState { get => this[EntityFlag.UseBounceState]; set => this[EntityFlag.UseBounceState] = value; }
        public bool UseKillScripts { get => this[EntityFlag.UseKillScripts]; set => this[EntityFlag.UseKillScripts] = value; }
        public bool UseSpecial { get => this[EntityFlag.UseSpecial]; set => this[EntityFlag.UseSpecial] = value; }
        public bool VisibilityPulse { get => this[EntityFlag.VisibilityPulse]; set => this[EntityFlag.VisibilityPulse] = value; }
        public bool Vulnerable { get => this[EntityFlag.Vulnerable]; set => this[EntityFlag.Vulnerable] = value; }
        public bool WallSprite { get => this[EntityFlag.WallSprite]; set => this[EntityFlag.WallSprite] = value; }
        public bool WeaponAltAmmoOptional { get => this[EntityFlag.WeaponAltAmmoOptional]; set => this[EntityFlag.WeaponAltAmmoOptional] = value; }
        public bool WeaponAltUsesBoth { get => this[EntityFlag.WeaponAltUsesBoth]; set => this[EntityFlag.WeaponAltUsesBoth] = value; }
        public bool WeaponAmmoCheckBoth { get => this[EntityFlag.WeaponAmmoCheckBoth]; set => this[EntityFlag.WeaponAmmoCheckBoth] = value; }
        public bool WeaponAmmoOptional { get => this[EntityFlag.WeaponAmmoOptional]; set => this[EntityFlag.WeaponAmmoOptional] = value; }
        public bool WeaponAxeBlood { get => this[EntityFlag.WeaponAxeBlood]; set => this[EntityFlag.WeaponAxeBlood] = value; }
        public bool WeaponBfg { get => this[EntityFlag.WeaponBfg]; set => this[EntityFlag.WeaponBfg] = value; }
        public bool WeaponCheatNotWeapon { get => this[EntityFlag.WeaponCheatNotWeapon]; set => this[EntityFlag.WeaponCheatNotWeapon] = value; }
        public bool WeaponDontBob { get => this[EntityFlag.WeaponDontBob]; set => this[EntityFlag.WeaponDontBob] = value; }
        public bool WeaponExplosive { get => this[EntityFlag.WeaponExplosive]; set => this[EntityFlag.WeaponExplosive] = value; }
        public bool WeaponMeleeWeapon { get => this[EntityFlag.WeaponMeleeWeapon]; set => this[EntityFlag.WeaponMeleeWeapon] = value; }
        public bool WeaponNoAlert { get => this[EntityFlag.WeaponNoAlert]; set => this[EntityFlag.WeaponNoAlert] = value; }
        public bool WeaponNoAutoaim { get => this[EntityFlag.WeaponNoAutoaim]; set => this[EntityFlag.WeaponNoAutoaim] = value; }
        public bool WeaponNoAutofire { get => this[EntityFlag.WeaponNoAutofire]; set => this[EntityFlag.WeaponNoAutofire] = value; }
        public bool WeaponNoDeathDeselect { get => this[EntityFlag.WeaponNoDeathDeselect]; set => this[EntityFlag.WeaponNoDeathDeselect] = value; }
        public bool WeaponNoDeathInput { get => this[EntityFlag.WeaponNoDeathInput]; set => this[EntityFlag.WeaponNoDeathInput] = value; }
        public bool WeaponNoAutoSwitch { get => this[EntityFlag.WeaponNoAutoSwitch]; set => this[EntityFlag.WeaponNoAutoSwitch] = value; }
        public bool WeaponPoweredUp { get => this[EntityFlag.WeaponPoweredUp]; set => this[EntityFlag.WeaponPoweredUp] = value; }
        public bool WeaponPrimaryUsesBoth { get => this[EntityFlag.WeaponPrimaryUsesBoth]; set => this[EntityFlag.WeaponPrimaryUsesBoth] = value; }
        public bool WeaponReadySndHalf { get => this[EntityFlag.WeaponReadySndHalf]; set => this[EntityFlag.WeaponReadySndHalf] = value; }
        public bool WeaponStaff2Kickback { get => this[EntityFlag.WeaponStaff2Kickback]; set => this[EntityFlag.WeaponStaff2Kickback] = value; }
        public bool WeaponWimpyWeapon { get => this[EntityFlag.WeaponWimpyWeapon]; set => this[EntityFlag.WeaponWimpyWeapon] = value; }
        public bool WeaponSpawn { get => this[EntityFlag.WeaponSpawn]; set => this[EntityFlag.WeaponSpawn] = value; }
        public bool WindThrust { get => this[EntityFlag.WindThrust]; set => this[EntityFlag.WindThrust] = value; }
        public bool ZdoomTrans { get => this[EntityFlag.ZdoomTrans]; set => this[EntityFlag.ZdoomTrans] = value; }

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
            Flags11 = model.Bits[10];
        }

        public EntityFlagsModel ToEntityFlagsModel()
        {
            EntityFlagsModel entityFlagsModel = new EntityFlagsModel()
            {
                Bits = new int[NumFlags]
            };

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
            entityFlagsModel.Bits[10] = Flags11;

            return entityFlagsModel;
        }

        public bool this[EntityFlag flag]
        {
            get
            {
                int iFlag = (int)flag;
                int index = iFlag / Bits;

                return GetValue(index, ShiftBit << (iFlag - (index * Bits)));
                //return (Flags[index] & (ShiftBit << (iFlag - (index * Bits)))) != 0;
            }

            set
            {
                int iFlag = (int)flag;
                int index = iFlag / Bits;

                if (value)
                    SetTrue(index, (ShiftBit << (iFlag - (index * Bits))));
                else
                    SetFalse(index, (ShiftBit << (iFlag - (index * Bits))));
                //    Flags[index] |= (ShiftBit << (iFlag - (index * Bits)));
                //else
                //    Flags[index] &= ~(ShiftBit << (iFlag - (index * Bits)));
            }
        }

        private void SetTrue(int index, int value)
        {
            switch (index)
            {
                case 0:
                    Flags1 |= value;
                    break;
                case 1:
                    Flags2 |= value;
                    break;
                case 2:
                    Flags3 |= value;
                    break;
                case 3:
                    Flags4 |= value;
                    break;
                case 4:
                    Flags5 |= value;
                    break;
                case 5:
                    Flags6 |= value;
                    break;
                case 6:
                    Flags7 |= value;
                    break;
                case 7:
                    Flags8 |= value;
                    break;
                case 8:
                    Flags9 |= value;
                    break;
                case 9:
                    Flags10 |= value;
                    break;
                case 10:
                    Flags11 |= value;
                    break;
            }
        }

        private void SetFalse(int index, int value)
        {
            switch (index)
            {
                case 0:
                    Flags1 &= ~value;
                    break;
                case 1:
                    Flags2 &= ~value;
                    break;
                case 2:
                    Flags3 &= ~value;
                    break;
                case 3:
                    Flags4 &= ~value;
                    break;
                case 4:
                    Flags5 &= ~value;
                    break;
                case 5:
                    Flags6 &= ~value;
                    break;
                case 6:
                    Flags7 &= ~value;
                    break;
                case 7:
                    Flags8 &= ~value;
                    break;
                case 8:
                    Flags9 &= ~value;
                    break;
                case 9:
                    Flags10 &= ~value;
                    break;
                case 10:
                    Flags11 &= ~value;
                    break;
            }
        }

        private bool GetValue(int index, int value)
        {
            switch (index)
            {
                case 0:
                    return (Flags1 & value) != 0;
                case 1:
                    return (Flags2 & value) != 0;
                case 2:
                    return (Flags3 & value) != 0;
                case 3:
                    return (Flags4 & value) != 0;
                case 4:
                    return (Flags5 & value) != 0;
                case 5:
                    return (Flags6 & value) != 0;
                case 6:
                    return (Flags7 & value) != 0;
                case 7:
                    return (Flags8 & value) != 0;
                case 8:
                    return (Flags9 & value) != 0;
                case 9:
                    return (Flags10 & value) != 0;
                case 10:
                    return (Flags11 & value) != 0;
            }

            return false;
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
            Flags11 = 0;
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
                if (Flags11 != entityFlags.Flags11)
                    return false;

                return true;
            }

            return false;
        }
    }
}