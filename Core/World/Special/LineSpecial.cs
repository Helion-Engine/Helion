using Helion.Maps.Hexen.Components;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Definitions.Locks;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;

namespace Helion.World.Special
{
    /// <summary>
    /// Represents a line special.
    /// </summary>
    public class LineSpecial
    {
        public static LineSpecial Default { get; private set; } = new LineSpecial(ZDoomLineSpecialType.None);

        public const int NoLock = 0;

        public readonly ZDoomLineSpecialType LineSpecialType;
        public readonly LineSpecialCompatibility LineSpecialCompatibility;
        public bool Active { get; set; }
        private readonly bool m_moveSpecial;
        private readonly bool m_sectorStopMoveSpecial;
        private readonly bool m_lightSpecial;
        private readonly bool m_sectorTriggerSpecial;
        private readonly LineActivationType m_lineActivationType;

        public LineSpecial(ZDoomLineSpecialType type) : this(type, LineActivationType.Any, null)
        {
        }

        public LineSpecial(ZDoomLineSpecialType type, LineActivationType lineActivationType, LineSpecialCompatibility? compatibility)
        {
            LineSpecialType = type;

            if (compatibility == null)
                LineSpecialCompatibility = LineSpecialCompatibility.Default;
            else
                LineSpecialCompatibility = compatibility;

            m_lineActivationType = lineActivationType;
            m_moveSpecial = SetMoveSpecial();
            m_sectorStopMoveSpecial = SetSectorStopSpecial();
            m_lightSpecial = SetLightSpecial();
            m_sectorTriggerSpecial = SetSectorTriggerSpecial();
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

                case ZDoomLineSpecialType.Teleport:
                case ZDoomLineSpecialType.TeleportNoFog:
                    if (flags.ActivationType == ActivationType.PlayerLineCross)
                        flags.ActivationType = ActivationType.PlayerOrMonsterLineCross;
                    break;
            }
        }

        public static LineSpecialCompatibility? GetCompatibility(HexenLine hexenLine)
        {
            if (hexenLine.LineType == ZDoomLineSpecialType.DoorLockedRaise)
            {
                LineSpecialCompatibilityType type = hexenLine.Args.Arg0 == Sector.NoTag ?
                    LineSpecialCompatibilityType.KeyDoor : LineSpecialCompatibilityType.KeyObject;

                return new LineSpecialCompatibility() { CompatibilityType = type };
            }

            return null;
        }

        public bool CanActivateByTag => (m_lineActivationType & LineActivationType.Tag) != 0;
        public bool CanActivateByBackSide => (m_lineActivationType & LineActivationType.BackSide) != 0;

        /// <summary>
        /// Returns true if the given entity can activate this special given the activation context.
        /// </summary>
        public bool CanActivate(Entity entity, Line line, ActivationContext context, LockDefinitions lockDefinitions, out LockDef? lockFail)
        {
            lockFail = null;
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
                if (line.Flags.Secret)
                    return false;

                if (context == ActivationContext.CrossLine)
                    return flags.ActivationType == ActivationType.MonsterLineCross || flags.ActivationType == ActivationType.PlayerOrMonsterLineCross;
                else if (context == ActivationContext.UseLine)
                    return flags.ActivationType == ActivationType.PlayerUse && line.TagArg == 0 && !line.Flags.Secret && 
                        line.Special.MonsterCanUse();
            }
            else if (entity is Player player)
            {
                bool contextSuccess = false;

                if (context == ActivationContext.CrossLine)
                    contextSuccess = flags.ActivationType == ActivationType.PlayerLineCross || flags.ActivationType == ActivationType.PlayerOrMonsterLineCross;
                else if (context == ActivationContext.UseLine)
                    contextSuccess = flags.ActivationType == ActivationType.PlayerUse || flags.ActivationType == ActivationType.PlayerUsePassThrough;
                else if (context == ActivationContext.ProjectileHitLine)
                    contextSuccess = flags.ActivationType == ActivationType.ProjectileHitsWall || flags.ActivationType == ActivationType.ProjectileHitsOrCrossesLine;
                else if (context == ActivationContext.PlayerPushesWall)
                    contextSuccess = flags.ActivationType == ActivationType.PlayerPushesWall;

                if (contextSuccess && IsLockType(line, out int keyNumber))
                {
                    LockDef? lockDef = lockDefinitions.GetLockDef(keyNumber);
                    if (lockDef == null || !PlayerCanUnlock(player, lockDef))
                    {
                        lockFail = lockDef;
                        return false;
                    }
                }

                return contextSuccess;
            }

