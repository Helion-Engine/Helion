using Helion.Maps.Geometry.Lines;

namespace Helion.Maps.Special
{
    class VanillaLineSpecTranslator
    {
        private const byte SectorSlowSpeed = 8;
        private const byte SectorFastSpeed = 16;

        private const byte LiftSlowSpeed = 32;
        private const byte LiftFastSpeed = 64;

        private const byte DoorSlowSpeed = 16;
        private const int DoorFastSpeed = 32;

        private const byte StairSlowSpeed = 2;
        private const byte StairFastSpeed = 4;

        private const byte DonutSpeed = 4;

        private const byte LiftDelay = 105;
        private const byte DoorDelay = 150;

        public static ZLineSpecialType Translate(Line line, VLineSpecialType type, byte tag)
        {
            line.Flags.ActivationType = GetSpecialActivationType(type);
            line.Flags.Repeat = GetRepeat(type);

            // TODO handle keys
            switch (type)
            {
                case VLineSpecialType.W1_Teleport:
                case VLineSpecialType.WR_Teleport:
                case VLineSpecialType.W1_MonsterTeleport:
                case VLineSpecialType.WR_MonsterTeleport:
                    line.Args[0] = tag;
                    return ZLineSpecialType.Teleport;

                case VLineSpecialType.SR_LowerLiftRaise:
                case VLineSpecialType.SR_LowerLiftFastRaise:
                case VLineSpecialType.WR_LowerLiftRaise:
                case VLineSpecialType.W1_LowerLiftRaise:
                case VLineSpecialType.S1_LowerLiftRaise:
                case VLineSpecialType.WR_LowerLiftFastRaise:
                case VLineSpecialType.W1_LowerLiftFastRaise:
                case VLineSpecialType.S1_LowerLiftFastRaise:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = GetDelay(type);
                    return ZLineSpecialType.LiftDownWaitUpStay;
                
                case VLineSpecialType.D1_OpenDoorFastStay:
                case VLineSpecialType.D1_OpenDoorStay:
                case VLineSpecialType.GR_OpenDoorStayOpen:
                case VLineSpecialType.SR_OpenDoorFastStay:
                case VLineSpecialType.SR_OpenDoorStay:
                case VLineSpecialType.W1_OpenDoorFastStay:
                case VLineSpecialType.WR_OpenDoorStay:
                case VLineSpecialType.S1_OpenDoorFastStay:
                case VLineSpecialType.S1_OpenDoorStay:
                case VLineSpecialType.W1_DoorOpenStay:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    return ZLineSpecialType.DoorOpenStay;

                case VLineSpecialType.D1_OpenBlueKeyStay:
                case VLineSpecialType.SR_OpenBlueKeyFastStay:
                case VLineSpecialType.D1_OpenRedKeyStay:
                case VLineSpecialType.SR_OpenRedKeyFastStay:
                case VLineSpecialType.S1_OpenRedKeyFastStay:
                case VLineSpecialType.D1_OpenYellowKeyStay:
                case VLineSpecialType.SR_OpenYellowKeyFastStay:
                case VLineSpecialType.S1_OpenYellowKeyFastStay:
                case VLineSpecialType.DR_OpenBlueKeyClose:
                case VLineSpecialType.DR_OpenRedKeyClose:
                case VLineSpecialType.DR_OpenYellowKeyClose:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = GetDelay(type);
                    line.Args[3] = GetDoorKey(type);
                    return ZLineSpecialType.DoorLockedRaise;

                case VLineSpecialType.W1_DoorOpenClose:
                case VLineSpecialType.DR_DoorOpenClose:
                case VLineSpecialType.WR_OpenDoorClose:
                case VLineSpecialType.WR_OpenDoorFastClose:
                case VLineSpecialType.SR_OpenDoorClose:
                case VLineSpecialType.SR_OpenDoorFastClose:
                case VLineSpecialType.S1_OpenDoorClose:
                case VLineSpecialType.S1_OpenDoorFastClose:
                case VLineSpecialType.DR_OpenDoorFastClose:
                case VLineSpecialType.W1_OpenDoorFastClose:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = GetDelay(type);
                    return ZLineSpecialType.DoorOpenClose;

                case VLineSpecialType.SR_CloseDoor:
                case VLineSpecialType.S1_CloseDoor:
                case VLineSpecialType.WR_CloseDoor:
                case VLineSpecialType.WR_CloseDoorFast:
                case VLineSpecialType.W1_CloseDoorFast:
                case VLineSpecialType.S1_CloseDoorFast:
                case VLineSpecialType.SR_CloseDoorFast:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    return ZLineSpecialType.DoorClose;

                case VLineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.S1_LowerFLoorToHighestAdjacentFloor:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    return ZLineSpecialType.FloorLowerToHighest;

                case VLineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
                case VLineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case VLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    return ZLineSpecialType.FloorLowerToLowest;

                case VLineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.S1_RaiseFLoorToLowestAdjacentCeiling:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    return ZLineSpecialType.FloorRaiseToLowestCeiling;

                case VLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = 136;
                    return ZLineSpecialType.FloorLowerToHighest;

                case VLineSpecialType.WR_RaiseFloorToNextHigherFloor:
                case VLineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.S1_RaiseFloorToNextHigherFloor:
                case VLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                case VLineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
                case VLineSpecialType.S1_RaiseFloorToMatchNextHigher:
                case VLineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
                case VLineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
                case VLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
                case VLineSpecialType.SR_RaiseFloorToNextHigher:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    return ZLineSpecialType.FloorRaiseToNearset;

                case VLineSpecialType.W1_RaiseFloorByShortestLowerTexture:
                case VLineSpecialType.WR_RaiseByShortestLowerTexture:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    break;             
                
                // TODO handle crusher floor...
                case VLineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = 10; // Damage
                    line.Args[3] = (byte)ZCrushMode.Hexen; // ZDoom wiki has these translated to hexen type, not sure if this is right
                    return ZLineSpecialType.FloorRaiseAndCrushDoom;

                // TODO handle crusher ceilings...
                case VLineSpecialType.W1_FastCrusherCeiling:
                case VLineSpecialType.W1_SlowCrusherCeiling:
                case VLineSpecialType.WR_SlowCrusherCeilingFastDamage:
                case VLineSpecialType.WR_FastCrusherCeilingSlowDamage:
                    line.Args[0] = tag;
                    line.Args[1] = 0; // Distance above floor
                    line.Args[2] = GetSectorMoveSpeed(type);
                    line.Args[3] = 8; // Damage
                    line.Args[4] = (byte)ZCrushMode.DoomWithSlowDown;
                    return ZLineSpecialType.CeilingCrushAndRaiseDist;

                case VLineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
                    line.Args[0] = tag;
                    line.Args[1] = 8; // Distance above floor
                    line.Args[2] = GetSectorMoveSpeed(type);
                    line.Args[3] = 8; // Damage
                    line.Args[4] = (byte)ZCrushMode.DoomWithSlowDown;
                    return ZLineSpecialType.CeilingCrushAndRaiseDist;

                case VLineSpecialType.W1_QuietCrusherCeilingFastDamage:
                    return ZLineSpecialType.CeilingCrushRaiseSilent;

                case VLineSpecialType.WR_RaiseFloorTwentyFour:
                case VLineSpecialType.W1_RaiseFloorTwentyFour:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = 24;
                    return ZLineSpecialType.FloorRaiseByValue;

                case VLineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
                case VLineSpecialType.S1_RaiseFloorTwentyFourMatchAdjacentChangeTexture:
                case VLineSpecialType.WR_RaiseFloorTwentyFourChangeTexture:
                case VLineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = 24;
                    return ZLineSpecialType.FloorRaiseByValueTxTy;

                case VLineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
                case VLineSpecialType.S1_RaiseFloorThirtyTwoMatchAdjacentChangeTexture:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = 32;
                    return ZLineSpecialType.FloorRaiseByValue;

                case VLineSpecialType.S1_RaiseFloor512:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = 64;
                    return ZLineSpecialType.FloorRaiseByValueTimes8;

                case VLineSpecialType.W1_StartMovingFloorPerpetual:
                case VLineSpecialType.WR_StartMovingFloorPerpetual:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = GetDelay(type);
                    return ZLineSpecialType.PlatPerpetualRaiseLip;

                case VLineSpecialType.S1_RaiseStairs8:
                case VLineSpecialType.W1_RaiseStairs8:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = 8;
                    return ZLineSpecialType.StairsBuildUpDoom;

                case VLineSpecialType.W1_RaiseStairsFast:
                case VLineSpecialType.S1_RaiseStairsFast:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    line.Args[2] = 16;
                    return ZLineSpecialType.StairsBuildUpDoomCrush;

                // TODO
                case VLineSpecialType.W1_LightOnMaxBrightness:
                case VLineSpecialType.WR_LightOnMaxBrigthness:
                case VLineSpecialType.SR_LightOnMaxBrightness:
                    line.Args[0] = tag;
                    line.Args[1] = 255; // Brightness
                    return ZLineSpecialType.LightChangeToValue;

                case VLineSpecialType.W1_LightOffMinBrightness:
                case VLineSpecialType.WR_LightOffMinBrightness:
                case VLineSpecialType.SR_LightOffMinBrightness:
                    line.Args[0] = tag;
                    line.Args[1] = 35; // Brightness
                    return ZLineSpecialType.LightChangeToValue;

                case VLineSpecialType.W1_LightLevelMatchBrightness:
                case VLineSpecialType.WR_LightLevelMatchBrightestAdjacent:
                    line.Args[0] = tag;
                    return ZLineSpecialType.LightMaxNeighor;

                case VLineSpecialType.W1_LightMatchDimmestAdjacent:
                    line.Args[0] = tag;
                    return ZLineSpecialType.LightMinNeighbor;

                case VLineSpecialType.W1_StopMovingFloor:
                case VLineSpecialType.WR_StopMovingFloor:
                    line.Args[0] = tag;
                    return ZLineSpecialType.PlatStop;

                case VLineSpecialType.W1_StopCrusherCeiling:
                case VLineSpecialType.WR_StopCrusherCeiling:
                    line.Args[0] = tag;
                    return ZLineSpecialType.CeilingCrushStop;

                case VLineSpecialType.W1_CloseDoorThirtySeconds:
                case VLineSpecialType.WR_CloseDoorThirtySeconds:
                    line.Args[0] = tag;
                    line.Args[1] = GetSectorMoveSpeed(type);
                    // TODO verify if this is actually the correct function
                    return ZLineSpecialType.DoorWaitClose;

                case VLineSpecialType.W1_BlinkLightStartEveryOneSecond:
                    line.Args[0] = tag;
                    line.Args[1] = 5; // Tics to stay at upper light level
                    line.Args[5] = 35; // Tics ot stay at lower light level
                    return ZLineSpecialType.LightStrobeDoom;

                case VLineSpecialType.S1_Donut:
                    line.Args[0] = tag;

                    // TODO verify these speeds
                    line.Args[1] = DonutSpeed; // Pillar speed
                    line.Args[2] = DonutSpeed; // Surrounding speed
                    return ZLineSpecialType.FloorDonut;

                case VLineSpecialType.ScrollTextureLeft:
                    line.Args[0] = 64; // Speed
                    return ZLineSpecialType.ScrollTextureLeft;

                case VLineSpecialType.S_EndLevel:
                case VLineSpecialType.W_EndLevel:
                    return ZLineSpecialType.ExitNormal;

                case VLineSpecialType.S_EndLevelSecret:
                case VLineSpecialType.W_EndLevelSecret:
                    return ZLineSpecialType.ExitSecret;
            }

            return ZLineSpecialType.None;
        }

