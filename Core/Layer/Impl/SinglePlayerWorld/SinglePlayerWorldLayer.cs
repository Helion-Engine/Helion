using Helion.Cheats;
using Helion.Input;
using Helion.Maps;
using Helion.Render.Commands;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Time;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using NLog;
using System;

namespace Helion.Layer.Impl
{
    public class SinglePlayerWorldLayer : GameLayer
    {
        public event EventHandler LevelExit;

        private const int TickOverflowThreshold = (int)(10 * Constants.TicksPerSecond);
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Ticker m_ticker = new Ticker(Constants.TicksPerSecond);
        private readonly Config m_config;
        private SinglePlayerWorld m_world;
        private TickerInfo m_lastTickInfo = new TickerInfo(0, 0);
        private TickCommand m_tickCommand = new TickCommand();
        private bool m_firstInputHandling = true;
        private (ConfigValue<InputKey>, TickCommands)[] m_consumeKeys;
        private (ConfigValue<InputKey>, TickCommands)[] m_consumeDownKeys;

        public SinglePlayerWorldLayer(Config config)
            : base(config)
        {
            m_config = config;
            m_consumeKeys = new (ConfigValue<InputKey>, TickCommands)[]
            {
                (Config.Engine.Controls.MoveForward,    TickCommands.Forward),
                (Config.Engine.Controls.MoveLeft,       TickCommands.Left),
                (Config.Engine.Controls.MoveBackward,   TickCommands.Backward),
                (Config.Engine.Controls.MoveRight,      TickCommands.Right),
                (Config.Engine.Controls.Jump,           TickCommands.Jump),
                (Config.Engine.Controls.Crouch,         TickCommands.Crouch),
            };

            m_consumeDownKeys = new (ConfigValue<InputKey>, TickCommands)[]
            {
                (Config.Engine.Controls.Use,            TickCommands.Use),
            };
        }

        public bool LoadMap(string mapName, ArchiveCollection archiveCollection)
        {
            m_ticker.Stop();

            (IMap? map, MapEntryCollection? collection) = archiveCollection.FindMap(mapName);
            if (map == null || collection == null)
            {
                Log.Warn("Unable to find map {0}", mapName);
                return false;
            }

            SinglePlayerWorld? world = SinglePlayerWorld.Create(m_config, archiveCollection, map, collection);
            if (world != null)
            {
                world.LevelExit += World_LevelExit;
                m_world = world;
                m_ticker.Start();
                return true;
            }

            Log.Warn("Map is corrupt, unable to create map {0}", mapName);
            return false;
        }

        private void World_LevelExit(object sender, EventArgs e)
        {
            LevelExit?.Invoke(this, e);
        }

        public override void HandleInput(ConsumableInput consumableInput)
        {
            // TODO: We ignore the first command for now because it is an
            //       accumulation of the mouse movement at the beginning of
            //       the window loading. We should fix this there later on.
            if (m_firstInputHandling)
            {
                m_firstInputHandling = false;
                return;
            }

            CheatManager.Instance.HandleInput(consumableInput);
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
            renderCommands.DrawWorld(m_world, camera, m_lastTickInfo.Ticks, m_lastTickInfo.Fraction);
        }

        protected override double GetPriority() => 0.25;

        private void HandleMovementInput(ConsumableInput consumableInput)
        {
            foreach (var consumeKey in m_consumeKeys)
            {
                if (consumableInput.ConsumeKeyPressedOrDown(consumeKey.Item1))
                    m_tickCommand.Add(consumeKey.Item2);
            }

            foreach (var consumeKey in m_consumeDownKeys)
            {
                if (consumableInput.ConsumeKeyPressed(consumeKey.Item1))
                    m_tickCommand.Add(consumeKey.Item2);
            }
        }
    }
}