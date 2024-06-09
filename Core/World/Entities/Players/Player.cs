using Helion.Audio;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.Render.Common.World;
using Helion.Render.OpenGL.Shared;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.World.Blockmap;
using Helion.World.Cheats;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Sound;
using Helion.World.StatusBar;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assertion.Assert;
using static Helion.World.Entities.EntityManager;

namespace Helion.World.Entities.Players;

public class Player : Entity
{
    public const double MaxMovement = 30.0;
    public const double ForwardMovementSpeedWalk = 0.78125;
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
    private const double MaxPitch = Camera.MaxPitch;
    private static readonly PowerupType[] PowerupsWithBrightness = { PowerupType.LightAmp, PowerupType.Invulnerable };
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // These are set instantly from mouse movement.
    // They are added when rendering so the view does not need to be interpolated.
    public double ViewAngleRadians;
    public double ViewPitchRadians;

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

    protected double m_prevPitch;
    protected double m_viewZ;
    protected double m_prevViewZ;
    protected bool m_interpolateAngle;

    private bool m_isJumping;
    private bool m_hasNewWeapon;
    private bool m_strafeCommand;
    private int m_jumpTics;
    private int m_deathTics;
    private double m_viewBob;
    private double m_jumpStartZ = double.MaxValue;
    private WeakEntity m_killer = WeakEntity.Default;

    private readonly OldCamera m_camera = new (Vec3F.Zero, Vec3F.Zero, 0, 0);

    public IAudioSource?[] SoundChannels = new IAudioSource[Constants.MaxSoundChannels];

    public Inventory Inventory;
    public Weapon? Weapon;
    public Weapon? PendingWeapon;
    public Weapon? AnimationWeapon;
    public int WeaponSlot;
    public int WeaponSubSlot;
    public Vec2D PrevWeaponOffset;
    public Vec2D WeaponOffset;
    public Vec2D PrevWeaponBobOffset;
    public Vec2D WeaponBobOffset;
    public WeakEntity Attacker = WeakEntity.Default;
    public WeakEntity CrosshairTarget = WeakEntity.Default;
    public PlayerStatusBar StatusBar;
    public PlayerCheats Cheats = new PlayerCheats();
    public PlayerInfo Info = new PlayerInfo();
    public PlayerTracers Tracers = new();
    public bool IsVooDooDoll;
    public bool IsSyncVooDoo;
    public bool Refire;
    public bool AttackDown;
    public double DeltaViewHeight;
    public double ViewHeight;
    public bool WeaponFlashState;
    // Possible line with middle texture clipping player's view.
    public bool ViewLineClip;
    public bool ViewPlaneClip;
    public override Player? PlayerObj => this;
    public override bool IsPlayer => true;
    public override int ProjectileKickBack => Weapon == null ? WorldStatic.World.GameInfo.DefKickBack : Weapon.KickBack;
    public virtual bool IsCamera => false;

    public virtual bool DrawFullBright()
    {
        if (WorldStatic.World.Config.Render.Fullbright)
            return true;

        foreach (PowerupType powerupType in PowerupsWithBrightness)
        {
            IPowerup? powerup = Inventory.GetPowerup(powerupType);
            if (powerup != null)
                return powerup.DrawPowerupEffect;
        }

        return false;
    }

    public bool DrawInvulnerableColorMap() => Inventory.PowerupEffectColorMap != null && Inventory.PowerupEffectColorMap.DrawPowerupEffect;
    public int GetExtraLightRender() => WorldStatic.World.Config.Render.ExtraLight + (ExtraLight * Constants.ExtraLightFactor);

    public override double ViewZ => m_viewZ;
    public override SoundChannel WeaponSoundChannel => SoundChannel.Weapon;
    public override bool IsInvulnerable => Flags.Invulnerable || Inventory.IsPowerupActive(PowerupType.Invulnerable);
    public override bool CanMakeSound() => !IsVooDooDoll;

    public Player(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
        Sector sector, IWorld world, int playerNumber)
    {
        Precondition(playerNumber >= 0, "Player number should not be negative");
        Set(id, thingId, definition, position, angleRadians, sector);

        PlayerNumber = playerNumber;
        // Going to default to true for players, otherwise jumping without moving X/Y can allow for clipping through ceilings
        // See PhysicsManager.MoveZ
        MoveLinked = true;
        Inventory = new Inventory(this, world.EntityManager.DefinitionComposer);

        PrevAngle = AngleRadians;
        ViewHeight = definition.Properties.Player.ViewHeight;
        m_viewZ = m_prevViewZ = Definition.Properties.Player.ViewHeight;

        WeaponOffset.Y = Constants.WeaponBottom;
        PrevWeaponOffset.Y = Constants.WeaponBottom;

        StatusBar = new PlayerStatusBar(this);
        SetPlayerInfo();
        SetupEvents();
    }