        private static byte GetDoorKey(VLineSpecialType type)
        {
            switch (type)
            {
                case VLineSpecialType.DR_OpenRedKeyClose:
                case VLineSpecialType.D1_OpenRedKeyStay:              
                case VLineSpecialType.SR_OpenRedKeyFastStay:
                case VLineSpecialType.S1_OpenRedKeyFastStay:
                    return (byte)ZDoomKeyType.RedAny;

                case VLineSpecialType.DR_OpenBlueKeyClose:
                case VLineSpecialType.D1_OpenBlueKeyStay:
                case VLineSpecialType.SR_OpenBlueKeyFastStay:
                case VLineSpecialType.S1_OpenBlueKeyFastStay:
                    return (byte)ZDoomKeyType.BlueAny;

                case VLineSpecialType.DR_OpenYellowKeyClose:
                case VLineSpecialType.D1_OpenYellowKeyStay:               
                case VLineSpecialType.SR_OpenYellowKeyFastStay:
                case VLineSpecialType.S1_OpenYellowKeyFastStay:
                    return (byte)ZDoomKeyType.YellowAny;
            }

            return 0;
        }

        private static bool IsDoorWithDelay(VLineSpecialType type)
        {
            switch (type)
            {
                case VLineSpecialType.DR_DoorOpenClose:
                case VLineSpecialType.DR_OpenBlueKeyClose:
                case VLineSpecialType.DR_OpenDoorFastClose:
                case VLineSpecialType.DR_OpenRedKeyClose:
                case VLineSpecialType.DR_OpenYellowKeyClose:
                case VLineSpecialType.GR_OpenDoorStayOpen:
                case VLineSpecialType.SR_OpenDoorClose:
                case VLineSpecialType.SR_OpenDoorFastClose:
                case VLineSpecialType.SR_OpenDoorStay:
                case VLineSpecialType.SR_OpenRedKeyFastStay:
                case VLineSpecialType.S1_OpenDoorClose:
                case VLineSpecialType.S1_OpenDoorFastClose:
                case VLineSpecialType.S1_OpenDoorFastStay:
                case VLineSpecialType.W1_DoorOpenClose:
                case VLineSpecialType.W1_DoorOpenStay:
                case VLineSpecialType.W1_OpenDoorFastClose:
                case VLineSpecialType.WR_OpenDoorClose:
                case VLineSpecialType.WR_OpenDoorFastClose:
                    return true;
            }

            return false;
        }

