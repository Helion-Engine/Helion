using System;
using Helion.Input;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using OpenTK;
using Vector2 = System.Numerics.Vector2;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        public readonly Player Player;
        
        private SinglePlayerWorld(Config config, ArchiveCollection archiveCollection, Map map, BspTree bspTree) : 
            base(config, archiveCollection, map, bspTree)
        {
            Player = EntityManager.CreatePlayer(1);
        }

        public static SinglePlayerWorld? Create(Config config, ArchiveCollection archiveCollection, Map map, 
            MapEntryCollection? mapEntryCollection)
        {
            BspTree? bspTree = BspTree.Create(map, mapEntryCollection);
            return bspTree != null ? new SinglePlayerWorld(config, archiveCollection, map, bspTree) : null;
        }

        public void HandleFrameInput(ConsumableInput frameInput)
        {
            Vec2I pixelsMoved = frameInput.ConsumeMouseDelta();
            Vector2 moveDelta = pixelsMoved.ToFloat() / (float)Config.Engine.Mouse.PixelDivisor;
            moveDelta.X *= (float)(Config.Engine.Mouse.Sensitivity * Config.Engine.Mouse.Yaw);
            moveDelta.Y *= (float)(Config.Engine.Mouse.Sensitivity * Config.Engine.Mouse.Pitch);

            Player.AddToYaw(moveDelta.X);

            if (Config.Engine.Mouse.MouseLook)
                Player.AddToPitch(moveDelta.Y);
        }

        public void HandleTickCommand(TickCommand tickCommand)
        {
            Entity entity = Player.Entity;

            Vec2D movement = Vec2D.Zero;
            if (tickCommand.Has(TickCommands.Forward))
                movement += CalculateForwardMovement(entity);
            if (tickCommand.Has(TickCommands.Backward))
                movement -= CalculateForwardMovement(entity);
            if (tickCommand.Has(TickCommands.Right))
                movement += CalculateStrafeRightMovement(entity);
            if (tickCommand.Has(TickCommands.Left))
                movement -= CalculateStrafeRightMovement(entity);

            if (movement != Vec2D.Zero)
            {
                entity.Velocity.X += Math.Clamp(movement.X, -Player.MaxMovement, Player.MaxMovement);
                entity.Velocity.Y += Math.Clamp(movement.Y, -Player.MaxMovement, Player.MaxMovement);
            }

            if (tickCommand.Has(TickCommands.Jump) && Player.AbleToJump())
                entity.Velocity.Z += Player.JumpZ;
        }

        private static Vec2D CalculateForwardMovement(Entity entity)
        {
            double x = Math.Cos(entity.Angle) * Player.ForwardMovementSpeed;
            double y = Math.Sin(entity.Angle) * Player.ForwardMovementSpeed;
            return new Vec2D(x, y);
        }
        
        private static Vec2D CalculateStrafeRightMovement(Entity entity)
        {
            double rightRotateAngle = entity.Angle - MathHelper.PiOver2;
            double x = Math.Cos(rightRotateAngle) * Player.SideMovementSpeed;
            double y = Math.Sin(rightRotateAngle) * Player.SideMovementSpeed;
            return new Vec2D(x, y);
        }
    }
}