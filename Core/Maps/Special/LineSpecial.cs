using System;
using Helion.World.Entities;
using Helion.World.Physics;

namespace Helion.Maps.Special
{
    /// <summary>
    /// Represents a line speical.
    /// </summary>
    public class LineSpecial
    {
        public LineSpecialType LineSpecialType;
        public ActivationType ActivationType;
        public bool Repeat;
        public bool Active;

        private const double SectorSlowSpeed = 1.0;
        private const double SectorFastSpeed = 2.0;

        private const double LiftSlowSpeed = 4.0;
        private const double LiftFastSpeed = 8.0;

        private const double DoorSlowSpeed = 4.0;
        private const double DoorFastSpeed = 8.0;

        private const double StairSlowSpeed = 0.5;
        private const double StairFastSpeed = 1.0;

        private const int LiftDelay = 105;
        private const int DoorDelay = 150;

        private const int DoorDestOffset = -4;

        private LineSpecialData m_lineSpecialData;

        public LineSpecial(LineSpecialType type)
        {
            LineSpecialType = type;
            ActivationType = GetSpecialActivationType(type);
            Repeat = GetRepeat(type);

            m_lineSpecialData = new LineSpecialData(LineSpecialType, ActivationType, GetSectorMoveType(), GetSectorStartDirection(), GetSectorDestination(), 
                GetRepetition(), GetSectorMoveSpeed(), GetDelay());
        }

        /// <summary>
        /// Returns true if the given entity can activate this special given the activation context.
        /// </summary>
        public bool CanActivate(Entity entity, ActivationContext context)
        {
            if (!Active && entity.Player != null)
            {
                if (context == ActivationContext.CrossLine)
                    return ActivationType == ActivationType.PlayerLineCross;
                else if (context == ActivationContext.UseLine)
                    return ActivationType == ActivationType.PlayerUse || ActivationType == ActivationType.PlayerUsePassThrough;
            }

            return false;
        }

        public bool IsSectorMoveSpecial() => m_lineSpecialData.SectorDestination != SectorDest.None;

        public LineSpecialData GetLineSpecialData()
        {
            return m_lineSpecialData;
        }

        public bool IsTeleport()
        {
            switch (LineSpecialType)
            {
                case LineSpecialType.W1_Teleport:
                case LineSpecialType.WR_Teleport:
                case LineSpecialType.W1_MonsterTeleport:
                case LineSpecialType.WR_MonsterTeleport:
                    return true;
            }

            return false;
        }

        public bool IsDoor()
        {
            switch (LineSpecialType)
            {
                case LineSpecialType.D1_OpenBlueKeyStay:
                case LineSpecialType.D1_OpenDoorFastStay:
                case LineSpecialType.D1_OpenDoorStay:
                case LineSpecialType.D1_OpenRedKeyStay:
                case LineSpecialType.D1_OpenYellowKeyStay:
                case LineSpecialType.DR_DoorOpenClose:
                case LineSpecialType.DR_OpenBlueKeyClose:
                case LineSpecialType.DR_OpenDoorFastClose:
                case LineSpecialType.DR_OpenRedKeyClose:
                case LineSpecialType.DR_OpenYellowKeyClose:
                case LineSpecialType.GR_OpenDoorStayOpen:
                case LineSpecialType.SR_OpenDoorClose:
                case LineSpecialType.SR_OpenDoorFastClose:
                case LineSpecialType.SR_OpenDoorFastStay:
                case LineSpecialType.SR_OpenDoorStay:
                case LineSpecialType.SR_OpenRedKeyFastStay:
                case LineSpecialType.SR_OpenYellowKeyFastStay:
                case LineSpecialType.S1_OpenDoorClose:
                case LineSpecialType.S1_OpenDoorFastClose:
                case LineSpecialType.S1_OpenDoorFastSay:
                case LineSpecialType.S1_OpenDoorStay:
                case LineSpecialType.S1_OpenRedKeyFastStay:
                case LineSpecialType.S1_OpenYellowKeyFastStay:
                case LineSpecialType.W1_DoorOpenClose:
                case LineSpecialType.W1_DoorOpenStay:
                case LineSpecialType.W1_OpenDoorFastClose:
                case LineSpecialType.W1_OpenDoorFastStay:
                case LineSpecialType.SR_OpenBlueKeyFastStay:
                case LineSpecialType.WR_OpenDoorStay:
                case LineSpecialType.WR_OpenDoorClose:
                case LineSpecialType.WR_OpenDoorFastClose:
                    return true;
            }

            return false;
        }

