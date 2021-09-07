using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.Render.Legacy.Shared;
using Helion.Util;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Geometry.Sectors;
using Helion.World.Sound;
using Helion.World.StatusBar;
using static Helion.Util.Assertion.Assert;
using Helion.World.Cheats;
using Helion.Resources.Definitions.MapInfo;
using NLog;

namespace Helion.World.Entities.Players
{
    public class Player : Entity
    {
        public const double MaxMovement = 30.0;
        public const double ForwardMovementSpeedWalk = 0.71875;
        public const double ForwardMovementSpeedRun = 1.5625;
        public const double SideMovementSpeedWalk = 0.75;
        public const double SideMovementSpeedRun = 1.25;
        public const double AirControl = 0.00390625;
        private const double PlayerViewDivider = 8.0;
        private const double ViewHeightMin = 4.0;
        private const double DeathHeight = 8.0;
        private const double SlowTurnSpeed = 1.7578125 / 180 * Math.PI;
        private const double NormalTurnSpeed = 3.515625 / 180 * Math.PI;
        private const double FastTurnSpeed = 7.03125 / 180 * Math.PI;
        private const int JumpDelayTicks = 7;
        private const int SlowTurnTicks = 6;
        private static readonly PowerupType[] PowerupsWithBrightness = { PowerupType.LightAmp, PowerupType.Invulnerable };
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly int PlayerNumber;
        public double PitchRadians;
        public double PrevAngle;
        public int DamageCount;
        public int BonusCount;
        public TickCommand TickCommand = new();
        public int ExtraLight;
        public int TurnTics;
        public int KillCount;
        public int ItemCount;
        public int SecretsFound;

        private bool m_isJumping;
        private bool m_hasNewWeapon;
        private int m_jumpTics;
        private int m_deathTics;
        private double m_prevPitch;
        private double m_viewHeight;
        private double m_viewZ;
        private double m_prevViewZ;
        private double m_deltaViewHeight;
        private double m_bob;
        private Entity? m_killer;
        private bool m_interpolateAngle;

        public Inventory Inventory { get; private set; }
        public Weapon? Weapon { get; private set; }
        public Weapon? PendingWeapon { get; private set; }
        public Weapon? AnimationWeapon { get; private set; }
        public int WeaponSlot { get; private set; }
        public int WeaponSubSlot { get; private set; }
        public Vec2D PrevWeaponOffset;
        public Vec2D WeaponOffset;
        public Entity? Attacker { get; private set; }
        public PlayerStatusBar StatusBar { get; private set; }
        public PlayerCheats Cheats { get; } = new PlayerCheats();
        public PlayerInfo Info { get; set; } = new PlayerInfo();
        public bool IsVooDooDoll { get; set; }
        public override Player? PlayerObj => this;
        public override bool IsPlayer => true;

        public bool DrawFullBright()
        {
            foreach (PowerupType powerupType in PowerupsWithBrightness)
            {
                IPowerup? powerup = Inventory.GetPowerup(powerupType);
                if (powerup != null)
                    return powerup.DrawPowerupEffect;
            }

            return false;
        }

        public bool DrawInvulnerableColorMap() => Inventory.PowerupEffectColorMap != null && Inventory.PowerupEffectColorMap.DrawPowerupEffect;

        public override double ViewZ => m_viewZ;
        public override SoundChannelType WeaponSoundChannel => SoundChannelType.Weapon;
        public override bool IsInvulnerable => Flags.Invulnerable || Inventory.IsPowerupActive(PowerupType.Invulnerable);
        public override bool CanMakeSound() => !IsVooDooDoll;

        public Player(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
            Sector sector, EntityManager entityManager, WorldSoundManager soundManager, IWorld world, int playerNumber)
            : base(id, thingId, definition, position, angleRadians, sector, entityManager, soundManager, world)
        {
            Precondition(playerNumber >= 0, "Player number should not be negative");

            PlayerNumber = playerNumber;
            // Going to default to true for players, otherwise jumping without moving X/Y can allow for clipping through ceilings
            // See PhysicsManager.MoveZ
            MoveLinked = true;
            Inventory = new Inventory(this, entityManager.DefinitionComposer);

            PrevAngle = AngleRadians;
            m_viewHeight = definition.Properties.Player.ViewHeight;
            m_viewZ = m_prevViewZ = Definition.Properties.Player.ViewHeight;

            WeaponOffset.Y = Constants.WeaponBottom;
            PrevWeaponOffset.Y = Constants.WeaponBottom;

            StatusBar = new PlayerStatusBar(this);
            SetPlayerInfo();
        }

