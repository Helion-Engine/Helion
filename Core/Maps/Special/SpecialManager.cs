using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Maps.Special.Specials;
using Helion.Util;
using Helion.World.Physics;

namespace Helion.Maps.Special
{
    public class SpecialManager
    {
        public event EventHandler LevelExit;

        private const int DoorDestOffset = 4;

        // Doom used speeds 1/8 of map unit, Helion uses map units so doom speeds have to be multiplied by 1/8
        private const double SpeedFactor = 0.125;

        private LinkedList<ISpecial> m_specials = new LinkedList<ISpecial>();
        private List<ISpecial> m_destroyedMoveSpecials = new List<ISpecial>();
        private PhysicsManager m_physicsManager;
        private IMap m_map;

        private SwitchManager m_switchManager = new SwitchManager();
        private DoomRandom m_random = new DoomRandom();

        public SpecialManager(PhysicsManager physicsManager, IMap map)
        {
            m_physicsManager = physicsManager;
            m_map = map;

            StartInitSpecials();
        }

        public bool TryAddActivatedLineSpecial(EntityActivateSpecialEventArgs args)
        {
            if (args.ActivateLineSpecial == null || (args.ActivateLineSpecial.Activated && !args.ActivateLineSpecial.Flags.Repeat))
                return false;

            int startSpecialCount = m_specials.Count;
            var special = args.ActivateLineSpecial.Special;
            bool sectorSpecialSuccess = false;

            if (special.IsTeleport())
                AddSpecial(new TeleportSpecial(args, m_physicsManager, m_map));
            else if (special.IsSectorMoveSpecial() || special.IsSectorLightSpecial())
                sectorSpecialSuccess = HandleSectorLineSpecial(args, special);
            else
                HandleDefault(args, special);

            if (m_specials.Count > startSpecialCount)
            {
                if (sectorSpecialSuccess && args.ActivationContext == ActivationContext.UseLine && !args.ActivateLineSpecial.Activated)
                    AddSpecial(new SwitchChangeSpecial(m_switchManager, args.ActivateLineSpecial));

                return true;
            }

            return false;
        }

        private void StartInitSpecials()
        {
            var lines = m_map.Lines.Where(x => x.Special != null && x.Flags.ActivationType == ActivationType.LevelStart);
            foreach (var line in lines)
                HandleLineInitSpecial(line);

            var sectors = m_map.Sectors.Where(x => x.SectorSpecialType != ZSectorSpecialType.None);
            foreach (var sector in sectors)
                HandleSectorSpecial(sector);
        }

        private void HandleLineInitSpecial(Line line)
        {
            switch (line.Special.LineSpecialType)
            {
                case ZLineSpecialType.ScrollTextureLeft:
                    AddSpecial(new LineScrollSpecial(line, line.Args[0] / 32, (ZLineScroll)line.Args[1]));
                    break;
            }
        }

        private void HandleSectorSpecial(Sector sector)
        {
            switch (sector.SectorSpecialType)
            {
                case ZSectorSpecialType.LightFlickerDoom:
                    AddSpecial(new LightFlickerDoomSpecial(sector, m_random, m_map.GetMinLightLevelNeighbor(sector)));
                    break;

                case ZSectorSpecialType.LightStrobeFastDoom:
                    AddSpecial(new LightStrobeSpecial(sector, m_map.GetMinLightLevelNeighbor(sector), 17, 5));
                    break;

                case ZSectorSpecialType.LightStrobeSlowDoom:
                    AddSpecial(new LightStrobeSpecial(sector, m_map.GetMinLightLevelNeighbor(sector), 35, 5));
                    break;

                case ZSectorSpecialType.LightStrobeHurtDoom:
                    AddSpecial(new LightStrobeSpecial(sector, m_map.GetMinLightLevelNeighbor(sector), 35, 5));
                    break;

                case ZSectorSpecialType.LightGlow:
                    AddSpecial(new LightPulsateSpecial(sector, m_map.GetMinLightLevelNeighbor(sector)));
                    break;

                case ZSectorSpecialType.SectorDoorClose30Seconds:
                    AddSpecial(new DelayedExecuteSpecial(this, CreateDoorCloseSpecial(sector, 16 * SpeedFactor), 35 * 30));
                    break;

                case ZSectorSpecialType.DamageEnd:
                    break;

                case ZSectorSpecialType.LightStrobeSlowSync:
                    AddSpecial(new LightStrobeSpecial(sector, m_map.GetMinLightLevelNeighbor(sector), 35, 5));
                    break;

                case ZSectorSpecialType.LightStrobeFastSync:
                    AddSpecial(new LightStrobeSpecial(sector, m_map.GetMinLightLevelNeighbor(sector), 17, 5));
                    break;

                case ZSectorSpecialType.DoorRaiseIn5Minutes:
                    AddSpecial(new DelayedExecuteSpecial(this, CreateDoorOpenStaySpecial(sector, 16 * SpeedFactor), 35 * 60 * 5));
                    break;

                case ZSectorSpecialType.LightFireFlicker:
                    AddSpecial(new LightFireFlickerDoom(sector, m_random, m_map.GetMinLightLevelNeighbor(sector)));
                    break;
            }
        }

