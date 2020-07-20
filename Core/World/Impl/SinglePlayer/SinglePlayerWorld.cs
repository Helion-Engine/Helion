using System;
using System.Numerics;
using Helion.Audio;
using Helion.Input;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
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
        private const double EntityShootDistance = 8192.0;
        private const double EntityMeleeDistance = 64.0;
        private const double DefaultSpreadAngle = 5.6 * Math.PI / 180.0;
        private const double SuperShotgunSpreadAngle = 11.2 * Math.PI / 180.0;
        private const double SuperShotgunSpreadPitch = 7.1 * Math.PI / 180.0;
        private const int ShotgunBullets = 7;
        private const int SuperShotgunBullets = 20;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly Player Player;
        
        private SinglePlayerWorld(Config config, ArchiveCollection archiveCollection, IAudioSystem audioSystem,
            MapGeometry geometry, IMap map)
            : base(config, archiveCollection, audioSystem, geometry, map)
        {
            EntityManager.PopulateFrom(map);
            
            Player = EntityManager.CreatePlayer(0);

            CheatManager.Instance.CheatActivationChanged += Instance_CheatActivationChanged;
            PhysicsManager.EntityActivatedSpecial += PhysicsManager_EntityActivatedSpecial;
            PhysicsManager.PlayerUseFail += PhysicsManager_PlayerUseFail;
        }

        ~SinglePlayerWorld()
        {
            FailedToDispose(this);
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
            CheatManager.Instance.HandleInput(frameInput);
            HandleMouseLook(frameInput);
        }

        public void HandleTickCommand(TickCommand tickCommand)
        {
            if (Player.IsFrozen)
                return;

            if (!Player.IsDead)
            {
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
                    if (Player.Flags.NoGravity)
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
                    if (!Player.OnGround && !Player.Flags.NoGravity)
                        movement *= AirControl;

                    Player.Velocity.X += MathHelper.Clamp(movement.X, -Player.MaxMovement, Player.MaxMovement);
                    Player.Velocity.Y += MathHelper.Clamp(movement.Y, -Player.MaxMovement, Player.MaxMovement);
                    Player.Velocity.Z += MathHelper.Clamp(movement.Z, -Player.MaxMovement, Player.MaxMovement);
                }

                if (tickCommand.Has(TickCommands.Attack))
                {
                    Player.Weapon?.RequestFire();

                    // TODO remove when decorate weapons are implemented
                    switch (Player.WeaponIndex)
                    {
                        case 0:
                            PhysicsManager.FireHitscanBullets(Player, 1, DefaultSpreadAngle, 0.0, Player.PitchRadians, EntityMeleeDistance, Config.Engine.Gameplay.AutoAim);
                            break;

                        case 1:
                            PhysicsManager.FireHitscanBullets(Player, 1, DefaultSpreadAngle, 0.0, Player.PitchRadians, EntityShootDistance, Config.Engine.Gameplay.AutoAim);
                            break;

                        case 2:
                            PhysicsManager.FireHitscanBullets(Player, ShotgunBullets, DefaultSpreadAngle, 0.0, Player.PitchRadians, EntityShootDistance, Config.Engine.Gameplay.AutoAim);
                            break;

                        case 3:
                            PhysicsManager.FireHitscanBullets(Player, SuperShotgunBullets, SuperShotgunSpreadAngle, SuperShotgunSpreadPitch, Player.PitchRadians, 8192.0, Config.Engine.Gameplay.AutoAim);
                            break;

                        case 4:
                            PhysicsManager.FireProjectile(Player, Player.PitchRadians, EntityShootDistance, Config.Engine.Gameplay.AutoAim, "Rocket");
                            break;

                        case 5:
                            PhysicsManager.FireProjectile(Player, Player.PitchRadians, EntityShootDistance, Config.Engine.Gameplay.AutoAim, "PlasmaBall");
                            break;

                        case 6:
                            PhysicsManager.FireProjectile(Player, Player.PitchRadians, EntityShootDistance, Config.Engine.Gameplay.AutoAim, "BFGBall");
                            break;
                    }

                    Player.Refire = true;
                }
                else
                {
                    Player.Refire = false;
                }

                if (tickCommand.Has(TickCommands.NextWeapon))
                    ++Player.WeaponIndex;

                if (tickCommand.Has(TickCommands.PreviousWeapon))
                    --Player.WeaponIndex;

                if (Player.WeaponIndex > 6)
                    Player.WeaponIndex = 0;
                if (Player.WeaponIndex < 0)
                    Player.WeaponIndex = 6;
            }

            if (tickCommand.Has(TickCommands.Use))
                PhysicsManager.EntityUse(Player);
        }
        
        protected override void PerformDispose()
        {
            CheatManager.Instance.CheatActivationChanged -= Instance_CheatActivationChanged;
            PhysicsManager.EntityActivatedSpecial -= PhysicsManager_EntityActivatedSpecial;
            PhysicsManager.PlayerUseFail -= PhysicsManager_PlayerUseFail;
            
            base.PerformDispose();
        }

        private static Vec3D CalculateForwardMovement(Player player)
        {
            double x = Math.Cos(player.AngleRadians) * Player.ForwardMovementSpeed;
            double y = Math.Sin(player.AngleRadians) * Player.ForwardMovementSpeed;
            double z = 0;

            if (player.Flags.NoGravity)
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
                Player.Flags.NoGravity = cheatEvent.Activated;
                break;
            case CheatType.God:
                Player.Flags.Invulnerable = cheatEvent.Activated;
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
            if (Player.IsFrozen || Player.IsDead)
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