        public Player(PlayerModel playerModel, Dictionary<int, Entity> entities, EntityDefinition definition,
            EntityManager entityManager, WorldSoundManager soundManager, IWorld world)
            : base(playerModel, definition, entityManager, soundManager, world)
        {
            Precondition(playerModel.Number >= 0, "Player number should not be negative");

            PlayerNumber = playerModel.Number;
            AngleRadians = playerModel.AngleRadians;
            PitchRadians = playerModel.PitchRadians;
            DamageCount = playerModel.DamageCount;
            BonusCount = playerModel.BonusCount;
            ExtraLight = playerModel.ExtraLight;
            m_isJumping = playerModel.IsJumping;
            m_jumpTics = playerModel.JumpTics;
            m_deathTics = playerModel.DeathTics;
            m_viewHeight = playerModel.ViewHeight;
            m_viewZ = playerModel.ViewZ;
            m_deltaViewHeight = playerModel.DeltaViewHeight;
            m_bob = playerModel.Bob;
            WeaponOffset = (playerModel.WeaponOffsetX, playerModel.WeaponOffsetY);
            PrevWeaponOffset = (playerModel.WeaponOffsetX, playerModel.WeaponOffsetY);
            WeaponSlot = playerModel.WeaponSlot;
            WeaponSubSlot = playerModel.WeaponSubSlot;
            KillCount = playerModel.KillCount;
            ItemCount = playerModel.ItemCount;
            SecretsFound = playerModel.SecretsFound;
            
            Inventory = new Inventory(playerModel, this, entityManager.DefinitionComposer);

            if (playerModel.Weapon != null)
                Weapon = Inventory.Weapons.GetWeapon(playerModel.Weapon);
            if (playerModel.PendingWeapon != null)
                PendingWeapon = Inventory.Weapons.GetWeapon(playerModel.PendingWeapon);
            if (playerModel.AnimationWeapon != null)
                AnimationWeapon = Inventory.Weapons.GetWeapon(playerModel.AnimationWeapon);

            if (playerModel.Attacker.HasValue && entities.TryGetValue(playerModel.Attacker.Value, out Entity? attacker))
                Attacker = attacker;
            if (playerModel.Killer.HasValue)
                entities.TryGetValue(playerModel.Killer.Value, out m_killer);

            PrevAngle = AngleRadians;
            m_prevPitch = PitchRadians;
            m_prevViewZ = m_viewZ;

            StatusBar = new PlayerStatusBar(this);

            foreach (CheatType cheat in playerModel.Cheats)
                Cheats.SetCheatActive(cheat);

            SetPlayerInfo();
        }

        private void SetPlayerInfo()
        {
            Info.Name = World.Config.Player.Name;
            Info.Gender = World.Config.Player.Gender;
        }

        public PlayerModel ToPlayerModel()
        {
            PlayerModel playerModel = new PlayerModel()
            {
                Number = PlayerNumber,
                PitchRadians = PitchRadians,
                DamageCount = DamageCount,
                BonusCount = BonusCount,
                ExtraLight = ExtraLight,
                IsJumping = m_isJumping,
                JumpTics = m_jumpTics,
                DeathTics = m_deathTics,
                ViewHeight = m_viewHeight,
                ViewZ = ViewZ,
                DeltaViewHeight = m_deltaViewHeight,
                Bob = m_bob,
                Killer = m_killer?.Id,
                Attacker = Attacker?.Id,
                KillCount = KillCount,
                ItemCount = ItemCount,
                SecretsFound = SecretsFound,
                Weapon = Weapon?.Definition.Name.ToString(),
                PendingWeapon = PendingWeapon?.Definition.Name.ToString(),
                AnimationWeapon = AnimationWeapon?.Definition.Name.ToString(),
                WeaponOffsetX = WeaponOffset.X,
                WeaponOffsetY = WeaponOffset.Y,
                WeaponSlot = WeaponSlot,
                WeaponSubSlot = WeaponSubSlot,
                Inventory = Inventory.ToInventoryModel(),
                AnimationWeaponFrame = AnimationWeapon?.FrameState.ToFrameStateModel(),
                WeaponFlashFrame = AnimationWeapon?.FlashState.ToFrameStateModel(),
                Cheats = Cheats.GetActiveCheats().Cast<int>().ToList()
            };

            ToEntityModel(playerModel);
            return playerModel;
        }

        public void VodooSync(Player player)
        {
            base.CopyProperties(player);

            // NoClip did not apply to the player
            Flags.NoClip = false;

            foreach (InventoryItem item in player.Inventory.GetInventoryItems())
            {
                if (!Inventory.HasItem(item.Definition.Name))
                    Inventory.Add(item.Definition, item.Amount);
                else
                    Inventory.SetAmount(item.Definition, item.Amount);
            }
        }

