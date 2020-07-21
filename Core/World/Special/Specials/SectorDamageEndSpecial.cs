using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials
{
    public class SectorDamageEndSpecial : SectorDamageSpecial
    {
        public SectorDamageEndSpecial(WorldBase world, Sector sector, int damage)
            : base(world, sector, damage)
        {
        }

        public override void Tick(Player player)
        {
            base.Tick(player);

            if (player.Health <= 10)
                m_world.ExitLevel(LevelChangeType.Next);
        }
    }
}
