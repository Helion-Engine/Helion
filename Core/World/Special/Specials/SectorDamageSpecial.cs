using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;

namespace Helion.World.Special.Specials
{
    public class SectorDamageSpecial
    {
        protected readonly WorldBase m_world;
        private readonly Sector m_sector;
        private readonly int m_damage;

        public SectorDamageSpecial(WorldBase world, Sector sector, int damage)
        {
            m_world = world;
            m_sector = sector;
            m_damage = damage;
        }

        public virtual void Tick(Player player)
        {
            if (player.Position.Z != m_sector.ToFloorZ(player.Position) || (m_world.Gametick & 31) != 0)
                return;

            m_world.PhysicsManager.DamageEntity(player, null, m_damage);
        }
    }
}
