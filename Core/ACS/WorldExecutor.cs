using Helion.Maps.Specials;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.Specials;
using HelionACS;

namespace Helion.ACS;

public class WorldExecutor : Executor
{
    private IWorld m_world;

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
        AddCodeDataACS0( 57, "",        2, CF_Random);
        AddCodeDataACS0( 58, "WW",      0, CF_Random);
        AddCodeDataACS0( 59, "",        2, CF_ThingCount);
        AddCodeDataACS0( 60, "WW",      0, CF_ThingCount);
        AddCodeDataACS0( 61, "",        1, CF_TagWait);
        AddCodeDataACS0( 62, "W",       0, CF_TagWait);
        //AddCodeDataACS0( 63, "",        1, CF_PolyWait); TODO
        //AddCodeDataACS0( 64, "W",       0, CF_PolyWait); TODO
        AddCodeDataACS0( 65, "",        2, CF_ChangeFloor);
        AddCodeDataACS0( 66, "WWS",     0, CF_ChangeFloor);
        AddCodeDataACS0( 67, "",        2, CF_ChangeCeiling);
        AddCodeDataACS0( 68, "WWS",     0, CF_ChangeCeiling);
        // 69-79: ACSVM internal codes.
        //AddCodeDataACS0( 80, "",        0, CF_LineSide); TODO
        // 81-82: ACSVM internal codes.
        //AddCodeDataACS0( 83, "",        0, CF_ClearLineSpecial); TODO
        // 84-85: ACSVM internal codes.
        AddCodeDataACS0( 86, "",        0, CF_EndPrint);
        // 87-89: ACSVM internal codes.
        //AddCodeDataACS0( 90, "",        0, CF_PlayerCount); TODO
        //AddCodeDataACS0( 91, "",        0, CF_GameType); TODO
        //AddCodeDataACS0( 92, "",        0, CF_GameSkill); TODO
        //AddCodeDataACS0( 93, "",        0, CF_Timer); TODO
        //AddCodeDataACS0( 94, "",        2, CF_SectorSound); TODO?
        //AddCodeDataACS0( 95, "",        2, CF_AmbientSound); TODO?
        //AddCodeDataACS0( 96, "",        1, CF_SoundSequence); TODO?
        //AddCodeDataACS0( 97, "",        4, CF_SetLineTexture); TODO
        //AddCodeDataACS0( 98, "",        2, CF_SetLineBlocking); TODO
        //AddCodeDataACS0( 99, "",        7, CF_SetLineSpecial); TODO
        //AddCodeDataACS0(100, "",        3, CF_ThingSound); TODO
        AddCodeDataACS0(101, "",        0, CF_EndPrintBold);
        //AddCodeDataACS0(102, "",        2, CF_ActivatorSound);
        //AddCodeDataACS0(103, "",        2, CF_LocalAmbientSound);
        //AddCodeDataACS0(104, "",        2, CF_SetLineMonsterBlocking);
        // 105-118: Unused codes.
        //AddCodeDataACS0(119, "",        0, CF_ActivatorTeam);
        //AddCodeDataACS0(120, "",        0, CF_PlayerHealth);
        //AddCodeDataACS0(121, "",        0, CF_PlayerArmorPoints);
        //AddCodeDataACS0(122, "",        0, CF_PlayerFrags);
        // 123-123: Unused codes.
        //AddCodeDataACS0(124, "",        0, CF_BlueTeamCount);
        //AddCodeDataACS0(125, "",        0, CF_RedTeamCount);
        //AddCodeDataACS0(126, "",        0, CF_BlueTeamScore);
        //AddCodeDataACS0(127, "",        0, CF_RedTeamScore);
        //AddCodeDataACS0(128, "",        0, CF_OneFlagCTF);
        //AddCodeDataACS0(129, "",        0, CF_GetInvasionWave);
        //AddCodeDataACS0(130, "",        0, CF_GetInvasionState);
        AddCodeDataACS0(131, "",        0, CF_PrintName);
        //AddCodeDataACS0(132, "",        2, CF_SetMusic);
        //AddCodeDataACS0(133, "WSWW",    0, CF_ConsoleCommand);
        //AddCodeDataACS0(134, "",        3, CF_ConsoleCommand);
        //AddCodeDataACS0(135, "",        0, CF_SinglePlayer);
        // 136-137: ACSVM internal codes.
        //AddCodeDataACS0(138, "",        1, CF_SetGravity);
        //AddCodeDataACS0(139, "W",       0, CF_SetGravity);
        //AddCodeDataACS0(140, "",        1, CF_SetAirControl);
        //AddCodeDataACS0(141, "W",       0, CF_SetAirControl);
        //AddCodeDataACS0(142, "",        0, CF_ClearInventory);
        //AddCodeDataACS0(143, "",        2, CF_GiveInventory);
        //AddCodeDataACS0(144, "WSW",     0, CF_GiveInventory);
        //AddCodeDataACS0(145, "",        2, CF_TakeInventory);
        //AddCodeDataACS0(146, "WSW",     0, CF_TakeInventory);
        //AddCodeDataACS0(147, "",        1, CF_CheckInventory);
        //AddCodeDataACS0(148, "WS",      0, CF_CheckInventory);
        AddCodeDataACS0(149, "",        6, CF_Spawn);
        AddCodeDataACS0(150, "WSWWWWW", 0, CF_Spawn);
        AddCodeDataACS0(151, "",        4, CF_SpawnSpot);
        AddCodeDataACS0(152, "WSWWW",   0, CF_SpawnSpot);
        //AddCodeDataACS0(153, "",        3, CF_SetMusic);
        //AddCodeDataACS0(154, "WSWW",    0, CF_SetMusic);
        //AddCodeDataACS0(155, "",        3, CF_LocalSetMusic);
        //AddCodeDataACS0(156, "WSWW",    0, CF_LocalSetMusic);
        // 157-157: ACSVM internal codes.
        //AddCodeDataACS0(158, "",        1, CF_PrintLocale);
        //AddCodeDataACS0(159, "",        0, CF_PrintHudMore);
        //AddCodeDataACS0(160, "",        0, CF_PrintHudOpt);
        //AddCodeDataACS0(161, "",        0, CF_PrintHudEnd);
        //AddCodeDataACS0(162, "",        0, CF_PrintHudEndB);
        // 163-164: Unused codes.
        //AddCodeDataACS0(165, "",        1, CF_SetFont);
        //AddCodeDataACS0(166, "WS",      0, CF_SetFont);
        // 167-173: ACSVM internal codes.
        //AddCodeDataACS0(174, "BB",      0, CF_Random);
        // 175-179: ACSVM internal codes.
        //AddCodeDataACS0(180, "",        7, CF_SetThingSpecial);
        // 181-189: ACSVM internal codes.
        //AddCodeDataACS0(190, "",        5, CF_FadeTo);
        //AddCodeDataACS0(191, "",        9, CF_FadeRange);
        //AddCodeDataACS0(192, "",        0, CF_FadeCancel);
        //AddCodeDataACS0(193, "",        1, CF_PlayMovie);
        //AddCodeDataACS0(194, "",        8, CF_SetFloorTrig);
        //AddCodeDataACS0(195, "",        8, CF_SetCeilTrig);
        //AddCodeDataACS0(196, "",        1, CF_GetActorX);
        //AddCodeDataACS0(197, "",        1, CF_GetActorY);
        //AddCodeDataACS0(198, "",        1, CF_GetActorZ);
        //AddCodeDataACS0(199, "",        1, CF_TransStart);
        //AddCodeDataACS0(200, "",        4, CF_TransPalette);
        //AddCodeDataACS0(201, "",        8, CF_TransRGB);
        //AddCodeDataACS0(202, "",        0, CF_TransEnd);
        // 203-217: ACSVM internal codes.
        // 218-219: Unused codes.
        //AddCodeDataACS0(220, "",        1, CF_Sin);
        //AddCodeDataACS0(221, "",        1, CF_Cos);
        //AddCodeDataACS0(222, "",        2, CF_VectorAngle);
        //AddCodeDataACS0(223, "",        1, CF_CheckWeapon);
        //AddCodeDataACS0(224, "",        1, CF_SetWeapon);
        // 225-243: ACSVM internal codes.
        //AddCodeDataACS0(244, "",        2, CF_SetMarineWeapon);
        //AddCodeDataACS0(245, "",        3, CF_SetActorProperty);
        //AddCodeDataACS0(246, "",        2, CF_GetActorProperty);
        //AddCodeDataACS0(247, "",        0, CF_PlayerNumber);
        AddCodeDataACS0(248, "",        0, CF_ActivatorTID);
        //AddCodeDataACS0(249, "",        2, CF_SetMarineSprite);
        //AddCodeDataACS0(250, "",        0, CF_GetScreenW);
        //AddCodeDataACS0(251, "",        0, CF_GetScreenH);
        //AddCodeDataACS0(252, "",        7, CF_Thing_Projectile2);
        // 253-253: ACSVM internal codes.
        //AddCodeDataACS0(254, "",        3, CF_SetHudSize);
        //AddCodeDataACS0(255, "",        1, CF_GetCVar);
        // 256-257: ACSVM internal codes.
        //AddCodeDataACS0(258, "",        0, CF_GetLineRowOffset);
        //AddCodeDataACS0(259, "",        1, CF_GetActorFloorZ);
        //AddCodeDataACS0(260, "",        1, CF_GetActorAngle);
        //AddCodeDataACS0(261, "",        3, CF_GetSectorFloorZ);
        //AddCodeDataACS0(262, "",        3, CF_GetSectorCeilingZ);
        // 263-263: ACSVM internal codes.
        //AddCodeDataACS0(264, "",        0, CF_GetSigilPieces);
        //AddCodeDataACS0(265, "",        1, CF_GetLevelInfo);
        //AddCodeDataACS0(266, "",        2, CF_ChangeSky);
        //AddCodeDataACS0(267, "",        1, CF_PlayerInGame);
        //AddCodeDataACS0(268, "",        1, ACS_CF_PlayerIsBot);
        //AddCodeDataACS0(269, "",        0, CF_SetCameraTex);
        //AddCodeDataACS0(270, "",        0, CF_EndLog);
        //AddCodeDataACS0(271, "",        1, CF_GetAmmoCap);
        //AddCodeDataACS0(272, "",        2, CF_SetAmmoCap);
        // 273-275: ACSVM internal codes.
        //AddCodeDataACS0(276, "",        2, CF_SetActorAngle);
        // 277-279: Unused codes.
        //AddCodeDataACS0(280, "",        7, CF_SpawnProjectile);
        //AddCodeDataACS0(281, "",        1, CF_GetSectorLightLevel);
        //AddCodeDataACS0(282, "",        1, CF_GetActorCeilingZ);
        //AddCodeDataACS0(283, "",        5, CF_SetActorPosition);
        //AddCodeDataACS0(284, "",        1, CF_ClrThingInv);
        //AddCodeDataACS0(285, "",        3, CF_AddThingInv);
        //AddCodeDataACS0(286, "",        3, CF_SubThingInv);
        //AddCodeDataACS0(287, "",        2, CF_GetThingInv);
        //AddCodeDataACS0(288, "",        2, CF_ThingCountName);
        AddCodeDataACS0(289, "",        3, CF_SpawnSpotFacing);
        //AddCodeDataACS0(290, "",        1, CF_PlayerClass);
        // 291-325: ACSVM internal codes.
        //AddCodeDataACS0(326, "",        2, CF_GetPlayerProp);
        //AddCodeDataACS0(327, "",        4, CF_ChangeLevel);
        //AddCodeDataACS0(328, "",        5, CF_SectorDamage);
        //AddCodeDataACS0(329, "",        3, CF_ReplaceTextures);
        // 330-330: ACSVM internal codes.
        //AddCodeDataACS0(331, "",        1, CF_GetActorPitch);
        //AddCodeDataACS0(332, "",        2, CF_SetActorPitch);
        //AddCodeDataACS0(333, "",        1, CF_PrintBind);
        //AddCodeDataACS0(334, "",        3, CF_SetActorState);
        //AddCodeDataACS0(335, "",        3, CF_Thing_Damage2);
        //AddCodeDataACS0(336, "",        1, CF_UseInventory);
        //AddCodeDataACS0(337, "",        2, CF_UseThingInv);
        //AddCodeDataACS0(338, "",        2, CF_CheckActorCeilingTexture);
        //AddCodeDataACS0(339, "",        2, CF_CheckActorFloorTexture);
        //AddCodeDataACS0(340, "",        1, CF_GetActorLightLevel);
        //AddCodeDataACS0(341, "",        1, CF_SetMugState);
        //AddCodeDataACS0(342, "",        3, CF_ThingCountSector);
        //AddCodeDataACS0(343, "",        3, CF_ThingCountNameSector);
        //AddCodeDataACS0(344, "",        1, CF_GetPlayerCam);
        //AddCodeDataACS0(345, "",        7, CF_MorphThing);
        //AddCodeDataACS0(346, "",        2, CF_UnmorphThing);
        //AddCodeDataACS0(347, "",        2, CF_GetPlayerInput);
        //AddCodeDataACS0(348, "",        1, CF_ClassifyActor);
        // 349-361: ACSVM internal codes.
        //AddCodeDataACS0(362, "",        8, CF_TransDesat);
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
        //AddFuncDataACS0(  1, CF_GetLineUDMFInt);
        //AddFuncDataACS0(  2, CF_GetLineUDMFFixed);
        //AddFuncDataACS0(  3, CF_GetThingUDMFInt);
        //AddFuncDataACS0(  4, CF_GetThingUDMFFixed);
        //AddFuncDataACS0(  5, CF_GetSectorUDMFInt);
        //AddFuncDataACS0(  6, CF_GetSectorUDMFFixed);
        //AddFuncDataACS0(  7, CF_GetSideUDMFInt);
        //AddFuncDataACS0(  8, CF_GetSideUDMFFixed);
        //AddFuncDataACS0(  9, CF_GetActorVelX);
        //AddFuncDataACS0( 10, CF_GetActorVelY);
        //AddFuncDataACS0( 11, CF_GetActorVelZ);
        //AddFuncDataACS0( 12, CF_SetActivator);
        //AddFuncDataACS0( 13, CF_SetActivatorToTarget);
        //AddFuncDataACS0( 14, CF_GetThingViewHeight);
        // 15-15: ACSVM internal funcs.
        //AddFuncDataACS0( 16, CF_GetPlayerAir);
        //AddFuncDataACS0( 17, CF_SetPlayerAir);
        //AddFuncDataACS0( 18, CF_SetSkyScrollSpeed);
        //AddFuncDataACS0( 19, CF_GetPlayerArmor);
        AddFuncDataACS0( 20, CF_SpawnSpotForced);
        AddFuncDataACS0( 21, CF_SpawnSpotFacingForced);
        //AddFuncDataACS0( 22, CF_CheckActorProperty);
        //AddFuncDataACS0( 23, CF_SetActorVelocity);
        //AddFuncDataACS0( 24, CF_SetThingUserVar);
        //AddFuncDataACS0( 25, CF_GetThingUserVar);
        //AddFuncDataACS0( 26, CF_Radius_Quake2);
        //AddFuncDataACS0( 27, CF_CheckActorClass);
        //AddFuncDataACS0( 28, CF_SetThingUserArr);
        //AddFuncDataACS0( 29, CF_GetThingUserArr);
        //AddFuncDataACS0( 30, CF_SoundSequenceOnActor);
        //AddFuncDataACS0( 31, CF_SectorSoundSeq);
        //AddFuncDataACS0( 32, CF_PolyojbSoundSeq);
        //AddFuncDataACS0( 33, CF_GetPolyobjX);
        //AddFuncDataACS0( 34, CF_GetPolyobjY);
        //AddFuncDataACS0( 35, CF_CheckSight);
        //AddFuncDataACS0( 36, CF_SpawnForced);
        //AddFuncDataACS0( 37, CF_AnnouncerSound);
        //AddFuncDataACS0( 38, CF_SetPointer);
        // 39-45: ACSVM internal funcs.
        //AddFuncDataACS0( 46, CF_UniqueTID);
        //AddFuncDataACS0( 47, CF_IsTIDUsed);
        //AddFuncDataACS0( 48, CF_Sqrt);
        //AddFuncDataACS0( 49, CF_FixedSqrt);
        //AddFuncDataACS0( 50, CF_VectorLength);
        //AddFuncDataACS0( 51, CF_SetHudClipRect);
        //AddFuncDataACS0( 52, CF_SetHudWrapWidth);
        //AddFuncDataACS0( 53, CF_SetCVar);
        //AddFuncDataACS0( 54, CF_GetUserCVar);
        //AddFuncDataACS0( 55, CF_SetUserCVar);
        //AddFuncDataACS0( 56, CF_GetCVarString);
        //AddFuncDataACS0( 57, CF_SetCVarString);
        //AddFuncDataACS0( 58, CF_GetUserCVarString);
        //AddFuncDataACS0( 59, CF_SetUserCVarString);
        //AddFuncDataACS0( 60, CF_LineAttack);
        //AddFuncDataACS0( 61, CF_PlaySound);
        //AddFuncDataACS0( 62, CF_StopSound);
        // 63-67: ACSVM internal funcs.
        //AddFuncDataACS0( 68, CF_GetThingType);
        //AddFuncDataACS0( 69, CF_GetWeapon);
        //AddFuncDataACS0( 70, CF_SoundVolume);
        //AddFuncDataACS0( 71, CF_PlayActorSound);
        //AddFuncDataACS0( 72, CF_SpawnDecal);
        //AddFuncDataACS0( 73, CF_CheckFont);
        //AddFuncDataACS0( 74, CF_DropItem);
        //AddFuncDataACS0( 75, CF_CheckFlag);
        //AddFuncDataACS0( 76, CF_SetLineActivation);
        //AddFuncDataACS0( 77, CF_GetLineActivation);
        //AddFuncDataACS0( 78, CF_GetThingPowerupTics);
        //AddFuncDataACS0( 79, CF_ChangeActorAngle);
        //AddFuncDataACS0( 80, CF_ChangeActorPitch);
        //AddFuncDataACS0( 81, CF_GetArmorInfo);
        //AddFuncDataACS0( 82, CF_DropInventory);
        //AddFuncDataACS0( 83, CF_PickThing);
        //AddFuncDataACS0( 84, CF_IsPointerEqual);
        //AddFuncDataACS0( 85, CF_CanRaiseThing);
        //AddFuncDataACS0( 86, CF_SetThingTeleFog);
        //AddFuncDataACS0( 87, CF_SwapThingTeleFog);
        //AddFuncDataACS0( 88, CF_SetThingRoll);
        //AddFuncDataACS0( 89, CF_SetThingRoll);
        //AddFuncDataACS0( 90, CF_GetThingRoll);
        //AddFuncDataACS0( 91, CF_QuakeEx);
        //AddFuncDataACS0( 92, CF_Warp);
        //AddFuncDataACS0( 93, CF_GetMaxInventory);
        //AddFuncDataACS0( 94, CF_SetSectorDamage);
        //AddFuncDataACS0( 95, CF_SetSectorTerrain);
        //AddFuncDataACS0( 96, CF_SpawnParticle);
        //AddFuncDataACS0( 97, CF_SetMusicVolume);
        //AddFuncDataACS0( 98, CF_CheckProximity);
        //AddFuncDataACS0( 99, CF_CheckActorState);

