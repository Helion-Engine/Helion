using System.Collections.Generic;
using Helion.Audio;
using Helion.Util;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Sound;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials
{
    public class StairSpecial : SectorMoveSpecial
    {
        private readonly int m_stairHeight;
        private readonly int m_stairDelay;
        private readonly double m_startZ;
        private readonly List<StairMove> m_stairs = new List<StairMove>();
        private int m_destroyCount;
        private int m_stairDelayTics;

        private class StairMove
        {
            public StairMove (Sector sector, int height)
            {
                Sector = sector;
                Height = height;
            }

            public Sector Sector { get; private set; }
            public int Height { get; private set; }
        }

        public StairSpecial(WorldBase world, Sector sector, double speed, int height, int delay, bool crush) : 
            base(world, sector, 0, 0, new SectorMoveData(SectorPlaneType.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0), 
                new SectorSoundData(null, null, Constants.PlatStopSound, null))
        {
            m_stairHeight = height;
            m_stairDelay = delay; 
            m_startZ = Sector.Floor.Z;

            StairMove? stairMove = new StairMove(sector, m_stairHeight);

            do
            {
                stairMove.Sector.ActiveMoveSpecial = this;
                m_stairs.Add(stairMove);
                stairMove = GetNextStair(stairMove, Sector.Floor.TextureHandle);
            }
            while (stairMove != null);

            for (int i = 0; i < m_stairs.Count; i++)
                m_world.SoundManager.CreateSoundOn(m_stairs[i].Sector, Constants.PlatMoveSound, SoundChannelType.Auto, new SoundParams(m_stairs[i].Sector, true));
        }

        public override SpecialTickStatus Tick()
        {
            if (m_stairDelayTics > 0)
            {
                m_stairDelayTics--;
                return SpecialTickStatus.Continue;
            }

            SpecialTickStatus currentStatus = SpecialTickStatus.Continue;
            int height = 0;

            for (int i = 0; i < m_stairs.Count; i++)
            {
                height += m_stairs[i].Height;
                Sector = m_stairs[i].Sector;
                SectorPlane = Sector.Floor;
                DestZ = m_startZ + height;
                if (Sector.IsMoving)
                    currentStatus = base.Tick();
                else
                    SectorPlane.PrevZ = SectorPlane.Z;

                if (currentStatus == SpecialTickStatus.Destroy)
                {
                    m_destroyCount++;
                    m_stairDelayTics = m_stairDelay;
                }

                if (m_destroyCount == m_stairs.Count)
                    return SpecialTickStatus.Destroy;
            }

            return SpecialTickStatus.Continue;
        }

        public override void FinalizeDestroy()
        {
            if (m_stairs.Count > 0)
            {
                Sector = m_stairs[^1].Sector;
                Sector.Floor.PrevZ = Sector.Floor.Z;
            }
        }

        private StairMove? GetNextStair(StairMove start, int floorpic)
        {
            int height = 0;
            for (int i = 0; i < start.Sector.Lines.Count; i++)
            {
                Line line = start.Sector.Lines[i];
                if (line.Back != null && line.Front.Sector == start.Sector && line.Back.Sector.Floor.TextureHandle == floorpic)
                {
                    // The original game had this bug where it would increment height before checking if th sector was already in motion
                    height += m_stairHeight;
                    if (line.Back.Sector.ActiveMoveSpecial == null)
                        return new StairMove(line.Back.Sector, height);
                }
            }

            return null;
        }
    }
}
