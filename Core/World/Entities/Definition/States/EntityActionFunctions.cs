using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio;
using Helion.Dehacked;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using Helion.World.Special;
using NLog;

namespace Helion.World.Entities.Definition.States
{
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
                entity.World.SoundManager.CreateSoundOn(entity, "weapons/bfgf", entity.WeaponSoundChannel, DataCache.Instance.GetSoundParams(entity));
        }

        private static void A_BFGSpray(Entity entity)
        {
            if (entity.Owner == null)
                return;

            for (int i = 0; i < 40; i++)
            {
                double angle = entity.AngleRadians - MathHelper.QuarterPi + (MathHelper.HalfPi / 40 * i);
                if (!entity.World.GetAutoAimEntity(entity.Owner, entity.Owner.HitscanAttackPos, angle, Constants.EntityShootDistance, out _,
                    out Entity? hitEntity) || hitEntity == null)
                    continue;

                int damage = 0;
                for (int j = 0; j < 15; j++)
                    damage += (entity.World.Random.NextByte() & 7) + 1;

                entity.World.EntityManager.Create("BFGExtra", hitEntity.CenterPoint);
                entity.World.DamageEntity(hitEntity, entity, damage, false, Thrust.Horizontal);
            }
        }

        private static void A_BabyMetal(Entity entity)
        {
            entity.SoundManager.CreateSoundOn(entity, "baby/walk", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
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
            entity.World.BossDeath(entity);
        }

        private static void A_BrainAwake(Entity entity)
        {
            entity.SoundManager.CreateSoundOn(entity, "brain/sight", SoundChannelType.Auto, 
                DataCache.Instance.GetSoundParams(entity, false, Attenuation.None));
        }

        private static void A_BrainDie(Entity entity)
        {
            entity.World.ExitLevel(LevelChangeType.Next);
        }

        private static void A_BrainExplode(Entity entity)
        {
            Vec3D pos = new Vec3D(entity.Position.X + (entity.World.Random.NextDiff() * 0.03125),
                entity.Position.Y, 128 + (entity.World.Random.NextByte() * 2));
            BrainExplodeRocket(entity, pos);
        }

        private static void A_BrainPain(Entity entity)
        {
            entity.SoundManager.CreateSoundOn(entity, "brain/pain", SoundChannelType.Auto, 
                DataCache.Instance.GetSoundParams(entity, false, Attenuation.None));
        }

        private static void A_BrainScream(Entity entity)
        {
            for (double x = entity.Position.X - 196; x < entity.Position.X + 320; x += 8)
            {
                Vec3D pos = new Vec3D(x, entity.Position.Y - 320, 128 + entity.World.Random.NextByte() + 1);
                BrainExplodeRocket(entity, pos);
            }

            entity.SoundManager.CreateSoundOn(entity, "brain/death", SoundChannelType.Auto, 
                DataCache.Instance.GetSoundParams(entity, false, Attenuation.None));
        }

        private static void BrainExplodeRocket(Entity entity, in Vec3D pos)
        {
            Entity? rocket = entity.EntityManager.Create("BossRocket", pos);
            if (rocket != null)
            {
                rocket.SetRandomizeTicks(7);
                rocket.Velocity.Z = (entity.World.Random.NextByte() << 9) / 65536.0;
            }
        }

        private static void A_BrainSpit(Entity entity)
        {
            Entity[] targets = entity.World.GetBossTargets();
            if (targets.Length == 0)
                return;

            Entity target = targets[entity.World.CurrentBossTarget++];
            entity.World.CurrentBossTarget %= targets.Length;

            double pitch = entity.PitchTo(target);
            Entity? spawnShot = entity.World.FireProjectile(entity, pitch, 0.0, false, "SpawnShot");

            if (spawnShot != null)
            {
                spawnShot.Flags.Friendly = entity.Flags.Friendly;
                double distance = entity.Position.Distance(target.Position);
                double speed = spawnShot.Definition.Properties.Speed;
                double reactionTime = distance / speed;

                spawnShot.AngleRadians = entity.Position.Angle(target.Position);
                spawnShot.Velocity = Vec3D.UnitSphere(spawnShot.AngleRadians, pitch) * speed;
                spawnShot.Target = target;
                spawnShot.ReactionTime = (int)reactionTime;
                spawnShot.Flags.BossSpawnShot = true;
            }

            entity.SoundManager.CreateSoundOn(entity, "brain/spit", SoundChannelType.Auto, 
                DataCache.Instance.GetSoundParams(entity, false, Attenuation.None));
        }

        private static void A_SpawnFly(Entity entity)
        {
            if (entity.Target == null)
            {
                entity.EntityManager.Destroy(entity);
                return;
            }
            
            if (entity.ReactionTime > 0)
                return;
                
            entity.EntityManager.Create("ArchvileFire", entity.Target.Position);
            entity.SoundManager.CreateSoundOn(entity.Target, "misc/teleport", SoundChannelType.Auto, 
                DataCache.Instance.GetSoundParams(entity));

            Entity? enemy = entity.EntityManager.Create(GetRandomBossSpawn(entity.World.Random), entity.Target.Position);
            if (enemy != null)
            {
                enemy.Flags.Friendly = entity.Flags.Friendly;
                enemy.SetNewTarget(true);
                entity.World.TelefragBlockingEntities(enemy);
            }

            entity.EntityManager.Destroy(entity);
        }

        private static string GetRandomBossSpawn(IRandom random)
        {
            byte value = random.NextByte();
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
            if (entity.Target == null)
                return;

            if (entity.InMeleeRange(entity.Target))
            {
                int damage = ((entity.EntityManager.World.Random.NextByte() % 8) + 1) * 10;
                entity.World.DamageEntity(entity.Target, entity, damage, false, Thrust.Horizontal);
                entity.SoundManager.CreateSoundOn(entity, "baron/melee", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
                return;
            }

            entity.World.FireProjectile(entity, entity.PitchTo(entity.ProjectileAttackPos, entity.Target), Constants.EntityShootDistance, false, "BaronBall");
        }

        private static void A_BspiAttack(Entity entity)
        {
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);
            entity.World.FireProjectile(entity, entity.PitchTo(entity.ProjectileAttackPos, entity.Target), Constants.EntityShootDistance, false, "ArachnotronPlasma");
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
            PosessedAttack(entity, 1, true);
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

        private static void A_Chase(Entity entity)
        {
            if (entity.ReactionTime > 0)
                entity.ReactionTime--;

            if (entity.Threshold > 0)
            {
                if (entity.Target == null || entity.Target.IsDead)
                    entity.Threshold = 0;
                else
                    entity.Threshold--;
            }

            if (entity.Target == null || entity.Target.IsDead)
            {
                if (!entity.SetNewTarget(true))
                    entity.SetSpawnState();
                return;
            }

            if (entity.Target != null && entity.IsFriend(entity.Target))
                entity.SetNewTarget(true);

            if (entity.Flags.JustAttacked)
            {
                entity.Flags.JustAttacked = false;
                if (!entity.World.SkillDefinition.IsFastMonsters(entity.World.Config))
                    entity.SetNewChaseDirection();
                return;
            }

            if (entity.Target != null && entity.HasMeleeState() && entity.InMeleeRange(entity.Target))
            {
                // ATTACK SOUND?
                entity.SetMeleeState();
            }

            entity.MoveCount--;

            if (entity.MoveCount < 0 || !entity.MoveEnemy(out TryMoveData? _))
                entity.SetNewChaseDirection();

            if ((entity.MoveCount == 0 || entity.World.SkillDefinition.IsFastMonsters(entity.World.Config)) && 
                entity.HasMissileState() && entity.CheckMissileRange())
            {
                entity.Flags.JustAttacked = true;
                entity.SetMissileState();
            }
            else if (entity.EntityManager.World.Random.NextByte() < 3)
            {
                entity.PlayActiveSound();
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
             // TODO
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
            entity.World.SoundManager.CreateSoundOn(entity, "weapons/sshotc", entity.WeaponSoundChannel, 
                DataCache.Instance.GetSoundParams(entity));
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
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);
            entity.World.FireProjectile(entity, entity.PitchTo(entity.ProjectileAttackPos, entity.Target),
                Constants.EntityShootDistance, false, "Rocket");
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
            Entity? attackSource = entity.Owner ?? entity.Target;
            entity.World.RadiusExplosion(entity, attackSource ?? entity, 128);
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

        private static void A_FaceTarget(Entity entity)
        {
            if (entity.Target == null)
                return;

            entity.AngleRadians = entity.Position.Angle(entity.Target.Position);
            if (entity.Target.Flags.Shadow)
                entity.AngleRadians += entity.World.Random.NextDiff() * Constants.ShadowRandomSpread / 255;
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
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);
            double baseAngle = entity.AngleRadians;

            entity.AngleRadians = baseAngle + fireSpread1;
            entity.World.FireProjectile(entity, entity.PitchTo(entity.ProjectileAttackPos, entity.Target),
                Constants.EntityShootDistance, false, "FatShot");

            entity.AngleRadians = baseAngle + fireSpread2;
            entity.World.FireProjectile(entity, entity.PitchTo(entity.ProjectileAttackPos, entity.Target),
                Constants.EntityShootDistance, false, "FatShot");

            entity.AngleRadians = baseAngle;
        }

        private static void A_FatRaise(Entity entity)
        {
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);
            entity.SoundManager.CreateSoundOn(entity, "fatso/raiseguns", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
        }

        private static void A_Fire(Entity entity)
        {
            if (entity.Target == null || entity.Tracer == null)
                return;

            if (!entity.World.CheckLineOfSight(entity.Target, entity.Tracer))
                return;

            Vec3D newPos = entity.Tracer.Position;
            Vec3D unit = Vec3D.UnitSphere(entity.Tracer.AngleRadians, 0.0);
            newPos.X += unit.X * 24;
            newPos.Y += unit.Y * 24;

            entity.SetPosition(newPos);
        }

        private static void A_FireAssaultGun(Entity entity)
        {
             // TODO
        }

        private static void A_FireBFG(Entity entity)
        {
            // TODO not sure of difference between A_FireBFG and A_FireOldBFG
            if (entity.PlayerObj != null)
                entity.PlayerObj.World.FireProjectile(entity, entity.PlayerObj.PitchRadians, Constants.EntityShootDistance,
                    entity.World.Config.Game.AutoAim, "BFGBall");
        }

        private static void A_FireBullets(Entity entity)
        {
             // TODO
        }

        private static void A_FireCGun(Entity entity)
        {
            if (entity.PlayerObj != null)
            {
                entity.World.SoundManager.CreateSoundOn(entity, "weapons/pistol", entity.WeaponSoundChannel, DataCache.Instance.GetSoundParams(entity));
                int offset = entity.PlayerObj.Weapon == null ? 0 : Math.Clamp(entity.PlayerObj.Weapon.FrameState.Frame.Frame, 0, 1);
                entity.PlayerObj.Weapon?.SetFlashState(offset);
                entity.World.FireHitscanBullets(entity, 1, Constants.DefaultSpreadAngle, 0,
                    entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, entity.World.Config.Game.AutoAim);
            }
        }

        private static void A_FireCrackle(Entity entity)
        {
            entity.SoundManager.CreateSoundOn(entity, "vile/firecrkl", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
        }

        private static void A_FireCustomMissile(Entity entity)
        {
             // TODO
        }

        private static void A_FireMissile(Entity entity)
        {
            if (entity.PlayerObj != null)
            {
                entity.World.FireProjectile(entity, entity.PlayerObj.PitchRadians, Constants.EntityShootDistance,
                    entity.World.Config.Game.AutoAim, "Rocket");
            }
        }

        private static void A_FireOldBFG(Entity entity)
        {
            // TODO not sure of difference between A_FireBFG and A_FireOldBFG
            if (entity.PlayerObj != null)
                entity.World.FireProjectile(entity, entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, false, "BFGBall");
        }

        private static void A_FirePistol(Entity entity)
        {
            if (entity.PlayerObj != null)
            {
                entity.World.SoundManager.CreateSoundOn(entity, "weapons/pistol", entity.WeaponSoundChannel, DataCache.Instance.GetSoundParams(entity));
                entity.PlayerObj.Weapon?.SetFlashState();
                entity.World.FireHitscanBullets(entity, 1, Constants.DefaultSpreadAngle, 0,
                    entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, entity.World.Config.Game.AutoAim);
            }
        }

        private static void A_FirePlasma(Entity entity)
        {
            if (entity.PlayerObj != null)
            {
                entity.PlayerObj.Weapon?.SetFlashState(entity.World.Random.NextByte() & 1);
                entity.World.FireProjectile(entity, entity.PlayerObj.PitchRadians, Constants.EntityShootDistance,
                    entity.World.Config.Game.AutoAim, "PlasmaBall");
            }
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
                entity.World.SoundManager.CreateSoundOn(entity, "weapons/shotgf", entity.WeaponSoundChannel, DataCache.Instance.GetSoundParams(entity));
                entity.PlayerObj.Weapon?.SetFlashState();
                entity.World.FireHitscanBullets(entity, Constants.ShotgunBullets, Constants.DefaultSpreadAngle, 0.0,
                    entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, entity.World.Config.Game.AutoAim);
            }
        }

        private static void A_FireShotgun2(Entity entity)
        {
            if (entity.PlayerObj != null)
            {
                entity.World.SoundManager.CreateSoundOn(entity, "weapons/sshotf", entity.WeaponSoundChannel, DataCache.Instance.GetSoundParams(entity));
                entity.PlayerObj.Weapon?.SetFlashState();
                entity.World.FireHitscanBullets(entity, Constants.SuperShotgunBullets, Constants.SuperShotgunSpreadAngle, Constants.SuperShotgunSpreadPitch,
                    entity.PlayerObj.PitchRadians, Constants.EntityShootDistance, entity.World.Config.Game.AutoAim);
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
             // TODO
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
                entity.SetMissileState();
                entity.PlayerObj.Weapon?.SetFlashState();
            }
        }

        private static void A_HeadAttack(Entity entity)
        {
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);

            if (entity.InMeleeRange(entity.Target))
            {
                int damage = ((entity.EntityManager.World.Random.NextByte() % 6) + 1) * 10;
                entity.World.DamageEntity(entity.Target, entity, damage, false, Thrust.Horizontal);
                entity.PlayAttackSound();
                return;
            }

            entity.World.FireProjectile(entity, entity.PitchTo(entity.ProjectileAttackPos, entity.Target),
                Constants.EntityShootDistance, false, "CacodemonBall");
        }

        private static void A_HideThing(Entity entity)
        {
             // TODO
        }

        private static void A_Hoof(Entity entity)
        {
            entity.World.SoundManager.CreateSoundOn(entity, "cyber/hoof", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
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

        private static void A_JumpIfTracerCloser(Entity entity)
        {
             // TODO
        }

        private static void A_KeenDie(Entity entity)
        {
            var world = entity.World;
            var def = world.EntityManager.DefinitionComposer.GetByName("CommanderKeen");
            if (def == null || !def.EditorId.HasValue)
                return;

            if (world.EntityAliveCount(def.EditorId.Value, false) == 0)
            {
                var sectors = world.Sectors.Where(x => x.Tag == 666);
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
            entity.World.SoundManager.CreateSoundOn(entity, "weapons/sshotl", entity.WeaponSoundChannel, DataCache.Instance.GetSoundParams(entity));
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

        private static void A_Look(Entity entity)
        {
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
            entity.World.SoundManager.CreateSoundOn(entity, "spider/walk", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
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
            entity.World.SoundManager.CreateSoundOn(entity, "weapons/sshoto", entity.WeaponSoundChannel, DataCache.Instance.GetSoundParams(entity));
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
            if (entity.Target == null)
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
            Vec3D skullPos = entity.Position;
            Vec3D startPos = entity.Position;
            skullPos.Z += 8;

            Entity? skull = entity.EntityManager.Create("LostSoul", startPos);
            if (skull == null)
                return;

            skull.Flags.Friendly = entity.Flags.Friendly;
            double step = 4 + (3 * (entity.Radius + skull.Radius) / 2);
            skullPos += Vec3D.UnitSphere(angle, 0.0) * step;
            startPos += Vec3D.UnitSphere(angle, 0.0) * (entity.Radius + skull.Radius - 2);
            skull.SetPosition(startPos);
            skull.Flags.CountKill = false;

            // Ignore parent for clip checking
            bool wasSolid = entity.Flags.Solid;
            entity.Flags.Solid = false;

            // Add some better checking from the original
            // Set the skull barely clipped into the parent
            // Then check if it can move to it's final position (TryMoveXY does step checking and won't skip lines/entities)
            if (!entity.World.IsPositionValid(skull, startPos.XY) || !entity.World.TryMoveXY(skull, skullPos.XY).Success)
            {
                skull.Kill(null);
                entity.Flags.Solid = wasSolid;
                return;
            }

            entity.Flags.Solid = wasSolid;
            skull.Target = entity.Target;
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
            PosessedAttack(entity, 1, true);
        }

        private static void PosessedAttack(Entity entity, int bullets, bool attackSound)
        {
            if (entity.Target == null)
                return;

            if (attackSound)
                entity.PlayAttackSound();

            A_FaceTarget(entity);

            // could remove GetAutoAimEntity if FireHitscanBullets took optional auto aim angle
            entity.World.GetAutoAimEntity(entity, entity.HitscanAttackPos, entity.AngleRadians, Constants.EntityShootDistance, out double pitch, out _);
            entity.AngleRadians += entity.World.Random.NextDiff() * Constants.PosRandomSpread / 255;
            entity.World.FireHitscanBullets(entity, bullets, Constants.DefaultSpreadAngle, 0,
                pitch, Constants.EntityShootDistance, false);
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
            {
                int damage = ((2 * entity.World.Random.NextByte()) % 10) + 1;
                if (entity.PlayerObj.Inventory.IsPowerupActive(PowerupType.Strength))
                    damage *= 10;

                double angle = entity.AngleRadians + (entity.World.Random.NextDiff() * Constants.MeleeAngle / 255);
                Entity? hitEntity = entity.World.FireHitscan(entity, entity.AngleRadians, 0, Constants.EntityMeleeDistance, damage);
                if (hitEntity != null)
                {
                    entity.AngleRadians = angle;
                    entity.World.SoundManager.CreateSoundOn(entity, "player/male/fist", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
                    entity.AngleRadians = entity.Position.Angle(hitEntity.Position);
                }
            }
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
            if (entity.PlayerObj != null && entity.PlayerObj.AnimationWeapon != null)
            {
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
        }

        private static void A_Raise(Entity entity)
        {
            if (entity.PlayerObj != null && entity.PlayerObj.AnimationWeapon != null)
            {
                entity.PlayerObj.WeaponOffset.Y -= Constants.WeaponRaiseSpeed;
                if (entity.PlayerObj.WeaponOffset.Y > Constants.WeaponTop)
                    return;

                entity.PlayerObj.SetWeaponUp();
                entity.PlayerObj.WeaponOffset.Y = Constants.WeaponTop;
                entity.PlayerObj.AnimationWeapon.SetReadyState();
            }
        }

        public static void A_WeaponReady(Entity entity)
        {
            if (entity.PlayerObj != null && entity.PlayerObj.Weapon != null)
            {
                entity.PlayerObj.Weapon.ReadyToFire = true;

                if (entity.PlayerObj.PendingWeapon != null || entity.PlayerObj.IsDead)
                    entity.PlayerObj.LowerWeapon();

                if (!entity.PlayerObj.IsVooDooDoll && entity.PlayerObj.Weapon.Definition.Properties.Weapons.ReadySound.Length > 0 &&
                    entity.PlayerObj.Weapon.FrameState.IsState(Constants.FrameStates.Ready))
                {
                    entity.World.SoundManager.CreateSoundOn(entity, entity.PlayerObj.Weapon.Definition.Properties.Weapons.ReadySound,
                        SoundChannelType.Weapon, DataCache.Instance.GetSoundParams(entity));
                }
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
            PosessedAttack(entity, 3, false);
        }

        private static void A_SargAttack(Entity entity)
        {
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);
            entity.PlayAttackSound();
            if (entity.InMeleeRange(entity.Target))
            {
                int damage = ((entity.World.Random.NextByte() % 10) + 1) * 4;
                entity.World.DamageEntity(entity.Target, entity, damage, false, Thrust.Horizontal);
            }
        }

        private static void A_Saw(Entity entity)
        {
            if (entity.IsPlayer)
            {
                int damage = ((2 * entity.World.Random.NextByte()) % 10) + 1;
                double angle = entity.AngleRadians + (entity.World.Random.NextDiff() * Constants.MeleeAngle / 255);
                // Doom added + 1 so the bulletpuff would include the spark state
                Entity? hitEntity = entity.World.FireHitscan(entity, angle, 0, Constants.EntityMeleeDistance + 1, damage);
                if (hitEntity == null)
                {
                    entity.World.SoundManager.CreateSoundOn(entity, "weapons/sawfull", entity.WeaponSoundChannel, DataCache.Instance.GetSoundParams(entity));
                }
                else
                {
                    entity.AngleRadians = angle;
                    entity.World.SoundManager.CreateSoundOn(entity, "weapons/sawhit", entity.WeaponSoundChannel, DataCache.Instance.GetSoundParams(entity));
                    entity.AngleRadians = entity.Position.Angle(hitEntity.Position);
                }
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

            entity.SoundManager.CreateSoundOn(entity, entity.Definition.Properties.PainSound, SoundChannelType.Auto, 
                DataCache.Instance.GetSoundParams(entity));
        }

        private static void A_PlayerScream(Entity entity)
        {
            if (!entity.IsPlayer || entity.Definition.Properties.DeathSound.Length > 0)
            {
                A_Scream(entity);
                return;
            }

            string deathSound =  entity.Health > -50 ? "*death" : "*xdeath";
            entity.SoundManager.CreateSoundOn(entity, deathSound, SoundChannelType.Voice, DataCache.Instance.GetSoundParams(entity));
        }

        private static void A_Scream(Entity entity)
        {
            if (entity.Definition.Properties.DeathSound.Length == 0)
                return;

            Attenuation attenuation = entity.Flags.Boss ? Attenuation.None : Attenuation.Default;
            entity.SoundManager.CreateSoundOn(entity, entity.Definition.Properties.DeathSound, SoundChannelType.Auto,
                DataCache.Instance.GetSoundParams(entity, attenuation: attenuation));
        }

        private static void A_XScream(Entity entity)
        {
            if (entity.IsPlayer)
                entity.SoundManager.CreateSoundOn(entity, "*gibbed", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
            else
                entity.SoundManager.CreateSoundOn(entity, "misc/gibbed", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));

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
             // TODO
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
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);

            if (entity.InMeleeRange(entity.Target))
            {
                int damage = ((entity.World.Random.NextByte() % 10) + 1) * 6;
                entity.World.DamageEntity(entity.Target, entity, damage, false, Thrust.Horizontal);
                entity.SoundManager.CreateSoundOn(entity, "skeleton/melee", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
            }
        }

        private static void A_SkelMissile(Entity entity)
        {
            if (entity.Target == null)
                return;

            Entity? fireball = entity.World.FireProjectile(entity, entity.PitchTo(entity.ProjectileAttackPos, entity.Target),
                Constants.EntityShootDistance, false, "RevenantTracer", 16);

            if (fireball != null)
                fireball.Tracer = entity.Target;
        }

        private static void A_SkelWhoosh(Entity entity)
        {
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);
            entity.SoundManager.CreateSoundOn(entity, "skeleton/swing", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
        }

        private static void A_SkullAttack(Entity entity)
        {
            if (entity.Target == null)
                return;

            entity.PlayAttackSound();
            A_FaceTarget(entity);

            entity.Velocity = Vec3D.UnitSphere(entity.AngleRadians, entity.PitchTo(entity.CenterPoint, entity.Target)) * 20;
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
            entity.SoundManager.CreateSoundOn(entity, "brain/cube", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity, false, Attenuation.Default));
        }

        private static void A_SpidRefire(Entity entity)
        {
            Refire(entity, 10);
        }

        private static void Refire(Entity entity, int randomChance)
        {
            A_FaceTarget(entity);

            if (entity.World.Random.NextByte() < randomChance)
                return;

            if (entity.Target == null || entity.Target.IsDead ||
                !entity.World.CheckLineOfSight(entity, entity.Target))
            {
                entity.SetSeeState();
            }
        }

        private static void A_SPosAttackUseAtkSound(Entity entity)
        {
            PosessedAttack(entity, 3, true);
        }

        private static void A_SprayDecal(Entity entity)
        {
             // TODO
        }

        private static void A_StartFire(Entity entity)
        {
            entity.SoundManager.CreateSoundOn(entity, "vile/firestrt", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
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

        private static void A_Tracer(Entity entity)
        {
            if (entity.Tracer == null || entity.Tracer.IsDead)
                return;

            if ((entity.World.Gametick & 3) != 0)
                return;

            SpawnTracerPuff(entity);
            SetTracerAngle(entity);

            double z = entity.Velocity.Z;
            entity.Velocity = Vec3D.UnitSphere(entity.AngleRadians, 0.0) * entity.Definition.Properties.Speed;
            entity.Velocity.Z = z;

            double distance = entity.Position.ApproximateDistance2D(entity.Tracer.Position);
            double slope = GetTracerSlope(entity.Tracer.Position.Z + 40 - entity.Position.Z, distance, entity.Definition.Properties.Speed);

            if (slope < entity.Velocity.Z)
                entity.Velocity.Z -= 0.125;
            else
                entity.Velocity.Z += 0.125;
        }

        private static void SpawnTracerPuff(Entity entity)
        {
            entity.EntityManager.Create("RevenantTracerSmoke", entity.Position);

            Entity? puff = entity.EntityManager.Create("BulletPuff", entity.Position);
            if (puff != null)
            {
                puff.SetZ(entity.Position.Z + (entity.World.Random.NextDiff() * Constants.PuffRandZ), false);
                puff.SetRandomizeTicks();
                puff.Velocity.Z = 1;
            }
        }

        private static void SetTracerAngle(Entity entity)
        {
            if (entity.Tracer == null)
                return;
            // Doom's angles were always 0-360 and did not allow negatives (thank you arithmetic overflow)
            // To keep this code familiar GetPositiveAngle will keep angle between 0 and 2pi
            double exact = MathHelper.GetPositiveAngle(entity.Position.Angle(entity.Tracer.Position));
            double currentAngle = MathHelper.GetPositiveAngle(entity.AngleRadians);
            double diff = MathHelper.GetPositiveAngle(exact - currentAngle);

            if (!MathHelper.AreEqual(exact, currentAngle))
            {
                if (diff > Math.PI)
                {
                    entity.AngleRadians = MathHelper.GetPositiveAngle(entity.AngleRadians - Constants.TracerAngle);
                    if (MathHelper.GetPositiveAngle(exact - entity.AngleRadians) < Math.PI)
                        entity.AngleRadians = exact;
                }
                else
                {
                    entity.AngleRadians = MathHelper.GetPositiveAngle(entity.AngleRadians + Constants.TracerAngle);
                    if (MathHelper.GetPositiveAngle(exact - entity.AngleRadians) > Math.PI)
                        entity.AngleRadians = exact;
                }
            }
        }

        private static double GetTracerSlope(double z, double distance, double speed)
        {
            distance /= speed;
            if (distance < 1)
                distance = 1;
            return z / distance;
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
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);

            if (entity.InMeleeRange(entity.Target))
            {
                int damage = ((entity.EntityManager.World.Random.NextByte() % 8) + 1) * 3;
                entity.World.DamageEntity(entity.Target, entity, damage, false, Thrust.Horizontal);
                entity.SoundManager.CreateSoundOn(entity, "imp/melee", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
                return;
            }

            entity.World.FireProjectile(entity, entity.PitchTo(entity.ProjectileAttackPos, entity.Target),
                Constants.EntityShootDistance, false, "DoomImpBall");
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
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);

            if (!entity.World.CheckLineOfSight(entity, entity.Target))
                return;

            entity.SoundManager.CreateSoundOn(entity, "vile/stop", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
            entity.World.DamageEntity(entity.Target, entity, 20, false, Thrust.Horizontal);
            entity.Target.Velocity.Z = 1000.0 / entity.Target.Definition.Properties.Mass;

            if (entity.Tracer == null)
                return;

            Vec3D newPos = entity.Tracer.Position;
            Vec3D unit = Vec3D.UnitSphere(entity.Tracer.AngleRadians, 0.0);
            newPos.X -= unit.X * 24;
            newPos.Y -= unit.Y * 24;

            entity.Tracer.SetPosition(newPos);
            entity.World.RadiusExplosion(entity.Tracer, entity, 70);
        }

        private static void A_VileChase(Entity entity)
        {
            Box2D nextBox = new(entity.GetNextEnemyPos(), entity.Radius);
            List<BlockmapIntersect> intersections = entity.World.BlockmapTraverser.GetBlockmapIntersections(nextBox,
                BlockmapTraverseFlags.Entities, BlockmapTraverseEntityFlags.Corpse);

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];

                if (bi.Entity == null || !bi.Entity.HasRaiseState() || bi.Entity.FrameState.Frame.Ticks != -1)
                    continue;

                if (bi.Entity.World.IsPositionBlockedByEntity(bi.Entity, bi.Entity.Position))
                    continue;

                bi.Entity.Flags.Solid = true;
                bi.Entity.SetHeight(entity.Definition.Properties.Height);

                Entity? saveTarget = entity.Target;
                entity.Target = bi.Entity;
                A_FaceTarget(entity);
                entity.Target = saveTarget;
                entity.SetHealState();

                entity.SoundManager.CreateSoundOn(bi.Entity, "vile/raise", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
                bi.Entity.SetRaiseState();
                bi.Entity.Flags.Friendly = entity.Flags.Friendly;
                break;
            }

            DataCache.Instance.FreeBlockmapIntersectList(intersections);

            A_Chase(entity);
        }

        private static void A_VileStart(Entity entity)
        {
            entity.SoundManager.CreateSoundOn(entity, "vile/start", SoundChannelType.Auto, DataCache.Instance.GetSoundParams(entity));
        }

        private static void A_VileTarget(Entity entity)
        {
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);
            Entity? fire = entity.EntityManager.Create("ArchvileFire", entity.Position);
            if (fire != null)
            {
                fire.Owner = entity;
                entity.Tracer = fire;
                fire.Target = entity;
                fire.Tracer = entity.Target;
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
            // TODO
        }

        private static void A_Die(Entity entity)
        {
            entity.Kill(null);
        }

        private static void A_RandomJump(Entity entity)
        {
            var entityFrameTable = entity.World.ArchiveCollection.Definitions.EntityFrameTable;
            if (entity.World.Random.NextByte() < entity.Frame.DehackedMisc2 &&
                entityFrameTable.VanillaFrameMap.TryGetValue(entity.Frame.DehackedMisc1, out EntityFrame? newFrame))
            {
                entity.FrameState.SetState(newFrame);
            }
        }

        private static void A_PlaySound(Entity entity)
        {
            var dehacked = entity.World.ArchiveCollection.Definitions.DehackedDefinition;
            int soundIndex = entity.Frame.DehackedMisc1;
            if (dehacked == null)
                return;

            Attenuation attenuation = entity.Frame.DehackedMisc2 > 0 ? Attenuation.None : Attenuation.Default;
            PlayDehackedSound(dehacked, entity, soundIndex, attenuation);
        }

        private static void PlayDehackedSound(DehackedDefinition dehacked, Entity entity, int soundIndex, Attenuation attenuation)
        {
            if (soundIndex < 0 || soundIndex >= dehacked.SoundStrings.Length)
                return;

            entity.World.SoundManager.CreateSoundOn(entity, dehacked.SoundStrings[soundIndex], SoundChannelType.Auto,
                DataCache.Instance.GetSoundParams(entity, attenuation: attenuation));
        }

        private static void A_Detonate(Entity entity)
        {
            entity.World.RadiusExplosion(entity, entity.Target ?? entity, entity.Properties.Damage.Value);
        }

        private static void A_Spawn(Entity entity)
        {
            var dehacked = entity.World.ArchiveCollection.Definitions.DehackedDefinition;
            int actorIndex = entity.Frame.DehackedMisc1 - 1;
            if (dehacked == null || actorIndex < 0 || actorIndex >= dehacked.ActorNames.Length)
                return;

            Vec3D pos = entity.Position;
            pos.Z += entity.Frame.DehackedMisc2;
            entity.World.EntityManager.Create(dehacked.ActorNames[actorIndex], pos);
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
            if (entity.Target == null)
                return;

            A_FaceTarget(entity);
            if (entity.InMeleeRange(entity.Target))
            {
                var dehacked = entity.World.ArchiveCollection.Definitions.DehackedDefinition;
                if (dehacked != null)
                    PlayDehackedSound(dehacked, entity, entity.Frame.DehackedMisc2, Attenuation.Default);

                entity.World.DamageEntity(entity.Target, entity, entity.Frame.DehackedMisc1, false, Thrust.Horizontal);
            }
        }

        // A_Mushroom from WinMBF.
        // Credit to Lee Killough et al.
        private static void A_Mushroom(Entity entity)
        {
            int count = entity.Properties.Damage.Value;
            double misc1 = entity.Frame.DehackedMisc1 > 0 ? MathHelper.FromFixed(entity.Frame.DehackedMisc1) : 4;
            double misc2 = entity.Frame.DehackedMisc2 > 0 ? MathHelper.FromFixed(entity.Frame.DehackedMisc2) : 0.5;

            Vec3D velocity = new Vec3D(misc2, misc2, misc2);
            Vec3D oldPos = entity.Position;
            double oldAngle = entity.AngleRadians;

            A_Explode(entity);

            for (int i = -count; i <= count; i+=8)
            {
                for (int j = -count; j <= count; j+=8)
                {
                    Vec3D firePos = entity.Position;
                    firePos.X += i;
                    firePos.Y += j;
                    firePos.Z += MathHelper.ApproximateDistance(i, j) * misc1;

                    entity.AngleRadians = entity.Position.Angle(firePos);
                    double pitch = entity.Position.Pitch(firePos, entity.Position.XY.Distance(firePos.XY));
                    Entity? projectile = entity.World.FireProjectile(entity, pitch, 0, false, "FatShot");
                    if (projectile != null)
                    {
                        projectile.Velocity *= velocity;
                        projectile.Flags.NoGravity = false;
                    }
                }
            }

            entity.SetPosition(oldPos);
            entity.AngleRadians = oldAngle;
        }

        private static Line? m_dummyLine;

        private static void A_LineEffect(Entity entity)
        {
            if (entity.World.Sectors.Count == 0)
                return;

            SpecialArgs specialArgs = new();
            var flags = new LineFlags(MapLineFlags.Doom(0));
            var specialType = VanillaLineSpecTranslator.Translate(flags, (VanillaLineSpecialType)entity.Frame.DehackedMisc1, 
                entity.Frame.DehackedMisc2, ref specialArgs, out LineActivationType activationType, out LineSpecialCompatibility? compat);

            if (specialType == ZDoomLineSpecialType.None)
                return;

            LineSpecial lineSpecial = new LineSpecial(specialType, activationType, compat);
            // MBF used the first line in the map - this is a little too janky so instead create a dummy inaccessible one...
            // Because the same line was reused single activations will be broken with further calls of A_LineEffect
            if (m_dummyLine == null)
                m_dummyLine = CreateDummyLine(flags, lineSpecial, specialArgs, entity.World.Sectors[0]);

            m_dummyLine.Args = specialArgs;
            m_dummyLine.Flags = flags;

            EntityActivateSpecialEventArgs args = new EntityActivateSpecialEventArgs(ActivationContext.CrossLine, entity, m_dummyLine);
            entity.World.SpecialManager.TryAddActivatedLineSpecial(args);
        }

        private static Line CreateDummyLine(LineFlags flags, LineSpecial special, SpecialArgs args, Sector sector)
        {
            var wall = new Wall(0, Constants.NoTextureIndex, WallLocation.Middle);
            var side = new Side(0, Vec2I.Zero, wall, wall, wall, sector);
            var seg = new Seg2D(Vec2D.Zero, Vec2D.One);
            return new Line(0, 0, seg, side, null, flags, special, args);
        }
    }
}