        public bool IsLift()
        {
            switch (LineSpecialType)
            {
                case LineSpecialType.SR_LowerLiftRaise:
                case LineSpecialType.SR_LowerLiftFastRaise:
                case LineSpecialType.WR_LowerLiftRaise:
                case LineSpecialType.W1_LowerLiftRaise:
                case LineSpecialType.S1_LowerLiftRaise:
                case LineSpecialType.WR_LowerLiftFastRaise:
                case LineSpecialType.W1_LowerLiftFastRaise:
                case LineSpecialType.S1_LowerLiftFastRaise:
                    return true;
            }

            return false;
        }

        public bool IsFloorMover()
        {
            if (IsLift())
                return true;

            switch (LineSpecialType)
            {
                case LineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.S1_RaiseFloor32MatchAdjacentChangeTexture:
                case LineSpecialType.S1_RaiseFloor24MatchAdjacentChangeTexture:
                case LineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
                case LineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.S1_RaiseFloorToMatchNextHigher:
                case LineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.W1_RaiseFloorByShortestLowerTexture:
                case LineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.W1_StartMovingFloorPerpetual:
                case LineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.W1_RaiseFloorTwentyFour:
                case LineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
                case LineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                case LineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigher:
                case LineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.WR_StartMovingFloorPerpetual:
                case LineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.WR_RaiseFLoorTwentyFour:
                case LineSpecialType.WR_RaiseFLoorChangeTexture:
                case LineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_RaiseFLoorToLowestAdjacentCeiling:
                case LineSpecialType.S1_LowerFLoorToHighestAdjacentFloor:
                case LineSpecialType.WR_RaiseFloorToNextHigherFloor:
                case LineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.S1_RaiseFloorToNextHigherFloor:
                case LineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.S1_RaiseFloor512:
                case LineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.S1_RaiseStairs8:
                case LineSpecialType.W1_RaiseStairs8:
                case LineSpecialType.W1_RaiseStairsFast:
                case LineSpecialType.S1_RaiseStairsFast:
                    return true;
            }

            return false;
        }

        public SectorMoveType GetSectorMoveType()
        {
            return IsFloorMover() ? SectorMoveType.Floor : SectorMoveType.Ceiling;
        }

        public SectorDest GetSectorDestination()
        {
            if (IsDoor())
                return SectorDest.LowestAdjacentCeiling;
            else if (IsLift())
                return SectorDest.LowestAdjacentFloor;

            switch (LineSpecialType)
            {
                case LineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.S1_RaiseFLoorToLowestAdjacentCeiling:
                    return SectorDest.LowestAdjacentCeiling;

                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.S1_RaiseFloorToMatchNextHigher:
                    return SectorDest.LowestAdjacentFloor;

                case LineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                case LineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_LowerFLoorToHighestAdjacentFloor:
                    return SectorDest.HighestAdjacentFloor;

                case LineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
                case LineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                    return SectorDest.LowestAdjacentCeiling;

                case LineSpecialType.W1_LowerCeilingToFloor:
                case LineSpecialType.SR_LowerCeilingToFloor:
                case LineSpecialType.W1_LowerCeilingToEightAboveFloor:
                case LineSpecialType.WR_LowerCeilingToEightAboveFloor:
                case LineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
                case LineSpecialType.WR_SlowCrusherCeilingFastDamage:
                case LineSpecialType.WR_FastCrusherCeilingSlowDamage:
                case LineSpecialType.SR_CloseDoor:
                case LineSpecialType.W1_CloseDoor30Seconds:
                case LineSpecialType.S1_CloseDoor:
                case LineSpecialType.WR_CloseDoor:
                case LineSpecialType.WR_CloseDoorThirtySeconds:
                case LineSpecialType.WR_CloseDoorFast:
                case LineSpecialType.W1_CloseDoorFast:
                case LineSpecialType.S1_CloseDoorFast:
                case LineSpecialType.SR_CloseDoorFast:
                    return SectorDest.Floor;
            }

            return SectorDest.None;
        }