        private void HandleDefault(EntityActivateSpecialEventArgs args, LineSpecial special)
        {
            switch (special.LineSpecialType)
            {
                case ZLineSpecialType.ExitNormal:
                    LevelExit?.Invoke(this, new EventArgs());
                    break;
            }
        }

        private bool HandleSectorLineSpecial(EntityActivateSpecialEventArgs args, LineSpecial special)
        {
            bool success = false;

            List<Sector> sectors = GetSectorsFromSpecialLine(args.ActivateLineSpecial);
            var lineSpecial = args.ActivateLineSpecial.Special;
            foreach (var sector in sectors)
            {
                if ((lineSpecial.IsSectorMoveSpecial() && !sector.IsMoving) || lineSpecial.IsSectorLightSpecial())
                {
                    ISpecial? sectorSpecial = CreateSectorSpecial(args, special, sector);
                    if (sectorSpecial != null)
                    {
                        success = true;
                        AddSpecial(sectorSpecial);
                    }
                }
                else if (args.ActivationContext == ActivationContext.UseLine && lineSpecial.CanActivateDuringSectorMovement() 
                    && args.ActivateLineSpecial.SectorTag == 0)
                {
                    sector.ActiveMoveSpecial.Use();
                }
            }

            return success;
        }

        private ISpecial? CreateSectorSpecial(EntityActivateSpecialEventArgs args, LineSpecial special, Sector sector)
        {
            Line line = args.ActivateLineSpecial;

            switch (special.LineSpecialType)
            {
                case ZLineSpecialType.Teleport:
                    return new TeleportSpecial(args, m_physicsManager, m_map);

                case ZLineSpecialType.DoorOpenClose:
                    return CreateDoorOpenCloseSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg);

                case ZLineSpecialType.DoorOpenStay:
                    return CreateDoorOpenStaySpecial(sector, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.DoorClose:
                    return CreateDoorCloseSpecial(sector, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.DoorCloseWaitOpen:
                    return CreateDoorCloseOpenSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg);

                case ZLineSpecialType.DoorLockedRaise:
                    return CreateDoorLockedSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg, line.Args[3]);

                case ZLineSpecialType.LiftDownWaitUpStay:
                    return CreateLiftSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg);

