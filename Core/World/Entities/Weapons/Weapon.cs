using Helion.Util;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Players;

namespace Helion.World.Entities.Weapons
{
    public class Weapon : ITickable
    {
        public EntityDefinition Definition;
        private bool m_tryingToFire;
        private Player m_owner;

        public Weapon(EntityDefinition definition, Player owner)
        {
            // TODO: Check that it extends from Weapon.
            Definition = definition;
            m_owner = owner;
        }

        /// <summary>
        /// Requests that the gun fire.
        /// </summary>
        /// <remarks>
        /// A request does not mean the action will take place, this is just a
        /// notification to the weapon that the owner of it wants it to attempt
        /// to start firing if it is not already.
        /// </remarks>
        public void Fire()
        {
            m_tryingToFire = true;
        }

        public void Tick()
        {
            if (m_tryingToFire && ReadyToBeFired())
            {
                // TODO
            }

            // TODO: Weapon raise/lower.

            m_tryingToFire = false;
        }

        private bool ReadyToBeFired()
        {
            // TODO: Check if at state with A_WeaponReady.
            return false;
        }
    }
}