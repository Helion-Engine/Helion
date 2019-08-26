using System;
using Helion.Cheats;
using Helion.Input;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Physics;
using OpenTK;
using Vector2 = System.Numerics.Vector2;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        private const double AirControl = 0.00390625;

        public readonly Player Player;
        
        private SinglePlayerWorld(Config config, ArchiveCollection archiveCollection, IMap map, BspTree bspTree) : 
            base(config, archiveCollection, map, bspTree)
        {
            Player = EntityManager.CreatePlayer(1);
            CheatManager.Instance.CheatActivationChanged += Instance_CheatActivationChanged;
            CheatManager.Instance.ActivateToggleCheats();

            PhysicsManager.EntityActivatedSpecial += PhysicsManager_EntityActivatedSpecial;
            PhysicsManager.PlayerUseFail += PhysicsManager_PlayerUseFail;
        }

        public static SinglePlayerWorld? Create(Config config, ArchiveCollection archiveCollection, IMap map, 
            MapEntryCollection? mapEntryCollection)
        {
            BspTree? bspTree = BspTree.Create(map, mapEntryCollection);
            return bspTree != null ? new SinglePlayerWorld(config, archiveCollection, map, bspTree) : null;
        }

        public void HandleFrameInput(ConsumableInput frameInput)
        {
            if (Player.Entity.IsFrozen)
                return;

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
            if (Player.Entity.IsFrozen)
                return;

            Entity entity = Player.Entity;

            Vec3D movement = Vec3D.Zero;
            if (tickCommand.Has(TickCommands.Forward))
                movement += CalculateForwardMovement(entity);
            if (tickCommand.Has(TickCommands.Backward))
                movement -= CalculateForwardMovement(entity);
            if (tickCommand.Has(TickCommands.Right))
                movement += CalculateStrafeRightMovement(entity);
            if (tickCommand.Has(TickCommands.Left))
                movement -= CalculateStrafeRightMovement(entity);

            if (tickCommand.Has(TickCommands.Jump))
            {
                if (Player.Entity.IsFlying)
                {
                    // This z velocity overrides z movement velocity
                    movement.Z = 0;
                    entity.Velocity.Z = Player.ForwardMovementSpeed * 2;
                }
                else
                {
                    Player.Jump();
                }
            }

            if (movement != Vec3D.Zero)
            {
                if (!entity.OnGround && !Player.Entity.IsFlying)
                    movement *= AirControl;
                
                entity.Velocity.X += MathHelper.Clamp(movement.X, -Player.MaxMovement, Player.MaxMovement);
                entity.Velocity.Y += MathHelper.Clamp(movement.Y, -Player.MaxMovement, Player.MaxMovement);
                entity.Velocity.Z += MathHelper.Clamp(movement.Z, -Player.MaxMovement, Player.MaxMovement);
            }

            if (tickCommand.Has(TickCommands.Use))
                PhysicsManager.EntityUse(Player.Entity);             
        }

        private static Vec3D CalculateForwardMovement(Entity entity)
        {
            double x = Math.Cos(entity.Angle) * Player.ForwardMovementSpeed;
            double y = Math.Sin(entity.Angle) * Player.ForwardMovementSpeed;
            double z = 0;

            if (entity.Player != null && entity.IsFlying)
               z = Player.ForwardMovementSpeed * entity.Player.Pitch;

            return new Vec3D(x, y, z);
        }
        
        private static Vec3D CalculateStrafeRightMovement(Entity entity)
        {
            double rightRotateAngle = entity.Angle - MathHelper.PiOver2;
            double x = Math.Cos(rightRotateAngle) * Player.SideMovementSpeed;
            double y = Math.Sin(rightRotateAngle) * Player.SideMovementSpeed;

            return new Vec3D(x, y, 0);
        }

        private void PhysicsManager_EntityActivatedSpecial(object sender, EntityActivateSpecialEventArgs e)
        {
            if (e.ActivateLineSpecial != null)
            {
                var special = e.ActivateLineSpecial.Special;
                if (SpecialManager.TryAddActivatedLineSpecial(e))
                    Console.WriteLine($"Activate line special - line id[{e.ActivateLineSpecial.Id}] activation[{e.ActivationContext}] type[{special.LineSpecialType}] repeat[{e.ActivateLineSpecial.Flags.Repeat}]");
            }
        }

        private void PhysicsManager_PlayerUseFail(object sender, Entity e)
        {
            Console.WriteLine("Player - 'oof'");
        }

        private void Instance_CheatActivationChanged(object sender, ICheat e)
        {
            if (e.CheatType == CheatType.NoClip)
                Player.Entity.NoClip = e.Activated;
            else if (e.CheatType == CheatType.Fly)
                Player.Entity.IsFlying = e.Activated;
        }
    }
}