        // 100-199 are from Zandronum and mostly do MP-focused features

        // 200-299 are originally from ZDoom:
        //AddFuncDataACS0(200, CF_CheckClass);
        //AddFuncDataACS0(201, DamageActor CF_);// [arookas]
        //AddFuncDataACS0(202, CF_SetActorFlag);
        //AddFuncDataACS0(203, CF_SetTranslation);
        //AddFuncDataACS0(204, CF_GetActorFloorTexture);
        //AddFuncDataACS0(205, CF_GetActorFloorTerrain);
        //AddFuncDataACS0(206, CF_StrArg);
        //AddFuncDataACS0(207, CF_Floor);
        //AddFuncDataACS0(208, CF_Round);
        //AddFuncDataACS0(209, CF_Ceil);
        //AddFuncDataACS0(210, CF_ScriptCall);
        //AddFuncDataACS0(211, CF_StartSlideshow);
        //AddFuncDataACS0(212, CF_GetSectorHealth);
        //AddFuncDataACS0(213, CF_GetLineHealth);
        //AddFuncDataACS0(214, CF_SetSubtitleNumber);
        //AddFuncDataACS0(215, CF_GetNetID);
        //AddFuncDataACS0(216, CF_SetActivatorByNetID);

        // 300-399 are from Eternity:
        //AddFuncDataACS0(300, CF_GetLineX);
        //AddFuncDataACS0(301, CF_GetLineY);
        //AddFuncDataACS0(302, CF_SetAirFriction);
        //AddFuncDataACS0(303, CF_SetPolyObjXY);

