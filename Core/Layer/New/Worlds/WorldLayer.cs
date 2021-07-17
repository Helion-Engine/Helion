using System;
using Helion.Audio;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.World.Impl.SinglePlayer;

namespace Helion.Layer.New.Worlds
{
    public partial class WorldLayer : IGameLayerParent
    {
        public IntermissionLayerNew? Intermission { get; private set; }
        public SinglePlayerWorld? World { get; private set; }
        private readonly Config Config;
        private readonly HelionConsole Console;
        private readonly ArchiveCollection ArchiveCollection;
        private readonly IAudioSystem AudioSystem;
        private readonly GameLayerManager Parent;
        private bool m_disposed;
        
        public WorldLayer(GameLayerManager parent, Config config, HelionConsole console, ArchiveCollection archiveCollection,
            IAudioSystem audioSystem, SinglePlayerWorld world)
        {
            Config = config;
            Console = console;
            ArchiveCollection = archiveCollection;
            AudioSystem = audioSystem;
            Parent = parent;
            World = world;
        }

        public void Remove(object layer)
        {
            if (ReferenceEquals(layer, Intermission))
            {
                Intermission?.Dispose();
                Intermission = null;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            Intermission?.Dispose();
            Intermission = null;

            m_disposed = true;
        }
    }
}
