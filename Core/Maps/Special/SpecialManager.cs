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
        private PhysicsManager m_physicsManager;
        private IMap m_map;

        public SpecialManager(PhysicsManager physicsManager, IMap map)
        {
            m_physicsManager = physicsManager;
            m_map = map;
        }

        public bool TryAddActivatedLineSpecial(EntityActivateSpecialEventArgs args)
        {
            if (args.ActivateLineSpecial == null || (args.ActivateLineSpecial.Activated && !args.ActivateLineSpecial.Flags.Repeat))
                return false;

            int startSpecialCount = m_specials.Count;
            var special = args.ActivateLineSpecial.Special;

            if (special.IsTeleport())
                AddSpecial(new TeleportSpecial(args, m_physicsManager, m_map));
            else if (special.IsSectorMoveSpecial())
                HandleSectorMoveSpecial(args, special);
            else
                HandleDefault(args, special);

            if (m_specials.Count > startSpecialCount)
            {
                args.ActivateLineSpecial.Activated = true;
                return true;
            }

            return false;
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

        private void HandleSectorMoveSpecial(EntityActivateSpecialEventArgs args, LineSpecial special)
        {
            List<Sector> sectors = GetSectorsFromSpecialLine(args.ActivateLineSpecial);
            foreach (var sector in sectors)
            {
                if (!sector.IsMoving)
                {
                    ISpecial? sectorSpecial = CreateSectorSpecial(args, special, sector);
                    if (sectorSpecial != null)
                        AddSpecial(sectorSpecial);
                }
            }
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

                case ZLineSpecialType.FloorDonut:
                    HandleFloorDonut(line, sector);
                    return null;
            }

            return null;
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

        public void Tick()
        {
            var node = m_specials.First;

            while (node != null)
            {
                var next = node.Next;
                if (node.Value.Tick() == SpecialTickStatus.Destroy)
                    m_specials.Remove(node);
                node = next;
            }
        }

        private void AddSpecial(ISpecial special)
        {
            m_specials.AddLast(special);
        }

        public ISpecial CreateLiftSpecial(Sector sector, double speed, int delay)
        {
            double destZ = GetDestZ(sector, SectorDest.NextLowestFloor);
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Down, MoveRepetition.DelayReturn, speed, delay));
        }

        public ISpecial CreateDoorOpenCloseSpecial(Sector sector, double speed, int delay)
        {
            double destZ = GetDestZ(sector, SectorDest.NextHighestCeiling) - DoorDestOffset;
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Up, MoveRepetition.DelayReturn, speed, delay));
        }

        public ISpecial CreateDoorCloseOpenSpecial(Sector sector, double speed, int delay)
        {
            double destZ = GetDestZ(sector, SectorDest.Floor);
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Down, MoveRepetition.DelayReturn, speed, delay));
        }

        public ISpecial CreateDoorLockedSpecial(Sector sector, double speed, int delay, int key)
        {
            // TODO handle keys
            double destZ = GetDestZ(sector, SectorDest.NextHighestCeiling) - DoorDestOffset;
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Up, delay > 0 ? MoveRepetition.DelayReturn : MoveRepetition.None, speed, delay));
        }

        public ISpecial CreateDoorOpenStaySpecial(Sector sector, double speed)
        {
            double destZ = GetDestZ(sector, SectorDest.NextHighestCeiling) - DoorDestOffset;
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Up, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateDoorCloseSpecial(Sector sector, double speed)
        {
            double destZ = GetDestZ(sector, SectorDest.Floor);
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateFloorLowerSpecial(Sector sector, SectorDest sectorDest, double speed, int adjust = 0)
        {
            double destZ = GetDestZ(sector, sectorDest);
            if (adjust != 0)
                destZ = destZ + adjust - 128;
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateFloorLowerSpecial(Sector sector, double amount, double speed)
        {
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z - amount, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateFloorRaiseSpecial(Sector sector, SectorDest sectorDest, double speed)
        {
            double destZ = GetDestZ(sector, sectorDest);
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Up, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateFloorRaiseSpecial(Sector sector, double amount, double speed, CIString? floorChangeTexture = null)
        {
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Floor.Z + amount, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Up, MoveRepetition.None, speed, 0, floorChangeTexture));
        }

        public ISpecial CreateCeilingLowerSpecial(Sector sector, SectorDest sectorDest, double speed)
        {
            double destZ = GetDestZ(sector, sectorDest);
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateCeilingLowerSpecial(Sector sector, int amount, double speed)
        {
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Ceiling.Z - amount, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Down, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateCeilingRaiseSpecial(Sector sector, SectorDest sectorDest, double speed)
        {
            double destZ = GetDestZ(sector, sectorDest);
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Up, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreateCeilingRaiseSpecial(Sector sector, int amount, double speed)
        {
            return new SectorMoveSpecial(m_physicsManager, sector, sector.Ceiling.Z + amount, new SectorMoveData(SectorMoveType.Ceiling,
                MoveDirection.Up, MoveRepetition.None, speed, 0));
        }

        public ISpecial CreatePerpetualMovingFloorSpecial(Sector sector, double speed, int delay, int lip)
        {
            double destZ = GetDestZ(sector, SectorDest.NextLowestFloor) + lip;
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(SectorMoveType.Floor,
                MoveDirection.Down, MoveRepetition.Perpetual, speed, delay));
        }

        public ISpecial CreateSectorMoveSpecial(Sector sector, SectorFlat flat, SectorMoveType moveType, double speed, double destZ, byte negative)
        {
            if (negative > 0)
                destZ = -destZ;
            MoveDirection dir = destZ > flat.Z ? MoveDirection.Up : MoveDirection.Down;
            return new SectorMoveSpecial(m_physicsManager, sector, destZ, new SectorMoveData(moveType,
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