            return false;
        }

        private static bool PlayerCanUnlock(Player player, LockDef lockDef)
        {
            foreach (var definitions in lockDef.AnyKeyDefinitionNames)
            {
                if (!player.Inventory.HasAnyItem(definitions))
                    return false;
            }

            foreach (var key in lockDef.KeyDefinitionNames)
            {
                if (!player.Inventory.HasItem(key))
                    return false;
            }

            return true;
        }

        private static bool IsLockType(Line line, out int keyNumber)
        {
            switch (line.Special.LineSpecialType)
            {
                case ZDoomLineSpecialType.DoorLockedRaise:
                    keyNumber = line.Args.Arg3;
                    return true;

                case ZDoomLineSpecialType.DoorGeneric:
                    keyNumber = line.Args.Arg4;
                    return (ZDoomKeyType)keyNumber != ZDoomKeyType.None;

                default:
                    keyNumber = 0;
                    return false;
            }
        }

        public bool IsSectorMoveSpecial() => m_moveSpecial;
        public bool IsSectorStopMoveSpecial() => m_sectorStopMoveSpecial;
        public bool IsSectorLightSpecial() => m_lightSpecial;
        public bool IsSectorStopLightSpecial() => LineSpecialType == ZDoomLineSpecialType.LightStop;
        public bool MonsterCanUse() => LineSpecialType == ZDoomLineSpecialType.DoorOpenClose;
        public bool IsSectorTriggerSpecial() => m_sectorTriggerSpecial;

        public bool CanActivateDuringSectorMovement()
        {
            switch (LineSpecialType)
            {
                case ZDoomLineSpecialType.DoorOpenClose:
                case ZDoomLineSpecialType.DoorLockedRaise:
                    return true;

                default:
                    break;
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
                case ZDoomLineSpecialType.PlatToggleCeiling:
                    return true;

                default:
                    break;
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

                default:
                    break;
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
                case ZDoomLineSpecialType.FloorAndCeilingLowerRaise:
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
                case ZDoomLineSpecialType.CeilingCrushRaiseSilent:
                case ZDoomLineSpecialType.PlatRaiseAndStay:
                case ZDoomLineSpecialType.CeilingRaiseToHighest:
                case ZDoomLineSpecialType.DoorWaitClose:
                case ZDoomLineSpecialType.DoorGeneric:
                case ZDoomLineSpecialType.GenericCeiling:
                case ZDoomLineSpecialType.GenericFloor:
                case ZDoomLineSpecialType.GenericLift:
                case ZDoomLineSpecialType.GenericCrusher:
                case ZDoomLineSpecialType.StairsGeneric:
                case ZDoomLineSpecialType.PlatUpValueStayTx:
                case ZDoomLineSpecialType.CeilingLowerToHighestFloor:
                case ZDoomLineSpecialType.PlatToggleCeiling:
                    return true;

                default:
                    break;
            }

            return false;
        }

        private bool SetSectorTriggerSpecial()
        {
            switch (LineSpecialType)
            {
                case ZDoomLineSpecialType.FloorTransferNumeric:
                case ZDoomLineSpecialType.FloorTransferTrigger:
                    return true;

                default:
                    break;
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

                default:
                    break;
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
                case ZDoomLineSpecialType.TransferFloorLight:
                case ZDoomLineSpecialType.TransferCeilingLight:
                    return true;

                default:
                    break;
            }

            return false;
        }
    }
}