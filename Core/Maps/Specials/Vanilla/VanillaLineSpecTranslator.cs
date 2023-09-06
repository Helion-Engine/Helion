using Helion.Maps.Doom.Components;
using Helion.Maps.Specials.Boom;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Lines;
using Helion.World.Special;
using NLog;

namespace Helion.Maps.Specials.Vanilla;

public static class VanillaLineSpecTranslator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static ZDoomLineSpecialType Translate(ref LineFlags lineFlags, VanillaLineSpecialType type, int tag,
        ref SpecialArgs argsToMutate, out LineActivationType lineActivationType, out LineSpecialCompatibility compatibility)
    {
        compatibility = LineSpecialCompatibility.DefaultVanilla;
        lineActivationType = GetLineTagActivation(type);
        if (type == VanillaLineSpecialType.None)
        {
            lineFlags.Activations = LineActivations.None;
            return ZDoomLineSpecialType.None;
        }

        if (BoomLineSpecTranslator.IsBoomLineSpecial((ushort)type))
            return BoomLineSpecTranslator.Translate(ref lineFlags, (ushort)type, tag, ref argsToMutate, out compatibility, out lineActivationType);

        lineFlags.Activations = GetSpecialActivations(type);
        lineFlags.Repeat = GetRepeat(type);

        switch (type)
        {
            case VanillaLineSpecialType.W1_Teleport:
            case VanillaLineSpecialType.WR_Teleport:
            case VanillaLineSpecialType.W1_MonsterTeleport:
            case VanillaLineSpecialType.WR_MonsterTeleport:
            case VanillaLineSpecialType.S1_Teleport:
            case VanillaLineSpecialType.SR_Teleport:
                argsToMutate.Arg1 = tag;
                return ZDoomLineSpecialType.Teleport;

            case VanillaLineSpecialType.W1_TeleportNoFog:
            case VanillaLineSpecialType.WR_TeleportNoFog:
            case VanillaLineSpecialType.S1_TeleportNoFog:
            case VanillaLineSpecialType.SR_TeleportNoFog:
            case VanillaLineSpecialType.W1_MonsterTeleportNoFog:
            case VanillaLineSpecialType.WR_MonsterTeportNoFog:
                argsToMutate.Arg1 = (int)TeleportType.BoomFixed;
                argsToMutate.Arg2 = tag;
                argsToMutate.Arg3 = 1;
                return ZDoomLineSpecialType.TeleportNoFog;

            case VanillaLineSpecialType.W1_TeleportLine:
            case VanillaLineSpecialType.WR_TeleportLine:
            case VanillaLineSpecialType.W1_MonsterTeleportLine:
            case VanillaLineSpecialType.WR_MonsterTeleportLine:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = tag;
                return ZDoomLineSpecialType.TeleportLine;

            case VanillaLineSpecialType.W1_TeleportLineReversed:
            case VanillaLineSpecialType.WR_TeleportLineReversed:
            case VanillaLineSpecialType.W1_MonsterTeleportLineReversed:
            case VanillaLineSpecialType.WR_MonsterTeleportLineReversed:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = tag;
                argsToMutate.Arg2 = 1;
                return ZDoomLineSpecialType.TeleportLine;

            case VanillaLineSpecialType.SR_LowerLiftRaise:
            case VanillaLineSpecialType.SR_LowerLiftFastRaise:
            case VanillaLineSpecialType.WR_LowerLiftRaise:
            case VanillaLineSpecialType.W1_LowerLiftRaise:
            case VanillaLineSpecialType.S1_LowerLiftRaise:
            case VanillaLineSpecialType.WR_LowerLiftFastRaise:
            case VanillaLineSpecialType.W1_LowerLiftFastRaise:
            case VanillaLineSpecialType.S1_LowerLiftFastRaise:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = GetDelay(type);
                return ZDoomLineSpecialType.PlatDownWaitUpStayLip;

            case VanillaLineSpecialType.D1_OpenDoorFastStay:
            case VanillaLineSpecialType.D1_OpenDoorStay:
            case VanillaLineSpecialType.GR_OpenDoorStayOpen:
            case VanillaLineSpecialType.SR_OpenDoorFastStay:
            case VanillaLineSpecialType.SR_OpenDoorStay:
            case VanillaLineSpecialType.W1_OpenDoorFastStay:
            case VanillaLineSpecialType.WR_OpenDoorStay:
            case VanillaLineSpecialType.S1_OpenDoorFastStay:
            case VanillaLineSpecialType.S1_OpenDoorStay:
            case VanillaLineSpecialType.W1_DoorOpenStay:
            case VanillaLineSpecialType.WR_OpenDoorFastStayOpen:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.DoorOpenStay;

            case VanillaLineSpecialType.D1_OpenBlueKeyStay:
            case VanillaLineSpecialType.SR_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.S1_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.D1_OpenRedKeyStay:
            case VanillaLineSpecialType.SR_OpenRedKeyFastStay:
            case VanillaLineSpecialType.S1_OpenRedKeyFastStay:
            case VanillaLineSpecialType.D1_OpenYellowKeyStay:
            case VanillaLineSpecialType.SR_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.S1_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.DR_OpenBlueKeyClose:
            case VanillaLineSpecialType.DR_OpenRedKeyClose:
            case VanillaLineSpecialType.DR_OpenYellowKeyClose:
                compatibility = new LineSpecialCompatibility();
                HandleDoor(type, tag, ref argsToMutate, ref compatibility);
                return ZDoomLineSpecialType.DoorLockedRaise;

            case VanillaLineSpecialType.DR_DoorOpenClose:
            case VanillaLineSpecialType.W1_DoorOpenClose:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = GetDelay(type);
                lineFlags.Activations |= LineActivations.Monster;
                return ZDoomLineSpecialType.DoorOpenClose;

            case VanillaLineSpecialType.WR_OpenDoorClose:
            case VanillaLineSpecialType.WR_OpenDoorFastClose:
            case VanillaLineSpecialType.SR_OpenDoorClose:
            case VanillaLineSpecialType.SR_OpenDoorFastClose:
            case VanillaLineSpecialType.DR_OpenDoorFastClose:
            case VanillaLineSpecialType.W1_OpenDoorFastClose:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = GetDelay(type);
                return ZDoomLineSpecialType.DoorOpenClose;

            case VanillaLineSpecialType.S1_OpenDoorClose:
            case VanillaLineSpecialType.S1_OpenDoorFastClose:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = GetDelay(type);
                return ZDoomLineSpecialType.DoorOpenClose;

            case VanillaLineSpecialType.W1_CloseDoor:
            case VanillaLineSpecialType.SR_CloseDoor:
            case VanillaLineSpecialType.S1_CloseDoor:
            case VanillaLineSpecialType.WR_CloseDoor:
            case VanillaLineSpecialType.WR_CloseDoorFast:
            case VanillaLineSpecialType.W1_CloseDoorFast:
            case VanillaLineSpecialType.S1_CloseDoorFast:
            case VanillaLineSpecialType.SR_CloseDoorFast:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.DoorClose;

            case VanillaLineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_LowerFloorToHighestAdjacentFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.FloorLowerToHighest;

            case VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.FloorLowerToLowest;

            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloorChangeTexture:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.FloorLowerToLowestTxTy;

            case VanillaLineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.S1_RaiseFloorToLowestAdjacentCeiling:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.FloorRaiseToLowestCeiling;

            case VanillaLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 136;
                return ZDoomLineSpecialType.FloorLowerToHighest;

            case VanillaLineSpecialType.WR_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.S1_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.W1_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigher:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.FloorRaiseToNearest;

            case VanillaLineSpecialType.S1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
            case VanillaLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 0; // Lockout ???
                return ZDoomLineSpecialType.PlatRaiseAndStay;

            case VanillaLineSpecialType.W1_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.WR_RaiseByShortestLowerTexture:
            case VanillaLineSpecialType.S1_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.SR_RaiseFloorByShortestLowerTexture:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.FloorRaiseByTexture;

            case VanillaLineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 10; // Damage
                argsToMutate.Arg3 = (byte)ZDoomCrushMode.DoomNoSlowDown;
                return ZDoomLineSpecialType.FloorRaiseAndCrushDoom;

            case VanillaLineSpecialType.W1_FastCrusherCeiling:
            case VanillaLineSpecialType.WR_FastCrusherCeilingSlowDamage:
            case VanillaLineSpecialType.S1_FastCrusherCeiling:
            case VanillaLineSpecialType.SR_FastCrusherCeiling:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 8; // Distance above floor
                argsToMutate.Arg2 = GetSectorMoveSpeed(type);
                argsToMutate.Arg3 = 10; // Damage
                argsToMutate.Arg4 = (byte)ZDoomCrushMode.DoomNoSlowDown;
                return ZDoomLineSpecialType.CeilingCrushAndRaiseDist;

            case VanillaLineSpecialType.W1_SlowCrusherCeiling:
            case VanillaLineSpecialType.WR_SlowCrusherCeilingFastDamage:
            case VanillaLineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
            case VanillaLineSpecialType.SR_SlowCrusherCeiling:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 8; // Distance above floor
                argsToMutate.Arg2 = GetSectorMoveSpeed(type);
                argsToMutate.Arg3 = 8; // Damage
                argsToMutate.Arg4 = (byte)ZDoomCrushMode.DoomWithSlowDown;
                return ZDoomLineSpecialType.CeilingCrushAndRaiseDist;

            case VanillaLineSpecialType.W1_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.WR_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.S1_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.SR_QuietCrusherCeilingFastDamage:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 8; // Distance above floor
                argsToMutate.Arg2 = GetSectorMoveSpeed(type);
                argsToMutate.Arg3 = 8; // Damage
                argsToMutate.Arg4 = (byte)ZDoomCrushMode.DoomWithSlowDown;
                return ZDoomLineSpecialType.CeilingCrushRaiseSilent;

            case VanillaLineSpecialType.WR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFour:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 24;
                return ZDoomLineSpecialType.FloorRaiseByValue;

            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFourChangeTexture:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 24;
                return ZDoomLineSpecialType.PlatUpValueStayTx;

            case VanillaLineSpecialType.S1_RaiseFloorThirtyTwoChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoChangeTexture:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 32;
                return ZDoomLineSpecialType.PlatUpValueStayTx;

            case VanillaLineSpecialType.W1_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.WR_FloorRaiseByTwentyFourChangeTextureType:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTextureType:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 24;
                return ZDoomLineSpecialType.FloorRaiseByValueTxTy;

            case VanillaLineSpecialType.W1_FloorRaiseByThirtyTwoChangeTextureType:
            case VanillaLineSpecialType.WR_FloorRaiseByThirtyTwoChangeTextureType:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 32;
                return ZDoomLineSpecialType.FloorRaiseByValueTxTy;

            case VanillaLineSpecialType.S1_RaiseFloor512:
            case VanillaLineSpecialType.W1_RaiseFloor512:
            case VanillaLineSpecialType.WR_RaiseFloor512:
            case VanillaLineSpecialType.SR_RaiseFloor512:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 64;
                return ZDoomLineSpecialType.FloorRaiseByValueTimes8;

            case VanillaLineSpecialType.WR_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.W1_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.S1_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.SR_LowerCeilingToEightAboveFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 0;
                argsToMutate.Arg3 = (byte)ZDoomCrushMode.DoomWithSlowDown;
                return ZDoomLineSpecialType.CeilingCrushStayDown;

            case VanillaLineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 0;
                return ZDoomLineSpecialType.CeilingRaiseToHighest;

            case VanillaLineSpecialType.W1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.WR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.S1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.SR_StartMovingFloorPerpetual:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = GetDelay(type);
                return ZDoomLineSpecialType.PlatPerpetualRaiseLip;

            case VanillaLineSpecialType.S1_RaiseStairs8:
            case VanillaLineSpecialType.W1_RaiseStairs8:
            case VanillaLineSpecialType.WR_RaiseStairs8:
            case VanillaLineSpecialType.SR_RaiseStairs8:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 8;
                return ZDoomLineSpecialType.StairsBuildUpDoom;

            case VanillaLineSpecialType.W1_RaiseStairsFast:
            case VanillaLineSpecialType.S1_RaiseStairsFast:
            case VanillaLineSpecialType.WR_RaiseStairsFast:
            case VanillaLineSpecialType.SR_RaiseStairsFast:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 16;
                return ZDoomLineSpecialType.StairsBuildUpDoomCrush;

            case VanillaLineSpecialType.S1_LowerCeilingToFloor:
            case VanillaLineSpecialType.SR_LowerCeilingToFloor:
            case VanillaLineSpecialType.W1_LowerCeilingToFloor:
            case VanillaLineSpecialType.WR_LowerCeilingToFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.CeilingLowerToFloor;

            case VanillaLineSpecialType.W1_LightOnMaxBrightness:
            case VanillaLineSpecialType.WR_LightOnMaxBrightness:
            case VanillaLineSpecialType.SR_LightOnMaxBrightness:
            case VanillaLineSpecialType.S1_LightOnMaxBrightness:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 255; // Brightness
                return ZDoomLineSpecialType.LightChangeToValue;

            case VanillaLineSpecialType.W1_LightOffMinBrightness:
            case VanillaLineSpecialType.WR_LightOffMinBrightness:
            case VanillaLineSpecialType.SR_LightOffMinBrightness:
            case VanillaLineSpecialType.S1_LightOffMinBrightness:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 35; // Brightness
                return ZDoomLineSpecialType.LightChangeToValue;

            case VanillaLineSpecialType.W1_LightLevelMatchBrightness:
            case VanillaLineSpecialType.WR_LightLevelMatchBrightestAdjacent:
            case VanillaLineSpecialType.S1_LightLevelMatchBrightness:
            case VanillaLineSpecialType.SR_LightLevelMatchBrightness:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.LightMaxNeighbor;

            case VanillaLineSpecialType.W1_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.WR_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.S1_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.SR_LightMatchDimmestAdjacent:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.LightMinNeighbor;

            case VanillaLineSpecialType.W1_StopMovingFloor:
            case VanillaLineSpecialType.WR_StopMovingFloor:
            case VanillaLineSpecialType.S1_StopMovingFloor:
            case VanillaLineSpecialType.SR_StopMovingFloor:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.PlatStop;

            case VanillaLineSpecialType.W1_StopCrusherCeiling:
            case VanillaLineSpecialType.WR_StopCrusherCeiling:
            case VanillaLineSpecialType.S1_StopCrusherCeiling:
            case VanillaLineSpecialType.SR_StopCrusherCeiling:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.CeilingCrushStop;

            case VanillaLineSpecialType.W1_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.WR_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.S1_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.SR_CloseDoorOpenThirtySeconds:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 35 * 30; // Wait tics
                return ZDoomLineSpecialType.DoorCloseWaitOpen;

            case VanillaLineSpecialType.W1_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.WR_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.S1_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.SR_BlinkLightStartEveryOneSecond:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 5; // Tics to stay at upper light level
                argsToMutate.Arg2 = 35; // Tics to stay at lower light level
                return ZDoomLineSpecialType.LightStrobeDoom;

            case VanillaLineSpecialType.S1_Donut:
            case VanillaLineSpecialType.W1_Donut:
            case VanillaLineSpecialType.WR_Donut:
            case VanillaLineSpecialType.SR_Donut:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = VanillaConstants.DonutSpeed; // Pillar speed
                argsToMutate.Arg2 = VanillaConstants.DonutSpeed; // Surrounding speed
                return ZDoomLineSpecialType.FloorDonut;

            case VanillaLineSpecialType.ScrollTextureLeft:
                argsToMutate.Arg0 = 64; // Speed
                return ZDoomLineSpecialType.ScrollTextureLeft;

            case VanillaLineSpecialType.ScrollTextureRight:
                argsToMutate.Arg0 = 64;
                return ZDoomLineSpecialType.ScrollTextureRight;

            case VanillaLineSpecialType.ScrollTextureOffsets:
                return ZDoomLineSpecialType.ScrollUsingTextureOffsets;

            case VanillaLineSpecialType.S_EndLevel:
            case VanillaLineSpecialType.W_EndLevel:
            case VanillaLineSpecialType.G_EndLevel:
                return ZDoomLineSpecialType.ExitNormal;

            case VanillaLineSpecialType.S_EndLevelSecret:
            case VanillaLineSpecialType.W_EndLevelSecret:
            case VanillaLineSpecialType.G_EndLevelSecret:
                return ZDoomLineSpecialType.ExitSecret;

            case VanillaLineSpecialType.SR_FloorTransferNumeric:
            case VanillaLineSpecialType.W1_FloorTransferNumeric:
            case VanillaLineSpecialType.WR_FloorTransferNumeric:
            case VanillaLineSpecialType.S1_FloorTransferNumeric:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.FloorTransferNumeric;

            case VanillaLineSpecialType.W1_FloorTransferTrigger:
            case VanillaLineSpecialType.WR_FloorTransferTrigger:
            case VanillaLineSpecialType.S1_FloorTransferTrigger:
            case VanillaLineSpecialType.SR_FloorTransferTrigger:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.FloorTransferTrigger;

            case VanillaLineSpecialType.WR_CeilingToHighestFloorToLowest:
            case VanillaLineSpecialType.S1_CeilingToHighestFloorToLowest:
            case VanillaLineSpecialType.SR_CeilingToHighestFloorToLowest:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 8; // Floor speed
                argsToMutate.Arg2 = 8; // Ceiling speed
                argsToMutate.Arg3 = type == VanillaLineSpecialType.WR_CeilingToHighestFloorToLowest ? 0 : 1998;
                return ZDoomLineSpecialType.FloorAndCeilingLowerRaise;

            case VanillaLineSpecialType.W1_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.WR_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.S1_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_CeilingLowerToLowestAdjacentCeiling:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 8;
                return ZDoomLineSpecialType.CeilingLowerToLowest;

            case VanillaLineSpecialType.W1_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_CeilingLowerToHighestAdjacentFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 8;
                return ZDoomLineSpecialType.CeilingLowerToHighestFloor;

            case VanillaLineSpecialType.W1_LowerFloorToNearest:
            case VanillaLineSpecialType.WR_LowerFloorToNearest:
            case VanillaLineSpecialType.S1_LowerFloorToNearest:
            case VanillaLineSpecialType.SR_LowerFloorToNearest:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 8;
                return ZDoomLineSpecialType.FloorLowerToNearest;

            case VanillaLineSpecialType.SR_ToggleFloorToCeiling:
            case VanillaLineSpecialType.WR_ToggleFloorToCeiling:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.PlatToggleCeiling;

            case VanillaLineSpecialType.TransferFloorLight:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.TransferFloorLight;

            case VanillaLineSpecialType.TransferCeilingLight:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.TransferCeilingLight;

            case VanillaLineSpecialType.W1_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.WR_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.S1_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.SR_ElevatorRaiseToNearest:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 32;
                return ZDoomLineSpecialType.ElevatorRaiseToNearest;

            case VanillaLineSpecialType.W1_ElevatorLowerToNearest:
            case VanillaLineSpecialType.WR_ElevatorLowerToNearest:
            case VanillaLineSpecialType.S1_ElevatorLowerToNearest:
            case VanillaLineSpecialType.SR_ElevatorLowerToNearest:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 32;
                return ZDoomLineSpecialType.ElevatorLowerToNearest;

            case VanillaLineSpecialType.W1_ElevatorMoveToActivatingFloor:
            case VanillaLineSpecialType.WR_ElevatorMoveToActivatingFloor:
            case VanillaLineSpecialType.S1_ElevatorMoveToActivatingFloor:
            case VanillaLineSpecialType.SR_ElevatorMoveToActivatingFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 32;
                return ZDoomLineSpecialType.ElevatorMoveToFloor;

            case VanillaLineSpecialType.ScrollAccelTaggedFloorFirstSide:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line | (int)ZDoomScroll.Accelerative;
                argsToMutate.Arg2 = (int)ZDoomPlaneScrollType.Scroll;
                return ZDoomLineSpecialType.ScrollFloor;

            case VanillaLineSpecialType.ScrollAccelObjectsTaggedFloorFirstSide:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line | (int)ZDoomScroll.Accelerative;
                argsToMutate.Arg2 = (int)ZDoomPlaneScrollType.Carry;
                return ZDoomLineSpecialType.ScrollFloor;

            case VanillaLineSpecialType.ScrollAccelObjectsFloorFirstSide:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line | (int)ZDoomScroll.Accelerative;
                argsToMutate.Arg2 = (int)ZDoomPlaneScrollType.ScrollAndCarry;
                return ZDoomLineSpecialType.ScrollFloor;

            case VanillaLineSpecialType.ScrollTaggedFloorFirstSide:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line | (int)ZDoomScroll.Displacement;
                argsToMutate.Arg2 = (int)ZDoomPlaneScrollType.Scroll;
                return ZDoomLineSpecialType.ScrollFloor;

            case VanillaLineSpecialType.PushObjectsTaggedFloorFirstSide:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line | (int)ZDoomScroll.Displacement;
                argsToMutate.Arg2 = (int)ZDoomPlaneScrollType.Carry;
                return ZDoomLineSpecialType.ScrollFloor;

            case VanillaLineSpecialType.PushObjectsAndFloorTaggedFirstSide:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line | (int)ZDoomScroll.Displacement;
                argsToMutate.Arg2 = (int)ZDoomPlaneScrollType.ScrollAndCarry;
                return ZDoomLineSpecialType.ScrollFloor;

            case VanillaLineSpecialType.ScrollTaggedFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line;
                argsToMutate.Arg2 = (int)ZDoomPlaneScrollType.Scroll;
                return ZDoomLineSpecialType.ScrollFloor;

            case VanillaLineSpecialType.CarryObjectsTaggedFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line;
                argsToMutate.Arg2 = (int)ZDoomPlaneScrollType.Carry;
                return ZDoomLineSpecialType.ScrollFloor;

            case VanillaLineSpecialType.ScrollTagedFloorAndCarryObjects:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line;
                argsToMutate.Arg2 = (int)ZDoomPlaneScrollType.ScrollAndCarry;
                return ZDoomLineSpecialType.ScrollFloor;

            case VanillaLineSpecialType.ScrollAccelTaggedCeiling:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line | (int)ZDoomScroll.Accelerative;
                return ZDoomLineSpecialType.ScrollCeiling;

            case VanillaLineSpecialType.ScrollTaggedCeilingFirstSide:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line | (int)ZDoomScroll.Displacement;
                return ZDoomLineSpecialType.ScrollCeiling;

            case VanillaLineSpecialType.ScrollTaggedCeiling:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Line;
                return ZDoomLineSpecialType.ScrollCeiling;

            case VanillaLineSpecialType.SectorSetWind:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg3 = 1; // Use line for angle / magnitude
                return ZDoomLineSpecialType.SectorSetWind;

            case VanillaLineSpecialType.SectorSetCurrent:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg3 = 1; // Use line for angle / magnitude
                return ZDoomLineSpecialType.SectorSetCurrent;

            case VanillaLineSpecialType.SetPush:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg3 = 1; // Use line for angle / magnitude
                return ZDoomLineSpecialType.PointPushSetForce;

            case VanillaLineSpecialType.SectorSetFriction:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg3 = 1; // Use line for angle / magnitude
                return ZDoomLineSpecialType.SectorSetFriction;

            case VanillaLineSpecialType.ScrollAccellTaggedWallFirstSide:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Accelerative;
                return ZDoomLineSpecialType.ScrollTextureModel;

            case VanillaLineSpecialType.ScrollTaggedWallFirstSide:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Displacement;
                return ZDoomLineSpecialType.ScrollTextureModel;

            case VanillaLineSpecialType.ScrollTaggedWallSameAsFloorCeiling:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.ScrollTextureModel;

            case VanillaLineSpecialType.TranslucentLine:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 168;
                return ZDoomLineSpecialType.TranslucentLine;

            case VanillaLineSpecialType.TransferSky:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomStaticInit.Sky;
                return ZDoomLineSpecialType.StaticInit;

            case VanillaLineSpecialType.TransferSkyFlipped:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomStaticInit.Sky;
                argsToMutate.Arg2 = 1;
                return ZDoomLineSpecialType.StaticInit;

            case VanillaLineSpecialType.TransferHeights:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.TransferHeights;

            case VanillaLineSpecialType.StandardScrollMbf21:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.OffsetSpeed;
                return ZDoomLineSpecialType.ScrollTextureModel;

            case VanillaLineSpecialType.AccelerativeScrollMbf21:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Accelerative | (int)ZDoomScroll.OffsetSpeed;
                return ZDoomLineSpecialType.ScrollTextureModel;

            case VanillaLineSpecialType.DisplacementScrollMbf21:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = (int)ZDoomScroll.Displacement | (int)ZDoomScroll.OffsetSpeed;
                return ZDoomLineSpecialType.ScrollTextureModel;

            default:
                Log.Error($"Missing type in VanillaLineSpecTranslator: [{(int)type}]{type}");
                break;
        }

        return ZDoomLineSpecialType.None;
    }

    public static void FinalizeLine(DoomLine doomLine, Line line)
    {
        // Based on testing boom seems to do this for every line...
        line.LineId = doomLine.SectorTag;
    }

    private static void HandleDoor(VanillaLineSpecialType type, int tag, ref SpecialArgs argsToMutate, ref LineSpecialCompatibility compatibility)
    {
        switch (type)
        {
            case VanillaLineSpecialType.D1_OpenBlueKeyStay:
            case VanillaLineSpecialType.D1_OpenRedKeyStay:
            case VanillaLineSpecialType.D1_OpenYellowKeyStay:
            case VanillaLineSpecialType.DR_OpenBlueKeyClose:
            case VanillaLineSpecialType.DR_OpenRedKeyClose:
            case VanillaLineSpecialType.DR_OpenYellowKeyClose:
                compatibility.CompatibilityType = LineSpecialCompatibilityType.KeyDoor;
                break;

            default:
                compatibility.CompatibilityType = LineSpecialCompatibilityType.KeyObject;
                break;
        }

        argsToMutate.Arg0 = tag;
        argsToMutate.Arg1 = GetSectorMoveSpeed(type);
        argsToMutate.Arg2 = GetDelay(type);
        argsToMutate.Arg3 = GetDoorKey(type);
    }

    private static LineActivationType GetLineTagActivation(VanillaLineSpecialType type)
    {
        switch (type)
        {
            case VanillaLineSpecialType.D1_OpenDoorStay:
            case VanillaLineSpecialType.D1_OpenBlueKeyStay:
            case VanillaLineSpecialType.D1_OpenRedKeyStay:
            case VanillaLineSpecialType.D1_OpenYellowKeyStay:
            case VanillaLineSpecialType.D1_OpenDoorFastStay:
            case VanillaLineSpecialType.DR_DoorOpenClose:
            case VanillaLineSpecialType.DR_OpenBlueKeyClose:
            case VanillaLineSpecialType.DR_OpenYellowKeyClose:
            case VanillaLineSpecialType.DR_OpenRedKeyClose:
            case VanillaLineSpecialType.DR_OpenDoorFastClose:
                return LineActivationType.BackSide;

            case VanillaLineSpecialType.S1_RaiseStairs8:
            case VanillaLineSpecialType.S1_Donut:
            case VanillaLineSpecialType.S1_RaiseFloorThirtyTwoChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
            case VanillaLineSpecialType.S1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.S1_LowerLiftRaise:
            case VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.S1_OpenDoorClose:
            case VanillaLineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
            case VanillaLineSpecialType.S1_CloseDoor:
            case VanillaLineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.S1_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_OpenDoorStay:
            case VanillaLineSpecialType.S1_OpenDoorFastClose:
            case VanillaLineSpecialType.S1_OpenDoorFastStay:
            case VanillaLineSpecialType.S1_CloseDoorFast:
            case VanillaLineSpecialType.S1_LowerLiftFastRaise:
            case VanillaLineSpecialType.S1_RaiseStairsFast:
            case VanillaLineSpecialType.S1_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.S1_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.S1_OpenRedKeyFastStay:
            case VanillaLineSpecialType.S1_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.S1_RaiseFloor512:
            case VanillaLineSpecialType.SR_CloseDoor:
            case VanillaLineSpecialType.SR_LowerCeilingToFloor:
            case VanillaLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.SR_OpenDoorStay:
            case VanillaLineSpecialType.SR_LowerLiftRaise:
            case VanillaLineSpecialType.SR_OpenDoorClose:
            case VanillaLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigher:
            case VanillaLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.SR_OpenDoorFastClose:
            case VanillaLineSpecialType.SR_OpenDoorFastStay:
            case VanillaLineSpecialType.SR_CloseDoorFast:
            case VanillaLineSpecialType.SR_LowerLiftFastRaise:
            case VanillaLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.SR_OpenRedKeyFastStay:
            case VanillaLineSpecialType.SR_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.SR_LightOnMaxBrightness:
            case VanillaLineSpecialType.SR_LightOffMinBrightness:
            case VanillaLineSpecialType.W1_CloseDoor:
            case VanillaLineSpecialType.SR_FloorTransferNumeric:
            case VanillaLineSpecialType.S1_FloorTransferTrigger:
            case VanillaLineSpecialType.SR_FloorTransferTrigger:
            case VanillaLineSpecialType.S1_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.S1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.S1_StopMovingFloor:
            case VanillaLineSpecialType.S1_FastCrusherCeiling:
            case VanillaLineSpecialType.S1_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.S1_CeilingToHighestFloorToLowest:
            case VanillaLineSpecialType.S1_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.S1_StopCrusherCeiling:
            case VanillaLineSpecialType.S1_LightOffMinBrightness:
            case VanillaLineSpecialType.S1_LightOnMaxBrightness:
            case VanillaLineSpecialType.S1_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.S1_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.S1_Teleport:
            case VanillaLineSpecialType.S1_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.SR_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloor512:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.SR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.SR_StopMovingFloor:
            case VanillaLineSpecialType.SR_FastCrusherCeiling:
            case VanillaLineSpecialType.SR_SlowCrusherCeiling:
            case VanillaLineSpecialType.SR_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.SR_CeilingToHighestFloorToLowest:
            case VanillaLineSpecialType.SR_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.SR_StopCrusherCeiling:
            case VanillaLineSpecialType.SR_Donut:
            case VanillaLineSpecialType.SR_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.SR_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.SR_Teleport:
            case VanillaLineSpecialType.SR_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.S1_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.S1_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_TeleportNoFog:
            case VanillaLineSpecialType.SR_TeleportNoFog:
            case VanillaLineSpecialType.S1_LowerFloorToNearest:
            case VanillaLineSpecialType.SR_LowerFloorToNearest:
            case VanillaLineSpecialType.SR_ToggleFloorToCeiling:
            case VanillaLineSpecialType.S1_ElevatorLowerToNearest:
            case VanillaLineSpecialType.SR_ElevatorLowerToNearest:
            case VanillaLineSpecialType.S1_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.SR_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.S1_ElevatorMoveToActivatingFloor:
            case VanillaLineSpecialType.SR_ElevatorMoveToActivatingFloor:
                return LineActivationType.Tag;

            default:
                break;
        }

        return LineActivationType.Any;
    }

    private static byte GetDoorKey(VanillaLineSpecialType type)
    {
        switch (type)
        {
            case VanillaLineSpecialType.DR_OpenRedKeyClose:
            case VanillaLineSpecialType.D1_OpenRedKeyStay:
            case VanillaLineSpecialType.SR_OpenRedKeyFastStay:
            case VanillaLineSpecialType.S1_OpenRedKeyFastStay:
                return (byte)ZDoomKeyType.RedAny;

            case VanillaLineSpecialType.DR_OpenBlueKeyClose:
            case VanillaLineSpecialType.D1_OpenBlueKeyStay:
            case VanillaLineSpecialType.SR_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.S1_OpenBlueKeyFastStay:
                return (byte)ZDoomKeyType.BlueAny;

            case VanillaLineSpecialType.DR_OpenYellowKeyClose:
            case VanillaLineSpecialType.D1_OpenYellowKeyStay:
            case VanillaLineSpecialType.SR_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.S1_OpenYellowKeyFastStay:
                return (byte)ZDoomKeyType.YellowAny;

            default:
                break;
        }

        return 0;
    }

    private static bool IsDoorWithDelay(VanillaLineSpecialType type)
    {
        switch (type)
        {
            case VanillaLineSpecialType.DR_DoorOpenClose:
            case VanillaLineSpecialType.DR_OpenBlueKeyClose:
            case VanillaLineSpecialType.DR_OpenDoorFastClose:
            case VanillaLineSpecialType.DR_OpenRedKeyClose:
            case VanillaLineSpecialType.DR_OpenYellowKeyClose:
            case VanillaLineSpecialType.SR_OpenDoorClose:
            case VanillaLineSpecialType.SR_OpenDoorFastClose:
            case VanillaLineSpecialType.S1_OpenDoorClose:
            case VanillaLineSpecialType.S1_OpenDoorFastClose:
            case VanillaLineSpecialType.W1_DoorOpenClose:
            case VanillaLineSpecialType.W1_OpenDoorFastClose:
            case VanillaLineSpecialType.WR_OpenDoorClose:
            case VanillaLineSpecialType.WR_OpenDoorFastClose:
                return true;

            default:
                break;
        }

        return false;
    }

    private static bool IsLift(VanillaLineSpecialType type)
    {
        switch (type)
        {
            case VanillaLineSpecialType.SR_LowerLiftRaise:
            case VanillaLineSpecialType.SR_LowerLiftFastRaise:
            case VanillaLineSpecialType.WR_LowerLiftRaise:
            case VanillaLineSpecialType.W1_LowerLiftRaise:
            case VanillaLineSpecialType.S1_LowerLiftRaise:
            case VanillaLineSpecialType.WR_LowerLiftFastRaise:
            case VanillaLineSpecialType.W1_LowerLiftFastRaise:
            case VanillaLineSpecialType.S1_LowerLiftFastRaise:
                return true;

            default:
                break;
        }

        return false;
    }

    private static byte GetSectorMoveSpeed(VanillaLineSpecialType type)
    {
        switch (type)
        {
            case VanillaLineSpecialType.W1_SlowCrusherCeiling:
            case VanillaLineSpecialType.W1_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
            case VanillaLineSpecialType.WR_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.WR_SlowCrusherCeilingFastDamage:
            case VanillaLineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.W1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.WR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.W1_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.WR_RaiseByShortestLowerTexture:
            case VanillaLineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
            case VanillaLineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
            case VanillaLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigher:
            case VanillaLineSpecialType.S1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.S1_RaiseFloor512:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.W1_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.WR_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.SR_LowerCeilingToFloor:
            case VanillaLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.W1_RaiseFloor512:
            case VanillaLineSpecialType.WR_RaiseFloor512:
            case VanillaLineSpecialType.S1_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.S1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.S1_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.SR_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.SR_RaiseFloor512:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.SR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.SR_SlowCrusherCeiling:
            case VanillaLineSpecialType.SR_LowerCeilingToEightAboveFloor:
                return VanillaConstants.SectorSlowSpeed;

            case VanillaLineSpecialType.W1_FastCrusherCeiling:
            case VanillaLineSpecialType.WR_FastCrusherCeilingSlowDamage:
            case VanillaLineSpecialType.W1_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.S1_LowerCeilingToFloor:
            case VanillaLineSpecialType.W1_LowerCeilingToFloor:
            case VanillaLineSpecialType.WR_LowerCeilingToFloor:
            case VanillaLineSpecialType.WR_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.S1_FastCrusherCeiling:
            case VanillaLineSpecialType.S1_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.SR_FastCrusherCeiling:
            case VanillaLineSpecialType.SR_QuietCrusherCeilingFastDamage:
                return VanillaConstants.SectorFastSpeed;

            case VanillaLineSpecialType.S1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
            case VanillaLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorThirtyTwoChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoChangeTexture:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.W1_FloorRaiseByThirtyTwoChangeTextureType:
            case VanillaLineSpecialType.WR_FloorRaiseByTwentyFourChangeTextureType:
            case VanillaLineSpecialType.WR_FloorRaiseByThirtyTwoChangeTextureType:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTextureType:
                return VanillaConstants.FloorSlowSpeed;

            case VanillaLineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloorChangeTexture:
                return VanillaConstants.LiftSlowSpeed;

            case VanillaLineSpecialType.W1_LowerLiftRaise:
            case VanillaLineSpecialType.S1_LowerLiftRaise:
            case VanillaLineSpecialType.SR_LowerLiftRaise:
            case VanillaLineSpecialType.WR_LowerLiftRaise:
            case VanillaLineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
                return VanillaConstants.LiftFastSpeed;

            case VanillaLineSpecialType.WR_LowerLiftFastRaise:
            case VanillaLineSpecialType.W1_LowerLiftFastRaise:
            case VanillaLineSpecialType.S1_LowerLiftFastRaise:
            case VanillaLineSpecialType.SR_LowerLiftFastRaise:
                return VanillaConstants.LiftTurboSpeed;

            case VanillaLineSpecialType.DR_OpenRedKeyClose:
            case VanillaLineSpecialType.DR_OpenBlueKeyClose:
            case VanillaLineSpecialType.DR_OpenYellowKeyClose:
            case VanillaLineSpecialType.D1_OpenRedKeyStay:
            case VanillaLineSpecialType.D1_OpenBlueKeyStay:
            case VanillaLineSpecialType.D1_OpenYellowKeyStay:
            case VanillaLineSpecialType.DR_DoorOpenClose:
            case VanillaLineSpecialType.W1_DoorOpenStay:
            case VanillaLineSpecialType.W1_CloseDoor:
            case VanillaLineSpecialType.W1_DoorOpenClose:
            case VanillaLineSpecialType.W1_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.S1_OpenDoorClose:
            case VanillaLineSpecialType.D1_OpenDoorStay:
            case VanillaLineSpecialType.SR_CloseDoor:
            case VanillaLineSpecialType.GR_OpenDoorStayOpen:
            case VanillaLineSpecialType.S1_CloseDoor:
            case VanillaLineSpecialType.SR_OpenDoorStay:
            case VanillaLineSpecialType.SR_OpenDoorClose:
            case VanillaLineSpecialType.WR_CloseDoor:
            case VanillaLineSpecialType.WR_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.WR_OpenDoorStay:
            case VanillaLineSpecialType.WR_OpenDoorClose:
            case VanillaLineSpecialType.S1_OpenDoorStay:
            case VanillaLineSpecialType.S1_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.SR_CloseDoorOpenThirtySeconds:
                return VanillaConstants.DoorSlowSpeed;

            case VanillaLineSpecialType.SR_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.S1_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.WR_OpenDoorFastClose:
            case VanillaLineSpecialType.WR_OpenDoorFastStayOpen:
            case VanillaLineSpecialType.W1_OpenDoorFastClose:
            case VanillaLineSpecialType.W1_OpenDoorFastStay:
            case VanillaLineSpecialType.S1_OpenDoorFastClose:
            case VanillaLineSpecialType.S1_OpenDoorFastStay:
            case VanillaLineSpecialType.SR_OpenDoorFastClose:
            case VanillaLineSpecialType.SR_OpenDoorFastStay:
            case VanillaLineSpecialType.DR_OpenDoorFastClose:
            case VanillaLineSpecialType.D1_OpenDoorFastStay:
            case VanillaLineSpecialType.S1_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.SR_OpenRedKeyFastStay:
            case VanillaLineSpecialType.S1_OpenRedKeyFastStay:
            case VanillaLineSpecialType.SR_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.WR_CloseDoorFast:
            case VanillaLineSpecialType.W1_CloseDoorFast:
            case VanillaLineSpecialType.S1_CloseDoorFast:
            case VanillaLineSpecialType.SR_CloseDoorFast:
                return VanillaConstants.DoorFastSpeed;

            case VanillaLineSpecialType.S1_RaiseStairs8:
            case VanillaLineSpecialType.W1_RaiseStairs8:
            case VanillaLineSpecialType.WR_RaiseStairs8:
            case VanillaLineSpecialType.SR_RaiseStairs8:
                return VanillaConstants.StairSlowSpeed;

            case VanillaLineSpecialType.W1_RaiseStairsFast:
            case VanillaLineSpecialType.S1_RaiseStairsFast:
            case VanillaLineSpecialType.WR_RaiseStairsFast:
            case VanillaLineSpecialType.SR_RaiseStairsFast:
                return VanillaConstants.StairFastSpeed;

            default:
                break;
        }

        return 0;
    }

    private static byte GetDelay(VanillaLineSpecialType type)
    {
        if (IsDoorWithDelay(type))
            return VanillaConstants.DoorDelay;
        if (IsLift(type))
            return VanillaConstants.LiftDelay;

        switch (type)
        {
            case VanillaLineSpecialType.W1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.WR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.S1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.SR_StartMovingFloorPerpetual:
                return VanillaConstants.LiftDelay;

            default:
                break;
        }

        return 0;
    }

    private static LineActivations GetSpecialActivations(VanillaLineSpecialType type)
    {
        LineActivations activations = LineActivations.None;

        switch (type)
        {
            case VanillaLineSpecialType.DR_DoorOpenClose:
            case VanillaLineSpecialType.S1_RaiseStairs8:
            case VanillaLineSpecialType.S1_Donut:
            case VanillaLineSpecialType.S_EndLevel:
            case VanillaLineSpecialType.S1_RaiseFloorThirtyTwoChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
            case VanillaLineSpecialType.S1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.S1_LowerLiftRaise:
            case VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.DR_OpenBlueKeyClose:
            case VanillaLineSpecialType.DR_OpenYellowKeyClose:
            case VanillaLineSpecialType.DR_OpenRedKeyClose:
            case VanillaLineSpecialType.D1_OpenDoorStay:
            case VanillaLineSpecialType.D1_OpenBlueKeyStay:
            case VanillaLineSpecialType.D1_OpenRedKeyStay:
            case VanillaLineSpecialType.D1_OpenYellowKeyStay:
            case VanillaLineSpecialType.S1_OpenDoorClose:
            case VanillaLineSpecialType.SR_CloseDoor:
            case VanillaLineSpecialType.SR_LowerCeilingToFloor:
            case VanillaLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
            case VanillaLineSpecialType.S1_CloseDoor:
            case VanillaLineSpecialType.S_EndLevelSecret:
            case VanillaLineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.SR_OpenDoorStay:
            case VanillaLineSpecialType.SR_LowerLiftRaise:
            case VanillaLineSpecialType.SR_OpenDoorClose:
            case VanillaLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigher:
            case VanillaLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.S1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.S1_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_OpenDoorStay:
            case VanillaLineSpecialType.S1_OpenDoorFastClose:
            case VanillaLineSpecialType.S1_OpenDoorFastStay:
            case VanillaLineSpecialType.S1_CloseDoorFast:
            case VanillaLineSpecialType.SR_OpenDoorFastClose:
            case VanillaLineSpecialType.SR_OpenDoorFastStay:
            case VanillaLineSpecialType.SR_CloseDoorFast:
            case VanillaLineSpecialType.DR_OpenDoorFastClose:
            case VanillaLineSpecialType.D1_OpenDoorFastStay:
            case VanillaLineSpecialType.S1_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.S1_LowerLiftFastRaise:
            case VanillaLineSpecialType.SR_LowerLiftFastRaise:
            case VanillaLineSpecialType.S1_RaiseStairsFast:
            case VanillaLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.S1_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.SR_OpenRedKeyFastStay:
            case VanillaLineSpecialType.S1_OpenRedKeyFastStay:
            case VanillaLineSpecialType.SR_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.S1_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.SR_LightOnMaxBrightness:
            case VanillaLineSpecialType.SR_LightOffMinBrightness:
            case VanillaLineSpecialType.S1_RaiseFloor512:
            case VanillaLineSpecialType.S1_LowerCeilingToFloor:
            case VanillaLineSpecialType.SR_FloorTransferNumeric:
            case VanillaLineSpecialType.S1_FloorTransferNumeric:
            case VanillaLineSpecialType.S1_FloorTransferTrigger:
            case VanillaLineSpecialType.SR_FloorTransferTrigger:
            case VanillaLineSpecialType.S1_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.S1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.S1_StopMovingFloor:
            case VanillaLineSpecialType.S1_FastCrusherCeiling:
            case VanillaLineSpecialType.S1_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.S1_CeilingToHighestFloorToLowest:
            case VanillaLineSpecialType.S1_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.S1_StopCrusherCeiling:
            case VanillaLineSpecialType.S1_LightLevelMatchBrightness:
            case VanillaLineSpecialType.S1_LightOffMinBrightness:
            case VanillaLineSpecialType.S1_LightOnMaxBrightness:
            case VanillaLineSpecialType.S1_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.S1_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.S1_Teleport:
            case VanillaLineSpecialType.S1_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.SR_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloor512:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.SR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.SR_StopMovingFloor:
            case VanillaLineSpecialType.SR_FastCrusherCeiling:
            case VanillaLineSpecialType.SR_SlowCrusherCeiling:
            case VanillaLineSpecialType.SR_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.SR_CeilingToHighestFloorToLowest:
            case VanillaLineSpecialType.SR_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.SR_StopCrusherCeiling:
            case VanillaLineSpecialType.SR_Donut:
            case VanillaLineSpecialType.SR_LightLevelMatchBrightness:
            case VanillaLineSpecialType.SR_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.SR_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.SR_Teleport:
            case VanillaLineSpecialType.SR_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.S1_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.S1_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_TeleportNoFog:
            case VanillaLineSpecialType.SR_TeleportNoFog:
            case VanillaLineSpecialType.S1_LowerFloorToNearest:
            case VanillaLineSpecialType.SR_LowerFloorToNearest:
            case VanillaLineSpecialType.SR_RaiseStairs8:
            case VanillaLineSpecialType.SR_RaiseStairsFast:
            case VanillaLineSpecialType.SR_ToggleFloorToCeiling:
            case VanillaLineSpecialType.S1_ElevatorLowerToNearest:
            case VanillaLineSpecialType.SR_ElevatorLowerToNearest:
            case VanillaLineSpecialType.S1_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.SR_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.S1_ElevatorMoveToActivatingFloor:
            case VanillaLineSpecialType.SR_ElevatorMoveToActivatingFloor:
                activations = LineActivations.Player | LineActivations.UseLine;
                return activations;

            case VanillaLineSpecialType.W1_DoorOpenStay:
            case VanillaLineSpecialType.W1_CloseDoor:
            case VanillaLineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.W1_FastCrusherCeiling:
            case VanillaLineSpecialType.W1_RaiseStairs8:
            case VanillaLineSpecialType.W1_LightLevelMatchBrightness:
            case VanillaLineSpecialType.W1_LightOnMaxBrightness:
            case VanillaLineSpecialType.W1_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.W1_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.W1_SlowCrusherCeiling:
            case VanillaLineSpecialType.W1_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.W1_LightOffMinBrightness:
            case VanillaLineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
            case VanillaLineSpecialType.W1_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.W_EndLevel:
            case VanillaLineSpecialType.W1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.W1_StopMovingFloor:
            case VanillaLineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.W1_StopCrusherCeiling:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.WR_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.WR_SlowCrusherCeilingFastDamage:
            case VanillaLineSpecialType.WR_StopCrusherCeiling:
            case VanillaLineSpecialType.WR_CloseDoor:
            case VanillaLineSpecialType.WR_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.WR_FastCrusherCeilingSlowDamage:
            case VanillaLineSpecialType.WR_LightOffMinBrightness:
            case VanillaLineSpecialType.WR_LightLevelMatchBrightestAdjacent:
            case VanillaLineSpecialType.WR_LightOnMaxBrightness:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.WR_OpenDoorStay:
            case VanillaLineSpecialType.WR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.WR_StopMovingFloor:
            case VanillaLineSpecialType.WR_OpenDoorClose:
            case VanillaLineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.WR_RaiseByShortestLowerTexture:
            case VanillaLineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.W1_RaiseStairsFast:
            case VanillaLineSpecialType.W1_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.WR_OpenDoorFastClose:
            case VanillaLineSpecialType.WR_OpenDoorFastStayOpen:
            case VanillaLineSpecialType.WR_CloseDoorFast:
            case VanillaLineSpecialType.W1_OpenDoorFastClose:
            case VanillaLineSpecialType.W1_OpenDoorFastStay:
            case VanillaLineSpecialType.W1_CloseDoorFast:
            case VanillaLineSpecialType.WR_LowerLiftFastRaise:
            case VanillaLineSpecialType.W1_LowerLiftFastRaise:
            case VanillaLineSpecialType.W_EndLevelSecret:
            case VanillaLineSpecialType.WR_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.W1_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.W1_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.W1_FloorTransferNumeric:
            case VanillaLineSpecialType.WR_FloorTransferNumeric:
            case VanillaLineSpecialType.W1_FloorTransferTrigger:
            case VanillaLineSpecialType.WR_FloorTransferTrigger:
            case VanillaLineSpecialType.W1_RaiseFloor512:
            case VanillaLineSpecialType.WR_RaiseFloor512:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.W1_FloorRaiseByThirtyTwoChangeTextureType:
            case VanillaLineSpecialType.W1_LowerCeilingToFloor:
            case VanillaLineSpecialType.WR_LowerCeilingToFloor:
            case VanillaLineSpecialType.W1_Donut:
            case VanillaLineSpecialType.WR_Donut:
            case VanillaLineSpecialType.WR_FloorRaiseByTwentyFourChangeTextureType:
            case VanillaLineSpecialType.WR_FloorRaiseByThirtyTwoChangeTextureType:
            case VanillaLineSpecialType.WR_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.WR_CeilingToHighestFloorToLowest:
            case VanillaLineSpecialType.WR_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.WR_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.W1_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.W1_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.WR_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.W1_LowerFloorToNearest:
            case VanillaLineSpecialType.WR_LowerFloorToNearest:
            case VanillaLineSpecialType.WR_RaiseStairs8:
            case VanillaLineSpecialType.WR_RaiseStairsFast:
            case VanillaLineSpecialType.WR_ToggleFloorToCeiling:
            case VanillaLineSpecialType.W1_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.WR_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.W1_ElevatorLowerToNearest:
            case VanillaLineSpecialType.WR_ElevatorLowerToNearest:
            case VanillaLineSpecialType.W1_ElevatorMoveToActivatingFloor:
            case VanillaLineSpecialType.WR_ElevatorMoveToActivatingFloor:
                activations = LineActivations.Player | LineActivations.CrossLine;
                return activations;

            case VanillaLineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.GR_OpenDoorStayOpen:
            case VanillaLineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.G_EndLevel:
            case VanillaLineSpecialType.G_EndLevelSecret:
                activations = LineActivations.Hitscan | LineActivations.ImpactLine | LineActivations.CrossLine;
                return activations;

            case VanillaLineSpecialType.ScrollTextureLeft:
            case VanillaLineSpecialType.ScrollTextureRight:
            case VanillaLineSpecialType.TransferFloorLight:
            case VanillaLineSpecialType.TransferCeilingLight:
            case VanillaLineSpecialType.ScrollTextureOffsets:
            case VanillaLineSpecialType.ScrollAccelTaggedFloorFirstSide:
            case VanillaLineSpecialType.ScrollAccelObjectsTaggedFloorFirstSide:
            case VanillaLineSpecialType.ScrollAccelObjectsFloorFirstSide:
            case VanillaLineSpecialType.ScrollTaggedFloorFirstSide:
            case VanillaLineSpecialType.PushObjectsTaggedFloorFirstSide:
            case VanillaLineSpecialType.PushObjectsAndFloorTaggedFirstSide:
            case VanillaLineSpecialType.ScrollTaggedFloor:
            case VanillaLineSpecialType.CarryObjectsTaggedFloor:
            case VanillaLineSpecialType.ScrollTagedFloorAndCarryObjects:
            case VanillaLineSpecialType.ScrollAccelTaggedCeiling:
            case VanillaLineSpecialType.ScrollTaggedCeilingFirstSide:
            case VanillaLineSpecialType.ScrollTaggedCeiling:
            case VanillaLineSpecialType.ScrollAccellTaggedWallFirstSide:
            case VanillaLineSpecialType.ScrollTaggedWallFirstSide:
            case VanillaLineSpecialType.ScrollTaggedWallSameAsFloorCeiling:
            case VanillaLineSpecialType.TranslucentLine:
            case VanillaLineSpecialType.TransferSky:
            case VanillaLineSpecialType.TransferSkyFlipped:
            case VanillaLineSpecialType.SectorSetFriction:
            case VanillaLineSpecialType.SectorSetWind:
            case VanillaLineSpecialType.SectorSetCurrent:
            case VanillaLineSpecialType.SetPush:
            case VanillaLineSpecialType.TransferHeights:
            case VanillaLineSpecialType.StandardScrollMbf21:
            case VanillaLineSpecialType.AccelerativeScrollMbf21:
            case VanillaLineSpecialType.DisplacementScrollMbf21:
                activations = LineActivations.LevelStart;
                return activations;

            case VanillaLineSpecialType.W1_Teleport:
            case VanillaLineSpecialType.WR_Teleport:
            case VanillaLineSpecialType.WR_LowerLiftRaise:
            case VanillaLineSpecialType.W1_LowerLiftRaise:
            case VanillaLineSpecialType.W1_DoorOpenClose:
            case VanillaLineSpecialType.W1_TeleportNoFog:
            case VanillaLineSpecialType.WR_TeleportNoFog:
            case VanillaLineSpecialType.W1_TeleportLine:
            case VanillaLineSpecialType.WR_TeleportLine:
            case VanillaLineSpecialType.W1_TeleportLineReversed:
            case VanillaLineSpecialType.WR_TeleportLineReversed:
                activations = LineActivations.Player | LineActivations.Monster | LineActivations.CrossLine;
                return activations;

            case VanillaLineSpecialType.W1_MonsterTeleport:
            case VanillaLineSpecialType.WR_MonsterTeleport:
            case VanillaLineSpecialType.W1_MonsterTeleportLine:
            case VanillaLineSpecialType.WR_MonsterTeleportLine:
            case VanillaLineSpecialType.W1_MonsterTeleportLineReversed:
            case VanillaLineSpecialType.WR_MonsterTeleportLineReversed:
            case VanillaLineSpecialType.W1_MonsterTeleportNoFog:
            case VanillaLineSpecialType.WR_MonsterTeportNoFog:
                activations = LineActivations.Monster | LineActivations.CrossLine;
                return activations;

            default:
                break;
        }

        if (MonsterCanActivate(type))
            activations |= LineActivations.Monster;

        return activations;
    }

    private static bool GetRepeat(VanillaLineSpecialType type)
    {
        switch (type)
        {
            case VanillaLineSpecialType.DR_DoorOpenClose:
            case VanillaLineSpecialType.DR_OpenBlueKeyClose:
            case VanillaLineSpecialType.DR_OpenYellowKeyClose:
            case VanillaLineSpecialType.DR_OpenRedKeyClose:
            case VanillaLineSpecialType.SR_CloseDoor:
            case VanillaLineSpecialType.SR_LowerCeilingToFloor:
            case VanillaLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.SR_OpenDoorStay:
            case VanillaLineSpecialType.SR_LowerLiftRaise:
            case VanillaLineSpecialType.SR_OpenDoorClose:
            case VanillaLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigher:
            case VanillaLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.SR_OpenDoorFastClose:
            case VanillaLineSpecialType.SR_OpenDoorFastStay:
            case VanillaLineSpecialType.SR_CloseDoorFast:
            case VanillaLineSpecialType.DR_OpenDoorFastClose:
            case VanillaLineSpecialType.SR_LowerLiftFastRaise:
            case VanillaLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.SR_OpenRedKeyFastStay:
            case VanillaLineSpecialType.SR_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.SR_LightOnMaxBrightness:
            case VanillaLineSpecialType.SR_LightOffMinBrightness:
            case VanillaLineSpecialType.WR_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.WR_SlowCrusherCeilingFastDamage:
            case VanillaLineSpecialType.WR_StopCrusherCeiling:
            case VanillaLineSpecialType.WR_CloseDoor:
            case VanillaLineSpecialType.WR_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.WR_FastCrusherCeilingSlowDamage:
            case VanillaLineSpecialType.WR_LightOffMinBrightness:
            case VanillaLineSpecialType.WR_LightLevelMatchBrightestAdjacent:
            case VanillaLineSpecialType.WR_LightOnMaxBrightness:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.WR_OpenDoorStay:
            case VanillaLineSpecialType.WR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.WR_LowerLiftRaise:
            case VanillaLineSpecialType.WR_StopMovingFloor:
            case VanillaLineSpecialType.WR_OpenDoorClose:
            case VanillaLineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.WR_RaiseByShortestLowerTexture:
            case VanillaLineSpecialType.WR_Teleport:
            case VanillaLineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_OpenDoorFastClose:
            case VanillaLineSpecialType.WR_OpenDoorFastStayOpen:
            case VanillaLineSpecialType.WR_CloseDoorFast:
            case VanillaLineSpecialType.WR_LowerLiftFastRaise:
            case VanillaLineSpecialType.WR_MonsterTeleport:
            case VanillaLineSpecialType.WR_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.GR_OpenDoorStayOpen:
            case VanillaLineSpecialType.SR_FloorTransferNumeric:
            case VanillaLineSpecialType.WR_FloorTransferNumeric:
            case VanillaLineSpecialType.WR_FloorTransferTrigger:
            case VanillaLineSpecialType.SR_FloorTransferTrigger:
            case VanillaLineSpecialType.WR_LowerCeilingToFloor:
            case VanillaLineSpecialType.WR_Donut:
            case VanillaLineSpecialType.WR_RaiseFloor512:
            case VanillaLineSpecialType.WR_FloorRaiseByTwentyFourChangeTextureType:
            case VanillaLineSpecialType.WR_FloorRaiseByThirtyTwoChangeTextureType:
            case VanillaLineSpecialType.WR_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.WR_CeilingToHighestFloorToLowest:
            case VanillaLineSpecialType.WR_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.WR_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.SR_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloor512:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourChangeTextureType:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.SR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.SR_StopMovingFloor:
            case VanillaLineSpecialType.SR_FastCrusherCeiling:
            case VanillaLineSpecialType.SR_SlowCrusherCeiling:
            case VanillaLineSpecialType.SR_QuietCrusherCeilingFastDamage:
            case VanillaLineSpecialType.SR_CeilingToHighestFloorToLowest:
            case VanillaLineSpecialType.SR_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.SR_StopCrusherCeiling:
            case VanillaLineSpecialType.SR_Donut:
            case VanillaLineSpecialType.SR_LightLevelMatchBrightness:
            case VanillaLineSpecialType.SR_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.SR_LightMatchDimmestAdjacent:
            case VanillaLineSpecialType.SR_Teleport:
            case VanillaLineSpecialType.SR_CloseDoorOpenThirtySeconds:
            case VanillaLineSpecialType.WR_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.WR_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_CeilingLowerToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_CeilingLowerToHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_TeleportNoFog:
            case VanillaLineSpecialType.SR_TeleportNoFog:
            case VanillaLineSpecialType.WR_LowerFloorToNearest:
            case VanillaLineSpecialType.SR_LowerFloorToNearest:
            case VanillaLineSpecialType.WR_RaiseStairs8:
            case VanillaLineSpecialType.WR_RaiseStairsFast:
            case VanillaLineSpecialType.SR_RaiseStairs8:
            case VanillaLineSpecialType.SR_RaiseStairsFast:
            case VanillaLineSpecialType.WR_ToggleFloorToCeiling:
            case VanillaLineSpecialType.SR_ToggleFloorToCeiling:
            case VanillaLineSpecialType.WR_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.WR_ElevatorLowerToNearest:
            case VanillaLineSpecialType.SR_ElevatorLowerToNearest:
            case VanillaLineSpecialType.SR_ElevatorRaiseToNearest:
            case VanillaLineSpecialType.WR_ElevatorMoveToActivatingFloor:
            case VanillaLineSpecialType.SR_ElevatorMoveToActivatingFloor:
            case VanillaLineSpecialType.WR_TeleportLine:
            case VanillaLineSpecialType.WR_TeleportLineReversed:
            case VanillaLineSpecialType.WR_MonsterTeleportLine:
            case VanillaLineSpecialType.WR_MonsterTeportNoFog:
            case VanillaLineSpecialType.WR_MonsterTeleportLineReversed:
                return true;

            default:
                break;
        }

        return false;
    }

    private static bool MonsterCanActivate(VanillaLineSpecialType type)
    {
        switch (type)
        {
            case VanillaLineSpecialType.SR_Teleport:
            case VanillaLineSpecialType.SR_TeleportNoFog:
            case VanillaLineSpecialType.S1_Teleport:
                return true;
            default:
                break;
        }

        return false;
    }
}
