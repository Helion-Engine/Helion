using Helion.World.Entities;
using Helion.World.Physics;

namespace Helion.Maps.Geometry.Lines
{
    /// <summary>
    /// Represents a line speical.
    /// </summary>
    public class LineSpecial
    {
        public LineSpecialType LineSpecialType;
        public SpecialActivationType SpecialActivationType;
        public bool Repeat;
        public bool Active;

        public LineSpecial(LineSpecialType type)
        {
            LineSpecialType = type;
            SpecialActivationType = GetSpecialActivationType(type);
            Repeat = GetRepeat(type);
        }

        /// <summary>
        /// Returns true if the given entity can activate this special given the activation context.
        /// </summary>
        public bool CanActivate(Entity entity, ActivationContext context)
        {
            if (entity.Player != null)
            {
                if (context == ActivationContext.CrossLine)
                    return SpecialActivationType == SpecialActivationType.PlayerLineCross;
                else if (context == ActivationContext.UseLine)
                    return SpecialActivationType == SpecialActivationType.PlayerUse || SpecialActivationType == SpecialActivationType.PlayerUsePassThrough;
            }

            return false;
        }

        private SpecialActivationType GetSpecialActivationType(LineSpecialType type)
        {
            switch (type)
            {
                case LineSpecialType.DR_DoorOpenClose:
                case LineSpecialType.S1_RaiseStairs8:
                case LineSpecialType.S1_Donut:
                case LineSpecialType.S_EndLevel:
                case LineSpecialType.S1_RaiseFloor32MatchAdjacentChangeTexture:
                case LineSpecialType.S1_RaiseFloor24MatchAdjacentChangeTexture:
                case LineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
                case LineSpecialType.S1_RaiseFloorToMatchNextHigher:
                case LineSpecialType.S1_LowerLiftRaise:
                case LineSpecialType.S1_LowerFloorToLowerAdjacentFloor:
                case LineSpecialType.DR_OpenBlueKeyClose:
                case LineSpecialType.DR_OpenYellowKeyClose:
                case LineSpecialType.DR_OpenRedKeyClose:
                case LineSpecialType.D1_OpenDoorStay:
                case LineSpecialType.D1_OpenBlueKeyStay:
                case LineSpecialType.D1_OpenRedKeyStay:
                case LineSpecialType.D1_OpenYellowKeyStay:
                case LineSpecialType.S1_OpenDoorClose:
                case LineSpecialType.SR_CloseDoor:
                case LineSpecialType.SR_LowerCeilingToFloor:
                case LineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
                case LineSpecialType.S1_CloseDoor:
                case LineSpecialType.S_EndLevelSecret:
                case LineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.SR_OpenDoorStay:
                case LineSpecialType.SR_LowerLiftRaise:
                case LineSpecialType.SR_OpenDoorClose:
                case LineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                case LineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigher:
                case LineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.SR_OpenBlueKeyFastStay:
                case LineSpecialType.S1_RaiseFLoorToLowestAdjacentCeiling:
                case LineSpecialType.S1_LowerFLoorToHighestAdjacentFloor:
                case LineSpecialType.S1_OpenDoorStay:
                case LineSpecialType.S1_OpenDoorFastClose:
                case LineSpecialType.S1_OpenDoorFastSay:
                case LineSpecialType.S1_CloseDoorFast:
                case LineSpecialType.SR_OpenDoorFastClose:
                case LineSpecialType.SR_OpenDoorFastStay:
                case LineSpecialType.SR_CloseDoorFast:
                case LineSpecialType.DR_OpenDoorFastClose:
                case LineSpecialType.D1_OpenDoorFastSay:
                case LineSpecialType.S1_RaiseFloorToNextHigherFLoor:
                case LineSpecialType.S1_LowerLiftFastRaise:
                case LineSpecialType.SR_LowerLiftFastRaise:
                case LineSpecialType.S1_RaiseStairsFast:
                case LineSpecialType.S1_RaiseFloorToNextHigherFloor:
                case LineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.S1_OpenBlueKeyFastStay:
                case LineSpecialType.SR_OpenRedKeyFastStay:
                case LineSpecialType.S1_OpenRedKeyFastStay:
                case LineSpecialType.SR_OpenYellowKeyFastStay:
                case LineSpecialType.S1_OpenYellowKeyFastStay:
                case LineSpecialType.SR_LightOnMaxBrightness:
                case LineSpecialType.SR_LightOffMinBrightness:
                case LineSpecialType.S1_RaiseFloor512:
                    return SpecialActivationType.PlayerUse;

                case LineSpecialType.W1_DoorOpenStay:
                case LineSpecialType.W1_CloseDoor:
                case LineSpecialType.W1_DoorOpenClose:
                case LineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.W1_FastCrusherCeiling:
                case LineSpecialType.W1_RaiseStairs8:
                case LineSpecialType.W1_LowerLiftRaise:
                case LineSpecialType.W1_LightLevelMatchBrightness:
                case LineSpecialType.W1_LightOnMaxBrightness:
                case LineSpecialType.W1_CloseDoor30Seconds:
                case LineSpecialType.W1_BlinkLightStartEveryOneSecond:
                case LineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.W1_SlowCrusherCeiling:
                case LineSpecialType.W1_RaiseFloorByShortestLowerTexture:
                case LineSpecialType.W1_LightOffMinBrightness:
                case LineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.W1_Teleport:
                case LineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
                case LineSpecialType.W1_LowerCeilingToFloor:
                case LineSpecialType.W1_LowerCeilingToEightAboveFloor:
                case LineSpecialType.W_EndLevel:
                case LineSpecialType.W1_StartMovingFloorPerpetual:
                case LineSpecialType.W1_StopMovingFloor:
                case LineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.W1_StopCrusherCeiling:
                case LineSpecialType.W1_RaiseFloorTwentyFour:
                case LineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
                case LineSpecialType.WR_LowerCeilingToEightAboveFloor:
                case LineSpecialType.WR_SlowCrusherCeilingFastDamage:
                case LineSpecialType.WR_StopCrusherCeiling:
                case LineSpecialType.WR_CloseDoor:
                case LineSpecialType.WR_CloseDoorThirtySeconds:
                case LineSpecialType.WR_FastCrusherCeilingSlowDamage:
                case LineSpecialType.WR_LiftOffMinBrightness:
                case LineSpecialType.WR_LightLevelMatchBrightestAdjacent:
                case LineSpecialType.WR_LightOnMaxBrigthness:
                case LineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.WR_OpenDoorStay:
                case LineSpecialType.WR_StartMovingFloorPerpetual:
                case LineSpecialType.WR_LowerLiftRaise:
                case LineSpecialType.WR_StopMovingFloor:
                case LineSpecialType.WR_OpenDoorClose:
                case LineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.WR_RaiseFLoorTwentyFour:
                case LineSpecialType.WR_RaiseFLoorChangeTexture:
                case LineSpecialType.WR_CrusherFLoorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.WR_RaiseByShortestLowerTexture:
                case LineSpecialType.WR_Teleport:
                case LineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.W1_RaiseStairsFast:
                case LineSpecialType.W1_LightMatchDimmestAdjacent:
                case LineSpecialType.WR_OpenDoorFastClose:
                case LineSpecialType.WR_OpenDoorFastStayOpen:
                case LineSpecialType.WR_CloseDoorFast:
                case LineSpecialType.W1_OpenDoorFastClose:
                case LineSpecialType.W1_OpenDoorFastStay:
                case LineSpecialType.W1_CloseDoorFast:
                case LineSpecialType.WR_LowerLiftFastRaise:
                case LineSpecialType.W1_LowerLiftFastRaise:
                case LineSpecialType.W_EndLevelSecret:
                case LineSpecialType.W1_MonsterTeleport:
                case LineSpecialType.WR_MonsterTeleport:
                case LineSpecialType.WR_RaiseFloorToNextHigherFloor:
                case LineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.W1_QuietCrusherCeilingFastDamage:
                    return SpecialActivationType.PlayerLineCross;

                case LineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.GR_OpenDoorStayOpen:
                case LineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
                    return SpecialActivationType.ProjectileHitsWall;

                case LineSpecialType.ScrollTextureLeft:
                    return SpecialActivationType.LevelStart;

                default:
                    break;
            }

            return SpecialActivationType.None;
        }