        private static bool IsLift(VLineSpecialType type)
        {
            switch (type)
            {
                case VLineSpecialType.SR_LowerLiftRaise:
                case VLineSpecialType.SR_LowerLiftFastRaise:
                case VLineSpecialType.WR_LowerLiftRaise:
                case VLineSpecialType.W1_LowerLiftRaise:
                case VLineSpecialType.S1_LowerLiftRaise:
                case VLineSpecialType.WR_LowerLiftFastRaise:
                case VLineSpecialType.W1_LowerLiftFastRaise:
                case VLineSpecialType.S1_LowerLiftFastRaise:
                    return true;
            }

            return false;
        }

        private static byte GetSectorMoveSpeed(VLineSpecialType type)
        {
            switch (type)
            {
                case VLineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.W1_FastCrusherCeiling:
                case VLineSpecialType.WR_FastCrusherCeilingSlowDamage:
                case VLineSpecialType.W1_QuietCrusherCeilingFastDamage:
                    return SectorFastSpeed;

                case VLineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.S1_RaiseFloorThirtyTwoMatchAdjacentChangeTexture:
                case VLineSpecialType.S1_RaiseFloorTwentyFourMatchAdjacentChangeTexture:
                case VLineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
                case VLineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.S1_RaiseFloorToMatchNextHigher:
                case VLineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
                case VLineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.W1_RaiseFloorByShortestLowerTexture:
                case VLineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                case VLineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
                case VLineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
                case VLineSpecialType.W1_StartMovingFloorPerpetual:
                case VLineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.W1_RaiseFloorTwentyFour:
                case VLineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
                case VLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                case VLineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
                case VLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
                case VLineSpecialType.SR_RaiseFloorToNextHigher:
                case VLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case VLineSpecialType.WR_StartMovingFloorPerpetual:
                case VLineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.WR_RaiseFloorTwentyFour:
                case VLineSpecialType.WR_RaiseFloorTwentyFourChangeTexture:
                case VLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                case VLineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.S1_RaiseFLoorToLowestAdjacentCeiling:
                case VLineSpecialType.S1_LowerFLoorToHighestAdjacentFloor:
                case VLineSpecialType.W1_RaiseFloorToNextHigherFloor:
                case VLineSpecialType.WR_RaiseFloorToNextHigherFloor:
                case VLineSpecialType.S1_RaiseFloorToNextHigherFloor:
                case VLineSpecialType.S1_RaiseFloor512:
                case VLineSpecialType.W1_SlowCrusherCeiling:
                case VLineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
                case VLineSpecialType.W1_LowerCeilingToFloor:
                case VLineSpecialType.SR_LowerCeilingToFloor:
                case VLineSpecialType.W1_LowerCeilingToEightAboveFloor:
                case VLineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
                case VLineSpecialType.WR_SlowCrusherCeilingFastDamage:
                case VLineSpecialType.WR_LowerCeilingToEightAboveFloor:
                    return SectorSlowSpeed;

                case VLineSpecialType.W1_LowerLiftRaise:
                case VLineSpecialType.S1_LowerLiftRaise:
                case VLineSpecialType.SR_LowerLiftRaise:
                case VLineSpecialType.WR_LowerLiftRaise:
                    return LiftSlowSpeed;

                case VLineSpecialType.WR_LowerLiftFastRaise:
                case VLineSpecialType.W1_LowerLiftFastRaise:
                case VLineSpecialType.S1_LowerLiftFastRaise:
                case VLineSpecialType.SR_LowerLiftFastRaise:
                    return LiftFastSpeed;

                case VLineSpecialType.DR_OpenRedKeyClose:
                case VLineSpecialType.DR_OpenBlueKeyClose:
                case VLineSpecialType.DR_OpenYellowKeyClose:
                case VLineSpecialType.D1_OpenRedKeyStay:
                case VLineSpecialType.D1_OpenBlueKeyStay:
                case VLineSpecialType.D1_OpenYellowKeyStay:
                case VLineSpecialType.DR_DoorOpenClose:
                case VLineSpecialType.W1_DoorOpenStay:
                case VLineSpecialType.W1_CloseDoor:
                case VLineSpecialType.W1_DoorOpenClose:
                case VLineSpecialType.W1_CloseDoorThirtySeconds:
                case VLineSpecialType.S1_OpenDoorClose:
                case VLineSpecialType.D1_OpenDoorStay:
                case VLineSpecialType.SR_CloseDoor:
                case VLineSpecialType.GR_OpenDoorStayOpen:
                case VLineSpecialType.S1_CloseDoor:
                case VLineSpecialType.SR_OpenDoorStay:
                case VLineSpecialType.SR_OpenDoorClose:
                case VLineSpecialType.WR_CloseDoor:
                case VLineSpecialType.WR_CloseDoorThirtySeconds:
                case VLineSpecialType.WR_OpenDoorStay:
                case VLineSpecialType.WR_OpenDoorClose:
                case VLineSpecialType.S1_OpenDoorStay:
                    return DoorSlowSpeed;

                case VLineSpecialType.SR_OpenYellowKeyFastStay:
                case VLineSpecialType.S1_OpenYellowKeyFastStay:
                case VLineSpecialType.WR_OpenDoorFastClose:
                case VLineSpecialType.WR_OpenDoorFastStayOpen:
                case VLineSpecialType.W1_OpenDoorFastClose:
                case VLineSpecialType.W1_OpenDoorFastStay:
                case VLineSpecialType.S1_OpenDoorFastClose:
                case VLineSpecialType.S1_OpenDoorFastStay:
                case VLineSpecialType.SR_OpenDoorFastClose:
                case VLineSpecialType.SR_OpenDoorFastStay:
                case VLineSpecialType.DR_OpenDoorFastClose:
                case VLineSpecialType.D1_OpenDoorFastStay:
                case VLineSpecialType.S1_OpenBlueKeyFastStay:
                case VLineSpecialType.SR_OpenRedKeyFastStay:
                case VLineSpecialType.S1_OpenRedKeyFastStay:
                case VLineSpecialType.SR_OpenBlueKeyFastStay:
                case VLineSpecialType.WR_CloseDoorFast:
                case VLineSpecialType.W1_CloseDoorFast:
                case VLineSpecialType.S1_CloseDoorFast:
                case VLineSpecialType.SR_CloseDoorFast:
                    return DoorFastSpeed;

                case VLineSpecialType.S1_RaiseStairs8:
                case VLineSpecialType.W1_RaiseStairs8:
                    return StairSlowSpeed;

                case VLineSpecialType.W1_RaiseStairsFast:
                case VLineSpecialType.S1_RaiseStairsFast:
                    return StairFastSpeed;
            }

            return 0;
        }

