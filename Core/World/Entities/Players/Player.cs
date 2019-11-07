using Helion.Render.Shared;
using Helion.Util;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Properties.Components;
using Helion.World.Entities.Inventories;
using Helion.World.Geometry.Sectors;
using Helion.World.Sound;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Players
{
    public class Player : Entity
    {
        public const double ForwardMovementSpeed = 1.5625;
        public const double SideMovementSpeed = 1.25;
        public const double MaxMovement = 30.0;
        private const double PlayerViewDivider = 8.0;
        private const int JumpDelayTicks = 7;
        
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly int PlayerNumber;
        public double PitchRadians;
        public Weapon? Weapon;
        public int LastPickupGametick = int.MinValue / 2;
        public int WeaponIndex = 1; // Temporary test variable
        private bool m_isJumping;
        private int m_jumpTics;
        private double m_prevAngle;
        private double m_prevPitch;
        private double m_viewHeight;
        private double m_prevViewHeight;
        private double m_deltaViewHeight;

        public Player(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians, 
            Sector sector, EntityManager entityManager, SoundManager soundManager, IWorld world, int playerNumber) 
            : base(id, thingId, definition, position, angleRadians, sector, entityManager, soundManager, world)
        {
            Precondition(playerNumber >= 0, "Player number should not be negative");
            
            PlayerNumber = playerNumber;
            m_prevAngle = AngleRadians;
            m_viewHeight = definition.Properties.Player.ViewHeight;
            m_prevViewHeight = m_viewHeight;
            
            AddStartItems();
        }

        public Vec3D GetViewPosition()
        {
            Vec3D position = Position;
            position.Z += m_viewHeight;
            return position;
        }

        public Vec3D GetPrevViewPosition()
        {
            Vec3D position = PrevPosition;
            position.Z += m_prevViewHeight;
            return position;
        }

        public override void SetZ(double z, bool smooth)
        {
            if (smooth && Box.Bottom < z)
            {
                m_viewHeight -= z - Box.Bottom;
                m_deltaViewHeight = (Definition.Properties.Player.ViewHeight - m_viewHeight) / PlayerViewDivider;
                ClampViewHeight();
            }
            
            base.SetZ(z, smooth);
        }

        public override void ResetInterpolation()
        {
            m_viewHeight = Definition.Properties.Player.ViewHeight;
            m_prevViewHeight = m_viewHeight;
            m_deltaViewHeight = 0;
            
            base.ResetInterpolation();
        }

        /// <summary>
        /// Sets the entity hitting the floor / another entity.
        /// </summary>
        /// <param name="hardHit">If the player hit hard and should crouch down a bit and grunt.</param>
        public void SetHitZ(bool hardHit)
        {
            // If we're airborne and just landed on the ground, we need a delay
            // for jumping. This should only happen if we've coming down from a manual jump.
            if (m_isJumping)
                m_jumpTics = JumpDelayTicks;

            m_isJumping = false;

            if (hardHit && !IsFlying)
            {
                SoundManager.CreateSoundOn(this, "DSOOF", SoundChannelType.Voice);
                m_deltaViewHeight = Velocity.Z / PlayerViewDivider;
            }
        }

        public void AddToYaw(double delta)
        {
            AngleRadians = (AngleRadians + delta) % MathHelper.TwoPi;
            if (AngleRadians < 0)
                AngleRadians += MathHelper.TwoPi;
        }
        
        public void AddToPitch(double delta)
        {
            const double notQuiteVertical = MathHelper.HalfPi - 0.001;
            PitchRadians = MathHelper.Clamp(PitchRadians + delta, -notQuiteVertical, notQuiteVertical);
        }

        public Camera GetCamera(double t)
        {
            Vec3D position = GetPrevViewPosition().Interpolate(GetViewPosition(), t);

            // When rendering, we always want the most up-to-date values. We
            // would only want to interpolate here if looking at another player
            // and would likely need to add more logic for wrapping around if
            // the player rotates from 359 degrees -> 2 degrees since that will
            // interpolate in the wrong direction.
            float yaw = (float)AngleRadians;
            float pitch = (float)PitchRadians;

            return new Camera(position.ToFloat(), yaw, pitch);
        }
        
        public override void Tick()
        {
            base.Tick();
            Weapon?.Tick();
            
            m_prevAngle = AngleRadians;
            m_prevPitch = PitchRadians;

            if (m_jumpTics > 0)
                m_jumpTics--;

            m_prevViewHeight = m_viewHeight;
            m_viewHeight += m_deltaViewHeight;

            ClampViewHeight();
        }
        
        public override void GivePickedUpItem(Entity item)
        {
            base.GivePickedUpItem(item);
            
            LastPickupGametick = World.Gametick;
        }

        private void AddStartItems()
        {
            if (Definition.Properties.Player.StartItem == null)
                return;

            foreach (PlayerStartItem item in Definition.Properties.Player.StartItem)
            {
                // TODO: If not an inventory item, don't give.
                // TODO: Give item.
            }
        }

        public void Jump()
        {
            if (AbleToJump)
            {
                m_isJumping = true;
                Velocity.Z += Properties.Player.JumpZ;
            }
        }

        private void ClampViewHeight()
        {
            double playerViewHeight = Definition.Properties.Player.ViewHeight;
            double halfPlayerViewHeight = playerViewHeight / 2;
            
            if (m_viewHeight > playerViewHeight)
            {
                m_deltaViewHeight = 0;
                m_viewHeight = playerViewHeight;
            }

            if (m_viewHeight < halfPlayerViewHeight)
                m_viewHeight = halfPlayerViewHeight;

            if (m_viewHeight < playerViewHeight)
                m_deltaViewHeight += 0.25;

            double newViewHeight = MathHelper.Clamp(m_viewHeight, 0, LowestCeilingZ - HighestFloorZ - 8);
            if (newViewHeight != m_viewHeight)
            {
                m_prevViewHeight = newViewHeight;
                m_viewHeight = newViewHeight;
            }
        }

        private bool AbleToJump => OnGround && Velocity.Z == 0 && m_jumpTics == 0;
    }
}