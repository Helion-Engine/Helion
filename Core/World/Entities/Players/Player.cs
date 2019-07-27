using System;
using Helion.Render.Shared;
using Helion.Util;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Players
{
    public class Player
    {
        public const double ForwardMovementSpeed = 1.5625;
        public const double SideMovementSpeed = 1.25;
        public const double MaxMovement = 30.0;
        public const double JumpZ = 8.0;
        
        public readonly int PlayerNumber;
        public Entity Entity;
        public double Pitch;
        private double m_prevAngle;
        private double m_prevPitch;
        
        public Player(int playerNumber, Entity entity)
        {
            Precondition(playerNumber >= 0, "Player number should not be negative");
            
            PlayerNumber = playerNumber;
            Entity = entity;
            m_prevAngle = entity.Angle;
        }
        
        public void AddToYaw(double delta)
        {
            Entity.Angle = (Entity.Angle + delta) % MathHelper.TwoPi;
            if (Entity.Angle < 0)
                Entity.Angle += MathHelper.TwoPi;
        }
        
        public void AddToPitch(double delta)
        {
            const double notQuiteVertical = MathHelper.HalfPi - 0.001;
            Pitch = Math.Clamp(Pitch + delta, -notQuiteVertical, notQuiteVertical);
        }

        public Camera GetCamera(double t)
        {
            Vec3D position = Entity.PrevPosition.Interpolate(Entity.Position, t);
            
            // When rendering, we always want the most up-to-date values. We
            // would only want to interpolate here if looking at another player
            // and would likely need to add more logic for wrapping around if
            // the player rotates from 359 degrees -> 2 degrees since that will
            // interpolate in the wrong direction.
            float yaw = (float)Entity.Angle;
            float pitch = (float)Pitch;
            
            // TODO: This should be clamped to the floor/ceiling and use the
            //       property for the player.
            position.Z += 42.0;
                
            return new Camera(position.ToFloat(), yaw, pitch);
        }
        
        public void Tick()
        {
            m_prevAngle = Entity.Angle;
            m_prevPitch = Pitch;
        }
        
        public bool AbleToJump() => Entity.OnGround && Entity.Velocity.Z == 0 && Entity.JumpDelayTicks == 0;
    }
}