        private static byte GetDelay(VLineSpecialType type)
        {
            if (IsDoorWithDelay(type))
                return DoorDelay;
            else if (IsLift(type))
                return LiftDelay;

            switch (type)
            {
                case VLineSpecialType.W1_StartMovingFloorPerpetual:
                case VLineSpecialType.WR_StartMovingFloorPerpetual:
                    return LiftDelay;
            }

            return 0;
        }

        private static ActivationType GetSpecialActivationType(VLineSpecialType type)
        {
            switch (type)
            {
                case VLineSpecialType.DR_DoorOpenClose:
                case VLineSpecialType.S1_RaiseStairs8:
                case VLineSpecialType.S1_Donut:
                case VLineSpecialType.S_EndLevel:
                case VLineSpecialType.S1_RaiseFloorThirtyTwoMatchAdjacentChangeTexture:
                case VLineSpecialType.S1_RaiseFloorTwentyFourMatchAdjacentChangeTexture:
                case VLineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
                case VLineSpecialType.S1_RaiseFloorToMatchNextHigher:
                case VLineSpecialType.S1_LowerLiftRaise:
                case VLineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.DR_OpenBlueKeyClose:
                case VLineSpecialType.DR_OpenYellowKeyClose:
                case VLineSpecialType.DR_OpenRedKeyClose:
                case VLineSpecialType.D1_OpenDoorStay:
                case VLineSpecialType.D1_OpenBlueKeyStay:
                case VLineSpecialType.D1_OpenRedKeyStay:
                case VLineSpecialType.D1_OpenYellowKeyStay:
                case VLineSpecialType.S1_OpenDoorClose:
                case VLineSpecialType.SR_CloseDoor:
                case VLineSpecialType.SR_LowerCeilingToFloor:
                case VLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
                case VLineSpecialType.S1_CloseDoor:
                case VLineSpecialType.S_EndLevelSecret:
                case VLineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.SR_OpenDoorStay:
                case VLineSpecialType.SR_LowerLiftRaise:
                case VLineSpecialType.SR_OpenDoorClose:
                case VLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                case VLineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
                case VLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
                case VLineSpecialType.SR_RaiseFloorToNextHigher:
                case VLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.SR_OpenBlueKeyFastStay:
                case VLineSpecialType.S1_RaiseFLoorToLowestAdjacentCeiling:
                case VLineSpecialType.S1_LowerFLoorToHighestAdjacentFloor:
                case VLineSpecialType.S1_OpenDoorStay:
                case VLineSpecialType.S1_OpenDoorFastClose:
                case VLineSpecialType.S1_OpenDoorFastStay:
                case VLineSpecialType.S1_CloseDoorFast:
                case VLineSpecialType.SR_OpenDoorFastClose:
                case VLineSpecialType.SR_OpenDoorFastStay:
                case VLineSpecialType.SR_CloseDoorFast:
                case VLineSpecialType.DR_OpenDoorFastClose:
                case VLineSpecialType.D1_OpenDoorFastStay:
                case VLineSpecialType.S1_RaiseFloorToNextHigherFloor:
                case VLineSpecialType.S1_LowerLiftFastRaise:
                case VLineSpecialType.SR_LowerLiftFastRaise:
                case VLineSpecialType.S1_RaiseStairsFast:
                case VLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.S1_OpenBlueKeyFastStay:
                case VLineSpecialType.SR_OpenRedKeyFastStay:
                case VLineSpecialType.S1_OpenRedKeyFastStay:
                case VLineSpecialType.SR_OpenYellowKeyFastStay:
                case VLineSpecialType.S1_OpenYellowKeyFastStay:
                case VLineSpecialType.SR_LightOnMaxBrightness:
                case VLineSpecialType.SR_LightOffMinBrightness:
                case VLineSpecialType.S1_RaiseFloor512:
                    return ActivationType.PlayerUse;

                case VLineSpecialType.W1_DoorOpenStay:
                case VLineSpecialType.W1_CloseDoor:
                case VLineSpecialType.W1_DoorOpenClose:
                case VLineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.W1_FastCrusherCeiling:
                case VLineSpecialType.W1_RaiseStairs8:
                case VLineSpecialType.W1_LowerLiftRaise:
                case VLineSpecialType.W1_LightLevelMatchBrightness:
                case VLineSpecialType.W1_LightOnMaxBrightness:
                case VLineSpecialType.W1_CloseDoorThirtySeconds:
                case VLineSpecialType.W1_BlinkLightStartEveryOneSecond:
                case VLineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
                case VLineSpecialType.W1_SlowCrusherCeiling:
                case VLineSpecialType.W1_RaiseFloorByShortestLowerTexture:
                case VLineSpecialType.W1_LightOffMinBrightness:
                case VLineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                case VLineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
                case VLineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.W1_Teleport:
                case VLineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
                case VLineSpecialType.W1_LowerCeilingToFloor:
                case VLineSpecialType.W1_LowerCeilingToEightAboveFloor:
                case VLineSpecialType.W_EndLevel:
                case VLineSpecialType.W1_StartMovingFloorPerpetual:
                case VLineSpecialType.W1_StopMovingFloor:
                case VLineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.W1_StopCrusherCeiling:
                case VLineSpecialType.W1_RaiseFloorTwentyFour:
                case VLineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
                case VLineSpecialType.WR_LowerCeilingToEightAboveFloor:
                case VLineSpecialType.WR_SlowCrusherCeilingFastDamage:
                case VLineSpecialType.WR_StopCrusherCeiling:
                case VLineSpecialType.WR_CloseDoor:
                case VLineSpecialType.WR_CloseDoorThirtySeconds:
                case VLineSpecialType.WR_FastCrusherCeilingSlowDamage:
                case VLineSpecialType.WR_LightOffMinBrightness:
                case VLineSpecialType.WR_LightLevelMatchBrightestAdjacent:
                case VLineSpecialType.WR_LightOnMaxBrigthness:
                case VLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case VLineSpecialType.WR_OpenDoorStay:
                case VLineSpecialType.WR_StartMovingFloorPerpetual:
                case VLineSpecialType.WR_LowerLiftRaise:
                case VLineSpecialType.WR_StopMovingFloor:
                case VLineSpecialType.WR_OpenDoorClose:
                case VLineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.WR_RaiseFloorTwentyFour:
                case VLineSpecialType.WR_RaiseFloorTwentyFourChangeTexture:
                case VLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                case VLineSpecialType.WR_RaiseByShortestLowerTexture:
                case VLineSpecialType.WR_Teleport:
                case VLineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.W1_RaiseStairsFast:
                case VLineSpecialType.W1_LightMatchDimmestAdjacent:
                case VLineSpecialType.WR_OpenDoorFastClose:
                case VLineSpecialType.WR_OpenDoorFastStayOpen:
                case VLineSpecialType.WR_CloseDoorFast:
                case VLineSpecialType.W1_OpenDoorFastClose:
                case VLineSpecialType.W1_OpenDoorFastStay:
                case VLineSpecialType.W1_CloseDoorFast:
                case VLineSpecialType.WR_LowerLiftFastRaise:
                case VLineSpecialType.W1_LowerLiftFastRaise:
                case VLineSpecialType.W_EndLevelSecret:
                case VLineSpecialType.WR_RaiseFloorToNextHigherFloor:
                case VLineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.W1_QuietCrusherCeilingFastDamage:
                case VLineSpecialType.W1_RaiseFloorToNextHigherFloor:
                    return ActivationType.PlayerLineCross;

                case VLineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.GR_OpenDoorStayOpen:
                case VLineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
                    return ActivationType.ProjectileHitsWall;

                case VLineSpecialType.ScrollTextureLeft:
                    return ActivationType.LevelStart;

                case VLineSpecialType.W1_MonsterTeleport:
                case VLineSpecialType.WR_MonsterTeleport:
                    return ActivationType.MonsterLineCross;
            }

            return ActivationType.None;
        }

