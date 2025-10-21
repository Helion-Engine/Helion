using Helion.World.Entities;

namespace Helion.World.Special;

public interface IMonsterCounterSpecial
{
    SpecialTickStatus Tick(Entity? ignoreEntity);
    int EntityDefinitionId { get; }
}