        public MoveRepetition GetRepetition()
        {
            switch (LineSpecialType)
            {
                case LineSpecialType.W1_DoorOpenStay:
                case LineSpecialType.D1_OpenDoorStay:
                case LineSpecialType.D1_OpenBlueKeyStay:
                case LineSpecialType.D1_OpenRedKeyStay:
                case LineSpecialType.D1_OpenYellowKeyStay:
                case LineSpecialType.GR_OpenDoorStayOpen:
                case LineSpecialType.SR_OpenDoorStay:
                case LineSpecialType.WR_OpenDoorStay:
                case LineSpecialType.SR_OpenBlueKeyFastStay:
                case LineSpecialType.S1_OpenDoorStay:
                case LineSpecialType.W1_OpenDoorFastStay:
                case LineSpecialType.SR_OpenDoorFastStay:
                case LineSpecialType.D1_OpenDoorFastStay:
                case LineSpecialType.S1_OpenBlueKeyFastStay:
                case LineSpecialType.SR_OpenRedKeyFastStay:
                case LineSpecialType.S1_OpenRedKeyFastStay:
                case LineSpecialType.SR_OpenYellowKeyFastStay:
                case LineSpecialType.S1_OpenYellowKeyFastStay:
                case LineSpecialType.S1_LowerFLoorToHighestAdjacentFloor:
                case LineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_RaiseFloor24MatchAdjacentChangeTexture:
                case LineSpecialType.S1_RaiseFloor32MatchAdjacentChangeTexture:
                case LineSpecialType.S1_RaiseFloor512:
                case LineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
                case LineSpecialType.S1_RaiseFLoorToLowestAdjacentCeiling:
                case LineSpecialType.S1_RaiseFloorToMatchNextHigher:
                case LineSpecialType.S1_RaiseFloorToNextHigherFloor:
                case LineSpecialType.W1_RaiseFloorToNextHigherFloor:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.W1_LowerCeilingToEightAboveFloor:
                case LineSpecialType.W1_LowerCeilingToFloor:
                case LineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                case LineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.SR_LowerCeilingToFloor:
                case LineSpecialType.WR_LowerCeilingToEightAboveFloor:
                case LineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
                case LineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.SR_CloseDoor:
                case LineSpecialType.W1_CloseDoor30Seconds:
                case LineSpecialType.S1_CloseDoor:
                case LineSpecialType.WR_CloseDoor:
                case LineSpecialType.WR_CloseDoorThirtySeconds:
                case LineSpecialType.WR_CloseDoorFast:
                case LineSpecialType.W1_CloseDoorFast:
                case LineSpecialType.S1_CloseDoorFast:
                case LineSpecialType.SR_CloseDoorFast:
                    return MoveRepetition.None;
            }

            return MoveRepetition.DelayReturn;
        }

