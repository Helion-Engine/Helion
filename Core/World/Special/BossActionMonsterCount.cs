using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using System;

namespace Helion.World.Special;

public class BossActionMonsterCount : IMonsterCounterSpecial
{
    public int EntityDefinitionId { get; private set; }

    private readonly IWorld m_world;
    private readonly BossAction m_bossAction;
    private bool m_destroyed;

    public BossActionMonsterCount(IWorld world, BossAction bossAction, int entityDefinitionId)
    {
        m_world = world;
        m_bossAction = bossAction;
        EntityDefinitionId = entityDefinitionId;
    }

    public SpecialTickStatus Tick()
    {
        if (m_destroyed)
            return SpecialTickStatus.Destroy;

        if (m_world.EntityAliveCount(EntityDefinitionId) == 0)
        {
            m_destroyed = true;
            ExecuteSpecial();
            return SpecialTickStatus.Destroy;
        }

        return SpecialTickStatus.Continue;
    }

    private void ExecuteSpecial()
    {
        ZDoomLineSpecialType specialType = ZDoomLineSpecialType.None;
        LineActivationType activationType = LineActivationType.Tag;
        LineSpecialCompatibility compat = LineSpecialCompatibility.Default;
        SpecialArgs specialArgs = new();
        var flags = new LineFlags(MapLineFlags.Doom(0));

        if (m_bossAction.Action.HasValue)
        {
            specialType = VanillaLineSpecTranslator.Translate(ref flags, (VanillaLineSpecialType)m_bossAction.Action,
                m_bossAction.Tag, ref specialArgs, out activationType, out compat);
        }
        else if (m_bossAction.ZDoomAction.HasValue)
        {
            specialType = m_bossAction.ZDoomAction.Value;
            specialArgs = m_bossAction.ZDoomSpecialArgs;
        }

        if (specialType == ZDoomLineSpecialType.None)
            return;

        activationType = LineActivationType.Tag;
        LineSpecial lineSpecial = new(specialType, activationType, compat);
        EntityActivateSpecial args = new(ActivationContext.CrossLine, m_world.Player, CreateDummyLine(flags, lineSpecial, specialArgs, m_world.Sectors[0]));
        m_world.SpecialManager.TryAddActivatedLineSpecial(args);
    }

    public static Line CreateDummyLine(LineFlags flags, LineSpecial special, SpecialArgs args, Sector sector)
    {
        var wall = new Wall(Constants.NoTextureIndex, WallLocation.Middle);
        var side = new Side(0, Vec2I.Zero, wall, wall, wall, sector);
        var seg = new Seg2D(Vec2D.Zero, Vec2D.One);
        return new Line(0, seg, side, null, flags, special, args);
    }

    public bool Use(Entity entity)
    {
        return false;
    }
}
