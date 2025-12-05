using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Models;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Profiling;
using Helion.Util.RandomGenerators;
using Helion.Window;
using Helion.Window.Input;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Physics;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assertion.Assert;
using static Helion.World.Entities.EntityManager;

namespace Helion.World.Impl.SinglePlayer;

public class SinglePlayerWorld : WorldBase
{
    private static bool SoundsCached;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly CheatType[] ChaseCameraCheats = [CheatType.AutoMapModeShowAllLines, CheatType.AutoMapModeShowAllLinesAndThings];
    private readonly AutomapMarker m_automapMarker;
    private readonly Dictionary<string, byte[]> m_musicLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<int> m_renderDistanceOverrideTags = [];
    private bool m_chaseCamMode;
    private WorldType m_worldType = WorldType.SinglePlayer;
    private int m_renderDistanceOverride;
    private bool m_firstUpdate = true;

    public override WorldType WorldType => m_worldType;
    public override Player Player { get; protected set; }
    public readonly Player ChaseCamPlayer;
    public override bool IsChaseCamMode => m_chaseCamMode;
    public override Player GetCameraPlayer()
    {
        if (m_chaseCamMode)
            return ChaseCamPlayer;
        return EntityManager.GetRealPlayer(0) ?? Player;
    }

    private bool IsMultiPlayer => m_worldType != WorldType.SinglePlayer;

    public SinglePlayerWorld(GlobalData globalData, IConfig config, ArchiveCollection archiveCollection,
        IAudioSystem audioSystem, Profiler profiler, MapGeometry geometry, MapInfoDef mapDef, SkillDef skillDef,
        IMap map, bool sameAsPreviousMap, Player? existingPlayer = null, WorldModel? worldModel = null, IRandom? random = null, bool reuse = true, int playerSpawnArg0 = 0)
        : base(globalData, config, archiveCollection, audioSystem, profiler, geometry, mapDef, skillDef, map, worldModel, random, sameAsPreviousMap, reuse)
    {
        m_worldType = config.Game.SoloNet ? WorldType.Cooperative : WorldType.SinglePlayer;

        if (worldModel == null)
        {
            EntityManager.PopulateFrom(map, LevelStats);

            var spawns = EntityManager.SpawnLocations.GetPlayerSpawns(0);

            if (playerSpawnArg0 == 0)
            {
                if (spawns.Count == 0)
                    throw new HelionException("No player 1 starts");
                Player = EntityManager.CreatePlayer(0, spawns[^1]);
            }
            else
            {
                var spawnLocation = EntityManager.SpawnLocations.GetPlayerSpawnByArg0(playerSpawnArg0) ?? throw new HelionException($"No player starts with arg0 {playerSpawnArg0}");
                Player = EntityManager.CreatePlayer(0, spawnLocation);
            }

            // Make voodoo dolls
            for (int i = spawns.Count - 2; i >= 0; i--)
            {
                var player = EntityManager.CreatePlayer(0, spawns[i], CreatePlayerOptions.VooDooDoll);
                player.SetDefaultInventory();
            }

            if (existingPlayer != null && !existingPlayer.IsDead() && !mapDef.HasOption(MapOptions.ResetInventory) && !config.Game.PistolStart)
            {
                Player.CopyProperties(existingPlayer);
                Player.Inventory.ClearKeys();
                Player.Flags.ClearShadow();
            }
            else
            {
                Player.SetDefaultInventory();
            }

            if (mapDef.HasOption(MapOptions.ResetHealth))
                Player.Health = Player.Properties.Health;
        }
        else
        {
            var result = EntityManager.PopulateFrom(worldModel);
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

            EntityManager.FinalizeFromWorldLoad(result);
            SpecialManager.AddSpecialModels(worldModel);
        }

        // Allocating on the save can take a significant amount of time on the first save. Ensure it has most of the required size plus buffer.
        EnsureEntityModelSize(EntityManager.EntityCount + (int)(EntityManager.EntityCount * 0.25));

        var bspTree = Geometry.GetBspTree();
        if (config.Game.MonsterCloset.Value && bspTree != null)
            ClosetClassifier.Classify(this, bspTree, worldModel != null);
        else if (worldModel != null)
            ClearMonsterClosets();

        CheatManager.CheatActivationChanged += Instance_CheatActivationChanged;

        config.Player.Name.OnChanged += PlayerName_OnChanged;
        config.Player.Gender.OnChanged += PlayerGender_OnChanged;
        config.Render.AutomapBspThread.OnChanged += AutomapBspThread_OnChanged;
        config.Game.MarkSpecials.OnChanged += MarkSpecials_OnChanged;

        ChaseCamPlayer = CreateChaseCamPlayer();

        CheckDistanceOverride();

        config.Render.MaxDistance.ResetToUserValue();

        m_automapMarker = new AutomapMarker(ArchiveCollection);
        CacheSounds();
        CacheMusic();
    }