        public MoveDirection GetSectorStartDirection()
        {
            switch (LineSpecialType)
            {
                case LineSpecialType.W1_CloseDoor:
                case LineSpecialType.W1_FastCrusherCeiling:
                case LineSpecialType.W1_LowerLiftRaise:
                case LineSpecialType.W1_CloseDoor30Seconds:
                case LineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.S1_LowerLiftRaise:
                case LineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.W1_SlowCrusherCeiling:
                case LineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.W1_LowerCeilingToFloor:
                case LineSpecialType.SR_CloseDoor:
                case LineSpecialType.SR_LowerCeilingToFloor:
                case LineSpecialType.W1_LowerCeilingToEightAboveFloor:
                case LineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
                case LineSpecialType.S1_CloseDoor:
                case LineSpecialType.W1_StartMovingFloorPerpetual: // TODO not sure how this works yet...
                case LineSpecialType.WR_StartMovingFloorPerpetual: // TODO not sure how this works yet...
                case LineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.WR_LowerCeilingToEightAboveFloor:
                case LineSpecialType.SR_LowerLiftRaise:
                case LineSpecialType.WR_SlowCrusherCeilingFastDamage:
                case LineSpecialType.WR_CloseDoor:
                case LineSpecialType.WR_CloseDoorThirtySeconds:
                case LineSpecialType.WR_FastCrusherCeilingSlowDamage:
                case LineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.WR_LowerLiftRaise:
                case LineSpecialType.WR_OpenDoorClose:
                case LineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_LowerFLoorToHighestAdjacentFloor:
                case LineSpecialType.WR_CloseDoorFast:
                case LineSpecialType.W1_CloseDoorFast:
                case LineSpecialType.S1_CloseDoorFast:
                case LineSpecialType.SR_CloseDoorFast:
                case LineSpecialType.WR_LowerLiftFastRaise:
                case LineSpecialType.W1_LowerLiftFastRaise:
                case LineSpecialType.S1_LowerLiftFastRaise:
                case LineSpecialType.SR_LowerLiftFastRaise:
                case LineSpecialType.S1_RaiseStairsFast:
                case LineSpecialType.WR_RaiseFloorToNextHigherFloor:
                case LineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.S1_RaiseFloorToNextHigherFloor:
                case LineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.W1_QuietCrusherCeilingFastDamage:
                    return MoveDirection.Down;

                case LineSpecialType.DR_DoorOpenClose:
                case LineSpecialType.W1_DoorOpenStay:
                case LineSpecialType.W1_DoorOpenClose:
                case LineSpecialType.S1_RaiseFloor32MatchAdjacentChangeTexture:
                case LineSpecialType.S1_RaiseFloor24MatchAdjacentChangeTexture:
                case LineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
                case LineSpecialType.S1_RaiseFloorToMatchNextHigher:
                case LineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.DR_OpenBlueKeyClose:
                case LineSpecialType.DR_OpenYellowKeyClose:
                case LineSpecialType.DR_OpenRedKeyClose:
                case LineSpecialType.S1_OpenDoorClose:
                case LineSpecialType.W1_RaiseFloorByShortestLowerTexture:
                case LineSpecialType.D1_OpenDoorStay:
                case LineSpecialType.D1_OpenBlueKeyStay:
                case LineSpecialType.D1_OpenRedKeyStay:
                case LineSpecialType.D1_OpenYellowKeyStay:
                case LineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
                case LineSpecialType.GR_OpenDoorStayOpen:
                case LineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.W1_RaiseFloorTwentyFour:
                case LineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
                case LineSpecialType.SR_OpenDoorStay:
                case LineSpecialType.SR_OpenDoorClose:
                case LineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                case LineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigher:
                case LineSpecialType.WR_OpenDoorStay:
                case LineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.WR_RaiseFLoorTwentyFour:
                case LineSpecialType.WR_RaiseFLoorChangeTexture:
                case LineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.WR_RaiseByShortestLowerTexture:
                case LineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.SR_OpenBlueKeyFastStay:
                case LineSpecialType.W1_RaiseStairsFast:
                case LineSpecialType.S1_RaiseFLoorToLowestAdjacentCeiling:
                case LineSpecialType.S1_OpenDoorStay:
                case LineSpecialType.WR_OpenDoorFastClose:
                case LineSpecialType.WR_OpenDoorFastStayOpen:
                case LineSpecialType.W1_OpenDoorFastClose:
                case LineSpecialType.W1_OpenDoorFastStay:
                case LineSpecialType.S1_OpenDoorFastClose:
                case LineSpecialType.S1_OpenDoorFastSay:
                case LineSpecialType.SR_OpenDoorFastClose:
                case LineSpecialType.SR_OpenDoorFastStay:
                case LineSpecialType.DR_OpenDoorFastClose:
                case LineSpecialType.D1_OpenDoorFastStay:
                case LineSpecialType.W1_RaiseFloorToNextHigherFloor:
                case LineSpecialType.S1_OpenBlueKeyFastStay:
                case LineSpecialType.SR_OpenRedKeyFastStay:
                case LineSpecialType.S1_OpenRedKeyFastStay:
                case LineSpecialType.SR_OpenYellowKeyFastStay:
                case LineSpecialType.S1_OpenYellowKeyFastStay:
                case LineSpecialType.S1_RaiseFloor512:
                    return MoveDirection.Up;
            }

            return MoveDirection.None;
        }

