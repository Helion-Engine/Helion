using Helion.Models;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class SectorDamageEndSpecial : SectorDamageSpecial
{
    public SectorDamageEndSpecial(IWorld world, Sector sector, int damage)
        : base(world, sector, damage)
    {
    }

    public SectorDamageEndSpecial(IWorld world, Sector sector, SectorDamageSpecialModel model)
        : base(world, sector, model)
    {
    }

    public override SectorDamageSpecialModel ToSectorDamageSpecialModel()
    {
        SectorDamageSpecialModel model = base.ToSectorDamageSpecialModel();
        model.End = true;
        return model;
    }

    public override void Tick(Player player)
    {
        base.Tick(player);

        if (player.Health <= 10)
            m_world.ExitLevel(LevelChangeType.Next);
    }

    public override SectorDamageSpecial Copy(Sector sector) =>
        new SectorDamageEndSpecial(m_world, sector, m_damage);
}