    private void CheckDistanceOverride()
    {
        if (CompatibilityMapDefinition != null && CompatibilityMapDefinition.MaxDistanceOverride > 0)
        {
            foreach (var tag in CompatibilityMapDefinition.MaxDistanceOverrideTags)
                m_renderDistanceOverrideTags.Add(tag);
            m_renderDistanceOverride = CompatibilityMapDefinition.MaxDistanceOverride;
        }
    }

    private CameraPlayer CreateChaseCamPlayer()
    {
        var player = EntityManager.CreateCameraPlayer(Player);
        player.Flags.SetInvisible();
        player.Flags.SetNoClip();
        player.Flags.SetNoGravity();
        player.Flags.SetFly();
        player.Flags.SetNoBlockmap();
        player.Flags.SetNoSector();
        return player;
    }

    // Intended for unit testing
    public void SetWorldType(WorldType worldType)
    {
        m_worldType = worldType;
    }

    private void CacheSounds()
    {
        if (SoundsCached)
            return;

        SoundsCached = true;
        List<SoundInfo> sounds = new(256);
        ArchiveCollection.Definitions.SoundInfo.GetSounds(sounds);
        foreach (var sound in sounds)
            SoundManager.CacheSound(sound.Name);
    }

    private void CacheMusic()
    {
        for (int i = 0; i < Lines.Count; i++)
        {
            var line = Lines[i];
            if (line.MusicChangeFront != null)
                CacheMusic(line.MusicChangeFront);
            if (line.MusicChangeBack != null)
                CacheMusic(line.MusicChangeBack);
        }
    }

    private void CacheMusic(string name)
    {
        if (m_musicLookup.ContainsKey(name))
            return;

        GetMusicEntry(name, out _, out var entry);
        if (entry == null)
            return;

        m_musicLookup[name] = entry.ReadData();
    }

    private void ClearMonsterClosets()
    {
        for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
        {
            if ((entity.ClosetFlags & ClosetFlags.MonsterCloset) == 0)
                continue;
            entity.ClearMonsterCloset();
        }
    }

    private void MarkSpecials_OnChanged(object? sender, bool e)
    {
        MarkSpecials.Clear(this, Player);
    }

    private void AutomapBspThread_OnChanged(object? sender, bool set)
    {
        m_automapMarker.Stop();

        if (!set)
            return;

        m_automapMarker.Start(this);
    }

    public override ListenerParams GetListener()
    {
        var player = GetCameraPlayer();
        return new ListenerParams(player, player.PitchRadians);
    }

    public override void Tick()
    {
        if (Config.Render.AutomapBspThread)
        {
            var camera = Player.GetCamera(0);
            m_automapMarker.AddPosition(camera.PositionInterpolated.Double, camera.Direction.Double, Player.AngleRadians, Player.PitchRadians);
        }

        if (GetCrosshairTarget(out Entity? entity))
            Player.SetCrosshairTarget(entity);
        else
            Player.SetCrosshairTarget(null);

        if (m_chaseCamMode)
            TickChaseCamPlayer();

        base.Tick();
    }

    private void TickChaseCamPlayer()
    {
        bool ignore = AnyLayerObscuring || DrawPause;
        if (ignore)
        {
            ChaseCamPlayer.ResetInterpolation();
            return;
        }

        if (ChaseCamPlayer == null || ignore)
            return;

        ChaseCamPlayer.HandleTickCommand();
        ChaseCamPlayer.TickCommand.TickHandled();
        ChaseCamPlayer.Tick();
        PhysicsManager.Move(ChaseCamPlayer);
    }

