using System;
using System.Linq;
using Helion.Audio;
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
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Physics;
using NLog;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Util.Profiling;
using Helion.Window;
using static Helion.Util.Assertion.Assert;
using static Helion.World.Entities.EntityManager;
using Helion.Util.Container;
using Helion.Util.RandomGenerators;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;

namespace Helion.World.Impl.SinglePlayer;

public class SinglePlayerWorld : WorldBase
{
    private static int StaticId;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly AutomapMarker m_automapMarker;
    private bool m_chaseCamMode;

    public int Id = StaticId++;

    public override Player Player { get; protected set; }
    public readonly Player ChaseCamPlayer;
    public override bool IsChaseCamMode => m_chaseCamMode;
    public override Player GetCameraPlayer()
    { 
        if (m_chaseCamMode)
            return ChaseCamPlayer;
        return Player;
    }

    public SinglePlayerWorld(GlobalData globalData, IConfig config, ArchiveCollection archiveCollection,
        IAudioSystem audioSystem, Profiler profiler, MapGeometry geometry, MapInfoDef mapDef, SkillDef skillDef,
        IMap map, Player? existingPlayer = null, WorldModel? worldModel = null, IRandom? random = null)
        : base(globalData, config, archiveCollection, audioSystem, profiler, geometry, mapDef, skillDef, map, worldModel, random)
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

            if (existingPlayer != null && !existingPlayer.IsDead && !mapDef.HasOption(MapOptions.ResetInventory))
            {
                Player.CopyProperties(existingPlayer);
                Player.Inventory.ClearKeys();
                Player.Flags.Shadow = false;
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

            for (var entity = EntityManager.Head; entity != null; entity = entity.Next)
                EntityManager.FinalizeFromWorldLoad(result, entity);

            SpecialManager.AddSpecialModels(worldModel.Specials);
        }

        if (config.Game.MonsterCloset.Value)
            ClosetClassifier.Classify(this);

        CheatManager.CheatActivationChanged += Instance_CheatActivationChanged;

        config.Player.Name.OnChanged += PlayerName_OnChanged;
        config.Player.Gender.OnChanged += PlayerGender_OnChanged;
        config.Render.AutomapBspThread.OnChanged += AutomapBspThread_OnChanged;
        config.Game.MarkSpecials.OnChanged += MarkSpecials_OnChanged;

        // Right now lazy loading from zip causes a noticeable delay. Preload to prevent stutter.
        SoundManager.CacheSound("misc/secret");
        ChaseCamPlayer = EntityManager.CreateCameraPlayer(Player);
        ChaseCamPlayer.Flags.Invisible = true;
        ChaseCamPlayer.Flags.NoClip = true;
        ChaseCamPlayer.Flags.NoGravity = true;
        ChaseCamPlayer.Flags.NoBlockmap = true;
        ChaseCamPlayer.Flags.NoSector = true;

        m_automapMarker = new AutomapMarker(ArchiveCollection);
    }

    private void MarkSpecials_OnChanged(object? sender, bool e)
    {
        MarkSpecials.Clear(Player);
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
        if (Config.Game.AutoAim)
            GetAutoAimEntity(Player, Player.HitscanAttackPos, Player.AngleRadians, Constants.EntityShootDistance, out _, out entity);
        else
            entity = FireHitscan(Player, Player.AngleRadians, Player.PitchRadians, Constants.EntityShootDistance, 0);

        return entity != null && !entity.Flags.Friendly && entity.Health > 0;
    }

    private void PlayerName_OnChanged(object? sender, string name) => Player.Info.Name = name;
    private void PlayerGender_OnChanged(object? sender, PlayerGender gender) =>  Player.Info.Gender = gender;

    private void ApplyCheats(WorldModel worldModel)
    {
        foreach (PlayerModel playerModel in worldModel.Players)
        {
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

            Lines[lineModel.Id].ApplyLineModel(this, lineModel);
        }
    }

    private void ApplySectorModels(WorldModel worldModel, WorldModelPopulateResult result)
    {
        for (int i = 0; i < worldModel.Sectors.Count; i++)
        {
            SectorModel sectorModel = worldModel.Sectors[i];
            if (sectorModel.Id < 0 || sectorModel.Id >= Sectors.Count)
                continue;

            Sectors[sectorModel.Id].ApplySectorModel(this, sectorModel, result);
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
        if (!PlayLevelMusic(AudioSystem, MapInfo.Music, ArchiveCollection))
            AudioSystem.Music.Stop();

        m_automapMarker.Start(this);
    }

    public static bool PlayLevelMusic(IAudioSystem audioSystem, string entryName, ArchiveCollection archiveCollection)
    {
        if (string.IsNullOrWhiteSpace(entryName))
            return false;

        Entry? entry = archiveCollection.Entries.FindByName(archiveCollection.Definitions.Language.GetMessage(entryName));
        if (entry == null)
        {
            Log.Warn("Cannot find music track: {0}", entryName);
            return false;
        }

        byte[] data = entry.ReadData();
        byte[]? midiData = MusToMidi.Convert(data);
        if (midiData == null)
        {
            Log.Warn("Unable to play music, cannot convert from MUS to MIDI");
            return false;
        }

        bool playingSuccess = audioSystem.Music.Play(midiData);
        if (!playingSuccess)
            Log.Warn("Unable to play MIDI track through device");
        return playingSuccess;
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

        if (PlayingDemo && player.PlayerNumber != short.MaxValue)
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
        if (entity.IsPlayer && entity.IsDead)
            ResetLevel(Config.Game.LoadLatestOnDeath);

        return base.EntityUse(entity);
    }

    public override void OnTryEntityUseLine(Entity entity, Line line)
    {
        MarkSpecials.Mark(this, entity, line);
        base.OnTryEntityUseLine(entity, line);
    }

    public override bool ActivateSpecialLine(Entity entity, Line line, ActivationContext context)
    {
        MarkSpecials.Mark(this, entity, line);
        return base.ActivateSpecialLine(entity, line, context);
    }

    public override void ToggleChaseCameraMode()
    {
        m_chaseCamMode = !m_chaseCamMode;
        string activated = m_chaseCamMode ? "activated" : "deactivated";
        Log.Info($"Chase camera {activated}.");

        DrawHud = !m_chaseCamMode;

        if (m_chaseCamMode)
        {
            ChaseCamPlayer.Position = Player.Position;
            ChaseCamPlayer.AngleRadians = Player.AngleRadians;
            ChaseCamPlayer.PitchRadians = Player.PitchRadians;
            ChaseCamPlayer.Velocity = Vec3D.Zero;
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

        if (player.IsFrozen || player.IsDead || WorldState == WorldState.Exit || (WorldStatic.World.PlayingDemo && !player.IsCamera))
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
    }
}