        public override void CopyProperties(Entity entity)
        {
            if (entity.PlayerObj != null)
            {
                Player player = entity.PlayerObj;
                foreach (InventoryItem item in player.Inventory.GetInventoryItems())
                {
                    if (!Inventory.IsPowerup(item.Definition))
                        Inventory.Add(item.Definition, item.Amount);
                }

                foreach (Weapon weapon in player.Inventory.Weapons.GetWeapons())
                    GiveWeapon(weapon.Definition, giveDefaultAmmo: false, autoSwitch: false);

                if (player.Weapon != null)
                {
                    Weapon? setWeapon = Inventory.Weapons.GetWeapon(player.Weapon.Definition.Name);
                    if (setWeapon != null)
                        ChangeWeapon(setWeapon);
                }

                foreach (CheatType cheat in player.Cheats.GetActiveCheats())
                    Cheats.SetCheatActive(cheat);
            }

            base.CopyProperties(entity);
            m_hasNewWeapon = false;
        }

        public void SetDefaultInventory()
        {
            EntityDefinition? startWeaponDef = null;
            foreach (var item in Properties.Player.StartItem)
            {
                GiveItem(item.Name, item.Amount, out EntityDefinition? definition);
                if (definition == null)
                    Log.Error($"Invalid player start item: {item.Name}");
                else if (definition != null && IsWeapon(definition))
                    startWeaponDef = definition;
            }

            if (startWeaponDef != null)
            {
                var weapon = Inventory.Weapons.GetWeapon(startWeaponDef.Name);
                if (weapon != null)
                {
                    ChangeWeapon(weapon);
                    ForceLowerWeapon(false);
                }
            }

            m_hasNewWeapon = false;
        }

        private void GiveItem(string name, int amount, out EntityDefinition? definition)
        {
            definition = World.EntityManager.DefinitionComposer.GetByName(name);
            if (definition == null)
                return;

            if (IsWeapon(definition))
                GiveWeapon(definition, false);
            else
                GiveItemBase(definition, null, false, amount);
        }

        public bool HasNewWeapon() => m_hasNewWeapon;

        public Vec3D GetViewPosition()
        {
            Vec3D position = Position;
            position.Z += m_viewZ;
            return position;
        }

        public Vec3D GetPrevViewPosition()
        {
            Vec3D position = PrevPosition;
            position.Z += m_prevViewZ;
            return position;
        }

        public override void SetZ(double z, bool smooth)
        {
            if (smooth && Box.Bottom < z)
            {
                m_viewHeight -= z - Box.Bottom;
                m_deltaViewHeight = (Definition.Properties.Player.ViewHeight - m_viewHeight) / PlayerViewDivider;
                SetViewHeight();
            }

            base.SetZ(z, smooth);
        }

        public override void Hit(in Vec3D velocity)
        {
            if (BlockingSectorPlane != null && OnGround)
            {
                // If we're airborne and just landed on the ground, we need a delay
                // for jumping. This should only happen if we've coming down from a manual jump.
                if (m_isJumping)
                    m_jumpTics = JumpDelayTicks;

                m_isJumping = false;

                bool hardHit = velocity.Z < -(World.Gravity * 8);
                if (hardHit && !Flags.NoGravity && !IsDead)
                {
                    PlayLandSound();
                    m_deltaViewHeight = velocity.Z / PlayerViewDivider;
                }
            }

            base.Hit(velocity);
        }

        public override void ResetInterpolation()
        {
            m_viewHeight = Definition.Properties.Player.ViewHeight;
            m_prevViewZ = m_viewZ;
            PrevAngle = AngleRadians;
            m_prevPitch = PitchRadians;
            m_deltaViewHeight = 0;
            PrevWeaponOffset = WeaponOffset;

            base.ResetInterpolation();
        }

        public override void SetRaiseState()
        {
            base.SetRaiseState();
            PendingWeapon = Weapon;
            BringupWeapon();
            m_killer = null;
        }

        public override bool CanDamage(Entity source, bool isHitscan)
        {
            // Always return true for now - this will likely change with multiplayer options
            return true;
        }

        public void AddToYaw(double delta, bool isMouse)
        {
            if (!TickCommand.Has(TickCommands.Strafe))
                AngleRadians = MathHelper.GetPositiveAngle(AngleRadians + delta);

            if (isMouse)
                TickCommand.MouseAngle += delta;
        }

        public void AddToPitch(double delta, bool isMouse)
        {
            if (World.MapInfo.HasOption(MapOptions.NoFreelook))
                return;

            const double notQuiteVertical = MathHelper.HalfPi - 0.001;
            PitchRadians = MathHelper.Clamp(PitchRadians + delta, -notQuiteVertical, notQuiteVertical);
            if (isMouse)
                TickCommand.MousePitch += delta;
        }