    private bool GetCrosshairTarget(out Entity? entity)
    {
        var cameraPlayer = GetCameraPlayer();
        if (Config.Game.AutoAim)
            GetAutoAimEntity(cameraPlayer, cameraPlayer.HitscanAttackPos, cameraPlayer.AngleRadians, Constants.EntityShootDistance, out _, out entity);
        else
            entity = FireHitscan(cameraPlayer, cameraPlayer.AngleRadians, cameraPlayer.PitchRadians, Constants.EntityShootDistance, Constants.HitscanTestDamage);

        return entity != null && !entity.Flags.Friendly() && entity.Health > 0;
    }

    private void PlayerName_OnChanged(object? sender, string name) => Player.Info.Name = name;
    private void PlayerGender_OnChanged(object? sender, PlayerGender gender) => Player.Info.Gender = gender;

    private void ApplyCheats(WorldModel worldModel)
    {
        for (int i = 0; i < worldModel.Players.Count; i++)
        {
            var playerModel = worldModel.Players[i];
            Player? player = EntityManager.Players.FirstOrDefault(x => x.Id == playerModel.Id);
            if (player == null)
                continue;

            foreach (var cheat in playerModel.Cheats)
                player.Cheats.SetCheatActive((CheatType)cheat);
        }
    }

    private void CreateDamageSpecials(WorldModel worldModel)
    {
        for (int i = 0; i < worldModel.DamageSpecials.Count; i++)
        {
            var model = worldModel.DamageSpecials[i];
            if (!((IWorld)this).IsSectorIdValid(model.SectorId))
                continue;

            Sectors[model.SectorId].SectorDamageSpecial = model.ToWorldSpecial(this);
        }
    }

    private void ApplyLineModels(WorldModel worldModel)
    {
        var lines = worldModel.Lines;
        for (int i = 0; i < worldModel.Lines.Count; i++)
        {
            var id = lines[i].Id;
            if (id < 0 || id >= Lines.Count)
                continue;

            var line = Lines[id];
            line.ApplyLineModel(this, lines[i]);
            ref StructLine structLine = ref StructLines.Data[id];
            structLine.Update(line);
        }
    }

    private void ApplySectorModels(WorldModel worldModel, WorldModelPopulateResult result)
    {
        var sectors = worldModel.Sectors;
        for (int i = 0; i < worldModel.Sectors.Count; i++)
        {
            var id = sectors[i].Id;
            if (id < 0 || id >= Sectors.Count)
                continue;

            Sectors[id].ApplySectorModel(this, sectors[i], result);
        }
    }

    ~SinglePlayerWorld()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public override void Start(WorldModel? worldModel)
    {
        base.Start(worldModel);
        var musicName = worldModel?.MusicName ?? MapInfo.Music;
        if (!PlayLevelMusic(musicName, null))
            AudioSystem.Music.Stop();

        if (Config.Render.AutomapBspThread.Value)
            m_automapMarker.Start(this);
    }

    public override bool PlayLevelMusic(string name, byte[]? data, MusicFlags flags = MusicFlags.Loop)
    {
        base.PlayLevelMusic(name, data, flags);
        GetMusicEntry(name, out var lookup, out var entry);

        if (data == null)
            m_musicLookup.TryGetValue(lookup, out data);

        if (entry == null)
        {
            Log.Warn("Cannot find music track: {0}", lookup);
            return false;
        }

        InvokeMusicChange(entry, flags);
        if (data != null)
            return PlayMusic(data, flags);

        return PlayMusic(entry.ReadData(), flags);
    }

    private void GetMusicEntry(string name, out string lookup, out Entry? entry)
    {
        lookup = ArchiveCollection.Definitions.Language.GetMessage(name);
        entry = ArchiveCollection.Entries.FindByName(lookup);
    }

    private bool PlayMusic(byte[] data, MusicFlags flags)
    {
        var musicPlayerOptions = MusicPlayerOptions.IgnoreAlreadyPlaying;
        if ((flags & MusicFlags.Loop) != 0)
            musicPlayerOptions |= MusicPlayerOptions.Loop;
        return AudioSystem.Music.Play(data, musicPlayerOptions);
    }

    public void HandleMouseMovement(IConsumableInput input)
    {
        HandleMouseLook(input);
    }

    public void HandleKeyInput(IConsumableInput input)
    {
        if (!Paused)
            CheatManager.HandleInput(Player, input);
    }