    public Player(PlayerModel playerModel, Dictionary<int, EntityModelPair> entities, EntityDefinition definition, IWorld world)
    {
        Precondition(playerModel.Number >= 0, "Player number should not be negative");
        Set(playerModel, definition, world);

        PlayerNumber = playerModel.Number;
        AngleRadians = playerModel.AngleRadians;
        PitchRadians = playerModel.PitchRadians;
        DamageCount = playerModel.DamageCount;
        BonusCount = playerModel.BonusCount;
        ExtraLight = playerModel.ExtraLight;
        m_isJumping = playerModel.IsJumping;
        m_jumpTics = playerModel.JumpTics;
        m_deathTics = playerModel.DeathTics;
        ViewHeight = playerModel.ViewHeight;
        m_viewZ = playerModel.ViewZ;
        DeltaViewHeight = playerModel.DeltaViewHeight;
        WeaponOffset = (playerModel.WeaponOffsetX, playerModel.WeaponOffsetY);
        PrevWeaponOffset = (playerModel.WeaponOffsetX, playerModel.WeaponOffsetY);
        WeaponSlot = playerModel.WeaponSlot;
        WeaponSubSlot = playerModel.WeaponSubSlot;
        KillCount = playerModel.KillCount;
        ItemCount = playerModel.ItemCount;
        SecretsFound = playerModel.SecretsFound;
        AttackDown = playerModel.AttackDown;
        Refire = playerModel.Refire;

        Inventory = new Inventory(playerModel, this, world.EntityManager.DefinitionComposer);

        if (playerModel.Weapon != null)
            Weapon = Inventory.Weapons.GetWeapon(playerModel.Weapon);
        if (playerModel.PendingWeapon != null)
            PendingWeapon = Inventory.Weapons.GetWeapon(playerModel.PendingWeapon);
        if (playerModel.AnimationWeapon != null)
            AnimationWeapon = Inventory.Weapons.GetWeapon(playerModel.AnimationWeapon);

        if (playerModel.Attacker.HasValue && entities.TryGetValue(playerModel.Attacker.Value, out var attacker))
            SetAttacker(attacker.Entity);
        if (playerModel.Killer.HasValue && entities.TryGetValue(playerModel.Killer.Value, out var killer))
            m_killer = WeakEntity.GetReference(killer.Entity);

        PrevAngle = AngleRadians;
        m_prevPitch = PitchRadians;
        m_prevViewZ = m_viewZ;

        m_viewBob = playerModel.Bob;
        if (playerModel.WeaponBobX.HasValue && playerModel.WeaponBobY.HasValue)
        {
            WeaponBobOffset.X = playerModel.WeaponBobX.Value;
            WeaponBobOffset.Y = playerModel.WeaponBobY.Value;
            PrevWeaponBobOffset = WeaponBobOffset;
        }    

        StatusBar = new PlayerStatusBar(this);

        foreach (CheatType cheat in playerModel.Cheats)
            Cheats.SetCheatActive(cheat);

        SetPlayerInfo();
        SetupEvents();
    }

    private void SetupEvents()
    {
        Inventory.Weapons.WeaponsCleared += Inventory_WeaponsCleared;
        Inventory.Weapons.WeaponRemoved += Weapons_WeaponRemoved;
    }

    private void Weapons_WeaponRemoved(object? sender, Weapon removeWeapon)
    {
        if (ReferenceEquals(Weapon, removeWeapon))
        {
            ForceLowerWeapon(setTop: false);
            ForceSwitchWeapon();
        }

        if (ReferenceEquals(PendingWeapon, removeWeapon))
        {
            ForceLowerWeapon(setTop: false);
            ForceSwitchWeapon();
        }
    }

    private void Inventory_WeaponsCleared(object? sender, EventArgs e)
    {
        if (Weapon != null || PendingWeapon != null)
            ForceLowerWeapon(setTop: false);
        PendingWeapon = null;
    }

    public void SetAttacker(Entity? entity) =>
        Attacker = WeakEntity.GetReference(entity);

    public void SetCrosshairTarget(Entity? entity) =>
        CrosshairTarget = WeakEntity.GetReference(entity);

    private void SetPlayerInfo()
    {
        Info.Name = WorldStatic.World.Config.Player.Name;
        Info.Gender = WorldStatic.World.Config.Player.Gender;
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
            ViewHeight = ViewHeight,
            ViewZ = ViewZ,
            DeltaViewHeight = DeltaViewHeight,
            Bob = m_viewBob,
            WeaponBobX = WeaponBobOffset.X,
            WeaponBobY = WeaponBobOffset.Y,
            Killer = m_killer.Entity?.Id,
            Attacker = Attacker.Entity?.Id,
            KillCount = KillCount,
            ItemCount = ItemCount,
            SecretsFound = SecretsFound,
            Weapon = Weapon?.Definition.Name,
            PendingWeapon = PendingWeapon?.Definition.Name,
            AnimationWeapon = AnimationWeapon?.Definition.Name,
            WeaponOffsetX = WeaponOffset.X,
            WeaponOffsetY = WeaponOffset.Y,
            WeaponSlot = WeaponSlot,
            WeaponSubSlot = WeaponSubSlot,
            Inventory = Inventory.ToInventoryModel(),
            AnimationWeaponFrame = AnimationWeapon?.FrameState.ToFrameStateModel(),
            WeaponFlashFrame = AnimationWeapon?.FlashState.ToFrameStateModel(),
            Cheats = Cheats.GetActiveCheats().Cast<int>().ToList(),
            AttackDown = AttackDown,
            Refire = Refire
        };

