﻿using System;
using System.Linq;
using System.Numerics;
using Helion.Audio;
using Helion.Input;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Geometry.Vectors;
using Helion.Util.Sounds.Mus;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Physics;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Impl.SinglePlayer
{
    public class SinglePlayerWorld : WorldBase
    {
        private const double AirControl = 0.00390625;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public override Vec3D ListenerPosition => Player.Position;
        public override double ListenerAngle => Player.AngleRadians;
        public override double ListenerPitch => Player.PitchRadians;
        public override Entity ListenerEntity => Player;

        public readonly Player Player;

        private readonly IAudioSystem m_audioSystem;

        public SinglePlayerWorld(Config config, ArchiveCollection archiveCollection, IAudioSystem audioSystem,
            MapGeometry geometry, MapInfoDef mapDef, IMap map, Player? existingPlayer = null)
            : base(config, archiveCollection, audioSystem, geometry, mapDef, map)
        {
            EntityManager.PopulateFrom(map);

            Player = EntityManager.CreatePlayer(0);

            if (existingPlayer != null)
            {
                Player.CopyProperties(existingPlayer);
                Player.Inventory.ClearKeys();
            }
            else
            {
                Player.SetDefaultInventory();
            }

            CheatManager.Instance.CheatActivationChanged += Instance_CheatActivationChanged;
            EntityActivatedSpecial += PhysicsManager_EntityActivatedSpecial;

            m_audioSystem = audioSystem;
        }

        ~SinglePlayerWorld()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public override void Start()
        {
            PlayLevelMusic(AudioSystem, MapInfo.Music, ArchiveCollection);
            base.Start();
        }

        private static void PlayLevelMusic(IAudioSystem audioSystem, string entryName, ArchiveCollection archiveCollection)
        {
            Entry? entry = archiveCollection.Entries.FindByName(entryName);
            if (entry == null)
            {
                Log.Warn("Cannot find music track: {0}", entryName);
                return;
            }

            byte[]? midiData;
            byte[] data = entry.ReadData();
            if (data.Length > 3 && data[0] == 'M' && data[1] == 'U' && data[2] == 'S')
            {
                midiData = MusToMidi.Convert(data);
                if (midiData == null)
                {
                    Log.Warn("Unable to play music, cannot convert from MUS to MIDI");
                    return;
                }
            }
            else
            {
                midiData = data;
            }

            bool playingSuccess = audioSystem.Music.Play(midiData);
            if (!playingSuccess)
                Log.Warn("Unable to play MIDI track through device");
        }

        public void HandleFrameInput(ConsumableInput frameInput)
        {
            CheatManager.Instance.HandleInput(frameInput);
            HandleMouseLook(frameInput);
        }

        public void HandleTickCommand(TickCommand tickCommand)
        {
            Player.TickCommand = tickCommand;

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
                    if (Player.FireWeapon())
                        NoiseAlert(Player);
                }
                else
                {
                    Player.Refire = false;
                }

                if (tickCommand.Has(TickCommands.NextWeapon))
                {
                    var slot = Player.Inventory.Weapons.GetNextSlot(Player);
                    ChangePlayerWeaponSlot(slot);
                }
                else if (tickCommand.Has(TickCommands.PreviousWeapon))
                {
                    var slot = Player.Inventory.Weapons.GetPreviousSlot(Player);
                    ChangePlayerWeaponSlot(slot);
                }
                else if (GetWeaponSlotCommand(tickCommand) != TickCommands.None)
                {
                    TickCommands weaponSlotCommand = GetWeaponSlotCommand(tickCommand);
                    int slot = GetWeaponSlot(weaponSlotCommand);
                    Weapon? weapon;
                    if (Player.WeaponSlot == slot)
                    {
                        int subslotCount = Player.Inventory.Weapons.GetSubSlots(slot);
                        int subslot = (Player.WeaponSubSlot + 1) % subslotCount;
                        weapon = Player.Inventory.Weapons.GetWeapon(Player, slot, subslot);
                    }
                    else
                    {
                        weapon = Player.Inventory.Weapons.GetWeapon(Player, slot);
                    }

                    if (weapon != null && weapon != Player.Weapon)
                        Player.ChangeWeapon(weapon);
                }
            }

            if (tickCommand.Has(TickCommands.Use))
                EntityUse(Player);
        }

        private int GetWeaponSlot(TickCommands tickCommand)
        {
            return (int)tickCommand - (int)TickCommands.WeaponSlot1 + 1;
        }

        private static readonly TickCommands[] WeaponSlotCommands = new TickCommands[]
        {
            TickCommands.WeaponSlot1,
            TickCommands.WeaponSlot2,
            TickCommands.WeaponSlot3,
            TickCommands.WeaponSlot4,
            TickCommands.WeaponSlot5,
            TickCommands.WeaponSlot6,
            TickCommands.WeaponSlot7,
        };

        private TickCommands GetWeaponSlotCommand(TickCommand tickCommand)
        {
            TickCommands? command = WeaponSlotCommands.FirstOrDefault(x => tickCommand.Has(x));
            if (command != null)
                return command.Value;
            return TickCommands.None;
        }

        private void ChangePlayerWeaponSlot((int, int) slot)
        {
            if (slot.Item1 != Player.WeaponSlot || slot.Item2 != Player.WeaponSubSlot)
            {
                var weapon = Player.Inventory.Weapons.GetWeapon(Player, slot.Item1, slot.Item2);
                if (weapon != null)
                    Player.ChangeWeapon(weapon);
            }
        }

        public override bool EntityUse(Entity entity)
        {
            if (entity is Player && entity.IsDead)
                ResetLevel();

            return base.EntityUse(entity);
        }

        protected override void PerformDispose()
        {
            CheatManager.Instance.CheatActivationChanged -= Instance_CheatActivationChanged;
            EntityActivatedSpecial -= PhysicsManager_EntityActivatedSpecial;

            AudioSystem.Music.Stop();

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
                    Player.Flags.NoClip = cheatEvent.Activated;
                    break;
                case CheatType.Fly:
                    Player.Flags.NoGravity = cheatEvent.Activated;
                    break;
                case CheatType.God:
                    Player.Flags.Invulnerable = cheatEvent.Activated;
                    break;
                case CheatType.GiveAllNoKeys:
                    GiveAllWeapons();
                    GiveMaxHealth();
                    Player.GiveBestArmor(EntityManager.DefinitionComposer);
                    break;
                case CheatType.GiveAll:
                    GiveAllWeapons();
                    GiveMaxHealth();
                    Player.Inventory.GiveAllKeys(EntityManager.DefinitionComposer);
                    Player.GiveBestArmor(EntityManager.DefinitionComposer);
                    break;
            }
        }

        private void GiveAllWeapons()
        {
            foreach (CIString name in Weapons.GetWeaponDefinitionNames())
            {
                var weapon = EntityManager.DefinitionComposer.GetByName(name);
                if (weapon != null)
                    Player.GiveWeapon(weapon);
            }

            Player.Inventory.GiveAllAmmo(EntityManager.DefinitionComposer);
        }

        private void GiveMaxHealth()
        {
            if (Player.Health < Player.Definition.Properties.Health)
                Player.Health = Player.Definition.Properties.Health;
        }

        private void PhysicsManager_EntityActivatedSpecial(object? sender, EntityActivateSpecialEventArgs e)
        {
            if (e.ActivateLineSpecial != null)
                e.Success = SpecialManager.TryAddActivatedLineSpecial(e);
        }

        private void HandleMouseLook(ConsumableInput frameInput)
        {
            if (Player.IsFrozen || Player.IsDead || WorldState == WorldState.Exit)
                return;

            Vec2I pixelsMoved = frameInput.ConsumeMouseDelta();
            if (pixelsMoved.X != 0 || pixelsMoved.Y != 0)
            {
                Vector2 moveDelta = pixelsMoved.ToFloat() / (float)Config.Mouse.PixelDivisor;
                moveDelta.X *= (float)(Config.Mouse.Sensitivity * Config.Mouse.Yaw);
                moveDelta.Y *= (float)(Config.Mouse.Sensitivity * Config.Mouse.Pitch);

                Player.AddToYaw(moveDelta.X);

                if (Config.Mouse.Look)
                    Player.AddToPitch(moveDelta.Y);
            }
        }
    }
}