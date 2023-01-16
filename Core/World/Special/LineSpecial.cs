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

namespace Helion.World.Special;

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
    private readonly bool m_sectorStopMove;
    private readonly bool m_lightSpecial;
    private readonly bool m_sectorTrigger;
    private readonly bool m_floorMove;
    private readonly bool m_ceilingMove;
    private readonly LineActivationType m_lineActivationType;

    public LineSpecial(ZDoomLineSpecialType type) : this(type, LineActivationType.Any, LineSpecialCompatibility.Default)
    {
    }

    public LineSpecial(ZDoomLineSpecialType type, LineActivationType lineActivationType, LineSpecialCompatibility compatibility)
    {
        LineSpecialType = type;
        LineSpecialCompatibility = compatibility;
        m_lineActivationType = lineActivationType;
        m_moveSpecial = SetMoveSpecial();
        m_sectorStopMove = SetSectorStopSpecial();
        m_lightSpecial = SetLightSpecial();
        m_sectorTrigger = SetSectorTriggerSpecial();
        m_floorMove = SetFloorMove();
        m_ceilingMove = SetCeilingMove();
    }

    public static void ValidateActivationFlags(ZDoomLineSpecialType type, ref LineFlags flags)
    {
        switch (type)
        {
            case ZDoomLineSpecialType.ScrollTextureLeft:
            case ZDoomLineSpecialType.ScrollTextureRight:
            case ZDoomLineSpecialType.ScrollTextureUp:
            case ZDoomLineSpecialType.ScrollTextureDown:
            case ZDoomLineSpecialType.ScrollFloor:
            case ZDoomLineSpecialType.ScrollCeiling:
                flags.Activations = LineActivations.LevelStart;
                break;

            case ZDoomLineSpecialType.Teleport:
            case ZDoomLineSpecialType.TeleportNoFog:
                if (flags.Activations == (LineActivations.Player | LineActivations.CrossLine))
                    flags.Activations |= LineActivations.Monster;
                break;
        }
    }

    public static LineSpecialCompatibility GetCompatibility(HexenLine hexenLine)
    {
        if (hexenLine.LineType == ZDoomLineSpecialType.DoorLockedRaise)
        {
            LineSpecialCompatibilityType type = hexenLine.Args.Arg0 == Sector.NoTag ?
                LineSpecialCompatibilityType.KeyDoor : LineSpecialCompatibilityType.KeyObject;

            return new LineSpecialCompatibility() { CompatibilityType = type };
        }

        return LineSpecialCompatibility.Default;
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

        if (entity.Flags.NoTeleport && IsTeleport())
            return false;

        bool contextSuccess = false;
        LineFlags flags = line.Flags;
        if (context == ActivationContext.HitscanCrossLine || context == ActivationContext.HitscanImpactsWall)
        {
            if ((flags.Activations & LineActivations.Hitscan) == 0)
                return false;

            if (context == ActivationContext.HitscanCrossLine)
                contextSuccess = (flags.Activations & LineActivations.CrossLine) != 0;
            else
                contextSuccess = (flags.Activations & LineActivations.ImpactLine) != 0;
        }
        else if (entity.Flags.Missile)
        {
            if ((flags.Activations & LineActivations.Projectile) == 0)
                return false;

            if (context == ActivationContext.EntityImpactsWall)
                contextSuccess = (flags.Activations & LineActivations.ImpactLine) != 0;
            else if (context == ActivationContext.CrossLine)
                contextSuccess = (flags.Activations & LineActivations.CrossLine) != 0;

        }
        else if (!entity.IsPlayer)
        {
            if (line.Flags.Secret || (flags.Activations & LineActivations.Monster) == 0)
                return false;

            if (context == ActivationContext.CrossLine)
                contextSuccess = (flags.Activations & LineActivations.CrossLine) != 0;
            // Based on testing this implementation appears to be goofed. Only works with two-sided lines.
            else if (context == ActivationContext.UseLine && line.Back != null)
                contextSuccess = (flags.Activations & LineActivations.UseLine) != 0;
        }

        if (entity.PlayerObj != null)
        {
            if ((flags.Activations & LineActivations.Player) != 0)
            {
                if (context == ActivationContext.CrossLine)
                    contextSuccess = (flags.Activations & LineActivations.CrossLine) != 0;
                else if (context == ActivationContext.UseLine)
                    contextSuccess = (flags.Activations & LineActivations.UseLine) != 0;
                else if (context == ActivationContext.EntityImpactsWall)
                    contextSuccess = (flags.Activations & LineActivations.ImpactLine) != 0;
            }

            if (contextSuccess && IsLockType(line, out int keyNumber))
            {
                LockDef? lockDef = lockDefinitions.GetLockDef(keyNumber);
                if (lockDef == null || !PlayerCanUnlock(entity.PlayerObj, lockDef))
                {
                    lockFail = lockDef;
                    return false;
                }
            }
        }

        return contextSuccess;
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

    public bool IsSectorMove() => m_moveSpecial;
    public bool IsFloorMove() => m_floorMove;
    public bool IsCeilingMove() => m_ceilingMove;
    public bool IsSectorStopMove() => m_sectorStopMove;
    public bool IsSectorLight() => m_lightSpecial;
    public bool IsSectorStopLight() => LineSpecialType == ZDoomLineSpecialType.LightStop;
    public bool IsSectorFloorTrigger() => m_sectorTrigger;
    public bool IsTransferLight() => LineSpecialType == ZDoomLineSpecialType.TransferFloorLight || LineSpecialType == ZDoomLineSpecialType.TransferCeilingLight;
    public bool IsFloorDonut() => LineSpecialType == ZDoomLineSpecialType.FloorDonut;
    public bool IsStairBuild() =>  LineSpecialType == ZDoomLineSpecialType.StairsBuildUpDoom || LineSpecialType == ZDoomLineSpecialType.StairsBuildUpDoomCrush ||
        LineSpecialType == ZDoomLineSpecialType.StairsGeneric || LineSpecialType == ZDoomLineSpecialType.BuildStairsDown || LineSpecialType == ZDoomLineSpecialType.BuildStairsDownSync ||
        LineSpecialType == ZDoomLineSpecialType.BuildStairsUp || LineSpecialType == ZDoomLineSpecialType.BuildStairsUpSync;
    public bool IsSectorSpecial() => IsSectorMove() || IsSectorLight() || IsSectorStopMove() ||
        IsSectorStopLight() || IsSectorFloorTrigger();

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
            case ZDoomLineSpecialType.GenericCrusher:
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
        switch (LineSpecialType)
        {
            case ZDoomLineSpecialType.Teleport:
            case ZDoomLineSpecialType.TeleportNoFog:
            case ZDoomLineSpecialType.TeleportLine:
                return true;

            default:
                return false;
        }
    }

    public bool IsPlaneScroller()
    {
        switch (LineSpecialType)
        {
            case ZDoomLineSpecialType.ScrollFloor:
            case ZDoomLineSpecialType.ScrollCeiling:
                return true;

            default:
                return false;
        }
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
            case ZDoomLineSpecialType.ElevatorRaiseToNearest:
            case ZDoomLineSpecialType.ElevatorLowerToNearest:
            case ZDoomLineSpecialType.ElevatorMoveToFloor:
                return true;

            default:
                break;
        }

        return false;
    }

    private bool SetFloorMove()
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
            case ZDoomLineSpecialType.LiftPerpetual:
            case ZDoomLineSpecialType.LiftDownWaitUpStay:
            case ZDoomLineSpecialType.LiftDownValueTimes8:
            case ZDoomLineSpecialType.LiftUpWaitDownStay:
            case ZDoomLineSpecialType.PlatUpByValue:
            case ZDoomLineSpecialType.FloorLowerNow:
            case ZDoomLineSpecialType.FloorRaiseNow:
            case ZDoomLineSpecialType.FloorMoveToValueTimes8:
            case ZDoomLineSpecialType.PillarBuildCrush:
            case ZDoomLineSpecialType.FloorAndCeilingLowerByValue:
            case ZDoomLineSpecialType.FloorAndCeilingRaiseByValue:
            case ZDoomLineSpecialType.FloorLowerToHighest:
            case ZDoomLineSpecialType.FloorRaiseToLowestCeiling:
            case ZDoomLineSpecialType.FloorLowerToLowestTxTy:
            case ZDoomLineSpecialType.FloorRaiseToLowest:
            case ZDoomLineSpecialType.FloorRaiseByValueTxTy:
            case ZDoomLineSpecialType.FloorRaiseByTexture:
            case ZDoomLineSpecialType.FloorDonut:
            case ZDoomLineSpecialType.FloorAndCeilingLowerRaise:
            case ZDoomLineSpecialType.PlatPerpetualRaiseLip:
            case ZDoomLineSpecialType.FloorRaiseAndCrushDoom:
            case ZDoomLineSpecialType.StairsBuildUpDoom:
            case ZDoomLineSpecialType.StairsBuildUpDoomCrush:
            case ZDoomLineSpecialType.PlatRaiseAndStay:
            case ZDoomLineSpecialType.GenericFloor:
            case ZDoomLineSpecialType.GenericLift:
            case ZDoomLineSpecialType.StairsGeneric:
            case ZDoomLineSpecialType.PlatUpValueStayTx:
            case ZDoomLineSpecialType.PlatToggleCeiling:
            case ZDoomLineSpecialType.ElevatorRaiseToNearest:
            case ZDoomLineSpecialType.ElevatorLowerToNearest:
            case ZDoomLineSpecialType.ElevatorMoveToFloor:
                return true;

            default:
                break;
        }

        return false;
    }

    private bool SetCeilingMove()
    {
        switch (LineSpecialType)
        {
            case ZDoomLineSpecialType.CeilingLowerByValue:
            case ZDoomLineSpecialType.CeilingRaiseByValue:
            case ZDoomLineSpecialType.CeilingCrushRaiseAndLower:
            case ZDoomLineSpecialType.CeilingCrushStayDown:
            case ZDoomLineSpecialType.CeilingCrushRaiseStay:
            case ZDoomLineSpecialType.CeilingMoveToValueTimes8:
            case ZDoomLineSpecialType.DoorClose:
            case ZDoomLineSpecialType.DoorOpenStay:
            case ZDoomLineSpecialType.DoorOpenClose:
            case ZDoomLineSpecialType.DoorCloseWaitOpen:
            case ZDoomLineSpecialType.FloorAndCeilingLowerRaise:
            case ZDoomLineSpecialType.CeilingRaiseToNearest:
            case ZDoomLineSpecialType.CeilingLowerToLowest:
            case ZDoomLineSpecialType.CeilingLowerToFloor:
            case ZDoomLineSpecialType.CeilingCrushRaiseStaySilent:
            case ZDoomLineSpecialType.DoorLockedRaise:
            case ZDoomLineSpecialType.CeilingCrushAndRaiseDist:
            case ZDoomLineSpecialType.CeilingCrushRaiseSilent:
            case ZDoomLineSpecialType.CeilingRaiseToHighest:
            case ZDoomLineSpecialType.DoorWaitClose:
            case ZDoomLineSpecialType.DoorGeneric:
            case ZDoomLineSpecialType.GenericCeiling:
            case ZDoomLineSpecialType.CeilingLowerToHighestFloor:
            case ZDoomLineSpecialType.ElevatorRaiseToNearest:
            case ZDoomLineSpecialType.ElevatorLowerToNearest:
            case ZDoomLineSpecialType.ElevatorMoveToFloor:
            case ZDoomLineSpecialType.GenericCrusher:
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
