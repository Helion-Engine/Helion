using Helion.Maps.Specials;
using Helion.Models;
using Helion.World.Entities;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special.Specials;

public enum DamageTickOptions
{
    None = 0,
    CheckOnFloor = 1
}

public class SectorDamageSpecial
{
    public const int DefaultDamageInterval = 32;

    public int Damage => m_damage;
    public int RadSuitLeakChance => m_radSuitLeakChance;
    public int DamageInterval => m_damageInterval;
    public bool AlwaysDamage => m_alwaysDamage;
    public readonly InstantKillEffect InstantKillEffect;

    protected readonly IWorld m_world;
    protected readonly Sector m_sector;
    protected readonly int m_damage;
    private readonly int m_radSuitLeakChance;
    private readonly int m_damageInterval;
    protected bool m_alwaysDamage;

    public SectorDamageSpecial(IWorld world, Sector sector, int damage, int radSuitLeakChance = 0, int damageInterval = DefaultDamageInterval)
    {
        m_world = world;
        m_sector = sector;
        m_damage = damage;
        m_radSuitLeakChance = radSuitLeakChance;
        m_damageInterval = damageInterval;
    }

    public SectorDamageSpecial(IWorld world, Sector sector, InstantKillEffect instantKillEffect)
    {
        m_world = world;
        m_sector = sector;
        m_damageInterval = DefaultDamageInterval;
        InstantKillEffect = instantKillEffect;
    }

    public SectorDamageSpecial(IWorld world, Sector sector, SectorDamageSpecialModel model)
    {
        m_world = world;
        m_sector = sector;
        m_damage = model.Damage;
        m_damageInterval = model.DamageInterval ?? DefaultDamageInterval;
        m_radSuitLeakChance = model.RadSuitLeak;
        InstantKillEffect = model.InstantKillEffect;
    }

    public static SectorDamageSpecial CreateNoDamage(IWorld world, Sector sector) =>
        new(world, sector, 0, 0);

    public virtual SectorDamageSpecialModel ToSectorDamageSpecialModel()
    {
        return new SectorDamageSpecialModel()
        {
            SectorId = m_sector.Id,
            Damage = m_damage,
            DamageInterval = m_damageInterval,
            RadSuitLeak = m_radSuitLeakChance,
            InstantKillEffect = InstantKillEffect,
            End = false,
        };
    }

    public virtual void Tick(Entity entity, DamageTickOptions options)
    {
        if (entity.IsDisposed)
            return;

        if (InstantKillEffect != InstantKillEffect.None)
        {
            CheckInstantKillEffect(entity);
            return;
        }

        if (entity.PlayerObj == null || entity.PlayerObj.IsVooDooDoll)
            return;

        var player = entity.PlayerObj;
        if (!ShouldDamage(player, options))
            return;

        if (m_alwaysDamage || !player.Inventory.IsPowerupActive(PowerupType.IronFeet) || (m_radSuitLeakChance > 0 && m_world.Random.NextByte() < m_radSuitLeakChance))
            m_world.DamageEntity(player, null, m_damage, DamageType.Normal, sectorSource: m_sector);
    }

    protected bool ShouldDamage(Entity entity, DamageTickOptions options)
    {
        var shouldDamage = options switch
        {
            DamageTickOptions.CheckOnFloor => entity.OnSectorFloorZ(m_sector),
            _ => true,
        };
        return m_damageInterval > 0 && m_damage > 0 && shouldDamage && (m_world.LevelTime % m_damageInterval) == 0;
    }

    private void CheckInstantKillEffect(Entity entity)
    {
        var isPlayer = entity.IsPlayer;
        if (!isPlayer && (InstantKillEffect & InstantKillEffect.KillMonsters) == 0)
            return;

        if (isPlayer)
        {
            if (!entity.OnSectorFloorZ(m_sector))
                return;
        }
        else
        {
            // The entity doesn't need to be on the kill sector floor z, but the highest floor z.
            if (entity.HighestFloorSector.Floor.Z > entity.Position.Z)
                return;
        }

        m_world.SectorInstantKillEffect(entity, InstantKillEffect);
    }

    public virtual SectorDamageSpecial Copy(Sector sector)
    {
        if (InstantKillEffect != InstantKillEffect.None)
            return new SectorDamageSpecial(m_world, sector, InstantKillEffect);

        return new(m_world, sector, m_damage, m_radSuitLeakChance);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SectorDamageSpecial damage)
            return false;

        return damage.Damage == Damage &&
            damage.m_damageInterval == m_damageInterval &&
            damage.RadSuitLeakChance == RadSuitLeakChance &&
            damage.AlwaysDamage == AlwaysDamage &&
            damage.m_sector.Id == m_sector.Id &&
            damage.InstantKillEffect == InstantKillEffect;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
