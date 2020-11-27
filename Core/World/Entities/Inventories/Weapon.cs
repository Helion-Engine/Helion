using System;
using Helion.Util;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Inventories
{
    /// <summary>
    /// A weapon that can be fired by some player.
    /// </summary>
    public class Weapon : InventoryItem, ITickable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        /// <summary>
        /// The current state of the weapon.
        /// </summary>
        public readonly FrameState FrameState;
        public readonly FrameState FlashState;
        
        /// <summary>
        /// True if this gun is eligible to fire, false if not.
        /// </summary>
        /// <remarks>
        /// Intended to be set by something like A_WeaponReady. We need some
        /// method of having an action function communicate with the weapon,
        /// and this is the best option currently due to the static nature of
        /// action functions.
        /// </remarks>
        public bool ReadyToFire;
        
        /// <summary>
        /// The amount of height this weapon has been raised in [0.0, 1.0]. A
        /// value of 0.0 means it is not visible and not raised, 1.0 is fully
        /// raised.
        /// </summary>
        public double RaiseFraction
        {
            get => m_raiseFraction;
            set => m_raiseFraction = Math.Clamp(value, 0.0, 1.0);
        }
        
        /// <summary>
        /// An interpolatable value for the previous raise fraction value.
        /// </summary>
        public double PrevRaiseFraction { get; private set; }

        private bool m_tryingToFire;
        private double m_raiseFraction;

        public Weapon(EntityDefinition definition, Player owner, EntityManager entityManager) :
            base(definition, 1)
        {
            Precondition(definition.IsType(EntityDefinitionType.Weapon), "Trying to create a weapon from a non-weapon type");

            FrameState = new FrameState(owner, definition, entityManager);
            FlashState = new FrameState(owner, definition, entityManager, false);
            
            if (!FrameState.SetState(FrameStateLabel.Ready))
                Log.Warn("Unable to find Ready state for weapon {0}", definition.Name);
        }

        /// <summary>
        /// Requests that the gun fire.
        /// </summary>
        /// <remarks>
        /// A request does not mean the action will take place, this is just a
        /// notification to the weapon that the owner of it wants it to attempt
        /// to start firing if it is not already.
        /// </remarks>
        public void RequestFire()
        {
            m_tryingToFire = true;
        }

        public void SetFireState()
        {
            FrameState.SetState("FIRE");
        }

        public void SetFlashState()
        {
            FlashState.SetState("FLASH");
        }

        public void Tick()
        {
            PrevRaiseFraction = m_raiseFraction;
            
            if (m_tryingToFire && ReadyToFire)
                SetToFireState();

            // TODO: Weapon raise/lower.

            ReadyToFire = false;
            m_tryingToFire = false; 
            
            FrameState.Tick();
            FlashState.Tick();
        }
        
        private void SetToFireState()
        {
            if (!FrameState.SetState(FrameStateLabel.Fire))
                Log.Warn("Unable to find Fire state for weapon {0}", Definition.Name);
        }
    }
}