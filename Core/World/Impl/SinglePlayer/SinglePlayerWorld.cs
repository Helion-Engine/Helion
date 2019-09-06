using System;
using System.Numerics;
using Helion.Input;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Builder;
using Helion.World.Physics;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        private const double AirControl = 0.00390625;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly Player Player;
        private readonly CheatManager m_cheatManager = new CheatManager();
        
        private SinglePlayerWorld(Config config, ArchiveCollection archiveCollection, MapGeometry geometry, IMap map) : 
            base(config, archiveCollection, geometry, map)
        {
            EntityManager.PopulateFrom(map);
            
            // TODO: Did we want to force creation of the player if missing? Like stick them at (0, 0)?
            Player? player = EntityManager.CreatePlayer(0);
            if (player == null)
                throw new NullReferenceException("TODO: Should not allow this, maybe spawn player forcefully?");
            Player = player;

            m_cheatManager.CheatActivationChanged += Instance_CheatActivationChanged;
            PhysicsManager.EntityActivatedSpecial += PhysicsManager_EntityActivatedSpecial;
            PhysicsManager.PlayerUseFail += PhysicsManager_PlayerUseFail;
        }

        ~SinglePlayerWorld()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public static SinglePlayerWorld? Create(Config config, ArchiveCollection archiveCollection, IMap map)
        {
            MapGeometry? geometry = GeometryBuilder.Create(map);
            if (geometry == null)
            {
                Log.Error("Cannot make single player world, geometry is malformed");
                return null;
            }
            
            return new SinglePlayerWorld(config, archiveCollection, geometry, map);
        }

        public void HandleFrameInput(ConsumableInput frameInput)
        {
            m_cheatManager.HandleInput(frameInput);
            HandleMouseLook(frameInput);
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
        
        protected override void PerformDispose()
        {
            m_cheatManager.CheatActivationChanged -= Instance_CheatActivationChanged;
            PhysicsManager.EntityActivatedSpecial -= PhysicsManager_EntityActivatedSpecial;
            PhysicsManager.PlayerUseFail -= PhysicsManager_PlayerUseFail;
            
            base.PerformDispose();
        }

        private static Vec3D CalculateForwardMovement(Entity entity)
        {
            double x = Math.Cos(entity.AngleRadians) * Player.ForwardMovementSpeed;
            double y = Math.Sin(entity.AngleRadians) * Player.ForwardMovementSpeed;
            double z = 0;

            if (entity.Player != null && entity.IsFlying)
               z = Player.ForwardMovementSpeed * entity.Player.Pitch;

            return new Vec3D(x, y, z);
        }
        
        private static Vec3D CalculateStrafeRightMovement(Entity entity)
        {
            double rightRotateAngle = entity.AngleRadians - MathHelper.HalfPi;
            double x = Math.Cos(rightRotateAngle) * Player.SideMovementSpeed;
            double y = Math.Sin(rightRotateAngle) * Player.SideMovementSpeed;

            return new Vec3D(x, y, 0);
        }

        private void Instance_CheatActivationChanged(object? sender, ICheat cheatEvent)
        {
            if (cheatEvent is ChangeLevelCheat changeLevel)
            {
                ChangeToLevel(changeLevel.LevelNumber);
                return;
            }
            
            switch (cheatEvent.CheatType)
            {
            case CheatType.NoClip:
                Player.Entity.NoClip = cheatEvent.Activated;
                break;
            case CheatType.Fly:
                Player.Entity.IsFlying = cheatEvent.Activated;
                break;
            }
        }

        private void PhysicsManager_EntityActivatedSpecial(object? sender, EntityActivateSpecialEventArgs e)
        {
            if (e.ActivateLineSpecial != null)
            {
                var special = e.ActivateLineSpecial.Special;
                if (SpecialManager.TryAddActivatedLineSpecial(e))
                    Log.Debug($"Activate line special - line id[{e.ActivateLineSpecial.Id}] activation[{e.ActivationContext}] type[{special.LineSpecialType}] repeat[{e.ActivateLineSpecial.Flags.Repeat}]");
            }
        }

        private void PhysicsManager_PlayerUseFail(object? sender, Entity e)
        {
            Log.Debug("Player - 'oof'");
        }

        private void HandleMouseLook(ConsumableInput frameInput)
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
    }
}