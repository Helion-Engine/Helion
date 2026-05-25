using Helion.Geometry.Vectors;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special.Specials;
using HelionACS;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.ACS;

public class WorldExecutor : Executor
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private byte[] SaveBuffer = new byte[1024 * 1024];
    private IWorld m_world;

    public static ThreadInfoData CreateThreadInfoData(Entity? activator, Line? line, bool frontSide)
    {
        int side = line == null ? -1 : frontSide ? 0 : 1;
        return new ThreadInfoData(activator?.Id ?? -1, line?.Id ?? -1, side, -1);
    }

    public WorldExecutor(IWorld world)
    {
        m_world = world;

        // NOTE: the following mappings come from Eternity Engine's acs_intr.cpp,
        // which was the most clear source I could find on what ACSVM considers something
        // for you to implement and what is done for you.

        #region "CodeData"

        // first up is the (op)code data. these are ACS instructions that the VM can't
        // provide for you because they use world data.
        //
        // the first argument, `code`, is the ACS opcode index. the second argument,
        // `args`, details how the immediate values are turned into arguments.
        // the third argument, `stackArgC`, is how many arguments are popped off the stack.
        // and finally, the fourth argument, `callFunc`, is the function that the VM will
        // call for us when the instruction is reached
        //
        // notably, everything up to and including 101 comes from the original Hexen ACS0
        // instruction set.

        // 0-56: ACSVM internal codes.
        AddCodeDataACS0I( 57, "",        2, CF_Random);
        AddCodeDataACS0I( 58, "WW",      0, CF_Random);
        AddCodeDataACS0I( 59, "",        2, CF_ThingCount);
        AddCodeDataACS0I( 60, "WW",      0, CF_ThingCount);
        AddCodeDataACS0 ( 61, "",        1, CF_TagWait);
        AddCodeDataACS0 ( 62, "W",       0, CF_TagWait);
        //AddCodeDataACS0 ( 63, "",        1, CF_PolyWait); TODO
        //AddCodeDataACS0 ( 64, "W",       0, CF_PolyWait); TODO
        AddCodeDataACS0V( 65, "",        2, CF_ChangeFloor);
        AddCodeDataACS0V( 66, "WWS",     0, CF_ChangeFloor);
        AddCodeDataACS0V( 67, "",        2, CF_ChangeCeiling);
        AddCodeDataACS0V( 68, "WWS",     0, CF_ChangeCeiling);
        // 69-79: ACSVM internal codes.
        //AddCodeDataACS0I( 80, "",        0, CF_LineSide); TODO
        // 81-82: ACSVM internal codes.
        //AddCodeDataACS0V( 83, "",        0, CF_ClearLineSpecial); TODO
        // 84-85: ACSVM internal codes.
        AddCodeDataACS0V( 86, "",        0, CF_EndPrint);
        // 87-89: ACSVM internal codes.
        //AddCodeDataACS0I( 90, "",        0, CF_PlayerCount); TODO
        //AddCodeDataACS0I( 91, "",        0, CF_GameType); TODO
        AddCodeDataACS0I( 92, "",        0, CF_GameSkill);
        AddCodeDataACS0I( 93, "",        0, CF_Timer);
        AddCodeDataACS0V( 94, "",        2, CF_SectorSound);
        AddCodeDataACS0V( 95, "",        2, CF_AmbientSound);
        //AddCodeDataACS0V( 96, "",        1, CF_SoundSequence); TODO?
        AddCodeDataACS0V( 97, "",        4, CF_SetLineTexture);
        AddCodeDataACS0V( 98, "",        2, CF_SetLineBlocking);
        //AddCodeDataACS0V( 99, "",        7, CF_SetLineSpecial); TODO
        AddCodeDataACS0V(100, "",        3, CF_ThingSound);
        AddCodeDataACS0V(101, "",        0, CF_EndPrintBold);
        AddCodeDataACS0V(102, "",        2, CF_ActivatorSound);
        AddCodeDataACS0V(103, "",        2, CF_LocalAmbientSound);
        AddCodeDataACS0V(104, "",        2, CF_SetLineMonsterBlocking);
        // 105-118: Unused codes.
        //AddCodeDataACS0I(119, "",        0, CF_ActivatorTeam);
        //AddCodeDataACS0I(120, "",        0, CF_PlayerHealth);
        //AddCodeDataACS0I(121, "",        0, CF_PlayerArmorPoints);
        //AddCodeDataACS0I(122, "",        0, CF_PlayerFrags);
        // 123-123: Unused codes.
        //AddCodeDataACS0I(124, "",        0, CF_BlueTeamCount);
        //AddCodeDataACS0I(125, "",        0, CF_RedTeamCount);
        //AddCodeDataACS0I(126, "",        0, CF_BlueTeamScore);
        //AddCodeDataACS0I(127, "",        0, CF_RedTeamScore);
        //AddCodeDataACS0I(128, "",        0, CF_OneFlagCTF);
        //AddCodeDataACS0I(129, "",        0, CF_GetInvasionWave);
        //AddCodeDataACS0I(130, "",        0, CF_GetInvasionState);
        AddCodeDataACS0V(131, "",        0, CF_PrintName);
        AddCodeDataACS0V(132, "",        2, CF_SetMusic);
        //AddCodeDataACS0V(133, "WSWW",    0, CF_ConsoleCommand);
        //AddCodeDataACS0V(134, "",        3, CF_ConsoleCommand);
        //AddCodeDataACS0I(135, "",        0, CF_SinglePlayer);
        // 136-137: ACSVM internal codes.
        //AddCodeDataACS0V(138, "",        1, CF_SetGravity);
        //AddCodeDataACS0V(139, "W",       0, CF_SetGravity);
        //AddCodeDataACS0V(140, "",        1, CF_SetAirControl);
        //AddCodeDataACS0V(141, "W",       0, CF_SetAirControl);
        AddCodeDataACS0V(142, "",        0, CF_ClearInventory);
        AddCodeDataACS0V(143, "",        2, CF_GiveInventory);
        AddCodeDataACS0V(144, "WSW",     0, CF_GiveInventory);
        AddCodeDataACS0V(145, "",        2, CF_TakeInventory);
        AddCodeDataACS0V(146, "WSW",     0, CF_TakeInventory);
        AddCodeDataACS0I(147, "",        1, CF_CheckInventory);
        AddCodeDataACS0I(148, "WS",      0, CF_CheckInventory);
        AddCodeDataACS0I(149, "",        6, CF_Spawn);
        AddCodeDataACS0I(150, "WSWWWWW", 0, CF_Spawn);
        AddCodeDataACS0I(151, "",        4, CF_SpawnSpot);
        AddCodeDataACS0I(152, "WSWWW",   0, CF_SpawnSpot);
        AddCodeDataACS0V(153, "",        3, CF_SetMusic);
        AddCodeDataACS0V(154, "WSWW",    0, CF_SetMusic);
        AddCodeDataACS0V(155, "",        3, CF_LocalSetMusic);
        AddCodeDataACS0V(156, "WSWW",    0, CF_LocalSetMusic);
        // 157-157: ACSVM internal codes.
        //AddCodeDataACS0V(158, "",        1, CF_PrintLocale);
        //AddCodeDataACS0V(159, "",        0, CF_PrintHudMore);
        //AddCodeDataACS0V(160, "",        0, CF_PrintHudOpt);
        //AddCodeDataACS0V(161, "",        0, CF_PrintHudEnd);
        //AddCodeDataACS0V(162, "",        0, CF_PrintHudEndB);
        // 163-164: Unused codes.
        //AddCodeDataACS0V(165, "",        1, CF_SetFont);
        //AddCodeDataACS0V(166, "WS",      0, CF_SetFont);
        // 167-173: ACSVM internal codes.
        AddCodeDataACS0I(174, "BB",      0, CF_Random);
        // 175-179: ACSVM internal codes.
        AddCodeDataACS0V(180, "",        7, CF_SetThingSpecial);
        // 181-189: ACSVM internal codes.
        //AddCodeDataACS0V(190, "",        5, CF_FadeTo);
        //AddCodeDataACS0V(191, "",        9, CF_FadeRange);
        //AddCodeDataACS0V(192, "",        0, CF_FadeCancel);
        //AddCodeDataACS0I(193, "",        1, CF_PlayMovie);
        //AddCodeDataACS0V(194, "",        8, CF_SetFloorTrig);
        //AddCodeDataACS0V(195, "",        8, CF_SetCeilTrig);
        AddCodeDataACS0F(196, "",        1, CF_GetActorX);
        AddCodeDataACS0F(197, "",        1, CF_GetActorY);
        AddCodeDataACS0F(198, "",        1, CF_GetActorZ);
        //AddCodeDataACS0V(199, "",        1, CF_TransStart);
        //AddCodeDataACS0V(200, "",        4, CF_TransPalette);
        //AddCodeDataACS0V(201, "",        8, CF_TransRGB);
        //AddCodeDataACS0V(202, "",        0, CF_TransEnd);
        // 203-217: ACSVM internal codes.
        // 218-219: Unused codes.
        AddCodeDataACS0F(220, "",        1, CF_Sin);
        AddCodeDataACS0F(221, "",        1, CF_Cos);
        AddCodeDataACS0F(222, "",        2, CF_VectorAngle);
        //AddCodeDataACS0I(223, "",        1, CF_CheckWeapon);
        //AddCodeDataACS0I(224, "",        1, CF_SetWeapon);
        // 225-243: ACSVM internal codes.
        //AddCodeDataACS0V(244, "",        2, CF_SetMarineWeapon);
        //AddCodeDataACS0V(245, "",        3, CF_SetActorProperty);
        //AddCodeDataACS0I(246, "",        2, CF_GetActorProperty);
        AddCodeDataACS0I(247, "",        0, CF_PlayerNumber);
        AddCodeDataACS0I(248, "",        0, CF_ActivatorTID);
        //AddCodeDataACS0V(249, "",        2, CF_SetMarineSprite);
        //AddCodeDataACS0I(250, "",        0, CF_GetScreenW);
        //AddCodeDataACS0I(251, "",        0, CF_GetScreenH);
        //AddCodeDataACS0V(252, "",        7, CF_Thing_Projectile2);
        // 253-253: ACSVM internal codes.
        //AddCodeDataACS0V(254, "",        3, CF_SetHudSize);
        //AddCodeDataACS0I(255, "",        1, CF_GetCVar);
        // 256-257: ACSVM internal codes.
        //AddCodeDataACS0I(258, "",        0, CF_GetLineRowOffset);
        AddCodeDataACS0F(259, "",        1, CF_GetActorFloorZ);
        AddCodeDataACS0F(260, "",        1, CF_GetActorAngle);
        AddCodeDataACS0F(261, "",        3, CF_GetSectorFloorZ);
        AddCodeDataACS0F(262, "",        3, CF_GetSectorCeilingZ);
        // 263-263: ACSVM internal codes.
        //AddCodeDataACS0I(264, "",        0, CF_GetSigilPieces);
        //AddCodeDataACS0I(265, "",        1, CF_GetLevelInfo);
        //AddCodeDataACS0V(266, "",        2, CF_ChangeSky);
        //AddCodeDataACS0I(267, "",        1, CF_PlayerInGame);
        //AddCodeDataACS0I(268, "",        1, ACS_CF_PlayerIsBot);
        //AddCodeDataACS0V(269, "",        0, CF_SetCameraTex);
        //AddCodeDataACS0V(270, "",        0, CF_EndLog);
        //AddCodeDataACS0I(271, "",        1, CF_GetAmmoCap);
        //AddCodeDataACS0V(272, "",        2, CF_SetAmmoCap);
        // 273-275: ACSVM internal codes.
        //AddCodeDataACS0V(276, "",        2, CF_SetActorAngle);
        // 277-279: Unused codes.
        //AddCodeDataACS0V(280, "",        7, CF_SpawnProjectile);
        //AddCodeDataACS0I(281, "",        1, CF_GetSectorLightLevel);
        AddCodeDataACS0F(282, "",        1, CF_GetActorCeilingZ);
        AddCodeDataACS0B(283, "",        5, CF_SetActorPosition);
        //AddCodeDataACS0V(284, "",        1, CF_ClrThingInv);
        //AddCodeDataACS0V(285, "",        3, CF_AddThingInv);
        //AddCodeDataACS0V(286, "",        3, CF_SubThingInv);
        //AddCodeDataACS0I(287, "",        2, CF_GetThingInv);
        AddCodeDataACS0I(288, "",        2, CF_ThingCountName);
        AddCodeDataACS0I(289, "",        3, CF_SpawnSpotFacing);
        //AddCodeDataACS0I(290, "",        1, CF_PlayerClass);
        // 291-325: ACSVM internal codes.
        //AddCodeDataACS0I(326, "",        2, CF_GetPlayerProp);
        //AddCodeDataACS0V(327, "",        4, CF_ChangeLevel);
        //AddCodeDataACS0V(328, "",        5, CF_SectorDamage);
        //AddCodeDataACS0V(329, "",        3, CF_ReplaceTextures);
        // 330-330: ACSVM internal codes.
        //AddCodeDataACS0F(331, "",        1, CF_GetActorPitch);
        //AddCodeDataACS0V(332, "",        2, CF_SetActorPitch);
        //AddCodeDataACS0V(333, "",        1, CF_PrintBind);
        //AddCodeDataACS0I(334, "",        3, CF_SetActorState);
        //AddCodeDataACS0I(335, "",        3, CF_Thing_Damage2);
        //AddCodeDataACS0I(336, "",        1, CF_UseInventory);
        //AddCodeDataACS0I(337, "",        2, CF_UseThingInv);
        //AddCodeDataACS0I(338, "",        2, CF_CheckActorCeilingTexture);
        //AddCodeDataACS0I(339, "",        2, CF_CheckActorFloorTexture);
        //AddCodeDataACS0I(340, "",        1, CF_GetActorLightLevel);
        //AddCodeDataACS0V(341, "",        1, CF_SetMugState);
        AddCodeDataACS0I(342, "",        3, CF_ThingCountSector);
        AddCodeDataACS0I(343, "",        3, CF_ThingCountNameSector);
        //AddCodeDataACS0I(344, "",        1, CF_GetPlayerCam);
        //AddCodeDataACS0I(345, "",        7, CF_MorphThing);
        //AddCodeDataACS0I(346, "",        2, CF_UnmorphThing);
        //AddCodeDataACS0I(347, "",        2, CF_GetPlayerInput);
        //AddCodeDataACS0I(348, "",        1, CF_ClassifyActor);
        // 349-361: ACSVM internal codes.
        //AddCodeDataACS0V(362, "",        8, CF_TransDesat);
        // 363-380: ACSVM internal codes.

        #endregion "CodeData"

        #region "FuncData"

        // and now the func data, and tbh i don't know why these are separate
        // from the opcodes, but on the whole they're simpler. just an integer
        // and a callback. you can find a listing of these in zspecial.acs, in
        // the negative indices section
        //
        // 

        // 0-0: ACSVM internal funcs.
        AddFuncDataACS0I(  1, CF_GetLineUDMFInt);
        AddFuncDataACS0F(  2, CF_GetLineUDMFFixed);
        AddFuncDataACS0I(  3, CF_GetThingUDMFInt);
        AddFuncDataACS0F(  4, CF_GetThingUDMFFixed);
        AddFuncDataACS0I(  5, CF_GetSectorUDMFInt);
        AddFuncDataACS0F(  6, CF_GetSectorUDMFFixed);
        AddFuncDataACS0I(  7, CF_GetSideUDMFInt);
        AddFuncDataACS0F(  8, CF_GetSideUDMFFixed);
        AddFuncDataACS0F(  9, CF_GetActorVelX);
        AddFuncDataACS0F( 10, CF_GetActorVelY);
        AddFuncDataACS0F( 11, CF_GetActorVelZ);
        //AddFuncDataACS0V( 12, CF_SetActivator);
        //AddFuncDataACS0V( 13, CF_SetActivatorToTarget);
        //AddFuncDataACS0F( 14, CF_GetThingViewHeight);
        // 15-15: ACSVM internal funcs.
        //AddFuncDataACS0F( 16, CF_GetPlayerAir);
        //AddFuncDataACS0I( 17, CF_SetPlayerAir);
        //AddFuncDataACS0V( 18, CF_SetSkyScrollSpeed);
        //AddFuncDataACS0I( 19, CF_GetPlayerArmor);
        AddFuncDataACS0I( 20, CF_SpawnSpotForced);
        AddFuncDataACS0I( 21, CF_SpawnSpotFacingForced);
        //AddFuncDataACS0I( 22, CF_CheckActorProperty);
        AddFuncDataACS0V( 23, CF_SetActorVelocity);
        //AddFuncDataACS0I( 24, CF_SetThingUserVar);
        //AddFuncDataACS0I( 25, CF_GetThingUserVar);
        //AddFuncDataACS0I( 26, CF_Radius_Quake2);
        //AddFuncDataACS0I( 27, CF_CheckActorClass);
        //AddFuncDataACS0V( 28, CF_SetThingUserArr);
        //AddFuncDataACS0I( 29, CF_GetThingUserArr);
        //AddFuncDataACS0V( 30, CF_SoundSequenceOnActor);
        //AddFuncDataACS0V( 31, CF_SectorSoundSeq);
        //AddFuncDataACS0V( 32, CF_PolyobjSoundSeq);
        //AddFuncDataACS0F( 33, CF_GetPolyobjX);
        //AddFuncDataACS0F( 34, CF_GetPolyobjY);
        //AddFuncDataACS0I( 35, CF_CheckSight);
        AddFuncDataACS0I( 36, CF_SpawnForced);
        //AddFuncDataACS0V( 37, CF_AnnouncerSound);
        //AddFuncDataACS0B( 38, CF_SetPointer);
        // 39-45: ACSVM internal funcs.
        AddFuncDataACS0I( 46, CF_UniqueTID);
        AddFuncDataACS0B( 47, CF_IsTIDUsed);
        AddFuncDataACS0I( 48, CF_Sqrt);
        AddFuncDataACS0F( 49, CF_FixedSqrt);
        AddFuncDataACS0F( 50, CF_VectorLength);
        //AddFuncDataACS0V( 51, CF_SetHudClipRect);
        //AddFuncDataACS0V( 52, CF_SetHudWrapWidth);
        //AddFuncDataACS0V( 53, CF_SetCVar);
        //AddFuncDataACS0I( 54, CF_GetUserCVar);
        //AddFuncDataACS0V( 55, CF_SetUserCVar);
        //AddFuncDataACS0S( 56, CF_GetCVarString);
        //AddFuncDataACS0V( 57, CF_SetCVarString);
        //AddFuncDataACS0S( 58, CF_GetUserCVarString);
        //AddFuncDataACS0V( 59, CF_SetUserCVarString);
        //AddFuncDataACS0V( 60, CF_LineAttack);
        //AddFuncDataACS0V( 61, CF_PlaySound);
        //AddFuncDataACS0V( 62, CF_StopSound);
        // 63-67: ACSVM internal funcs.
        //AddFuncDataACS0S( 68, CF_GetThingType);
        //AddFuncDataACS0S( 69, CF_GetWeapon);
        //AddFuncDataACS0I( 70, CF_SoundVolume);
        //AddFuncDataACS0V( 71, CF_PlayActorSound);
        //AddFuncDataACS0I( 72, CF_SpawnDecal);
        //AddFuncDataACS0B( 73, CF_CheckFont);
        //AddFuncDataACS0I( 74, CF_DropItem);
        //AddFuncDataACS0B( 75, CF_CheckFlag);
        //AddFuncDataACS0V( 76, CF_SetLineActivation);
        //AddFuncDataACS0I( 77, CF_GetLineActivation);
        //AddFuncDataACS0I( 78, CF_GetThingPowerupTics);
        //AddFuncDataACS0V( 79, CF_ChangeActorAngle);
        //AddFuncDataACS0V( 80, CF_ChangeActorPitch);
        //AddFuncDataACS0*( 81, CF_GetArmorInfo); // returns str int or fixed - TODO add something for that?
        //AddFuncDataACS0V( 82, CF_DropInventory);
        //AddFuncDataACS0I( 83, CF_PickThing);
        //AddFuncDataACS0B( 84, CF_IsPointerEqual);
        //AddFuncDataACS0B( 85, CF_CanRaiseThing);
        //AddFuncDataACS0V( 86, CF_SetThingTeleFog);
        //AddFuncDataACS0I( 87, CF_SwapThingTeleFog);
        //AddFuncDataACS0V( 88, CF_SetThingRoll);
        //AddFuncDataACS0V( 89, CF_SetThingRoll);
        //AddFuncDataACS0V( 90, CF_GetThingRoll);
        //AddFuncDataACS0B( 91, CF_QuakeEx);
        //AddFuncDataACS0B( 92, CF_Warp);
        //AddFuncDataACS0I( 93, CF_GetMaxInventory);
        //AddFuncDataACS0V( 94, CF_SetSectorDamage);
        //AddFuncDataACS0V( 95, CF_SetSectorTerrain);
        //AddFuncDataACS0V( 96, CF_SpawnParticle);
        //AddFuncDataACS0V( 97, CF_SetMusicVolume);
        //AddFuncDataACS0B( 98, CF_CheckProximity);
        //AddFuncDataACS0B( 99, CF_CheckActorState);

        // 100-199 are from Zandronum and mostly do MP-focused features

        // 200-299 are originally from ZDoom:
        //AddFuncDataACS0B(200, CF_CheckClass);
        //AddFuncDataACS0I(201, CF_DamageActor);
        //AddFuncDataACS0I(202, CF_SetActorFlag);
        //AddFuncDataACS0V(203, CF_SetTranslation);
        //AddFuncDataACS0S(204, CF_GetActorFloorTexture);
        //AddFuncDataACS0S(205, CF_GetActorFloorTerrain);
        //AddFuncDataACS0I(206, CF_StrArg);
        AddFuncDataACS0F(207, CF_Floor);
        AddFuncDataACS0F(208, CF_Round);
        AddFuncDataACS0F(209, CF_Ceil);
        //AddFuncDataACS0*(210, CF_ScriptCall);
        //AddFuncDataACS0V(211, CF_StartSlideshow);
        //AddFuncDataACS0I(212, CF_GetSectorHealth);
        //AddFuncDataACS0I(213, CF_GetLineHealth);
        //AddFuncDataACS0V(214, CF_SetSubtitleNumber);
        //AddFuncDataACS0I(215, CF_GetNetID);
        //AddFuncDataACS0B(216, CF_SetActivatorByNetID);

        // 300-399 are from Eternity:
        AddFuncDataACS0F(300, CF_GetLineX);
        AddFuncDataACS0F(301, CF_GetLineY);
        //AddFuncDataACS0V(302, CF_SetAirFriction);
        //AddFuncDataACS0V(303, CF_SetPolyObjXY);

        // 400-499 was originally for GZDoom OpenGL features:
        //AddFuncDataACS0V(400, CF_SetSectorGlow);
        //AddFuncDataACS0V(401, CF_SetFogDensity);

        #endregion "FuncData"
    }

    public void UpdateWorld(IWorld world)
    {
        m_world = world;
    }

    public IEnumerable<string> GetStringTable()
    {
        var length = GetTableStringLength();
        for (uint i = 0; i < length; i++)
            yield return GetTableString(i);
    }

    public ReadOnlyMemory<byte> GetSaveState()
    {
        // If the save fails because the buffer is too small then the requiredSize will be full size needed to allocate
        if (!SaveStateToBuffer(SaveBuffer, out var requiredSize))
        {
            Array.Resize(ref SaveBuffer, requiredSize * 2);
            SaveStateToBuffer(SaveBuffer, out requiredSize);
        }

        return SaveBuffer.AsMemory(0, requiredSize);
    }

    public override uint CallSpecImpl(ThreadHandle thread, uint spec, ReadOnlySpan<uint> args)
    {
        var arg0 = args.Get(0);
        var arg1 = args.Get(1);
        var arg2 = args.Get(2);
        var arg3 = args.Get(3);
        var arg4 = args.Get(4);
        m_world.SpecialManager.AddActivatedLineSpecial(
            thread.GetActivator(m_world) ?? m_world.Player,
            (ZDoomLineSpecialType)spec,
            new SpecialArgs(arg0, arg1, arg2, arg3, arg4)
        );
        return 0;
    }

    public override byte[] LoadModule(string moduleName)
    {
        const string BehaviorPrefix = "BEHAVIOR:";
        if (moduleName.StartsWith(BehaviorPrefix, StringComparison.Ordinal))
        {
            var mapName = moduleName.AsSpan(BehaviorPrefix.Length..);
            if (mapName.EqualsIgnoreCase(m_world.MapName))
                return m_world.Behavior ?? [];

            Log.Error($"ACS LoadModule doesn't match loaded map: {moduleName} {m_world.MapName}");
        }
        else
        {
            Log.Error($"ACS LoadModule missing BEHAVIOR: prefix: {moduleName}");
        }

        return [];
    }

    public override bool CheckTag(uint type, uint tag)
    {
        foreach (var sector in m_world.FindBySectorTag((int)tag))
        {
            if (sector.ActiveFloorMove != null || sector.ActiveCeilingMove != null)
            {
                return false;
            }
        }
        return true;
    }

    public int CF_Random(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var min = args.Get(0);
        var max = args.Get(1);
        return m_world.Random.GenInt32Range(min, max);
    }

    public void CF_EndPrint(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        DoPrint(thread, args);
    }

    public void CF_EndPrintBold(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        DoPrint(thread, args);
    }

    public void CF_PrintName(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var stackValue = (int)thread.GetStack(1);
        string? print = null;

        if (stackValue >= 0)
        {
            if (stackValue == 0)
            {
                print = m_world.GetCameraPlayer().Info.Name;
            }
            else
            {
                var player = m_world.EntityManager.GetRealPlayer(stackValue);
                if (player != null)
                    print = player.Info.Name;
            }
        }
        else
        {
            switch ((PrintName)stackValue)
            {
                case PrintName.LevelName:
                    print = m_world.MapInfo.GetNiceNameOrLookup(m_world.ArchiveCollection.Language);
                    break;
                case PrintName.Level:
                    print = m_world.MapName;
                    break;
                case PrintName.Skill:
                    print = m_world.ArchiveCollection.Language.GetMessage(m_world.SkillDefinition.Name);
                    break;
                case PrintName.NextLevel:
                    {
                        var result = m_world.ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextMap(m_world.MapInfo);
                        if (result.MapInfo != null)
                            print = result.MapName;
                    }
                    break;
                case PrintName.NextSecret:
                    {
                        var result = m_world.ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextSecretMap(m_world.MapInfo);
                        if (result.MapInfo != null)
                            print = result.MapName;
                    }
                    break;
            }
        }

        if (print != null && print.Length > 0)
            thread.AppendToPrintBuf(print);
    }

    public void CF_SetMusic(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        PlayMusic(null, args.GetString(thread, 0), args.Get(1));
    }

    public void CF_LocalSetMusic(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        PlayMusic(thread.GetActivator(m_world), args.GetString(thread, 0), args.Get(1));
    }

    // TODO order arg
    private void PlayMusic(Entity? activator, string music, int order)
    {
        if (music == "*")
            music = m_world.MapInfo.Music;
        m_world.PlayLevelMusic(music, activator: activator);
    }

    public static CallFuncResult CF_TagWait(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        thread.MakeTagWait(0, args.GetU(0));
        return CallFuncResult.ReevaluateState;
    }

    public int CF_ThingCount(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        return GetThingCount(args.Get(0), args.Get(1), Sector.NoTag);
    }

    public int CF_ThingCountName(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        return GetThingCount(args.GetStringSpan(thread, 0), args.Get(1), Sector.NoTag);
    }

    public int CF_ThingCountSector(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        return GetThingCount(args.Get(0), args.Get(1), args.Get(2));
    }

    public int CF_ThingCountNameSector(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        return GetThingCount(args.GetStringSpan(thread, 0), args.Get(1), args.Get(2));
    }

    public int CF_PlayerNumber(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var activator = thread.GetActivator(m_world);
        if (activator == null)
        {
            return m_world.Player.PlayerNumber;
        }

        if (activator.PlayerObj != null)
            return activator.PlayerObj.PlayerNumber;
        else
            return -1;
    }

    public int CF_ActivatorTID(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var activator = thread.GetActivator(m_world);
        if (activator == null)
        {
            return 0;
        }

        return activator.ThingId;
    }

    public void CF_ChangeFloor(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var tag = args.Get(0);
        var flat = args.GetStringSpan(thread, 1);
        ActionSpecials.ChangeFlat(m_world, tag, flat, SectorPlaneFace.Floor);
    }

    public void CF_ChangeCeiling(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var tag = args.Get(0);
        var flat = args.GetStringSpan(thread, 1);
        ActionSpecials.ChangeFlat(m_world, tag, flat, SectorPlaneFace.Ceiling);
    }

    public void CF_SetLineTexture(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var lineId = args.Get(0);
        var side = args.Get(1);
        var location = GetWallLocation(args.Get(2));
        var textureName = args.GetStringSpan(thread, 3);
        if (location != WallLocation.None)
            ActionSpecials.SetLineTexture(m_world, lineId, !Convert.ToBoolean(side), location, textureName);
    }

    public void CF_SetLineBlocking(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var lineId = args.Get(0);
        var type = (LineBlockType)args.Get(1);
        var setType = ZDoomLineBlockFlags.None;

        switch(type)
        {
            case LineBlockType.Creatures:
                setType = ZDoomLineBlockFlags.Creatures;
                break;
            case LineBlockType.Everything:
                setType = ZDoomLineBlockFlags.Everything;
                break;
            case LineBlockType.Players:
                setType = ZDoomLineBlockFlags.Players;
                break;
        }

        var clearFlags = type == LineBlockType.Nothing ? ZDoomLineBlockFlags.All : ZDoomLineBlockFlags.None;
        foreach (var line in m_world.FindByLineId(lineId))
            m_world.SetLineBlockFlags(line.Id, setType, clearFlags);
    }

    public void CF_SetLineMonsterBlocking(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var lineId = args.Get(0);
        var setType = args.Get(1) == 0 ? ZDoomLineBlockFlags.Monsters : ZDoomLineBlockFlags.None;

        foreach (var line in m_world.FindByLineId(lineId))
            m_world.SetLineBlockFlags(line.Id, setType, ZDoomLineBlockFlags.None);
    }

    private static WallLocation GetWallLocation(int sideTexture)
    {
        return sideTexture switch
        {
            0 => WallLocation.Upper,
            1 => WallLocation.Middle,
            2 => WallLocation.Lower,
            _ => WallLocation.None,
        };
    }

    public int CF_Spawn(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var className = args.GetStringSpan(thread, 0);
        var x = args.Get(1);
        var y = args.Get(2);
        var z = args.Get(3);
        var tid = args.Get(4);
        var angle = args.Get(5);
        return ActionSpecials.Spawn(m_world, className, x, y, z, tid, angle, false) ? 1 : 0;
    }

    public int CF_SpawnForced(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var className = args.GetStringSpan(thread, 0);
        var x = args.Get(1);
        var y = args.Get(2);
        var z = args.Get(3);
        var tid = args.Get(4);
        var angle = args.Get(5);
        return ActionSpecials.Spawn(m_world, className, x, y, z, tid, angle, true) ? 1 : 0;
    }

    public int CF_SpawnSpot(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var className = args.GetStringSpan(thread, 0);
        var spotTid = args.Get(1);
        var tid = args.Get(2);
        var angle = args.Get(3);
        return ActionSpecials.SpawnSpot(m_world, thread.GetActivator(m_world), className, spotTid, tid, MathHelper.FromByteAngle(angle), false) ? 1 : 0;
    }

    public int CF_SpawnSpotFacing(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var className = args.GetStringSpan(thread, 0);
        var spotTid = args.Get(1);
        var tid = args.Get(2);
        return ActionSpecials.SpawnSpot(m_world, thread.GetActivator(m_world), className, spotTid, tid, null, false) ? 1 : 0;
    }

    public int CF_SpawnSpotFacingForced(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var className = args.GetStringSpan(thread, 0);
        var spotTid = args.Get(1);
        var tid = args.Get(2);
        return ActionSpecials.SpawnSpot(m_world, thread.GetActivator(m_world), className, spotTid, tid, null, true) ? 1 : 0;
    }

    public int CF_SpawnSpotForced(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var className = args.GetStringSpan(thread, 0);
        var spotTid = args.Get(1);
        var tid = args.Get(2);
        var angle = args.Get(3);
        return ActionSpecials.SpawnSpot(m_world, thread.GetActivator(m_world), className, spotTid, tid, MathHelper.FromByteAngle(angle), true) ? 1 : 0;
    }

    public double CF_GetActorX(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        return entity?.Position.X ?? 0;
    }
    public double CF_GetActorY(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        return entity?.Position.Y ?? 0;
    }
    public double CF_GetActorZ(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        return entity?.Position.Z ?? 0;
    }

    public bool CF_SetActorPosition(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        if (entity == null)
            return false;

        var x = args.GetDouble(1);
        var y = args.GetDouble(2);
        var z = args.GetDouble(3);
        var fog = args.GetBool(4);
        var old = entity.Position;
        m_world.SetEntityPosition(entity, new Vec3D(x, y, z));

        if (fog)
        {
            m_world.CreateTeleportFog(old);
            m_world.CreateTeleportFog(entity);
        }

        return true;
    }

    public double CF_GetActorVelX(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        return entity?.Velocity.X ?? 0;
    }

    public double CF_GetActorVelY(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        return entity?.Velocity.Y ?? 0;
    }

    public double CF_GetActorVelZ(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        return entity?.Velocity.Z ?? 0;
    }

    public void CF_SetActorVelocity(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        var velx = args.GetDouble(1);
        var vely = args.GetDouble(2);
        var velz = args.GetDouble(3);
        var add = args.GetBool(4);
        // var setbob = args.GetBool(5); // not used (yet?), since there's no separated bob velocity from Boom
        if (entity == null)
            return;

        var vel = new Vec3D(velx, vely, velz);
        m_world.ApplyVelocity(entity, (add ? entity.Velocity : Vec3D.Zero) + vel);
    }

    public void CF_SetThingSpecial(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var tid = args.Get(0);
        var special = (ZDoomLineSpecialType)args.Get(1);
        var arg0 = args.Get(2);
        var arg1 = args.Get(3);
        var arg2 = args.Get(4);
        var arg3 = args.Get(5);
        var arg4 = args.Get(6);
        var activator = thread.GetActivator(m_world) ?? m_world.Player;
        ActionSpecials.ThingSetSpecial(activator, m_world, tid, special, arg0, arg1, arg2, arg3, arg4);
    }

    public int CF_UniqueTID(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        while (true)
        {
            var potentialTid = m_world.Random.GenInt32Range(0, int.MaxValue);
            // vanishingly unlikely given the 2^31 potential choices
            if (m_world.EntityManager.TidInUse(potentialTid))
                continue;
            return potentialTid;
        }
    }
    
    public bool CF_IsTIDUsed(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var tid = args.Get(0);
        return m_world.EntityManager.TidInUse(tid);
    }

    public void CF_ClearInventory(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var activator = thread.GetActivator(m_world);
        if (activator?.PlayerObj != null)
            m_world.ClearInventory(activator.PlayerObj);
    }

    public void CF_GiveInventory(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var activator = thread.GetActivator(m_world);
        if (activator?.PlayerObj == null)
            return;

        var className = args.GetStringSpan(thread, 0);
        var amount = args.Get(1);
        m_world.GiveInventory(activator.PlayerObj, className, amount);
    }

    public void CF_TakeInventory(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var activator = thread.GetActivator(m_world);
        if (activator?.PlayerObj == null)
            return;

        var className = args.GetStringSpan(thread, 0);
        var amount = args.Get(1);
        m_world.TakeInventory(activator.PlayerObj, className, amount);
    }

    public int CF_CheckInventory(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var activator = thread.GetActivator(m_world);
        if (activator?.PlayerObj == null)
            return 0;

        var className = args.GetStringSpan(thread, 0);
        return activator.PlayerObj.Inventory.CheckInventory(className);
    }

    public void CF_SectorSound(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var line = thread.GetLine(m_world);
        var sound = args.GetStringSpan(thread, 0);
        var volume = args.GetNormalizedVolume(1);

        if (line == null)
        {
            // If this is called without a source line then play a global static sound
            m_world.PlayStaticSound(null, sound, volume);
            return;
        }

        m_world.SetSectorSound(line.Front.Sector, sound, volume);
    }

    public void CF_ThingSound(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        ActionSpecials.ThingSound(m_world, args.Get(0), args.GetStringSpan(thread, 1), args.GetNormalizedVolume(2));
    }

    public void CF_ActivatorSound(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var activator = thread.GetActivator(m_world);
        if (activator == null)
            return;

        m_world.SetEntitySound(activator, args.GetStringSpan(thread, 0), args.GetNormalizedVolume(1));
    }

    public void CF_AmbientSound(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        m_world.PlayStaticSound(null, args.GetStringSpan(thread, 0), args.GetNormalizedVolume(1));
    }

    public void CF_LocalAmbientSound(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var activator = thread.GetActivator(m_world);
        if (activator?.PlayerObj != null)
            m_world.PlayStaticSound(activator.PlayerObj, args.GetStringSpan(thread, 0), args.GetNormalizedVolume(1));
    }

    enum GameSkillResult : int
    {
        VeryEasy = 0,
        Easy = 1,
        Normal = 2,
        Hard = 3,
        VeryHard = 4
    }

    public int CF_GameSkill(ThreadHandle thread, ReadOnlySpan<uint> args) => 
        (int)(m_world.SkillLevel switch {
            SkillLevel.VeryEasy => GameSkillResult.VeryEasy,
            SkillLevel.Easy => GameSkillResult.Easy,
            SkillLevel.Medium => GameSkillResult.Normal,
            SkillLevel.Hard => GameSkillResult.Hard,
            SkillLevel.Nightmare => GameSkillResult.VeryHard,

            _ => GameSkillResult.Normal
        });

    public int CF_Timer(ThreadHandle thread, ReadOnlySpan<uint> args) => m_world.LevelTime;

    private static double FixedAngleWraparound(double value) => value - Math.Floor(value);

    // ACS trigonometry uses "fixed-point angles" instead of something normal like radians or degrees
    public static double CF_Sin(ThreadHandle thread, ReadOnlySpan<uint> args) => Math.Sin(args.GetDouble(0) * 2 * Math.PI);
    public static double CF_Cos(ThreadHandle thread, ReadOnlySpan<uint> args) => Math.Cos(args.GetDouble(0) * 2 * Math.PI);
    public static double CF_VectorAngle(ThreadHandle thread, ReadOnlySpan<uint> args) => FixedAngleWraparound(Math.Atan2(args.GetDouble(1), args.GetDouble(0)) / (2 * Math.PI));

    public static int CF_Sqrt(ThreadHandle thread, ReadOnlySpan<uint> args) => (int)Math.Sqrt(args.Get(0));
    public static double CF_FixedSqrt(ThreadHandle thread, ReadOnlySpan<uint> args) => Math.Sqrt(args.GetDouble(0));
    public static double CF_VectorLength(ThreadHandle thread, ReadOnlySpan<uint> args) => new Vec2D(args.GetDouble(0), args.GetDouble(1)).Length();

    // these can be done with fixed-point bit manipulation but it's harder to read than the double version for questionable benefit on modern hardware
    public static double CF_Floor(ThreadHandle thread, ReadOnlySpan<uint> args) => Math.Floor(args.GetDouble(0));
    public static double CF_Round(ThreadHandle thread, ReadOnlySpan<uint> args) => Math.Round(args.GetDouble(0));
    public static double CF_Ceil(ThreadHandle thread, ReadOnlySpan<uint> args) => Math.Ceiling(args.GetDouble(0));

    private Vec2D GetLineInner(int lineId, double interpolationT, double normalOffset)
    {
        var line = m_world.FindByLineId(lineId).First();
        if (line == null) return Vec2D.Zero;

        var point = line.Segment.FromTime(interpolationT);
        if (normalOffset != 0.0)
        {
            var normal = new Vec2D(line.Segment.Delta.Y, -line.Segment.Delta.X).Unit();
            point += point + normal * normalOffset;
        }
        return point;
    }
    public double CF_GetLineX(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var lineId = args.Get(0);
        var interpolationT = args.GetDouble(1);
        var normalOffset = args.GetDouble(2);
        return GetLineInner(lineId, interpolationT, normalOffset).X;
    }

    public double CF_GetLineY(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var lineId = args.Get(0);
        var interpolationT = args.GetDouble(1);
        var normalOffset = args.GetDouble(2);
        return GetLineInner(lineId, interpolationT, normalOffset).Y;
    }

    public double CF_GetActorFloorZ(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        return entity?.HighestFloorZ ?? 0.0;
    }

    public double CF_GetActorCeilingZ(ThreadHandle thread, ReadOnlySpan<uint> args) {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        return entity?.LowestCeilingZ ?? 0.0;
    }

    public double CF_GetActorAngle(ThreadHandle thread, ReadOnlySpan<uint> args) {
        var entity = args.GetTidOrActivator(thread, m_world, 0);
        return FixedAngleWraparound((entity?.AngleRadians ?? 0.0) / (2 * Math.PI));
    }

    private Sector? GetSectorForTagOrPoint(int tag, double x, double y) =>
        (tag != 0) ? m_world.FindBySectorTag(tag).First() : m_world.ToSubsector(x, y).Sector;

    public double CF_GetSectorFloorZ(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var tag = args.Get(0);
        // NOTE: intentionally not `GetDouble`. idk why but that's how the function works.
        var x = args.Get(1);
        var y = args.Get(2);
        return GetSectorForTagOrPoint(tag, x, y)?.Floor?.Plane.ToZ(new Vec2D(x, y)) ?? 0.0;
    }

    public double CF_GetSectorCeilingZ(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var tag = args.Get(0);
        // NOTE: intentionally not `GetDouble`. idk why but that's how the function works.
        var x = args.Get(1);
        var y = args.Get(2);
        return GetSectorForTagOrPoint(tag, x, y)?.Ceiling?.Plane.ToZ(new Vec2D(x, y)) ?? 0.0;
    }

    public int CF_GetLineUDMFInt(ThreadHandle thread, ReadOnlySpan<uint> args) =>
        m_world.FindByLineId(args.Get(0))
            .First()
            ?.UserProperties.GetInteger(args.GetStringSpan(thread, 1))
            ?? 0;
    public double CF_GetLineUDMFFixed(ThreadHandle thread, ReadOnlySpan<uint> args) =>
        m_world.FindByLineId(args.Get(0))
            .First()
            ?.UserProperties.GetDecimal(args.GetStringSpan(thread, 1))
            ?? 0;

    public int CF_GetThingUDMFInt(ThreadHandle thread, ReadOnlySpan<uint> args) =>
        m_world.FindByTid(args.Get(0))
            .First()
            ?.UserProperties.GetInteger(args.GetStringSpan(thread, 1))
            ?? 0;
    public double CF_GetThingUDMFFixed(ThreadHandle thread, ReadOnlySpan<uint> args) =>
        m_world.FindByTid(args.Get(0))
        .First()
        ?.UserProperties.GetDecimal(args.GetStringSpan(thread, 1))
        ?? 0;

    public int CF_GetSectorUDMFInt(ThreadHandle thread, ReadOnlySpan<uint> args) =>
        m_world.FindBySectorTag(args.Get(0))
        .First()
        ?.UserProperties.GetInteger(args.GetStringSpan(thread, 1))
        ?? 0;
    public double CF_GetSectorUDMFFixed(ThreadHandle thread, ReadOnlySpan<uint> args) =>
        m_world.FindBySectorTag(args.Get(0))
        .First()
        ?.UserProperties.GetDecimal(args.GetStringSpan(thread, 1))
        ?? 0;

    private static Side? GetLineSide(Line? line, bool back) => back ? line?.Back : line?.Front;
    public int CF_GetSideUDMFInt(ThreadHandle thread, ReadOnlySpan<uint> args) =>
        GetLineSide(m_world.FindByLineId(args.Get(0)).First(), args.GetBool(1))
            ?.UserProperties.GetInteger(args.GetStringSpan(thread, 2))
            ?? 0;
    public double CF_GetSideUDMFFixed(ThreadHandle thread, ReadOnlySpan<uint> args) =>
        GetLineSide(m_world.FindByLineId(args.Get(0)).First(), args.GetBool(1))
            ?.UserProperties.GetDecimal(args.GetStringSpan(thread, 2))
            ?? 0;

    private int GetThingCount(int type, int tid, int sectorTag)
    {
        // Check any type by tid only
        if (type == 0)
            return m_world.EntityAliveCount(-1, tid, sectorTag);

        if (!ThingSpawnTypes.Lookup.TryGetValue(type, out var definitionName))
            return 0;

        var def = m_world.EntityManager.DefinitionComposer.GetByName(definitionName);
        if (def == null)
            return 0;

        return m_world.EntityAliveCount(def.Id, tid, sectorTag);
    }

    private int GetThingCount(ReadOnlySpan<char> name, int tid, int sectorTag)
    {
        var def = m_world.EntityManager.DefinitionComposer.GetByName(name);
        if (def == null)
            return 0;

        return m_world.EntityAliveCount(def.Id, tid, sectorTag);
    }

    private void DoPrint(ThreadHandle thread, ReadOnlySpan<uint> args)
    {
        var activator = thread.GetActivator(m_world);
        var printBuf = thread.GetPrintBuf()!;

        if (activator == null)
            m_world.DisplayMessage(new DisplayMessageArgs(printBuf, null, null, IsCentered: true, ForAllPlayers: true));
        else if (activator.PlayerObj != null)
            m_world.DisplayMessage(new DisplayMessageArgs(printBuf, activator.PlayerObj, null, IsCentered: true));
    }
}
