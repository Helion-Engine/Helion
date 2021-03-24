using Helion.Models;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class SectorDamageSpecial
    {
        protected readonly IWorld m_world;
        private readonly Sector m_sector;
        private readonly int m_damage;
        private readonly int m_radSuitLeakChance;

        public SectorDamageSpecial(IWorld world, Sector sector, int damage, int radSuitLeakChance = 0)
        {
            m_world = world;
            m_sector = sector;
            m_damage = damage;
            m_radSuitLeakChance = radSuitLeakChance;
        }

        public SectorDamageSpecial(IWorld world, Sector sector, SectorDamageSpecialModel sectorDamageSpecialModel)
        {
            m_world = world;
            m_sector = sector;
            m_damage = sectorDamageSpecialModel.Damage;
            m_radSuitLeakChance = sectorDamageSpecialModel.RadSuitLeak;
        }

        public virtual SectorDamageSpecialModel ToSectorDamageSpecialModel()
        {
            return new SectorDamageSpecialModel()
            {
                SectorId = m_sector.Id,
                Damage = m_damage,
                RadSuitLeak = m_radSuitLeakChance
            };
        }

        public virtual void Tick(Player player)
        {
            if (player.Position.Z != m_sector.ToFloorZ(player.Position) || (m_world.Gametick & 31) != 0)
                return;

            if (!player.Inventory.IsPowerupActive(PowerupType.IronFeet) || (m_radSuitLeakChance > 0 && m_world.Random.NextByte() < m_radSuitLeakChance))
                m_world.DamageEntity(player, null, m_damage, sectorSource: m_sector);
        }
    }
}
