using Helion.Worlds.Entities.Players;
using Helion.Worlds.Geometry.Sectors;

namespace Helion.Worlds.Special.Specials
{
    public class SectorDamageEndSpecial : SectorDamageSpecial
    {
        public SectorDamageEndSpecial(World world, Sector sector, int damage)
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
