using System.Collections.Generic;
using Helion.Audio;
using Helion.Models;
using Helion.Util;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Sound;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials
{
    public class StairSpecial : SectorMoveSpecial
    {
        private readonly int m_stairDelay;
        private readonly double m_startZ;
        private readonly List<StairMove> m_stairs = new List<StairMove>();
        private readonly bool m_crush;
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

        public StairSpecial(IWorld world, Sector sector, double speed, int height, int delay, bool crush) : 
            base(world, sector, 0, 0, new SectorMoveData(SectorPlaneType.Floor, MoveDirection.Up, MoveRepetition.None, speed, 0), 
                new SectorSoundData(null, null, Constants.PlatStopSound, null))
        {
            m_stairDelay = delay;
            m_startZ = Sector.Floor.Z;
            m_crush = crush;

            StairMove? stairMove = new StairMove(sector, height);

            do
            {
                if (stairMove.Sector.ActiveMoveSpecial == null || ReferenceEquals(stairMove.Sector.ActiveMoveSpecial, this))
                {
                    stairMove.Sector.ActiveMoveSpecial = this;
                    CreateMovementSound(stairMove.Sector);
                    m_stairs.Add(stairMove);
                }
                stairMove = GetNextStair(stairMove, Sector.Floor.TextureHandle, height);
            }
            while (stairMove != null);
        }

        public StairSpecial(IWorld world, Sector sector, StairSpecialModel model)  :
            base(world, sector, model.MoveSpecial)
        {
            m_stairDelay = model.Delay;
            m_startZ = model.StartZ;
            m_destroyCount = model.Destroy;
            m_stairDelayTics = model.DelayTics;
            m_crush = model.Crush;

            for (int i = 0; i < model.SectorIds.Count && i < model.Heights.Count; i++)
            {
                if (!world.IsSectorIdValid(model.SectorIds[i]))
                    continue;

                m_stairs.Add(new StairMove(world.Sectors[model.SectorIds[i]], model.Heights[i]));

                if (i >= m_destroyCount)
                {
                    m_stairs[i].Sector.ActiveMoveSpecial = this;
                    CreateMovementSound(m_stairs[i].Sector);
                }
            }
        }

        public override ISpecialModel ToSpecialModel()
        {
            StairSpecialModel model = new StairSpecialModel()
            {
                Delay = m_stairDelay,
                StartZ = m_startZ,
                Destroy = m_destroyCount,
                DelayTics = m_stairDelayTics,
                Crush = m_crush,
                MoveSpecial = (SectorMoveSpecialModel)base.ToSpecialModel()
            };

            List<int> sectors = new List<int>(m_stairs.Count);
            List<int> heights = new List<int>(m_stairs.Count);

            for (int i = 0; i < m_stairs.Count; i++)
            {
                sectors.Add(m_stairs[i].Sector.Id);
                heights.Add(m_stairs[i].Height);
            }

            model.SectorIds = sectors;
            model.Heights = heights;

            return model;
        }

        public override SpecialTickStatus Tick()
        {
            if (m_stairDelayTics > 0)
            {
                for (int i = m_destroyCount - 1; i < m_stairs.Count; i++)
                {
                    if (!ReferenceEquals(m_stairs[i].Sector.ActiveMoveSpecial, this))
                        continue;

                    m_stairs[i].Sector.Floor.PrevZ = m_stairs[i].Sector.Floor.Z;
                }

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
                if (ReferenceEquals(Sector.ActiveMoveSpecial, this))
                    currentStatus = base.Tick();                    

                if (currentStatus == SpecialTickStatus.Destroy)
                {
                    SectorPlane.PrevZ = SectorPlane.Z;
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

        private static StairMove? GetNextStair(StairMove start, int floorpic, int stairHeight)
        {
            int height = 0;
            for (int i = 0; i < start.Sector.Lines.Count; i++)
            {
                Line line = start.Sector.Lines[i];
                if (line.Back != null && line.Front.Sector == start.Sector && line.Back.Sector.Floor.TextureHandle == floorpic)
                {
                    // The original game had this bug where it would increment height before checking if th sector was already in motion
                    height += stairHeight;
                    if (line.Back.Sector.ActiveMoveSpecial == null)
                        return new StairMove(line.Back.Sector, height);
                }
            }

            return null;
        }

        private void CreateMovementSound(Sector sector) =>
            m_world.SoundManager.CreateSoundOn(sector, Constants.PlatMoveSound, SoundChannelType.Auto, new SoundParams(sector, true));
    }
}
