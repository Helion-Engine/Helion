using Helion.World.Entities;
using Helion.World.Geometry.Lines;

namespace Helion.World.Physics;

/// <summary>
/// How to activate a special.
/// </summary>
public enum ActivationContext
{
    CrossLine,
    UseLine,
    HitscanCrossLine,
    HitscanImpactsWall,
    EntityImpactsWall
}

/// <summary>
/// Event arguments for when a special is activated.
/// </summary>
public readonly struct EntityActivateSpecial
{
    /// <summary>
    /// How the special was activated.
    /// </summary>
    public readonly ActivationContext ActivationContext;

    /// <summary>
    /// Entity that activated the special.
    /// </summary>
    public readonly Entity Entity;

    /// <summary>
    /// The line that activated the special if applicable.
    /// </summary>
    public readonly Line ActivateLineSpecial;

    public EntityActivateSpecial(ActivationContext activationContext, Entity entity, Line activateLineSpecial)
    {
        ActivationContext = activationContext;
        Entity = entity;
        ActivateLineSpecial = activateLineSpecial;
    }
}
