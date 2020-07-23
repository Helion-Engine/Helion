using System.Collections.Generic;
using System.Linq;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Definitions;
using Helion.Util;
using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
using Helion.World.Special.Switches;

namespace Helion.World.Special
{
    public class SpecialManager : ITickable
    {
        // Doom used speeds 1/8 of map unit, Helion uses map units so doom speeds have to be multiplied by 1/8
        private const double SpeedFactor = 0.125;

        private readonly LinkedList<ISpecial> m_specials = new LinkedList<ISpecial>();
        private readonly List<ISectorSpecial> m_destroyedMoveSpecials = new List<ISectorSpecial>();
        private readonly IRandom m_random;
        private readonly SwitchManager m_switchManager;
        private readonly WorldBase m_world;

        private static SectorSoundData GetDefaultSectorSound() => new SectorSoundData(null, null, "DSPSTOP", "DSSTNMOV");
        private static SectorSoundData GetLiftSound() => new SectorSoundData("DSPSTART", "DSPSTART", "DSPSTOP");
        private static SectorSoundData GetCrusherSound() => new SectorSoundData(null, null, null, "DSSTNMOV");

        public SpecialManager(WorldBase world, DefinitionEntries definition, IRandom random)
        {
            m_world = world;
            m_switchManager = new SwitchManager(definition);
            m_random = random;

            StartInitSpecials();
        }

        public bool TryAddActivatedLineSpecial(EntityActivateSpecialEventArgs args)
        {
            if (args.ActivateLineSpecial == null || (args.ActivateLineSpecial.Activated && !args.ActivateLineSpecial.Flags.Repeat))
                return false;

            var special = args.ActivateLineSpecial.Special;
            bool specialActivateSuccess;

            if (special.IsSectorMoveSpecial() || special.IsSectorLightSpecial() || special.IsSectorStopMoveSpecial() || special.IsSectorStopLightSpecial())
                specialActivateSuccess = HandleSectorLineSpecial(args, special);
            else
                specialActivateSuccess = HandleDefault(args, special, m_world);

            if (specialActivateSuccess && args.ActivationContext == ActivationContext.UseLine && 
                !args.ActivateLineSpecial.Activated && m_switchManager.IsLineSwitch(args.ActivateLineSpecial))
            {
                AddSpecial(new SwitchChangeSpecial(m_switchManager, m_world.SoundManager, args.ActivateLineSpecial));
            }

            return specialActivateSuccess;
        }

        public ISpecial CreateFloorRaiseSpecialMatchTexture(Sector sector, Line line, double amount, double speed)
        {
            sector.Floor.TextureHandle = line.Front.Sector.Floor.TextureHandle;
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, sector.Floor.Z + amount, 
                new SectorMoveData(SectorPlaneType.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0));
        }
        
