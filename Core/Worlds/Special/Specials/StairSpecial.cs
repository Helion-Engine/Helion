using System.Collections.Generic;
using Helion.Audio;
using Helion.Util;
using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Special.SectorMovement;
using Helion.Worlds.Textures;

namespace Helion.Worlds.Special.Specials
{
    public class StairSpecial : SectorMoveSpecial
    {
        private readonly int m_stairHeight;
        private readonly int m_stairDelay;
        private readonly double m_startZ;
        private readonly List<Sector> m_sectors = new();
        private int m_destroyCount;
        private int m_stairDelayTics;

        public StairSpecial(World world, Sector sector, double speed, int height, int delay, bool crush) :
            base(world, sector, 0, 0, new SectorMoveData(SectorPlaneType.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0),
                new SectorSoundData(null, null, Constants.PlatStopSound, null))
        {
            m_stairHeight = height;
            m_stairDelay = delay;
            m_startZ = Sector.Floor.Z;

            Sector? nextSector = sector;

            do
            {
                m_sectors.Add(nextSector);
                nextSector = GetNextSector(nextSector, Sector.Floor.Texture);
            }
            while (nextSector != null);

            m_sectors.ForEach(sec => InitSector(sec));
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
                else
                    SectorPlane.PrevZ = SectorPlane.Z;

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

        public override void FinalizeDestroy()
        {
            if (m_sectors.Count > 0)
            {
                Sector = m_sectors[m_sectors.Count - 1];
                Sector.Floor.PrevZ = Sector.Floor.Z;
            }
        }

        private static Sector? GetNextSector(Sector start, IWorldTexture floorpic)
        {
            for (int i = 0; i < start.Lines.Count; i++)
            {
                Line line = start.Lines[i];
                if (line.Back != null && line.Front.Sector == start && line.Back.Sector.Floor.Texture == floorpic)
                    return line.Back.Sector;
            }

            return null;
        }

        private void InitSector(Sector sector)
        {
            m_world.SoundManager.CreateSectorSound(sector, MoveData.SectorMoveType, Constants.PlatMoveSound, new SoundParams(Sector, true));
            sector.ActiveMoveSpecial = this;
        }
    }
}
