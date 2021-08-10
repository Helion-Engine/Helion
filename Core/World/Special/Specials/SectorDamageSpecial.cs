using Helion.Models;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class SectorDamageSpecial
    {
        protected readonly IWorld m_world;
        protected readonly Sector m_sector;
        protected readonly int m_damage;
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

        public static SectorDamageSpecial CreateNoDamage(IWorld world, Sector sector) =>
            new(world, sector, 0, 0);

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
            if (player.Position.Z != m_sector.ToFloorZ(player.Position) || (m_world.Gametick & 31) != 0 || m_damage == 0)
                return;

            if (!player.Inventory.IsPowerupActive(PowerupType.IronFeet) || (m_radSuitLeakChance > 0 && m_world.Random.NextByte() < m_radSuitLeakChance))
                m_world.DamageEntity(player, null, m_damage, false, sectorSource: m_sector);
        }

        public virtual SectorDamageSpecial Copy(Sector sector) =>
            new SectorDamageSpecial(m_world, sector, m_damage, m_radSuitLeakChance);
    }
}