        public void Tick()
        {
            if (m_destroyedMoveSpecials.Count > 0)
            {
                // TODO: Encapsulate in a 'reset interpolation' function?
                // As we need to also update the Plane (if present) as well.
                for (int i = 0; i < m_destroyedMoveSpecials.Count; i++)
                    m_destroyedMoveSpecials[i].FinalizeDestroy();

                m_destroyedMoveSpecials.Clear();
            }

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
                var node = m_specials.First;
                while (node != null)
                {
                    if (node.Value.Tick() == SpecialTickStatus.Destroy)
                    {
                        m_specials.Remove(node);
                        if (node.Value is ISectorSpecial sectorSpecial)
                            m_destroyedMoveSpecials.Add(sectorSpecial);
                    }

                    node = node.Next;
                }
            }
        }

        public void AddSpecial(ISpecial special)
        {
            m_specials.AddLast(special);
        }

        public ISpecial CreateLiftSpecial(Sector sector, double speed, int delay)
        {
            double destZ = GetDestZ(sector, SectorDest.LowestAdjacentFloor);
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneType.Floor,
                MoveDirection.Down, MoveRepetition.DelayReturn, speed, delay), GetLiftSound());
        }

        public ISpecial CreateDoorOpenCloseSpecial(Sector sector, double speed, int delay)
        {
            double destZ = GetDestZ(sector, SectorDest.LowestAdjacentCeiling) - VanillaConstants.DoorDestOffset;
            return new DoorOpenCloseSpecial(m_world, sector, destZ, speed, delay);
        }

        public ISpecial CreateDoorCloseOpenSpecial(Sector sector, double speed, int delay)
        {
            double destZ = GetDestZ(sector, SectorDest.Floor);
            return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneType.Ceiling,
                MoveDirection.Down, MoveRepetition.DelayReturn, speed, delay), new SectorSoundData("DSDORCLS", "DSDOROPN", null));
        }

        public ISpecial CreateDoorLockedSpecial(Sector sector, double speed, int delay, int key)
        {
            // TODO handle keys
            double destZ = GetDestZ(sector, SectorDest.NextHighestCeiling) - VanillaConstants.DoorDestOffset;
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneType.Ceiling,
                MoveDirection.Up, delay > 0 ? MoveRepetition.DelayReturn : MoveRepetition.None, speed, delay), new SectorSoundData("DSDOROPN", "DSDORCLS", null));
        }

        public ISpecial CreateDoorOpenStaySpecial(Sector sector, double speed)
        {
            double destZ = GetDestZ(sector, SectorDest.LowestAdjacentCeiling) - VanillaConstants.DoorDestOffset;
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneType.Ceiling,
                MoveDirection.Up, MoveRepetition.None, speed, 0), new SectorSoundData("DSDOROPN", null, null));
        }

        public ISpecial CreateDoorCloseSpecial(Sector sector, double speed)
        {
            double destZ = GetDestZ(sector, SectorDest.Floor);
            return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneType.Ceiling,
                MoveDirection.Down, MoveRepetition.None, speed, 0), new SectorSoundData("DSDORCLS", null, null));
        }

        public ISpecial CreateFloorLowerSpecial(Sector sector, SectorDest sectorDest, double speed, int adjust = 0)
        {
            double destZ = GetDestZ(sector, sectorDest);
            if (adjust != 0)
                destZ = destZ + adjust - 128;
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneType.Floor,
                MoveDirection.Down, MoveRepetition.None, speed, 0), GetDefaultSectorSound());
        }

        public ISpecial CreateFloorLowerSpecial(Sector sector, double amount, double speed)
        {
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, sector.Floor.Z - amount, new SectorMoveData(SectorPlaneType.Floor,
                MoveDirection.Down, MoveRepetition.None, speed, 0), GetDefaultSectorSound());
        }

        public ISpecial CreateFloorRaiseSpecial(Sector sector, SectorDest sectorDest, double speed)
        {
            double destZ = GetDestZ(sector, sectorDest);
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneType.Floor,
                MoveDirection.Up, MoveRepetition.None, speed, 0), GetDefaultSectorSound());
        }

        public ISpecial CreateFloorRaiseSpecial(Sector sector, double amount, double speed, int? floorChangeTexture = null)
        {
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, sector.Floor.Z + amount, new SectorMoveData(SectorPlaneType.Floor,
                MoveDirection.Up, MoveRepetition.None, speed, 0, null, floorChangeTexture), GetDefaultSectorSound());
        }

        public ISpecial CreateCeilingLowerSpecial(Sector sector, SectorDest sectorDest, double speed)
        {
            double destZ = GetDestZ(sector, sectorDest);
            return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneType.Ceiling,
                MoveDirection.Down, MoveRepetition.None, speed, 0), GetDefaultSectorSound());
        }

        public ISpecial CreateCeilingLowerSpecial(Sector sector, int amount, double speed)
        {
            return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, sector.Ceiling.Z - amount, new SectorMoveData(SectorPlaneType.Ceiling,
                MoveDirection.Down, MoveRepetition.None, speed, 0), GetDefaultSectorSound());
        }

        public ISpecial CreateCeilingRaiseSpecial(Sector sector, SectorDest sectorDest, double speed)
        {
            double destZ = GetDestZ(sector, sectorDest);
            return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneType.Ceiling,
                MoveDirection.Up, MoveRepetition.None, speed, 0), GetDefaultSectorSound());
        }

        public ISpecial CreateCeilingRaiseSpecial(Sector sector, int amount, double speed)
        {
            return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, sector.Ceiling.Z + amount, new SectorMoveData(SectorPlaneType.Ceiling,
                MoveDirection.Up, MoveRepetition.None, speed, 0), GetDefaultSectorSound());
        }

        public ISpecial CreatePerpetualMovingFloorSpecial(Sector sector, double speed, int delay, int lip)
        {
            double destZ = GetDestZ(sector, SectorDest.NextLowestFloor) + lip;
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneType.Floor,
                MoveDirection.Down, MoveRepetition.Perpetual, speed, delay), GetLiftSound());
        }

        public ISpecial CreateSectorMoveSpecial(Sector sector, SectorPlane plane, SectorPlaneType moveType, double speed, double destZ, byte negative)
        {
            if (negative > 0)
                destZ = -destZ;
            
            MoveDirection dir = destZ > plane.Z ? MoveDirection.Up : MoveDirection.Down;
            return new SectorMoveSpecial(m_world, sector, plane.Z, destZ, new SectorMoveData(moveType,
                dir, MoveRepetition.None, speed, 0), GetDefaultSectorSound());
        }

        public ISpecial CreateStairSpecial(Sector sector, double speed, int height, int delay, bool crush)
        {
            return new StairSpecial(m_world, sector, speed, height, delay, crush);
        }

        private void StartInitSpecials()
        {
            var lines = m_world.Lines.Where(line => line.Special != null && line.Flags.ActivationType == ActivationType.LevelStart);
            foreach (var line in lines)
                HandleLineInitSpecial(line);

            var sectors = m_world.Sectors.Where(sec => sec.SectorSpecialType != ZDoomSectorSpecialType.None);
            foreach (var sector in sectors)
                HandleSectorSpecial(sector);
        }

        private void HandleLineInitSpecial(Line line)
        {
            switch (line.Special.LineSpecialType)
            {
            case ZDoomLineSpecialType.ScrollTextureLeft:
                AddSpecial(new LineScrollSpecial(line, line.Args.Arg0 / 64.0, 0.0, (ZDoomLineScroll)line.Args.Arg1));
                break;
            case ZDoomLineSpecialType.ScrollTextureRight:
                AddSpecial(new LineScrollSpecial(line, line.Args.Arg0 / -64.0, 0.0, (ZDoomLineScroll)line.Args.Arg1));
                break;
            case ZDoomLineSpecialType.ScrollTextureUp:
                AddSpecial(new LineScrollSpecial(line, 0.0, line.Args.Arg0 / 64.0, (ZDoomLineScroll)line.Args.Arg1));
                break;
            case ZDoomLineSpecialType.ScrollTextureDown:
                AddSpecial(new LineScrollSpecial(line, 0.0, line.Args.Arg0 / -64.0, (ZDoomLineScroll)line.Args.Arg1));
                break;
            }
        }

        private void HandleSectorSpecial(Sector sector)
        {
            switch (sector.SectorSpecialType)
            {
                case ZDoomSectorSpecialType.LightFlickerDoom:
                    AddSpecial(new LightFlickerDoomSpecial(sector, m_random, sector.GetMinLightLevelNeighbor()));
                    break;

                case ZDoomSectorSpecialType.LightStrobeFastDoom:
                    AddSpecial(new LightStrobeSpecial(sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.FastDarkTime, false));
                    break;

                case ZDoomSectorSpecialType.LightStrobeSlowDoom:
                    AddSpecial(new LightStrobeSpecial(sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.SlowDarkTime, false));
                    break;

                case ZDoomSectorSpecialType.LightStrobeHurtDoom:
                    AddSpecial(new LightStrobeSpecial(sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.SlowDarkTime, false));
                    break;

                case ZDoomSectorSpecialType.LightGlow:
                    AddSpecial(new LightPulsateSpecial(sector, sector.GetMinLightLevelNeighbor()));
                    break;

                case ZDoomSectorSpecialType.SectorDoorClose30Seconds:
                    AddSpecial(new DelayedExecuteSpecial(this, CreateDoorCloseSpecial(sector, VanillaConstants.DoorSlowSpeed * SpeedFactor), 35 * 30));
                    break;

                case ZDoomSectorSpecialType.LightStrobeSlowSync:
                    AddSpecial(new LightStrobeSpecial(sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.SlowDarkTime, true));
                    break;

                case ZDoomSectorSpecialType.LightStrobeFastSync:
                    AddSpecial(new LightStrobeSpecial(sector, m_random, sector.GetMinLightLevelNeighbor(), VanillaConstants.BrightTime, VanillaConstants.FastDarkTime, true));
                    break;

                case ZDoomSectorSpecialType.DoorRaiseIn5Minutes:
                    AddSpecial(new DelayedExecuteSpecial(this, CreateDoorOpenStaySpecial(sector, VanillaConstants.DoorSlowSpeed * SpeedFactor), 35 * 60 * 5));
                    break;

                case ZDoomSectorSpecialType.LightFireFlicker:
                    AddSpecial(new LightFireFlickerDoom(sector, m_random, sector.GetMinLightLevelNeighbor()));
                    break;
            }

            switch (sector.SectorSpecialType)
            {
                case ZDoomSectorSpecialType.DamageNukage:
                case ZDoomSectorSpecialType.DamageHellslime:
                case ZDoomSectorSpecialType.LightStrobeHurtDoom:
                case ZDoomSectorSpecialType.DamageSuperHell:
                    sector.SectorDamageSpecial = new SectorDamageSpecial(m_world, sector, GetDamageAmount(sector.SectorSpecialType));
                    break;
                case ZDoomSectorSpecialType.DamageEnd:
                    sector.SectorDamageSpecial = new SectorDamageEndSpecial(m_world, sector, GetDamageAmount(sector.SectorSpecialType));
                    break;
            }
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

        private bool HandleDefault(EntityActivateSpecialEventArgs args, LineSpecial special, WorldBase world)
        {
            switch (special.LineSpecialType)
            {
            case ZDoomLineSpecialType.Teleport:
                AddSpecial(new TeleportSpecial(args, world));
                return true;
            
            case ZDoomLineSpecialType.ExitNormal:
                m_world.ExitLevel(LevelChangeType.Next);
                return true;

            case ZDoomLineSpecialType.ExitSecret:
                m_world.ExitLevel(LevelChangeType.SecretNext);
                return true;
            }

            return false;
        }

        private bool HandleSectorLineSpecial(EntityActivateSpecialEventArgs args, LineSpecial special)
        {
            bool success = false;

            List<Sector> sectors = GetSectorsFromSpecialLine(args.ActivateLineSpecial);
            var lineSpecial = args.ActivateLineSpecial.Special;
            foreach (var sector in sectors)
            {
                if (lineSpecial.IsSectorStopLightSpecial())
                {
                    StopSectorLightSpecials(sector);
                }
                else if (lineSpecial.IsSectorStopMoveSpecial() && sector.IsMoving)
                {
                    StopSectorMoveSpecials(lineSpecial, sector);
                }
                else if ((lineSpecial.IsSectorMoveSpecial() && !sector.IsMoving) || lineSpecial.IsSectorLightSpecial())
                {
                    ISpecial? sectorSpecial = CreateSectorSpecial(args, special, sector);
                    if (sectorSpecial != null)
                    {
                        success = true;
                        AddSpecial(sectorSpecial);
                    }
                }
                else if (sector.ActiveMoveSpecial != null && args.ActivationContext == ActivationContext.UseLine &&
                    args.ActivateLineSpecial.SectorTag == 0 && lineSpecial.CanActivateDuringSectorMovement())
                {
                    sector.ActiveMoveSpecial.Use();
                }
            }

            return success;
        }

        private void StopSectorLightSpecials(Sector sector)
        {
            LinkedListNode<ISpecial>? specNode = m_specials.First;
            while (specNode != null)
            {
                if (specNode.Value.SectorBaseSpecialType == SectorBaseSpecialType.Light && specNode.Value is ISectorSpecial sectorSpecial && 
                    sectorSpecial.Sector.Equals(sector))
                {
                    sector.ActiveMoveSpecial = null;
                    m_specials.Remove(specNode);
                    m_destroyedMoveSpecials.Add((ISectorSpecial)specNode.Value);
                }

                specNode = specNode.Next;
            }
        }

        private void StopSectorMoveSpecials(LineSpecial lineSpecial, Sector sector)
        {
            LinkedListNode<ISpecial>? specNode = m_specials.First;
            while (specNode != null)
            {
                if (specNode.Value.SectorBaseSpecialType == SectorBaseSpecialType.Move && specNode.Value is SectorMoveSpecial sectorMoveSpecial && 
                    sectorMoveSpecial.Sector.Equals(sector) && IsSectorMoveSpecialMatch(lineSpecial, sectorMoveSpecial))
                {
                    sector.ActiveMoveSpecial = null;
                    m_specials.Remove(specNode);
                    m_destroyedMoveSpecials.Add(sectorMoveSpecial);
                }

                specNode = specNode.Next;
            }
        }

        private bool IsSectorMoveSpecialMatch(LineSpecial lineSpec, SectorMoveSpecial spec)
        {
            if (lineSpec.LineSpecialType == ZDoomLineSpecialType.CeilingCrushStop && spec.MoveData.Crush != null && spec.MoveData.SectorMoveType == SectorPlaneType.Ceiling)
                return true;
            else if (lineSpec.LineSpecialType == ZDoomLineSpecialType.FloorStopCrush && spec.MoveData.Crush != null && spec.MoveData.SectorMoveType == SectorPlaneType.Floor)
                return true;
            else if (lineSpec.LineSpecialType == ZDoomLineSpecialType.PlatStop && spec.MoveData.Crush == null && spec.MoveData.SectorMoveType == SectorPlaneType.Floor)
                return true;

            return false;
        }

        private ISpecial? CreateSectorSpecial(EntityActivateSpecialEventArgs args, LineSpecial special, Sector sector)
        {
            Line line = args.ActivateLineSpecial;

            switch (special.LineSpecialType)
            {
            case ZDoomLineSpecialType.Teleport:
                return new TeleportSpecial(args, m_world);

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
                return CreateFloorLowerSpecial(sector, SectorDest.HighestAdjacentFloor, line.SpeedArg * SpeedFactor, line.Args.Arg2);

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
                return CreateSectorMoveSpecial(sector, sector.Floor, SectorPlaneType.Floor, line.SpeedArg * SpeedFactor,
                    line.AmountArg, line.Args.Arg3);

            case ZDoomLineSpecialType.FloorMoveToValueTimes8:
                return CreateSectorMoveSpecial(sector, sector.Floor, SectorPlaneType.Floor, line.SpeedArg * SpeedFactor, 
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
                return CreateSectorMoveSpecial(sector, sector.Ceiling, SectorPlaneType.Ceiling, line.SpeedArg * SpeedFactor,
                    line.AmountArg, line.Args.Arg3);

            case ZDoomLineSpecialType.CeilingMoveToValueTimes8:
                return CreateSectorMoveSpecial(sector, sector.Ceiling, SectorPlaneType.Ceiling, line.SpeedArg * SpeedFactor,
                    line.AmountArg * 8, line.Args.Arg3);

            case ZDoomLineSpecialType.PlatPerpetualRaiseLip:
                return CreatePerpetualMovingFloorSpecial(sector, line.SpeedArg * SpeedFactor, line.DelayArg, line.Args.Arg3);

            case ZDoomLineSpecialType.StairsBuildUpDoom:
                return CreateStairSpecial(sector, line.SpeedArg * SpeedFactor, line.Args.Arg2, line.Args.Arg3, false);

            case ZDoomLineSpecialType.StairsBuildUpDoomCrush:
                return CreateStairSpecial(sector, line.SpeedArg * SpeedFactor, line.Args.Arg2, line.Args.Arg3, true);

            case ZDoomLineSpecialType.CeilingCrushAndRaiseDist:
                return CreateCeilingCrusherSpecial(sector, line.Args.Arg1, line.Args.Arg2 * SpeedFactor, line.Args.Arg3, (ZDoomCrushMode)line.Args.Arg4);

            case ZDoomLineSpecialType.CeilingCrushRaiseAndLower:
                return CreateCeilingCrusherSpecial(sector, line.Args.Arg1 * SpeedFactor, new CrushData((ZDoomCrushMode)line.Args.Arg3, line.Args.Arg2, 0.5));

            case ZDoomLineSpecialType.FloorRaiseAndCrushDoom:
                return CreateFloorCrusherSpecial(sector, line.Args.Arg1 * SpeedFactor, line.Args.Arg2, (ZDoomCrushMode)line.Args.Arg3);

            case ZDoomLineSpecialType.FloorRaiseCrush:
                return CreateFloorCrusherSpecial(sector, line.Args.Arg1 * SpeedFactor, line.Args.Arg2, (ZDoomCrushMode)line.Args.Arg3);

            case ZDoomLineSpecialType.CeilingCrushStop:
                break;

            case ZDoomLineSpecialType.FloorRaiseByValueTxTy:
                return CreateFloorRaiseSpecialMatchTexture(sector, line, line.AmountArg, line.SpeedArg * SpeedFactor);

            case ZDoomLineSpecialType.PlatRaiseAndStay:
                return CreateRaisePlatTxSpecial(sector, line, line.Args.Arg1 * SpeedFactor, line.Args.Arg2);

            case ZDoomLineSpecialType.LightChangeToValue:
                return CreateLightChangeSpecial(sector, line.Args.Arg1);

            case ZDoomLineSpecialType.LightMinNeighbor:
                return CreateLightChangeSpecial(sector, sector.GetMinLightLevelNeighbor());

            case ZDoomLineSpecialType.LightMaxNeighbor:
                return CreateLightChangeSpecial(sector, sector.GetMaxLightLevelNeighbor());

            case ZDoomLineSpecialType.FloorDonut:
                HandleFloorDonut(line, sector);
                return null;
            }

            return null;
        }

        private ISpecial CreateLightChangeSpecial(Sector sector, short lightLevel, int fadeTics = 0)
        {
            return new LightChangeSpecial(sector, lightLevel, fadeTics);
        }

        private ISpecial CreateRaisePlatTxSpecial(Sector sector, Line line, double speed, byte lockout)
        {
            double destZ = GetDestZ(sector, SectorDest.NextHighestFloor);
            sector.Floor.TextureHandle = line.Front.Sector.Floor.TextureHandle;
            sector.SectorDamageSpecial = null;

            SectorMoveData moveData = new SectorMoveData(SectorPlaneType.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0);
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, moveData, GetDefaultSectorSound());
        }

        private ISpecial CreateCeilingCrusherSpecial(Sector sector, double dist, double speed, int damage, ZDoomCrushMode crushMode)
        {
            double destZ = sector.Floor.Z + dist;
            return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneType.Ceiling, MoveDirection.Down, 
                MoveRepetition.Perpetual, speed, 0, new CrushData(crushMode, damage)), GetCrusherSound());
        }

        private ISpecial CreateCeilingCrusherSpecial(Sector sector, double speed, CrushData crushData)
        {
            double destZ = sector.Floor.Z + 8;
            return new SectorMoveSpecial(m_world, sector, sector.Ceiling.Z, destZ, new SectorMoveData(SectorPlaneType.Ceiling, MoveDirection.Down, 
                MoveRepetition.Perpetual, speed, 0, crushData), GetCrusherSound());
        }

        private ISpecial CreateFloorCrusherSpecial(Sector sector, double speed, int damage, ZDoomCrushMode crushMode)
        {
            double destZ = sector.Ceiling.Z - 8;
            return new SectorMoveSpecial(m_world, sector, sector.Floor.Z, destZ, new SectorMoveData(SectorPlaneType.Floor, MoveDirection.Up, 
                MoveRepetition.None, speed, 0, new CrushData(crushMode, damage)), GetCrusherSound());
        }

        private void HandleFloorDonut(Line line, Sector sector)
        {
            List<Sector>? donutSectors = DonutSpecial.GetDonutSectors(sector);
            if (donutSectors == null) 
                return;
            
            var lowerSector = donutSectors[0];
            var raiseSector = donutSectors[1];
            var destSector = donutSectors[2];

            AddSpecial(CreateFloorLowerSpecial(lowerSector, lowerSector.Floor.Z - destSector.Floor.Z, line.Args.Arg1 * SpeedFactor));
            AddSpecial(CreateFloorRaiseSpecial(raiseSector, destSector.Floor.Z - raiseSector.Floor.Z, line.Args.Arg2 * SpeedFactor, destSector.Floor.TextureHandle));
        }

        private List<Sector> GetSectorsFromSpecialLine(Line line)
        {
            if (line.Special.CanActivateByTag && line.HasSectorTag)
                return m_world.Sectors.Where(x => x.Tag == line.SectorTag).ToList();
            else if (line.Special.CanActivateByBackSide && line.Back != null)
                return new List<Sector> { line.Back.Sector };

            return new List<Sector>();
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
            Sector? destSector = sector.GetNextLowestFloor();
            return destSector?.Floor.Z ?? sector.Floor.Z;
        }

        private double GetNextLowestCeilingDestZ(Sector sector)
        {
            Sector? destSector = sector.GetNextLowestCeiling();
            return destSector?.Ceiling.Z ?? sector.Floor.Z;
        }

        private double GetNextHighestFloorDestZ(Sector sector)
        {
            Sector? destSector = sector.GetNextHighestFloor();
            return destSector?.Floor.Z ?? sector.Floor.Z;
        }

        private double GetNextHighestCeilingDestZ(Sector sector)
        {
            Sector? destSector = sector.GetNextHighestCeiling();
            return destSector?.Ceiling.Z ?? sector.Ceiling.Z;
        }

        private double GetLowestFloorDestZ(Sector sector)
        {
            Sector? destSector = sector.GetLowestAdjacentFloor();
            return destSector?.Floor.Z ?? sector.Floor.Z;
        }

        private double GetHighestFloorDestZ(Sector sector)
        {
            Sector? destSector = sector.GetHighestAdjacentFloor();
            return destSector?.Floor.Z ?? sector.Floor.Z;
        }

        private double GetLowestCeilingDestZ(Sector sector)
        {
            Sector? destSector = sector.GetLowestAdjacentCeiling();
            return destSector?.Ceiling.Z ?? sector.Ceiling.Z;
        }

        private double GetHighestCeilingDestZ(Sector sector)
        {
            Sector? destSector = sector.GetHighestAdjacentCeiling();
            return destSector?.Ceiling.Z ?? sector.Ceiling.Z;
        }
    }
}