        public Camera GetCamera(double t)
        {
            Vec3D position = GetPrevViewPosition().Interpolate(GetViewPosition(), t);

            // When rendering, we always want the most up-to-date values. We
            // would only want to interpolate here if looking at another player
            // and would likely need to add more logic for wrapping around if
            // the player rotates from 359 degrees -> 2 degrees since that will
            // interpolate in the wrong direction.

            if (m_interpolateAngle)
            {
                double prev = MathHelper.GetPositiveAngle(PrevAngle);
                double current = MathHelper.GetPositiveAngle(AngleRadians);
                double diff = Math.Abs(prev - current);

                if (diff >= MathHelper.Pi)
                {
                    if (prev > AngleRadians)
                        prev -= MathHelper.TwoPi;
                    else
                        current -= MathHelper.TwoPi;
                }

                float yaw = (float)(prev + t * (current - prev));
                float pitch = (float)(m_prevPitch + t * (PitchRadians - m_prevPitch));

                return new Camera(position.Float, yaw, pitch);
            }
            else
            {
                float yaw = (float)AngleRadians;
                float pitch = (float)PitchRadians;

                return new Camera(position.Float, yaw, pitch);
            }
        }

        public override void Tick()
        {
            base.Tick();
            Inventory.Tick();
            AnimationWeapon?.Tick();

            m_interpolateAngle = TickCommand.AngleTurn != 0 || TickCommand.PitchTurn != 0 || IsDead;
            PrevAngle = AngleRadians;
            m_prevPitch = PitchRadians;
            m_prevViewZ = m_viewZ;

            PrevWeaponOffset = WeaponOffset;

            if (m_jumpTics > 0)
                m_jumpTics--;

            m_viewHeight += m_deltaViewHeight;

            if (DamageCount > 0)
                DamageCount--;

            if (BonusCount > 0)
                BonusCount--;

            if (m_deathTics > 0)
            {
                m_deathTics--;
                if (m_viewHeight > DeathHeight)
                    m_viewHeight -= 1.0;
                else
                    m_deathTics = 0;
            }

            SetBob();
            SetViewHeight();

            if (IsDead)
                DeathTick();

            StatusBar.Tick();
            m_hasNewWeapon = false;
        }

        public void HandleTickCommand()
        {
            if (TickCommand.Has(TickCommands.Use))
                World.EntityUse(this);

            if (IsDead || IsFrozen)
                return;

            if (TickCommand.AngleTurn != 0 && !TickCommand.Has(TickCommands.Strafe))
                AddToYaw(TickCommand.AngleTurn, false);

            if (TickCommand.PitchTurn != 0)
                AddToPitch(TickCommand.PitchTurn, false);

            Vec3D movement = Vec3D.Zero;
            movement += CalculateForwardMovement(TickCommand.ForwardMoveSpeed);
            movement += CalculateStrafeMovement(TickCommand.SideMoveSpeed);

            if (TickCommand.Has(TickCommands.Jump))
            {
                if (Flags.NoGravity)
                {
                    // This z velocity overrides z movement velocity
                    movement.Z = 0;
                    Velocity.Z = GetForwardMovementSpeed() * 2;
                }
                else
                {
                    Jump();
                }
            }

            if (movement != Vec3D.Zero)
            {
                if (!OnGround && !Flags.NoGravity)
                    movement *= AirControl;

                Velocity.X += MathHelper.Clamp(movement.X, -MaxMovement, MaxMovement);
                Velocity.Y += MathHelper.Clamp(movement.Y, -MaxMovement, MaxMovement);
                Velocity.Z += MathHelper.Clamp(movement.Z, -MaxMovement, MaxMovement);
            }

            if (TickCommand.Has(TickCommands.Attack))
            {
                if (FireWeapon())
                    World.NoiseAlert(this);
            }
            else
            {
                Refire = false;
            }

            if (TickCommand.Has(TickCommands.NextWeapon))
            {
                var slot = Inventory.Weapons.GetNextSlot(this);
                ChangePlayerWeaponSlot(slot);
            }
            else if (TickCommand.Has(TickCommands.PreviousWeapon))
            {
                var slot = Inventory.Weapons.GetPreviousSlot(this);
                ChangePlayerWeaponSlot(slot);
            }
            else if (GetWeaponSlotCommand(TickCommand) != TickCommands.None)
            {
                TickCommands weaponSlotCommand = GetWeaponSlotCommand(TickCommand);
                int slot = GetWeaponSlot(weaponSlotCommand);
                Weapon? weapon;
                if (WeaponSlot == slot)
                {
                    int subslotCount = Inventory.Weapons.GetSubSlots(slot);
                    int subslot = (WeaponSubSlot + 1) % subslotCount;
                    weapon = Inventory.Weapons.GetWeapon(this, slot, subslot);
                }
                else
                {
                    weapon = Inventory.Weapons.GetWeapon(this, slot);
                }

                if (weapon != null)
                    ChangeWeapon(weapon);
            }
        }

