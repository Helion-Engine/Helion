using Helion.Audio;
using Helion.Input;
using Helion.Maps;
using Helion.Render.Commands;
using Helion.Render.Shared;
using Helion.Render.Shared.Drawers;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Values;
using Helion.Util.Consoles;
using Helion.Util.Time;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using Helion.World.Geometry;
using Helion.World.Geometry.Builder;
using Helion.World.Impl.SinglePlayer;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Layer.WorldLayers
{
    public class SinglePlayerWorldLayer : WorldLayer
    {
        private const int TickOverflowThreshold = (int)(10 * Constants.TicksPerSecond);
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Ticker m_ticker = new(Constants.TicksPerSecond);
        private readonly (ConfigValueEnum<InputKey>, TickCommands)[] m_consumeDownKeys;
        private readonly (ConfigValueEnum<InputKey>, TickCommands)[] m_consumePressedKeys;
        private readonly WorldHudDrawer m_worldHudDrawer;
        private TickerInfo m_lastTickInfo = new(0, 0);
        private TickCommand m_tickCommand = new();
        private SinglePlayerWorld m_world;

        public override WorldBase World => m_world;
        public MapInfoDef CurrentMap { get; set; }

        private SinglePlayerWorldLayer(Config config, HelionConsole console, ArchiveCollection archiveCollection,
            IAudioSystem audioSystem, SinglePlayerWorld world, MapInfoDef mapInfoDef)
            : base(config, console, archiveCollection, audioSystem)
        {
            CurrentMap = mapInfoDef;
            m_world = world;
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

        public static SinglePlayerWorldLayer? Create(Config config, HelionConsole console, IAudioSystem audioSystem,
            ArchiveCollection archiveCollection, MapInfoDef mapInfoDef, IMap map)
        {
            TextureManager.Init(archiveCollection, mapInfoDef);
            CheatManager.Instance.Clear();
            SinglePlayerWorld? world = CreateWorldGeometry(config, audioSystem, archiveCollection, mapInfoDef, map);
            if (world == null)
                return null;
            return new SinglePlayerWorldLayer(config, console, archiveCollection, audioSystem, world, mapInfoDef);
        }

        private static SinglePlayerWorld? CreateWorldGeometry(Config config, IAudioSystem audioSystem,
            ArchiveCollection archiveCollection, MapInfoDef mapDef, IMap map, Player? existingPlayer = null)
        {
            MapGeometry? geometry = GeometryBuilder.Create(map, config);
            if (geometry == null)
                return null;

            return new SinglePlayerWorld(config, archiveCollection, audioSystem, geometry, mapDef, map, existingPlayer);
        }

        public void LoadMap(MapInfoDef mapDef, bool keepPlayer)
        {
            IMap? map = ArchiveCollection.FindMap(mapDef.MapName);
            if (map == null)
            {
                Log.Warn("Unable to find map {0}", mapDef.MapName);
                return;
            }

            CurrentMap = mapDef;

            Player? existingPlayer = null;
            if (keepPlayer && !m_world.Player.IsDead)
                existingPlayer = m_world.Player;
            else
                CheatManager.Instance.Clear();

            SinglePlayerWorld? world = CreateWorldGeometry(Config, AudioSystem, ArchiveCollection, mapDef, map, existingPlayer);
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

        public override void HandleInput(ConsumableInput consumableInput)
        {
            HandleMovementInput(consumableInput);
            m_world.HandleFrameInput(consumableInput);
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
        }

        public override void Render(RenderCommands renderCommands)
        {
            Camera camera = m_world.Player.GetCamera(m_lastTickInfo.Fraction);
            Player player = m_world.Player;
            renderCommands.DrawWorld(m_world, camera, m_lastTickInfo.Ticks, m_lastTickInfo.Fraction, player);

            // TODO: Should not be passing the window dimension as the viewport.
            m_worldHudDrawer.Draw(player, m_world, m_lastTickInfo.Fraction, Console, renderCommands.WindowDimension,
                Config, renderCommands);
        }

        protected override void PerformDispose()
        {
            RemoveWorldEventListeners(m_world);

            m_world.Dispose();

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
                    // TODO: Need to figure out this ExMx situation...
                    //string levelNumber = e.LevelNumber.ToString().PadLeft(2, '0');
                    //LoadMap($"MAP{levelNumber}", false);
                    break;

                case LevelChangeType.Reset:
                    LoadMap(CurrentMap, false);
                    break;
            }
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

        private void HandleMovementInput(ConsumableInput consumableInput)
        {
            foreach (var (inputKey, command) in m_consumeDownKeys)
                if (consumableInput.ConsumeKeyPressedOrDown(inputKey))
                    m_tickCommand.Add(command);

            foreach (var (inputKey, command) in m_consumePressedKeys)
                if (consumableInput.ConsumeKeyPressed(inputKey))
                    m_tickCommand.Add(command);
        }
    }
}