        public double GetSectorMoveSpeed()
        {
            switch (LineSpecialType)
            {
                case LineSpecialType.WR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.W1_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.W1_FastCrusherCeiling:
                case LineSpecialType.WR_FastCrusherCeilingSlowDamage:
                case LineSpecialType.W1_QuietCrusherCeilingFastDamage:
                    return SectorFastSpeed;

                case LineSpecialType.W1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.S1_RaiseFloor32MatchAdjacentChangeTexture:
                case LineSpecialType.S1_RaiseFloor24MatchAdjacentChangeTexture:
                case LineSpecialType.S1_RaiseFloorMatchNextHigherFloor:
                case LineSpecialType.W1_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.S1_RaiseFloorToMatchNextHigher:
                case LineSpecialType.W1_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.W1_RaiseFloorByShortestLowerTexture:
                case LineSpecialType.W1_LowerFloorEightAboveHighestAdjacentFloor:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.W1_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.SR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.W1_StartMovingFloorPerpetual:
                case LineSpecialType.S1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.W1_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.W1_RaiseFloorTwentyFour:
                case LineSpecialType.W1_RaiseFloorTwentyFourMatchTexture:
                case LineSpecialType.SR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.SR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.SR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.SR_RaiseFloorTwentyFourMatchTexture:
                case LineSpecialType.SR_RaiseFloorThirtyTwoMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigherMatchTexture:
                case LineSpecialType.SR_RaiseFloorToNextHigher:
                case LineSpecialType.SR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToLowestAdjacentFloor:
                case LineSpecialType.WR_LowerFloorToHighestAdjacentFloor:
                case LineSpecialType.WR_LowerFLoorToLowestAdjacentFloorChangeTexture:
                case LineSpecialType.WR_StartMovingFloorPerpetual:
                case LineSpecialType.WR_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.WR_RaiseFLoorTwentyFour:
                case LineSpecialType.WR_RaiseFLoorChangeTexture:
                case LineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
                case LineSpecialType.WR_RaiseFloorToMatchNextHigherChangeTexture:
                case LineSpecialType.WR_LowerFloorToEightAboveHighestAdjacentFloor:
                case LineSpecialType.S1_RaiseFLoorToLowestAdjacentCeiling:
                case LineSpecialType.S1_LowerFLoorToHighestAdjacentFloor:
                case LineSpecialType.W1_RaiseFloorToNextHigherFloor:
                case LineSpecialType.WR_RaiseFloorToNextHigherFloor:
                case LineSpecialType.S1_RaiseFloorToNextHigherFloor:
                case LineSpecialType.S1_RaiseFloor512:
                case LineSpecialType.W1_SlowCrusherCeiling:
                case LineSpecialType.W1_RaiseCeilingToHighestAdjacentCeiling:
                case LineSpecialType.W1_LowerCeilingToFloor:
                case LineSpecialType.SR_LowerCeilingToFloor:
                case LineSpecialType.W1_LowerCeilingToEightAboveFloor:
                case LineSpecialType.S1_SlowCrusherCeilingToEightAboveFloor:
                case LineSpecialType.WR_SlowCrusherCeilingFastDamage:
                case LineSpecialType.WR_LowerCeilingToEightAboveFloor:
                    return SectorSlowSpeed;

                case LineSpecialType.W1_LowerLiftRaise:
                case LineSpecialType.S1_LowerLiftRaise:
                case LineSpecialType.SR_LowerLiftRaise:
                case LineSpecialType.WR_LowerLiftRaise:
                    return LiftSlowSpeed;

                case LineSpecialType.WR_LowerLiftFastRaise:
                case LineSpecialType.W1_LowerLiftFastRaise:
                case LineSpecialType.S1_LowerLiftFastRaise:
                case LineSpecialType.SR_LowerLiftFastRaise:             
                    return LiftFastSpeed;

                case LineSpecialType.DR_DoorOpenClose:
                case LineSpecialType.W1_DoorOpenStay:
                case LineSpecialType.W1_CloseDoor:
                case LineSpecialType.W1_DoorOpenClose:
                case LineSpecialType.W1_CloseDoor30Seconds:
                case LineSpecialType.S1_OpenDoorClose:
                case LineSpecialType.D1_OpenDoorStay:
                case LineSpecialType.SR_CloseDoor:
                case LineSpecialType.GR_OpenDoorStayOpen:
                case LineSpecialType.S1_CloseDoor:
                case LineSpecialType.SR_OpenDoorStay:
                case LineSpecialType.SR_OpenDoorClose:
                case LineSpecialType.WR_CloseDoor:
                case LineSpecialType.WR_CloseDoorThirtySeconds:
                case LineSpecialType.WR_OpenDoorStay:
                case LineSpecialType.WR_OpenDoorClose:
                case LineSpecialType.S1_OpenDoorStay:
                    return DoorSlowSpeed;

                case LineSpecialType.SR_OpenYellowKeyFastStay:
                case LineSpecialType.S1_OpenYellowKeyFastStay:
                case LineSpecialType.WR_OpenDoorFastClose:
                case LineSpecialType.WR_OpenDoorFastStayOpen:
                case LineSpecialType.W1_OpenDoorFastClose:
                case LineSpecialType.W1_OpenDoorFastStay:
                case LineSpecialType.S1_OpenDoorFastClose:
                case LineSpecialType.S1_OpenDoorFastSay:
                case LineSpecialType.SR_OpenDoorFastClose:
                case LineSpecialType.SR_OpenDoorFastStay:
                case LineSpecialType.DR_OpenDoorFastClose:
                case LineSpecialType.D1_OpenDoorFastStay:
                case LineSpecialType.S1_OpenBlueKeyFastStay:
                case LineSpecialType.SR_OpenRedKeyFastStay:
                case LineSpecialType.S1_OpenRedKeyFastStay:
                case LineSpecialType.SR_OpenBlueKeyFastStay:
                case LineSpecialType.WR_CloseDoorFast:
                case LineSpecialType.W1_CloseDoorFast:
                case LineSpecialType.S1_CloseDoorFast:
                case LineSpecialType.SR_CloseDoorFast:
                    return DoorFastSpeed;

                case LineSpecialType.S1_RaiseStairs8:
                case LineSpecialType.W1_RaiseStairs8:
                    return StairSlowSpeed;

                case LineSpecialType.W1_RaiseStairsFast:
                case LineSpecialType.S1_RaiseStairsFast:
                    return StairFastSpeed;
            }

            return 0.0;
        }