        private Vec3D CalculateForwardMovement(double speed)
        {
            double x = Math.Cos(AngleRadians) * speed;
            double y = Math.Sin(AngleRadians) * speed;
            double z = 0;

            if (Flags.NoGravity)
                z = speed * PitchRadians;

            return new Vec3D(x, y, z);
        }

        private Vec3D CalculateStrafeMovement(double speed)
        {
            double rightRotateAngle = AngleRadians - MathHelper.HalfPi;
            double x = Math.Cos(rightRotateAngle) * speed;
            double y = Math.Sin(rightRotateAngle) * speed;

            return new Vec3D(x, y, 0);
        }

        private static int GetWeaponSlot(TickCommands tickCommand) =>
            (int)tickCommand - (int)TickCommands.WeaponSlot1 + 1;

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
            if (slot.Item1 != WeaponSlot || slot.Item2 != WeaponSubSlot)
            {
                var weapon = Inventory.Weapons.GetWeapon(this, slot.Item1, slot.Item2);
                if (weapon != null)
                    ChangeWeapon(weapon);
            }
        }

        private void DeathTick()
        {
            if (PitchRadians > 0.0)
            {
                PitchRadians -= Math.PI / Definition.Properties.Player.ViewHeight;
                if (PitchRadians < 0.0)
                    PitchRadians = 0.0;
            }
            else if (PitchRadians < 0.0)
            {
                PitchRadians += Math.PI / Definition.Properties.Player.ViewHeight;
                if (PitchRadians > 0.0)
                    PitchRadians = 0.0;
            }

            if (m_killer != null)
            {
                double angle = MathHelper.GetPositiveAngle(Position.Angle(m_killer.Position));
                double diff = angle - AngleRadians;
                double addAngle = 0.08726646; // 5 Degrees

                if (diff < addAngle && diff > -addAngle)
                {
                    AngleRadians = angle;
                }
                else
                {
                    if (MathHelper.GetPositiveAngle(diff) < Math.PI)
                        AngleRadians += addAngle;
                    else
                        AngleRadians -= addAngle;

                    AngleRadians = MathHelper.GetPositiveAngle(AngleRadians);
                }
            }
        }

        private void SetBob()
        {
            m_bob = Math.Min(16, (Velocity.X * Velocity.X) + (Velocity.Y * Velocity.Y) / 4) * World.Config.Hud.MoveBob;
            if (Weapon != null && Weapon.ReadyToFire)
            {
                double value = 0.1 * World.LevelTime;
                WeaponOffset.X = m_bob * Math.Cos(value % MathHelper.TwoPi);
                WeaponOffset.Y = Constants.WeaponTop + m_bob * Math.Sin(value % MathHelper.Pi);
            }

            double angle = MathHelper.TwoPi / 20 * World.LevelTime % MathHelper.TwoPi;
            m_bob = m_bob / 2 * Math.Sin(angle);
        }

        public bool GiveItem(EntityDefinition definition, EntityFlags? flags, bool pickupFlash = true)
        {
            if (IsDead)
                return false;

            bool success = GiveWeapon(definition);
            if (!success)
                success = GiveItemBase(definition, flags);

            if (success && pickupFlash)
            {
                BonusCount = 6;
                return true;
            }

            return false;
        }

        private bool GiveItemBase(EntityDefinition definition, EntityFlags? flags, bool autoSwitchWeapon = true, int amount = -1)
        {
            var invData = definition.Properties.Inventory;
            bool isHealth = definition.IsType(Inventory.HealthClassName);
            bool isArmor = definition.IsType(Inventory.ArmorClassName);
            bool isAmmo = IsAmmo(definition);
            if (amount == -1)
                amount = invData.Amount;

            if (isHealth)
            {
                return AddHealthOrArmor(definition, flags, ref Health, amount, false);
            }
            else if (isArmor)
            {
                bool success = AddHealthOrArmor(definition, flags, ref Armor, definition.Properties.Armor.SaveAmount, true);
                if (success)
                    ArmorDefinition = GetArmorDefinition(definition);
                return success;
            }

            if (IsWeapon(definition))
            {
                EntityDefinition? ammoDef = EntityManager.DefinitionComposer.GetByName(definition.Properties.Weapons.AmmoType);
                if (ammoDef != null)
                    return AddAmmo(ammoDef, definition.Properties.Weapons.AmmoGive, flags, autoSwitchWeapon);

                return false;
            }

            if (definition.IsType(Inventory.BackPackBaseClassName))
            {
                Inventory.AddBackPackAmmo(EntityManager.DefinitionComposer);
                Inventory.Add(definition, invData.Amount, flags);
                return true;
            }

            if (isAmmo)
                return AddAmmo(definition, amount, flags, autoSwitchWeapon);

            return Inventory.Add(definition, amount, flags);
        }

