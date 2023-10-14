using Helion.World.Entities;
using Helion.World.Entities.Definition.States;

namespace Helion.World;

internal struct HealChaseData
{
    public Entity HealEntity;
    public EntityFrame HealState;
    public string HealSound;
    public bool Healed;
}
