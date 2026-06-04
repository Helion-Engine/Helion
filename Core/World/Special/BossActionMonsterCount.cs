using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Definitions.MapInfo;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special;

public class BossActionMonsterCount : IMonsterCounterSpecial
{
    public int EntityDefinitionId { get; private set; }

    private readonly IWorld m_world;
    private readonly BossAction m_bossAction;

    public BossActionMonsterCount(IWorld world, BossAction bossAction, int entityDefinitionId)
    {
        m_world = world;
        m_bossAction = bossAction;
        EntityDefinitionId = entityDefinitionId;
    }

    public SpecialTickStatus Tick(Entity? ignoreEntity)
    {
        if (m_world.EntityAliveCount(EntityDefinitionId, EntityManager.NoTid, Sector.NoTag, ignoreEntity) == 0)
            ExecuteSpecial();

        return SpecialTickStatus.Continue;
    }

    private void ExecuteSpecial()
    {
        ZDoomLineSpecialType specialType = ZDoomLineSpecialType.None;
        LineSpecialCompatibility compat = LineSpecialCompatibility.Default;
        SpecialArgs specialArgs = new();
        var flags = new LineFlags(MapLineFlags.Doom(0));

        if (m_bossAction.Action.HasValue)
        {
            specialType = VanillaLineSpecTranslator.Translate(ref flags, (VanillaLineSpecialType)m_bossAction.Action,
                m_bossAction.Tag, ref specialArgs, out _, out compat);
        }
        else if (m_bossAction.ZDoomAction.HasValue)
        {
            specialType = m_bossAction.ZDoomAction.Value;
            specialArgs = m_bossAction.ZDoomSpecialArgs;
        }

        if (specialType == ZDoomLineSpecialType.None)
            return;

        m_world.SpecialManager.AddActivatedLineSpecial(m_world.Player, specialType, specialArgs, compat);
    }
}
