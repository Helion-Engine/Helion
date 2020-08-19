using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Entities;
using Helion.World.Entities.Players;
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
        public bool Active { get; set; }
        private readonly bool m_moveSpecial;
        private readonly bool m_sectorStopMoveSpecial;
        private readonly bool m_lightSpecial;
        private readonly bool m_stopLightSpecial;
        private readonly bool m_monsterCanUse;
        private readonly LineActivationType m_lineActivationType;

        public LineSpecial(ZDoomLineSpecialType type) : this(type, LineActivationType.Any)
        {
        }

        public LineSpecial(ZDoomLineSpecialType type, LineActivationType lineActivationType)
        {
            LineSpecialType = type;
            m_lineActivationType = lineActivationType;
            m_moveSpecial = SetMoveSpecial();
            m_sectorStopMoveSpecial = SetSectorStopSpecial();
            m_lightSpecial = SetLightSpecial();
            m_stopLightSpecial = SetStopLightSpecial();
            m_monsterCanUse = SetMonsterCanUse();
        }

        public static void ValidateActivationFlags(ZDoomLineSpecialType type, LineFlags flags)
        {
            switch (type)
            {
                case ZDoomLineSpecialType.ScrollTextureLeft:
                case ZDoomLineSpecialType.ScrollTextureRight:
                case ZDoomLineSpecialType.ScrollTextureUp:
                case ZDoomLineSpecialType.ScrollTextureDown:
                    flags.ActivationType = ActivationType.LevelStart;
                    break;
            }
        }

        public bool CanActivateByTag => (m_lineActivationType & LineActivationType.Tag) != 0;
        public bool CanActivateByBackSide => (m_lineActivationType & LineActivationType.BackSide) != 0;

        /// <summary>
        /// Returns true if the given entity can activate this special given the activation context.
        /// </summary>
        public bool CanActivate(Entity entity, Line line, ActivationContext context)
        {
            if (Active)
                return false;

            LineFlags flags = line.Flags;
            if (entity.Flags.Missile)
            {
                if (context == ActivationContext.ProjectileHitLine)
                    return flags.ActivationType == ActivationType.ProjectileHitsWall || flags.ActivationType == ActivationType.ProjectileHitsOrCrossesLine;
                else if (context == ActivationContext.CrossLine)
                    return flags.ActivationType == ActivationType.ProjectileCrossesLine || flags.ActivationType == ActivationType.ProjectileHitsOrCrossesLine;
            }
            else if (entity.Flags.IsMonster)
            {
                if (context == ActivationContext.CrossLine)
                    return flags.ActivationType == ActivationType.MonsterLineCross;
                else if (context == ActivationContext.UseLine)
                    return flags.ActivationType == ActivationType.PlayerUse && line.TagArg == 0 && line.Special.MonsterCanUse();
            }
            else if (entity is Player)
            {
                if (context == ActivationContext.CrossLine)
                    return flags.ActivationType == ActivationType.PlayerLineCross;
                else if (context == ActivationContext.UseLine)
                    return flags.ActivationType == ActivationType.PlayerUse || flags.ActivationType == ActivationType.PlayerUsePassThrough;
                else if (context == ActivationContext.ProjectileHitLine)
                    return flags.ActivationType == ActivationType.ProjectileHitsWall;
                else if (context == ActivationContext.PlayerPushesWall)
                    return flags.ActivationType == ActivationType.PlayerPushesWall;
            }

            return false;
        }

        public bool IsSectorMoveSpecial() => m_moveSpecial;
        public bool IsSectorStopMoveSpecial() => m_sectorStopMoveSpecial;
        public bool IsSectorLightSpecial() => m_lightSpecial;
        public bool IsSectorStopLightSpecial() => m_stopLightSpecial;
        public bool MonsterCanUse() => m_monsterCanUse;

        public bool CanActivateDuringSectorMovement()
        {
            switch (LineSpecialType)
            {
                case ZDoomLineSpecialType.DoorOpenClose:
                case ZDoomLineSpecialType.DoorLockedRaise:
                    return true;
            }

            return false;
        }

        public bool CanPause()
        {
            switch (LineSpecialType)
            {
                case ZDoomLineSpecialType.LiftPerpetual:
                case ZDoomLineSpecialType.PlatPerpetualRaiseLip:
                case ZDoomLineSpecialType.PlatStop:
                case ZDoomLineSpecialType.CeilingCrushRaiseAndLower:
                case ZDoomLineSpecialType.CeilingCrushAndRaiseDist:
                case ZDoomLineSpecialType.CeilingCrushRaiseAlways:
                case ZDoomLineSpecialType.CeilingCrushRaiseSilent:
                case ZDoomLineSpecialType.CeilingCrushStop:
                case ZDoomLineSpecialType.FloorRaiseAndCrushDoom:
                case ZDoomLineSpecialType.FloorRaiseCrush:
                case ZDoomLineSpecialType.FloorCrushStop:
                    return true;
            }

            return false;
        }

        public bool IsExitSpecial()
        {
            switch (LineSpecialType)
            {
                case ZDoomLineSpecialType.ExitNormal:
                case ZDoomLineSpecialType.ExitSecret:
                    return true;
            }

            return false;
        }

        public bool IsTeleport()
        {
            return LineSpecialType == ZDoomLineSpecialType.Teleport;
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
                case ZDoomLineSpecialType.CeilingCrushRaiseStay:
                case ZDoomLineSpecialType.LiftPerpetual:
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

        private bool SetSectorStopSpecial()
        {
            switch (LineSpecialType)
            {
                case ZDoomLineSpecialType.PlatStop:
                case ZDoomLineSpecialType.CeilingCrushStop:
                case ZDoomLineSpecialType.FloorCrushStop:
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

        private bool SetStopLightSpecial()
        {
            return LineSpecialType == ZDoomLineSpecialType.LightStop;
        }

        private bool SetMonsterCanUse()
        {
            switch (LineSpecialType)
            {
                case ZDoomLineSpecialType.DoorOpenClose:
                case ZDoomLineSpecialType.DoorOpenStay:
                case ZDoomLineSpecialType.DoorLockedRaise:
                    return true;
            }

            return false;
        }
    }
}