        private bool GetRepeat(LineSpecialType type)
        {
            switch (type)
            {
                case LineSpecialType.DR_DoorOpenClose:
                case LineSpecialType.DR_OpenBlueKeyClose:
                case LineSpecialType.DR_OpenYellowKeyClose:
                case LineSpecialType.DR_OpenRedKeyClose:
                case LineSpecialType.SR_CloseDoor:
                case LineSpecialType.SR_LowerCeilingToFloor:
                case LineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.SR_OpenDoorStay:
                case LineSpecialType.SR_LowerLiftRaise:
                case LineSpecialType.SR_OpenDoorClose:
                case LineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                case LineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigher:
                case LineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.SR_OpenBlueKeyFastStay:
                case LineSpecialType.SR_OpenDoorFastClose:
                case LineSpecialType.SR_OpenDoorFastStay:
                case LineSpecialType.SR_CloseDoorFast:
                case LineSpecialType.DR_OpenDoorFastClose:
                case LineSpecialType.SR_LowerLiftFastRaise:
                case LineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.SR_OpenRedKeyFastStay:
                case LineSpecialType.SR_OpenYellowKeyFastStay:
                case LineSpecialType.SR_LightOnMaxBrightness:
                case LineSpecialType.SR_LightOffMinBrightness:
                case LineSpecialType.WR_LowerCeilingToEightAboveFloor:
                case LineSpecialType.WR_SlowCrusherCeilingFastDamage:
                case LineSpecialType.WR_StopCrusherCeiling:
                case LineSpecialType.WR_CloseDoor:
                case LineSpecialType.WR_CloseDoorThirtySeconds:
                case LineSpecialType.WR_FastCrusherCeilingSlowDamage:
                case LineSpecialType.WR_LiftOffMinBrightness:
                case LineSpecialType.WR_LightLevelMatchBrightestAdjacent:
                case LineSpecialType.WR_LightOnMaxBrigthness:
                case LineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.WR_OpenDoorStay:
                case LineSpecialType.WR_StartMovingFloorPerpetual:
                case LineSpecialType.WR_LowerLiftRaise:
                case LineSpecialType.WR_StopMovingFloor:
                case LineSpecialType.WR_OpenDoorClose:
                case LineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.WR_RaiseFLoorTwentyFour:
                case LineSpecialType.WR_RaiseFLoorChangeTexture:
                case LineSpecialType.WR_CrusherFLoorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.WR_RaiseByShortestLowerTexture:
                case LineSpecialType.WR_Teleport:
                case LineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.WR_OpenDoorFastClose:
                case LineSpecialType.WR_OpenDoorFastStayOpen:
                case LineSpecialType.WR_CloseDoorFast:
                case LineSpecialType.WR_LowerLiftFastRaise:
                case LineSpecialType.WR_MonsterTeleport:
                case LineSpecialType.WR_RaiseFloorToNextHigherFloor:
                case LineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.GR_OpenDoorStayOpen:
                    return true;

                default:
                    break;
            }

            return false;
        }
    }
}
