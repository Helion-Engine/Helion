using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Audio;
using Helion.Dehacked;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Special;
using NLog;

namespace Helion.World.Entities.Definition.States;

public static class EntityActionFunctions
{
    public delegate void ActionFunction(Entity entity);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly Dictionary<string, ActionFunction> ActionFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ACS_NAMEDEXECUTE"] = ACS_NamedExecute,
        ["ACS_NAMEDEXECUTEALWAYS"] = ACS_NamedExecuteAlways,
        ["ACS_NAMEDEXECUTEWITHRESULT"] = ACS_NamedExecuteWithResult,
        ["ACS_NAMEDLOCKEDEXECUTE"] = ACS_NamedLockedExecute,
        ["ACS_NAMEDLOCKEDEXECUTEDOOR"] = ACS_NamedLockedExecuteDoor,
        ["ACS_NAMEDSUSPEND"] = ACS_NamedSuspend,
        ["ACS_NAMEDTERMINATE"] = ACS_NamedTerminate,
        ["A_ACTIVEANDUNBLOCK"] = A_ActiveAndUnblock,
        ["A_ACTIVESOUND"] = A_ActiveSound,
        ["A_ALERTMONSTERS"] = A_AlertMonsters,
        ["A_BFGSOUND"] = A_BFGSound,
        ["A_BFGSPRAY"] = A_BFGSpray,
        ["A_BABYMETAL"] = A_BabyMetal,
        ["A_BARRELDESTROY"] = A_BarrelDestroy,
        ["A_BASICATTACK"] = A_BasicAttack,
        ["A_BETASKULLATTACK"] = A_BetaSkullAttack,
        ["A_BISHOPMISSILEWEAVE"] = A_BishopMissileWeave,
        ["A_BOSSDEATH"] = A_BossDeath,
        ["A_BRAINAWAKE"] = A_BrainAwake,
        ["A_BRAINDIE"] = A_BrainDie,
        ["A_BRAINEXPLODE"] = A_BrainExplode,
        ["A_BRAINPAIN"] = A_BrainPain,
        ["A_BRAINSCREAM"] = A_BrainScream,
        ["A_BRAINSPIT"] = A_BrainSpit,
        ["A_BRUISATTACK"] = A_BruisAttack,
        ["A_BSPIATTACK"] = A_BspiAttack,
        ["A_BULLETATTACK"] = A_BulletAttack,
        ["A_BURST"] = A_Burst,
        ["A_CPOSATTACK"] = A_CPosAttack,
        ["A_CPOSREFIRE"] = A_CPosRefire,
        ["A_CSTAFFMISSILESLITHER"] = A_CStaffMissileSlither,
        ["A_CENTAURDEFEND"] = A_CentaurDefend,
        ["A_CHANGECOUNTFLAGS"] = A_ChangeCountFlags,
        ["A_CHANGEFLAG"] = A_ChangeFlag,
        ["A_CHANGEVELOCITY"] = A_ChangeVelocity,
        ["A_CHASE"] = A_Chase,
        ["A_CHECKBLOCK"] = A_CheckBlock,
        ["A_CHECKCEILING"] = A_CheckCeiling,
        ["A_CHECKFLAG"] = A_CheckFlag,
        ["A_CHECKFLOOR"] = A_CheckFloor,
        ["A_CHECKFORRELOAD"] = A_CheckForReload,
        ["A_CHECKFORRESURRECTION"] = A_CheckForResurrection,
        ["A_CHECKLOF"] = A_CheckLOF,
        ["A_CHECKPLAYERDONE"] = A_CheckPlayerDone,
        ["A_CHECKPROXIMITY"] = A_CheckProximity,
        ["A_CHECKRANGE"] = A_CheckRange,
        ["A_CHECKRELOAD"] = A_CheckReload,
        ["A_CHECKSIGHT"] = A_CheckSight,
        ["A_CHECKSIGHTORRANGE"] = A_CheckSightOrRange,
        ["A_CHECKSPECIES"] = A_CheckSpecies,
        ["A_CHECKTERRAIN"] = A_CheckTerrain,
        ["A_CLEARLASTHEARD"] = A_ClearLastHeard,
        ["A_CLEAROVERLAYS"] = A_ClearOverlays,
        ["A_CLEARREFIRE"] = A_ClearReFire,
        ["A_CLEARSHADOW"] = A_ClearShadow,
        ["A_CLEARSOUNDTARGET"] = A_ClearSoundTarget,
        ["A_CLEARTARGET"] = A_ClearTarget,
        ["A_CLOSESHOTGUN2"] = A_CloseShotgun2,
        ["A_COMBOATTACK"] = A_ComboAttack,
        ["A_COPYFRIENDLINESS"] = A_CopyFriendliness,
        ["A_COPYSPRITEFRAME"] = A_CopySpriteFrame,
        ["A_COUNTDOWN"] = A_Countdown,
        ["A_COUNTDOWNARG"] = A_CountdownArg,
        ["A_CUSTOMBULLETATTACK"] = A_CustomBulletAttack,
        ["A_CUSTOMCOMBOATTACK"] = A_CustomComboAttack,
        ["A_CUSTOMMELEEATTACK"] = A_CustomMeleeAttack,
        ["A_CUSTOMMISSILE"] = A_CustomMissile,
        ["A_CUSTOMPUNCH"] = A_CustomPunch,
        ["A_CUSTOMRAILGUN"] = A_CustomRailgun,
        ["A_CYBERATTACK"] = A_CyberAttack,
        ["A_DAMAGECHILDREN"] = A_DamageChildren,
        ["A_DAMAGEMASTER"] = A_DamageMaster,
        ["A_DAMAGESELF"] = A_DamageSelf,
        ["A_DAMAGESIBLINGS"] = A_DamageSiblings,
        ["A_DAMAGETARGET"] = A_DamageTarget,
        ["A_DAMAGETRACER"] = A_DamageTracer,
        ["A_DEQUEUECORPSE"] = A_DeQueueCorpse,
        ["A_DETONATE"] = A_Detonate,
        ["A_DIE"] = A_Die,
        ["A_DROPINVENTORY"] = A_DropInventory,
        ["A_DROPITEM"] = A_DropItem,
        ["A_DUALPAINATTACK"] = A_DualPainAttack,
        ["A_EXPLODE"] = A_Explode,
        ["A_EXTCHASE"] = A_ExtChase,
        ["A_FLOOPACTIVESOUND"] = A_FLoopActiveSound,
        ["A_FACEMASTER"] = A_FaceMaster,
        ["A_FACEMOVEMENTDIRECTION"] = A_FaceMovementDirection,
        ["A_FACETARGET"] = A_FaceTarget,
        ["A_FACETRACER"] = A_FaceTracer,
        ["A_FACETRACER"] = A_FaceTracer,
        ["A_FADEIN"] = A_FadeIn,
        ["A_FADEOUT"] = A_FadeOut,
        ["A_FADETO"] = A_FadeTo,
        ["A_FALL"] = A_Fall,
        ["A_FASTCHASE"] = A_FastChase,
        ["A_FATATTACK1"] = A_FatAttack1,
        ["A_FATATTACK2"] = A_FatAttack2,
        ["A_FATATTACK3"] = A_FatAttack3,
        ["A_FATRAISE"] = A_FatRaise,
        ["A_FIRE"] = A_Fire,
        ["A_FIREASSAULTGUN"] = A_FireAssaultGun,
        ["A_FIREBFG"] = A_FireBFG,
        ["A_FIREBULLETS"] = A_FireBullets,
        ["A_FIRECGUN"] = A_FireCGun,
        ["A_FIRECRACKLE"] = A_FireCrackle,
        ["A_FIRECUSTOMMISSILE"] = A_FireCustomMissile,
        ["A_FIREMISSILE"] = A_FireMissile,
        ["A_FIREOLDBFG"] = A_FireOldBFG,
        ["A_FIREPISTOL"] = A_FirePistol,
        ["A_FIREPLASMA"] = A_FirePlasma,
        ["A_FIREPROJECTILE"] = A_FireProjectile,
        ["A_FIRESTGRENADE"] = A_FireSTGrenade,
        ["A_FIRESHOTGUN"] = A_FireShotgun,
        ["A_FIRESHOTGUN2"] = A_FireShotgun2,
        ["A_FREEZEDEATH"] = A_FreezeDeath,
        ["A_FREEZEDEATHCHUNKS"] = A_FreezeDeathChunks,
        ["A_GENERICFREEZEDEATH"] = A_GenericFreezeDeath,
        ["A_GETHURT"] = A_GetHurt,
        ["A_GIVEINVENTORY"] = A_GiveInventory,
        ["A_GIVETOCHILDREN"] = A_GiveToChildren,
        ["A_GIVETOSIBLINGS"] = A_GiveToSiblings,
        ["A_GIVETOTARGET"] = A_GiveToTarget,
        ["A_GRAVITY"] = A_Gravity,
        ["A_GUNFLASH"] = A_GunFlash,
        ["A_HEADATTACK"] = A_HeadAttack,
        ["A_HIDETHING"] = A_HideThing,
        ["A_HOOF"] = A_Hoof,
        ["A_ICEGUYDIE"] = A_IceGuyDie,
        ["A_JUMP"] = A_Jump,
        ["A_JUMPIF"] = A_JumpIf,
        ["A_JUMPIFARMORTYPE"] = A_JumpIfArmorType,
        ["A_JUMPIFCLOSER"] = A_JumpIfCloser,
        ["A_JUMPIFHEALTHLOWER"] = A_JumpIfHealthLower,
        ["A_JUMPIFHIGHERORLOWER"] = A_JumpIfHigherOrLower,
        ["A_JUMPIFINTARGETINVENTORY"] = A_JumpIfInTargetInventory,
        ["A_JUMPIFINTARGETLOS"] = A_JumpIfInTargetLOS,
        ["A_JUMPIFINVENTORY"] = A_JumpIfInventory,
        ["A_JUMPIFMASTERCLOSER"] = A_JumpIfMasterCloser,
        ["A_JUMPIFNOAMMO"] = A_JumpIfNoAmmo,
        ["A_JUMPIFTARGETINLOS"] = A_JumpIfTargetInLOS,
        ["A_JUMPIFTARGETINSIDEMELEERANGE"] = A_JumpIfTargetInsideMeleeRange,
        ["A_JUMPIFTARGETOUTSIDEMELEERANGE"] = A_JumpIfTargetOutsideMeleeRange,
        ["A_JUMPIFTRACERCLOSER"] = A_JumpIfTracerCloser,
        ["A_KEENDIE"] = A_KeenDie,
        ["A_KILLCHILDREN"] = A_KillChildren,
        ["A_KILLMASTER"] = A_KillMaster,
        ["A_KILLSIBLINGS"] = A_KillSiblings,
        ["A_KILLTARGET"] = A_KillTarget,
        ["A_KILLTRACER"] = A_KillTracer,
        ["A_KLAXONBLARE"] = A_KlaxonBlare,
        ["A_LIGHT"] = A_Light,
        ["A_LIGHT0"] = A_Light0,
        ["A_LIGHT1"] = A_Light1,
        ["A_LIGHT2"] = A_Light2,
        ["A_LIGHTINVERSE"] = A_LightInverse,
        ["A_LOADSHOTGUN2"] = A_LoadShotgun2,
        ["A_LOG"] = A_Log,
        ["A_LOGFLOAT"] = A_LogFloat,
        ["A_LOGINT"] = A_LogInt,
        ["A_LOOK"] = A_Look,
        ["A_LOOK2"] = A_Look2,
        ["A_LOOKEX"] = A_LookEx,
        ["A_LOOPACTIVESOUND"] = A_LoopActiveSound,
        ["A_LOWGRAVITY"] = A_LowGravity,
        ["A_LOWER"] = A_Lower,
        ["A_M_SAW"] = A_M_Saw,
        ["A_MELEEATTACK"] = A_MeleeAttack,
        ["A_METAL"] = A_Metal,
        ["A_MISSILEATTACK"] = A_MissileAttack,
        ["A_MONSTERRAIL"] = A_MonsterRail,
        ["A_MONSTERREFIRE"] = A_MonsterRefire,
        ["A_MORPH"] = A_Morph,
        ["A_MUSHROOM"] = A_Mushroom,
        ["A_NOBLOCKING"] = A_NoBlocking,
        ["A_NOGRAVITY"] = A_NoGravity,
        ["A_OPENSHOTGUN2"] = A_OpenShotgun2,
        ["A_OVERLAY"] = A_Overlay,
        ["A_OVERLAYALPHA"] = A_OverlayAlpha,
        ["A_OVERLAYFLAGS"] = A_OverlayFlags,
        ["A_OVERLAYOFFSET"] = A_OverlayOffset,
        ["A_OVERLAYRENDERSTYLE"] = A_OverlayRenderstyle,
        ["A_PAIN"] = A_Pain,
        ["A_PAINATTACK"] = A_PainAttack,
        ["A_PAINDIE"] = A_PainDie,
        ["A_PLAYSOUND"] = A_PlaySound,
        ["A_PLAYSOUNDEX"] = A_PlaySoundEx,
        ["A_PLAYWEAPONSOUND"] = A_PlayWeaponSound,
        ["A_PLAYERSCREAM"] = A_PlayerScream,
        ["A_PLAYERSKINCHECK"] = A_PlayerSkinCheck,
        ["A_POSATTACK"] = A_PosAttack,
        ["A_PRINT"] = A_Print,
        ["A_PRINTBOLD"] = A_PrintBold,
        ["A_PUNCH"] = A_Punch,
        ["A_QUAKE"] = A_Quake,
        ["A_QUAKEEX"] = A_QuakeEx,
        ["A_QUEUECORPSE"] = A_QueueCorpse,
        ["A_RADIUSDAMAGESELF"] = A_RadiusDamageSelf,
        ["A_RADIUSGIVE"] = A_RadiusGive,
        ["A_RADIUSTHRUST"] = A_RadiusThrust,
        ["A_RAILATTACK"] = A_RailAttack,
        ["A_RAISE"] = A_Raise,
        ["A_RAISECHILDREN"] = A_RaiseChildren,
        ["A_RAISEMASTER"] = A_RaiseMaster,
        ["A_RAISESELF"] = A_RaiseSelf,
        ["A_RAISESIBLINGS"] = A_RaiseSiblings,
        ["A_REFIRE"] = A_ReFire,
        ["A_REARRANGEPOINTERS"] = A_RearrangePointers,
        ["A_RECOIL"] = A_Recoil,
        ["A_REMOVE"] = A_Remove,
        ["A_REMOVECHILDREN"] = A_RemoveChildren,
        ["A_REMOVEMASTER"] = A_RemoveMaster,
        ["A_REMOVESIBLINGS"] = A_RemoveSiblings,
        ["A_REMOVETARGET"] = A_RemoveTarget,
        ["A_REMOVETRACER"] = A_RemoveTracer,
        ["A_RESETHEALTH"] = A_ResetHealth,
        ["A_RESETRELOADCOUNTER"] = A_ResetReloadCounter,
        ["A_RESPAWN"] = A_Respawn,
        ["A_SPOSATTACK"] = A_SPosAttack,
        ["A_SARGATTACK"] = A_SargAttack,
        ["A_SAW"] = A_Saw,
        ["A_SCALEVELOCITY"] = A_ScaleVelocity,
        ["A_SCREAM"] = A_Scream,
        ["A_SCREAMANDUNBLOCK"] = A_ScreamAndUnblock,
        ["A_SEEKERMISSILE"] = A_SeekerMissile,
        ["A_SELECTWEAPON"] = A_SelectWeapon,
        ["A_SENTINELBOB"] = A_SentinelBob,
        ["A_SENTINELREFIRE"] = A_SentinelRefire,
        ["A_SETANGLE"] = A_SetAngle,
        ["A_SETARG"] = A_SetArg,
        ["A_SETBLEND"] = A_SetBlend,
        ["A_SETCHASETHRESHOLD"] = A_SetChaseThreshold,
        ["A_SETCROSSHAIR"] = A_SetCrosshair,
        ["A_SETDAMAGETYPE"] = A_SetDamageType,
        ["A_SETFLOAT"] = A_SetFloat,
        ["A_SETFLOATBOBPHASE"] = A_SetFloatBobPhase,
        ["A_SETFLOATSPEED"] = A_SetFloatSpeed,
        ["A_SETFLOORCLIP"] = A_SetFloorClip,
        ["A_SETGRAVITY"] = A_SetGravity,
        ["A_SETHEALTH"] = A_SetHealth,
        ["A_SETINVENTORY"] = A_SetInventory,
        ["A_SETINVULNERABLE"] = A_SetInvulnerable,
        ["A_SETMASS"] = A_SetMass,
        ["A_SETMUGSHOTSTATE"] = A_SetMugshotState,
        ["A_SETPAINTHRESHOLD"] = A_SetPainThreshold,
        ["A_SETPITCH"] = A_SetPitch,
        ["A_SETREFLECTIVE"] = A_SetReflective,
        ["A_SETREFLECTIVEINVULNERABLE"] = A_SetReflectiveInvulnerable,
        ["A_SETRENDERSTYLE"] = A_SetRenderStyle,
        ["A_SETRIPMAX"] = A_SetRipMax,
        ["A_SETRIPMIN"] = A_SetRipMin,
        ["A_SETRIPPERLEVEL"] = A_SetRipperLevel,
        ["A_SETROLL"] = A_SetRoll,
        ["A_SETSCALE"] = A_SetScale,
        ["A_SETSHADOW"] = A_SetShadow,
        ["A_SETSHOOTABLE"] = A_SetShootable,
        ["A_SETSIZE"] = A_SetSize,
        ["A_SETSOLID"] = A_SetSolid,
        ["A_SETSPECIAL"] = A_SetSpecial,
        ["A_SETSPECIES"] = A_SetSpecies,
        ["A_SETSPEED"] = A_SetSpeed,
        ["A_SETSPRITEANGLE"] = A_SetSpriteAngle,
        ["A_SETSPRITEROTATION"] = A_SetSpriteRotation,
        ["A_SETTELEFOG"] = A_SetTeleFog,
        ["A_SETTICS"] = A_SetTics,
        ["A_SETTRANSLATION"] = A_SetTranslation,
        ["A_SETTRANSLUCENT"] = A_SetTranslucent,
        ["A_SETUSERARRAY"] = A_SetUserArray,
        ["A_SETUSERARRAYFLOAT"] = A_SetUserArrayFloat,
        ["A_SETUSERVAR"] = A_SetUserVar,
        ["A_SETUSERVARFLOAT"] = A_SetUserVarFloat,
        ["A_SETVISIBLEROTATION"] = A_SetVisibleRotation,
        ["A_SKELFIST"] = A_SkelFist,
        ["A_SKELMISSILE"] = A_SkelMissile,
        ["A_SKELWHOOSH"] = A_SkelWhoosh,
        ["A_SKULLATTACK"] = A_SkullAttack,
        ["A_SKULLPOP"] = A_SkullPop,
        ["A_SOUNDPITCH"] = A_SoundPitch,
        ["A_SOUNDVOLUME"] = A_SoundVolume,
        ["A_SPAWNDEBRIS"] = A_SpawnDebris,
        ["A_SPAWNFLY"] = A_SpawnFly,
        ["A_SPAWNITEM"] = A_SpawnItem,
        ["A_SPAWNITEMEX"] = A_SpawnItemEx,
        ["A_SPAWNPARTICLE"] = A_SpawnParticle,
        ["A_SPAWNPROJECTILE"] = A_SpawnProjectile,
        ["A_SPAWNSOUND"] = A_SpawnSound,
        ["A_SPIDREFIRE"] = A_SpidRefire,
        ["A_SPOSATTACKUSEATKSOUND"] = A_SPosAttackUseAtkSound,
        ["A_SPRAYDECAL"] = A_SprayDecal,
        ["A_STARTFIRE"] = A_StartFire,
        ["A_STOP"] = A_Stop,
        ["A_STOPSOUND"] = A_StopSound,
        ["A_STOPSOUNDEX"] = A_StopSoundEx,
        ["A_SWAPTELEFOG"] = A_SwapTeleFog,
        ["A_TAKEFROMCHILDREN"] = A_TakeFromChildren,
        ["A_TAKEFROMSIBLINGS"] = A_TakeFromSiblings,
        ["A_TAKEFROMTARGET"] = A_TakeFromTarget,
        ["A_TAKEINVENTORY"] = A_TakeInventory,
        ["A_TELEPORT"] = A_Teleport,
        ["A_THROWGRENADE"] = A_ThrowGrenade,
        ["A_TOSSGIB"] = A_TossGib,
        ["A_TRACER"] = A_Tracer,
        ["A_TRACER2"] = A_Tracer2,
        ["A_TRANSFERPOINTER"] = A_TransferPointer,
        ["A_TROOPATTACK"] = A_TroopAttack,
        ["A_TURRETLOOK"] = A_TurretLook,
        ["A_UNHIDETHING"] = A_UnHideThing,
        ["A_UNSETFLOORCLIP"] = A_UnSetFloorClip,
        ["A_UNSETINVULNERABLE"] = A_UnSetInvulnerable,
        ["A_UNSETREFLECTIVE"] = A_UnSetReflective,
        ["A_UNSETREFLECTIVEINVULNERABLE"] = A_UnSetReflectiveInvulnerable,
        ["A_UNSETSHOOTABLE"] = A_UnSetShootable,
        ["A_UNSETFLOAT"] = A_UnsetFloat,
        ["A_UNSETSOLID"] = A_UnsetSolid,
        ["A_VILEATTACK"] = A_VileAttack,
        ["A_VILECHASE"] = A_VileChase,
        ["A_VILESTART"] = A_VileStart,
        ["A_VILETARGET"] = A_VileTarget,
        ["A_WANDER"] = A_Wander,
        ["A_WARP"] = A_Warp,
        ["A_WEAPONOFFSET"] = A_WeaponOffset,
        ["A_WEAPONREADY"] = A_WeaponReady,
        ["A_WEAVE"] = A_Weave,
        ["A_WOLFATTACK"] = A_WolfAttack,
        ["A_XSCREAM"] = A_XScream,
        ["A_ZOOMFACTOR"] = A_ZoomFactor,
        ["HEALTHING"] = HealThing,
        ["A_RandomJump"] = A_RandomJump,
        ["A_LineEffect"] = A_LineEffect,
        ["A_Spawn"] = A_Spawn,
        ["A_Face"] = A_Face,
        ["A_Turn"] = A_Turn,
        ["A_Scratch"] = A_Scratch,
        ["A_WeaponProjectile"] = A_WeaponProjectile,
        ["A_WeaponBulletAttack"] = A_WeaponBulletAttack,
        ["A_WeaponMeleeAttack"] = A_WeaponMeleeAttack,
        ["A_WeaponSound"] = A_WeaponSound,
        ["A_WeaponJump"] = A_WeaponJump,
        ["A_ConsumeAmmo"] = A_ConsumeAmmo,
        ["A_CheckAmmo"] = A_CheckAmmo,
        ["A_RefireTo"] = A_RefireTo,
        ["A_GunFlashTo"] = A_GunFlashTo,
        ["A_WeaponAlert"] = A_WeaponAlert,
        ["A_SpawnObject"] = A_SpawnObject,
        ["A_MonsterProjectile"] = A_MonsterProjectile,
        ["A_MonsterBulletAttack"] = A_MonsterBulletAttack,
        ["A_MonsterMeleeAttack"] = A_MonsterMeleeAttack,
        ["A_RadiusDamage"] = A_RadiusDamage,
        ["A_NoiseAlert"] = A_NoiseAlert,
        ["A_HealChase"] = A_HealChase,
        ["A_JumpIfTargetCloser"] = A_JumpIfTargetCloser,
        ["A_AddFlags"] = A_AddFlags,
        ["A_RemoveFlags"] = A_RemoveFlags,
        ["A_SeekTracer"] = A_SeekTracer,
        ["A_FindTracer"] = A_FindTracer,
        ["A_ClearTracer"] = A_ClearTracer,
        ["A_JumpIfHealthBelow"] = A_JumpIfHealthBelow,
        ["A_JumpIfTargetInSight"] = A_JumpIfTargetInSight,
        ["A_JumpIfTracerInSight"] = A_JumpIfTracerInSight,
        ["A_JumpIfFlagsSet"] = A_JumpIfFlagsSet,
        ["A_FireRailGun"] = A_FireRailGun
    };

    public static ActionFunction? Find(string? actionFuncName)
    {
         if (actionFuncName != null)
         {
              if (ActionFunctions.TryGetValue(actionFuncName, out ActionFunction? func))
                   return func;
              Log.Warn("Unable to find action function: {0}", actionFuncName);
         }

         return null;
    }

    private static void ACS_NamedExecute(Entity entity)
    {
         // TODO
    }

    private static void ACS_NamedExecuteAlways(Entity entity)
    {
         // TODO
    }

    private static void ACS_NamedExecuteWithResult(Entity entity)
    {
         // TODO
    }

    private static void ACS_NamedLockedExecute(Entity entity)
    {
         // TODO
    }

    private static void ACS_NamedLockedExecuteDoor(Entity entity)
    {
         // TODO
    }

    private static void ACS_NamedSuspend(Entity entity)
    {
         // TODO
    }

    private static void ACS_NamedTerminate(Entity entity)
    {
         // TODO
    }

    private static void A_ActiveAndUnblock(Entity entity)
    {
         // TODO
    }

    private static void A_ActiveSound(Entity entity)
    {
         // TODO
    }

    private static void A_AlertMonsters(Entity entity)
    {
         // TODO
    }

    private static void A_BFGSound(Entity entity)
    {
        if (entity.IsPlayer)
            WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/bfgf", new SoundParams(entity, channel: entity.WeaponSoundChannel));
    }

    private static void A_BFGSpray(Entity entity)
    {
        if (entity.Owner.Entity == null)
            return;

        for (int i = 0; i < 40; i++)
        {
            double angle = entity.AngleRadians - MathHelper.QuarterPi + (MathHelper.HalfPi / 40 * i);
            if (!WorldStatic.World.GetAutoAimEntity(entity.Owner.Entity, entity.Owner.Entity.HitscanAttackPos, angle, Constants.EntityShootDistance, out _,
                out Entity? hitEntity) || hitEntity == null)
                continue;

            int damage = 0;
            for (int j = 0; j < 15; j++)
                damage += (WorldStatic.Random.NextByte() & 7) + 1;

            WorldStatic.EntityManager.Create("BFGExtra", hitEntity.CenterPoint);
            WorldStatic.World.DamageEntity(hitEntity, entity, damage, DamageType.Normal, Thrust.Horizontal);
        }
    }

    private static void A_BabyMetal(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "baby/walk", new SoundParams(entity));
    }

    private static void A_BarrelDestroy(Entity entity)
    {
        entity.Flags.Solid = false;
    }

    private static void A_BasicAttack(Entity entity)
    {
         // TODO
    }

    private static void A_BetaSkullAttack(Entity entity)
    {
         // TODO
    }

    private static void A_BishopMissileWeave(Entity entity)
    {
         // TODO
    }

    private static void A_BossDeath(Entity entity)
    {
        WorldStatic.World.BossDeath(entity);
    }

    private static void A_BrainAwake(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "brain/sight",
            new SoundParams(entity, false, Attenuation.None));
    }

    private static void A_BrainDie(Entity entity)
    {
        WorldStatic.World.ExitLevel(LevelChangeType.Next);
    }

    private static void A_BrainExplode(Entity entity)
    {
        Vec3D pos = new Vec3D(entity.Position.X + (WorldStatic.Random.NextDiff() * 0.03125),
            entity.Position.Y, 128 + (WorldStatic.Random.NextByte() * 2));
        BrainExplodeRocket(entity, pos);
    }

    private static void A_BrainPain(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "brain/pain",
            new SoundParams(entity, false, Attenuation.None));
    }

    private static void A_BrainScream(Entity entity)
    {
        for (double x = entity.Position.X - 196; x < entity.Position.X + 320; x += 8)
        {
            Vec3D pos = new Vec3D(x, entity.Position.Y - 320, 128 + WorldStatic.Random.NextByte() + 1);
            BrainExplodeRocket(entity, pos);
        }

        WorldStatic.SoundManager.CreateSoundOn(entity, "brain/death",
            new SoundParams(entity, false, Attenuation.None));
    }

    private static void BrainExplodeRocket(Entity entity, in Vec3D pos)
    {
        Entity? rocket = WorldStatic.EntityManager.Create("BossRocket", pos);
        if (rocket != null)
        {
            rocket.SetRandomizeTicks(7);
            rocket.Velocity.Z = (WorldStatic.Random.NextByte() << 9) / 65536.0;
        }
    }

    private static void A_BrainSpit(Entity entity)
    {
        Entity[] targets = WorldStatic.World.GetBossTargets();
        if (targets.Length == 0)
            return;

        Entity target = targets[WorldStatic.World.CurrentBossTarget++];
        WorldStatic.World.CurrentBossTarget %= targets.Length;

        double pitch = entity.PitchTo(target);
        Entity? spawnShot = WorldStatic.World.FireProjectile(entity, entity.AngleRadians, pitch, 0.0, false, WorldStatic.SpawnShot, out _);

        if (spawnShot != null)
        {
            spawnShot.Flags.Friendly = entity.Flags.Friendly;
            double distance = entity.Position.Distance(target.Position);
            double speed = spawnShot.Definition.Properties.MissileMovementSpeed;
            double reactionTime = distance / speed;

            spawnShot.AngleRadians = entity.Position.Angle(target.Position);
            spawnShot.Velocity = Vec3D.UnitSphere(spawnShot.AngleRadians, pitch) * speed;
            spawnShot.SetTarget(target);
            spawnShot.ReactionTime = (int)reactionTime;
            spawnShot.Flags.BossSpawnShot = true;
        }

        WorldStatic.SoundManager.CreateSoundOn(entity, "brain/spit",
            new SoundParams(entity, false, Attenuation.None));
    }

    private static void A_SpawnFly(Entity entity)
    {
        if (entity.Target.Entity == null)
        {
            WorldStatic.EntityManager.Destroy(entity);
            return;
        }

        if (entity.ReactionTime > 0)
            return;

        WorldStatic.EntityManager.Create("ArchvileFire", entity.Target.Entity.Position);
        WorldStatic.SoundManager.CreateSoundOn(entity.Target.Entity, "misc/teleport",
            new SoundParams(entity));

        Entity? enemy = WorldStatic.EntityManager.Create(GetRandomBossSpawn(WorldStatic.Random), entity.Target.Entity.Position);
        if (enemy != null)
        {
            enemy.Flags.Friendly = entity.Flags.Friendly;
            enemy.SetNewTarget(true);
            WorldStatic.World.TelefragBlockingEntities(enemy);
        }

        WorldStatic.EntityManager.Destroy(entity);
    }

    private static string GetRandomBossSpawn(IRandom random)
    {
        int value = random.NextByte();
        if (value < 50)
            return "DoomImp";
        else if (value < 90)
            return "Demon";
        else if (value < 120)
            return "Spectre";
        else if (value < 130)
            return "PainElemental";
        else if (value < 160)
            return "Cacodemon";
        else if (value < 162)
            return "Archvile";
        else if (value < 172)
            return "Revenant";
        else if (value < 192)
            return "Arachnotron";
        else if (value < 222)
            return "Fatso";
        else if (value < 246)
            return "HellKnight";

        return "BaronOfHell";
    }

    private static void A_BruisAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        if (entity.InMeleeRange(entity.Target.Entity))
        {
            int damage = ((WorldStatic.Random.NextByte() % 8) + 1) * 10;
            WorldStatic.World.DamageEntity(entity.Target.Entity, entity, damage, DamageType.AlwaysApply, Thrust.Horizontal);
            WorldStatic.SoundManager.CreateSoundOn(entity, "baron/melee", new SoundParams(entity));
            return;
        }

        if (WorldStatic.BaronBall != null)
            FireEnemyProjectile(entity, entity.Target.Entity, WorldStatic.BaronBall);
    }

    private static void A_BspiAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);
        if (WorldStatic.ArachnotronPlasma != null)
            FireEnemyProjectile(entity, entity.Target.Entity, WorldStatic.ArachnotronPlasma);
    }

    private static void A_BulletAttack(Entity entity)
    {
         // TODO
    }

    private static void A_Burst(Entity entity)
    {
         // TODO
    }

    private static void A_CPosAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        entity.PlayAttackSound();
        A_FaceTarget(entity);

        WorldStatic.World.GetAutoAimEntity(entity, entity.HitscanAttackPos, entity.AngleRadians, Constants.EntityShootDistance, out double pitch, out _);

        double angle = entity.AngleRadians + WorldStatic.Random.NextDiff() * Constants.PosRandomSpread / 255;
        WorldStatic.World.FireHitscan(entity, angle, pitch, Constants.EntityShootDistance, 3 * ((WorldStatic.Random.NextByte() % 5) + 1));
    }

    private static void A_CPosRefire(Entity entity)
    {
        Refire(entity, 40);
    }

    private static void A_CStaffMissileSlither(Entity entity)
    {
         // TODO
    }

    private static void A_CentaurDefend(Entity entity)
    {
         // TODO
    }

    private static void A_ChangeCountFlags(Entity entity)
    {
         // TODO
    }

    private static void A_ChangeFlag(Entity entity)
    {
         // TODO
    }

    private static void A_ChangeVelocity(Entity entity)
    {
         // TODO
    }

    public static void A_Chase(Entity entity)
    {
        if (entity.ReactionTime > 0)
            entity.ReactionTime -= entity.SlowTickMultiplier;

        if (entity.Threshold > 0)
        {
            if (entity.Target.Entity == null || entity.Target.Entity.IsDead)
                entity.Threshold = 0;
            else
                entity.Threshold -= entity.SlowTickMultiplier;
        }

        if (entity.SlowTickMultiplier <= 1)
            entity.TurnTowardsMovementDirection();

        if (entity.Target.Entity == null || entity.Target.Entity.IsDead)
        {
            if (!entity.SetNewTarget(true))
                entity.SetSpawnState();
            return;
        }

        if (entity.Target.Entity != null && entity.IsFriend(entity.Target.Entity))
            entity.SetNewTarget(true);

        if (entity.Flags.JustAttacked)
        {
            entity.Flags.JustAttacked = false;
            if (!WorldStatic.IsFastMonsters)
                entity.SetNewChaseDirection();
            return;
        }

        if (entity.Target.Entity != null && entity.Definition.MeleeState != null && entity.InMeleeRange(entity.Target.Entity))
        {
            entity.PlayAttackSound();
            entity.FrameState.SetFrameIndex(entity.Definition.MeleeState.Value);
        }

        if (entity.IsDisposed)
            return;

        // Set the Move Count to 1 so that the attack happens on the next A_Chase call. Otherwise the monster will attack more often than normal.
        if (entity.MoveCount > 1 && entity.SlowTickMultiplier > 1)
            entity.MoveCount = Math.Clamp(entity.MoveCount - entity.SlowTickMultiplier, 1, int.MaxValue);

        if ((entity.MoveCount == 0 || WorldStatic.IsFastMonsters) &&
            entity.Definition.MissileState != null && entity.CheckMissileRange())
        {
            entity.Flags.JustAttacked = true;
            entity.FrameState.SetFrameIndex(entity.Definition.MissileState.Value);
        }
        else if (WorldStatic.Random.NextByte() < 3)
        {
            entity.PlayActiveSound();
        }

        if (entity.IsDisposed)
            return;

        entity.MoveCount--;

        if (entity.MoveCount < 0 || !entity.MoveEnemy(out TryMoveData? _))
        {
            entity.SetNewChaseDirection();
            // Need to turn here if slow ticking, otherwise monsters will slide in directions they aren't facing.
            if (entity.SlowTickMultiplier > 1)
                entity.SetToMovementDirection();
        }
    }

    private static void A_CheckBlock(Entity entity)
    {
         // TODO
    }

    private static void A_CheckCeiling(Entity entity)
    {
         // TODO
    }

    private static void A_CheckFlag(Entity entity)
    {
         // TODO
    }

    private static void A_CheckFloor(Entity entity)
    {
         // TODO
    }

    private static void A_CheckForReload(Entity entity)
    {
         // TODO
    }

    private static void A_CheckForResurrection(Entity entity)
    {
         // TODO
    }

    private static void A_CheckLOF(Entity entity)
    {
         // TODO
    }

    private static void A_CheckPlayerDone(Entity entity)
    {
         // TODO
    }

    private static void A_CheckProximity(Entity entity)
    {
         // TODO
    }

    private static void A_CheckRange(Entity entity)
    {
         // TODO
    }

    private static void A_CheckReload(Entity entity)
    {
        if (entity.PlayerObj == null || entity.PlayerObj.CheckAmmo())
            return;

        entity.PlayerObj.ForceLowerWeapon(true);
        entity.PlayerObj.TrySwitchWeapon();
    }

    private static void A_CheckSight(Entity entity)
    {
         // TODO
    }

    private static void A_CheckSightOrRange(Entity entity)
    {
         // TODO
    }

    private static void A_CheckSpecies(Entity entity)
    {
         // TODO
    }

    private static void A_CheckTerrain(Entity entity)
    {
         // TODO
    }

    private static void A_ClearLastHeard(Entity entity)
    {
         // TODO
    }

    private static void A_ClearOverlays(Entity entity)
    {
         // TODO
    }

    private static void A_ClearReFire(Entity entity)
    {
         // TODO
    }

    private static void A_ClearShadow(Entity entity)
    {
         // TODO
    }

    private static void A_ClearSoundTarget(Entity entity)
    {
         // TODO
    }

    private static void A_ClearTarget(Entity entity)
    {
         // TODO
    }

    private static void A_CloseShotgun2(Entity entity)
    {
        WorldStatic.World.SoundManager.CreateSoundOn(entity, "weapons/sshotc",
            new SoundParams(entity, channel: entity.WeaponSoundChannel));
        A_ReFire(entity);
    }

    private static void A_ComboAttack(Entity entity)
    {
         // TODO
    }

    private static void A_CopyFriendliness(Entity entity)
    {
         // TODO
    }

    private static void A_CopySpriteFrame(Entity entity)
    {
         // TODO
    }

    private static void A_Countdown(Entity entity)
    {
         // TODO
    }

    private static void A_CountdownArg(Entity entity)
    {
         // TODO
    }

    private static void A_CustomBulletAttack(Entity entity)
    {
         // TODO
    }

    private static void A_CustomComboAttack(Entity entity)
    {
         // TODO
    }

    private static void A_CustomMeleeAttack(Entity entity)
    {
         // TODO
    }

    private static void A_CustomMissile(Entity entity)
    {
         // TODO
    }

    private static void A_CustomPunch(Entity entity)
    {
         // TODO
    }

    private static void A_CustomRailgun(Entity entity)
    {
         // TODO
    }

    private static void A_CyberAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);
        FireEnemyProjectile(entity, entity.Target.Entity, WorldStatic.Rocket);
    }

    private static void A_DamageChildren(Entity entity)
    {
         // TODO
    }

    private static void A_DamageMaster(Entity entity)
    {
         // TODO
    }

    private static void A_DamageSelf(Entity entity)
    {
         // TODO
    }

    private static void A_DamageSiblings(Entity entity)
    {
         // TODO
    }

    private static void A_DamageTarget(Entity entity)
    {
         // TODO
    }

    private static void A_DamageTracer(Entity entity)
    {
         // TODO
    }

    private static void A_DeQueueCorpse(Entity entity)
    {
         // TODO
    }

    private static void A_DropInventory(Entity entity)
    {
         // TODO
    }

    private static void A_DropItem(Entity entity)
    {
         // TODO
    }

    private static void A_DualPainAttack(Entity entity)
    {
         // TODO
    }

    private static void A_Explode(Entity entity)
    {
        // Pass through owner if set (usually a projectile)
        // Barrels pass through who shot them (Target)
        Entity? attackSource = entity.Owner.Entity ?? entity.Target.Entity;
        WorldStatic.World.RadiusExplosion(entity, attackSource ?? entity, 128, 128);
    }

    private static void A_ExtChase(Entity entity)
    {
         // TODO
    }

    private static void A_FLoopActiveSound(Entity entity)
    {
         // TODO
    }

    private static void A_FaceMaster(Entity entity)
    {
         // TODO
    }

    private static void A_FaceMovementDirection(Entity entity)
    {
         // TODO
    }

    public static void A_FaceTarget(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        entity.AngleRadians = entity.Position.Angle(entity.Target.Entity.Position);
        if (entity.Target.Entity.Flags.Shadow)
            entity.AngleRadians += WorldStatic.Random.NextDiff() * Constants.ShadowRandomSpread / 255;
    }

    private static void A_FaceTracer(Entity entity)
    {
         // TODO
    }

    private static void A_FadeIn(Entity entity)
    {
         // TODO
    }

    private static void A_FadeOut(Entity entity)
    {
         // TODO
    }

    private static void A_FadeTo(Entity entity)
    {
         // TODO
    }

    private static void A_Fall(Entity entity)
    {
        A_NoBlocking(entity);
    }

    private static void A_FastChase(Entity entity)
    {
         // TODO
    }

    private static void A_FatAttack1(Entity entity)
    {
        FatAttack(entity, 0.0, Constants.MancSpread);
    }

    private static void A_FatAttack2(Entity entity)
    {
        FatAttack(entity, 0.0, -Constants.MancSpread * 2);
    }

    private static void A_FatAttack3(Entity entity)
    {
        FatAttack(entity, -Constants.MancSpread / 2, Constants.MancSpread / 2);
    }

    private static void FatAttack(Entity entity, double fireSpread1, double fireSpread2)
    {
        if (entity.Target.Entity == null || WorldStatic.FatShot == null)
            return;

        A_FaceTarget(entity);
        double baseAngle = entity.AngleRadians;

        entity.AngleRadians = baseAngle + fireSpread1;
        FireEnemyProjectile(entity, entity.Target.Entity, WorldStatic.FatShot);

        entity.AngleRadians = baseAngle + fireSpread2;
        FireEnemyProjectile(entity, entity.Target.Entity, WorldStatic.FatShot);

        entity.AngleRadians = baseAngle;
    }

    private static void A_FatRaise(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);
        WorldStatic.SoundManager.CreateSoundOn(entity, "fatso/raiseguns", new SoundParams(entity));
    }

    private static void A_Fire(Entity entity)
    {
        if (entity.Target.Entity == null || entity.Tracer.Entity == null)
            return;

        if (!WorldStatic.World.CheckLineOfSight(entity.Target.Entity, entity.Tracer.Entity))
            return;

        Vec3D newPos = entity.Tracer.Entity.Position;
        Vec3D unit = Vec3D.UnitSphere(entity.Tracer.Entity.AngleRadians, 0.0);
        newPos.X += unit.X * 24;
        newPos.Y += unit.Y * 24;

        entity.Position = newPos;
    }

    private static void A_FireAssaultGun(Entity entity)
    {
         // TODO
    }

    private static void A_FireBFG(Entity entity)
    {
        if (entity.PlayerObj == null || WorldStatic.BFGBall == null)
            return;

        entity.PlayerObj.DecreaseAmmoCompatibility(40);
        WorldStatic.World.FireProjectile(entity, entity.AngleRadians, entity.PlayerObj.PitchRadians, Constants.EntityShootDistance,
            WorldStatic.World.Config.Game.AutoAim, WorldStatic.BFGBall, out _);
    }

    private static void A_FireBullets(Entity entity)
    {
         // TODO
    }

    private static void A_FireCGun(Entity entity)
    {
        if (entity.PlayerObj == null)
            return;

        if (!entity.PlayerObj.CheckAmmo())
            return;

        entity.PlayerObj.DecreaseAmmoCompatibility(1);
        WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/pistol", new SoundParams(entity, channel: entity.WeaponSoundChannel));
        int offset = entity.PlayerObj.Weapon == null ? 0 : Math.Clamp(entity.PlayerObj.Weapon.FrameState.Frame.Frame, 0, 1);
        entity.PlayerObj.Weapon?.SetFlashState(offset);
        WorldStatic.World.FirePlayerHitscanBullets(entity.PlayerObj, 1, Constants.DefaultSpreadAngle, 0,
            entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, WorldStatic.World.Config.Game.AutoAim);
    }

    private static void A_FireCrackle(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "vile/firecrkl", new SoundParams(entity));
    }

    private static void A_FireCustomMissile(Entity entity)
    {
         // TODO
    }

    private static void A_FireMissile(Entity entity)
    {
        if (entity.PlayerObj == null || WorldStatic.Rocket == null)
            return;

        entity.PlayerObj.DecreaseAmmoCompatibility(1);
        WorldStatic.World.FireProjectile(entity, entity.AngleRadians, entity.PlayerObj.PitchRadians, Constants.EntityShootDistance,
            WorldStatic.World.Config.Game.AutoAim, WorldStatic.Rocket, out _);
    }

    private static void A_FireOldBFG(Entity entity)
    {
        // TODO not sure of difference between A_FireBFG and A_FireOldBFG
        if (entity.PlayerObj == null)
            return;

        entity.PlayerObj.DecreaseAmmoCompatibility(40);
        WorldStatic.World.FireProjectile(entity, entity.AngleRadians, entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, false, WorldStatic.BFGBall, out _);
    }

    private static void A_FirePistol(Entity entity)
    {
        if (entity.PlayerObj == null)
            return;
        
        entity.PlayerObj.DecreaseAmmoCompatibility(1);
        WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/pistol", new SoundParams(entity, channel: entity.WeaponSoundChannel));
        entity.PlayerObj.Weapon?.SetFlashState();
        WorldStatic.World.FirePlayerHitscanBullets(entity.PlayerObj, 1, Constants.DefaultSpreadAngle, 0,
            entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, WorldStatic.World.Config.Game.AutoAim);
    }

    private static void A_FirePlasma(Entity entity)
    {
        if (entity.PlayerObj == null)
            return;

        entity.PlayerObj.DecreaseAmmoCompatibility(1);
        entity.PlayerObj.Weapon?.SetFlashState(WorldStatic.Random.NextByte() & 1);
        WorldStatic.World.FireProjectile(entity, entity.AngleRadians, entity.PlayerObj.PitchRadians, Constants.EntityShootDistance,
            WorldStatic.World.Config.Game.AutoAim, WorldStatic.PlasmaBall, out _);
    }

    private static void A_FireProjectile(Entity entity)
    {
         // TODO
    }

    private static void A_FireSTGrenade(Entity entity)
    {
         // TODO
    }

    private static void A_FireShotgun(Entity entity)
    {
        if (entity.PlayerObj != null)
        {
            entity.PlayerObj.DecreaseAmmoCompatibility(1);
            WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/shotgf", new SoundParams(entity, channel: entity.WeaponSoundChannel));
            entity.PlayerObj.Weapon?.SetFlashState();
            WorldStatic.World.FirePlayerHitscanBullets(entity.PlayerObj, Constants.ShotgunBullets, Constants.DefaultSpreadAngle, 0.0,
                entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, WorldStatic.World.Config.Game.AutoAim);
        }
    }

    private static void A_FireShotgun2(Entity entity)
    {
        if (entity.PlayerObj != null)
        {
            entity.PlayerObj.DecreaseAmmoCompatibility(2);
            WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/sshotf", new SoundParams(entity, channel: entity.WeaponSoundChannel));
            entity.PlayerObj.Weapon?.SetFlashState();
            WorldStatic.World.FirePlayerHitscanBullets(entity.PlayerObj, Constants.SuperShotgunBullets, Constants.SuperShotgunSpreadAngle, Constants.SuperShotgunSpreadPitch,
                entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, WorldStatic.World.Config.Game.AutoAim);
        }
    }

    private static void A_FreezeDeath(Entity entity)
    {
         // TODO
    }

    private static void A_FreezeDeathChunks(Entity entity)
    {
         // TODO
    }

    private static void A_GenericFreezeDeath(Entity entity)
    {
         // TODO
    }

    private static void A_GetHurt(Entity entity)
    {
         // TODO
    }

    private static void A_GiveInventory(Entity entity)
    {
        if (entity.PickupPlayer == null || entity.Frame.Args.Values.Count == 0)
            return;

        int amount = 1;
        if (entity.Frame.Args.Values.Count > 1)
            amount = entity.Frame.Args.GetInt(1);

        var def = WorldStatic.EntityManager.DefinitionComposer.GetByName(entity.Frame.Args.GetString(0));
        if (def == null)
            return;

        for (int i = 0; i < amount; i++)
            entity.PickupPlayer.GiveItem(def, null);
    }

    private static void A_GiveToChildren(Entity entity)
    {
         // TODO
    }

    private static void A_GiveToSiblings(Entity entity)
    {
         // TODO
    }

    private static void A_GiveToTarget(Entity entity)
    {
         // TODO
    }

    private static void A_Gravity(Entity entity)
    {
         // TODO
    }

    private static void A_GunFlash(Entity entity)
    {
        if (entity.PlayerObj != null)
        {
            if (entity.Definition.MissileState != null)
                entity.FrameState.SetFrameIndex(entity.Definition.MissileState.Value);
            entity.PlayerObj.Weapon?.SetFlashState();
        }
    }

    private static void A_HeadAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);

        if (entity.InMeleeRange(entity.Target.Entity))
        {
            int damage = ((WorldStatic.Random.NextByte() % 6) + 1) * 10;
            WorldStatic.World.DamageEntity(entity.Target.Entity, entity, damage, DamageType.AlwaysApply, Thrust.Horizontal);
            entity.PlayAttackSound();
            return;
        }

        if (WorldStatic.CacodemonBall != null)
            FireEnemyProjectile(entity, entity.Target.Entity, WorldStatic.CacodemonBall);
    }

    private static void A_HideThing(Entity entity)
    {
         // TODO
    }

    private static void A_Hoof(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "cyber/hoof", new SoundParams(entity));
    }

    private static void A_IceGuyDie(Entity entity)
    {
         // TODO
    }

    private static void A_Jump(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIf(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfArmorType(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfCloser(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfHealthLower(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfHigherOrLower(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfInTargetInventory(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfInTargetLOS(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfInventory(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfMasterCloser(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfNoAmmo(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfTargetInLOS(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfTargetInsideMeleeRange(Entity entity)
    {
         // TODO
    }

    private static void A_JumpIfTargetOutsideMeleeRange(Entity entity)
    {
         // TODO
    }

    public static void A_KeenDie(Entity entity)
    {
        var world = WorldStatic.World;
        if (world.EntityAliveCount(entity.Definition.Id) == 0)
        {
            var sectors = world.FindBySectorTag(666);
            foreach (var sector in sectors)
            {
                var special = world.SpecialManager.CreateDoorOpenStaySpecial(sector, VanillaConstants.DoorSlowSpeed * SpecialManager.SpeedFactor);
                world.SpecialManager.AddSpecial(special);
            }
        }
    }

    private static void A_KillChildren(Entity entity)
    {
         // TODO
    }

    private static void A_KillMaster(Entity entity)
    {
         // TODO
    }

    private static void A_KillSiblings(Entity entity)
    {
         // TODO
    }

    private static void A_KillTarget(Entity entity)
    {
         // TODO
    }

    private static void A_KillTracer(Entity entity)
    {
         // TODO
    }

    private static void A_KlaxonBlare(Entity entity)
    {
         // TODO
    }

    private static void A_Light(Entity entity)
    {
        // TODO this is based on decorate parameters
        //if (entity is Player player)
        //    player.ExtraLight = 0;
    }

    private static void A_Light0(Entity entity)
    {
        if (entity.PlayerObj != null)
            entity.PlayerObj.ExtraLight = 0;
    }

    private static void A_Light1(Entity entity)
    {
        if (entity.PlayerObj != null)
            entity.PlayerObj.ExtraLight = 1;
    }

    private static void A_Light2(Entity entity)
    {
        if (entity.PlayerObj != null)
            entity.PlayerObj.ExtraLight = 2;
    }

    private static void A_LightInverse(Entity entity)
    {
         // TODO
    }

    private static void A_LoadShotgun2(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/sshotl", new SoundParams(entity, channel: entity.WeaponSoundChannel));
    }

    private static void A_Log(Entity entity)
    {
         // TODO
    }

    private static void A_LogFloat(Entity entity)
    {
         // TODO
    }

    private static void A_LogInt(Entity entity)
    {
         // TODO
    }

    public static void A_Look(Entity entity)
    {
        if (entity.InMonsterCloset)
        {
            entity.SetClosetLook();
            return;
        }    

        entity.SetNewTarget(false);
    }

    private static void A_Look2(Entity entity)
    {
         // TODO
    }

    private static void A_LookEx(Entity entity)
    {
         // TODO
    }

    private static void A_LoopActiveSound(Entity entity)
    {
         // TODO
    }

    private static void A_LowGravity(Entity entity)
    {
         // TODO
    }

    private static void A_M_Saw(Entity entity)
    {
         // TODO
    }

    private static void A_MeleeAttack(Entity entity)
    {
         // TODO
    }

    private static void A_Metal(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "spider/walk", new SoundParams(entity));
    }

    private static void A_MissileAttack(Entity entity)
    {
         // TODO
    }

    private static void A_MonsterRail(Entity entity)
    {
         // TODO
    }

    private static void A_MonsterRefire(Entity entity)
    {
         // TODO
    }

    private static void A_Morph(Entity entity)
    {
         // TODO
    }

    private static void A_NoBlocking(Entity entity)
    {
        entity.Flags.Solid = false;
    }

    private static void A_NoGravity(Entity entity)
    {
         // TODO
    }

    private static void A_OpenShotgun2(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/sshoto", new SoundParams(entity, channel: entity.WeaponSoundChannel));
    }

    private static void A_Overlay(Entity entity)
    {
         // TODO
    }

    private static void A_OverlayAlpha(Entity entity)
    {
         // TODO
    }

    private static void A_OverlayFlags(Entity entity)
    {
         // TODO
    }

    private static void A_OverlayOffset(Entity entity)
    {
         // TODO
    }

    private static void A_OverlayRenderstyle(Entity entity)
    {
         // TODO
    }

    private static void A_PainAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);
        A_PainShootSkull(entity, entity.AngleRadians);
    }

    private static void A_PainDie(Entity entity)
    {
        A_PainShootSkull(entity, entity.AngleRadians + MathHelper.HalfPi);
        A_PainShootSkull(entity, entity.AngleRadians + MathHelper.Pi);
        A_PainShootSkull(entity, entity.AngleRadians + MathHelper.Pi + MathHelper.HalfPi);
    }

    private static void A_PainShootSkull(Entity entity, double angle)
    {
        if (WorldStatic.World.Config.Compatibility.PainElementalLostSoulLimit)
        {
            var def = WorldStatic.EntityManager.DefinitionComposer.GetByName("LostSoul");
            if (def != null && WorldStatic.World.EntityCount(def.Id) > 20)
                return;
        }

        Vec3D skullPos = entity.Position;
        Vec3D startPos = entity.Position;
        skullPos.Z += 8;

        Entity? skull = WorldStatic.EntityManager.Create("LostSoul", startPos);
        if (skull == null)
            return;

        skull.Flags.Friendly = entity.Flags.Friendly;
        double step = 4 + (3 * (entity.Radius + skull.Radius) / 2);
        skullPos += Vec3D.UnitSphere(angle, 0.0) * step;
        startPos += Vec3D.UnitSphere(angle, 0.0) * (entity.Radius + skull.Radius - 2);
        skull.Position = startPos;
        skull.Flags.CountKill = false;
        skull.Flags.IsMonster = true;

        // Ignore parent for clip checking
        bool wasSolid = entity.Flags.Solid;
        entity.Flags.Solid = false;

        // Add some better checking from the original
        // Set the skull barely clipped into the parent
        // Then check if it can move to it's final position (TryMoveXY does step checking and won't skip lines/entities)
        if (!WorldStatic.World.PhysicsManager.IsPositionValid(skull, startPos.XY, WorldStatic.World.PhysicsManager.TryMoveData) || 
            !WorldStatic.World.PhysicsManager.TryMoveXY(skull, skullPos.XY).Success)
        {
            skull.Kill(null);
            entity.Flags.Solid = wasSolid;
            return;
        }

        entity.Flags.Solid = wasSolid;
        skull.SetTarget(entity.Target.Entity);
        A_SkullAttack(skull);
    }

    private static void A_PlaySoundEx(Entity entity)
    {
         // TODO
    }

    private static void A_PlayWeaponSound(Entity entity)
    {
         // TODO
    }

    private static void A_PlayerSkinCheck(Entity entity)
    {
         // TODO
    }

    private static void A_PosAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        entity.PlayAttackSound();
        A_FaceTarget(entity);

        // could remove GetAutoAimEntity if FireHitscanBullets took optional auto aim angle
        WorldStatic.World.GetAutoAimEntity(entity, entity.HitscanAttackPos, entity.AngleRadians, Constants.EntityShootDistance, out double pitch, out _);

        entity.AngleRadians += WorldStatic.Random.NextDiff() * Constants.PosRandomSpread / 255;
        WorldStatic.World.FireHitscan(entity, entity.AngleRadians, pitch, Constants.EntityShootDistance, 3 * ((WorldStatic.Random.NextByte() % 5) + 1));
    }

    private static void A_Print(Entity entity)
    {
         // TODO
    }

    private static void A_PrintBold(Entity entity)
    {
         // TODO
    }

    private static void A_Punch(Entity entity)
    {
        if (entity.PlayerObj != null)
            PlayerMelee(entity.PlayerObj, 2, 10, 10.0, Constants.EntityMeleeDistance, "player/male/fist");
    }

    private static void A_Quake(Entity entity)
    {
         // TODO
    }

    private static void A_QuakeEx(Entity entity)
    {
         // TODO
    }

    private static void A_QueueCorpse(Entity entity)
    {
         // TODO
    }

    private static void A_RadiusDamageSelf(Entity entity)
    {
         // TODO
    }

    private static void A_RadiusGive(Entity entity)
    {
         // TODO
    }

    private static void A_RadiusThrust(Entity entity)
    {
         // TODO
    }

    private static void A_RailAttack(Entity entity)
    {
        // TODO
    }

    private static void A_Lower(Entity entity)
    {
        if (entity.PlayerObj == null || entity.PlayerObj.AnimationWeapon == null)
            return;

        entity.PlayerObj.WeaponOffset.Y += Constants.WeaponLowerSpeed;
        if (entity.PlayerObj.WeaponOffset.Y < Constants.WeaponBottom)
            return;

        if (entity.PlayerObj.IsDead)
        {
            entity.PlayerObj.WeaponOffset.Y = Constants.WeaponBottom;
            entity.PlayerObj.AnimationWeapon.FrameState.SetState("NULL");
            return;
        }

        entity.PlayerObj.BringupWeapon();
    }

    private static void A_Raise(Entity entity)
    {
        if (entity.PlayerObj == null || entity.PlayerObj.AnimationWeapon == null)
            return;

        entity.PlayerObj.WeaponOffset.Y -= Constants.WeaponRaiseSpeed;
        if (entity.PlayerObj.WeaponOffset.Y > Constants.WeaponTop)
            return;

        entity.PlayerObj.SetWeaponUp();
        entity.PlayerObj.WeaponOffset.Y = Constants.WeaponTop;
        entity.PlayerObj.AnimationWeapon.SetReadyState();
    }

    public static void A_WeaponReady(Entity entity)
    {
        if (entity.PlayerObj == null || entity.PlayerObj.Weapon == null)
            return;
        
        var player = entity.PlayerObj;
        player.Weapon.ReadyState = true;
        player.WeaponOffset.Y = Constants.WeaponTop;
        if (!player.Weapon.Definition.Flags.WeaponNoAutofire || !player.AttackDown)
            entity.PlayerObj.Weapon.ReadyToFire = true;

        if (entity.PlayerObj.PendingWeapon != null || player.IsDead)
        {
            entity.PlayerObj.LowerWeapon();
            return;
        }

        if (player.TickCommand.Has(TickCommands.Attack))
        {
            player.FireWeapon();
            player.AttackDown = true;
        }
        else
        {
            player.AttackDown = false;
            player.Refire = false;
        }

        if (!player.IsVooDooDoll && player.Weapon.Definition.Properties.Weapons.ReadySound.Length > 0 &&
            player.Weapon.FrameState.IsState(Constants.FrameStates.Ready))
        {
            WorldStatic.SoundManager.CreateSoundOn(entity, player.Weapon.Definition.Properties.Weapons.ReadySound,
                new SoundParams(entity, channel: entity.WeaponSoundChannel));
        }
    }

    private static void A_RaiseChildren(Entity entity)
    {
         // TODO
    }

    private static void A_RaiseMaster(Entity entity)
    {
         // TODO
    }

    private static void A_RaiseSelf(Entity entity)
    {
         // TODO
    }

    private static void A_RaiseSiblings(Entity entity)
    {
         // TODO
    }

    private static void A_ReFire(Entity entity)
    {
        if (entity.PlayerObj != null)
        {
            if (entity.PlayerObj.PendingWeapon != null)
            {
                entity.PlayerObj.Refire = false;
                return;
            }

            if (entity.PlayerObj.CanFireWeapon())
            {
                entity.PlayerObj.Refire = true;
                entity.PlayerObj.Weapon?.SetFireState();
                entity.PlayerObj.SetFireState();
            }
            else
            {
                if (!entity.PlayerObj.CheckAmmo())
                    entity.PlayerObj.TrySwitchWeapon();
                entity.PlayerObj.Refire = false;
            }
        }
    }

    private static void A_RearrangePointers(Entity entity)
    {
         // TODO
    }

    private static void A_Recoil(Entity entity)
    {
         // TODO
    }

    private static void A_Remove(Entity entity)
    {
         // TODO
    }

    private static void A_RemoveChildren(Entity entity)
    {
         // TODO
    }

    private static void A_RemoveMaster(Entity entity)
    {
         // TODO
    }

    private static void A_RemoveSiblings(Entity entity)
    {
         // TODO
    }

    private static void A_RemoveTarget(Entity entity)
    {
         // TODO
    }

    private static void A_RemoveTracer(Entity entity)
    {
         // TODO
    }

    private static void A_ResetHealth(Entity entity)
    {
         // TODO
    }

    private static void A_ResetReloadCounter(Entity entity)
    {
         // TODO
    }

    private static void A_Respawn(Entity entity)
    {
         // TODO
    }

    private static void A_SPosAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);

        // could remove GetAutoAimEntity if FireHitscanBullets took optional auto aim angle
        WorldStatic.World.GetAutoAimEntity(entity, entity.HitscanAttackPos, entity.AngleRadians, Constants.EntityShootDistance, out double pitch, out _);

        double angle = entity.AngleRadians;
        for (int i = 0; i < 3; i++)
            WorldStatic.World.FireHitscan(entity, angle + (WorldStatic.Random.NextDiff() * Constants.PosRandomSpread / 255), 
                pitch, Constants.EntityShootDistance, 3 * ((WorldStatic.Random.NextByte() % 5) + 1));
    }

    private static void A_SargAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);
        if (entity.InMeleeRange(entity.Target.Entity))
        {
            int damage = ((WorldStatic.Random.NextByte() % 10) + 1) * 4;
            WorldStatic.World.DamageEntity(entity.Target.Entity, entity, damage, DamageType.AlwaysApply, Thrust.Horizontal);
        }
    }

    private static void A_Saw(Entity entity)
    {
        var player = entity.PlayerObj;
        if (player == null)
            return;

        int damage = 2 * ((WorldStatic.Random.NextByte()) % 10) + 1;
        double range = Constants.EntityMeleeDistance + 1;
        double angle = player.AngleRadians + (WorldStatic.Random.NextDiff() * Constants.MeleeAngle / 255);
        double pitch = player.PitchRadians;
        if (WorldStatic.World.Config.Game.AutoAim)
            WorldStatic.World.GetAutoAimEntity(entity, player.HitscanAttackPos, player.AngleRadians, range, out pitch, out _);
        // Doom added + 1 so the bulletpuff would include the spark state
        Entity? hitEntity = WorldStatic.World.FireHitscan(entity, angle, pitch, range, damage);
        if (hitEntity == null)
        {
            WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/sawfull", new SoundParams(entity, channel: entity.WeaponSoundChannel));
        }
        else
        {
            WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/sawhit", new SoundParams(entity, channel: entity.WeaponSoundChannel));
            double toAngle = MathHelper.GetPositiveAngle(entity.Position.Angle(hitEntity.Position));
            double playerAngle = MathHelper.GetPositiveAngle(player.AngleRadians);
            const double AngleLarger = MathHelper.HalfPi / 20;
            const double AngleSmaller = MathHelper.HalfPi / 21;
            if (MathHelper.GetNormalAngle(toAngle - playerAngle) < 0)
            {
                if (MathHelper.GetNormalAngle(toAngle - playerAngle) < -AngleLarger)
                    playerAngle = toAngle + AngleSmaller;
                else
                    playerAngle -= AngleLarger;
            }
            else
            {
                if (MathHelper.GetNormalAngle(toAngle - playerAngle) > AngleLarger)
                    playerAngle = toAngle - AngleSmaller;
                else
                    playerAngle += AngleLarger;
            }

            player.AngleRadians = playerAngle;
        }
    }

    private static void A_ScaleVelocity(Entity entity)
    {
         // TODO
    }

    private static void A_Pain(Entity entity)
    {
        if (entity.Definition.Properties.PainSound.Length == 0)
            return;

        WorldStatic.SoundManager.CreateSoundOn(entity, entity.Definition.Properties.PainSound,
            new SoundParams(entity, type: SoundType.Pain));
    }

    private static void A_PlayerScream(Entity entity)
    {
        if (!entity.IsPlayer || entity.Definition.Properties.DeathSound.Length > 0)
        {
            A_Scream(entity);
            return;
        }

        string deathSound =  entity.Health > -50 ? "*death" : "*xdeath";
        WorldStatic.SoundManager.CreateSoundOn(entity, deathSound, new SoundParams(entity));
    }

    private static void A_Scream(Entity entity)
    {
        entity.PlayDeathSound();
    }

    private static void A_XScream(Entity entity)
    {
        if (entity.IsPlayer)
            WorldStatic.SoundManager.CreateSoundOn(entity, "*gibbed",  new SoundParams(entity));
        else
            WorldStatic.SoundManager.CreateSoundOn(entity, "misc/gibbed", new SoundParams(entity));

    }

    private static void A_ScreamAndUnblock(Entity entity)
    {
         // TODO
    }

    private static void A_SeekerMissile(Entity entity)
    {
         // TODO
    }

    private static void A_SelectWeapon(Entity entity)
    {
        if (entity.PickupPlayer == null || entity.Frame.Args.Values.Count == 0)
            return;

        Weapon? weapon = entity.PickupPlayer.Inventory.Weapons.GetWeapon(entity.Frame.Args.GetString(0));
        if (weapon == null)
            return;

        entity.PickupPlayer.ChangeWeapon(weapon);
    }

    private static void A_SentinelBob(Entity entity)
    {
         // TODO
    }

    private static void A_SentinelRefire(Entity entity)
    {
         // TODO
    }

    private static void A_SetAngle(Entity entity)
    {
         // TODO
    }

    private static void A_SetArg(Entity entity)
    {
         // TODO
    }

    private static void A_SetBlend(Entity entity)
    {
         // TODO
    }

    private static void A_SetChaseThreshold(Entity entity)
    {
         // TODO
    }

    private static void A_SetCrosshair(Entity entity)
    {
         // TODO
    }

    private static void A_SetDamageType(Entity entity)
    {
         // TODO
    }

    private static void A_SetFloat(Entity entity)
    {
         // TODO
    }

    private static void A_SetFloatBobPhase(Entity entity)
    {
         // TODO
    }

    private static void A_SetFloatSpeed(Entity entity)
    {
         // TODO
    }

    private static void A_SetFloorClip(Entity entity)
    {
         // TODO
    }

    private static void A_SetGravity(Entity entity)
    {
         // TODO
    }

    private static void A_SetHealth(Entity entity)
    {
         // TODO
    }

    private static void A_SetInventory(Entity entity)
    {
         // TODO
    }

    private static void A_SetInvulnerable(Entity entity)
    {
         // TODO
    }

    private static void A_SetMass(Entity entity)
    {
         // TODO
    }

    private static void A_SetMugshotState(Entity entity)
    {
         // TODO
    }

    private static void A_SetPainThreshold(Entity entity)
    {
         // TODO
    }

    private static void A_SetPitch(Entity entity)
    {
         // TODO
    }

    private static void A_SetReflective(Entity entity)
    {
         // TODO
    }

    private static void A_SetReflectiveInvulnerable(Entity entity)
    {
         // TODO
    }

    private static void A_SetRenderStyle(Entity entity)
    {
         // TODO
    }

    private static void A_SetRipMax(Entity entity)
    {
         // TODO
    }

    private static void A_SetRipMin(Entity entity)
    {
         // TODO
    }

    private static void A_SetRipperLevel(Entity entity)
    {
         // TODO
    }

    private static void A_SetRoll(Entity entity)
    {
         // TODO
    }

    private static void A_SetScale(Entity entity)
    {
         // TODO
    }

    private static void A_SetShadow(Entity entity)
    {
         // TODO
    }

    private static void A_SetShootable(Entity entity)
    {
         // TODO
    }

    private static void A_SetSize(Entity entity)
    {
         // TODO
    }

    private static void A_SetSolid(Entity entity)
    {
         // TODO
    }

    private static void A_SetSpecial(Entity entity)
    {
         // TODO
    }

    private static void A_SetSpecies(Entity entity)
    {
         // TODO
    }

    private static void A_SetSpeed(Entity entity)
    {
         // TODO
    }

    private static void A_SetSpriteAngle(Entity entity)
    {
         // TODO
    }

    private static void A_SetSpriteRotation(Entity entity)
    {
         // TODO
    }

    private static void A_SetTeleFog(Entity entity)
    {
         // TODO
    }

    private static void A_SetTics(Entity entity)
    {
         // TODO
    }

    private static void A_SetTranslation(Entity entity)
    {
         // TODO
    }

    private static void A_SetTranslucent(Entity entity)
    {
         // TODO
    }

    private static void A_SetUserArray(Entity entity)
    {
         // TODO
    }

    private static void A_SetUserArrayFloat(Entity entity)
    {
         // TODO
    }

    private static void A_SetUserVar(Entity entity)
    {
         // TODO
    }

    private static void A_SetUserVarFloat(Entity entity)
    {
         // TODO
    }

    private static void A_SetVisibleRotation(Entity entity)
    {
         // TODO
    }

    private static void A_SkelFist(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);

        if (entity.InMeleeRange(entity.Target.Entity))
        {
            int damage = ((WorldStatic.Random.NextByte() % 10) + 1) * 6;
            WorldStatic.World.DamageEntity(entity.Target.Entity, entity, damage, DamageType.AlwaysApply, Thrust.Horizontal);
            WorldStatic.SoundManager.CreateSoundOn(entity, "skeleton/melee", new SoundParams(entity));
        }
    }

    private static void A_SkelMissile(Entity entity)
    {
        if (entity.Target.Entity == null || WorldStatic.RevenantTracer == null)
            return;

        Entity? fireball = FireEnemyProjectile(entity, entity.Target.Entity, WorldStatic.RevenantTracer, zOffset: 16);
        if (fireball != null)
            fireball.SetTracer(entity.Target.Entity);
    }

    private static void A_SkelWhoosh(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);
        WorldStatic.SoundManager.CreateSoundOn(entity, "skeleton/swing", new SoundParams(entity));
    }

    private static void A_SkullAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        entity.PlayAttackSound();
        A_FaceTarget(entity);

        entity.Velocity = Vec3D.UnitSphere(entity.AngleRadians, entity.PitchTo(entity.CenterPoint, entity.Target.Entity)) * 20;
        entity.Flags.Skullfly = true;
    }

    private static void A_SkullPop(Entity entity)
    {
         // TODO
    }

    private static void A_SoundPitch(Entity entity)
    {
         // TODO
    }

    private static void A_SoundVolume(Entity entity)
    {
         // TODO
    }

    private static void A_SpawnDebris(Entity entity)
    {
         // TODO
    }

    private static void A_SpawnItem(Entity entity)
    {
         // TODO
    }

    private static void A_SpawnItemEx(Entity entity)
    {
         // TODO
    }

    private static void A_SpawnParticle(Entity entity)
    {
         // TODO
    }

    private static void A_SpawnProjectile(Entity entity)
    {
         // TODO
    }

    private static void A_SpawnSound(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "brain/cube", new SoundParams(entity, false, Attenuation.Default));
    }

    private static void A_SpidRefire(Entity entity)
    {
        Refire(entity, 10);
    }

    private static void Refire(Entity entity, int randomChance)
    {
        A_FaceTarget(entity);

        if (WorldStatic.Random.NextByte() < randomChance)
            return;

        if (entity.Target.Entity == null || entity.Target.Entity.IsDead ||
            !WorldStatic.World.CheckLineOfSight(entity, entity.Target.Entity))
        {
            entity.SetSeeState();
        }
    }

    private static void A_SPosAttackUseAtkSound(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        entity.PlayAttackSound();
        A_FaceTarget(entity);

        // could remove GetAutoAimEntity if FireHitscanBullets took optional auto aim angle
        WorldStatic.World.GetAutoAimEntity(entity, entity.HitscanAttackPos, entity.AngleRadians, Constants.EntityShootDistance, out double pitch, out _);

        for (int i = 0; i < 3; i++)
        {
            double angle = entity.AngleRadians + WorldStatic.Random.NextDiff() * Constants.PosRandomSpread / 255;
            WorldStatic.World.FireHitscan(entity, angle, pitch, Constants.EntityShootDistance, 3 * ((WorldStatic.Random.NextByte() % 5) + 1));
        }
    }

    private static void A_SprayDecal(Entity entity)
    {
         // TODO
    }

    private static void A_StartFire(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "vile/firestrt", new SoundParams(entity));
    }

    private static void A_Stop(Entity entity)
    {
         // TODO
    }

    private static void A_StopSound(Entity entity)
    {
         // TODO
    }

    private static void A_StopSoundEx(Entity entity)
    {
         // TODO
    }

    private static void A_SwapTeleFog(Entity entity)
    {
         // TODO
    }

    private static void A_TakeFromChildren(Entity entity)
    {
         // TODO
    }

    private static void A_TakeFromSiblings(Entity entity)
    {
         // TODO
    }

    private static void A_TakeFromTarget(Entity entity)
    {
         // TODO
    }

    private static void A_TakeInventory(Entity entity)
    {
         // TODO
    }

    private static void A_Teleport(Entity entity)
    {
         // TODO
    }

    private static void A_ThrowGrenade(Entity entity)
    {
         // TODO
    }

    private static void A_TossGib(Entity entity)
    {
         // TODO
    }

    public static void A_Tracer(Entity entity)
    {
        if ((WorldStatic.World.Gametick & 3) != 0)
            return;

        SpawnTracerPuff(entity);
        WorldStatic.World.TracerSeek(entity, Math.PI, Constants.TracerAngle, DoomTracerVelocity);
    }

    private static void SpawnTracerPuff(Entity entity)
    {
        WorldStatic.EntityManager.Create("RevenantTracerSmoke", entity.Position);

        Entity? puff = WorldStatic.EntityManager.Create("BulletPuff", entity.Position);
        if (puff != null)
        {
            puff.Position.Z = entity.Position.Z + (WorldStatic.Random.NextDiff() * Constants.PuffRandZ);
            puff.SetRandomizeTicks();
            puff.Velocity.Z = 1;
        }
    }

    private static double DoomTracerVelocity(Entity tracer, Entity target)
    {
        double distance = tracer.Position.ApproximateDistance2D(target.Position);
        double z = target.Position.Z - tracer.Position.Z + 40;
        double slope = GetTracerSlope(z, distance, tracer.Definition.Properties.MissileMovementSpeed);

        if (slope < tracer.Velocity.Z)
            return tracer.Velocity.Z - 0.125;

        return tracer.Velocity.Z + 0.125;
    }

    private static void A_Tracer2(Entity entity)
    {
         // TODO
    }

    private static void A_TransferPointer(Entity entity)
    {
         // TODO
    }

    private static void A_TroopAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);

        if (entity.InMeleeRange(entity.Target.Entity))
        {
            int damage = ((WorldStatic.EntityManager.World.Random.NextByte() % 8) + 1) * 3;
            WorldStatic.World.DamageEntity(entity.Target.Entity, entity, damage, DamageType.AlwaysApply, Thrust.Horizontal);
            WorldStatic.SoundManager.CreateSoundOn(entity, "imp/melee", new SoundParams(entity));
            return;
        }

        if (WorldStatic.DoomImpBall != null)
            FireEnemyProjectile(entity, entity.Target.Entity, WorldStatic.DoomImpBall);
    }

    private static void A_TurretLook(Entity entity)
    {
         // TODO
    }

    private static void A_UnHideThing(Entity entity)
    {
         // TODO
    }

    private static void A_UnSetFloorClip(Entity entity)
    {
         // TODO
    }

    private static void A_UnSetInvulnerable(Entity entity)
    {
         // TODO
    }

    private static void A_UnSetReflective(Entity entity)
    {
         // TODO
    }

    private static void A_UnSetReflectiveInvulnerable(Entity entity)
    {
         // TODO
    }

    private static void A_UnSetShootable(Entity entity)
    {
         // TODO
    }

    private static void A_UnsetFloat(Entity entity)
    {
         // TODO
    }

    private static void A_UnsetSolid(Entity entity)
    {
         // TODO
    }

    private static void A_VileAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);

        if (!WorldStatic.World.CheckLineOfSight(entity, entity.Target.Entity))
            return;

        WorldStatic.SoundManager.CreateSoundOn(entity, "vile/stop", new SoundParams(entity));
        WorldStatic.World.DamageEntity(entity.Target.Entity, entity, 20, DamageType.Normal, Thrust.Horizontal);
        entity.Target.Entity.Velocity.Z = 1000.0 / entity.Target.Entity.Definition.Properties.Mass;

        if (entity.Tracer.Entity == null)
            return;

        Entity fire = entity.Tracer.Entity;
        Vec2D newPos = entity.Target.Entity.Position.XY - (Vec2D.UnitCircle(entity.AngleRadians) * 24);
        fire.Position = newPos.To3D(entity.Target.Entity.Position.Z);
        WorldStatic.World.RadiusExplosion(fire, entity, 70, 70);
    }

    public static void A_VileChase(Entity entity)
    {
        // Doom just used VILE_HEAL1 state. With dehacked this means you can give a random thing A_VileChase and it will use this state.
        EntityFrame? healState;
        if (entity.Definition.HealFrame != null)
            healState = entity.Definition.HealFrame;
        else
            healState = WorldStatic.World.ArchiveCollection.EntityFrameTable.GetVileHealFrame();

        if (healState == null)
        {
            A_Chase(entity);
            return;
        }

        if (!WorldStatic.World.HealChase(entity, healState, "vile/raise"))
            A_Chase(entity);
    }

    private static void A_VileStart(Entity entity)
    {
        WorldStatic.SoundManager.CreateSoundOn(entity, "vile/start", new SoundParams(entity));
    }

    private static void A_VileTarget(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);
        Entity? fire = WorldStatic.EntityManager.Create("ArchvileFire", entity.Position);
        if (fire != null)
        {
            fire.SetOwner(entity);
            entity.SetTracer(fire);
            fire.SetTarget(entity);
            fire.SetTracer(entity.Target.Entity);
            A_Fire(fire);
        }
    }

    private static void A_Wander(Entity entity)
    {
         // TODO
    }

    private static void A_Warp(Entity entity)
    {
         // TODO
    }

    private static void A_WeaponOffset(Entity entity)
    {
         // TODO
    }

    private static void A_Weave(Entity entity)
    {
         // TODO
    }

    private static void A_WolfAttack(Entity entity)
    {
         // TODO
    }

    private static void A_ZoomFactor(Entity entity)
    {
         // TODO
    }

    private static void HealThing(Entity entity)
    {
        if (entity.PickupPlayer == null)
            return;

        if (entity.PickupPlayer.Health < entity.PickupPlayer.Properties.Health)
            entity.PickupPlayer.Health = entity.PickupPlayer.Properties.Health;
    }

    private static void A_Die(Entity entity)
    {
        entity.Kill(null);
    }

    private static void A_RandomJump(Entity entity)
    {
        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (WorldStatic.World.Random.NextByte() < entity.Frame.DehackedMisc2 &&
            entityFrameTable.VanillaFrameMap.TryGetValue(entity.Frame.DehackedMisc1, out EntityFrame? newFrame))
        {
            entity.FrameState.SetState(newFrame);
        }
    }

    private static void A_PlaySound(Entity entity)
    {
        int soundIndex = entity.Frame.DehackedMisc1;

        Attenuation attenuation = entity.Frame.DehackedMisc2 > 0 ? Attenuation.None : Attenuation.Default;
        PlayDehackedSound(entity, soundIndex, attenuation);
    }

    private static void A_Detonate(Entity entity)
    {
        WorldStatic.World.RadiusExplosion(entity, entity.Target.Entity ?? entity, entity.Properties.Damage.Value, entity.Properties.Damage.Value);
    }

    private static void A_Spawn(Entity entity)
    {
        if (!GetDehackedActorName(entity, entity.Frame.DehackedMisc1, out string? name))
            return;

        Vec3D pos = entity.Position;
        pos.Z += entity.Frame.DehackedMisc2;
        WorldStatic.EntityManager.Create(name, pos);
    }

    private static void A_Face(Entity entity)
    {
        entity.AngleRadians = MathHelper.ToRadians(entity.Frame.DehackedMisc1);
    }

    private static void A_Turn(Entity entity)
    {
        entity.AngleRadians += MathHelper.ToRadians(entity.Frame.DehackedMisc1);
    }

    private static void A_Scratch(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        A_FaceTarget(entity);
        if (entity.InMeleeRange(entity.Target.Entity))
        {
            PlayDehackedSound(entity, entity.Frame.DehackedMisc2, Attenuation.Default);
            WorldStatic.World.DamageEntity(entity.Target.Entity, entity, entity.Frame.DehackedMisc1, DamageType.AlwaysApply, Thrust.Horizontal);
        }
    }

    // A_Mushroom from WinMBF.
    // Credit to Lee Killough et al.
    private static void A_Mushroom(Entity entity)
    {
        int count = entity.Properties.Damage.Value;
        double misc1 = entity.Frame.DehackedMisc1 > 0 ? MathHelper.FromFixed(entity.Frame.DehackedMisc1) : 4;
        double misc2 = entity.Frame.DehackedMisc2 > 0 ? MathHelper.FromFixed(entity.Frame.DehackedMisc2) : 0.5;

        Vec3D velocity = new(misc2, misc2, misc2);
        Vec3D oldPos = entity.Position;
        double oldAngle = entity.AngleRadians;

        A_Explode(entity);

        if (WorldStatic.FatShot != null)
        {
            for (int i = -count; i <= count; i += 8)
            {
                for (int j = -count; j <= count; j += 8)
                {
                    Vec3D firePos = entity.Position;
                    firePos.X += i;
                    firePos.Y += j;
                    firePos.Z += MathHelper.ApproximateDistance(i, j) * misc1;

                    entity.AngleRadians = entity.Position.Angle(firePos);
                    Entity? projectile = WorldStatic.World.FireProjectile(entity, entity.AngleRadians, 0, 0, false, WorldStatic.FatShot, out _);
                    if (projectile != null)
                    {
                        // Need to use fixed point calculations to get this right
                        int dist = MathHelper.ToFixed(entity.Position.ApproximateDistance2D(firePos));
                        dist /= MathHelper.ToFixed(projectile.Definition.Properties.MissileMovementSpeed);
                        dist = Math.Clamp(dist, 1, int.MaxValue);
                        projectile.Velocity.Z = MathHelper.FromFixed(MathHelper.ToFixed(firePos.Z - entity.Position.Z) / dist);
                        projectile.Velocity *= velocity;
                        projectile.Flags.NoGravity = false;
                    }
                }
            }
        }

        entity.Position = oldPos;
        entity.AngleRadians = oldAngle;
    }

    private static Line? DummyLine;
    private static LineSpecial DummyLineSpecial = new(ZDoomLineSpecialType.None);
    private static Sector DummySector = Sector.CreateDefault();

    public static void A_LineEffect(Entity entity)
    {
        SpecialArgs specialArgs = new();
        if (!CreateLineEffectSpecial(entity.Frame, DummyLineSpecial, out var flags, ref specialArgs))
            return;

        // MBF used the first line in the map - this is a little too janky so instead create a dummy inaccessible one...
        // Because the same line was reused single activations will be broken with further calls of A_LineEffect
        DummyLine ??= CreateDummyLine(flags, specialArgs, DummySector);

        DummyLine.Special = DummyLineSpecial;
        DummyLine.Args = specialArgs;
        DummyLine.Flags = flags;

        EntityActivateSpecial args = new(ActivationContext.CrossLine, entity, DummyLine);
        WorldStatic.World.SpecialManager.TryAddActivatedLineSpecial(args);
    }

    public static bool CreateLineEffectSpecial(EntityFrame frame, LineSpecial lineSpecial, 
        out LineFlags flags, ref SpecialArgs specialArgs)
    {
        flags = new LineFlags(MapLineFlags.Doom(0));
        var specialType = VanillaLineSpecTranslator.Translate(ref flags, (VanillaLineSpecialType)frame.DehackedMisc1,
            frame.DehackedMisc2, ref specialArgs, out LineActivationType activationType, out LineSpecialCompatibility compat);

        if (specialType == ZDoomLineSpecialType.None)
            return false;

        lineSpecial.Set(specialType, activationType, compat);
        return true;
    }

    public static Line CreateDummyLine(LineFlags flags, SpecialArgs args, Sector sector)
    {
        var wall = new Wall(0, Constants.NoTextureIndex, WallLocation.Middle);
        var side = new Side(0, Vec2I.Zero, wall, wall, wall, sector);
        var seg = new Seg2D(Vec2D.Zero, Vec2D.One);
        return new Line(0, 0, seg, side, null, flags, LineSpecial.Default, args);
    }

    public static void A_WeaponBulletAttack(Entity entity)
    {
        if (entity.PlayerObj == null || !GetPlayerWeaponFrame(entity, out EntityFrame? frame))
            return;

        double spreadAngle = MathHelper.ToRadians(MathHelper.FromFixed(frame.DehackedArgs1));
        double spreadPitch = MathHelper.ToRadians(MathHelper.FromFixed(frame.DehackedArgs2));
        int bullets = frame.DehackedArgs3;
        int damage = frame.DehackedArgs4;
        int mod = Math.Clamp(frame.DehackedArgs5, 0, int.MaxValue);

        WorldStatic.World.FirePlayerHitscanBullets(entity.PlayerObj, bullets, spreadAngle, spreadPitch, entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, true, DamageAttackFunction,
            new DamageFuncParams(entity, damage, mod));
    }

    public static void A_WeaponMeleeAttack(Entity entity)
    {
        if (entity.PlayerObj == null || !GetPlayerWeaponFrame(entity, out EntityFrame? frame))
            return;

        int damage = frame.DehackedArgs1;
        int mod = Math.Clamp(frame.DehackedArgs2, 1, int.MaxValue);
        double berserkFactor = MathHelper.FromFixed(frame.DehackedArgs3);
        int sound = frame.DehackedArgs4;
        double range = frame.DehackedArgs5 == 0 ? entity.Properties.MeleeRange : MathHelper.FromFixed(frame.DehackedArgs5);

        GetDehackedSound(entity, sound, out string? hitSound);
        PlayerMelee(entity.PlayerObj, damage, mod, berserkFactor, range, hitSound);
    }

    private static void A_WeaponSound(Entity entity)
    {
        if (!GetPlayerWeaponFrame(entity, out EntityFrame? frame))
            return;

        int sound = frame.DehackedArgs1;
        Attenuation attenuation = frame.DehackedArgs2 == 0 ? Attenuation.Default : Attenuation.None;
        PlayDehackedSound(entity, sound, attenuation);      
    }

    private static void A_WeaponJump(Entity entity)
    {
        if (!GetPlayerWeaponFrame(entity, out EntityFrame? frame))
            return;

        int state = frame.DehackedArgs1;
        int chance = frame.DehackedArgs2;

        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (WorldStatic.Random.NextByte() < chance && entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
            entity.PlayerObj!.Weapon!.FrameState.SetState(newFrame);
    }

    private static void A_ConsumeAmmo(Entity entity)
    {
        if (!GetPlayerWeaponFrame(entity, out EntityFrame? frame))
            return;

        int amount = frame.DehackedArgs1;
        if (amount == 0 && entity.PlayerObj != null && entity.PlayerObj.Weapon != null)
            amount = entity.PlayerObj.Weapon.Definition.Properties.Weapons.AmmoUse;

        if (amount < 0)
            entity.PlayerObj!.AddAmmo(-amount);
        else
            entity.PlayerObj!.DecreaseAmmo(amount);
    }

    public static void A_CheckAmmo(Entity entity)
    {
        if (!GetPlayerWeaponFrame(entity, out EntityFrame? frame))
            return;

        Weapon weapon = entity.PlayerObj!.Weapon!;
        int state = frame.DehackedArgs1;
        int amount = frame.DehackedArgs2 == 0 ? weapon.Definition.Properties.Weapons.AmmoUse : frame.DehackedArgs2;
        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (entity.PlayerObj!.Inventory.Amount(weapon.Definition.Properties.Weapons.AmmoType) < amount &&
            entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
            weapon.FrameState.SetState(newFrame);
    }

    public static void A_FireRailGun(Entity entity)
    {
        if (entity.PlayerObj == null)
            return;

        entity.PlayerObj.DecreaseAmmoCompatibility();
        WorldStatic.SoundManager.CreateSoundOn(entity, "weapons/railgf", new SoundParams(entity, channel: entity.WeaponSoundChannel));
        entity.PlayerObj.Weapon?.SetFlashState();
        WorldStatic.World.FireHitscan(entity, entity.AngleRadians, entity.PlayerObj.PitchRadians, 8192, 150, HitScanOptions.PassThroughEntities | HitScanOptions.DrawRail);
    }

    private static void A_RefireTo(Entity entity)
    {
        if (!GetPlayerWeaponFrame(entity, out EntityFrame? frame))
            return;

        int state = frame.DehackedArgs1;
        bool checkAmmo = frame.DehackedArgs2 == 0;

        if (entity.PlayerObj!.PendingWeapon != null || entity.IsDead || !entity.PlayerObj!.TickCommand.Has(TickCommands.Attack))
            return;

        if (checkAmmo && !entity.PlayerObj!.CheckAmmo())
            return;

        Weapon weapon = entity.PlayerObj!.Weapon!;
        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
            weapon.FrameState.SetState(newFrame);
    }

    private static void A_GunFlashTo(Entity entity)
    {
        if (!GetPlayerWeaponFrame(entity, out EntityFrame? frame))
            return;

        Weapon weapon = entity.PlayerObj!.Weapon!;
        int state = frame.DehackedArgs1;
        bool thirdPersonFrame = frame.DehackedArgs2 == 0;

        if (thirdPersonFrame)
            entity.PlayerObj!.FrameState.SetState(Constants.FrameStates.Missile);

        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
            weapon.FlashState.SetState(newFrame);
    }

    private static void A_WeaponAlert(Entity entity)
    {
        WorldStatic.World.NoiseAlert(entity, entity);
    }

    private static void A_SpawnObject(Entity entity)
    {
        if (!GetDehackedActorName(entity, entity.Frame.DehackedArgs1, out string? name))
            return;

        double angle = entity.AngleRadians + MathHelper.ToRadians(MathHelper.FromFixed(entity.Frame.DehackedArgs2));
        double forwadDist = MathHelper.FromFixed(entity.Frame.DehackedArgs3);
        double sideDist = MathHelper.FromFixed(entity.Frame.DehackedArgs4);
        double forwardVel = MathHelper.FromFixed(entity.Frame.DehackedArgs6);
        double sideVel = MathHelper.FromFixed(entity.Frame.DehackedArgs7);
        double zOffset = MathHelper.FromFixed(entity.Frame.DehackedArgs5);
        double zVelocity = MathHelper.FromFixed(entity.Frame.DehackedArgs8);

        Vec2D forwardUnit = Vec2D.UnitCircle(angle);
        Vec2D sideUnit = Vec2D.UnitCircle(angle + MathHelper.QuarterPi);
        Vec3D offset = ((forwardUnit * forwadDist) + (sideUnit * sideDist)).To3D(zOffset);
        Vec3D velocity = ((forwardUnit * forwardVel) + (sideUnit * sideVel)).To3D(zVelocity);

        Entity? createdEntity = WorldStatic.EntityManager.Create(name, entity.Position + offset);
        if (createdEntity == null)
            return;

        createdEntity.AngleRadians = angle;
        createdEntity.Velocity = velocity;

        if (!createdEntity.Flags.Missile && !createdEntity.Flags.MbfBouncer)
            return;

        if (entity.Flags.Missile || entity.Flags.MbfBouncer)
        {
            createdEntity.SetOwner(entity.Owner.Entity);
            createdEntity.SetTracer(entity.Tracer.Entity);
        }
        else
        {
            createdEntity.SetOwner(entity);
            createdEntity.SetTarget(entity);
            createdEntity.SetTracer(entity.Tracer.Entity);
        }
    }

    // The pitch gets inverted in dsda...
    private static double GetDehackedProjectilePitch(int fixedPitch) =>
        MathHelper.ToRadians(MathHelper.FromFixed(fixedPitch)) * -1;

    private static void A_WeaponProjectile(Entity entity)
    {
        if (entity.PlayerObj == null || !GetPlayerWeaponFrame(entity, out EntityFrame? frame))
            return;

        if (!GetDehackedActorName(entity, frame.DehackedArgs1, out string? name))
            return;

        var projectileDef = WorldStatic.EntityManager.DefinitionComposer.GetByName(name);
        if (projectileDef == null)
            return;

        double angle = MathHelper.ToRadians(MathHelper.FromFixed(frame.DehackedArgs2));
        double pitch = GetDehackedProjectilePitch(frame.DehackedArgs3);
        double offsetXY = MathHelper.FromFixed(frame.DehackedArgs4);
        double zOffset = MathHelper.FromFixed(frame.DehackedArgs5);
        FireProjectile(entity, null, projectileDef, angle, pitch, offsetXY, zOffset);
    }

    private static void A_MonsterProjectile(Entity entity)
    {
        if (entity.Target.Entity == null || !GetDehackedActorName(entity, entity.Frame.DehackedArgs1, out string? name))
            return;
        // TODO should cache these definitions
        var projectileDef = WorldStatic.EntityManager.DefinitionComposer.GetByName(name);
        if (projectileDef == null)
            return;

        double angle = MathHelper.ToRadians(MathHelper.FromFixed(entity.Frame.DehackedArgs2));
        double pitchOffset = GetDehackedProjectilePitch(entity.Frame.DehackedArgs3);
        double offsetXY = MathHelper.FromFixed(entity.Frame.DehackedArgs4);
        double zOffset = MathHelper.FromFixed(entity.Frame.DehackedArgs5);

        A_FaceTarget(entity);
        var projectile = FireProjectile(entity, entity.Target.Entity, projectileDef, angle, pitchOffset, offsetXY, zOffset);
        if (projectile != null)
            projectile.SetTracer(entity.Target.Entity);
    }

    private static void A_MonsterBulletAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        double spreadAngle = MathHelper.ToRadians(MathHelper.FromFixed(entity.Frame.DehackedArgs1));
        double spreadPitch = MathHelper.ToRadians(MathHelper.FromFixed(entity.Frame.DehackedArgs2));
        int bullets = entity.Frame.DehackedArgs3;
        int damage = entity.Frame.DehackedArgs4;
        int mod = Math.Clamp(entity.Frame.DehackedArgs5, 0, int.MaxValue);

        A_FaceTarget(entity);
        entity.PlayAttackSound();
        WorldStatic.World.GetAutoAimEntity(entity, entity.HitscanAttackPos, entity.AngleRadians, Constants.EntityShootDistance, out double pitch, out _);

        for (int i = 0; i < bullets; i++)
        {
            //Entity entity = (Entity)damageParams.Object!;
            //return damageParams.Arg0 * ((entity.World.Random.NextByte() % damageParams.Arg1) + 1);
            double angle = entity.AngleRadians + (WorldStatic.Random.NextDiff() * spreadAngle / 255);
            double newPitch = pitch + (WorldStatic.Random.NextDiff() * spreadPitch / 255);
            WorldStatic.World.FireHitscan(entity, angle, newPitch, Constants.EntityShootDistance,
               damage * ((WorldStatic.Random.NextByte() % mod) + 1));
        }

        //entity.World.FireHitscanBullets(entity, bullets, spreadAngle, spreadPitch, pitch, Constants.EntityShootDistance, true, DamageAttackFunction,
        //    new DamageFuncParams(entity, damage, mod));
    }

    private static void A_MonsterMeleeAttack(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        int damage = entity.Frame.DehackedArgs1;
        int mod = Math.Clamp(entity.Frame.DehackedArgs2, 1, int.MaxValue);
        int sound = entity.Frame.DehackedArgs3;
        double range = entity.Frame.DehackedArgs4 == 0 ? entity.Properties.MeleeRange : MathHelper.FromFixed(entity.Frame.DehackedArgs4);

        if (entity.InMeleeRange(entity.Target.Entity, range))
        {
            damage = (WorldStatic.Random.NextByte() % mod + 1) * damage;
            WorldStatic.World.DamageEntity(entity.Target.Entity, entity, damage, DamageType.AlwaysApply, Thrust.Horizontal);
            GetDehackedSound(entity, sound, out string? hitSound);
            if (!string.IsNullOrEmpty(hitSound))
                WorldStatic.SoundManager.CreateSoundOn(entity, hitSound, new SoundParams(entity));
        }
    }

    private static void A_RadiusDamage(Entity entity)
    {
        int maxDamage = entity.Frame.DehackedArgs1;
        Entity? attackSource = entity.Owner.Entity ?? entity.Target.Entity;
        WorldStatic.World.RadiusExplosion(entity, attackSource ?? entity, entity.Frame.DehackedArgs2, maxDamage);
    }

    private static void A_NoiseAlert(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        WorldStatic.World.NoiseAlert(entity.Target.Entity, entity);
    }

    public static void A_HealChase(Entity entity)
    {
        int state = entity.Frame.DehackedArgs1;
        int sound = entity.Frame.DehackedArgs2;

        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (!entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
        {
            A_Chase(entity);
            return;
        }

        GetDehackedSound(entity, sound, out string? healSound);
        if (!WorldStatic.World.HealChase(entity, newFrame, healSound ?? string.Empty))
            A_Chase(entity);
    }

    public static void A_SeekTracer(Entity entity)
    {
        double threshold = MathHelper.ToRadians(MathHelper.FromFixed(entity.Frame.DehackedArgs1));
        double maxTurnAngle = MathHelper.ToRadians(MathHelper.FromFixed(entity.Frame.DehackedArgs2));

        WorldStatic.World.TracerSeek(entity, threshold, maxTurnAngle, SeekTracerVelocityZ);
    }

    private static double SeekTracerVelocityZ(Entity tracer, Entity target)
    {
        double distance = tracer.Position.ApproximateDistance2D(target.Position);
        double z = target.Position.Z + (target.Height / 2) - tracer.Position.Z;
        return GetTracerSlope(z, distance, tracer.Definition.Properties.MissileMovementSpeed);
    }

    private static void A_FindTracer(Entity entity)
    {
        if (entity.Tracer.Entity != null)
            return;

        double fov = MathHelper.ToRadians(MathHelper.FromFixed(entity.Frame.DehackedArgs1));
        int blocks = entity.Frame.DehackedArgs2 == 0 ? 10 : entity.Frame.DehackedArgs2;
        WorldStatic.World.SetNewTracerTarget(entity, fov, blocks * 128);
    }

    private static void A_ClearTracer(Entity entity)
    {
        entity.SetTracer(null);
    }

    private static void A_JumpIfHealthBelow(Entity entity)
    {
        int state = entity.Frame.DehackedArgs1;
        int health = entity.Frame.DehackedArgs2;

        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (entity.Health < health && entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
            entity.FrameState.SetState(newFrame);
    }

    private static void A_JumpIfTargetInSight(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        int state = entity.Frame.DehackedArgs1;
        double fov = MathHelper.ToRadians(MathHelper.FromFixed(entity.Frame.DehackedArgs2));
        JumpToStateIfInSight(entity, entity.Target.Entity, state, fov);
    }

    private static void A_JumpIfTargetCloser(Entity entity)
    {
        if (entity.Target.Entity == null)
            return;

        int state = entity.Frame.DehackedArgs1;
        double distance = MathHelper.FromFixed(entity.Frame.DehackedArgs2);

        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (distance > entity.Position.ApproximateDistance2D(entity.Target.Entity.Position) &&
            entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
            entity.FrameState.SetState(newFrame);
    }

    private static void A_JumpIfTracerInSight(Entity entity)
    {
        if (entity.Tracer.Entity == null)
            return;

        int state = entity.Frame.DehackedArgs1;
        double fov = MathHelper.FromFixed(entity.Frame.DehackedArgs2);
        JumpToStateIfInSight(entity, entity.Tracer.Entity, state, fov);
    }

    private static void A_JumpIfTracerCloser(Entity entity)
    {
        if (entity.Tracer.Entity == null)
            return;

        int state = entity.Frame.DehackedArgs1;
        double distance = MathHelper.FromFixed(entity.Frame.DehackedArgs2);

        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (distance > entity.Position.ApproximateDistance2D(entity.Tracer.Entity.Position) &&
            entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
            entity.FrameState.SetState(newFrame);
    }

    private static void A_JumpIfFlagsSet(Entity entity)
    {
        int state = entity.Frame.DehackedArgs1;
        uint flags = (uint)entity.Frame.DehackedArgs2;
        uint flags2 = (uint)entity.Frame.DehackedArgs3;

        if (flags != 0 && !DehackedApplier.CheckEntityFlags(entity, flags))
            return;
        if (flags2 != 0 && !DehackedApplier.CheckEntityFlagsMbf21(entity, flags))
            return;

        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
            entity.FrameState.SetState(newFrame);
    }

    private static void A_AddFlags(Entity entity)
    {
        uint flags1 = (uint)entity.Frame.DehackedArgs1;
        uint flags2 = (uint)entity.Frame.DehackedArgs2;

        DehackedApplier.SetEntityFlags(entity.Properties, ref entity.Flags, flags1, false);
        DehackedApplier.SetEntityFlagsMbf21(entity.Properties, ref entity.Flags, flags2, false);
    }

    private static void A_RemoveFlags(Entity entity)
    {
        uint flags1 = ~(uint)entity.Frame.DehackedArgs1;
        uint flags2 = ~(uint)entity.Frame.DehackedArgs2;

        DehackedApplier.SetEntityFlags(entity.Properties, ref entity.Flags, flags1, true);
        DehackedApplier.SetEntityFlagsMbf21(entity.Properties, ref entity.Flags, flags2, true);
    }

    public static void A_ClosetLook(Entity entity)
    {
        if (entity.Sector.SoundTarget.Entity != null && entity.ValidEnemyTarget(entity.Sector.SoundTarget.Entity))
        {
            entity.SetTarget(entity.Sector.SoundTarget.Entity);
            entity.SetClosetChase();
        }
    }

    public static void A_ClosetChase(Entity entity)
    {
        if (entity.Target.Entity != null && entity.Target.Entity.IsDead)
            return;

        entity.SetNewChaseDirection();
    }

    private static void JumpToStateIfInSight(Entity from, Entity to, int state, double fov)
    {
        var entityFrameTable = WorldStatic.World.ArchiveCollection.Definitions.EntityFrameTable;
        if (!entityFrameTable.VanillaFrameMap.TryGetValue(state, out EntityFrame? newFrame))
            return;

        if (fov != 0 && !WorldStatic.World.InFieldOfView(from, to, fov))
            return;

        if (!WorldStatic.World.CheckLineOfSight(from, to))
            return;

        from.FrameState.SetState(newFrame);
    }

    private static bool GetDehackedActorName(Entity entity, int index, [NotNullWhen(true)] out string? name)
    {
        var dehacked = WorldStatic.World.ArchiveCollection.Definitions.DehackedDefinition;
        if (dehacked == null)
        {
            name = null;
            return false;
        }

        return dehacked.GetEntityDefinitionName(index, out name);
    }

    private static Entity? FireEnemyProjectile(Entity entity, Entity target, EntityDefinition def, double zOffset = 0)
    {
        return WorldStatic.World.FireProjectile(entity, entity.AngleRadians, entity.PitchTo(entity.Position, target),
            Constants.EntityShootDistance, false, def, out _, zOffset: zOffset);
    }

    private static Entity? FireProjectile(Entity entity, Entity? target, EntityDefinition projectileDef, double addAngle, double addPitch, double offsetXY, double zOffset)
    {
        double firePitch = 0;
        if (entity.PlayerObj != null)
            firePitch = entity.PlayerObj.PitchRadians;

        if (target != null)
            firePitch = entity.PitchTo(entity.Position, target);

        Entity? createdEntity = WorldStatic.World.FireProjectile(entity, entity.AngleRadians, firePitch, Constants.EntityShootDistance, true, projectileDef, 
            out Entity? autoAimEntity, addAngle: addAngle, addPitch: addPitch, zOffset: zOffset);
        if (createdEntity == null)
            return null;

        if (offsetXY != 0)
        {
            Vec2D offset = Vec2D.UnitCircle(entity.AngleRadians - MathHelper.HalfPi) * offsetXY;
            createdEntity.Position = createdEntity.Position + offset.To3D(0);
        }

        createdEntity.SetTracer(autoAimEntity);
        return createdEntity;
    }

    private static void PlayerMelee(Player player, int damageBase, int mod, double berserkFactor, double range, string? hitSound)
    {
        double damage = (WorldStatic.Random.NextByte() % mod + 1) * damageBase;
        if (player.Inventory.IsPowerupActive(PowerupType.Strength))
            damage *= berserkFactor;

        double angle = player.AngleRadians + (WorldStatic.Random.NextDiff() * Constants.MeleeAngle / 255);
        double pitch = player.PitchRadians;
        if (WorldStatic.World.Config.Game.AutoAim)
            WorldStatic.World.GetAutoAimEntity(player, player.HitscanAttackPos, player.AngleRadians, range, out pitch, out _);
        Entity? hitEntity = WorldStatic.World.FireHitscan(player, angle, pitch, range, (int)damage);
        if (hitEntity == null)
            return;

        player.AngleRadians = player.Position.Angle(hitEntity.Position);
        if (!string.IsNullOrEmpty(hitSound))
            WorldStatic.World.SoundManager.CreateSoundOn(player, hitSound, new SoundParams(player, channel: player.WeaponSoundChannel));
    }

    private static bool GetPlayerWeaponFrame(Entity entity, [NotNullWhen(true)] out EntityFrame? frame)
    {
        if (entity.PlayerObj == null || entity.PlayerObj.Weapon == null)
        {
            frame = null;
            return false;
        }

        frame = entity.PlayerObj.Weapon.FrameState.Frame;
        return true;
    }

    private static void PlayDehackedSound(Entity entity, int soundIndex, Attenuation attenuation)
    {
        if (!GetDehackedSound(entity, soundIndex, out string? soundName))
            return;

        WorldStatic.SoundManager.CreateSoundOn(entity, soundName, new SoundParams(entity, attenuation: attenuation));
    }

    private static bool GetDehackedSound(Entity entity, int soundIndex, [NotNullWhen(true)] out string? soundName)
    {
        var dehacked = WorldStatic.World.ArchiveCollection.Definitions.DehackedDefinition;
        if (dehacked == null || !dehacked.GetSoundName(soundIndex, out soundName))
        {
            soundName = string.Empty;
            return false;
        }

        return true;
    }

    private static double GetTracerSlope(double z, double distance, double speed)
    {
        distance /= speed;
        if (distance < 1)
            distance = 1;
        return z / distance;
    }

    private static int DamageAttackFunction(DamageFuncParams damageParams)
    {
        Entity entity = (Entity)damageParams.Object!;
        return damageParams.Arg0 * ((WorldStatic.World.Random.NextByte() % damageParams.Arg1) + 1);
    }
}