        private bool AddAmmo(EntityDefinition ammoDef, int amount, EntityFlags? flags, bool autoSwitchWeapon)
        {
            int oldCount = Inventory.Amount(Inventory.GetBaseInventoryName(ammoDef));
            bool success = Inventory.Add(ammoDef, World.SkillDefinition.GetAmmoAmount(amount, flags), flags);
            if (success && autoSwitchWeapon)
                CheckAutoSwitchAmmo(ammoDef, oldCount);
            return success;
        }

        public double GetForwardMovementSpeed()
        {
            if (TickCommand.IsFastSpeed(World.Config.Game.AlwaysRun))
                return ForwardMovementSpeedRun;

            return ForwardMovementSpeedWalk;
        }

        public double GetSideMovementSpeed()
        {
            if (TickCommand.IsFastSpeed(World.Config.Game.AlwaysRun))
                return SideMovementSpeedRun;

            return SideMovementSpeedWalk;
        }

        public double GetTurnAngle()
        {
            if (TurnTics < SlowTurnTicks)
                return SlowTurnSpeed;
            if (TickCommand.IsFastSpeed(World.Config.Game.AlwaysRun))
                return FastTurnSpeed;

            return NormalTurnSpeed;
        }

        private EntityDefinition? GetArmorDefinition(EntityDefinition definition)
        {
            bool isArmorBonus = definition.IsType(Inventory.BasicArmorBonusClassName);

            // Armor bonus keeps current property
            if (ArmorDefinition != null && isArmorBonus)
                return ArmorDefinition;

            // Find matching armor definition when picking up armor bonus with no armor
            if (ArmorDefinition == null && isArmorBonus)
            {
                IList<EntityDefinition> definitions = World.EntityManager.DefinitionComposer.GetEntityDefinitions();
                EntityDefinition? armorDef = definitions.FirstOrDefault(x => x.Properties.Armor.SavePercent == definition.Properties.Armor.SavePercent &&
                    x.IsType(Inventory.BasicArmorPickupClassName));

                if (armorDef != null)
                    return armorDef;
            }

            return definition;
        }

        private bool AddHealthOrArmor(EntityDefinition definition, EntityFlags? flags, ref int value, int amount, bool isArmor)
        {
            int max = GetMaxAmount(definition, isArmor);
            if (flags != null && !flags.Value.InventoryAlwaysPickup && value >= max)
                return false;

            value = MathHelper.Clamp(value + amount, 0, max);
            return true;
        }

        private int GetMaxAmount(EntityDefinition def, bool isArmor)
        {
            // I wish decorate made sense...
            if (isArmor)
            {
                if (def.Properties.Armor.MaxSaveAmount > 0)
                    return def.Properties.Armor.MaxSaveAmount;
                else
                    return def.Properties.Armor.SaveAmount;
            }

            if (def.Properties.Inventory.MaxAmount > 0)
                return Math.Max(def.Properties.Inventory.MaxAmount, Health);

            return Properties.Player.MaxHealth;
        }