        private static bool GetRepeat(VLineSpecialType type)
        {
            switch (type)
            {
                case VLineSpecialType.DR_DoorOpenClose:
                case VLineSpecialType.DR_OpenBlueKeyClose:
                case VLineSpecialType.DR_OpenYellowKeyClose:
                case VLineSpecialType.DR_OpenRedKeyClose:
                case VLineSpecialType.SR_CloseDoor:
                case VLineSpecialType.SR_LowerCeilingToFloor:
                case VLineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.SR_OpenDoorStay:
                case VLineSpecialType.SR_LowerLiftRaise:
                case VLineSpecialType.SR_OpenDoorClose:
                case VLineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                case VLineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
                case VLineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
                case VLineSpecialType.SR_RaiseFloorToNextHigher:
                case VLineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.SR_OpenBlueKeyFastStay:
                case VLineSpecialType.SR_OpenDoorFastClose:
                case VLineSpecialType.SR_OpenDoorFastStay:
                case VLineSpecialType.SR_CloseDoorFast:
                case VLineSpecialType.DR_OpenDoorFastClose:
                case VLineSpecialType.SR_LowerLiftFastRaise:
                case VLineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.SR_OpenRedKeyFastStay:
                case VLineSpecialType.SR_OpenYellowKeyFastStay:
                case VLineSpecialType.SR_LightOnMaxBrightness:
                case VLineSpecialType.SR_LightOffMinBrightness:
                case VLineSpecialType.WR_LowerCeilingToEightAboveFloor:
                case VLineSpecialType.WR_SlowCrusherCeilingFastDamage:
                case VLineSpecialType.WR_StopCrusherCeiling:
                case VLineSpecialType.WR_CloseDoor:
                case VLineSpecialType.WR_CloseDoorThirtySeconds:
                case VLineSpecialType.WR_FastCrusherCeilingSlowDamage:
                case VLineSpecialType.WR_LightOffMinBrightness:
                case VLineSpecialType.WR_LightLevelMatchBrightestAdjacent:
                case VLineSpecialType.WR_LightOnMaxBrigthness:
                case VLineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case VLineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case VLineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case VLineSpecialType.WR_OpenDoorStay:
                case VLineSpecialType.WR_StartMovingFloorPerpetual:
                case VLineSpecialType.WR_LowerLiftRaise:
                case VLineSpecialType.WR_StopMovingFloor:
                case VLineSpecialType.WR_OpenDoorClose:
                case VLineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case VLineSpecialType.WR_RaiseFloorTwentyFour:
                case VLineSpecialType.WR_RaiseFloorTwentyFourChangeTexture:
                case VLineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case VLineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                case VLineSpecialType.WR_RaiseByShortestLowerTexture:
                case VLineSpecialType.WR_Teleport:
                case VLineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case VLineSpecialType.WR_OpenDoorFastClose:
                case VLineSpecialType.WR_OpenDoorFastStayOpen:
                case VLineSpecialType.WR_CloseDoorFast:
                case VLineSpecialType.WR_LowerLiftFastRaise:
                case VLineSpecialType.WR_MonsterTeleport:
                case VLineSpecialType.WR_RaiseFloorToNextHigherFloor:
                case VLineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
                case VLineSpecialType.GR_OpenDoorStayOpen:
                    return true;
            }

            return false;
        }
    }
}