        // 400-499 was originally for GZDoom OpenGL features:
        //AddFuncDataACS0(400, CF_SetSectorGlow);
        //AddFuncDataACS0(401, CF_SetFogDensity);

        #endregion "FuncData"
    }

    public void UpdateWorld(IWorld world)
    {
        m_world = world;
    }

    public override uint CallSpecImpl(ThreadHandle thread, uint spec, uint[] args)
    {
        var arg0 = args.Get(0);
        var arg1 = args.Get(1);
        var arg2 = args.Get(2);
        var arg3 = args.Get(3);
        var arg4 = args.Get(4);
        m_world.SpecialManager.AddActivatedLineSpecial(
            thread.GetActivator(m_world) ?? m_world.Player,
            (Maps.Specials.ZDoom.ZDoomLineSpecialType)spec,
            new SpecialArgs(arg0, arg1, arg2, arg3, arg4)
        );
        return 0;
    }

    public override byte[] LoadModule(string moduleName)
    {
        const string BehaviorPrefix = "BEHAVIOR:";
        if (moduleName.StartsWith(BehaviorPrefix, System.StringComparison.Ordinal))
        {
            var map = moduleName[BehaviorPrefix.Length..];
            var mapEntries = m_world.ArchiveCollection.GetMapEntryCollection(map);
            var behavior = mapEntries?.Behavior;
            if (behavior == null)
            {
                return [];
            }
            return behavior.ReadData();
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

    public bool CF_Random(ThreadHandle thread, uint[] args)
    {
        var min = args.Get(0);
        var max = args.Get(1);
        thread.PushStack((uint)m_world.Random.GenInt32Range(min, max));
        return false;
    }

    public bool CF_EndPrint(ThreadHandle thread, uint[] args)
    {
        return DoPrint(thread, args);
    }

    public bool CF_EndPrintBold(ThreadHandle thread, uint[] args)
    {
        return DoPrint(thread, args);
    }

    public bool CF_PrintName(ThreadHandle thread, uint[] args)
    {
        var type = (PrintName)thread.GetStack(1);
        string? print = null;
        switch (type)
        {
            case PrintName.LevelName:
                print = m_world.MapInfo.DisplayName;
                break;
            case PrintName.Level:
                print = m_world.MapName;
                break;
            case PrintName.Skill:
                print = m_world.SkillDefinition.Name;
                break;
            case PrintName.NextLevel:
                print = m_world.MapInfo.Next;
                break;
            case PrintName.NextSecret:
                print = m_world.MapInfo.SecretNext;
                break;
        }
        if (print != null && print.Length > 0)
            thread.AppendToPrintBuf(print);
        return false;
    }

    public static bool CF_TagWait(ThreadHandle thread, uint[] args)
    {
        thread.MakeTagWait(0, args.GetU(0));
        return true;
    }

    public bool CF_ThingCount(ThreadHandle thread, uint[] args)
    {
        thread.PushStack(GetThingCount(args.Get(0), args.Get(1)));
        return false;
    }

    public bool CF_ActivatorTID(ThreadHandle thread, uint[] args)
    {
        var activator = thread.GetActivator(m_world);
        if (activator == null)
        {
            thread.PushStack(0);
            return false;
        }

        thread.PushStack((uint)activator.ThingId);
        return false;
    }

    public bool CF_ChangeFloor(ThreadHandle thread, uint[] args)
    {
        var tag = args.Get(0);
        var flat = args.GetString(thread, 1);
        ActionSpecials.ChangeFlat(m_world, tag, flat, SectorPlaneFace.Floor);
        return false;
    }

    public bool CF_ChangeCeiling(ThreadHandle thread, uint[] args)
    {
        var tag = args.Get(0);
        var flat = args.GetString(thread, 1);
        ActionSpecials.ChangeFlat(m_world, tag, flat, SectorPlaneFace.Ceiling);
        return false;
    }

    public bool CF_Spawn(ThreadHandle thread, uint[] args)
    {
        var className = args.GetString(thread, 0);
        var x = args.Get(1);
        var y = args.Get(2);
        var z = args.Get(3);
        var tid = args.Get(4);
        var angle = args.Get(5);
        thread.PushStack(ActionSpecials.Spawn(m_world, className, x, y, z, tid, angle) ? 1u : 0u);
        return false;
    }

    public bool CF_SpawnSpot(ThreadHandle thread, uint[] args)
    {
        var className = args.GetString(thread, 0);
        var spotTid = args.Get(1);
        var tid = args.Get(2);
        var angle = args.Get(3);
        thread.PushStack(ActionSpecials.SpawnSpot(m_world, thread.GetActivator(m_world), className, spotTid, tid, MathHelper.FromByteAngle(angle), false) ? 1u : 0u);
        return false;
    }

    public bool CF_SpawnSpotFacing(ThreadHandle thread, uint[] args)
    {
        var className = args.GetString(thread, 0);
        var spotTid = args.Get(1);
        var tid = args.Get(2);
        thread.PushStack(ActionSpecials.SpawnSpot(m_world, thread.GetActivator(m_world), className, spotTid, tid, null, false) ? 1u : 0u);
        return false;
    }

    public bool CF_SpawnSpotFacingForced(ThreadHandle thread, uint[] args)
    {
        var className = args.GetString(thread, 0);
        var spotTid = args.Get(1);
        var tid = args.Get(2);
        thread.PushStack(ActionSpecials.SpawnSpot(m_world, thread.GetActivator(m_world), className, spotTid, tid, null, true) ? 1u : 0u);
        return false;
    }

    public bool CF_SpawnSpotForced(ThreadHandle thread, uint[] args)
    {
        var className = args.GetString(thread, 0);
        var spotTid = args.Get(1);
        var tid = args.Get(2);
        var angle = args.Get(3);
        thread.PushStack(ActionSpecials.SpawnSpot(m_world, thread.GetActivator(m_world), className, spotTid, tid, MathHelper.FromByteAngle(angle), true) ? 1u : 0u);
        return false;
    }

    private uint GetThingCount(int type, int tid)
    {
        // Check any type by tid only
        if (type == 0)
            return (uint)m_world.EntityAliveCount(tid);

        if (!ThingSpawnTypes.Lookup.TryGetValue(type, out var definitionName))
            return 0;

        var def = m_world.EntityManager.DefinitionComposer.GetByName(definitionName);
        if (def == null)
            return 0;

        return (uint)m_world.EntityAliveCount(def.Id, tid);
    }

    private bool DoPrint(ThreadHandle thread, uint[] args)
    {
        var activator = thread.GetActivator(m_world);
        var printBuf = thread.GetPrintBuf()!;

        if (activator == null)
            m_world.DisplayMessage(new DisplayMessageArgs(printBuf, null, null, IsCentered: true, ForAllPlayers: true));
        else
            m_world.DisplayMessage(new DisplayMessageArgs(printBuf, null, null, IsCentered: true, ForAllPlayers: true));

        return false;
    }
}
