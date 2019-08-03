using Helion.Maps.Geometry.Lines;
using Helion.World.Entities;

namespace Helion.World.Physics
{
    /// <summary>
    /// How to activate a special.
    /// </summary>
    public enum ActivationContext
    {
        CrossLine,
        UseLine,
    }

    /// <summary>
    /// Event arguments for when a special is activated.
    /// </summary>
    public class EntityActivateSpecialEventArgs
    {
        /// <summary>
        /// How the special was activated.
        /// </summary>
        public ActivationContext ActivationContext;

        /// <summary>
        /// Entity that activated the special.
        /// </summary>
        public Entity Entity;

        /// <summary>
        /// The line that activated the special if applicable.
        /// </summary>
        public Line? ActivateLineSpecial;
    }
}
