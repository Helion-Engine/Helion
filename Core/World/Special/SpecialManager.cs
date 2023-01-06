using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.Resources;
using Helion.Resources.Definitions.Decorate.Properties;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Container;
using Helion.Util.RandomGenerators;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.States;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
using Helion.World.Special.Switches;
using Helion.World.Stats;

namespace Helion.World.Special;

public class SpecialManager : ITickable, IDisposable
{ 
    // Doom used speeds 1/8 of map unit, Helion uses map units so doom speeds have to be multiplied by 1/8
    public const double SpeedFactor = 0.125;
    public const double VisualScrollFactor = 0.015625;

    private readonly LinkedList<ISpecial> m_specials = new();
    private readonly List<ISectorSpecial> m_destroyedMoveSpecials = new();
    private readonly IRandom m_random;
    private readonly WorldBase m_world;
    private TextureManager TextureManager => m_world.ArchiveCollection.TextureManager;

    public static SectorSoundData GetDoorSound(double speed, bool reverse = false)
    {
        // This speed is already translated into map units - 64 * 0.125 = 8
        if (speed >= 8)
            return reverse ? DoorFastSoundReverse : DoorFastSound;
        else
            return reverse ? DoorSlowSoundReverse : DoorSlowSound;
    }

    private static readonly SectorSoundData DoorFastSound = new(Constants.DoorOpenFastSound, Constants.DoorCloseFastSound, null);
    private static readonly SectorSoundData DoorFastSoundReverse = new(Constants.DoorCloseFastSound, Constants.DoorOpenFastSound, null);
    private static readonly SectorSoundData DoorSlowSound = new(Constants.DoorOpenSlowSound, Constants.DoorCloseSlowSound, null);
    private static readonly SectorSoundData DoorSlowSoundReverse = new(Constants.DoorCloseSlowSound, Constants.DoorOpenSlowSound, null);
    private static readonly SectorSoundData DefaultFloorSound = new(null, null, Constants.PlatStopSound, Constants.PlatMoveSound);
    private static readonly SectorSoundData DefaultCeilingSound = new(null, null, null, Constants.PlatMoveSound);
    private static readonly SectorSoundData LiftSound = new(Constants.PlatStartSound, Constants.PlatStartSound, Constants.PlatStopSound);
    private static readonly SectorSoundData PlatSound = new(null, Constants.PlatStartSound, Constants.PlatStopSound, Constants.PlatMoveSound);
    private static readonly SectorSoundData CrusherSoundNoRepeat = new(null, null, Constants.PlatStopSound, Constants.PlatMoveSound);
    private static readonly SectorSoundData CrusherSoundRepeat = new(null, null, null, Constants.PlatMoveSound);
    private static readonly SectorSoundData SilentCrusherSound = new(null, null, Constants.PlatStopSound);
    private static readonly SectorSoundData NoSound = new();
    private const int DefaultCrushLip = 8;
    private const double CrushReturnFactor = 0.5;

    public LinkedList<ISpecial> GetSpecials() => m_specials;

    public EventHandler<ISectorSpecial>? SectorSpecialDestroyed;

    public SpecialManager(WorldBase world, IRandom random)
    {
        m_world = world;
        m_random = random;
    }

    public void Dispose()
    {
        m_specials.Clear();
        m_destroyedMoveSpecials.Clear();
        GC.SuppressFinalize(this);
    }

    public List<ISpecialModel> GetSpecialModels()
    {
        List<ISpecialModel> specials = new();
        foreach (var special in m_specials)
        {
            ISpecialModel? specialModel = special.ToSpecialModel();
            if (specialModel != null)
                specials.Add(specialModel);
        }

        return specials;
    }

    public void ResetInterpolation()
    {
        foreach (ISpecial special in m_specials)
            special.ResetInterpolation();

        for (int i = 0; i < m_destroyedMoveSpecials.Count; i++)
        {
            ISectorSpecial sectorSpecial = m_destroyedMoveSpecials[i];
            sectorSpecial.ResetInterpolation();
            SectorSpecialDestroyed?.Invoke(this, sectorSpecial);
        }
    }

    public bool TryAddActivatedLineSpecial(in EntityActivateSpecial args)
    {
        if (args.ActivateLineSpecial == null || (args.ActivateLineSpecial.Activated && !args.ActivateLineSpecial.Flags.Repeat))
            return false;

        var special = args.ActivateLineSpecial.Special;
        bool specialActivateSuccess;

        if (special.IsSectorSpecial())
            specialActivateSuccess = HandleSectorLineSpecial(args, special);
        else
            specialActivateSuccess = HandleDefault(args, special, m_world);

        if (specialActivateSuccess)
        {
            if (ShouldCreateSwitchSpecial(args))
            {
                var switchSpecial = GetExistingSwitchSpecial(args.ActivateLineSpecial);
                if (switchSpecial != null)
                    RemoveSpecial(switchSpecial);
                switchSpecial = new SwitchChangeSpecial(m_world, args.ActivateLineSpecial, GetSwitchType(args.ActivateLineSpecial.Special));
                if (switchSpecial.Tick() != SpecialTickStatus.Destroy)
                    AddSpecial(switchSpecial);
            }

            args.ActivateLineSpecial.SetActivated(true);
        }

        return specialActivateSuccess;
    }

    private SwitchChangeSpecial? GetExistingSwitchSpecial(Line line)
    {
        var node = m_specials.First;
        while (node != null)
        {
            if (node.Value is SwitchChangeSpecial switchChangeSpecial && switchChangeSpecial.Line.Id == line.Id)
            {
                switchChangeSpecial.ResetDelay();
                return switchChangeSpecial;
            }

            node = node.Next;
        }

        return null;
    }

    private bool ShouldCreateSwitchSpecial(in EntityActivateSpecial args)
    {
        if (args.ActivationContext == ActivationContext.CrossLine)
            return false;

        return SwitchManager.IsLineSwitch(m_world.ArchiveCollection, args.ActivateLineSpecial);
    }

    private static SwitchType GetSwitchType(LineSpecial lineSpecial)
    {
        if (lineSpecial.IsExitSpecial())
            return SwitchType.Exit;
        return SwitchType.Default;
    }

