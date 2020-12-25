using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio;
using Helion.Maps.Specials.ZDoom;
using Helion.Render.Shared;
using Helion.Util;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties.Components;
using Helion.World.Entities.Inventories;
using Helion.World.Geometry.Sectors;
using Helion.World.Sound;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Players
{
    public class Player : Entity
    {
        public const double ForwardMovementSpeed = 1.5625;
        public const double SideMovementSpeed = 1.25;
        public const double MaxMovement = 30.0;
        private const double PlayerViewDivider = 8.0;
        private const double ViewHeightMin = 4.0;
        private const double DeathHeight = 8.0;
        private const int JumpDelayTicks = 7;

        public readonly int PlayerNumber;
        public double PitchRadians;
        public int LastPickupGametick = int.MinValue / 2;
        public int DamageCount;
        public TickCommand TickCommand = new();
        public int ExtraLight;

        private bool m_isJumping;
        private int m_jumpTics;
        private int m_deathTics;
        private double m_prevAngle;
        private double m_prevPitch;
        private double m_viewHeight;
        private double m_viewZ;
        private double m_prevViewZ;
        private double m_deltaViewHeight;
        private double m_bob;
        private Entity? m_killer;

        public Weapon? Weapon { get; private set; }
        public Weapon? PendingWeapon { get; private set; }
        public Weapon? AnimationWeapon { get; private set; }
        public int WeaponSlot { get; private set; }
        public int WeaponSubSlot { get; private set; }
        public Vec2D PrevWeaponOffset;
        public Vec2D WeaponOffset;

        public override double ViewZ => m_viewZ;

        public Player(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
            Sector sector, EntityManager entityManager, SoundManager soundManager, IWorld world, int playerNumber)
            : base(id, thingId, definition, position, angleRadians, sector, entityManager, soundManager, world)
        {
            Precondition(playerNumber >= 0, "Player number should not be negative");

            PlayerNumber = playerNumber;
            // Going to default to true for players, otherwise jumping without moving X/Y can allow for clipping through ceilings
            // See PhysicsManager.MoveZ
            MoveLinked = true;
            m_prevAngle = AngleRadians;
            m_viewHeight = definition.Properties.Player.ViewHeight;
            m_viewZ = m_prevViewZ = m_deltaViewHeight;
        }

        public override void CopyProperties(Entity entity)
        {
            if (entity is Player player)
            {
                foreach (var item in player.Inventory.GetInventoryItems())
                    Inventory.Add(item.Definition, item.Amount);
                foreach (var weapon in player.Inventory.Weapons.GetWeapons())
                    GiveWeapon(weapon.Definition, false);
                if (player.Weapon != null)
                {
                    Weapon? setWeapon = Inventory.Weapons.GetWeapon(player.Weapon.Definition.Name);
                    if (setWeapon != null)
                        ChangeWeapon(setWeapon);
                }
            }

            base.CopyProperties(entity);
        }

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
            m_deltaViewHeight = 0;
            PrevWeaponOffset = WeaponOffset;

            base.ResetInterpolation();
        }

        public void AddToYaw(double delta)
        {
            AngleRadians = MathHelper.GetPositiveAngle(AngleRadians + delta);
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

            if (IsDead)
            {
                float yaw = (float)(m_prevAngle + t * (AngleRadians - m_prevAngle));
                float pitch = (float)(m_prevPitch + t * (PitchRadians - m_prevPitch));

                return new Camera(position.ToFloat(), yaw, pitch);
            }
            else
            {
                float yaw = (float)AngleRadians;
                float pitch = (float)PitchRadians;

                return new Camera(position.ToFloat(), yaw, pitch);
            }
        }

        public override void Tick()
        {
            base.Tick();
            AnimationWeapon?.Tick();

            m_prevAngle = AngleRadians;
            m_prevPitch = PitchRadians;
            m_prevViewZ = m_viewZ;

            PrevWeaponOffset = WeaponOffset;

            if (m_jumpTics > 0)
                m_jumpTics--;

            m_viewHeight += m_deltaViewHeight;

            if (DamageCount > 0)
                DamageCount--;

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
            m_bob = Math.Min(16, (Velocity.X * Velocity.X) + (Velocity.Y * Velocity.Y) / 4) * World.Config.Engine.Gameplay.MoveBob;
            if (Weapon != null && Weapon.FrameState.IsState(Entities.Definition.States.FrameStateLabel.Ready))
            {
                double value = 0.1 * World.LevelTime;
                WeaponOffset.X = m_bob * Math.Cos(value % MathHelper.TwoPi);
                WeaponOffset.Y = Constants.WeaponTop + m_bob * Math.Sin(value % MathHelper.Pi);
            }

            double angle = MathHelper.TwoPi / 20 * World.LevelTime % MathHelper.TwoPi;
            m_bob = m_bob / 2 * Math.Sin(angle);
        }

        public override bool GiveItem(EntityDefinition definition, EntityFlags? flags, bool pickupFlash = true)
        {
            bool ownedWeapon = Inventory.Weapons.OwnsWeapon(definition.Name);
            bool success = GiveWeapon(definition);
            if (success)
            {
                CheckAutoSwitchWeapon(definition, ownedWeapon);
            }
            else
            {
                bool isAmmo = IsAmmo(definition);
                int count = Inventory.Amount(definition.Name);
                success = base.GiveItem(definition, flags, pickupFlash:true);
                if (success && IsWeapon(definition))
                    CheckAutoSwitchWeapon(definition, ownedWeapon);
                else if (success && isAmmo)
                    CheckAutoSwitchAmmo(definition, count);
            }

            if (success && pickupFlash)
            {
                LastPickupGametick = World.Gametick;
                return true;
            }

            return false;
        }

        private void CheckAutoSwitchAmmo(EntityDefinition ammoDef, int oldCount)
        {
            // TODO the hardcoded checks are probably defined somewhere
            if (Weapon != null && Weapon.Definition.Name != "FIST" && Weapon.Definition.Name != "PISTOL")
                return;

            CIString name = Inventory.GetBaseInventoryName(ammoDef);
            Weapon? ammoWeapon = GetSelectionOrderedWeapons().FirstOrDefault(x => x.AmmoDefinition != null && x.AmmoDefinition.Name == name);
            if (ammoWeapon != null)
            {
                if (CheckAmmo(ammoWeapon, oldCount))
                    return;

                if (Weapon == null ||
                    ammoWeapon.Definition.Properties.Weapons.SelectionOrder < Weapon.Definition.Properties.Weapons.SelectionOrder)
                {
                    // Only switch to rocket launcher on fist (see above todo)
                    if (Weapon != null && Weapon.Definition.Name == "FIST" && ammoWeapon.Definition.Name == "ROCKETLAUNCHER")
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

            PendingWeapon = Inventory.Weapons.GetWeapon(definition.Name);
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

        public bool GiveWeapon(EntityDefinition definition, bool giveDefaultAmmo = true)
        {
            if (IsWeapon(definition) && !Inventory.Weapons.OwnsWeapon(definition.Name))
            {
                Weapon? addedWeapon = Inventory.Weapons.Add(definition, this, EntityManager);
                if (giveDefaultAmmo)
                    base.GiveItem(definition, null);

                return addedWeapon != null;
            }

            return false;
        }

        public void ChangeWeapon(Weapon weapon)
        {
            var slot = Weapons.GetWeaponSlot(weapon.Definition);
            if (Inventory.Weapons.OwnsWeapon(weapon.Definition.Name))
            {
                WeaponSlot = slot.Item1;
                WeaponSubSlot = slot.Item2;
                bool hadWeapon = Weapon != null;

                if (!hadWeapon && PendingWeapon == null)
                {
                    Weapon = weapon;
                    AnimationWeapon = weapon;
                    WeaponOffset.Y = Constants.WeaponBottom;
                }

                PendingWeapon = weapon;
                LowerWeapon(hadWeapon);
            }
        }


        public bool FireWeapon()
        {
            if (!CheckAmmo() || PendingWeapon != null)
                return false;

            SetWeaponTop();
            Weapon?.RequestFire();
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

            if (Weapon.FrameState.IsState(Entities.Definition.States.FrameStateLabel.Ready))
                ForceLowerWeapon(setTop);
        }

        private void ForceLowerWeapon(bool setTop)
        {
            if (Weapon != null)
            {
                if (setTop)
                    SetWeaponTop();
                Weapon.FrameState.SetState("DESELECT");
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
                World.SoundManager.CreateSoundOn(this, PendingWeapon.Definition.Properties.Weapons.UpSound, SoundChannelType.Auto, new SoundParams(this));

            AnimationWeapon = PendingWeapon;
            PendingWeapon = null;
            WeaponOffset.Y = Constants.WeaponBottom;
            AnimationWeapon.FrameState.SetState("SELECT");
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

        public override bool Damage(Entity? source, int damage, bool setPainState)
        {
            if (Sector.SectorSpecialType == ZDoomSectorSpecialType.DamageEnd && damage >= Health)
                damage = Health - 1;

            bool damageApplied = base.Damage(source, damage, setPainState);
            if (damageApplied)
            {
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
                    SoundManager.CreateSoundOn(this, "*pain25", SoundChannelType.Auto, new SoundParams(this));
                else if (Health < 51)
                    SoundManager.CreateSoundOn(this, "*pain50", SoundChannelType.Auto, new SoundParams(this));
                else if (Health < 76)
                    SoundManager.CreateSoundOn(this, "*pain75", SoundChannelType.Auto, new SoundParams(this));
                else
                    SoundManager.CreateSoundOn(this, "*pain100", SoundChannelType.Auto, new SoundParams(this));
            }
        }

        public void PlayGruntSound()
        {
            SoundManager.CreateSoundOn(this, "*grunt", SoundChannelType.Auto, new SoundParams(this));
        }

        public void PlayUseFailSound()
        {
            SoundManager.CreateSoundOn(this, "*usefail", SoundChannelType.Auto, new SoundParams(this));
        }

        public void PlayLandSound()
        {
            SoundManager.CreateSoundOn(this, "*land", SoundChannelType.Auto, new SoundParams(this));
        }

        public string GetGenderString() => "male";

        protected override void SetDeath(Entity? source, bool gibbed)
        {
            base.SetDeath(source, gibbed);

            string deathSound = "*death";
            if (gibbed)
                deathSound = "*gibbed";
            else if (Health <= -50)
                deathSound = "*xdeath";

            SoundManager.CreateSoundOn(this, deathSound, SoundChannelType.Auto, new SoundParams(this));
            m_deathTics = MathHelper.Clamp((int)(Definition.Properties.Player.ViewHeight - DeathHeight), 0, (int)Definition.Properties.Player.ViewHeight);
            m_killer = source;

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

        private bool AbleToJump() => OnGround && Velocity.Z == 0 && m_jumpTics == 0 && !IsClippedWithEntity();
    }
}