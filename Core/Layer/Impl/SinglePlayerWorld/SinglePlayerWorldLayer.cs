using Helion.Input;
using Helion.Maps;
using Helion.Projects;
using Helion.Render.Commands;
using Helion.Util;
using Helion.Util.Time;
using Helion.World.Impl.SinglePlayer;
using NLog;

namespace Helion.Layer.Impl
{
    public class SinglePlayerWorldLayer : GameLayer
    {
        private const int TickOverflowThreshold = (int)(10 * Constants.TicksPerSecond);
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly SinglePlayerWorld world;
        private readonly Ticker ticker = new Ticker(Constants.TicksPerSecond);
        private TickerInfo lastTickInfo = new TickerInfo(0, 0);

        private SinglePlayerWorldLayer(SinglePlayerWorld singlePlayerWorld)
        {
            world = singlePlayerWorld;
            
            ticker.Start();
        }

        protected override double GetPriority() => 0.25;
        
        public override void HandleInput(ConsumableInput consumableInput)
        {
            world.HandleFrameInput(consumableInput);
        }

        public override void RunLogic()
        {
            lastTickInfo = ticker.GetTickerInfo();
            int ticksToRun = lastTickInfo.Ticks;

            if (ticksToRun > TickOverflowThreshold)
            {
                log.Warn("Large tick overflow detected (likely due to delays/lag), reducing ticking amount");
                ticksToRun = 1;
            }

            while (ticksToRun >= 0)
            {
                world.Tick();
                ticksToRun--;
            }
        }

        public override void Render(RenderCommands renderCommands)
        {
            renderCommands.DrawWorld(world, world.Camera, lastTickInfo.Ticks, lastTickInfo.Fraction);
        }

        // TODO: Want to do Expected<...> so we can report an error reason?
        public static SinglePlayerWorldLayer? Create(UpperString mapName, Project project)
        {
            (Map? map, MapEntryCollection? collection) = project.GetMap(mapName);
            if (map == null || collection == null)
            {
                log.Warn("Unable to find map {0}", mapName);
                return null;
            }
            
            SinglePlayerWorld? world = SinglePlayerWorld.Create(project, map, collection);
            if (world != null)
                return new SinglePlayerWorldLayer(world);
            
            log.Warn("Map is corrupt, unable to create map {0}", mapName);
            return null;
        }
    }
}