        ToEntityModel(playerModel);
        return playerModel;
    }

    public void VoodooSync(Player player)
    {
        base.CopyProperties(player);

        // NoClip did not apply to the player
        Flags.NoClip = false;
        var items = player.Inventory.GetInventoryItems();
        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];
            if (!Inventory.HasItem(item.Definition))
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

            foreach (Weapon weapon in player.Inventory.Weapons.GetWeaponsInSelectionOrder())
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
                SetWeaponBottom();
                PendingWeapon = weapon;
                BringupWeapon();
            }
        }

        m_hasNewWeapon = false;
    }

    private void GiveItem(string name, int amount, out EntityDefinition? definition)
    {
        definition = WorldStatic.EntityManager.DefinitionComposer.GetByName(name);
        if (definition == null)
            return;

        if (IsWeapon(definition))
            GiveWeapon(definition, null, giveDefaultAmmo: false, autoSwitch: false);
        else
            GiveItemBase(definition, null, false, amount);
    }

    public bool HasNewWeapon() => m_hasNewWeapon;

    public Vec3D GetViewPosition() =>
        new(Position.X, Position.Y, Position.Z + m_viewZ);

    public Vec3D GetPrevViewPosition() =>
        new(PrevPosition.X, PrevPosition.Y, PrevPosition.Z + m_prevViewZ);

    public void SetAndSmoothZ(double z)
    {
        if (Position.Z < z)
        {
            ViewHeight -= z - Position.Z;
            DeltaViewHeight = (Definition.Properties.Player.ViewHeight - ViewHeight) / PlayerViewDivider;
            SetViewHeight();
        }

        Position.Z = z;
    }

    public override void Hit(in Vec3D velocity)
    {
        if (velocity.Z < 0 && OnGround)
        {
            // If we're airborne and just landed on the ground, we need a delay
            // for jumping. This should only happen if we've coming down from a manual jump.
            if (m_isJumping)
                m_jumpTics = JumpDelayTicks;

            // Check if the player is landing where they started. Otherwise a normal jump would play the oof sound.
            bool hardHit = (WorldStatic.World.Gravity > 1 || Position.Z != m_jumpStartZ) && velocity.Z < -(WorldStatic.World.Gravity * 8);
            if (hardHit && !Flags.NoGravity && !IsDead)
            {
                PlayLandSound();
                DeltaViewHeight = velocity.Z / PlayerViewDivider;
            }

            m_isJumping = false;
            m_jumpStartZ = double.MaxValue;
        }

        if (!Flags.NoGravity && !Flags.NoClip && !IsDead && BlockingLine != null && 
            Sector.Friction > Constants.DefaultFriction && 
            Position.Z <= Sector.Floor.Z &&
            Math.Abs(velocity.X) + Math.Abs(velocity.Y) > 8 && 
            CheckIcyBounceLineAngle(BlockingLine, velocity))
        {
            var existingSound = SoundChannels[(int)SoundChannel.Default];
            if (existingSound == null || !existingSound.AudioData.SoundInfo.Name.EndsWith("*grunt"))
                PlayGruntSound();
            var bounceVelocity = MathHelper.BounceVelocity(velocity.XY, null);
            Velocity.X = bounceVelocity.X/2;
            Velocity.Y = bounceVelocity.Y/2;
        }

        base.Hit(velocity);
    }

    private bool CheckIcyBounceLineAngle(Line line, in Vec3D velocity)
    {
        var onFront = line.Segment.OnRight(Position);
        var velocityAngle = Math.Atan2(velocity.Y, velocity.X);
        var lineAngle = onFront ? line.Segment.Start.Angle(line.Segment.End) : line.Segment.End.Angle(line.Segment.Start);
        var bounceAngle = MathHelper.GetPositiveAngle(velocityAngle - lineAngle);

        return bounceAngle > MathHelper.QuarterPi && bounceAngle < MathHelper.HalfPi + MathHelper.QuarterPi;
    }

    public override void ResetInterpolation()
    {
        ViewHeight = Definition.Properties.Player.ViewHeight;
        m_prevViewZ = m_viewZ;
        PrevAngle = AngleRadians;
        m_prevPitch = PitchRadians;
        DeltaViewHeight = 0;
        PrevWeaponOffset = WeaponOffset;
        PrevWeaponBobOffset = WeaponBobOffset;

        ViewAngleRadians = 0;
        ViewPitchRadians = 0;

        base.ResetInterpolation();
    }

    public override void SetRaiseState(bool restoreFlags = true)
    {
        base.SetRaiseState();
        PendingWeapon = Weapon;
        BringupWeapon();
        m_killer = WeakEntity.GetReference(null);
    }

    public override bool CanDamage(Entity source, DamageType damageType)
    {
        // Always return true for now - this will likely change with multiplayer options
        return true;
    }

    public void AddToYaw(double delta, bool isMouse)
    {
        if (isMouse)
        {
            ViewAngleRadians += delta;
            return;
        }

        AngleRadians = MathHelper.GetPositiveAngle(AngleRadians + delta);
    }

    public void AddToPitch(double delta, bool isMouse)
    {
        if (isMouse)
        {
            ViewPitchRadians = AddPitch(ViewPitchRadians, delta);
            return;
        }

        PitchRadians = AddPitch(PitchRadians, delta);
    }

    private static double AddPitch(double pitch, double delta)
    {        
        pitch = MathHelper.Clamp(pitch + delta, -MaxPitch, MaxPitch);
        return pitch;
    }

    public OldCamera GetCamera(double t)
    {
        Vec3D currentPos = GetViewPosition();
        Vec3D prevPos = GetPrevViewPosition();
        Vec3D position = prevPos.Interpolate(currentPos, t);
        CheckLineClip(currentPos);
        position = CheckPlaneClip(currentPos, prevPos, position);

        double playerAngle = AngleRadians;
        double playerPitch = PitchRadians;

        if (!m_strafeCommand && !WorldStatic.World.Config.Mouse.Interpolate && !IsMaxFpsTickRate())
        {
            playerAngle += ViewAngleRadians;
            playerPitch = MathHelper.Clamp(playerPitch + ViewPitchRadians, -MaxPitch, MaxPitch);
        }

        // When rendering, we always want the most up-to-date values. We
        // would only want to interpolate here if looking at another player
        // and would likely need to add more logic for wrapping around if
        // the player rotates from 359 degrees -> 2 degrees since that will
        // interpolate in the wrong direction.
        if (m_interpolateAngle)
        {
            double prev = MathHelper.GetPositiveAngle(PrevAngle);
            double current = MathHelper.GetPositiveAngle(playerAngle);
            double diff = Math.Abs(prev - current);

            if (diff >= MathHelper.Pi)
            {
                if (prev > current)
                    prev -= MathHelper.TwoPi;
                else
                    current -= MathHelper.TwoPi;
            }

            float yaw = (float)(prev + t * (current - prev));
            float pitch = (float)(m_prevPitch + t * (playerPitch - m_prevPitch));
            m_camera.Set(position.Float, GetViewPosition().Float, yaw, pitch);
        }
        else
        {
            float yaw = (float)MathHelper.GetPositiveAngle(playerAngle);
            float pitch = (float)(playerPitch);
            m_camera.Set(position.Float, GetViewPosition().Float, yaw, pitch);
        }

        return m_camera;
    }

    private unsafe void CheckLineClip(in Vec3D pos)
    {
        ViewLineClip = false;
        var box = new Box2D(pos.X, pos.Y, Radius);
        var grid = WorldStatic.World.BlockmapTraverser.BlockmapGrid;
        var it = grid.CreateBoxIteration(box);
        for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
        {
            for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
            {
                Block block = grid[by * it.Width + bx];
                for (int i = 0; i < block.BlockLines.Length; i++)
                {
                    fixed (BlockLine* blockLine = &block.BlockLines.Data[i])
                    {
                        if (!box.Intersects(blockLine->Segment))
                            continue;

                        var line = blockLine->Line;
                        if (line.Front.Middle.TextureHandle != Constants.NoTextureIndex ||
                            (line.Back != null && line.Back.Middle.TextureHandle != Constants.NoTextureIndex))
                        {
                            ViewLineClip = true;
                            return;
                        }
                    }
                }
            }
        }
    }

    public override void Tick()
    {
        base.Tick();

        // Matching Doom behavior for A_Saw
        if (Flags.JustAttacked)
        {
            TickCommand.AngleTurn = 0;
            TickCommand.SideMoveSpeed = 0;
            TickCommand.ForwardMoveSpeed = 3.125;
            Flags.JustAttacked = false;
        }

        Inventory.Tick();
        AnimationWeapon?.Tick();
        StatusBar.Tick();

        // Match Boom functionality that continually checks to change weapons in G_BuildTickCmd
        if (AttackDown && !CheckAmmo() && PendingWeapon == null)
            TrySwitchWeapon();

        m_interpolateAngle = ShouldInterpolate();

        PrevAngle = AngleRadians;
        m_prevPitch = PitchRadians;
        m_prevViewZ = m_viewZ;

        PrevWeaponOffset = WeaponOffset;
        PrevWeaponBobOffset = WeaponBobOffset;

        if (m_jumpTics > 0)
            m_jumpTics--;

        ViewHeight += DeltaViewHeight;

        if (DamageCount > 0)
            DamageCount--;

        if (BonusCount > 0)
            BonusCount--;

        if (m_deathTics > 0)
        {
            m_deathTics--;
            if (ViewHeight > DeathHeight)
                ViewHeight -= 1.0;
            else
                m_deathTics = 0;
        }

        SetBob();
        SetViewHeight();
        SetRunningFrameState();

        if (IsDead)
            DeathTick();
        
        m_hasNewWeapon = false;
    }

    public override void SoundCreated(SoundInfo soundInfo, IAudioSource? audioSource, SoundChannel channel)
    {
        SoundChannels[(int)channel] = audioSource;
    }

    public override bool TryClearSound(string sound, SoundChannel channel, out IAudioSource? clearedSound)
    {
        IAudioSource? audioSource = SoundChannels[(int)channel];
        if (audioSource != null)
        {
            clearedSound = audioSource;
            SoundChannels[(int)channel] = null;
            return true;
        }

        clearedSound = null;
        return false;
    }

    public override void ClearSound(IAudioSource audioSource, SoundChannel channel)
    {
        SoundChannels[(int)channel] = null;
    }

    private Vec3D CheckPlaneClip(Vec3D pos, Vec3D prevPos, Vec3D interpolatedPos)
    {
        ViewPlaneClip = false;
        if (Sector.TransferHeights == null)
            return interpolatedPos;

        var transferView = TransferHeights.GetView(Sector, pos.Z);
        var prevTransferView = TransferHeights.GetView(Sector, prevPos.Z);
        var transferViewInterpolated = TransferHeights.GetView(Sector, interpolatedPos.Z);
        var sector = Sector.GetRenderSector(Sector, interpolatedPos.Z);

        double viewZ = interpolatedPos.Z;
        double viewClipZ = transferViewInterpolated == TransferHeightView.Middle ? sector.Floor.Z : sector.Ceiling.Z;
        double viewDiff = Math.Abs(viewZ - viewClipZ);
        if (viewDiff < 10)
            ViewPlaneClip = true;

        // Check for when the transfer height plane is really close to the player camera z
        // This forces the view to be +0.25 or -0.25 of the transfer heights floor
        // Otherwise the clip is too close and looks terrible
        const double MinPlaneView = 0.25;
        double viewOffsetZ = 0;
        if (viewDiff < MinPlaneView)
            viewOffsetZ = transferView == TransferHeightView.Middle ? MinPlaneView : -MinPlaneView;

        // Can't interpolate z if changing transfer heights views
        if (transferView != prevTransferView || viewOffsetZ != 0)
            return new Vec3D(interpolatedPos.X, interpolatedPos.Y, pos.Z + viewOffsetZ);

        interpolatedPos.Z += viewOffsetZ;
        return interpolatedPos;
    }

    private void SetRunningFrameState()
    {
        if (!Definition.SeeState.HasValue)
            return;

        bool hasMoveSpeed = TickCommand.ForwardMoveSpeed > 0 || TickCommand.SideMoveSpeed > 0;

        // Toggle between spawn and see states (stopped and running) based on movement
        if (hasMoveSpeed && Definition.SpawnState.HasValue &&
            FrameState.Frame.MasterFrameIndex == Definition.SpawnState.Value &&
            Definition.SeeState.HasValue)
        {
            FrameState.SetFrameIndex(Definition.SeeState.Value);
        }
        else if (!hasMoveSpeed && Velocity == Vec3D.Zero && Definition.SpawnState.HasValue &&
            FrameState.Frame.MasterFrameIndex != Definition.SpawnState.Value &&
            // Doom hard-coded this to check for any of the 4 running states S_PLAY_RUN1 - S_PLAY_RUN4
            FrameState.Frame.MasterFrameIndex - Definition.SeeState.Value < 4)
        {
            FrameState.SetFrameIndex(Definition.SpawnState.Value);
        }
    }

    private bool IsMaxFpsTickRate() =>
        WorldStatic.World.Config.Render.MaxFPS != 0 && WorldStatic.World.Config.Render.MaxFPS <= Constants.TicksPerSecond;

    protected bool ShouldInterpolate()
    {
        if (IsMaxFpsTickRate())
            return false;

        if (WorldStatic.World.Config.Mouse.Interpolate)
            return true;

        return TickCommand.AngleTurn != 0 || TickCommand.PitchTurn != 0 || IsDead || WorldStatic.World.PlayingDemo;
    }

    public void HandleTickCommand()
    {
        m_strafeCommand = TickCommand.Has(TickCommands.Strafe);
        if (TickCommand.Has(TickCommands.Use))
            WorldStatic.World.EntityUse(this);

        if (IsDead || IsFrozen)
            return;

        if (TickCommand.AngleTurn != 0 && !m_strafeCommand)
            AddToYaw(TickCommand.AngleTurn, false);

        if (TickCommand.PitchTurn != 0)
            AddToPitch(TickCommand.PitchTurn, false);

        Vec3D movement = Vec3D.Zero;
        movement += CalculateForwardMovement(TickCommand.ForwardMoveSpeed);
        movement += CalculateStrafeMovement(TickCommand.SideMoveSpeed);

        if (TickCommand.ForwardMoveSpeed != 0 || TickCommand.SideMoveSpeed != 0)
        {
            double moveFactor = PhysicsManager.GetMoveFactor(this);
            movement.X *= moveFactor;
            movement.Y *= moveFactor;
        }

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

        if (TickCommand.WeaponScroll != 0)
        {
            var slot = Inventory.Weapons.GetNextSlot(this, TickCommand.WeaponScroll);
            ChangePlayerWeaponSlot(slot);
        }
        else if (TickCommand.Has(TickCommands.NextWeapon))
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
            Weapon? weapon = null;
            if (WeaponSlot == slot)
            {
                int subslotCount = Inventory.Weapons.GetSubSlots(slot);
                if (subslotCount > 0)
                {
                    int subslot = (WeaponSubSlot + 1) % subslotCount;
                    weapon = Inventory.Weapons.GetWeapon(this, slot, subslot);
                }
            }
            else
            {
                weapon = Inventory.Weapons.GetWeapon(this, slot);
            }

            if (weapon != null)
                ChangeWeapon(weapon);
        }

        if (TickCommand.Has(TickCommands.CenterView))
            PitchRadians = 0;

        if (!m_strafeCommand)
        {
            AngleRadians += MathHelper.GetPositiveAngle(TickCommand.MouseAngle);
            PitchRadians = AddPitch(PitchRadians, TickCommand.MousePitch);
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

    private static TickCommands GetWeaponSlotCommand(TickCommand tickCommand)
    {
        for (int i = 0; i < WeaponSlotCommands.Length; i++)
        {
            if (tickCommand.Has(WeaponSlotCommands[i]))
                return WeaponSlotCommands[i];
        }
        return TickCommands.None;
    }

    private void ChangePlayerWeaponSlot(WeaponSlot slot)
    {
        if (slot.Slot != WeaponSlot || slot.SubSlot != WeaponSubSlot)
        {
            var weapon = Inventory.Weapons.GetWeapon(this, slot.Slot, slot.SubSlot);
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

        if (m_killer.Entity != null)
        {
            double angle = MathHelper.GetPositiveAngle(Position.Angle(m_killer.Entity.Position));
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
        m_viewBob = CalculateBob(WorldStatic.World.Config.Hud.ViewBob);
        if (Weapon != null && Weapon.ReadyToFire)
        {
            const double WeaponSwayMultiplier = Math.PI / 32;
            double value = WeaponSwayMultiplier * WorldStatic.World.LevelTime;
            var weaponBob = CalculateBob(WorldStatic.World.Config.Hud.WeaponBob);
            WeaponBobOffset = (weaponBob * Math.Cos(value % MathHelper.TwoPi), weaponBob * Math.Sin(value % MathHelper.Pi));
        }

        double angle = MathHelper.TwoPi / 20 * WorldStatic.World.LevelTime % MathHelper.TwoPi;
        m_viewBob = m_viewBob / 2 * Math.Sin(angle);
    }

    private double CalculateBob(double bobAmount) => 
        Math.Min(16, ((Velocity.X * Velocity.X) + (Velocity.Y * Velocity.Y)) / 4) * bobAmount;

    public bool GiveItem(EntityDefinition definition, EntityFlags? flags, bool pickupFlash = true)
    {
        if (IsDead)
            return false;

        bool success = GiveWeapon(definition, flags);
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
            EntityDefinition? ammoDef = WorldStatic.EntityManager.DefinitionComposer.GetByName(definition.Properties.Weapons.AmmoType);
            if (ammoDef != null)
                return AddAmmo(ammoDef, definition.Properties.Weapons.AmmoGive, flags, autoSwitchWeapon);

            return false;
        }

        if (definition.IsType(Inventory.BackPackBaseClassName))
        {
            Inventory.AddBackPackAmmo(WorldStatic.EntityManager.DefinitionComposer);
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
        bool success = Inventory.Add(ammoDef, WorldStatic.World.SkillDefinition.GetAmmoAmount(amount, flags), flags);
        if (success && autoSwitchWeapon)
            CheckAutoSwitchAmmo(ammoDef, oldCount);
        return success;
    }

    public double GetForwardMovementSpeed()
    {
        if (TickCommand.IsFastSpeed(WorldStatic.World.Config.Game.AlwaysRun))
            return ForwardMovementSpeedRun;

        return ForwardMovementSpeedWalk;
    }

    public double GetSideMovementSpeed()
    {
        if (TickCommand.IsFastSpeed(WorldStatic.World.Config.Game.AlwaysRun))
            return SideMovementSpeedRun;

        return SideMovementSpeedWalk;
    }

    public double GetTurnAngle()
    {
        if (TurnTics < SlowTurnTicks)
            return SlowTurnSpeed;
        if (TickCommand.IsFastSpeed(WorldStatic.World.Config.Game.AlwaysRun))
            return FastTurnSpeed;

        return NormalTurnSpeed;
    }

    public void GiveAllWeapons(EntityDefinitionComposer definitionComposer)
    {
        foreach (string name in Inventory.Weapons.GetWeaponDefinitionNames())
        {
            var weapon = definitionComposer.GetByName(name);
            if (weapon != null)
                GiveWeapon(weapon, autoSwitch: false);
        }

        Inventory.GiveAllAmmo(definitionComposer);
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
            IList<EntityDefinition> definitions = WorldStatic.EntityManager.DefinitionComposer.GetEntityDefinitions();
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
        if (Weapon != null && !Weapon.Definition.Flags.WeaponWimpyWeapon)
            return;

        string name = Inventory.GetBaseInventoryName(ammoDef);
        var weapons = Inventory.Weapons.GetWeaponsInSelectionOrder();
        Weapon? ammoWeapon = null;
        foreach (var weapon in weapons)
        {
            if (weapon.AmmoDefinition != null && weapon.AmmoDefinition.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                ammoWeapon = weapon;
                break;
            }
        }

        if (ammoWeapon != null)
        {
            if (CheckAmmo(ammoWeapon, oldCount))
                return;

            if (Weapon == null ||
                ammoWeapon.Definition.Properties.Weapons.SelectionOrder < Weapon.Definition.Properties.Weapons.SelectionOrder)
            {
                // Only switch to rocket launcher on fist.
                if (Weapon != null && !Weapon.Definition.Flags.WeaponWimpyWeapon && ammoWeapon.Definition.Flags.WeaponNoAutoSwitch)
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
        var weapons = Inventory.Weapons.GetWeaponsInSelectionOrder();
        foreach (Weapon weapon in weapons)
        {
            if (weapon != Weapon && CheckAmmo(weapon) &&
                !weapon.Definition.Flags.WeaponNoAutoSwitch)
            {
                ChangeWeapon(weapon);
                break;
            }
        }
    }

    public bool ForceSwitchWeapon()
    {
        var weapons = Inventory.Weapons.GetWeaponsInSelectionOrder();
        if (weapons.Count == 0)
        {
            ForceLowerWeapon(setTop: false);
            return false;
        }

        ChangeWeapon(weapons.First());
        return true;
    }

    public bool GiveWeapon(EntityDefinition definition, EntityFlags? flags = null, bool giveDefaultAmmo = true, bool autoSwitch = true)
    {
        if (IsWeapon(definition) && !Inventory.Weapons.OwnsWeapon(definition))
        {
            Weapon? addedWeapon = Inventory.Weapons.Add(definition, this, WorldStatic.EntityManager);
            if (giveDefaultAmmo)
                GiveItemBase(definition, flags, autoSwitch);

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
        if (!Inventory.Weapons.OwnsWeapon(weapon.Definition) || Weapon == weapon || AnimationWeapon == weapon)
            return;

        bool hadWeapon = Weapon != null;
        var slot = Inventory.Weapons.GetWeaponSlot(weapon.Definition);
        WeaponSlot = slot.Slot;
        WeaponSubSlot = slot.SubSlot;

        if (!hadWeapon && PendingWeapon == null)
        {
            Weapon = weapon;
            AnimationWeapon = weapon;
            WeaponOffset.Y = Constants.WeaponBottom;
        }

        PendingWeapon = weapon;
        LowerWeapon(hadWeapon);
        if (!hadWeapon)
            ForceLowerWeapon(setTop: false);
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
        SetFireState();

        if (!Weapon.Definition.Flags.WeaponNoAlert)
            WorldStatic.World.NoiseAlert(this, this);

        return true;
    }

    public void SetFireState()
    {
        if (Weapon == null)
            return;

        if (Weapon.Definition.Flags.WeaponMeleeWeapon)
        {
            if (Definition.MissileState.HasValue)
                FrameState.SetFrameIndex(Definition.MissileState.Value);
            return;
        }

        if (Definition.MeleeState.HasValue)
            FrameState.SetFrameIndex(Definition.MeleeState.Value);
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
        // Inifinite if no ammo type (fist, chainsaw)
        string ammoType = weapon.Definition.Properties.Weapons.AmmoType;
        if (ammoType.Length == 0)
            return true;

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

        if (Weapon.ReadyState)
            ForceLowerWeapon(setTop);
    }

    public void ForceLowerWeapon(bool setTop)
    {
        if (Weapon != null)
        {
            if (setTop)
                SetWeaponTop();
            SetWeaponFrameState(Weapon, Constants.FrameStates.Deselect);
        }
    }

    private void SetWeaponTop()
    {
        WeaponOffset.X = PrevWeaponOffset.X = 0;
        WeaponOffset.Y = PrevWeaponOffset.Y = Constants.WeaponTop;
        WeaponBobOffset = PrevWeaponBobOffset = Vec2D.Zero;
    }

    private void SetWeaponBottom()
    {
        WeaponOffset.X = PrevWeaponOffset.X = 0;
        WeaponOffset.Y = PrevWeaponOffset.Y = Constants.WeaponBottom;
        WeaponBobOffset = PrevWeaponBobOffset = Vec2D.Zero;
    }

    public void BringupWeapon()
    {
        if (PendingWeapon == null)
        {
            // The Weapon reference exists on clear inventory while lowering the weapon. Need to clear the reference if no longer owned.
            if (Weapon != null && !Inventory.Weapons.OwnsWeapon(Weapon.Definition))
            {
                Weapon = null;
                AnimationWeapon = null;
            }
            return;
        }

        if (PendingWeapon.Definition.Properties.Weapons.UpSound.Length > 0)
            WorldStatic.SoundManager.CreateSoundOn(this, PendingWeapon.Definition.Properties.Weapons.UpSound, new SoundParams(this, channel: SoundChannel.Weapon));

        AnimationWeapon = PendingWeapon;
        Weapon = PendingWeapon;
        PendingWeapon = null;
        WeaponOffset.Y = Constants.WeaponBottom;
        SetWeaponFrameState(AnimationWeapon, Constants.FrameStates.Select);
    }

    private void SetWeaponFrameState(Weapon weapon, string label)
    {
        weapon.FrameState.SetState(label);
    }

    public void SetWeaponUp()
    {
        Weapon = AnimationWeapon;
    }

    public void DecreaseAmmoCompatibility(int amount = 0)
    {
        if (Weapon == null)
            return;

        // Doom hard coded the decrease amounts for each weapon fire. Have to check if ammo use was changed via dehacked.
        // Handles example case where weapon is rocket launcher but fire calls A_FireBFG.
        if (amount <= 0 || (Weapon.AmmoDefinition != null && Weapon.AmmoDefinition.Properties.Weapons.AmmoUseSet))
            amount = Weapon.Definition.Properties.Weapons.AmmoUse;

        Inventory.Remove(Weapon.Definition.Properties.Weapons.AmmoType, amount);
    }

    public void DecreaseAmmo(int amount)
    {
        if (Weapon == null)
            return;

        Inventory.Remove(Weapon.Definition.Properties.Weapons.AmmoType, amount);
    }

    public void AddAmmo(int amount)
    {
        if (Weapon == null)
            return;
        var weapon = Weapon.Definition.Properties.Weapons;
        if (weapon.AmmoTypeDef == null)
            weapon.AmmoTypeDef = WorldStatic.World.EntityManager.DefinitionComposer.GetByName(weapon.AmmoType);
        if (weapon.AmmoTypeDef != null)
            Inventory.Add(weapon.AmmoTypeDef, amount);
    }

    public override bool Damage(Entity? source, int damage, bool setPainState, DamageType damageType)
    {
        if (Inventory.IsPowerupActive(PowerupType.Invulnerable))
            return false;

        if (Sector.SectorSpecialType == ZDoomSectorSpecialType.DamageEnd && damage >= Health)
            damage = Health - 1;

        damage = WorldStatic.World.SkillDefinition.GetDamage(damage);

        bool damageApplied = base.Damage(source, damage, setPainState, damageType);
        if (damageApplied)
        {
            SetAttacker(source?.Owner.Entity ?? source);
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
                WorldStatic.SoundManager.CreateSoundOn(this, "*pain25", new SoundParams(this));
            else if (Health < 51)
                WorldStatic.SoundManager.CreateSoundOn(this, "*pain50", new SoundParams(this));
            else if (Health < 76)
                WorldStatic.SoundManager.CreateSoundOn(this, "*pain75", new SoundParams(this));
            else
                WorldStatic.SoundManager.CreateSoundOn(this, "*pain100", new SoundParams(this));
        }
    }

    public void PlayGruntSound()
    {
        WorldStatic.SoundManager.CreateSoundOn(this, "*grunt", new SoundParams(this));
    }

    public void PlayUseFailSound()
    {
        WorldStatic.SoundManager.CreateSoundOn(this, "*usefail", new SoundParams(this));
    }

    public void PlayLandSound()
    {
        WorldStatic.SoundManager.CreateSoundOn(this, "*land", new SoundParams(this));
    }

    protected override void SetDeath(Entity? source, bool gibbed)
    {
        base.SetDeath(source, gibbed);
        m_deathTics = MathHelper.Clamp((int)(Definition.Properties.Player.ViewHeight - DeathHeight), 0, (int)Definition.Properties.Player.ViewHeight);

        if (source != null)
            m_killer = WeakEntity.GetReference(source.Owner.Entity ?? source);
        if (m_killer.Entity == this)
            m_killer = WeakEntity.GetReference(null);

        ForceLowerWeapon(true);
    }

    public void Jump()
    {
        if (AbleToJump())
        {
            m_jumpStartZ = Position.Z;
            m_isJumping = true;
            Velocity.Z += Properties.Player.JumpZ;
        }
    }

    private void SetViewHeight()
    {
        double playerViewHeight = IsDead && m_deathTics == 0 ? DeathHeight : Definition.Properties.Player.ViewHeight;
        double halfPlayerViewHeight = playerViewHeight / 2;

        if (ViewHeight > playerViewHeight)
        {
            DeltaViewHeight = 0;
            ViewHeight = playerViewHeight;
        }

        if (m_deathTics == 0)
        {
            if (ViewHeight < halfPlayerViewHeight)
            {
                ViewHeight = halfPlayerViewHeight;
                if (DeltaViewHeight < 0)
                    DeltaViewHeight = 0;
            }

            if (ViewHeight < playerViewHeight)
                DeltaViewHeight += 0.25;
        }

        m_viewZ = MathHelper.Clamp(ViewHeight + m_viewBob, ViewHeightMin, LowestCeilingZ - HighestFloorZ - ViewHeightMin);
    }

    private bool AbleToJump() => OnGround && Velocity.Z == 0 && m_jumpTics == 0 && !WorldStatic.World.MapInfo.HasOption(MapOptions.NoJump) && !IsClippedWithEntity();

    public override bool Equals(object? obj)
    {
        if (obj is not Player player)
            return false;

        return player.PlayerNumber == PlayerNumber &&
            player.PitchRadians == PitchRadians &&
            player.DamageCount == DamageCount &&
            player.BonusCount == BonusCount &&
            player.ExtraLight == ExtraLight &&
            player.KillCount == KillCount &&
            player.ItemCount == ItemCount &&
            player.SecretsFound == SecretsFound &&
            player.m_isJumping == m_isJumping &&
            player.m_jumpTics == m_jumpTics &&
            player.m_deathTics == m_deathTics &&
            player.ViewHeight == ViewHeight &&
            player.m_viewZ == m_viewZ &&
            player.DeltaViewHeight == DeltaViewHeight &&
            player.m_viewBob == m_viewBob &&
            player.m_killer.Entity?.Id == m_killer.Entity?.Id;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
