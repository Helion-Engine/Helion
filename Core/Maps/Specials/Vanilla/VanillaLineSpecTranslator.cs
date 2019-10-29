using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Lines;
using Helion.World.Special;

namespace Helion.Maps.Specials.Vanilla
{
    public static class VanillaLineSpecTranslator
    {
        public static ZDoomLineSpecialType Translate(LineFlags lineFlags, VanillaLineSpecialType type, byte tag,
            ref SpecialArgs argsToMutate)
        {
            lineFlags.ActivationType = GetSpecialActivationType(type);
            lineFlags.Repeat = GetRepeat(type);

            // TODO handle keys
            switch (type)
            {
            case VanillaLineSpecialType.W1_Teleport:
            case VanillaLineSpecialType.WR_Teleport:
            case VanillaLineSpecialType.W1_MonsterTeleport:
            case VanillaLineSpecialType.WR_MonsterTeleport:
                argsToMutate.Arg1 = tag;
                return ZDoomLineSpecialType.Teleport;

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
                return ZDoomLineSpecialType.LiftDownWaitUpStay;
            
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
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.DoorOpenStay;

            case VanillaLineSpecialType.D1_OpenBlueKeyStay:
            case VanillaLineSpecialType.SR_OpenBlueKeyFastStay:
            case VanillaLineSpecialType.D1_OpenRedKeyStay:
            case VanillaLineSpecialType.SR_OpenRedKeyFastStay:
            case VanillaLineSpecialType.S1_OpenRedKeyFastStay:
            case VanillaLineSpecialType.D1_OpenYellowKeyStay:
            case VanillaLineSpecialType.SR_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.S1_OpenYellowKeyFastStay:
            case VanillaLineSpecialType.DR_OpenBlueKeyClose:
            case VanillaLineSpecialType.DR_OpenRedKeyClose:
            case VanillaLineSpecialType.DR_OpenYellowKeyClose:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = GetDelay(type);
                argsToMutate.Arg3 = GetDoorKey(type);
                return ZDoomLineSpecialType.DoorLockedRaise;

            case VanillaLineSpecialType.W1_DoorOpenClose:
            case VanillaLineSpecialType.DR_DoorOpenClose:
            case VanillaLineSpecialType.WR_OpenDoorClose:
            case VanillaLineSpecialType.WR_OpenDoorFastClose:
            case VanillaLineSpecialType.SR_OpenDoorClose:
            case VanillaLineSpecialType.SR_OpenDoorFastClose:
            case VanillaLineSpecialType.S1_OpenDoorClose:
            case VanillaLineSpecialType.S1_OpenDoorFastClose:
            case VanillaLineSpecialType.DR_OpenDoorFastClose:
            case VanillaLineSpecialType.W1_OpenDoorFastClose:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = GetDelay(type);
                return ZDoomLineSpecialType.DoorOpenClose;

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
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.FloorLowerToLowest;

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
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                break;

            case VanillaLineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 10; // Damage
                argsToMutate.Arg3 = (byte)ZDoomCrushMode.Hexen; // ZDoom wiki has these translated to hexen type, not sure if this is right
                return ZDoomLineSpecialType.FloorRaiseAndCrushDoom;

            // TODO handle crusher ceilings...
            case VanillaLineSpecialType.W1_FastCrusherCeiling:
            case VanillaLineSpecialType.W1_SlowCrusherCeiling:
            case VanillaLineSpecialType.WR_SlowCrusherCeilingFastDamage:
            case VanillaLineSpecialType.WR_FastCrusherCeilingSlowDamage:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 0; // Distance above floor
                argsToMutate.Arg2 = GetSectorMoveSpeed(type);
                argsToMutate.Arg3 = 8; // Damage
                argsToMutate.Arg4 = (byte)ZDoomCrushMode.DoomWithSlowDown;
                return ZDoomLineSpecialType.CeilingCrushAndRaiseDist;

            case VanillaLineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 8; // Distance above floor
                argsToMutate.Arg2 = GetSectorMoveSpeed(type);
                argsToMutate.Arg3 = 8; // Damage
                argsToMutate.Arg4 = (byte)ZDoomCrushMode.DoomWithSlowDown;
                return ZDoomLineSpecialType.CeilingCrushAndRaiseDist;

            case VanillaLineSpecialType.W1_QuietCrusherCeilingFastDamage:
                return ZDoomLineSpecialType.CeilingCrushRaiseSilent;

            case VanillaLineSpecialType.WR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFour:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 24;
                return ZDoomLineSpecialType.FloorRaiseByValue;

            case VanillaLineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourMatchAdjacentChangeTexture:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 24;
                return ZDoomLineSpecialType.FloorRaiseByValueTxTy;

            case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
            case VanillaLineSpecialType.S1_RaiseFloorThirtyTwoMatchAdjacentChangeTexture:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 32;
                return ZDoomLineSpecialType.FloorRaiseByValueTxTy;

            case VanillaLineSpecialType.S1_RaiseFloor512:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 64;
                return ZDoomLineSpecialType.FloorRaiseByValueTimes8;

            case VanillaLineSpecialType.WR_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.W1_LowerCeilingToEightAboveFloor:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                return ZDoomLineSpecialType.CeilingLowerToFloor;

            case VanillaLineSpecialType.W1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.WR_StartMovingFloorPerpetual:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = GetDelay(type);
                return ZDoomLineSpecialType.PlatPerpetualRaiseLip;

            case VanillaLineSpecialType.S1_RaiseStairs8:
            case VanillaLineSpecialType.W1_RaiseStairs8:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 8;
                return ZDoomLineSpecialType.StairsBuildUpDoom;

            case VanillaLineSpecialType.W1_RaiseStairsFast:
            case VanillaLineSpecialType.S1_RaiseStairsFast:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                argsToMutate.Arg2 = 16;
                return ZDoomLineSpecialType.StairsBuildUpDoomCrush;

            // TODO
            case VanillaLineSpecialType.W1_LightOnMaxBrightness:
            case VanillaLineSpecialType.WR_LightOnMaxBrightness:
            case VanillaLineSpecialType.SR_LightOnMaxBrightness:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 255; // Brightness
                return ZDoomLineSpecialType.LightChangeToValue;

            case VanillaLineSpecialType.W1_LightOffMinBrightness:
            case VanillaLineSpecialType.WR_LightOffMinBrightness:
            case VanillaLineSpecialType.SR_LightOffMinBrightness:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 35; // Brightness
                return ZDoomLineSpecialType.LightChangeToValue;

            case VanillaLineSpecialType.W1_LightLevelMatchBrightness:
            case VanillaLineSpecialType.WR_LightLevelMatchBrightestAdjacent:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.LightMaxNeighbor;

            case VanillaLineSpecialType.W1_LightMatchDimmestAdjacent:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.LightMinNeighbor;

            case VanillaLineSpecialType.W1_StopMovingFloor:
            case VanillaLineSpecialType.WR_StopMovingFloor:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.PlatStop;

            case VanillaLineSpecialType.W1_StopCrusherCeiling:
            case VanillaLineSpecialType.WR_StopCrusherCeiling:
                argsToMutate.Arg0 = tag;
                return ZDoomLineSpecialType.CeilingCrushStop;

            case VanillaLineSpecialType.W1_CloseDoorThirtySeconds:
            case VanillaLineSpecialType.WR_CloseDoorThirtySeconds:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = GetSectorMoveSpeed(type);
                // TODO verify if this is actually the correct function
                return ZDoomLineSpecialType.DoorWaitClose;

            case VanillaLineSpecialType.W1_BlinkLightStartEveryOneSecond:
                argsToMutate.Arg0 = tag;
                argsToMutate.Arg1 = 5; // Tics to stay at upper light level
                argsToMutate.Arg2 = 35; // Tics to stay at lower light level
                return ZDoomLineSpecialType.LightStrobeDoom;

            case VanillaLineSpecialType.S1_Donut:
                argsToMutate.Arg0 = tag;

                // TODO verify these speeds
                argsToMutate.Arg1 = VanillaConstants.DonutSpeed; // Pillar speed
                argsToMutate.Arg2 = VanillaConstants.DonutSpeed; // Surrounding speed
                return ZDoomLineSpecialType.FloorDonut;

            case VanillaLineSpecialType.ScrollTextureLeft:
                argsToMutate.Arg0 = 64; // Speed
                return ZDoomLineSpecialType.ScrollTextureLeft;

            case VanillaLineSpecialType.S_EndLevel:
            case VanillaLineSpecialType.W_EndLevel:
                return ZDoomLineSpecialType.ExitNormal;

            case VanillaLineSpecialType.S_EndLevelSecret:
            case VanillaLineSpecialType.W_EndLevelSecret:
                return ZDoomLineSpecialType.ExitSecret;
            }

            return ZDoomLineSpecialType.None;
        }