    public void SetTickCommand(Player player, TickCommand tickCommand)
    {
        player.TickCommand = tickCommand;

        if (PlayingDemo && player.PlayerNumber != CameraPlayer.CameraPlayerId)
            return;

        tickCommand.MouseAngle += player.ViewAngleRadians;
        tickCommand.MousePitch += player.ViewPitchRadians;

        player.ViewAngleRadians = 0;
        player.ViewPitchRadians = 0;

        if (tickCommand.HasTurnKey() || tickCommand.HasLookKey())
            player.TurnTics++;
        else
            player.TurnTics = 0;

        if (tickCommand.Has(TickCommands.TurnLeft))
            tickCommand.AngleTurn += player.GetTurnAngle();
        if (tickCommand.Has(TickCommands.TurnRight))
            tickCommand.AngleTurn -= player.GetTurnAngle();

        if (tickCommand.Has(TickCommands.LookUp))
            tickCommand.PitchTurn += player.GetTurnAngle();
        if (tickCommand.Has(TickCommands.LookDown))
            tickCommand.PitchTurn -= player.GetTurnAngle();

        if (tickCommand.Has(TickCommands.Forward))
            tickCommand.ForwardMoveSpeed += player.GetForwardMovementSpeed();
        if (tickCommand.Has(TickCommands.Backward))
            tickCommand.ForwardMoveSpeed -= player.GetForwardMovementSpeed();
        if (tickCommand.Has(TickCommands.Right))
            tickCommand.SideMoveSpeed += player.GetSideMovementSpeed();
        if (tickCommand.Has(TickCommands.Left))
            tickCommand.SideMoveSpeed -= player.GetSideMovementSpeed();

        if (tickCommand.Has(TickCommands.Strafe))
        {
            if (tickCommand.Has(TickCommands.TurnRight))
                tickCommand.SideMoveSpeed += player.GetSideMovementSpeed();
            if (tickCommand.Has(TickCommands.TurnLeft))
                tickCommand.SideMoveSpeed -= player.GetSideMovementSpeed();

            tickCommand.SideMoveSpeed -= tickCommand.MouseAngle * 16;
        }

        // SR-50 bug is that side movement speed was clamped by the forward run speed
        tickCommand.SideMoveSpeed = Math.Clamp(tickCommand.SideMoveSpeed, -Player.ForwardMovementSpeedRun, Player.ForwardMovementSpeedRun);
        tickCommand.ForwardMoveSpeed = Math.Clamp(tickCommand.ForwardMoveSpeed, -Player.ForwardMovementSpeedRun, Player.ForwardMovementSpeedRun);
    }

    public override bool EntityUse(Entity entity)
    {
        if (entity.IsPlayer && entity.IsDead())
        {
            if (IsMultiPlayer)
            {
                var respawnPlayer = RespawnPlayer(Player);
                if (respawnPlayer != null)
                {
                    SoundManager.MakeSoundsNotRelativeTo(Player);
                    Player = respawnPlayer;
                }
            }
            else
            {
                ResetLevel(Config.Game.LoadLatestOnDeath);
            }

            return false;
        }

        return base.EntityUse(entity);
    }

    public override void OnTryEntityUseLine(Entity entity, Line line)
    {
        MarkSpecials.Mark(this, entity, line, Gametick);
        base.OnTryEntityUseLine(entity, line);
    }

    public override bool ActivateSpecialLine(Entity entity, Line line, ActivationContext context, double originX, double originY)
    {
        MarkSpecials.Mark(this, entity, line, Gametick);

        bool success = base.ActivateSpecialLine(entity, line, context, originX, originY);
        if (success && m_renderDistanceOverride > 0 && m_renderDistanceOverrideTags.Contains(line.TagArg))
            ArchiveCollection.Config.Render.MaxDistance.Set(m_renderDistanceOverride, false);

        return success;
    }

    public override void ToggleChaseCameraMode()
    {
        m_chaseCamMode = !m_chaseCamMode;
        if (m_chaseCamMode)
            DisplayMessage("Chase camera activated.");
        else
            DisplayMessage("Chase camera deactivated.");

        DrawHud = !m_chaseCamMode;

        if (m_chaseCamMode)
        {
            ChaseCamPlayer.Position = Player.Position;
            ChaseCamPlayer.AngleRadians = Player.AngleRadians;
            ChaseCamPlayer.PitchRadians = Player.PitchRadians;
            ChaseCamPlayer.Velocity = Vec3D.Zero;
            ChaseCamPlayer.Cheats.ClearCheats();

            foreach (CheatType cheat in ChaseCameraCheats)
            {
                if (Player.Cheats.IsCheatActive(cheat))
                    ChaseCamPlayer.Cheats.SetCheatActive(cheat);
            }

            if (Player.Inventory.IsPowerupActive(PowerupType.ComputerAreaMap))
                ChaseCamPlayer.Cheats.SetCheatActive(CheatType.AutoMapModeShowAllLines);

            ChaseCamPlayer.ResetInterpolation();

            if (PlayingDemo)
                base.Resume();
            else
                base.Pause();
        }
        else
        {
            base.Resume();
        }
    }