        private void CheckAutoSwitchAmmo(EntityDefinition ammoDef, int oldCount)
        {
            // TODO the hardcoded checks are probably defined somewhere
            if (Weapon != null && !Weapon.Definition.Name.Equals("FIST", StringComparison.OrdinalIgnoreCase)
                && !Weapon.Definition.Name.Equals("PISTOL", StringComparison.OrdinalIgnoreCase))
                return;

            string name = Inventory.GetBaseInventoryName(ammoDef);
            Weapon? ammoWeapon = GetSelectionOrderedWeapons().FirstOrDefault(x => x.AmmoDefinition != null && 
                x.AmmoDefinition.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (ammoWeapon != null)
            {
                if (CheckAmmo(ammoWeapon, oldCount))
                    return;

                if (Weapon == null ||
                    ammoWeapon.Definition.Properties.Weapons.SelectionOrder < Weapon.Definition.Properties.Weapons.SelectionOrder)
                {
                    // Only switch to rocket launcher on fist (see above todo)
                    if (Weapon != null && !Weapon.Definition.Name.Equals("FIST", StringComparison.OrdinalIgnoreCase) &&
                        ammoWeapon.Definition.Name.Equals("ROCKETLAUNCHER", StringComparison.OrdinalIgnoreCase))
                        return;
                    ChangeWeapon(ammoWeapon);
                }
            }
        }

        private void CheckAutoSwitchWeapon(EntityDefinition definition, bool ownedWeapon)
        {
            if (ownedWeapon)
                return;

            Weapon? newWeapon = Inventory.Weapons.GetWeapon(definition.Name);
            if (newWeapon == null)
                return;

            ChangeWeapon(newWeapon);
        }

        /// <summary>
        /// Switches to the best available weapon based on Definition.Properties.Weapons.SelectionOrder / has enough ammo to fire.
        /// </summary>
        public void TrySwitchWeapon()
        {
            var weapons = GetSelectionOrderedWeapons();
            foreach (Weapon weapon in weapons)
            {
                if (weapon != Weapon && CheckAmmo(weapon))
                {
                    ChangeWeapon(weapon);
                    break;
                }
            }
        }

        private IEnumerable<Weapon> GetSelectionOrderedWeapons() => Inventory.Weapons.GetWeapons().OrderBy(x => x.Definition.Properties.Weapons.SelectionOrder);

        public bool GiveWeapon(EntityDefinition definition, bool giveDefaultAmmo = true, bool autoSwitch = true)
        {
            if (IsWeapon(definition) && !Inventory.Weapons.OwnsWeapon(definition.Name))
            {
                Weapon? addedWeapon = Inventory.Weapons.Add(definition, this, EntityManager);
                if (giveDefaultAmmo)
                    GiveItemBase(definition, null, autoSwitch);

                if (addedWeapon != null)
                {
                    if (autoSwitch)
                        CheckAutoSwitchWeapon(definition, false);
                    m_hasNewWeapon = true;
                    return true;
                }
            }

            return false;
        }

        public void ChangeWeapon(Weapon weapon)
        {
            if (!Inventory.Weapons.OwnsWeapon(weapon.Definition.Name) || Weapon == weapon || AnimationWeapon == weapon)
                return;

            bool hadWeapon = Weapon != null;
            var slot = Inventory.Weapons.GetWeaponSlot(weapon.Definition.Name); 
            WeaponSlot = slot.Item1;
            WeaponSubSlot = slot.Item2;

            if (!hadWeapon && PendingWeapon == null)
            {
                Weapon = weapon;
                AnimationWeapon = weapon;
                WeaponOffset.Y = Constants.WeaponBottom;
            }

            PendingWeapon = weapon;
            LowerWeapon(hadWeapon);
        }


        public bool FireWeapon()
        {
            if (!CheckAmmo())
            {
                TrySwitchWeapon();
                return false;
            }

            if (PendingWeapon != null || Weapon == null || !Weapon.ReadyToFire)
                return false;

            SetWeaponTop();
            Weapon.RequestFire();
            return true;
        }

        /// <summary>
        /// Checks if the player's current weapon has enough ammo to fire at least once.
        /// </summary>
        public bool CheckAmmo()
        {
            if (Weapon == null)
                return false;

            return CheckAmmo(Weapon);
        }

        /// <summary>
        /// Checks if the weapon has enough ammo to fire at least once.
        /// </summary>
        public bool CheckAmmo(Weapon weapon, int ammoCount = -1)
        {
            if (ammoCount == -1)
                ammoCount = Inventory.Amount(weapon.Definition.Properties.Weapons.AmmoType);

            return ammoCount >= weapon.Definition.Properties.Weapons.AmmoUse;
        }

        public bool CanFireWeapon()
        {
            return !IsDead && Weapon != null && TickCommand.Has(TickCommands.Attack) && CheckAmmo();
        }

        public void LowerWeapon(bool setTop = true)
        {
            if (Weapon == null)
                return;

            if (Weapon.ReadyToFire)
                ForceLowerWeapon(setTop);
        }

        public void ForceLowerWeapon(bool setTop)
        {
            if (Weapon != null)
            {
                if (setTop)
                    SetWeaponTop();
                Weapon.FrameState.SetState(Constants.FrameStates.Deselect);
            }
        }

        private void SetWeaponTop()
        {
            WeaponOffset.X = PrevWeaponOffset.X = 0;
            WeaponOffset.Y = PrevWeaponOffset.Y = Constants.WeaponTop;
        }

        public void BringupWeapon()
        {
            if (PendingWeapon == null)
                return;

            if (PendingWeapon.Definition.Properties.Weapons.UpSound.Length > 0)
                World.SoundManager.CreateSoundOn(this, PendingWeapon.Definition.Properties.Weapons.UpSound, SoundChannelType.Weapon, 
                    DataCache.Instance.GetSoundParams(this));

            AnimationWeapon = PendingWeapon;
            PendingWeapon = null;
            WeaponOffset.Y = Constants.WeaponBottom;
            AnimationWeapon.FrameState.SetState(Constants.FrameStates.Select);
        }

        public void SetWeaponUp()
        {
            Weapon = AnimationWeapon;
        }

        public void DescreaseAmmo()
        {
            if (Weapon == null)
                return;

            Inventory.Remove(Weapon.Definition.Properties.Weapons.AmmoType, Weapon.Definition.Properties.Weapons.AmmoUse);
        }

        public override bool Damage(Entity? source, int damage, bool setPainState, bool isHitscan)
        {
            if (Inventory.IsPowerupActive(PowerupType.Invulnerable))
                return false;

            if (Sector.SectorSpecialType == ZDoomSectorSpecialType.DamageEnd && damage >= Health)
                damage = Health - 1;

            damage = World.SkillDefinition.GetDamage(damage);

            bool damageApplied = base.Damage(source, damage, setPainState, isHitscan);
            if (damageApplied)
            {
                Attacker = source?.Owner ?? source;
                PlayPainSound();
                DamageCount += damage;
                DamageCount = Math.Min(DamageCount, Definition.Properties.Health);
                DamageCount = (int)((float)DamageCount / Definition.Properties.Health * 100);
            }

            return damageApplied;
        }

        private void PlayPainSound()
        {
            if (!IsDead)
            {
                if (Health < 26)
                    SoundManager.CreateSoundOn(this, "*pain25", SoundChannelType.Voice, DataCache.Instance.GetSoundParams(this));
                else if (Health < 51)
                    SoundManager.CreateSoundOn(this, "*pain50", SoundChannelType.Voice, DataCache.Instance.GetSoundParams(this));
                else if (Health < 76)
                    SoundManager.CreateSoundOn(this, "*pain75", SoundChannelType.Voice, DataCache.Instance.GetSoundParams(this));
                else
                    SoundManager.CreateSoundOn(this, "*pain100", SoundChannelType.Voice, DataCache.Instance.GetSoundParams(this));
            }
        }

        public void PlayGruntSound()
        {
            SoundManager.CreateSoundOn(this, "*grunt", SoundChannelType.Voice, DataCache.Instance.GetSoundParams(this));
        }

        public void PlayUseFailSound()
        {
            SoundManager.CreateSoundOn(this, "*usefail", SoundChannelType.Voice, DataCache.Instance.GetSoundParams(this));
        }

        public void PlayLandSound()
        {
            SoundManager.CreateSoundOn(this, "*land", SoundChannelType.Voice, DataCache.Instance.GetSoundParams(this));
        }

        protected override void SetDeath(Entity? source, bool gibbed)
        {
            base.SetDeath(source, gibbed);
            m_deathTics = MathHelper.Clamp((int)(Definition.Properties.Player.ViewHeight - DeathHeight), 0, (int)Definition.Properties.Player.ViewHeight);

            if (source != null)
                m_killer = source.Owner ?? source;
            if (m_killer == this)
                m_killer = null;

            ForceLowerWeapon(true);
        }

        public void Jump()
        {
            if (AbleToJump())
            {
                m_isJumping = true;
                Velocity.Z += Properties.Player.JumpZ;
            }
        }

        private void SetViewHeight()
        {
            double playerViewHeight = IsDead && m_deathTics == 0 ? DeathHeight : Definition.Properties.Player.ViewHeight;
            double halfPlayerViewHeight = playerViewHeight / 2;

            if (m_viewHeight > playerViewHeight)
            {
                m_deltaViewHeight = 0;
                m_viewHeight = playerViewHeight;
            }

            if (m_deathTics == 0)
            {
                if (m_viewHeight < halfPlayerViewHeight)
                {
                    m_viewHeight = halfPlayerViewHeight;
                    if (m_deltaViewHeight < 0)
                        m_deltaViewHeight = 0;
                }

                if (m_viewHeight < playerViewHeight)
                    m_deltaViewHeight += 0.25;
            }

            m_viewZ = MathHelper.Clamp(m_viewHeight + m_bob, ViewHeightMin, LowestCeilingZ - HighestFloorZ - ViewHeightMin);
        }

        private bool AbleToJump() => OnGround && Velocity.Z == 0 && m_jumpTics == 0 && !World.MapInfo.HasOption(MapOptions.NoJump) && !IsClippedWithEntity();
    }
}