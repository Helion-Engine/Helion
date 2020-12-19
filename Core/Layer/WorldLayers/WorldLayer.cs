using Helion.Audio;
using Helion.Resource;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Worlds;

namespace Helion.Layer.WorldLayers
{
    /// <summary>
    /// A layer that represents a world which can be interacted with. This is
    /// extendable for single player worlds, client worlds, demos, etc.
    /// </summary>
    public abstract class WorldLayer : GameLayer
    {
        protected override CIString Name { get; } = "WORLD";
        protected override double Priority { get; } = 0.25f;
        protected readonly Config Config;
        protected readonly HelionConsole Console;
        protected readonly Resources Resources;
        protected readonly IAudioSystem AudioSystem;

        public abstract World World { get; }

        public WorldLayer(Config config, HelionConsole console, Resources resources, IAudioSystem audioSystem)
        {
            Config = config;
            Console = console;
            Resources = resources;
            AudioSystem = audioSystem;
        }
    }
}