    public ISpecial CreateFloorRaiseSpecialMatchTextureAndType(Sector sector, Line line, double amount, double speed)
    {
        TriggerSpecials.PlaneTransferChange(m_world, sector, line, SectorPlaneFace.Floor, PlaneTransferType.Trigger);
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, sector.Floor.Z + amount,
            new SectorMoveData(SectorPlaneFace.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0), DefaultFloorSound);
    }

    public ISpecial CreateFloorRaiseSpecialMatchTexture(Sector sector, Line line, double amount, double speed)
    {
        TriggerSpecials.PlaneTransferChange(m_world, sector, line, SectorPlaneFace.Floor, PlaneTransferType.Trigger, transferSpecial: false);
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, sector.Floor.Z + amount,
            new SectorMoveData(SectorPlaneFace.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0), DefaultFloorSound);
    }

    public ISpecial CreateFloorRaiseByTextureSpecial(Sector sector, double speed)
    {
        double destZ = sector.Floor.Z + sector.GetShortestTexture(TextureManager, true, m_world.Config.Compatibility);
        SectorMoveData moveData = new SectorMoveData(SectorPlaneFace.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0);
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, moveData, DefaultFloorSound);
    }

    public void Tick()
    {
        if (m_destroyedMoveSpecials.Count > 0)
            TickDestroyedMoveSepcials();

        if (m_world.WorldState == WorldState.Exit)
        {
            var node = m_specials.First;
            while (node != null)
            {
                if (node.Value is SwitchChangeSpecial && node.Value.Tick() == SpecialTickStatus.Destroy)
                    m_specials.Remove(node);

                node = node.Next;
            }
        }
        else
        {
            LinkedListNode<ISpecial>? node = m_specials.First;
            LinkedListNode<ISpecial>? nextNode;
            while (node != null)
            {
                nextNode = node.Next;
                if (node.Value.Tick() == SpecialTickStatus.Destroy)
                {
                    m_specials.Remove(node);
                    if (node.Value is ISectorSpecial sectorSpecial)
                        m_destroyedMoveSpecials.Add(sectorSpecial);

                    node.Value.Destroy();
                }

                node = nextNode;
            }
        }
    }

    private void TickDestroyedMoveSepcials()
    {
        for (int i = 0; i < m_destroyedMoveSpecials.Count; i++)
        {
            ISectorSpecial sectorSpecial = m_destroyedMoveSpecials[i];
            sectorSpecial.FinalizeDestroy();
        }

        // Only invoke after all specials have been destroyed on this tick. Otherwise interpolation values can beo
        for (int i = 0; i < m_destroyedMoveSpecials.Count; i++)
        {
            ISectorSpecial sectorSpecial = m_destroyedMoveSpecials[i];
            if (sectorSpecial is not SectorMoveSpecial moveSpecial)
                continue;

            if (!moveSpecial.MultiSector)
            {
                SectorSpecialDestroyed?.Invoke(this, moveSpecial);
                continue;
            }

            foreach ((Sector sector, SectorPlane plane) in moveSpecial.GetSectors())
            {
                moveSpecial.Sector = sector;
                moveSpecial.SectorPlane = plane;
                SectorSpecialDestroyed?.Invoke(this, moveSpecial);
            }
        }

        m_destroyedMoveSpecials.Clear();
    }

    public ISpecial AddDelayedSpecial(SectorMoveSpecial special, int delayTics)
    {
        special.SetDelayTics(delayTics);
        m_specials.AddLast(special);
        return special;
    }

    public void AddSpecial(ISpecial special)
    {
        m_specials.AddLast(special);
    }

    public ISpecial? FindSpecialBySector(Sector sector)
    {
        var node = m_specials.First;
        while (node != null)
        {
            if (node.Value is SectorSpecialBase sectorSpecial && sectorSpecial.Sector.Id == sector.Id)
                return sectorSpecial;
            else if (node.Value is SectorMoveSpecial moveSpecial && moveSpecial.Sector.Id == sector.Id)
                return moveSpecial;

            node = node.Next;
        }

        return null;
    }

    public ScrollSpecial? FindLineScrollSpecial(Line line)
    {
        var node = m_specials.First;
        while (node != null)
        {
            if (node.Value is ScrollSpecial scrollSpecial && scrollSpecial.Line != null && scrollSpecial.Line.Id == line.Id)
                return scrollSpecial;

            node = node.Next;
        }

        return null;
    }

    public bool RemoveSpecial(ISpecial special)
    {
        if (!m_specials.Remove(special))
            return false;

        if (special is ISectorSpecial sectorSpecial)
        {
            m_destroyedMoveSpecials.Add(sectorSpecial);
            sectorSpecial.Sector.ClearActiveMoveSpecial();
        }

        return true;
    }

    public void AddSpecialModels(IList<ISpecialModel> specialModels)
    {
        for (int i = 0; i < specialModels.Count; i++)
        {
            ISpecial? special = specialModels[i].ToWorldSpecial(m_world);
            if (special != null)
                m_specials.AddLast(special);
        }
    }

    public ISpecial CreateLiftSpecial(Sector sector, double speed, int delay, SectorDest dest = SectorDest.LowestAdjacentFloor)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Floor, dest);
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneFace.Floor,
            MoveDirection.Down, MoveRepetition.DelayReturn, speed, delay), LiftSound);
    }

    public SectorMoveSpecial CreateDoorOpenCloseSpecial(Sector sector, double speed, int delay)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Ceiling, SectorDest.LowestAdjacentCeiling) - VanillaConstants.DoorDestOffset;
        return new DoorOpenCloseSpecial(m_world, sector, destZ, speed, delay);
    }

    public ISpecial CreateDoorCloseOpenSpecial(Sector sector, double speed, int delay)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Ceiling, SectorDest.Floor);
        return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneFace.Ceiling,
            MoveDirection.Down, delay > 0 ? MoveRepetition.DelayReturn : MoveRepetition.None, speed, delay, flags: SectorMoveFlags.Door), GetDoorSound(speed, true));
    }

    public ISpecial CreateDoorLockedSpecial(Sector sector, double speed, int delay, int key)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Ceiling, SectorDest.NextHighestCeiling) - VanillaConstants.DoorDestOffset;
        return new DoorOpenCloseSpecial(m_world, sector, destZ, speed, delay, key);
    }

    public SectorMoveSpecial CreateDoorOpenStaySpecial(Sector sector, double speed)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Ceiling, SectorDest.LowestAdjacentCeiling) - VanillaConstants.DoorDestOffset;
        return new DoorOpenCloseSpecial(m_world, sector, destZ, speed, 0);
    }

    public SectorMoveSpecial CreateDoorCloseSpecial(Sector sector, double speed)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Ceiling, SectorDest.Floor);
        return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneFace.Ceiling,
            MoveDirection.Down, MoveRepetition.None, speed, 0, flags: SectorMoveFlags.Door), GetDoorSound(speed, true));
    }

    public ISpecial CreateFloorLowerSpecial(Sector sector, SectorDest sectorDest, double speed, int adjust = 0, LineSpecialCompatibility? compat = null)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Floor, sectorDest);
        // Oof, ZDoom
        if (compat?.IsVanilla == false)
            destZ += adjust - 128;
        else if (adjust != 0)
            destZ = destZ + adjust - 128;

        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneFace.Floor,
            MoveDirection.Down, MoveRepetition.None, speed, 0), DefaultFloorSound);
    }

    public ISpecial CreateFloorLowerSpecial(Sector sector, double amount, double speed)
    {
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, sector.Floor.Z - amount, new SectorMoveData(SectorPlaneFace.Floor,
            MoveDirection.Down, MoveRepetition.None, speed, 0), DefaultFloorSound);
    }

    public ISpecial CreateFloorLowerSpecialChangeTextureAndType(Sector sector, SectorDest sectorDest, double speed)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Floor, sectorDest);
        TriggerSpecials.GetNumericModelChange(m_world, sector, SectorPlaneFace.Floor, destZ,
            out int floorChangeTexture, out SectorDamageSpecial? damageSpecial);

        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneFace.Floor,
            MoveDirection.Down, MoveRepetition.None, speed, 0, floorChangeTextureHandle: floorChangeTexture,
            damageSpecial: damageSpecial),
            DefaultFloorSound);
    }

    public ISpecial CreatePlaneSpecial(Sector sector, SectorPlaneFace planeType, Line line, MoveDirection start, SectorDest sectorDest,
        double amount, double speed, ZDoomGenericFlags flags)
    {
        double startZ = planeType == SectorPlaneFace.Floor ? sector.Floor.Z : sector.Ceiling.Z;
        double destZ;
        if (sectorDest == SectorDest.None)
            destZ = startZ + amount;
        else
            destZ = GetDestZ(sector, planeType, sectorDest, start);

        // Ugh... why
        if (start == MoveDirection.Down && sectorDest == SectorDest.HighestAdjacentFloor)
            destZ -= amount;

        int? changeTexture = null;
        SectorDamageSpecial? damageSpecial = null;
        CrushData? crush = null;

        if ((flags & ZDoomGenericFlags.CopyTxAndSpecial) != 0)
        {
            if ((flags & ZDoomGenericFlags.TriggerNumericModel) != 0)
            {
                if (TriggerSpecials.GetNumericModelChange(m_world, sector, planeType, destZ,
                    out int numericChangeTexture, out SectorDamageSpecial? changeSpecial))
                {
                    changeTexture = numericChangeTexture;
                    damageSpecial = changeSpecial;
                }
            }
            else
            {
                changeTexture = line.Front.Sector.GetTexture(planeType);
                damageSpecial = line.Front.Sector.SectorDamageSpecial;
            }

            ZDoomGenericFlags changeFlags = flags & ZDoomGenericFlags.CopyTxAndSpecial;
            if (changeFlags == ZDoomGenericFlags.CopyTxRemoveSpecial || damageSpecial == null)
                damageSpecial = SectorDamageSpecial.CreateNoDamage(m_world, sector);
            else if (changeFlags == ZDoomGenericFlags.CopyTx)
                damageSpecial = null;
        }

        if ((flags & ZDoomGenericFlags.Crush) != 0)
            crush = CrushData.Default;

        int? floorChangeTexture = null;
        int? ceilingChangeTexture = null;
        if (planeType == SectorPlaneFace.Floor)
            floorChangeTexture = changeTexture;
        else
            ceilingChangeTexture = changeTexture;

        return new SectorMoveSpecial(m_world, sector, startZ, destZ, new SectorMoveData(planeType,
            start, MoveRepetition.None, speed, 0, crush: crush,
            floorChangeTextureHandle: floorChangeTexture,
            ceilingChangeTextureHandle: ceilingChangeTexture,
            damageSpecial: damageSpecial),
            planeType == SectorPlaneFace.Floor ? DefaultFloorSound : DefaultCeilingSound);
    }

    public ISpecial CreateFloorRaiseSpecial(Sector sector, SectorDest sectorDest, double speed)
    {
        // There is a single type that raises to lowest adjacent ceiling
        // Need to include this sector's height in the check so the floor doesn't run through the ceiling
        double destZ = GetDestZ(sector, SectorPlaneFace.Floor, sectorDest);
        //double destZ = GetDestZ(sector, SectorPlaneFace.Floor, sectorDest, sectorDest == SectorDest.LowestAdjacentCeiling);
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneFace.Floor,
            MoveDirection.Up, MoveRepetition.None, speed, 0), DefaultFloorSound);
    }

    public ISpecial CreateFloorRaiseSpecial(Sector sector, double amount, double speed, int? floorChangeTexture = null, bool clearDamage = false)
    {
        SectorMoveFlags flags = clearDamage ? SectorMoveFlags.ClearDamage : SectorMoveFlags.None;
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, sector.Floor.Z + amount, new SectorMoveData(SectorPlaneFace.Floor,
            MoveDirection.Up, MoveRepetition.None, speed, 0, floorChangeTextureHandle: floorChangeTexture, flags: flags), DefaultFloorSound);
    }

    public ISpecial CreateCeilingLowerSpecial(Sector sector, SectorDest sectorDest, double speed)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Ceiling, sectorDest);
        return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneFace.Ceiling,
            MoveDirection.Down, MoveRepetition.None, speed, 0), DefaultCeilingSound);
    }

    public ISpecial CreateCeilingLowerSpecial(Sector sector, int amount, double speed)
    {
        return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, sector.Ceiling.Z - amount, new SectorMoveData(SectorPlaneFace.Ceiling,
            MoveDirection.Down, MoveRepetition.None, speed, 0), DefaultCeilingSound);
    }

    public ISpecial CreateCeilingRaiseSpecial(Sector sector, SectorDest sectorDest, double speed)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Ceiling, sectorDest);
        return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneFace.Ceiling,
            MoveDirection.Up, MoveRepetition.None, speed, 0), DefaultCeilingSound);
    }

    public ISpecial CreateCeilingRaiseSpecial(Sector sector, int amount, double speed)
    {
        return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, sector.Ceiling.Z + amount, new SectorMoveData(SectorPlaneFace.Ceiling,
            MoveDirection.Up, MoveRepetition.None, speed, 0), DefaultCeilingSound);
    }

    public ISpecial CreatePerpetualMovingFloorSpecial(Sector sector, double speed, int delay, int lip)
    {
        double lowZ = GetDestZ(sector, SectorPlaneFace.Floor, SectorDest.LowestAdjacentFloor);
        double highZ = GetDestZ(sector, SectorPlaneFace.Floor, SectorDest.HighestAdjacentFloor);
        if (lowZ > sector.Floor.Z)
            lowZ = sector.Floor.Z;
        if (highZ < sector.Floor.Z)
            highZ = sector.Floor.Z;

        lowZ += lip;
        if (lowZ > highZ)
            lowZ = highZ;

        int value = m_world.Random.NextByte() & 1;
        MoveDirection dir = value == 0 ? MoveDirection.Up : MoveDirection.Down;
        double startZ, destZ;
        if (dir == MoveDirection.Down)
        {
            destZ = lowZ;
            startZ = highZ;
        }
        else
        {
            destZ = highZ;
            startZ = lowZ;
        }

        return new SectorMoveSpecial(m_world, sector, startZ, destZ, new SectorMoveData(SectorPlaneFace.Floor,
            dir, MoveRepetition.Perpetual, speed, delay), LiftSound);
    }

    public ISpecial CreateSectorMoveSpecial(Sector sector, SectorPlane plane, SectorPlaneFace moveType, double speed, double destZ, int negative)
    {
        if (negative > 0)
            destZ = -destZ;

        MoveDirection dir = destZ > plane.Z ? MoveDirection.Up : MoveDirection.Down;
        return new SectorMoveSpecial(m_world, sector, plane.Z, destZ, new SectorMoveData(moveType,
            dir, MoveRepetition.None, speed, 0), DefaultFloorSound);
    }

    public ISpecial CreateStairSpecial(Sector sector, double speed, int height, int delay, bool crush)
    {
        return new StairSpecial(m_world, sector, speed, height, delay, crush);
    }

    public void StartInitSpecials(LevelStats levelStats)
    {
        foreach (var line in m_world.Lines)
        {
            if (line.Special != null && (line.Flags.Activations & LineActivations.LevelStart) != 0)
                HandleLineInitSpecial(line);
        }

        for (int i = 0; i < m_world.Sectors.Count; i++)
        {
            Sector sector = m_world.Sectors[i];
            if (sector.Secret)
                levelStats.TotalSecrets++;
            HandleSectorSpecial(sector);
        }
    }

    private void HandleLineInitSpecial(Line line)
    {
        switch (line.Special.LineSpecialType)
        {
            case ZDoomLineSpecialType.ScrollTextureLeft:
                AddSpecial(new ScrollSpecial(m_world, line, new Vec2D(line.Args.Arg0 * VisualScrollFactor, 0.0), (ZDoomLineScroll)line.Args.Arg1));
                break;
            case ZDoomLineSpecialType.ScrollTextureRight:
                AddSpecial(new ScrollSpecial(m_world, line, new Vec2D(line.Args.Arg0 * -VisualScrollFactor, 0.0), (ZDoomLineScroll)line.Args.Arg1));
                break;
            case ZDoomLineSpecialType.ScrollTextureUp:
                AddSpecial(new ScrollSpecial(m_world, line, new Vec2D(0.0, line.Args.Arg0 * VisualScrollFactor), (ZDoomLineScroll)line.Args.Arg1));
                break;
            case ZDoomLineSpecialType.ScrollTextureDown:
                AddSpecial(new ScrollSpecial(m_world, line, new Vec2D(0.0, line.Args.Arg0 * -VisualScrollFactor), (ZDoomLineScroll)line.Args.Arg1));
                break;
            case ZDoomLineSpecialType.ScrollUsingTextureOffsets:
                AddSpecial(new ScrollSpecial(m_world, line, new Vec2D(-line.Front.Offset.X, line.Front.Offset.Y), ZDoomLineScroll.All));
                break;
            case ZDoomLineSpecialType.ScrollTextureModel:
                CreateScrollTextureModel(line);
                break;
            case ZDoomLineSpecialType.TransferFloorLight:
                SetFloorLight(line);
                break;
            case ZDoomLineSpecialType.TransferCeilingLight:
                SetCeilingLight(line);
                break;
            case ZDoomLineSpecialType.ScrollFloor:
                CreateScrollPlane(line, SectorPlaneFace.Floor);
                break;
            case ZDoomLineSpecialType.ScrollCeiling:
                CreateScrollPlane(line, SectorPlaneFace.Ceiling);
                break;
            case ZDoomLineSpecialType.SectorSetWind:
                CreatePushSpecial(PushType.Wind, line);
                break;
            case ZDoomLineSpecialType.SectorSetCurrent:
                CreatePushSpecial(PushType.Current, line);
                break;
            case ZDoomLineSpecialType.PointPushSetForce:
                CreatePushSpecial(PushType.Push, line);
                break;
            case ZDoomLineSpecialType.SectorSetFriction:
                SetSectorFriction(line);
                break;
            case ZDoomLineSpecialType.TranslucentLine:
                SetTranslucentLine(line, line.Args.Arg0, line.Args.Arg1);
                break;
            case ZDoomLineSpecialType.StaticInit:
                SetStaticInit(line);
                break;
            case ZDoomLineSpecialType.TransferHeights:
                SetTransferHeights(line);
                break;
        }
    }

    private void SetTransferHeights(Line line)
    {
        IEnumerable<Sector> sectors = GetSectorsFromSpecialLine(line);
        foreach (var sector in sectors)
            sector.SetTransferHeights(line.Front.Sector);
    }

    // Constants and logic from WinMBF.
    // Credit to Lee Killough et al.
    private void SetSectorFriction(Line line)
    {
        IEnumerable<Sector> sectors = GetSectorsFromSpecialLine(line);
        foreach (var sector in sectors)
        {
            double length = line.GetLength();
            sector.Friction = Math.Clamp((0x1EB8 * length / 0x80 + 0xD000) / 65536.0, 0.0, 1.0);           
        }
    }

    private void CreatePushSpecial(PushType type, Line line)
    {
        IEnumerable<Sector> sectors = GetSectorsFromSpecialLine(line);
        foreach (var sector in sectors)
        {
            Entity? pusher = null;
            if (type == PushType.Push)
                pusher = GetPusher(sector);

            AddSpecial(new PushSpecial(type, m_world, sector, GetPushFactor(line), pusher));
        }
    }

    private static Entity? GetPusher(Sector sector)
    {
        LinkableNode<Entity>? node = sector.Entities.Head;
        while (node != null)
        {
            Entity entity = node.Value;
            if (entity.Definition.EditorId == (int)EditorId.PointPusher || entity.Definition.EditorId == (int)EditorId.PointPuller)
                return entity;
            node = node.Next;
        }

        return null;
    }

    private static Vec2D GetPushFactor(Line line)
    {
        if (line.Args.Arg3 != 0)
            return line.EndPosition - line.StartPosition;
        
        // Arg2 = angle, Arg1 = amount
        return Vec2D.UnitCircle(line.Args.Arg2) * line.Args.Arg1;
    }

    private void SetStaticInit(Line line)
    {
        if (line.Front.Upper.TextureHandle == Constants.NoTextureIndex)
            return;

        if (line.Args.Arg1 == (int)ZDoomStaticInit.Sky)
        {
            foreach (Sector sector in m_world.FindBySectorTag(line.Args.Arg0))
                sector.SetSkyTexture(line.Front.Upper.TextureHandle, line.Args.Arg2 != 0, m_world.Gametick);
        }
    }

    private void SetTranslucentLine(Line line, int lineId, int translucency)
    {
        float alpha = translucency / 255.0f;
        if (lineId == Line.NoLineId)
        {
            line.SetAlpha(alpha);
        }
        else
        {
            IEnumerable<Line> lines = m_world.FindByLineId(lineId);
            foreach (Line setLine in lines)
                setLine.SetAlpha(alpha);
        }
    }

    private void CreateScrollTextureModel(Line setLine)
    {
        IEnumerable<Line> lines = m_world.FindByLineId(setLine.Args.Arg0);
        ZDoomScroll flags = (ZDoomScroll)setLine.Args.Arg1;

        ScrollSpeeds speeds = GetScrollSpeedsFromLine(setLine, flags);
        if (!speeds.ScrollSpeed.HasValue)
            return;

        Sector? changeScroll = null;
        if ((flags & ZDoomScroll.Accelerative) != 0 || (flags & ZDoomScroll.Displacement) != 0)
            changeScroll = setLine.Front.Sector;

        Vec2D speed = speeds.ScrollSpeed.Value;
        foreach (Line line in lines)
        {
            if (line.Id == setLine.Id)
                continue;

            if (flags == ZDoomScroll.None)
            {
                speeds = ScrollUtil.GetScrollLineSpeed(setLine, line);
                if (!speeds.ScrollSpeed.HasValue)
                    continue;
                speed = speeds.ScrollSpeed.Value;
            }

            AddSpecial(new ScrollSpecial(m_world, line, speed, ZDoomLineScroll.All, accelSector: changeScroll, scrollFlags: flags));
        }
    }

    private static ScrollSpeeds GetScrollSpeedsFromLine(Line setLine, ZDoomScroll flags)
    {
        if (flags == ZDoomScroll.None)
            return new ScrollSpeeds() { ScrollSpeed = Vec2D.Zero };
        else if ((flags & ZDoomScroll.OffsetSpeed) != 0)
            return new ScrollSpeeds() { ScrollSpeed = new(-setLine.Front.Offset.X / 8.0, setLine.Front.Offset.Y / 8.0) };
        
        return ScrollUtil.GetScrollLineSpeed(setLine, flags | ZDoomScroll.Line, ZDoomPlaneScrollType.Scroll);
    }

    private void CreateScrollPlane(Line line, SectorPlaneFace planeType)
    {
        IEnumerable<Sector> sectors = GetSectorsFromSpecialLine(line);
        ZDoomScroll flags = (ZDoomScroll)line.Args.Arg1;
        ZDoomPlaneScrollType scrollType = ZDoomPlaneScrollType.Scroll;
        if (planeType == SectorPlaneFace.Floor)
            scrollType = (ZDoomPlaneScrollType)line.Args.Arg2;

        ScrollSpeeds speeds = ScrollUtil.GetScrollLineSpeed(line, flags, scrollType, VisualScrollFactor);
        Sector? changeScroll = null;

        if ((flags & ZDoomScroll.Accelerative) != 0 || (flags & ZDoomScroll.Displacement) != 0)
            changeScroll = line.Front.Sector;

        foreach (Sector sector in sectors)
        {
            SectorPlane sectorPlane = sector.GetSectorPlane(planeType);
            if (speeds.ScrollSpeed.HasValue)
            {
                Vec2D scrollSpeed = speeds.ScrollSpeed.Value;
                scrollSpeed.X = -scrollSpeed.X;
                AddSpecial(new ScrollSpecial(ScrollType.Scroll, sectorPlane, scrollSpeed, changeScroll, flags));
            }

            if (speeds.CarrySpeed.HasValue)
                AddSpecial(new ScrollSpecial(ScrollType.Carry, sectorPlane, speeds.CarrySpeed.Value, changeScroll, flags));
        }
    }

    private void HandleSectorSpecial(Sector sector)
    {
        switch (sector.SectorSpecialType)
        {
            case ZDoomSectorSpecialType.LightFlickerDoom:
                AddSpecial(new LightFlickerDoomSpecial(m_world, sector, m_random, sector.GetMinLightLevelNeighbor()));
                break;

            case ZDoomSectorSpecialType.LightStrobeFastDoom:
                AddSpecial(new LightStrobeSpecial(m_world, sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.FastDarkTime, false));
                break;

            case ZDoomSectorSpecialType.LightStrobeSlowDoom:
                AddSpecial(new LightStrobeSpecial(m_world, sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.SlowDarkTime, false));
                break;

            case ZDoomSectorSpecialType.LightStrobeHurtDoom:
                AddSpecial(new LightStrobeSpecial(m_world, sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.SlowDarkTime, false));
                break;

            case ZDoomSectorSpecialType.LightGlow:
                AddSpecial(new LightPulsateSpecial(m_world, sector, sector.GetMinLightLevelNeighbor()));
                break;

            case ZDoomSectorSpecialType.LightStrobeSlowSync:
                AddSpecial(new LightStrobeSpecial(m_world, sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.SlowDarkTime, true));
                break;

            case ZDoomSectorSpecialType.LightStrobeFastSync:
                AddSpecial(new LightStrobeSpecial(m_world, sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.FastDarkTime, true));
                break;

            case ZDoomSectorSpecialType.SectorDoorClose30Seconds:
                AddDelayedSpecial(CreateDoorCloseSpecial(sector, VanillaConstants.DoorSlowSpeed * SpeedFactor), 35 * 30);
                break;

            case ZDoomSectorSpecialType.DoorRaiseIn5Minutes:
                AddDelayedSpecial(CreateDoorOpenCloseSpecial(sector, VanillaConstants.DoorSlowSpeed * SpeedFactor, VanillaConstants.DoorDelay), 35 * 60 * 5);
                break;

            case ZDoomSectorSpecialType.LightFireFlicker:
                AddSpecial(new LightFireFlickerDoom(m_world, sector, m_random, sector.GetMinLightLevelNeighbor()));
                break;
        }

        switch (sector.SectorSpecialType)
        {
            case ZDoomSectorSpecialType.DamageNukage:
            case ZDoomSectorSpecialType.DamageHellslime:
                sector.SectorDamageSpecial = new SectorDamageSpecial(m_world, sector, GetDamageAmount(sector.SectorSpecialType));
                break;
            case ZDoomSectorSpecialType.LightStrobeHurtDoom:
            case ZDoomSectorSpecialType.DamageSuperHell:
                sector.SectorDamageSpecial = new SectorDamageSpecial(m_world, sector, GetDamageAmount(sector.SectorSpecialType), 5);
                break;
            case ZDoomSectorSpecialType.DamageEnd:
                sector.SectorDamageSpecial = new SectorDamageEndSpecial(m_world, sector, GetDamageAmount(sector.SectorSpecialType));
                break;
        }

        if (sector.DamageAmount > 0)
            sector.SectorDamageSpecial = new SectorDamageSpecial(m_world, sector, sector.DamageAmount);
    }

    private static int GetDamageAmount(ZDoomSectorSpecialType type)
    {
        switch (type)
        {
            case ZDoomSectorSpecialType.DamageNukage:
                return 5;
            case ZDoomSectorSpecialType.DamageHellslime:
                return 10;
            case ZDoomSectorSpecialType.LightStrobeHurtDoom:
            case ZDoomSectorSpecialType.DamageEnd:
            case ZDoomSectorSpecialType.DamageSuperHell:
                return 20;
        }

        return 0;
    }

    private bool HandleDefault(in EntityActivateSpecial args, LineSpecial special, WorldBase world)
    {
        Line line = args.ActivateLineSpecial;

        switch (special.LineSpecialType)
        {
            case ZDoomLineSpecialType.Teleport:
                return new TeleportSpecial(args, world, line.Args.Arg0, line.Args.Arg1, TeleportSpecial.GetTeleportFog(args.ActivateLineSpecial)).Teleport();

            case ZDoomLineSpecialType.TeleportNoFog:
                return new TeleportSpecial(args, world, line.Args.Arg0, line.Args.Arg2, TeleportSpecial.GetTeleportFog(args.ActivateLineSpecial),
                    (TeleportType)line.Args.Arg1).Teleport();

            case ZDoomLineSpecialType.TeleportLine:
                return new TeleportSpecial(args, world, line.Args.Arg1, TeleportFog.None, TeleportType.BoomFixed, line.Args.Arg2 != 0).Teleport();

            case ZDoomLineSpecialType.ExitNormal:
                m_world.ExitLevel(LevelChangeType.Next);
                return true;

            case ZDoomLineSpecialType.ExitSecret:
                m_world.ExitLevel(LevelChangeType.SecretNext);
                return true;
        }

        return false;
    }

    private bool HandleSectorLineSpecial(in EntityActivateSpecial args, LineSpecial special)
    {
        bool success = false;

        IEnumerable<Sector> sectors = GetSectorsFromSpecialLine(args.ActivateLineSpecial);
        var lineSpecial = args.ActivateLineSpecial.Special;
        foreach (var sector in sectors)
        {
            if (lineSpecial.IsSectorStopLight())
            {
                if (StopSectorLightSpecials(sector))
                    success = true;
            }
            else if (lineSpecial.IsSectorStopMove() && sector.IsMoving)
            {
                if (StopSectorMoveSpecials(lineSpecial, sector))
                    success = true;
            }
            else if (CheckUseActiveMoveSpecial(args, special, sector))
            {
                success = true;
            }
            else if (lineSpecial.IsSectorMove() || lineSpecial.IsSectorLight())
            {
                if (ResumeActiveMoveSpecial(lineSpecial, sector))
                {
                    success = true;
                    continue;
                }

                if (!CanActivateSectorSpecial(special, sector))
                    continue;

                if ((sector.DataChanges & SectorDataTypes.MovementLocked) == 0 && CreateSectorSpecial(args, special, sector))
                    success = true;
            }
            else
            {
                if (CreateSectorTriggerSpecial(args, special, sector))
                    success = true;
            }
        }

        return success;
    }

    private bool CanActivateSectorSpecial(LineSpecial special, Sector sector)
    {
        if (m_world.Config.Compatibility.VanillaSectorPhysics)
            return !sector.IsMoving;

        if (sector.ActiveCeilingMove != null && special.IsCeilingMove())
            return false;

        if (sector.ActiveFloorMove != null && special.IsFloorMove())
            return false;

        return true;
    }

    private static bool ResumeActiveMoveSpecial(LineSpecial lineSpecial, Sector sector)
    {
        // TODO missing a check?
        if (!lineSpecial.IsSectorMove() || !lineSpecial.CanPause())
            return false;

        bool success = false;
        if (sector.ActiveCeilingMove != null && sector.ActiveCeilingMove.IsPaused)
        {
            sector.ActiveCeilingMove.Resume();
            success = true;
        }
        if (sector.ActiveFloorMove != null && sector.ActiveFloorMove.IsPaused)
        {
            sector.ActiveFloorMove.Resume();
            success = true;
        }

        return success;
    }

    private static bool CheckUseActiveMoveSpecial(in EntityActivateSpecial args, LineSpecial lineSpecial, Sector sector)
    {
        if (args.ActivationContext != ActivationContext.UseLine || args.ActivateLineSpecial.SectorTag != 0)
            return false;

        if (!lineSpecial.CanActivateDuringSectorMovement())
            return false;

        bool success = false;
        if (sector.ActiveCeilingMove != null && sector.ActiveCeilingMove.Use(args.Entity))
            success = true;
        if (sector.ActiveFloorMove != null && sector.ActiveFloorMove.Use(args.Entity))
            success = true;

        return success;
    }

    private bool CreateSectorTriggerSpecial(in EntityActivateSpecial args, LineSpecial special, Sector sector)
    {
        Line line = args.ActivateLineSpecial;

        switch (special.LineSpecialType)
        {
            case ZDoomLineSpecialType.FloorTransferNumeric:
                TriggerSpecials.PlaneTransferChange(m_world, sector, line, SectorPlaneFace.Floor, PlaneTransferType.Numeric);
                return true;

            case ZDoomLineSpecialType.FloorTransferTrigger:
                TriggerSpecials.PlaneTransferChange(m_world, sector, line, SectorPlaneFace.Floor, PlaneTransferType.Trigger);
                return true;
        }

        return false;
    }

    private bool StopSectorLightSpecials(Sector sector)
    {
        bool success = false;
        LinkedListNode<ISpecial>? specNode = m_specials.First;
        LinkedListNode<ISpecial>? nextNode;
        while (specNode != null)
        {
            success = true;
            nextNode = specNode.Next;
            ISpecial spec = specNode.Value;
            if (spec.SectorBaseSpecialType == SectorBaseSpecialType.Light && spec is ISectorSpecial sectorSpecial &&
                sectorSpecial.Sector.Equals(sector))
            {
                sector.ClearActiveMoveSpecial();
                m_specials.Remove(specNode);
                m_destroyedMoveSpecials.Add((ISectorSpecial)specNode.Value);
            }

            specNode = nextNode;
        }

        return success;
    }

    private bool StopSectorMoveSpecials(LineSpecial lineSpecial, Sector sector)
    {
        bool success = false;
        LinkedListNode<ISpecial>? specNode = m_specials.First;
        LinkedListNode<ISpecial>? nextNode;
        while (specNode != null)
        {
            nextNode = specNode.Next;
            ISpecial spec = specNode.Value;
            if (spec.SectorBaseSpecialType == SectorBaseSpecialType.Move && spec is SectorMoveSpecial sectorMoveSpecial &&
                sectorMoveSpecial.Sector.Equals(sector) && IsSectorMoveSpecialMatch(lineSpecial, sectorMoveSpecial))
            {
                if (lineSpecial.CanPause())
                {
                    if (!sectorMoveSpecial.IsPaused)
                    {
                        success = true;
                        sectorMoveSpecial.Pause();
                    }
                }
                else
                {
                    success = true;
                    sector.ClearActiveMoveSpecial();
                    m_specials.Remove(specNode);
                    m_destroyedMoveSpecials.Add(sectorMoveSpecial);
                }
            }

            specNode = nextNode;
        }

        return success;
    }

    private bool IsSectorMoveSpecialMatch(LineSpecial lineSpec, SectorMoveSpecial spec)
    {
        SectorMoveData data = spec.MoveData;

        return lineSpec.LineSpecialType switch
        {
            ZDoomLineSpecialType.CeilingCrushStop => data.Crush != null && data.SectorMoveType == SectorPlaneFace.Ceiling,
            ZDoomLineSpecialType.FloorCrushStop => data.Crush != null && data.SectorMoveType == SectorPlaneFace.Floor,
            ZDoomLineSpecialType.PlatStop => data.Crush == null && data.MoveRepetition == MoveRepetition.Perpetual && data.SectorMoveType == SectorPlaneFace.Floor,
            _ => false,
        };
    }

    private bool CreateSectorSpecial(in EntityActivateSpecial args, LineSpecial special, Sector sector)
    {
        Line line = args.ActivateLineSpecial;

        switch (special.LineSpecialType)
        {
            case ZDoomLineSpecialType.FloorDonut:
                HandleFloorDonut(args.ActivateLineSpecial, sector);
                return true;

            case ZDoomLineSpecialType.FloorAndCeilingLowerRaise:
                // This is a lazy hack and not really correct. Should be wrapped in one special.
                return CreateFloorAndCeilingLowerRaise(sector, line.Args.Arg1 * SpeedFactor, line.Args.Arg2 * SpeedFactor, line.Args.Arg3);

            default:
                ISpecial? sectorSpecial = CreateSingleSectorSpecial(args.ActivateLineSpecial, special, sector);
                if (sectorSpecial != null)
                    AddSpecial(sectorSpecial);
                return sectorSpecial != null;
        }
    }

    public ISpecial? CreateSingleSectorSpecial(Line line, LineSpecial special, Sector sector)
    {
        switch (special.LineSpecialType)
        {
            case ZDoomLineSpecialType.DoorGeneric:
                return CreateGenericDoorSpecial(sector, line);

            case ZDoomLineSpecialType.GenericLift:
                return CreateGenericLiftSpecial(sector, line);

            case ZDoomLineSpecialType.GenericFloor:
                return CreateGenericPlaneSpecial(sector, line, SectorPlaneFace.Floor);

            case ZDoomLineSpecialType.GenericCeiling:
                return CreateGenericPlaneSpecial(sector, line, SectorPlaneFace.Ceiling);

            case ZDoomLineSpecialType.GenericCrusher:
                return CreateGenericCrusherSpecial(sector, line);

            case ZDoomLineSpecialType.StairsGeneric:
                return CreateGenericStairsSpecial(sector, line);

            case ZDoomLineSpecialType.DoorOpenClose:
                return CreateDoorOpenCloseSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg);

            case ZDoomLineSpecialType.DoorOpenStay:
                return CreateDoorOpenStaySpecial(sector, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.DoorClose:
                return CreateDoorCloseSpecial(sector, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.DoorCloseWaitOpen:
                return CreateDoorCloseOpenSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg);

            case ZDoomLineSpecialType.DoorLockedRaise:
                return CreateDoorLockedSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg, line.Args.Arg3);

            case ZDoomLineSpecialType.LiftDownWaitUpStay:
                return CreateLiftSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg);

            case ZDoomLineSpecialType.FloorLowerToLowest:
                return CreateFloorLowerSpecial(sector, SectorDest.LowestAdjacentFloor, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorLowerToHighest:
                return CreateFloorLowerSpecial(sector, SectorDest.HighestAdjacentFloor, line.SpeedArg * SpeedFactor, line.Args.Arg2, line.Special.LineSpecialCompatibility);

            case ZDoomLineSpecialType.FloorLowerToNearest:
                return CreateFloorLowerSpecial(sector, SectorDest.NextLowestFloor, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorLowerByValue:
                return CreateFloorLowerSpecial(sector, line.AmountArg, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorLowerByValueTimes8:
                return CreateFloorLowerSpecial(sector, line.AmountArg * 8, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorRaiseToLowest:
                return CreateFloorRaiseSpecial(sector, SectorDest.LowestAdjacentFloor, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorRaiseToHighest:
                return CreateFloorRaiseSpecial(sector, SectorDest.HighestAdjacentFloor, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorRaiseToLowestCeiling:
                return CreateFloorRaiseSpecial(sector, SectorDest.LowestAdjacentCeiling, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorRaiseToNearest:
                return CreateFloorRaiseSpecial(sector, SectorDest.NextHighestFloor, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorRaiseByValue:
                return CreateFloorRaiseSpecial(sector, line.AmountArg, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorRaiseByValueTimes8:
                return CreateFloorRaiseSpecial(sector, line.AmountArg * 8, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorMoveToValue:
                return CreateSectorMoveSpecial(sector, sector.Floor, SectorPlaneFace.Floor, line.SpeedArg * SpeedFactor,
                    line.AmountArg, line.Args.Arg3);

            case ZDoomLineSpecialType.FloorMoveToValueTimes8:
                return CreateSectorMoveSpecial(sector, sector.Floor, SectorPlaneFace.Floor, line.SpeedArg * SpeedFactor,
                    line.AmountArg * 8, line.Args.Arg3);

            case ZDoomLineSpecialType.CeilingLowerToLowest:
                return CreateCeilingLowerSpecial(sector, SectorDest.LowestAdjacentCeiling, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.CeilingLowerToHighestFloor:
                return CreateCeilingLowerSpecial(sector, SectorDest.HighestAdjacentFloor, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.CeilingLowerToFloor:
                return CreateCeilingLowerSpecial(sector, SectorDest.Floor, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.CeilingLowerByValue:
                return CreateCeilingLowerSpecial(sector, line.AmountArg, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.CeilingLowerByValueTimes8:
                return CreateCeilingLowerSpecial(sector, line.AmountArg * 8, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.CeilingRaiseToNearest:
                return CreateCeilingRaiseSpecial(sector, SectorDest.LowestAdjacentCeiling, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.CeilingRaiseByValue:
                return CreateCeilingRaiseSpecial(sector, line.AmountArg, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.CeilingRaiseByValueTimes8:
                return CreateCeilingRaiseSpecial(sector, line.AmountArg * 8, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.CeilingMoveToValue:
                return CreateSectorMoveSpecial(sector, sector.Ceiling, SectorPlaneFace.Ceiling, line.SpeedArg * SpeedFactor,
                    line.AmountArg, line.Args.Arg3);

            case ZDoomLineSpecialType.CeilingMoveToValueTimes8:
                return CreateSectorMoveSpecial(sector, sector.Ceiling, SectorPlaneFace.Ceiling, line.SpeedArg * SpeedFactor,
                    line.AmountArg * 8, line.Args.Arg3);

            case ZDoomLineSpecialType.PlatPerpetualRaiseLip:
                return CreatePerpetualMovingFloorSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg, line.Args.Arg3);

            case ZDoomLineSpecialType.LiftPerpetual:
                return CreatePerpetualMovingFloorSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg, 8);

            case ZDoomLineSpecialType.StairsBuildUpDoom:
                return CreateStairSpecial(sector, line.SpeedArg * SpeedFactor, line.Args.Arg2, line.Args.Arg3, false);

            case ZDoomLineSpecialType.StairsBuildUpDoomCrush:
                return CreateStairSpecial(sector, line.SpeedArg * SpeedFactor, line.Args.Arg2, line.Args.Arg3, true);

            case ZDoomLineSpecialType.CeilingCrushAndRaiseDist:
                return CreateCeilingCrusherSpecial(sector, line.Args.Arg1, line.Args.Arg2 * SpeedFactor, line.Args.Arg3, (ZDoomCrushMode)line.Args.Arg4);

            case ZDoomLineSpecialType.CeilingCrushRaiseAndLower:
                return CreateCeilingCrusherSpecial(sector, line.Args.Arg1 * SpeedFactor, new CrushData((ZDoomCrushMode)line.Args.Arg3, line.Args.Arg2, CrushReturnFactor));

            case ZDoomLineSpecialType.CeilingCrushStayDown:
                return CreateCeilingCrusherSpecial(sector, line.Args.Arg1 * SpeedFactor, new CrushData((ZDoomCrushMode)line.Args.Arg3, line.Args.Arg2),
                    MoveRepetition.None);

            case ZDoomLineSpecialType.CeilingCrushRaiseSilent:
                return CreateCeilingCrusherSpecial(sector, line.Args.Arg1 * SpeedFactor, new CrushData((ZDoomCrushMode)line.Args.Arg4, line.Args.Arg3),
                    silent: true);

            case ZDoomLineSpecialType.FloorRaiseAndCrushDoom:
                return CreateFloorCrusherSpecial(sector, line.Args.Arg1 * SpeedFactor, line.Args.Arg2, (ZDoomCrushMode)line.Args.Arg3);

            case ZDoomLineSpecialType.FloorRaiseCrush:
                return CreateFloorCrusherSpecial(sector, line.Args.Arg1 * SpeedFactor, line.Args.Arg2, (ZDoomCrushMode)line.Args.Arg3);

            case ZDoomLineSpecialType.CeilingCrushStop:
                break;

            case ZDoomLineSpecialType.FloorRaiseByValueTxTy:
                return CreateFloorRaiseSpecialMatchTextureAndType(sector, line, line.AmountArg, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.FloorLowerToLowestTxTy:
                return CreateFloorLowerSpecialChangeTextureAndType(sector, SectorDest.LowestAdjacentFloor, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.PlatUpValueStayTx:
                return CreateFloorRaiseSpecialMatchTexture(sector, line, line.AmountArg, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.PlatRaiseAndStay:
                return CreateRaisePlatTxSpecial(sector, line, line.Args.Arg1 * SpeedFactor, line.Args.Arg2);

            case ZDoomLineSpecialType.LightChangeToValue:
                return CreateLightChangeSpecial(sector, line.Args.Arg1);

            case ZDoomLineSpecialType.LightMinNeighbor:
                return CreateLightChangeSpecial(sector, sector.GetMinLightLevelNeighbor());

            case ZDoomLineSpecialType.LightMaxNeighbor:
                return CreateLightChangeSpecial(sector, sector.GetMaxLightLevelNeighbor());

            case ZDoomLineSpecialType.FloorRaiseByTexture:
                return CreateFloorRaiseByTextureSpecial(sector, line.Args.Arg1 * SpeedFactor);

            case ZDoomLineSpecialType.CeilingRaiseToHighest:
                return CreateCeilingRaiseSpecial(sector, SectorDest.HighestAdjacentCeiling, line.Args.Arg1 * SpeedFactor);

            case ZDoomLineSpecialType.DoorWaitClose:
                return AddDelayedSpecial(CreateDoorCloseSpecial(sector, line.Args.Arg1 * SpeedFactor), line.Args.Arg2);

            case ZDoomLineSpecialType.LightStrobeDoom:
                return new LightStrobeSpecial(m_world, sector, m_random, sector.GetMinLightLevelNeighbor(), line.Args.Arg1, line.Args.Arg2, false);

            case ZDoomLineSpecialType.PlatUpByValue:
                return CreatePlatUpByValue(sector, line.Args.Arg1 * SpeedFactor, line.Args.Arg2, line.Args.Arg3);

            case ZDoomLineSpecialType.PlatToggleCeiling:
                return CreatePlatToggleCeiling(sector);

            case ZDoomLineSpecialType.ElevatorRaiseToNearest:
                return CreateEleveatorToNearest(sector, MoveDirection.Up, line.Args.Arg1 * SpeedFactor);

            case ZDoomLineSpecialType.ElevatorLowerToNearest:
                return CreateEleveatorToNearest(sector, MoveDirection.Down, line.Args.Arg1 * SpeedFactor);

            case ZDoomLineSpecialType.ElevatorMoveToFloor:
                return CreateEleveatorToFloor(sector, line, line.Args.Arg1 * SpeedFactor);
        }

        return null;
    }

    private ISpecial? CreateEleveatorToFloor(Sector sector, Line line, double speed)
    {
        double destZ = line.Front.Sector.Floor.Z;
        MoveDirection direction = destZ < sector.Floor.Z ? MoveDirection.Down : MoveDirection.Up;
        return new ElevatorSpecial(m_world, sector, destZ, speed, direction, PlatSound);
    }

    private ISpecial? CreateEleveatorToNearest(Sector sector, MoveDirection direction, double speed)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Floor, direction == MoveDirection.Up ? SectorDest.NextHighestFloor : SectorDest.NextLowestFloor);
        return new ElevatorSpecial(m_world, sector, destZ, speed, direction, PlatSound);
    }

    private void SetCeilingLight(Line line)
    {
        IEnumerable<Sector> sectors = GetSectorsFromSpecialLine(line);
        foreach (var sector in sectors)
            sector.SetTransferCeilingLight(line.Front.Sector, m_world.Gametick);
    }

    private void SetFloorLight(Line line)
    {
        IEnumerable<Sector> sectors = GetSectorsFromSpecialLine(line);
        foreach (var sector in sectors)
            sector.SetTransferFloorLight(line.Front.Sector, m_world.Gametick);
    }

    private ISpecial? CreatePlatToggleCeiling(Sector sector)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Ceiling, SectorDest.Ceiling);
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new(SectorPlaneFace.Floor, MoveDirection.Up,
            MoveRepetition.PerpetualPause, SectorMoveData.InstantToggleSpeed, 0, flags: SectorMoveFlags.EntityBlockMovement));
    }

    private bool CreateFloorAndCeilingLowerRaise(Sector sector, double floorSpeed, double ceilingSpeed, int boomEmulation)
    {
        ISpecial? ceiling = CreateCeilingRaiseSpecial(sector, SectorDest.HighestAdjacentCeiling, ceilingSpeed);
        ISpecial? floor = null;

        // According to zdoom.org this value should be 1998...
        // Emulate boom bug causing only the floor to lower if the ceiling fails
        if (boomEmulation != 1998 || (boomEmulation == 1998 && ceiling == null))
            floor = CreateFloorLowerSpecial(sector, SectorDest.LowestAdjacentFloor, floorSpeed);

        if (floor != null)
            m_specials.AddLast(floor);
        if (ceiling != null)
            m_specials.AddLast(ceiling);

        return floor != null || ceiling != null;
    }

    private ISpecial? CreatePlatUpByValue(Sector sector, double speed, int delay, int height)
    {
        double destZ = sector.Floor.Z + height * 8;
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneFace.Floor,
            MoveDirection.Up, MoveRepetition.DelayReturn, speed, delay), PlatSound);
    }

    private ISpecial? CreateGenericDoorSpecial(Sector sector, Line line)
    {
        double speed = line.Args.Arg1 * SpeedFactor;
        int delay = GetOtics(line.Args.Arg3);
        return ((ZDoomDoorKind)line.Args.Arg2) switch
        {
            ZDoomDoorKind.OpenDelayClose => CreateDoorOpenCloseSpecial(sector, speed, delay),
            ZDoomDoorKind.OpenStay => CreateDoorOpenStaySpecial(sector, speed),
            ZDoomDoorKind.CloseDelayOpen => CreateDoorCloseOpenSpecial(sector, speed, delay),
            ZDoomDoorKind.CloseStay => CreateDoorCloseSpecial(sector, speed),
            _ => null,
        };
    }

    private ISpecial? CreateGenericLiftSpecial(Sector sector, Line line)
    {
        double speed = line.Args.Arg1 * SpeedFactor;
        int delay = GetOtics(line.Args.Arg2);
        return ((ZDoomLiftType)line.Args.Arg3) switch
        {
            ZDoomLiftType.UpByValue => CreatePlatUpByValue(sector, speed, delay, line.Args.Arg4),
            ZDoomLiftType.DownWaitUpStay => CreateLiftSpecial(sector, speed, delay),
            ZDoomLiftType.DownToNearestFloor => CreateLiftSpecial(sector, speed, delay, SectorDest.NextLowestFloor),
            ZDoomLiftType.DownToLowestCeiling => CreateLiftSpecial(sector, speed, delay, SectorDest.LowestAdjacentCeiling),
            ZDoomLiftType.PerpetualRaise => CreatePerpetualMovingFloorSpecial(sector, speed, delay, 0),
            _ => null,
        };
    }

    private ISpecial? CreateGenericPlaneSpecial(Sector sector, Line line, SectorPlaneFace planeType)
    {
        double speed = line.Args.Arg1 * SpeedFactor;
        bool raise = (line.Args.Arg4 & (int)ZDoomGenericFlags.Raise) != 0;
        double amount = raise ? line.Args.Arg2 : -line.Args.Arg2;
        MoveDirection start = raise ? MoveDirection.Up : MoveDirection.Down;
        SectorDest dest;

        if (planeType == SectorPlaneFace.Floor)
        {
            dest = ((ZDoomGenericDest)line.Args.Arg3) switch
            {
                ZDoomGenericDest.HighestPlane => SectorDest.HighestAdjacentFloor,
                ZDoomGenericDest.LowestPlane => SectorDest.LowestAdjacentFloor,
                ZDoomGenericDest.NearestPlane => raise ? SectorDest.NextHighestFloor : SectorDest.NextLowestFloor,
                ZDoomGenericDest.AdjacentOpposingPlane => SectorDest.LowestAdjacentCeiling,
                ZDoomGenericDest.OpposingPlane => SectorDest.Ceiling,
                ZDoomGenericDest.ShortestTexture => SectorDest.ShortestLowerTexture,
                _ => SectorDest.None,
            };
        }
        else
        {
            dest = ((ZDoomGenericDest)line.Args.Arg3) switch
            {
                ZDoomGenericDest.HighestPlane => SectorDest.HighestAdjacentCeiling,
                ZDoomGenericDest.LowestPlane => SectorDest.LowestAdjacentCeiling,
                ZDoomGenericDest.NearestPlane => raise ? SectorDest.NextHighestCeiling : SectorDest.NextLowestCeiling,
                ZDoomGenericDest.AdjacentOpposingPlane => SectorDest.HighestAdjacentFloor,
                ZDoomGenericDest.OpposingPlane => SectorDest.Floor,
                ZDoomGenericDest.ShortestTexture => SectorDest.ShortestUpperTexture,
                _ => SectorDest.None,
            };
        }

        return CreatePlaneSpecial(sector, planeType, line, start, dest, amount, speed, (ZDoomGenericFlags)line.Args.Arg4);
    }

    private ISpecial? CreateGenericCrusherSpecial(Sector sector, Line line)
    {
        double downSpeed = line.Args.Arg1 * SpeedFactor;
        double upSpeed = line.Args.Arg2 * SpeedFactor;
        bool silent = line.Args.Arg3 != 0;
        double destZ = sector.Floor.Z + DefaultCrushLip;
        // Note: The vanilla silent crusher still plays a stop sound when it hit it's dest, this is completely silent
        SectorSoundData soundData = silent ? NoSound : CrusherSoundRepeat;
        return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneFace.Ceiling, MoveDirection.Down,
            MoveRepetition.Perpetual, downSpeed, 0, new CrushData(ZDoomCrushMode.DoomWithSlowDown, line.Args.Arg4), returnSpeed: upSpeed),
            soundData);
    }

    private ISpecial? CreateGenericStairsSpecial(Sector sector, Line line)
    {
        double speed = line.Args.Arg1 * SpeedFactor;
        if (speed == 0)
            return null;

        MoveDirection direction = (line.Args.Arg3 & 1) == 0 ? MoveDirection.Down : MoveDirection.Up;
        bool ignoreTexture = (line.Args.Arg3 & 2) != 0;

        // Flip movement direction for next activation
        if (line.Flags.Repeat)
        {
            line.Args.Arg3 ^= 1;
            line.DataChanges |= LineDataTypes.Args;
        }

        return new StairSpecial(m_world, sector, line.Args.Arg1 * SpeedFactor, line.Args.Arg2, 0, false,
                direction, line.Args.Arg4, ignoreTexture);
    }

    private static int GetOtics(int value) => value * 35 / 8;

    private ISpecial CreateLightChangeSpecial(Sector sector, int lightLevel, int fadeTics = 0) =>
        new LightChangeSpecial(m_world, sector, (short)lightLevel, fadeTics);

    private ISpecial CreateRaisePlatTxSpecial(Sector sector, Line line, double speed, int lockout)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Floor, SectorDest.NextHighestFloor);
        m_world.SetPlaneTexture(sector.Floor, line.Front.Sector.Floor.TextureHandle);
        sector.SectorDamageSpecial = null;

        SectorMoveData moveData = new SectorMoveData(SectorPlaneFace.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0);
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, moveData, DefaultFloorSound);
    }

    private ISpecial CreateCeilingCrusherSpecial(Sector sector, double dist, double speed, int damage, ZDoomCrushMode crushMode)
    {
        double destZ = sector.Floor.Z + dist;
        return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneFace.Ceiling, MoveDirection.Down,
            MoveRepetition.Perpetual, speed, 0, new CrushData(crushMode, damage)), CrusherSoundRepeat);
    }

    private ISpecial CreateCeilingCrusherSpecial(Sector sector, double speed, CrushData crushData, MoveRepetition repetition = MoveRepetition.Perpetual,
        bool silent = false, double? returnSpeed = null)
    {
        double destZ = sector.Floor.Z + DefaultCrushLip;
        SectorSoundData sectorSoundData = silent ? SilentCrusherSound : CrusherSoundRepeat;
        return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneFace.Ceiling, MoveDirection.Down,
            repetition, speed, 0, crushData, returnSpeed: returnSpeed), sectorSoundData);
    }

    private ISpecial CreateFloorCrusherSpecial(Sector sector, double speed, int damage, ZDoomCrushMode crushMode)
    {
        double destZ = GetDestZ(sector, SectorPlaneFace.Floor, SectorDest.LowestAdjacentCeiling) - DefaultCrushLip;
        return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneFace.Floor, MoveDirection.Up,
            MoveRepetition.None, speed, 0, new CrushData(crushMode, damage)), CrusherSoundNoRepeat);
    }

    private void HandleFloorDonut(Line line, Sector sector)
    {
        IList<Sector> donutSectors = DonutSpecial.GetDonutSectors(sector);
        if (donutSectors.Count < 3)
            return;

        var lowerSector = donutSectors[0];
        var raiseSector = donutSectors[1];
        var destSector = donutSectors[2];

        AddSpecial(CreateFloorLowerSpecial(lowerSector, lowerSector.Floor.Z - destSector.Floor.Z, line.Args.Arg1 * SpeedFactor));
        AddSpecial(CreateFloorRaiseSpecial(raiseSector, destSector.Floor.Z - raiseSector.Floor.Z, line.Args.Arg2 * SpeedFactor, 
            floorChangeTexture: destSector.Floor.TextureHandle, clearDamage: true));
    }

    public IEnumerable<Sector> GetSectorsFromSpecialLine(Line line)
    {
        if (line.Special.CanActivateByTag && line.HasSectorTag)
            return m_world.FindBySectorTag(line.SectorTag);
        if (line.Special.CanActivateByBackSide && line.Back != null)
            return new List<Sector> { line.Back.Sector };

        return Enumerable.Empty<Sector>();
    }

    private double GetDestZ(Sector sector, SectorPlaneFace planeType, SectorDest destination, MoveDirection start = MoveDirection.None,
        LineSpecialCompatibility? compat = null)
    {
        switch (destination)
        {
            case SectorDest.LowestAdjacentFloor:
                return GetLowestFloorDestZ(sector);
            case SectorDest.HighestAdjacentFloor:
                return GetHighestFloorDestZ(sector);
            case SectorDest.LowestAdjacentCeiling:
                return GetLowestCeilingDestZ(sector, destination == SectorDest.LowestAdjacentCeiling && planeType == SectorPlaneFace.Floor);
            case SectorDest.HighestAdjacentCeiling:
                return GetHighestCeilingDestZ(sector);
            case SectorDest.NextLowestFloor:
                return GetNextLowestFloorDestZ(sector);
            case SectorDest.NextLowestCeiling:
                return GetNextLowestCeilingDestZ(sector);
            case SectorDest.NextHighestFloor:
                return GetNextHighestFloorDestZ(sector);
            case SectorDest.NextHighestCeiling:
                return GetNextHighestCeilingDestZ(sector);
            case SectorDest.Floor:
                return sector.Floor.Z;
            case SectorDest.Ceiling:
                return sector.Ceiling.Z;
            case SectorDest.ShortestLowerTexture:
            case SectorDest.ShortestUpperTexture:
                return GetShortTextureDestZ(sector, destination, start);
            case SectorDest.None:
            default:
                break;
        }

        return 0;
    }

    private double GetShortTextureDestZ(Sector sector, SectorDest destination, MoveDirection direction)
    {
        int dir = direction == MoveDirection.Down ? -1 : 1;
        return destination switch
        {
            SectorDest.ShortestLowerTexture => sector.Floor.Z + (sector.GetShortestTexture(TextureManager, true, m_world.Config.Compatibility) * dir),
            SectorDest.ShortestUpperTexture => sector.Floor.Z + (sector.GetShortestTexture(TextureManager, false, m_world.Config.Compatibility) * dir),
            _ => sector.Floor.Z,
        };
    }

    private static double GetNextLowestFloorDestZ(Sector sector)
    {
        Sector? destSector = sector.GetNextLowestFloor();
        return destSector?.Floor.Z ?? sector.Floor.Z;
    }

    private static double GetNextLowestCeilingDestZ(Sector sector)
    {
        Sector? destSector = sector.GetNextLowestCeiling();
        return destSector?.Ceiling.Z ?? sector.Floor.Z;
    }

    private static double GetNextHighestFloorDestZ(Sector sector)
    {
        Sector? destSector = sector.GetNextHighestFloor();
        return destSector?.Floor.Z ?? sector.Floor.Z;
    }

    private static double GetNextHighestCeilingDestZ(Sector sector)
    {
        Sector? destSector = sector.GetNextHighestCeiling();
        return destSector?.Ceiling.Z ?? sector.Ceiling.Z;
    }

    private static double GetLowestFloorDestZ(Sector sector)
    {
        Sector? destSector = sector.GetLowestAdjacentFloor();
        return destSector?.Floor.Z ?? sector.Floor.Z;
    }

    private static double GetHighestFloorDestZ(Sector sector)
    {
        Sector? destSector = sector.GetHighestAdjacentFloor();
        return destSector?.Floor.Z ?? sector.Floor.Z;
    }

    private static double GetLowestCeilingDestZ(Sector sector, bool includeThis)
    {
        Sector? destSector = sector.GetLowestAdjacentCeiling(includeThis);
        return destSector?.Ceiling.Z ?? sector.Ceiling.Z;
    }

    private static double GetHighestCeilingDestZ(Sector sector)
    {
        Sector? destSector = sector.GetHighestAdjacentCeiling();
        return destSector?.Ceiling.Z ?? sector.Ceiling.Z;
    }
}
