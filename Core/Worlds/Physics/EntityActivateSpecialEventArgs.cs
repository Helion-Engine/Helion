﻿using Helion.Worlds.Entities;
using Helion.Worlds.Geometry.Lines;

namespace Helion.Worlds.Physics
{
    /// <summary>
    /// How to activate a special.
    /// </summary>
    public enum ActivationContext
    {
        CrossLine,
        UseLine,
        ProjectileHitLine,
        PlayerPushesWall
    }

    /// <summary>
    /// Event arguments for when a special is activated.
    /// </summary>
    public class EntityActivateSpecialEventArgs
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

        public EntityActivateSpecialEventArgs(ActivationContext activationContext, Entity entity, Line activateLineSpecial)
        {
            ActivationContext = activationContext;
            Entity = entity;
            ActivateLineSpecial = activateLineSpecial;
        }
    }
}
