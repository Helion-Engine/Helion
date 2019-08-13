using System;
using Helion.Maps.Geometry.Lines;
using Helion.World.Entities;
using Helion.World.Physics;

namespace Helion.Maps.Special
{
    /// <summary>
    /// Represents a line speical.
    /// </summary>
    public class LineSpecial
    {
        public ZLineSpecialType LineSpecialType;
        public bool Active;

        private bool m_moveSpecial;

        public LineSpecial(ZLineSpecialType type)
        {
            LineSpecialType = type;
            m_moveSpecial = SetMoveSpecial();
        }

        /// <summary>
        /// Returns true if the given entity can activate this special given the activation context.
        /// </summary>
        public bool CanActivate(Entity entity, LineFlags flags, ActivationContext context)
        {
            if (!Active && entity.Player != null)
            {
                if (context == ActivationContext.CrossLine)
                    return flags.ActivationType == ActivationType.PlayerLineCross;
                else if (context == ActivationContext.UseLine)
                    return flags.ActivationType == ActivationType.PlayerUse || flags.ActivationType == ActivationType.PlayerUsePassThrough;
            }

            return false;
        }

        public bool IsSectorMoveSpecial() => m_moveSpecial;

        public bool IsTeleport()
        {
            switch (LineSpecialType)
            {
                case ZLineSpecialType.Teleport:
                    return true;
            }

            return false;
        }

        private bool SetMoveSpecial()
        {
            switch (LineSpecialType)
            {
                case ZLineSpecialType.FloorLowerByValue:
                case ZLineSpecialType.FloorLowerToLowest:
                case ZLineSpecialType.FloorLowerToNearest:
                case ZLineSpecialType.FloorRaiseByValue:
                case ZLineSpecialType.FloorRaiseToHighest:
                case ZLineSpecialType.FloorRaiseToNearset:
                case ZLineSpecialType.BuildStairsDown:
                case ZLineSpecialType.BuildStairsUp:
                case ZLineSpecialType.FloorRaiseCrush:
                case ZLineSpecialType.PillarRaiseFloorToCeiling:
                case ZLineSpecialType.PillarRaiseFlorAndLowerCeiling:
                case ZLineSpecialType.BuildStairsDownSync:
                case ZLineSpecialType.BuildStairsUpSync:
                case ZLineSpecialType.FloorRaiseByValueTimes8:
                case ZLineSpecialType.FloorLowerByValueTimes8:
                case ZLineSpecialType.CeilingLowerByValue:
                case ZLineSpecialType.CeilingRaiseByValue:
                case ZLineSpecialType.CeilingCrushRaiseAndLower:
                case ZLineSpecialType.CeilingCrushStayDown:
                case ZLineSpecialType.CeilingCrushStop:
                case ZLineSpecialType.CeilingCrushRaiseStay:
                case ZLineSpecialType.LiftPerpetual:
                case ZLineSpecialType.PlatStop:
                case ZLineSpecialType.LiftDownWaitUpStay:
                case ZLineSpecialType.LiftDownValueTimes8:
                case ZLineSpecialType.LiftUpWaitDownStay:
                case ZLineSpecialType.PlatUpByValue:
                case ZLineSpecialType.FloorLowerNow:
                case ZLineSpecialType.FloorRaiseNow:
                case ZLineSpecialType.FloorMoveToValueTimes8:
                case ZLineSpecialType.CeilingMoveToValueTimes8:
                case ZLineSpecialType.PillarBuildCrush:
                case ZLineSpecialType.FloorAndCeilingLowerByValue:
                case ZLineSpecialType.FloorAndCeilingRaiseByValue:
                case ZLineSpecialType.FloorLowerToHighest:
                case ZLineSpecialType.FloorRaiseToLowestCeiling:
                case ZLineSpecialType.FloorLowerToLowestTxTy:
                case ZLineSpecialType.FloorRaiseToLowest:
                case ZLineSpecialType.DoorClose:
                case ZLineSpecialType.DoorOpenStay:
                case ZLineSpecialType.DoorOpenClose:
                case ZLineSpecialType.FloorRaiseByValueTxTy:
                case ZLineSpecialType.FloorRaiseByTexutre:
                case ZLineSpecialType.DoorCloseWaitOpen:
                case ZLineSpecialType.FloorDonut:
                case ZLineSpecialType.FloorCeilingLowerRaise:
                case ZLineSpecialType.CeilingRaiseToNearest:
                case ZLineSpecialType.CeilingLowerToLowest:
                case ZLineSpecialType.CeilingLowerToFloor:
                case ZLineSpecialType.CeilingCrushRaiseStaySilent:
                case ZLineSpecialType.PlatPerpetualRaiseLip:
                case ZLineSpecialType.FloorRaiseAndCrushDoom:
                case ZLineSpecialType.StairsBuildUpDoom:
                case ZLineSpecialType.StairsBuildUpDoomCrush:
                case ZLineSpecialType.DoorLockedRaise:
                    return true;
            }

            return false;
        }
    }
}