    public override void Pause(PauseOptions options = PauseOptions.None)
    {
        base.Pause(options);
    }

    public override void Resume()
    {
        if (m_chaseCamMode && !PlayingDemo)
            return;

        if (!m_chaseCamMode)
            DrawHud = true;

        base.Resume();
    }

    protected override void PerformDispose()
    {
        CheatManager.CheatActivationChanged -= Instance_CheatActivationChanged;

        Config.Player.Name.OnChanged -= PlayerName_OnChanged;
        Config.Player.Gender.OnChanged -= PlayerGender_OnChanged;
        Config.Render.AutomapBspThread.OnChanged -= AutomapBspThread_OnChanged;
        Config.Game.MarkSpecials.OnChanged -= MarkSpecials_OnChanged;

        base.PerformDispose();
    }

    private void Instance_CheatActivationChanged(object? sender, CheatEventArgs e)
    {
        ActivateCheat(e.Player, e.Cheat);
    }

    private void HandleMouseLook(IConsumableInput input)
    {
        Player player = GetCameraPlayer();

        if (player.IsFrozen() || player.IsDead() || WorldState == WorldState.Exit || (WorldStatic.World.PlayingDemo && !player.IsCamera))
            return;

        Vec2I pixelsMoved = input.ConsumeMouseMove();
        if (pixelsMoved.X != 0 || pixelsMoved.Y != 0)
        {
            Vec2F moveDelta = pixelsMoved.Float / (float)Config.Mouse.PixelDivisor;
            moveDelta.X *= (float)(Config.Mouse.Sensitivity * Config.Mouse.Yaw);
            moveDelta.Y *= (float)(Config.Mouse.Sensitivity * Config.Mouse.Pitch);

            player.AddToYaw(moveDelta.X, true);

            if ((Config.Mouse.Look && !MapInfo.HasOption(MapOptions.NoFreelook)) || IsChaseCamMode)
            {
                float factorY = Config.Mouse.InvertY ? -1 : 1;
                player.AddToPitch(moveDelta.Y * factorY, true);
            }
        }

        if (m_firstUpdate)
        {
            // Reset gyro on first frame, so we're not looking in a weird direction at level start
            input.Manager.AnalogAdapter?.ZeroGyroAbsolute();
            m_firstUpdate = false;
            return;
        }

        if (input.Manager.AnalogAdapter != null)
        {
            double pitch = 0.0;
            double gyroSpeed;
            double slowFastFactor = 0.0;

            bool hasYaw = input.Manager.AnalogAdapter.TryGetGyroAbsolute((GyroAxis)(int)Config.Controller.GyroAimTurnAxis.Value, out double yaw);
            bool hasPitch = ((Config.Mouse.Look && !MapInfo.HasOption(MapOptions.NoFreelook)) || IsChaseCamMode)
                && input.Manager.AnalogAdapter.TryGetGyroAbsolute(GyroAxis.Pitch, out pitch);

            if (hasYaw)
            {
                gyroSpeed = hasPitch
                    ? Math.Sqrt(yaw * yaw + pitch * pitch) * 360 / Math.Tau
                    : yaw * 360 / Math.Tau;
                slowFastFactor = (gyroSpeed - Config.Controller.LowerGyroThreshold) / (Config.Controller.UpperGyroThreshold - Config.Controller.LowerGyroThreshold);
                player.AddToYaw((float)(yaw * (Config.Controller.GyroAimHorizontalSensitivity + (Config.Controller.GyroAcceleration * slowFastFactor))), true);
            }

            if (hasPitch)
            {
                pitch *= Config.Controller.GyroVerticalAimInvert ? -1 : 1;
                player.AddToPitch((float)(pitch * (Config.Controller.GyroAimVerticalSensitivity + (Config.Controller.GyroAcceleration * slowFastFactor))), true);
            }

            input.Manager.AnalogAdapter.ZeroGyroAbsolute();
        }
    }
}
