using Helion.Models;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class SectorDamageEndSpecial : SectorDamageSpecial
{
    public SectorDamageEndSpecial(IWorld world, Sector sector, int damage)
        : base(world, sector, damage)
    {
        m_alwaysDamage = true;
    }

    public SectorDamageEndSpecial(IWorld world, Sector sector, SectorDamageSpecialModel model)
        : base(world, sector, model)
    {
        m_alwaysDamage = true;
    }

    public override SectorDamageSpecialModel ToSectorDamageSpecialModel()
    {
        SectorDamageSpecialModel model = base.ToSectorDamageSpecialModel();
        model.End = true;
        return model;
    }

    public override void Tick(Entity entity)
    {
        if (entity.PlayerObj == null)
            return;

        var player = entity.PlayerObj;
        m_world.CheatManager.DeactivateCheat(player, CheatType.God);

        if (!ShouldDamage(player))
            return;

        m_world.DamageEntity(player, null, m_damage, DamageType.Normal, sectorSource: m_sector);
        if (player.Health <= 10)
            m_world.ExitLevel(LevelChangeType.Next);
    }

    public override SectorDamageSpecial Copy(Sector sector) =>
        new SectorDamageEndSpecial(m_world, sector, m_damage);
}
