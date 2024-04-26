using Helion.Maps.Specials.Vanilla;
using Helion.Resources.Definitions.MapInfo;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World.Special;

public class MonsterCountSpecial : IMonsterCounterSpecial
{
    public int EntityDefinitionId { get; set; }
    public readonly int SectorTag;
    public readonly MapSpecialAction MapSpecialAction;

    private readonly IWorld m_world;
    private readonly SpecialManager m_specailManager;

    public MonsterCountSpecial(IWorld world, SpecialManager specialManager, int entityDefinitionId, int sectorTag,
        MapSpecialAction mapSpecialAction)
    {
        MapSpecialAction = mapSpecialAction;
        SectorTag = sectorTag;
        m_world = world;
        m_specailManager = specialManager;
        EntityDefinitionId = entityDefinitionId;
    }

    public SpecialTickStatus Tick()
    {
        if (m_world.EntityAliveCount(EntityDefinitionId) == 0)
        {
            ExecuteSpecial();
            return SpecialTickStatus.Destroy;
        }

        return SpecialTickStatus.Continue;
    }

    public void ExecuteSpecial()
    {
        var sectors = m_world.FindBySectorTag(SectorTag);
        switch (MapSpecialAction)
        {
            case MapSpecialAction.LowerFloor:
                foreach (var sector in sectors)
                    m_specailManager.AddSpecial(m_specailManager.CreateFloorLowerSpecial(sector, SectorDest.LowestAdjacentFloor,
                        VanillaConstants.SectorSlowSpeed * SpecialManager.SpeedFactor));
                break;
            case MapSpecialAction.OpenDoor:
                foreach (var sector in sectors)
                    m_specailManager.AddSpecial(m_specailManager.CreateDoorOpenStaySpecial(sector,
                        VanillaConstants.DoorSlowSpeed * SpecialManager.SpeedFactor));
                break;
            case MapSpecialAction.FloorRaiseByLowestTexture:
                foreach (var sector in sectors)
                    m_specailManager.AddSpecial(m_specailManager.CreateFloorRaiseByTextureSpecial(sector,
                        VanillaConstants.SectorSlowSpeed * SpecialManager.SpeedFactor));
                break;
            case MapSpecialAction.ExitLevel:
                m_world.ExitLevel(LevelChangeType.Next);
                break;
        }
    }

    public bool IsFloorMove() => MapSpecialAction == MapSpecialAction.LowerFloor || MapSpecialAction == MapSpecialAction.FloorRaiseByLowestTexture;
    public bool IsCeilingMove() => MapSpecialAction == MapSpecialAction.OpenDoor;

    public bool Use(Entity entity)
    {
        return false;
    }
}
