using Helion.Models;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public class SectorDamageSpecial
{
    public int Damage => m_damage;
    public int RadSuitLeakChance => m_radSuitLeakChance;
    public bool AlwaysDamage => m_alwaysDamage;

    protected readonly IWorld m_world;
    protected readonly Sector m_sector;
    protected readonly int m_damage;
    private readonly int m_radSuitLeakChance;
    protected bool m_alwaysDamage;

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
        if (!player.OnSectorFloorZ(m_sector) || (m_world.LevelTime & 31) != 0 || m_damage == 0)
            return;

        if (m_alwaysDamage || !player.Inventory.IsPowerupActive(PowerupType.IronFeet) || (m_radSuitLeakChance > 0 && m_world.Random.NextByte() < m_radSuitLeakChance))
            m_world.DamageEntity(player, null, m_damage, DamageType.Normal, sectorSource: m_sector);
    }

    public virtual SectorDamageSpecial Copy(Sector sector) =>
        new(m_world, sector, m_damage, m_radSuitLeakChance);

    public override bool Equals(object? obj)
    {
        if (obj is not SectorDamageSpecial damage)
            return false;

        return damage.Damage == Damage &&
            damage.RadSuitLeakChance == RadSuitLeakChance &&
            damage.AlwaysDamage == AlwaysDamage &&
            damage.m_sector.Id == m_sector.Id;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
