using System.Collections.Generic;
using Helion.Util;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials
{
    public class StairSpecial : SectorMoveSpecial
    {
        private int m_stairHeight;
        private int m_stairDelayTics;
        private int m_stairDelay;
        private int m_destroyCount;
        private double m_startZ;
        private List<Sector> m_sectors = new List<Sector>();

        public StairSpecial(PhysicsManager physicsManager, Sector sector, double speed, int height, int delay, bool crush) : 
            base(physicsManager, sector, 0, 0, new SectorMoveData(SectorMoveType.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0))
        {
            m_stairHeight = height;
            m_stairDelay = 35; 
            m_startZ = Sector.Floor.Z;

            Sector? nextSector = sector;

            do
            {
                m_sectors.Add(nextSector);
                nextSector = GetNextSector(nextSector, Sector.Floor.Texture);
            }
            while (nextSector != null);

            m_sectors.ForEach(sec => sec.ActiveMoveSpecial = this);
        }

        // TODO verify me - PrevZ probably doesn't work right
        public override SpecialTickStatus Tick()
        {
            if (m_stairDelayTics > 0)
            {
                m_stairDelayTics--;
                return SpecialTickStatus.Continue;
            }

            SpecialTickStatus currentStatus = SpecialTickStatus.Continue;

            for (int i = 0; i < m_sectors.Count; i++)
            {
                Sector = m_sectors[i];
                SectorPlane = Sector.Floor;
                DestZ = m_startZ + (m_stairHeight * (i + 1));
                if (Sector.IsMoving)
                    currentStatus = base.Tick();

                if (currentStatus == SpecialTickStatus.Destroy)
                {
                    m_destroyCount++;
                    m_stairDelayTics = m_stairDelay;
                }

                if (m_destroyCount == m_sectors.Count)
                    return SpecialTickStatus.Destroy;
            }

            return SpecialTickStatus.Continue;
        }

        private static Sector? GetNextSector(Sector start, CIString floorpic)
        {
            foreach (var line in start.Lines)
                if (line.Back != null && line.Front.Sector == start && line.Back.Sector.Floor.Texture == floorpic)
                    return line.Back.Sector;

            return null;
        }
    }
}