        public static LineActivationType GetLineTagActivation(VanillaLineSpecialType type)
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
                case VanillaLineSpecialType.S1_RaiseFloorThirtyTwoMatchAdjacentChangeTexture:
                case VanillaLineSpecialType.S1_RaiseFloorTwentyFourMatchAdjacentChangeTexture:
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
                case VanillaLineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
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
                    return LineActivationType.Tag;
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
            case VanillaLineSpecialType.GR_OpenDoorStayOpen:
            case VanillaLineSpecialType.SR_OpenDoorClose:
            case VanillaLineSpecialType.SR_OpenDoorFastClose:
            case VanillaLineSpecialType.SR_OpenDoorStay:
            case VanillaLineSpecialType.SR_OpenRedKeyFastStay:
            case VanillaLineSpecialType.S1_OpenDoorClose:
            case VanillaLineSpecialType.S1_OpenDoorFastClose:
            case VanillaLineSpecialType.S1_OpenDoorFastStay:
            case VanillaLineSpecialType.W1_DoorOpenClose:
            case VanillaLineSpecialType.W1_DoorOpenStay:
            case VanillaLineSpecialType.W1_OpenDoorFastClose:
            case VanillaLineSpecialType.WR_OpenDoorClose:
            case VanillaLineSpecialType.WR_OpenDoorFastClose:
                return true;
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
            }

