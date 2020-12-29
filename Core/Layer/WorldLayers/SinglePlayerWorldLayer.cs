using System;
using Helion.Audio;
using Helion.Input;
using Helion.Maps;
using Helion.Render.Commands;
using Helion.Render.Shared;
using Helion.Render.Shared.Drawers;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
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
        private readonly (ConfigValue<InputKey>, TickCommands)[] m_consumeDownKeys;
        private readonly (ConfigValue<InputKey>, TickCommands)[] m_consumePressedKeys;
        private readonly WorldHudDrawer m_worldHudDrawer;
        private TickerInfo m_lastTickInfo = new(0, 0);
        private TickCommand m_tickCommand = new();
        private SinglePlayerWorld m_world;

        public override WorldBase World => m_world;

        private SinglePlayerWorldLayer(Config config, HelionConsole console, ArchiveCollection archiveCollection,
            IAudioSystem audioSystem, SinglePlayerWorld world)
            : base(config, console, archiveCollection, audioSystem)
        {
            m_world = world;
            m_worldHudDrawer = new(archiveCollection);
            AddWorldEventListeners(m_world);

            m_ticker.Start();

            m_consumeDownKeys = new[]
            {
                (config.Engine.Controls.MoveForward,    TickCommands.Forward),
                (config.Engine.Controls.MoveLeft,       TickCommands.Left),
                (config.Engine.Controls.MoveBackward,   TickCommands.Backward),
                (config.Engine.Controls.MoveRight,      TickCommands.Right),
                (config.Engine.Controls.Jump,           TickCommands.Jump),
                (config.Engine.Controls.Crouch,         TickCommands.Crouch),
                (config.Engine.Controls.Attack,         TickCommands.Attack),
            };

            m_consumePressedKeys = new[]
            {
                (config.Engine.Controls.Use,            TickCommands.Use),
                (config.Engine.Controls.NextWeapon,     TickCommands.NextWeapon),
                (config.Engine.Controls.PreviousWeapon, TickCommands.PreviousWeapon),
                (config.Engine.Controls.WeaponSlot1,    TickCommands.WeaponSlot1),
                (config.Engine.Controls.WeaponSlot2,    TickCommands.WeaponSlot2),
                (config.Engine.Controls.WeaponSlot3,    TickCommands.WeaponSlot3),
                (config.Engine.Controls.WeaponSlot4,    TickCommands.WeaponSlot4),
                (config.Engine.Controls.WeaponSlot5,    TickCommands.WeaponSlot5),
                (config.Engine.Controls.WeaponSlot6,    TickCommands.WeaponSlot6),
                (config.Engine.Controls.WeaponSlot7,    TickCommands.WeaponSlot7),
            };
        }

        ~SinglePlayerWorldLayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public static SinglePlayerWorldLayer? Create(Config config, HelionConsole console, IAudioSystem audioSystem,
            ArchiveCollection archiveCollection, IMap map)
        {
            TextureManager.Init(archiveCollection);
            CheatManager.Instance.Clear();
            SinglePlayerWorld? world = CreateWorldGeometry(config, audioSystem, archiveCollection, map);
            if (world == null)
                return null;
            return new SinglePlayerWorldLayer(config, console, archiveCollection, audioSystem, world);
        }

        private static SinglePlayerWorld? CreateWorldGeometry(Config config, IAudioSystem audioSystem,
            ArchiveCollection archiveCollection, IMap map, Player? existingPlayer = null)
        {
            MapGeometry? geometry = GeometryBuilder.Create(map, config);
            if (geometry == null)
                return null;

            return new SinglePlayerWorld(config, archiveCollection, audioSystem, geometry, map, existingPlayer);
        }

        public void LoadMap(string mapName, bool keepPlayer)
        {
            IMap? map = ArchiveCollection.FindMap(mapName);
            if (map == null)
            {
                Log.Warn("Unable to find map {0}", mapName);
                return;
            }

            Player? existingPlayer = null;
            if (keepPlayer && !m_world.Player.IsDead)
                existingPlayer = m_world.Player;
            else
                CheatManager.Instance.Clear();

            SinglePlayerWorld? world = CreateWorldGeometry(Config, AudioSystem, ArchiveCollection, map, existingPlayer);
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
                    string nextLevelName = GetNextLevelName(m_world.MapName.ToString());
                    LoadMap(nextLevelName, true);
                    break;

                case LevelChangeType.SecretNext:
                    // TODO: When we have MAPINFO working, we can do this.
                    Log.Warn("Change level to secret type to be implemented...");
                    break;

                case LevelChangeType.SpecificLevel:
                    // TODO: Need to figure out this ExMx situation...
                    string levelNumber = e.LevelNumber.ToString().PadLeft(2, '0');
                    LoadMap($"MAP{levelNumber}", false);
                    break;

                case LevelChangeType.Reset:
                    LoadMap(m_world.MapName.ToString(), false);
                    break;
            }
        }

        private string GetNextLevelName(string currentName)
        {
            // TODO: We'd use MAPINFO here eventually!
            // TODO: This ugly function will be fixed with MAPINFO.

            if (currentName.Length == 4 && currentName.StartsWith("E", StringComparison.OrdinalIgnoreCase))
            {
                string episodeText = currentName[1].ToString();
                string numberText = currentName[3].ToString();

                if (int.TryParse(episodeText, out int episode) && int.TryParse(numberText, out int number))
                {
                    if (number == 8)
                        return $"E{episode + 1}M1";
                    if (number == 9)
                        return $"E{episode}M1"; // TODO: Obviously wrong... (ex: E1M4)
                    return $"E{episode}M{number + 1}";
                }

                Log.Warn("Unable to parse E#M# from {0}", currentName);
            }
            else if (currentName.Length == 5 && currentName.StartsWith("MAP", StringComparison.OrdinalIgnoreCase))
            {
                string mapNumberText = currentName.Substring(3);
                if (int.TryParse(mapNumberText, out int mapNumber))
                {
                    // TODO: Obviously wrong... (overshoots to 33+)
                    string nextMapNumbers = (mapNumber + 1).ToString().PadLeft(2, '0');
                    return $"MAP{nextMapNumbers}";
                }

                Log.Warn("Unable to parse MAP## from {0}", currentName);
            }
            else
                Log.Warn("Cannot predict next map from {0}, going back to map", currentName);

            Log.Warn("Returning to current level");
            return currentName;
        }

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