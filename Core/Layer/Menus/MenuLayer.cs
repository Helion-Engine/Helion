using System.Collections.Generic;
using System.Diagnostics;
using Helion.Audio.Sounds;
using Helion.Menus;
using Helion.Menus.Impl;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.World.Save;

namespace Helion.Layer.Menus
{
    public partial class MenuLayer : IGameLayer
    {
        private readonly GameLayerManager m_parent;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly Stack<Menu> m_menus = new();
        private readonly SoundManager m_soundManager;
        private readonly Stopwatch m_stopwatch = new();
        private bool m_disposed;

        public MenuLayer(GameLayerManager parent, Config config, HelionConsole console, 
            ArchiveCollection archiveCollection, SoundManager soundManager, SaveGameManager saveGameManager)
        {
            m_parent = parent;
            m_archiveCollection = archiveCollection;
            m_soundManager = soundManager;
            m_stopwatch.Start();

            // TODO: This is going to be an invasive change for the first arg...
            MainMenu mainMenu = new(null!, config, console, soundManager, archiveCollection, saveGameManager);
            m_menus.Push(mainMenu);
        }
        
        public void RunLogic()
        {
            // No logic.
        }
        
        public void Dispose()
        {
            if (m_disposed)
                return;
            
            m_menus.Clear();

            m_disposed = true;
        }
    }
}
