using System;
using System.Numerics;
using Helion.Audio;
using Helion.Input;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Builder;
using Helion.World.Physics;
using Helion.World.Sound;
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
        
        private SinglePlayerWorld(Config config, ArchiveCollection archiveCollection, IAudioSystem audioSystem,
            MapGeometry geometry, IMap map)
            : base(config, archiveCollection, audioSystem, geometry, map)
        {
            EntityManager.PopulateFrom(map);
            
            Player = EntityManager.CreatePlayer(0);

            m_cheatManager.CheatActivationChanged += Instance_CheatActivationChanged;
            PhysicsManager.EntityActivatedSpecial += PhysicsManager_EntityActivatedSpecial;
            PhysicsManager.PlayerUseFail += PhysicsManager_PlayerUseFail;
        }

        ~SinglePlayerWorld()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            PerformDispose();
        }

        public static SinglePlayerWorld? Create(Config config, ArchiveCollection archiveCollection, 
            IAudioSystem audioSystem, IMap map)
        {
            MapGeometry? geometry = GeometryBuilder.Create(map);
            if (geometry == null)
            {
                Log.Error("Cannot make single player world, geometry is malformed");
                return null;
            }
            
            return new SinglePlayerWorld(config, archiveCollection, audioSystem, geometry, map);
        }

        public void HandleFrameInput(ConsumableInput frameInput)
        {
            m_cheatManager.HandleInput(frameInput);
            HandleMouseLook(frameInput);
        }

        public void HandleTickCommand(TickCommand tickCommand)
        {
            if (Player.IsFrozen)
                return;

            Vec3D movement = Vec3D.Zero;
            if (tickCommand.Has(TickCommands.Forward))
                movement += CalculateForwardMovement(Player);
            if (tickCommand.Has(TickCommands.Backward))
                movement -= CalculateForwardMovement(Player);
            if (tickCommand.Has(TickCommands.Right))
                movement += CalculateStrafeRightMovement(Player);
            if (tickCommand.Has(TickCommands.Left))
                movement -= CalculateStrafeRightMovement(Player);

            if (tickCommand.Has(TickCommands.Jump))
            {
                if (Player.IsFlying)
                {
                    // This z velocity overrides z movement velocity
                    movement.Z = 0;
                    Player.Velocity.Z = Player.ForwardMovementSpeed * 2;
                }
                else
                {
                    Player.Jump();
                }
            }

            if (movement != Vec3D.Zero)
            {
                if (!Player.OnGround && !Player.IsFlying)
                    movement *= AirControl;
                
                Player.Velocity.X += MathHelper.Clamp(movement.X, -Player.MaxMovement, Player.MaxMovement);
                Player.Velocity.Y += MathHelper.Clamp(movement.Y, -Player.MaxMovement, Player.MaxMovement);
                Player.Velocity.Z += MathHelper.Clamp(movement.Z, -Player.MaxMovement, Player.MaxMovement);
            }

            if (tickCommand.Has(TickCommands.Use))
                PhysicsManager.EntityUse(Player);
        }
        
        protected override void PerformDispose()
        {
            m_cheatManager.CheatActivationChanged -= Instance_CheatActivationChanged;
            PhysicsManager.EntityActivatedSpecial -= PhysicsManager_EntityActivatedSpecial;
            PhysicsManager.PlayerUseFail -= PhysicsManager_PlayerUseFail;
            
            base.PerformDispose();
        }

        private static Vec3D CalculateForwardMovement(Player player)
        {
            double x = Math.Cos(player.AngleRadians) * Player.ForwardMovementSpeed;
            double y = Math.Sin(player.AngleRadians) * Player.ForwardMovementSpeed;
            double z = 0;

            if (player.IsFlying)
               z = Player.ForwardMovementSpeed * player.PitchRadians;

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
                Player.NoClip = cheatEvent.Activated;
                break;
            case CheatType.Fly:
                Player.IsFlying = cheatEvent.Activated;
                break;
            }
        }

        private void PhysicsManager_EntityActivatedSpecial(object? sender, EntityActivateSpecialEventArgs e)
        {
            if (e.ActivateLineSpecial != null)
                SpecialManager.TryAddActivatedLineSpecial(e);
        }

        private void PhysicsManager_PlayerUseFail(object? sender, Entity entity)
        {
            // TODO: Use SNDINFO.
            // TODO: Should this be inside the physics class instead?
            SoundManager.CreateSoundOn(entity, "DSOOF", SoundChannelType.Voice);
        }

        private void HandleMouseLook(ConsumableInput frameInput)
        {
            if (Player.IsFrozen)
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