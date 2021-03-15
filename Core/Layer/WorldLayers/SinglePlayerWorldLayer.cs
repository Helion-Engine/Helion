using Helion.Audio;
using Helion.Input;
using Helion.Maps;
using Helion.Maps.Shared;
using Helion.Render.Commands;
using Helion.Render.Shared;
using Helion.Render.Shared.Drawers;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.MapInfo;
using Helion.Models;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles;
using Helion.Util.Time;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Builder;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Save;
using Helion.World.StatusBar;
using Helion.World.Util;
using Newtonsoft.Json;
using NLog;
using System;
using System.Linq;
using System.Collections.Generic;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.WorldLayers
{
    public class SinglePlayerWorldLayer : WorldLayer
    {
        private const int TickOverflowThreshold = (int)(10 * Constants.TicksPerSecond);
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        public readonly SaveGameManager SaveManager;
        private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
        private readonly (ConfigValueEnum<Key>, TickCommands)[] m_consumeDownKeys;
        private readonly (ConfigValueEnum<Key>, TickCommands)[] m_consumePressedKeys;
        private readonly WorldHudDrawer m_worldHudDrawer;
        private TickerInfo m_lastTickInfo = new(0, 0);
        private TickCommand m_tickCommand = new();
        private SinglePlayerWorld m_world;
        private bool m_disposed;

        public override WorldBase World => m_world;
        public MapInfoDef CurrentMap { get; set; }

        private SinglePlayerWorldLayer(GameLayer parent, Config config, HelionConsole console, ArchiveCollection archiveCollection,
            IAudioSystem audioSystem, SinglePlayerWorld world, MapInfoDef mapInfoDef)
            : base(parent, config, console, archiveCollection, audioSystem)
        {
            CurrentMap = mapInfoDef;
            m_world = world;
            SaveManager = new SaveGameManager(Config);
            m_worldHudDrawer = new(archiveCollection);
            AddWorldEventListeners(m_world);

            m_ticker.Start();

            m_consumeDownKeys = new[]
            {
                (config.Controls.Forward,  TickCommands.Forward),
                (config.Controls.Left,     TickCommands.Left),
                (config.Controls.Backward, TickCommands.Backward),
                (config.Controls.Right,    TickCommands.Right),
                (config.Controls.Jump,     TickCommands.Jump),
                (config.Controls.Crouch,   TickCommands.Crouch),
                (config.Controls.Attack,   TickCommands.Attack),
            };

            m_consumePressedKeys = new[]
            {
                (config.Controls.Use,            TickCommands.Use),
                (config.Controls.NextWeapon,     TickCommands.NextWeapon),
                (config.Controls.PreviousWeapon, TickCommands.PreviousWeapon),
                (config.Controls.WeaponSlot1,    TickCommands.WeaponSlot1),
                (config.Controls.WeaponSlot2,    TickCommands.WeaponSlot2),
                (config.Controls.WeaponSlot3,    TickCommands.WeaponSlot3),
                (config.Controls.WeaponSlot4,    TickCommands.WeaponSlot4),
                (config.Controls.WeaponSlot5,    TickCommands.WeaponSlot5),
                (config.Controls.WeaponSlot6,    TickCommands.WeaponSlot6),
                (config.Controls.WeaponSlot7,    TickCommands.WeaponSlot7),
            };
        }

        ~SinglePlayerWorldLayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public static SinglePlayerWorldLayer? Create(GameLayer parent, Config config, HelionConsole console, 
            IAudioSystem audioSystem, ArchiveCollection archiveCollection, MapInfoDef mapInfoDef, 
            SkillDef skillDef, IMap map)
        {
            string displayName = mapInfoDef.NiceName;
            if (mapInfoDef.LookupName.Length > 0)
            {
                displayName = archiveCollection.Definitions.Language.GetIWadMessage(mapInfoDef.LookupName,
                    archiveCollection.GetIWadInfo().IWadBaseType, IWadLanguageMessageType.LevelName);
            }

            Log.Info($"{mapInfoDef.MapName}: {displayName}");
            TextureManager.Init(archiveCollection, mapInfoDef);
            SinglePlayerWorld? world = CreateWorldGeometry(config, audioSystem, archiveCollection, mapInfoDef, skillDef, map);
            if (world == null)
                return null;
            return new SinglePlayerWorldLayer(parent, config, console, archiveCollection, audioSystem, world, mapInfoDef);
        }

        private static SinglePlayerWorld? CreateWorldGeometry(Config config, IAudioSystem audioSystem,
            ArchiveCollection archiveCollection, MapInfoDef mapDef, SkillDef skillDef, IMap map, 
            Player? existingPlayer = null, WorldModel? worldModel = null)
        {
            MapGeometry? geometry = GeometryBuilder.Create(map, config);
            if (geometry == null)
                return null;

            return new SinglePlayerWorld(config, archiveCollection, audioSystem, geometry, mapDef, skillDef, map, 
                existingPlayer, worldModel);
        }

        public void LoadMap(MapInfoDef mapDef, bool keepPlayer, WorldModel? worldModel = null)
        {
            IMap? map = ArchiveCollection.FindMap(mapDef.MapName);
            if (map == null)
            {
                Log.Warn("Unable to find map {0}", mapDef.MapName);
                return;
            }

            CurrentMap = mapDef;

            Player? existingPlayer = null;
            SkillLevel skillLevel;
            if (worldModel == null)
            {
                if (keepPlayer && !m_world.Player.IsDead)
                    existingPlayer = m_world.Player;

                skillLevel = Config.Game.Skill;
            }
            else
            {
                skillLevel = worldModel.Skill;
            }

            SkillDef?  skillDef = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkill(skillLevel);
            if (skillDef == null)
            {
                Log.Warn($"Could not find skill definition for {skillLevel}");
                return;
            }

            // TODO there is some duplication with the static Create call and it's kind of messy
            // Should probably make this all work without the same Create so this function can be shared
            TextureManager.Init(ArchiveCollection, mapDef);
            SinglePlayerWorld? world = CreateWorldGeometry(Config, AudioSystem, ArchiveCollection, mapDef, skillDef, map, existingPlayer, worldModel);
            if (world == null)
            {
                Log.Error("Unable to load map {0}", map.Name);
                return;
            }

            m_ticker.Stop();
            RemoveWorldEventListeners(m_world);
            m_world.Dispose();

            m_world = world;
            AddWorldEventListeners(world);
            m_world.Start();
            m_ticker.Restart();
        }

        public override void HandleInput(InputEvent input)
        {
            if (!m_world.Paused)
            {
                HandleMovementInput(input);
                m_world.HandleFrameInput(input);
            }

            if (input.ConsumeTypedKey(Config.Controls.HudDecrease))
                ChangeHudSize(false);
            else if (input.ConsumeTypedKey(Config.Controls.HudIncrease))
                ChangeHudSize(true);
            else if (input.ConsumeTypedKey(Config.Controls.Save))
                SaveGame();
            else if (input.ConsumeTypedKey(Config.Controls.Load))
                LoadGame();
			
			base.HandleInput(input);
        }

        private void LoadGame()
        {
            List<SaveGame> saveGames = SaveManager.GetSaveGames();

            if (saveGames.Count > 0)
            {
                WorldModel? worldModel = saveGames[0].ReadWorldModel();
                if (worldModel == null)
                    Log.Error("Failed to load corrupt world.");
                else
                    LoadWorldModel(worldModel);
            }
        }

        private void SaveGame()
        {
            const string SaveTitle = "Save";
            bool success = true;
            try
            {
                IList<SaveGame> saveGames = SaveManager.GetSaveGames();
                if (saveGames.Count > 0)
                    SaveManager.WriteSaveGame(World, SaveTitle, saveGames[0]);
                else
                    SaveManager.WriteNewSaveGame(World, SaveTitle);
            }
            catch
            {
                success = false;
            }

            string msg;
            if (success)
                msg = "Game saved.";
            else
                msg = "Failed to save game.";

            m_world.DisplayMessage(World.EntityManager.Players[0], null, msg, LanguageMessageType.None);
        }

        public override void RunLogic()
        {
            m_lastTickInfo = m_ticker.GetTickerInfo();
            int ticksToRun = m_lastTickInfo.Ticks;

            if (ticksToRun <= 0)
                return;

            m_world.HandleTickCommand(m_tickCommand);
            m_tickCommand = new TickCommand();

            if (ticksToRun > TickOverflowThreshold)
            {
                Log.Warn("Large tick overflow detected (likely due to delays/lag), reducing ticking amount");
                ticksToRun = 1;
            }

            while (ticksToRun > 0)
            {
                m_world.Tick();
                ticksToRun--;
            }
            
            base.RunLogic();
        }

        public override void Render(RenderCommands renderCommands)
        {
            Camera camera = m_world.Player.GetCamera(m_lastTickInfo.Fraction);
            Player player = m_world.Player;
            renderCommands.DrawWorld(m_world, camera, m_lastTickInfo.Ticks, m_lastTickInfo.Fraction, player);

            // TODO: Should not be passing the window dimension as the viewport.
            m_worldHudDrawer.Draw(player, m_world, m_lastTickInfo.Fraction, Console, renderCommands.WindowDimension,
                Config, renderCommands);
            
            base.Render(renderCommands);
        }

        protected override void PerformDispose()
        {
            if (m_disposed)
                return;
            
            RemoveWorldEventListeners(m_world);
            m_world.Dispose();

            m_disposed = true;

            base.PerformDispose();
        }

        private void World_LevelExit(object? sender, LevelChangeEvent e)
        {
            switch (e.ChangeType)
            {
                case LevelChangeType.Next:
                    {
                        MapInfoDef? nextMap = GetNextLevel(CurrentMap);
                        // TODO implement endgame, this also stupidly assumes endgame
                        if (nextMap == null)
                        {
                            Log.Info("Your did it!!!");
                            return;
                        }
                        LoadMap(nextMap, true);
                    }
                    break;

                case LevelChangeType.SecretNext:
                    {
                        MapInfoDef? nextMap = GetNextSecretLevel(CurrentMap);
                        if (nextMap == null)
                        {
                            Log.Error($"Unable to find map {CurrentMap.SecretNext}");
                            return;
                        }
                        LoadMap(nextMap, true);
                    }
                    break;

                case LevelChangeType.SpecificLevel:
                    ChangeLevel(e);
                    break;

                case LevelChangeType.Reset:
                    LoadMap(CurrentMap, false);
                    break;
            }
        }

        private void LoadWorldModel(WorldModel worldModel)
        {
            if (!VerifyWorldModelFiles(worldModel))
                return;

            MapInfoDef? loadMap = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetMap(worldModel.MapName);
            if (loadMap == null)
            {
                Log.Error($"Unable to find map {worldModel.MapName}");
                return;
            }

            LoadMap(loadMap, false, worldModel);
        }

        private bool VerifyWorldModelFiles(WorldModel worldModel)
        {
            if (!VerifyFileModel(worldModel.IWad))
                return false;

            if (worldModel.Files.Any(x => !VerifyFileModel(x)))
                return false;

            return true;
        }

        private bool VerifyFileModel(FileModel fileModel)
        {
            if (fileModel.FileName == null)
            {
                Log.Warn("File in save game was null.");
                return true;
            }

            var archive = ArchiveCollection.GetArchiveByFileName(fileModel.FileName);
            if (archive == null)
            {
                Log.Error($"Required archive  {fileModel.FileName} for this save game is not loaded.");
                return false;
            }

            if (fileModel.MD5 == null)
            {
                Log.Warn("MD5 for file in save game was null.");
                return true;
            }

            if (!fileModel.MD5.Equals(archive.MD5))
            {
                Log.Error($"Required archive {fileModel.FileName} did not match MD5 for save game.");
                Log.Error($"Save MD5: {fileModel.MD5} - Loaded MD5: {archive.MD5}");
                return false;
            }

            return true;
        }

        private void ChangeLevel(LevelChangeEvent e)
        {
            if (MapWarp.GetMap(e.LevelNumber, ArchiveCollection.Definitions.MapInfoDefinition.MapInfo, 
                out MapInfoDef? mapInfoDef) && mapInfoDef != null)
                LoadMap(mapInfoDef, false);
        }

        private MapInfoDef? GetNextLevel(MapInfoDef mapDef) => ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextMap(mapDef);

        private MapInfoDef? GetNextSecretLevel(MapInfoDef mapDef) => ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetNextSecretMap(mapDef);

        private void AddWorldEventListeners(WorldBase world)
        {
            world.LevelExit += World_LevelExit;
        }

        private void RemoveWorldEventListeners(WorldBase world)
        {
            world.LevelExit -= World_LevelExit;
        }

        private void HandleMovementInput(InputEvent input)
        {
            foreach (var (inputKey, command) in m_consumeDownKeys)
                if (input.ConsumeKeyPressedOrDown(inputKey))
                    m_tickCommand.Add(command);

            foreach (var (inputKey, command) in m_consumePressedKeys)
                if (input.ConsumeKeyPressed(inputKey))
                    m_tickCommand.Add(command);
        }

        private void ChangeHudSize(bool increase)
        {
            int size = (int)Config.Hud.StatusBarSize.Value;
            if (increase)
                size++;
            else
                size--;

            size = Math.Clamp(size, 0, Enum.GetValues(typeof(StatusBarSizeType)).Length - 1);

            if ((int)Config.Hud.StatusBarSize.Value != size)
            {
                Config.Hud.StatusBarSize.Set((StatusBarSizeType)size);
                m_world.SoundManager.PlayStaticSound(Constants.MenuSounds.Change);
            }
        }
    }
}