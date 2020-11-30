using System;
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
        public int DamageCount = 0;
        public TickCommand TickCommand;
        // TODO implement effect on world
        public int ExtraLight;

        private bool m_isJumping;
        private int m_jumpTics;
        private int m_deathTics;
        private double m_prevAngle;
        private double m_prevPitch;
        private double m_viewHeight;
        private double m_prevViewHeight;
        private double m_deltaViewHeight;

        public Weapon? Weapon { get; private set; }
        public Weapon? PendingWeapon { get; private set; }
        public Weapon? AnimationWeapon { get; private set; }
        public int WeaponSlot { get; private set; }
        public int WeaponSubSlot { get; private set; }
        public Vec2I WeaponOffset;

        public override double ViewHeight => m_viewHeight;

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
            m_prevViewHeight = m_viewHeight;

            AddStartItems();
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
            position.Z += m_viewHeight;
            return position;
        }

        public Vec3D GetPrevViewPosition()
        {
            Vec3D position = PrevPosition;
            position.Z += m_prevViewHeight;
            return position;
        }

        public override void SetZ(double z, bool smooth)
        {
            if (smooth && Box.Bottom < z)
            {
                m_viewHeight -= z - Box.Bottom;
                m_deltaViewHeight = (Definition.Properties.Player.ViewHeight - m_viewHeight) / PlayerViewDivider;
                ClampViewHeight();
            }

            base.SetZ(z, smooth);
        }

        public override void ResetInterpolation()
        {
            m_viewHeight = Definition.Properties.Player.ViewHeight;
            m_prevViewHeight = m_viewHeight;
            m_deltaViewHeight = 0;

            base.ResetInterpolation();
        }

        /// <summary>
        /// Sets the entity hitting the floor / another entity.
        /// </summary>
        /// <param name="hardHit">If the player hit hard and should crouch down a bit and grunt.</param>
        public void SetHitZ(bool hardHit)
        {
            // If we're airborne and just landed on the ground, we need a delay
            // for jumping. This should only happen if we've coming down from a manual jump.
            if (m_isJumping)
                m_jumpTics = JumpDelayTicks;

            m_isJumping = false;

            if (hardHit && !Flags.NoGravity && !IsDead)
            {
                PlayLandSound();
                m_deltaViewHeight = Velocity.Z / PlayerViewDivider;
            }
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
            float yaw = (float)AngleRadians;
            float pitch = (float)PitchRadians;

            return new Camera(position.ToFloat(), yaw, pitch);
        }

        public override void Tick()
        {
            base.Tick();
            AnimationWeapon?.Tick();

            m_prevAngle = AngleRadians;
            m_prevPitch = PitchRadians;

            if (m_jumpTics > 0)
                m_jumpTics--;

            m_prevViewHeight = m_viewHeight;
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

            ClampViewHeight();

            if (IsDead)
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
            }
        }

        public override bool GiveItem(EntityDefinition definition, EntityFlags? flags)
        {
            bool ownedWeapon = Inventory.Weapons.OwnsWeapon(definition.Name);
            bool success = GiveWeapon(definition);
            if (success)
            {
                CheckAutoSwitchWeapon(definition, ownedWeapon);
            }
            else
            {
                success = base.GiveItem(definition, flags);
                if (success && IsWeapon(definition))
                    CheckAutoSwitchWeapon(definition, ownedWeapon);
            }

            if (success)
            {
                LastPickupGametick = World.Gametick;
                return true;
            }

            return false;
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
            var weapons = Inventory.Weapons.GetWeapons().OrderBy(x => x.Definition.Properties.Weapons.SelectionOrder);
            foreach (Weapon weapon in weapons)
            {
                if (weapon != Weapon && CheckAmmo(weapon))
                {
                    ChangeWeapon(weapon);
                    break;
                }
            }
        }

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
                PendingWeapon = weapon;

                if (!hadWeapon)
                {
                    Weapon = PendingWeapon;
                    AnimationWeapon = PendingWeapon;
                    WeaponOffset.Y = Constants.WeaponBottom;
                }

                LowerWeapon();
            }
        }

        public bool FireWeapon()
        {
            if (!CheckAmmo())
                return false;

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
        public bool CheckAmmo(Weapon weapon)
        {
            return Inventory.Amount(weapon.Definition.Properties.Weapons.AmmoType) >= weapon.Definition.Properties.Weapons.AmmoUse;
        }

        public bool CanFireWeapon()
        {
            return !IsDead && Weapon != null && TickCommand.Has(TickCommands.Attack) && CheckAmmo();
        }

        public void LowerWeapon()
        {
            if (Weapon == null)
                return;

            if (Weapon.FrameState.IsState(Entities.Definition.States.FrameStateLabel.Ready))
                ForceLowerWeapon();
        }

        private void ForceLowerWeapon()
        {
            if (Weapon != null)
                Weapon.FrameState.SetState("DESELECT");
        }

        public void BringupWeapon()
        {
            if (PendingWeapon == null)
                return;

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

            if (IsDead)
                ForceLowerWeapon();

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

        protected override void SetDeath(bool gibbed)
        {
            base.SetDeath(gibbed);

            string deathSound = "*death";
            if (gibbed)
                deathSound = "*gibbed";
            else if (Health <= -50)  
                deathSound = "*xdeath";

            SoundManager.CreateSoundOn(this, deathSound, SoundChannelType.Auto, new SoundParams(this));
            m_deathTics = MathHelper.Clamp((int)(Definition.Properties.Player.ViewHeight - DeathHeight), 0, (int)Definition.Properties.Player.ViewHeight);
        }

        private void AddStartItems()
        {
            if (Definition.Properties.Player.StartItem == null)
                return;

            foreach (PlayerStartItem item in Definition.Properties.Player.StartItem)
            {
                // TODO: If not an inventory item, don't give.
                // TODO: Give item.
            }
        }

        public void Jump()
        {
            if (AbleToJump())
            {
                m_isJumping = true;
                Velocity.Z += Properties.Player.JumpZ;
            }
        }

        private void ClampViewHeight()
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
                    m_viewHeight = halfPlayerViewHeight;

                if (m_viewHeight < playerViewHeight)
                    m_deltaViewHeight += 0.25;
            }

            m_viewHeight = MathHelper.Clamp(m_viewHeight, ViewHeightMin, LowestCeilingZ - HighestFloorZ - ViewHeightMin);
        }

        private bool AbleToJump() => OnGround && Velocity.Z == 0 && m_jumpTics == 0 && !IsClippedWithEntity();
    }
}