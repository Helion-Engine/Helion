using Helion.Maps.Specials.Vanilla;
using Helion.Resources.Definitions.MapInfo;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World.Special
{
    class MonsterCountSpecial : ISpecial
    {
        public readonly int SectorTag;
        public readonly MapSpecialAction MapSpecialAction;

        private readonly IWorld m_world;
        private readonly SpecialManager m_specailManager;
        private readonly int m_countId;

        public MonsterCountSpecial(IWorld world, SpecialManager specialManager, int countId, int sectorTag, 
            MapSpecialAction mapSpecialAction)
        {
            MapSpecialAction = mapSpecialAction;
            SectorTag = sectorTag;
            m_world = world;
            m_specailManager = specialManager;
            m_countId = countId;
        }

        public SpecialTickStatus Tick()
        {
            if (m_world.EntityAliveCount(m_countId, true) == 0)
            {
                ExecuteSpecial();
                return SpecialTickStatus.Destroy;
            }

            return SpecialTickStatus.Continue;
        }

        private void ExecuteSpecial()
        {
            List<Sector> sectors = m_world.Sectors.Where(x => x.Tag == SectorTag).ToList();

            switch (MapSpecialAction)
            {
                case MapSpecialAction.LowerFloor:
                    foreach (var sector in sectors)
                        m_specailManager.AddSpecial(m_specailManager.CreateFloorLowerSpecial(sector, SectorDest.LowestAdjacentFloor,
                            VanillaConstants.SectorSlowSpeed * SpecialManager.SpeedFactor));
                    break;
                case MapSpecialAction.OpenDoor:
                    foreach (var sector in sectors)
                        m_specailManager.AddSpecial(m_specailManager.CreateDoorOpenStaySpecial(sector,
                            VanillaConstants.DoorSlowSpeed * SpecialManager.SpeedFactor));
                    break;
                case MapSpecialAction.FloorRaiseByLowestTexture:
                    foreach (var sector in sectors)
                        m_specailManager.AddSpecial(m_specailManager.CreateFloorRaiseByTextureSpecial(sector,
                            VanillaConstants.SectorSlowSpeed * SpecialManager.SpeedFactor));
                    break;
                case MapSpecialAction.ExitLevel:
                    m_world.ExitLevel(LevelChangeType.Next);
                    break;
            }
        }

        public void Use(Entity entity)
        {
            // Not needed
        }
    }
}
