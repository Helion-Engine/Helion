using System.Collections.Generic;
using Helion.Maps.Geometry;
using Helion.Util;
using Helion.World.Physics;

namespace Helion.Maps.Special.Specials
{
    public class StairSpecial : SectorMoveSpecial
    {
        private bool m_crush;
        private CIString m_floorpic;
        private int m_stairHeight;
        private int m_stairDelayTics;
        private int m_stairDelay;
        private double m_startZ;

        private List<Sector> m_sectors = new List<Sector>();

        public StairSpecial(PhysicsManager physicsManager, Sector sector, double speed, int height, int delay, bool crush)
            : base(physicsManager, sector, 0, new SectorMoveData(SectorMoveType.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0))
        {
            m_crush = crush;
            m_stairHeight = height;
            m_stairDelay = delay; 
            m_startZ = Sector.Floor.Z;
            m_floorpic = Sector.Floor.Texture;

            Sector? nextSector = sector;

            do
            {
                m_sectors.Add(nextSector);
                nextSector = GetNextSector(nextSector);                   
            }
            while (nextSector != null);
        }

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
                m_flat = Sector.Floor;
                m_destZ = m_startZ + (m_stairHeight * (i + 1));
                if (m_flat.Z < m_destZ)
                    currentStatus = base.Tick();
                
                if (currentStatus == SpecialTickStatus.Destroy)
                    m_stairDelayTics = m_stairDelay;

                if (i == m_sectors.Count - 1 && currentStatus == SpecialTickStatus.Destroy)
                    return SpecialTickStatus.Destroy;
            }

            return SpecialTickStatus.Continue;
        }

        private Sector? GetNextSector(Sector start)
        {
            foreach (var side in start.Sides)
            {
                if (side.Line == null)
                    continue;

                if (side.Line.TwoSided && side.Line.Front.Sector == start && side.Line.Back.Sector.Floor.Texture == m_floorpic)
                    return side.Line.Back.Sector;
            }

            return null;
        }
    }
}
