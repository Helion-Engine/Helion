using System;
using System.Linq;
using System.Numerics;
using Helion.Audio;
using Helion.Input;
using Helion.Maps;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.MapInfo;
using Helion.Models;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Sounds.Mus;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Physics;
using MoreLinq;
using NLog;
using static Helion.Util.Assertion.Assert;
using static Helion.World.Entities.EntityManager;
using Helion.Resources.Definitions.Language;
using System.Collections.Generic;
using Helion.Geometry.Vectors;

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

        public Player Player { get; private set; }

        public SinglePlayerWorld(GlobalData globalData, Config config, ArchiveCollection archiveCollection, IAudioSystem audioSystem,
            MapGeometry geometry, MapInfoDef mapDef, SkillDef skillDef, IMap map, 
            Player? existingPlayer = null, WorldModel? worldModel = null)
            : base(globalData, config, archiveCollection, audioSystem, geometry, mapDef, skillDef, map, worldModel)
        {
            if (worldModel == null)
            {
                EntityManager.PopulateFrom(map, LevelStats);

                IList<Entity> spawns = EntityManager.SpawnLocations.GetPlayerSpawns(0);
                if (spawns.Count == 0)
                    throw new HelionException("No player 1 starts.");

                Player = EntityManager.CreatePlayer(0, spawns.Last(), false);
                // Make voodoo dolls
                for (int i = spawns.Count - 2; i >= 0; i--)
                {
                    Player player = EntityManager.CreatePlayer(0, spawns[i], true);
                    player.SetDefaultInventory();
                }

                if (existingPlayer != null)
                {
                    Player.CopyProperties(existingPlayer);
                    Player.Inventory.ClearKeys();
                }
                else
                {
                    Player.SetDefaultInventory();
                }
            }
            else
            {
                WorldModelPopulateResult result = EntityManager.PopulateFrom(worldModel);
                if (result.Players.Count == 0)
                {
                    throw new HelionException("No players found in world.");
                }
                else
                {
                    if (result.Players.Any(x => x.PlayerNumber != 0))
                        Log.Warn("Other players found in world for single player game.");

                    Player = result.Players[0];
                }

                ApplyCheats(worldModel);                
                ApplySectorModels(worldModel, result);
                ApplyLineModels(worldModel);
                CreateDamageSpecials(worldModel);

                EntityManager.Entities.ForEach(entity => Link(entity));
                // Need to link again for clipping/stacked physics to be set correctly
                EntityManager.Entities.ForEach(entity =>
                {
                    entity.UnlinkFromWorld();
                    Link(entity);
                });

                SpecialManager.AddSpecialModels(worldModel.Specials);
            }

            CheatManager.Instance.CheatActivationChanged += Instance_CheatActivationChanged;
            EntityActivatedSpecial += PhysicsManager_EntityActivatedSpecial;
        }

        private void ApplyCheats(WorldModel worldModel)
        {
            foreach (PlayerModel playerModel in worldModel.Players)
            {
                Player? player = EntityManager.Players.FirstOrDefault(x => x.Id == playerModel.Id);
                if (player == null)
                    continue;

                playerModel.Cheats.ForEach(x => player.Cheats.SetCheatActive((CheatType)x));
            }
        }

        private void CreateDamageSpecials(WorldModel worldModel)
        {
            for (int i = 0; i < worldModel.DamageSpecials.Count; i++)
            {
                SectorDamageSpecialModel model = worldModel.DamageSpecials[i];
                if (!((IWorld)this).IsSectorIdValid(model.SectorId))
                    continue;

                Sectors[model.SectorId].SectorDamageSpecial = model.ToWorldSpecial(this);
            }
        }

        private void ApplyLineModels(WorldModel worldModel)
        {
            for (int i = 0; i < worldModel.Lines.Count; i++)
            {
                LineModel lineModel = worldModel.Lines[i];
                if (lineModel.Id < 0 || lineModel.Id >= Lines.Count)
                    continue;

                Lines[lineModel.Id].ApplyLineModel(lineModel);
            }
        }

        private void ApplySectorModels(WorldModel worldModel, WorldModelPopulateResult result)
        {
            for (int i = 0; i < worldModel.Sectors.Count; i++)
            {
                SectorModel sectorModel = worldModel.Sectors[i];
                if (sectorModel.Id < 0 || sectorModel.Id >= Sectors.Count)
                    continue;

                Sectors[sectorModel.Id].ApplySectorModel(sectorModel, result);
            }
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

        public static void PlayLevelMusic(IAudioSystem audioSystem, string entryName, ArchiveCollection archiveCollection)
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

        public void HandleFrameInput(InputEvent input)
        {
            CheatManager.Instance.HandleInput(Player, input);
            HandleMouseLook(input);
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

        private void Instance_CheatActivationChanged(object? sender, CheatEventArgs e)
        {
            ActivateCheat(e.Player, e.Cheat);
        }

        private void PhysicsManager_EntityActivatedSpecial(object? sender, EntityActivateSpecialEventArgs e)
        {
            if (e.ActivateLineSpecial != null)
                e.Success = SpecialManager.TryAddActivatedLineSpecial(e);
        }

        private void HandleMouseLook(InputEvent input)
        {
            if (Player.IsFrozen || Player.IsDead || WorldState == WorldState.Exit)
                return;

            Vec2I pixelsMoved = input.ConsumeMouseDelta();
            if (pixelsMoved.X != 0 || pixelsMoved.Y != 0)
            {
                Vec2F moveDelta = pixelsMoved.Float / (float)Config.Mouse.PixelDivisor;
                moveDelta.X *= (float)(Config.Mouse.Sensitivity * Config.Mouse.Yaw);
                moveDelta.Y *= (float)(Config.Mouse.Sensitivity * Config.Mouse.Pitch);

                Player.AddToYaw(moveDelta.X);

                if (Config.Mouse.Look)
                    Player.AddToPitch(moveDelta.Y);
            }
        }
    }
}