        public double GetDestOffset()
        {
            if (m_lineSpecialData.StartDirection == MoveDirection.Up && IsDoor())
                return DoorDestOffset;

            return 0;
        }

        private int GetDelay()
        {
            if (IsDoor())
                return DoorDelay;
            else if (IsLift())
                return LiftDelay;

            return 0;
        }

        private ActivationType GetSpecialActivationType(LineSpecialType type)
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
                case LineSpecialType.S1_LowerFloorToLowestAdjacentFloor:
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
                case LineSpecialType.D1_OpenDoorFastStay:
                case LineSpecialType.S1_RaiseFloorToNextHigherFloor:
                case LineSpecialType.S1_LowerLiftFastRaise:
                case LineSpecialType.SR_LowerLiftFastRaise:
                case LineSpecialType.S1_RaiseStairsFast:
                case LineSpecialType.SR_RaiseFloorFastToNextHigherFloor:
                case LineSpecialType.S1_OpenBlueKeyFastStay:
                case LineSpecialType.SR_OpenRedKeyFastStay:
                case LineSpecialType.S1_OpenRedKeyFastStay:
                case LineSpecialType.SR_OpenYellowKeyFastStay:
                case LineSpecialType.S1_OpenYellowKeyFastStay:
                case LineSpecialType.SR_LightOnMaxBrightness:
                case LineSpecialType.SR_LightOffMinBrightness:
                case LineSpecialType.S1_RaiseFloor512:
                    return ActivationType.PlayerUse;

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
                case LineSpecialType.WR_LightOffMinBrightness:
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
                case LineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
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
                case LineSpecialType.W1_RaiseFloorToNextHigherFloor:
                    return ActivationType.PlayerLineCross;

                case LineSpecialType.G1_RaiseFloorToLowestAdjacentCeiling:
                case LineSpecialType.GR_OpenDoorStayOpen:
                case LineSpecialType.G1_RaiseFloorToMatchNextHigherChangeTexture:
                    return ActivationType.ProjectileHitsWall;

                case LineSpecialType.ScrollTextureLeft:
                    return ActivationType.LevelStart;
            }

            return ActivationType.None;
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
                case LineSpecialType.WR_LightOffMinBrightness:
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
                case LineSpecialType.WR_CrusherFloorRaiseToEightBelowAdjacentCeiling:
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
            }

            return false;
        }
    }
}
