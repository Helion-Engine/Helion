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
public readonly struct EntityActivateSpecial(ActivationContext activationContext, Entity entity, Line activateLineSpecial, bool fromFront)
{
    /// <summary>
    /// How the special was activated.
    /// </summary>
    public readonly ActivationContext ActivationContext = activationContext;

    /// <summary>
    /// Entity that activated the special.
    /// </summary>
    public readonly Entity Entity = entity;

    /// <summary>
    /// The line that activated the special if applicable.
    /// </summary>
    public readonly Line ActivateLineSpecial = activateLineSpecial;

    public readonly bool FromFront = fromFront;
}
