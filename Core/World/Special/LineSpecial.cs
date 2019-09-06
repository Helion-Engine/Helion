using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Physics;

namespace Helion.World.Special
{
    /// <summary>
    /// Represents a line special.
    /// </summary>
    public class LineSpecial
    {
        public readonly ZDoomLineSpecialType LineSpecialType;
        public bool Active;
        private bool m_moveSpecial;
        private bool m_lightSpecial;

        public LineSpecial(ZDoomLineSpecialType type)
        {
            LineSpecialType = type;
            m_moveSpecial = SetMoveSpecial();
            m_lightSpecial = SetLightSpecial();
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
                if (context == ActivationContext.UseLine)
                    return flags.ActivationType == ActivationType.PlayerUse || flags.ActivationType == ActivationType.PlayerUsePassThrough;
            }

            return false;
        }

        public bool IsSectorMoveSpecial() => m_moveSpecial;
        public bool IsSectorLightSpecial() => m_lightSpecial;
        public bool CanActivateDuringSectorMovement() => LineSpecialType == ZDoomLineSpecialType.DoorOpenClose;

        public bool IsTeleport()
        {
            switch (LineSpecialType)
            {
            case ZDoomLineSpecialType.Teleport:
                return true;
            }

            return false;
        }

        private bool SetMoveSpecial()
        {
            switch (LineSpecialType)
            {
            case ZDoomLineSpecialType.FloorLowerByValue:
            case ZDoomLineSpecialType.FloorLowerToLowest:
            case ZDoomLineSpecialType.FloorLowerToNearest:
            case ZDoomLineSpecialType.FloorRaiseByValue:
            case ZDoomLineSpecialType.FloorRaiseToHighest:
            case ZDoomLineSpecialType.FloorRaiseToNearest:
            case ZDoomLineSpecialType.BuildStairsDown:
            case ZDoomLineSpecialType.BuildStairsUp:
            case ZDoomLineSpecialType.FloorRaiseCrush:
            case ZDoomLineSpecialType.PillarRaiseFloorToCeiling:
            case ZDoomLineSpecialType.PillarRaiseFlorAndLowerCeiling:
            case ZDoomLineSpecialType.BuildStairsDownSync:
            case ZDoomLineSpecialType.BuildStairsUpSync:
            case ZDoomLineSpecialType.FloorRaiseByValueTimes8:
            case ZDoomLineSpecialType.FloorLowerByValueTimes8:
            case ZDoomLineSpecialType.CeilingLowerByValue:
            case ZDoomLineSpecialType.CeilingRaiseByValue:
            case ZDoomLineSpecialType.CeilingCrushRaiseAndLower:
            case ZDoomLineSpecialType.CeilingCrushStayDown:
            case ZDoomLineSpecialType.CeilingCrushStop:
            case ZDoomLineSpecialType.CeilingCrushRaiseStay:
            case ZDoomLineSpecialType.LiftPerpetual:
            case ZDoomLineSpecialType.PlatStop:
            case ZDoomLineSpecialType.LiftDownWaitUpStay:
            case ZDoomLineSpecialType.LiftDownValueTimes8:
            case ZDoomLineSpecialType.LiftUpWaitDownStay:
            case ZDoomLineSpecialType.PlatUpByValue:
            case ZDoomLineSpecialType.FloorLowerNow:
            case ZDoomLineSpecialType.FloorRaiseNow:
            case ZDoomLineSpecialType.FloorMoveToValueTimes8:
            case ZDoomLineSpecialType.CeilingMoveToValueTimes8:
            case ZDoomLineSpecialType.PillarBuildCrush:
            case ZDoomLineSpecialType.FloorAndCeilingLowerByValue:
            case ZDoomLineSpecialType.FloorAndCeilingRaiseByValue:
            case ZDoomLineSpecialType.FloorLowerToHighest:
            case ZDoomLineSpecialType.FloorRaiseToLowestCeiling:
            case ZDoomLineSpecialType.FloorLowerToLowestTxTy:
            case ZDoomLineSpecialType.FloorRaiseToLowest:
            case ZDoomLineSpecialType.DoorClose:
            case ZDoomLineSpecialType.DoorOpenStay:
            case ZDoomLineSpecialType.DoorOpenClose:
            case ZDoomLineSpecialType.FloorRaiseByValueTxTy:
            case ZDoomLineSpecialType.FloorRaiseByTexture:
            case ZDoomLineSpecialType.DoorCloseWaitOpen:
            case ZDoomLineSpecialType.FloorDonut:
            case ZDoomLineSpecialType.FloorCeilingLowerRaise:
            case ZDoomLineSpecialType.CeilingRaiseToNearest:
            case ZDoomLineSpecialType.CeilingLowerToLowest:
            case ZDoomLineSpecialType.CeilingLowerToFloor:
            case ZDoomLineSpecialType.CeilingCrushRaiseStaySilent:
            case ZDoomLineSpecialType.PlatPerpetualRaiseLip:
            case ZDoomLineSpecialType.FloorRaiseAndCrushDoom:
            case ZDoomLineSpecialType.StairsBuildUpDoom:
            case ZDoomLineSpecialType.StairsBuildUpDoomCrush:
            case ZDoomLineSpecialType.DoorLockedRaise:
            case ZDoomLineSpecialType.CeilingCrushAndRaiseDist:
            case ZDoomLineSpecialType.PlatRaiseAndStay:
                return true;
            }

            return false;
        }

        private bool SetLightSpecial()
        {
            switch (LineSpecialType)
            {
            case ZDoomLineSpecialType.LightRaiseByValue:
            case ZDoomLineSpecialType.LightLowerByValue:
            case ZDoomLineSpecialType.LightChangeToValue:
            case ZDoomLineSpecialType.LightFadeToValue:
            case ZDoomLineSpecialType.LightGlow:
            case ZDoomLineSpecialType.LightFlicker:
            case ZDoomLineSpecialType.LightStrobe:
            case ZDoomLineSpecialType.LightStop:
            case ZDoomLineSpecialType.LightStrobeDoom:
            case ZDoomLineSpecialType.LightMinNeighbor:
            case ZDoomLineSpecialType.LightMaxNeighbor:
                return true;
            }

            return false;
        }
    }
}