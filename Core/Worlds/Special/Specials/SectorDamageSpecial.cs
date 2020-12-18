using Helion.Worlds.Entities.Players;
using Helion.Worlds.Geometry.Sectors;

namespace Helion.Worlds.Special.Specials
{
    public class SectorDamageSpecial
    {
        protected readonly World m_world;
        private readonly Sector m_sector;
        private readonly int m_damage;

        public SectorDamageSpecial(World world, Sector sector, int damage)
        {
            m_world = world;
            m_sector = sector;
            m_damage = damage;
        }

        public virtual void Tick(Player player)
        {
            if (player.Position.Z != m_sector.ToFloorZ(player.Position) || (m_world.Gametick & 31) != 0)
                return;

            m_world.DamageEntity(player, null, m_damage);
        }
    }
}
