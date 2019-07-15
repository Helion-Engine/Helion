using Helion.Input;
using Helion.Maps;
using Helion.Render.Commands;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Time;
using Helion.World.Entity.Player;
using Helion.World.Impl.SinglePlayer;
using NLog;

namespace Helion.Layer.Impl
{
    public class SinglePlayerWorldLayer : GameLayer
    {
        private const int TickOverflowThreshold = (int)(10 * Constants.TicksPerSecond);
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly SinglePlayerWorld m_world;
        private readonly Ticker m_ticker = new Ticker(Constants.TicksPerSecond);
        private TickerInfo m_lastTickInfo = new TickerInfo(0, 0);
        private TickCommand m_tickCommand = new TickCommand();

        private SinglePlayerWorldLayer(SinglePlayerWorld singlePlayerWorld)
        {
            m_world = singlePlayerWorld;
            
            m_ticker.Start();
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

            while (ticksToRun >= 0)
            {
                m_world.Tick();
                ticksToRun--;
            }
        }

        public override void Render(RenderCommands renderCommands)
        {
            renderCommands.DrawWorld(m_world, m_world.Camera, m_lastTickInfo.Ticks, m_lastTickInfo.Fraction);
        }

        public static SinglePlayerWorldLayer? Create(string mapName, Config config, ArchiveCollection archiveCollection)
        {
            (Map? map, MapEntryCollection? collection) = archiveCollection.FindMap(mapName);
            if (map == null || collection == null)
            {
                Log.Warn("Unable to find map {0}", mapName);
                return null;
            }
            
            SinglePlayerWorld? world = SinglePlayerWorld.Create(config, archiveCollection, map, collection);
            if (world != null)
                return new SinglePlayerWorldLayer(world);
            
            Log.Warn("Map is corrupt, unable to create map {0}", mapName);
            return null;
        }

        protected override double GetPriority() => 0.25;

        private void HandleMovementInput(ConsumableInput consumableInput)
        {
            if (consumableInput.ConsumeKeyPressedOrDown(InputKey.W))
                m_tickCommand.Add(TickCommands.Forward);
            if (consumableInput.ConsumeKeyPressedOrDown(InputKey.A))
                m_tickCommand.Add(TickCommands.Left);
            if (consumableInput.ConsumeKeyPressedOrDown(InputKey.S))
                m_tickCommand.Add(TickCommands.Backward);
            if (consumableInput.ConsumeKeyPressedOrDown(InputKey.D))
                m_tickCommand.Add(TickCommands.Right);
            if (consumableInput.ConsumeKeyPressedOrDown(InputKey.Space))
                m_tickCommand.Add(TickCommands.Jump);
            if (consumableInput.ConsumeKeyPressedOrDown(InputKey.C))
                m_tickCommand.Add(TickCommands.Crouch);
            if (consumableInput.ConsumeKeyPressedOrDown(InputKey.E))
                m_tickCommand.Add(TickCommands.Use);
        }
    }
}