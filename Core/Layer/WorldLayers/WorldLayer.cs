using Helion.Audio;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.World;

namespace Helion.Layer.WorldLayers
{
    /// <summary>
    /// A layer that represents a world which can be interacted with. This is
    /// extendable for single player worlds, client worlds, demos, etc.
    /// </summary>
    public abstract class WorldLayer : GameLayer
    {
        protected readonly Config Config;
        protected readonly HelionConsole Console;
        protected readonly ArchiveCollection ArchiveCollection;
        protected readonly IAudioSystem AudioSystem;
        protected override double Priority => 0.25f;

        public abstract WorldBase World { get; }

        public WorldLayer(GameLayer parent, Config config, HelionConsole console, ArchiveCollection archiveCollection,
            IAudioSystem audioSystem)
        {
            Config = config;
            Console = console;
            ArchiveCollection = archiveCollection;
            AudioSystem = audioSystem;
            Parent = parent;
        }

        public override void RunLogic()
        {
            if (Parent != null)
            {
                // If something is on top of our world (such as a menu, or a
                // console) then we should pause it. Likewise, if we're at the
                // top layer, then we should make sure we're not paused (like
                // if the user just removed the menu or console).
                if (Parent.IsTopLayer(this))
                {
                    if (World.Paused)
                        World.Resume();
                }
                else if (!World.Paused)
                {
                    World.Pause();
                }
            }

            base.RunLogic();
        }
    }
}