            return false;
        }

        private static byte GetSectorMoveSpeed(VanillaLineSpecialType type)
        {
            switch (type)
            {
            case VanillaLineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
            case VanillaLineSpecialType.W1_FastCrusherCeiling:
            case VanillaLineSpecialType.WR_FastCrusherCeilingSlowDamage:
            case VanillaLineSpecialType.W1_QuietCrusherCeilingFastDamage:
                return VanillaConstants.SectorFastSpeed;

            case VanillaLineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.S1_RaiseFloorThirtyTwoMatchAdjacentChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourMatchAdjacentChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
            case VanillaLineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.W1_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.W1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
            case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigher:
            case VanillaLineSpecialType.WR_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.S1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.W1_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.WR_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.S1_RaiseFloorToNextHigherFloor:
            case VanillaLineSpecialType.S1_RaiseFloor512:
            case VanillaLineSpecialType.W1_SlowCrusherCeiling:
            case VanillaLineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
            case VanillaLineSpecialType.W1_LowerCeilingToFloor:
            case VanillaLineSpecialType.SR_LowerCeilingToFloor:
            case VanillaLineSpecialType.W1_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
            case VanillaLineSpecialType.WR_SlowCrusherCeilingFastDamage:
            case VanillaLineSpecialType.WR_LowerCeilingToEightAboveFloor:
                return VanillaConstants.SectorFastSpeed;

            case VanillaLineSpecialType.S1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
            case VanillaLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                return VanillaConstants.FloorSlowSpeed;

            // TODO verify these!
            case VanillaLineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.WR_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                return VanillaConstants.LiftSlowSpeed;

            case VanillaLineSpecialType.W1_LowerLiftRaise:
            case VanillaLineSpecialType.S1_LowerLiftRaise:
            case VanillaLineSpecialType.SR_LowerLiftRaise:
            case VanillaLineSpecialType.WR_LowerLiftRaise:
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
            case VanillaLineSpecialType.W1_CloseDoorThirtySeconds:
            case VanillaLineSpecialType.S1_OpenDoorClose:
            case VanillaLineSpecialType.D1_OpenDoorStay:
            case VanillaLineSpecialType.SR_CloseDoor:
            case VanillaLineSpecialType.GR_OpenDoorStayOpen:
            case VanillaLineSpecialType.S1_CloseDoor:
            case VanillaLineSpecialType.SR_OpenDoorStay:
            case VanillaLineSpecialType.SR_OpenDoorClose:
            case VanillaLineSpecialType.WR_CloseDoor:
            case VanillaLineSpecialType.WR_CloseDoorThirtySeconds:
            case VanillaLineSpecialType.WR_OpenDoorStay:
            case VanillaLineSpecialType.WR_OpenDoorClose:
            case VanillaLineSpecialType.S1_OpenDoorStay:
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
                return VanillaConstants.StairSlowSpeed;

            case VanillaLineSpecialType.W1_RaiseStairsFast:
            case VanillaLineSpecialType.S1_RaiseStairsFast:
                return VanillaConstants.StairFastSpeed;
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
                return VanillaConstants.LiftDelay;
            }

            return 0;
        }

        private static ActivationType GetSpecialActivationType(VanillaLineSpecialType type)
        {
            switch (type)
            {
            case VanillaLineSpecialType.DR_DoorOpenClose:
            case VanillaLineSpecialType.S1_RaiseStairs8:
            case VanillaLineSpecialType.S1_Donut:
            case VanillaLineSpecialType.S_EndLevel:
            case VanillaLineSpecialType.S1_RaiseFloorThirtyTwoMatchAdjacentChangeTexture:
            case VanillaLineSpecialType.S1_RaiseFloorTwentyFourMatchAdjacentChangeTexture:
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
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
            case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
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
                return ActivationType.PlayerUse;

            case VanillaLineSpecialType.W1_DoorOpenStay:
            case VanillaLineSpecialType.W1_CloseDoor:
            case VanillaLineSpecialType.W1_DoorOpenClose:
            case VanillaLineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.W1_FastCrusherCeiling:
            case VanillaLineSpecialType.W1_RaiseStairs8:
            case VanillaLineSpecialType.W1_LowerLiftRaise:
            case VanillaLineSpecialType.W1_LightLevelMatchBrightness:
            case VanillaLineSpecialType.W1_LightOnMaxBrightness:
            case VanillaLineSpecialType.W1_CloseDoorThirtySeconds:
            case VanillaLineSpecialType.W1_BlinkLightStartEveryOneSecond:
            case VanillaLineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
            case VanillaLineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.W1_SlowCrusherCeiling:
            case VanillaLineSpecialType.W1_RaiseFloorByShortestLowerTexture:
            case VanillaLineSpecialType.W1_LightOffMinBrightness:
            case VanillaLineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
            case VanillaLineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
            case VanillaLineSpecialType.W1_Teleport:
            case VanillaLineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
            case VanillaLineSpecialType.W1_LowerCeilingToFloor:
            case VanillaLineSpecialType.W1_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.W_EndLevel:
            case VanillaLineSpecialType.W1_StartMovingFloorPerpetual:
            case VanillaLineSpecialType.W1_StopMovingFloor:
            case VanillaLineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.W1_StopCrusherCeiling:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFour:
            case VanillaLineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
            case VanillaLineSpecialType.WR_LowerCeilingToEightAboveFloor:
            case VanillaLineSpecialType.WR_SlowCrusherCeilingFastDamage:
            case VanillaLineSpecialType.WR_StopCrusherCeiling:
            case VanillaLineSpecialType.WR_CloseDoor:
            case VanillaLineSpecialType.WR_CloseDoorThirtySeconds:
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
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFourChangeTexture:
            case VanillaLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
            case VanillaLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
            case VanillaLineSpecialType.WR_RaiseByShortestLowerTexture:
            case VanillaLineSpecialType.WR_Teleport:
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
                return ActivationType.PlayerLineCross;

            case VanillaLineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
            case VanillaLineSpecialType.GR_OpenDoorStayOpen:
            case VanillaLineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
                return ActivationType.ProjectileHitsWall;

            case VanillaLineSpecialType.ScrollTextureLeft:
                return ActivationType.LevelStart;

            case VanillaLineSpecialType.W1_MonsterTeleport:
            case VanillaLineSpecialType.WR_MonsterTeleport:
                return ActivationType.MonsterLineCross;
            }

            return ActivationType.None;
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
            case VanillaLineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
            case VanillaLineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
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
            case VanillaLineSpecialType.WR_CloseDoorThirtySeconds:
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
            case VanillaLineSpecialType.WR_RaiseFloorTwentyFourChangeTexture:
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
                return true;
            }

            return false;
        }
    }
}