                case ZLineSpecialType.FloorLowerToLowest:
                    return CreateFloorLowerSpecial(sector, SectorDest.LowestAdjacentFloor, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorLowerToHighest:
                    return CreateFloorLowerSpecial(sector, SectorDest.HighestAdjacentFloor, line.SpeedArg * SpeedFactor, line.Args[2]);

                case ZLineSpecialType.FloorLowerToNearest:
                    return CreateFloorLowerSpecial(sector, SectorDest.NextLowestFloor, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorLowerByValue:
                    return CreateFloorLowerSpecial(sector, line.AmountArg, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorLowerByValueTimes8:
                    return CreateFloorLowerSpecial(sector, line.AmountArg * 8, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorRaiseToLowest:
                    return CreateFloorRaiseSpecial(sector, SectorDest.LowestAdjacentFloor, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorRaiseToHighest:
                    return CreateFloorRaiseSpecial(sector, SectorDest.HighestAdjacentFloor, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorRaiseToLowestCeiling:
                    return CreateFloorRaiseSpecial(sector, SectorDest.LowestAdjacentCeiling, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorRaiseToNearset:
                    return CreateFloorRaiseSpecial(sector, SectorDest.NextHighestFloor, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorRaiseByValue:
                    return CreateFloorRaiseSpecial(sector, line.AmountArg, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorRaiseByValueTimes8:
                    return CreateFloorRaiseSpecial(sector, line.AmountArg * 8, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.FloorMoveToValue:
                    return CreateSectorMoveSpecial(sector, sector.Floor, SectorMoveType.Floor, line.SpeedArg * SpeedFactor,
                        line.AmountArg, line.Args[3]);

                case ZLineSpecialType.FloorMoveToValueTimes8:
                    return CreateSectorMoveSpecial(sector, sector.Floor, SectorMoveType.Floor, line.SpeedArg * SpeedFactor, 
                        line.AmountArg * 8, line.Args[3]);

                case ZLineSpecialType.CeilingLowerToLowest:
                    return CreateCeilingLowerSpecial(sector, SectorDest.LowestAdjacentCeiling, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.CeilingLowerToHighestFloor:
                    return CreateCeilingLowerSpecial(sector, SectorDest.HighestAdjacentFloor, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.CeilingLowerToFloor:
                    return CreateCeilingLowerSpecial(sector, SectorDest.Floor, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.CeilingLowerByValue:
                    return CreateCeilingLowerSpecial(sector, line.AmountArg, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.CeilingLowerByValueTimes8:
                    return CreateCeilingLowerSpecial(sector, line.AmountArg * 8, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.CeilingRaiseToNearest:
                    return CreateCeilingRaiseSpecial(sector, SectorDest.LowestAdjacentCeiling, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.CeilingRaiseByValue:
                    return CreateCeilingRaiseSpecial(sector, line.AmountArg, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.CeilingRaiseByValueTimes8:
                    return CreateCeilingRaiseSpecial(sector, line.AmountArg * 8, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.CeilingMoveToValue:
                    return CreateSectorMoveSpecial(sector, sector.Ceiling, SectorMoveType.Ceiling, line.SpeedArg * SpeedFactor,
                        line.AmountArg, line.Args[3]);

                case ZLineSpecialType.CeilingMoveToValueTimes8:
                    return CreateSectorMoveSpecial(sector, sector.Ceiling, SectorMoveType.Ceiling, line.SpeedArg * SpeedFactor,
                        line.AmountArg * 8, line.Args[3]);

                case ZLineSpecialType.PlatPerpetualRaiseLip:
                    return CreatePerpetualMovingFloorSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg, line.Args[3]);

                case ZLineSpecialType.StairsBuildUpDoom:
                    return CreateStairSpecial(sector, line.SpeedArg * SpeedFactor, line.Args[2], line.Args[3], false);

                case ZLineSpecialType.StairsBuildUpDoomCrush:
                    return CreateStairSpecial(sector, line.SpeedArg * SpeedFactor, line.Args[2], line.Args[3], true);

                case ZLineSpecialType.CeilingCrushAndRaiseDist:
                    return CreateCeilingCrusherSpecial(sector, line.Args[1], line.Args[2] * SpeedFactor, line.Args[3], (ZCrushMode)line.Args[4]);

                case ZLineSpecialType.FloorRaiseAndCrushDoom:
                    return CreateFloorCrusherSpecial(sector, line.Args[1] * SpeedFactor, line.Args[2], (ZCrushMode)line.Args[3]);

                case ZLineSpecialType.FloorRaiseByValueTxTy:
                    return CreateFloorRaiseSpecialMatchTexture(sector, line, line.AmountArg, line.SpeedArg * SpeedFactor);

                case ZLineSpecialType.PlatRaiseAndStay:
                    return CreateRaisePlatTxSpecial(sector, line, line.Args[1] * SpeedFactor, line.Args[2]);

                case ZLineSpecialType.LightChangeToValue:
                    return CreateLightChangeSpecial(sector, line.Args[1]);

                case ZLineSpecialType.LightMinNeighbor:
                    return CreateLightChangeSpecial(sector, m_map.GetMinLightLevelNeighbor(sector));

                case ZLineSpecialType.LightMaxNeighor:
                    return CreateLightChangeSpecial(sector, m_map.GetMaxLightLevelNeighbor(sector));

                case ZLineSpecialType.FloorDonut:
                    HandleFloorDonut(line, sector);
                    return null;
            }

            return null;
        }

        private ISpecial CreateLightChangeSpecial(Sector sector, byte lightLevel, int fadeTics = 0)
        {
            return new LightChangeSpecial(sector, lightLevel, fadeTics);
        }

        public ISpecial CreateFloorRaiseSpecialMatchTexture(Sector sector, Line line, double amount, double speed)
        {
            sector.Floor.Texture = line.Front.Sector.Floor.Texture;
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, sector.Floor.Z + amount, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Up, MoveRepetition.None, speed, 0));
        }

        private ISpecial CreateRaisePlatTxSpecial(Sector sector, Line line, double speed, byte lockout)
        {
            // TODO clear sector special when implemented
            double destZ = GetDestZ(sector, SectorDest.NextHighestFloor);
            sector.Floor.Texture = line.Front.Sector.Floor.Texture;
            //sector.Special = null;
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Up, MoveRepetition.None, speed, 0, null));
        }

        private ISpecial CreateCeilingCrusherSpecial(Sector sector, double dist, double speed, int damage, ZCrushMode crushMode)
        {
            double destZ = sector.Floor.Z + dist;
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorMoveType.Ceiling, MoveDirection.Down, MoveRepetition.Perpetual, speed, 0, new CrushData(crushMode, damage)));
        }

        private ISpecial CreateFloorCrusherSpecial(Sector sector, double speed, int damage, ZCrushMode crushMode)
        {
            double destZ = sector.Ceiling.Z - 8;
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorMoveType.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0, new CrushData(crushMode, damage)));
        }

        private void HandleFloorDonut(Line line, Sector sector)
        {
            var donutSectors = DonutSpecial.GetDonutSectors(sector);

            if (donutSectors != null)
            {
                var lowerSector = donutSectors[0];
                var raiseSector = donutSectors[1];
                var destSector = donutSectors[2];

                AddSpecial(CreateFloorLowerSpecial(lowerSector, lowerSector.Floor.Z - destSector.Floor.Z, line.Args[1] * SpeedFactor));
                AddSpecial(CreateFloorRaiseSpecial(raiseSector, destSector.Floor.Z - raiseSector.Floor.Z, line.Args[2] * SpeedFactor, destSector.Floor.Texture));
            }
        }

        public void Tick(long gametic)
        {
            if (m_destroyedMoveSpecials.Count > 0)
            {
                foreach (var special in m_destroyedMoveSpecials)
                {
                    special.Sector.Floor.PrevZ = special.Sector.Floor.Z;
                    special.Sector.Ceiling.PrevZ = special.Sector.Ceiling.Z;
                }

                m_destroyedMoveSpecials.Clear();
            }

            var node = m_specials.First;

            while (node != null)
            {
                var next = node.Next;
                if (node.Value.Tick(gametic) == SpecialTickStatus.Destroy)
                {
                    m_specials.Remove(node);
                    if (node.Value.Sector != null)
                        m_destroyedMoveSpecials.Add(node.Value);
                }

                node = next;
            }
        }

        public void AddSpecial(ISpecial special)
        {
            m_specials.AddLast(special);
        }

        public ISpecial CreateLiftSpecial(Sector sector, double speed, int delay)
        {
            double destZ = GetDestZ(sector, SectorDest.LowestAdjacentFloor);
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Down, MoveRepetition.DelayReturn, speed, delay));
        }

        public ISpecial CreateDoorOpenCloseSpecial(Sector sector, double speed, int delay)
        {
            double destZ = GetDestZ(sector, SectorDest.LowestAdjacentCeiling) - DoorDestOffset;
            return new DoorOpenCloseSpecial(m_physicsManager, sector, destZ, speed, delay);
        }

        public ISpecial CreateDoorCloseOpenSpecial(Sector sector, double speed, int delay)
        {
            double destZ = GetDestZ(sector, SectorDest.Floor);
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Down, MoveRepetition.DelayReturn, speed, delay));
        }

        public ISpecial CreateDoorLockedSpecial(Sector sector, double speed, int delay, int key)
        {
            // TODO handle keys
            double destZ = GetDestZ(sector, SectorDest.NextHighestCeiling) - DoorDestOffset;
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Up, delay > 0 ? MoveRepetition.DelayReturn : MoveRepetition.None, speed, delay));
        }

        public ISpecial CreateDoorOpenStaySpecial(Sector sector, double speed)
        {
            double destZ = GetDestZ(sector, SectorDest.LowestAdjacentCeiling) - DoorDestOffset;
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Up, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateDoorCloseSpecial(Sector sector, double speed)
        {
            double destZ = GetDestZ(sector, SectorDest.Floor);
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateFloorLowerSpecial(Sector sector, SectorDest sectorDest, double speed, int adjust = 0)
        {
            double destZ = GetDestZ(sector, sectorDest);
            if (adjust != 0)
                destZ = destZ + adjust - 128;
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateFloorLowerSpecial(Sector sector, double amount, double speed)
        {
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, sector.Floor.Z - amount, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateFloorRaiseSpecial(Sector sector, SectorDest sectorDest, double speed)
        {
            double destZ = GetDestZ(sector, sectorDest);
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Up, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateFloorRaiseSpecial(Sector sector, double amount, double speed, CIString? floorChangeTexture = null)
        {
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, sector.Floor.Z + amount, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Up, MoveRepetition.None, speed, 0, null, floorChangeTexture));
        }

        public ISpecial CreateCeilingLowerSpecial(Sector sector, SectorDest sectorDest, double speed)
        {
            double destZ = GetDestZ(sector, sectorDest);
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateCeilingLowerSpecial(Sector sector, int amount, double speed)
        {
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Ceiling.Z, sector.Ceiling.Z - amount, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateCeilingRaiseSpecial(Sector sector, SectorDest sectorDest, double speed)
        {
            double destZ = GetDestZ(sector, sectorDest);
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Up, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateCeilingRaiseSpecial(Sector sector, int amount, double speed)
        {
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Ceiling.Z, sector.Ceiling.Z + amount, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Up, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreatePerpetualMovingFloorSpecial(Sector sector, double speed, int delay, int lip)
        {
            double destZ = GetDestZ(sector, SectorDest.NextLowestFloor) + lip;
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Down, MoveRepetition.Perpetual, speed, delay));
        }

        public ISpecial CreateSectorMoveSpecial(Sector sector, SectorFlat flat, SectorMoveType moveType, double speed, double destZ, byte negative)
        {
            if (negative > 0)
                destZ = -destZ;
            MoveDirection dir = destZ > flat.Z ? MoveDirection.Up : MoveDirection.Down;
            return new SectorMoveSpecial(m_physicsManager, sector, flat.Z, destZ, new SectorMoveData(moveType,
                dir, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateStairSpecial(Sector sector, double speed, int height, int delay, bool crush)
        {
            return new StairSpecial(m_physicsManager, sector, speed, height, delay, crush);
        }

        private List<Sector> GetSectorsFromSpecialLine(Line line)
        {
            if (line.HasSectorTag)
                return m_map.Sectors.Where(x => x.Tag == line.SectorTag).ToList();
            else if (line.Back != null)
                return new List<Sector> { line.Back.Sector };

            return new List<Sector> { };
        }

        private double GetDestZ(Sector sector, SectorDest destination)
        {
            switch (destination)
            {
                case SectorDest.LowestAdjacentFloor:
                    return GetLowestFloorDestZ(sector);
                case SectorDest.HighestAdjacentFloor:
                    return GetHighestFloorDestZ(sector);
                case SectorDest.LowestAdjacentCeiling:
                    return GetLowestCeilingDestZ(sector);
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
            }

            return 0;
        }

        private double GetNextLowestFloorDestZ(Sector sector)
        {
            Sector? destSector = m_map.GetNextLowestFloor(sector);
            return destSector == null ? sector.Floor.Z : destSector.Floor.Z;
        }

        private double GetNextLowestCeilingDestZ(Sector sector)
        {
            Sector? destSector = m_map.GetNextLowestCeiling(sector);
            return destSector == null ? sector.Floor.Z : destSector.Ceiling.Z;
        }

        private double GetNextHighestFloorDestZ(Sector sector)
        {
            Sector? destSector = m_map.GetNextHighestFloor(sector);
            return destSector == null ? sector.Floor.Z : destSector.Floor.Z;
        }

        private double GetNextHighestCeilingDestZ(Sector sector)
        {
            Sector? destSector = m_map.GetNextHighestCeiling(sector);
            return destSector == null ? sector.Floor.Z : destSector.Ceiling.Z;
        }

        private double GetLowestFloorDestZ(Sector sector)
        {
            Sector? destSector = m_map.GetLowestAdjacentFloor(sector);
            return destSector == null ? sector.Floor.Z : destSector.Floor.Z;
        }

        private double GetHighestFloorDestZ(Sector sector)
        {
            Sector? destSector = m_map.GetHighestAdjacentFloor(sector);
            return destSector == null ? sector.Floor.Z : destSector.Floor.Z;
        }

        private double GetLowestCeilingDestZ(Sector sector)
        {
            Sector? destSector = m_map.GetLowestAdjacentCeiling(sector);
            return destSector == null ? sector.Ceiling.Z : destSector.Ceiling.Z;
        }

        private double GetHighestCeilingDestZ(Sector sector)
        {
            Sector? destSector = m_map.GetHighestAdjacentCeiling(sector);
            return destSector == null ? sector.Ceiling.Z : destSector.Ceiling.Z;
        }
    }
}
